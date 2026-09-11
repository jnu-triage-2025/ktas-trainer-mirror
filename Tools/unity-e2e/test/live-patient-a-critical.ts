import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {Platform,loadConfig} from '../src/core.ts';
import {waitForInteractable} from '../src/ui-navigation.ts';
import {Runner} from '../src/runner.ts';
import {monitorMemoryPressure} from '../src/memory-pressure.ts';
import {verifyPatientRoles} from '../src/patient-roles.ts';
import {questEvidence} from '../src/quest-evidence.ts';
import {waitForScenarioCompletion} from '../src/scenario-completion.ts';
import {auditRuntimeLogs} from '../src/runtime-log-audit.ts';
import {drivePatientBed} from '../src/vehicle-navigation.ts';
import {joinInstances} from '../src/startup.ts';
import {setTimeout as delay} from 'node:timers/promises';
import assert from 'node:assert/strict';
import {monitorEventLoopDelay} from 'node:perf_hooks';
import {execFile} from 'node:child_process';
import {promisify} from 'node:util';
import {fileURLToPath} from 'node:url';
// This runner is intentionally isolated from the live BC investigation.  Do
// not widen it to another graph: its artifacts are used to freeze the A route.
const requestedGraph=process.argv[3]??'patient_a_critical';
assert.equal(requestedGraph,'patient_a_critical','This isolated runner is only for patient_a_critical');
const graph:string=requestedGraph;
const stage=process.argv[4]??'entry';
const profile=process.argv[5]??'mac_direct';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,error:unknown,cleanupError:unknown,phase='launch',fullPlayPassed=false;
const memoryPressure=monitorMemoryPressure(performance.now());
const eventLoop=monitorEventLoopDelay({resolution:20});eventLoop.enable();
// The platform deliberately unrefs its background heartbeat, event collector
// and history worker.  That is correct for reusable tooling, but a standalone
// live route can otherwise leave Node with only an in-flight fetch/top-level
// await and make it exit silently between two gameplay steps.  Keep this test
// process alive until its own finally block has recorded terminal evidence.
const processKeepalive=setInterval(()=>{},1000);
const schedulingSamples:any[]=[];let lastSample=performance.now();
const schedulingTimer=setInterval(()=>{
 const now=performance.now();
 schedulingSamples.push({at:new Date().toISOString(),phase,intervalMs:now-lastSample,eventLoopMaxMs:eventLoop.max/1e6});
 if(schedulingSamples.length>300)schedulingSamples.shift();
 lastSample=now;eventLoop.reset();
},1000);schedulingTimer.unref();
try {
 const launch=await platform.launch(profile,'host_plus_3_clients');runId=launch.runId;
 const sourcePaths=[`../../../Assets/Modules/TriageTrainer/Resources/Scenario/${graph}.scenario.json`,
  `../../../Assets/Modules/TriageTrainer/Resources/Quest/${graph}.quests.quest.json`,
  './live-patient-a-critical.ts','../src/runner.ts','../src/vehicle-navigation.ts','../src/scenario-completion.ts',
  '../src/runtime-log-audit.ts','../src/quest-evidence.ts','../documentation/positions.md','../documentation/check_accessible.py',
  '../../../Assets/Scenes/OverworldScene.unity','../../../Assets/Scenes/OverworldSceneMarked.unity',
  '../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Player/PlayerController.Interactables.cs',
  '../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Automation/PlayerController.Automation.cs',
  '../../../Assets/Modules/MultiplayerInfrastructure/Scripts/Automation/AutomationBridge.cs'];
 const sourceHashes=await Promise.allSettled(sourcePaths.map(async path=>({path,sha256:createHash('sha256').update(await readFile(new URL(path,import.meta.url))).digest('hex')})));
 for(const result of sourceHashes)if(result.status==='rejected')throw result.reason;
 await platform.artifact(runId,'route-manifest.json',{graph,profile,stage,
  fullPlayPassed:false,promotion:'Requires both complete scenarios without recovery before freezing a regression route.',
  sources:sourceHashes.flatMap(result=>result.status==='fulfilled'?[result.value]:[]),
  execution:'Fixed role commands and routes with observed completion gates; per-instance control runs concurrently where independent.'});
 phase='join';
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const chat=async(id:string,text:string)=>{
  await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:'T'}]});
  assert.equal((await platform.observe(id)).inputContext,'ChatUIController');
  await platform.command(id,'ui.text',{text,mode:'input_adapter'});
  if(text.startsWith('/scenario execute '))await platform.command(id,'control.heartbeat');
  await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:'Return'}]});
 };
 phase='role_commands';
 const roleStarted=performance.now();
 const roleOutcomes=await Promise.allSettled(Object.values(actors).map(async(id,index)=>{
  const startedMs=performance.now()-roleStarted;
  await platform.acquire(id);await chat(id,'/'+['a','b','c','d'][index]);
  return {startedMs,finishedMs:performance.now()-roleStarted};
 }));
 await platform.artifact(runId,'parallel-role-commands.json',roleOutcomes.map((result,index)=>({actor:`p${index+1}`,state:result.status,
  ...(result.status==='fulfilled'?result.value:{error:String(result.reason)})})));
 for(const result of roleOutcomes)if(result.status==='rejected')throw result.reason;
 phase='role_verification';
 const roleDeadline=performance.now()+15000;
 while(true){
  const reads=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>[actor,await platform.observe(id,false,{includeStaticItems:false})] as const));
  for(const result of reads)if(result.status==='rejected')throw result.reason;
  const observations=Object.fromEntries(reads.flatMap(result=>result.status==='fulfilled'?[result.value]:[]));
  try {
   const assignments=verifyPatientRoles(observations);
   await platform.artifact(runId,'role-verification.json',{assignments,observations});break;
  } catch(e){
   if((e as any).code!=='PATIENT_ROLE_NOT_READY'||performance.now()>roleDeadline)throw e;
  }
  await delay(250);
 }
 await platform.artifact(runId,'role-entry.json',{actors,commands:['/a','/b','/c','/d'],graph,profile,scope:'Entry observation only; full quest completion remains required'});
 phase='observation_projection';
 const fullObservation=await platform.observe(actors.p1);
 const navigationObservation=await platform.observe(actors.p1,false,{includeStaticItems:false});
 assert.ok(Array.isArray(fullObservation.staticPlacedItems)&&fullObservation.staticPlacedItems.length>0);
 assert.equal(navigationObservation.staticPlacedItems,null);
 assert.ok(navigationObservation.client.players.some((player:any)=>player.local));
 await platform.artifact(runId,'observation-projection.json',{
  fullStaticItemCount:fullObservation.staticPlacedItems.length,
  navigationStaticItems:navigationObservation.staticPlacedItems,
  fullBytes:Buffer.byteLength(JSON.stringify(fullObservation)),
  navigationBytes:Buffer.byteLength(JSON.stringify(navigationObservation))
 });
 phase='scenario_start';
 await chat(actors.p1,`/scenario execute @a ${graph}`);
 phase='entry_observation';
 const entryDeadline=performance.now()+30000;
 const entryOutcomes=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>{
  while(true){
   const snapshot=await platform.observe(id);
   const expected=graph==='patient_a_critical'?'D_START_BROADCAST':'ANNOUNCE';
   if(snapshot.scenario.graphId===graph&&snapshot.dialogue.nodeId===expected){
    await platform.artifact(runId!,`entry-ready-${actor}.json`,snapshot);break;
   }
   if(performance.now()>entryDeadline)throw new Error('PATIENT_ENTRY_NOT_READY');
   await delay(500);
  }
 }));
 for(const result of entryOutcomes)if(result.status==='rejected')throw result.reason;
 if(['arrival','bed','full','triage','move','recognition'].includes(stage)){
  phase='arrival_movement';
  const runner=new Runner(platform);
  phase='arrival_dialogue';
  const advanceArrivalDialogue=async([actor,id]:[string,string])=>{
   const signal=AbortSignal.timeout(30000);
   while(true){
    signal.throwIfAborted();const state=await platform.observe(id);
    if(state.inputContext==='Gameplay')break;
    if(state.inputContext!=='DialoguePanelUIController'||state.dialogue.hasChoices)throw new Error('UNHANDLED_ARRIVAL_UI');
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)try{
     await runner.step({actors} as any,{id:`arrival_announcement_${actor}`,type:'dialogueAdvance',actor},signal);
    }catch(error){
     if((error as any)?.code!=='TARGET_NOT_INTERACTABLE'&&(error as any)?.code!=='STATE_CONFLICT')throw error;
    }
    await delay(200,undefined,{signal});
   }
  };
  // Dialogue ownership is a single-client UI lease, unlike the independent
  // clinical assignments below.  Advance this short broadcast sequentially
  // to prevent four simultaneous lease renewals from cancelling one client.
  const dialogueOutcomes:PromiseSettledResult<void>[]=[];
  for(const entry of Object.entries(actors))try{
   await advanceArrivalDialogue(entry);dialogueOutcomes.push({status:'fulfilled',value:undefined});
  }catch(reason){dialogueOutcomes.push({status:'rejected',reason});}
  await platform.artifact(runId,'arrival-dialogue.json',dialogueOutcomes.map((r,index)=>({actor:Object.keys(actors)[index],state:r.status,error:r.status==='rejected'?String(r.reason):undefined})));
  const dialogueFailure=dialogueOutcomes.find(r=>r.status==='rejected');
  if(dialogueFailure?.status==='rejected')throw dialogueFailure.reason;
  phase='arrival_movement';
  const target=graph==='patient_a_critical'?'scen_a:quest_arrival_patient_a':'scen_b:quest_arrival_triage_area';
  const outcomes:PromiseSettledResult<void>[]=[];
  for(const [actor,id] of Object.entries(actors)){
   try {
    // Cross the shared entrance before spreading inside the arrival volume.
    const offsets=graph==='patient_b_c_ct'
     ? [[0,0,-7],[0,0,-2],({p1:[-2,0,-1],p2:[0,0,1],p3:[2,0,-1],p4:[0,0,-1]} as Record<string,number[]>)[actor]]
     : [[0,0,0]];
    for(const [index,targetOffset] of offsets.entries())
     await runner.navigate(id,{id:`arrival_${actor}_${index}`,type:'navigate',actor,target,mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:graph==='patient_b_c_ct'?1:3,targetOffset}},AbortSignal.timeout(16000));
    outcomes.push({status:'fulfilled',value:undefined});
   }catch(reason){outcomes.push({status:'rejected',reason});}
  }
  await platform.artifact(runId,'arrival-movement.json',outcomes.map((r,index)=>({actor:Object.keys(actors)[index],state:r.status,error:r.status==='rejected'?String(r.reason):undefined})));
  const fatalMovement=outcomes.find(r=>r.status==='rejected'&&!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(r.reason)));
  if(fatalMovement?.status==='rejected')throw fatalMovement.reason;
  phase='arrival_quest_confirmation';
  const confirmQuest=graph==='patient_a_critical'?'Quest_Arrive_PatientA':'Quest_Arrive_Triage';
  const confirmations=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>{
   const signal=AbortSignal.timeout(20000);
   const confirmationDeadline=performance.now()+20000;
   for(let attempt=0;attempt<4;attempt++){
    const snapshot=await platform.observe(id);
    const quest=snapshot.localQuests?.find((q:any)=>q.definitionId===confirmQuest);
    if(quest?.completed)return;
    if(!quest){
     const initial=graph==='patient_a_critical'?'P_START_A':'P_ANNOUNCE_ARRIVAL';
     if(snapshot.scenario.graphId===graph&&snapshot.scenario.nodeId!==initial
       &&Array.isArray(snapshot.scenario.recoveryNotes)&&snapshot.scenario.recoveryNotes.length===0
       &&snapshot.localQuests.some((q:any)=>q.scenarioId===graph&&!q.placeholder&&q.definitionId&&q.definitionId!==confirmQuest))return;
     if(snapshot.scenario.graphId===graph
       &&Array.isArray(snapshot.scenario.recoveryNotes)&&snapshot.scenario.recoveryNotes.length===0
       &&performance.now()<confirmationDeadline){
      await delay(200,undefined,{signal});attempt--;continue;
     }
     await platform.artifact(runId!,`arrival-missing-${actor}.json`,snapshot);
     throw new Error(`ARRIVAL_QUEST_MISSING:${actor}`);
    }
    if(snapshot.inputContext!=='Gameplay')throw new Error(`ARRIVAL_INPUT_BLOCKED:${actor}`);
    await platform.command(id,'input.execute',{sequence:[{operation:'hold',key:actor==='p4'?'D':'A',durationMs:350}]},{signal});
    await runner.navigate(id,{id:`arrival_retry_${actor}`,type:'navigate',actor,target,mode:'input_adapter',timeoutMs:5000,args:{arrivalRadius:5.5}},signal);
    await delay(300,undefined,{signal});
   }
   throw new Error(`ARRIVAL_QUEST_INCOMPLETE:${actor}`);
  }));
  await platform.artifact(runId,'arrival-confirmation.json',confirmations.map((r,index)=>({actor:Object.keys(actors)[index],state:r.status,error:r.status==='rejected'?String(r.reason):undefined})));
  const confirmationFailure=confirmations.find(r=>r.status==='rejected');
  if(confirmationFailure?.status==='rejected')throw confirmationFailure.reason;
  phase='arrival_result_observation';
  const resultDeadline=performance.now()+20000;
  const initialNode=graph==='patient_a_critical'?'P_START_A':'P_ANNOUNCE_ARRIVAL';
  const arrivalQuest=graph==='patient_a_critical'?'Quest_Arrive_PatientA':'Quest_Arrive_Triage';
  for(const [actor,id] of Object.entries(actors)){
   while(true){
    const snapshot=await platform.observe(id);
    assert.ok(Array.isArray(snapshot.localQuests),'QUEST_OBSERVATION_MISSING');
    assert.ok(Array.isArray(snapshot.scenario.recoveryNotes),'RECOVERY_OBSERVATION_MISSING');
    assert.equal(snapshot.scenario.recoveryNotes.length,0,'SCENARIO_RECOVERED_WITHOUT_COMPLETION');
    if(snapshot.scenario.graphId===graph&&snapshot.scenario.nodeId!==initialNode
      &&snapshot.localQuests.some((q:any)=>!q.placeholder&&q.scenarioId===graph&&q.definitionId&&q.definitionId!==arrivalQuest)){
     await platform.artifact(runId,`arrival-completed-${actor}.json`,snapshot);break;
    }
    if(performance.now()>resultDeadline){
     await platform.artifact(runId,`arrival-stalled-${actor}.json`,snapshot);
     throw new Error(`ARRIVAL_QUEST_DID_NOT_ADVANCE:${actor}`);
    }
    await delay(500);
   }
  }

 }
 if(['bed','full'].includes(stage)){
  phase='patient_a_bed_dialogue';
  const runner=new Runner(platform),signal=AbortSignal.timeout(120000);
  const advance=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>{
   while(true){
    const state=await platform.observe(id);
    if(state.inputContext==='Gameplay')return;
    if(state.inputContext!=='DialoguePanelUIController'||state.dialogue.hasChoices)
     throw new Error(`UNHANDLED_PATIENT_A_BED_DIALOGUE:${actor}:${state.dialogue?.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`patient_a_bed_dialogue_${actor}`,type:'dialogueAdvance',actor},signal);
    await delay(200,undefined,{signal});
   }
  }));
  for(const result of advance)if(result.status==='rejected')throw result.reason;
  phase='patient_a_bed_boarding';
  const offsets:Record<string,number[]>={p1:[0,0,-2],p2:[1.5,0,0],p3:[0,0,1],p4:[-1.5,0,0]};
  for(const [actor,id] of Object.entries(actors)){
   phase=`patient_a_bed_approach:${actor}`;
   try{
    await runner.navigate(id,{id:`patient_a_bed_approach_${actor}`,type:'navigate',actor,target:'bed_a',mode:'input_adapter',timeoutMs:25000,args:{targetType:'vehicle',arrivalRadius:.8,targetOffset:offsets[actor]}},signal);
   }catch(error){
    const observed=await platform.observe(id,false,{includeStaticItems:false});
    const player=(observed.client?.players??[]).find((player:any)=>player.local);
    const bed=(observed.vehicles??[]).find((vehicle:any)=>vehicle.id==='bed_a');
    const distance=player&&bed?Math.hypot(player.position[0]-bed.position[0],player.position[2]-bed.position[2]):Infinity;
    if(distance>2.5)throw error;
    await platform.artifact(runId!,`patient-a-bed-approach-recovered-${actor}.json`,{distance,observed});
   }
  }
  const boarded=[] as Array<{actor:string;state:string;error?:string}>;
  for(const [actor,id] of Object.entries(actors)){
   try{
   await runner.interact(id,{id:`patient_a_bed_control_${actor}`,type:'interact',target:'move_bed',args:{entityId:'bed_a'},mode:'input_adapter'},signal);
   const deadline=performance.now()+5000;
   while(!(await platform.observe(id,false,{includeStaticItems:false})).vehicles?.some((v:any)=>v.id==='bed_a'&&v.locallyControlled)){
    if(performance.now()>deadline)throw new Error(`BED_CONTROL_NOT_ACQUIRED:${actor}`);
    await delay(100,undefined,{signal});
   }
   boarded.push({actor,state:'fulfilled'});
   }catch(error){boarded.push({actor,state:'rejected',error:String(error)});throw error;}
  }
  await platform.artifact(runId,'patient-a-bed-boarding.json',boarded);
  phase='patient_a_bed_driving';
  // The direct line cuts through the treatment-room wall.  These scene-space
  // corridor points keep the four-person bed in the open route to zone_a.
  // Patient A's authoritative move gate accepts a bed latched at any valid
  // positioning point.  Do not force the treatment-room marker: its doorway
  // is intentionally too narrow for the four-person moving bed.
  const patientABedSnapPoint='zone_3:bed_snap_point';
  const primaryBedRoute=[
   // Cross the main auto-door at its central opening (x=-72).  A nurse-area
   // desk blocks the straight continuation at z=-11.9, so sidestep east
   // before reaching it and continue around the treatment-room south wall.
   [-72.7,0,-2],[-72.7,0,-6],[-68.3,0,-9.68],[-68.3,0,-16],[-68.3,0,-21],[-64,0,-23],
   // Remain south of the treatment-room doorway while crossing east.  The
   // former north-jamb turn wedged the bed against the doorway panel.
   [-68.3,0,-15.5],[-65,0,-15.5],[-62,0,-15.5],
   [-62,0,-12],[-62,0,-9.7],[-60.759,0,-8.354]
  ];
  try {
   await drivePatientBed(platform,actors.p1,'bed_a',patientABedSnapPoint,signal,90000,Object.values(actors),primaryBedRoute,750);
  } catch (error) {
   const bedState=await platform.observe(actors.p1,false,{includeStaticItems:false});
   const bed=(bedState.vehicles??[]).find((vehicle:any)=>vehicle.id==='bed_a');
   // E005 may immediately relocate a successfully latched bed to the
   // treatment point and release its operators before the driver polls again.
   if ((error as any)?.code === 'VEHICLE_NOT_CONTROLLED' && bed?.latchedPointId) {
    await platform.artifact(runId!,'patient-a-bed-relocated-after-latch.json',{error:String(error),bedState});
   } else {
   if (!(error as any)?.code || (error as any).code !== 'NAVIGATION_STUCK') throw error;
   // The south-side furniture can stop a four-person bed mid-turn.  Back out
   // to the open north corridor, then enter zone A from its unobstructed side.
   await platform.artifact(runId!,'patient-a-bed-primary-route-stuck.json',{error:String(error),snapshot:await platform.observe(actors.p1)});
   await drivePatientBed(platform,actors.p1,'bed_a',patientABedSnapPoint,signal,90000,Object.values(actors),[
    [-72.7,0,-6],[-68.3,0,-12],[-68.3,0,-18],[-68.3,0,-21],[-64,0,-23]
   ],750);
   }
  }
  phase='patient_a_bed_confirmation';
  const bedDone=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>{
   const deadline=performance.now()+30000;
   while(true){
    const state=await platform.observe(id);
    assert.deepEqual(state.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(state.localQuests?.some((q:any)=>q.scenarioId===graph&&!q.placeholder&&q.definitionId!=='Quest_Move_Patient_A')){
     await platform.artifact(runId!,`patient-a-bed-completed-${actor}.json`,state);return;
    }
    if(performance.now()>deadline)throw new Error(`PATIENT_A_BED_QUEST_DID_NOT_ADVANCE:${actor}`);
    await delay(250);
   }
  }));
 for(const result of bedDone)if(result.status==='rejected')throw result.reason;
 }
 if(stage==='full'){
  // This signal covers the whole initial-assessment sequence.  Bed movement
  // plus replicated dialogue can legitimately consume most of 90 seconds on
  // four local clients, so a 90-second shared signal aborts successful work
  // mid-step.  Individual navigation and convergence checks retain their
  // tighter timeouts below.
  const runner=new Runner(platform),signal=AbortSignal.timeout(1200000);
  phase='patient_a_initial_assignments';
  const waitForInitialAssignments=async(actor:string,id:string,questId:string)=>{
   const deadline=performance.now()+60000;
   while(true){
    const state=await platform.observe(id);
    assert.deepEqual(state.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(state.localQuests?.some((quest:any)=>quest.definitionId===questId&&!quest.placeholder&&quest.scenarioId===graph))return state;
    if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`patient_a_initial_dialogue_${actor}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>deadline)throw new Error(`INITIAL_ASSIGNMENT_NOT_READY:${actor}:${questId}`);
    await delay(200,undefined,{signal});
   }
  };
  const assigned=await Promise.all([
   waitForInitialAssignments('p2',actors.p2,'Quest_Check_Vital_PatientA'),
   waitForInitialAssignments('p3',actors.p3,'Quest_Check_GCS_PatientA')
  ]);
  await platform.artifact(runId!,'patient-a-initial-assignments.json',assigned);
  // D006 is replicated to all four clients while the two assigned nurses
  // already receive their quests. Each client must acknowledge its own
  // presentation; otherwise a remaining dialogue panel suppresses that
  // client's patient interaction registry.
  const dialogueDeadline=performance.now()+20000;
  while(true){
   const states=await Promise.all(Object.entries(actors).map(async([actor,id])=>[actor,await platform.observe(id)] as const));
   const pending=states.filter(([,state])=>state.inputContext==='DialoguePanelUIController');
   if(pending.length===0)break;
   await Promise.all(pending
    .filter(([,state])=>state.dialogue.canAdvance||state.dialogue.isTextAnimating)
    .map(([actor])=>runner.step({actors} as any,{id:`patient_a_initial_dialogue_${actor}`,type:'dialogueAdvance',actor},signal)));
   if(performance.now()>dialogueDeadline)throw new Error('INITIAL_DOCTOR_DIALOGUE_DID_NOT_CLOSE');
   await delay(150,undefined,{signal});
  }
  phase='patient_a_vital_item';
  const acquireStaticItem=async(actor:string,id:string,itemId:string,pattern:RegExp)=>{
   const deadline=performance.now()+15000;
   while(performance.now()<deadline){
    const state=await platform.observe(id);
    if(state.client.players.find((player:any)=>player.local).inventory.slots.some((slot:any)=>slot.itemId===itemId))return state;
    const pickup=(state.interactions??[]).find((interaction:any)=>(interaction.interactionId==='static_pickup'||!interaction.entityId)&&pattern.test(interaction.text));
    const selected=(state.interactions??[]).find((interaction:any)=>interaction.selected);
    if(!pickup||!selected){await delay(150,undefined,{signal});continue;}
    if(pickup.index!==selected.index){
     await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:pickup.index>selected.index?'Equals':'Minus'}]},{signal});
     // Interaction hints are rebuilt asynchronously.  Observing immediately
     // after a selector tap can reuse the old selected index and oscillate
     // past a nearby pickup.
     await delay(100,undefined,{signal});
     continue;
    }
    await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:'F'}]},{signal});
    await delay(200,undefined,{signal});
   }
   throw new Error(`STATIC_ITEM_NOT_ACQUIRED:${actor}:${itemId}`);
  };
  // Use the zone-3 counter from its open north side, next to bed release.
  const vital='static-item:vital_set-(8):7de57bf8b4194ab8b4b822f4f01a2933';
  // The BC supply cart now occupies (-67,-13.5). Use the marked west
  // corridor instead of a diagonal from room A through that furniture.
  const vitalStart=(await platform.observe(actors.p2,false,{includeStaticItems:false})).client.players.find((player:any)=>player.local).position;
  const vitalExit=vitalStart[2]<-19?[]:
   [[-65.12,0,-9.35],[-68.4,0,-9.35],[-68.5,0,-20.75],[-68.2,0,-22.6]];
  for(const [index,targetPosition] of [...vitalExit,
   [-66.04,0,-22.6]].entries())
   await runner.navigate(actors.p2,{id:`patient_a_vital_corridor_${index}`,type:'navigate',actor:'p2',
    target:`patient_a_vital_corridor_${index}`,mode:'input_adapter',timeoutMs:30000,
    args:{targetType:'position',targetPosition,arrivalRadius:.3}},signal);
  try {
   await runner.navigate(actors.p2,{id:'patient_a_pick_vital_set',type:'navigate',actor:'p2',target:vital,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.25,targetOffset:[0,0,1.25]}},signal);
  } catch (error) {
   const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
   if (!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
       || !state.interactions?.some((interaction:any)=>(interaction.interactionId==='static_pickup'||!interaction.entityId)&&/활력|vital/i.test(interaction.text))) throw error;
   await platform.artifact(runId!,'patient-a-vital-approach-recovered.json',{error:String(error),state});
  }
  const beforePickup=await platform.observe(actors.p2);
  await platform.artifact(runId!,'patient-a-before-vital-pickup.json',beforePickup);
  const afterPickup=await acquireStaticItem('p2',actors.p2,'vital_set',/활력|vital/i);
  await platform.artifact(runId!,'patient-a-after-vital-pickup.json',afterPickup);
  if(!afterPickup.client.players.find((player:any)=>player.local).inventory.slots.some((slot:any)=>slot.itemId==='vital_set'))throw new Error('VITAL_SET_NOT_ACQUIRED');
  phase='patient_a_initial_assessments';
  // Registry definitions arrive with the replicated patient, whereas the
  // player-local interaction scan is refreshed on the following frames.  Do
  // not turn that short convergence window into a false route failure.
  const interactWhenScanned=async(id:string,step:any)=>{
   let lastError:unknown;
   const deadline=performance.now()+15000;
   while(performance.now()<deadline){
    const state=await platform.observe(id,false,{includeStaticItems:false});
    if(state.inputContext==='DialoguePanelUIController'){
     if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_INTERACTION_CHOICE:${step.id}:${state.dialogue.nodeId}`);
     if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
      await runner.step({actors} as any,{id:`${step.id}_instruction`,type:'dialogueAdvance',actor:step.actor},signal);
     await delay(150,undefined,{signal});continue;
    }
    try{return await runner.interact(id,step,signal);}
    catch(error){
     lastError=error;
     // Other players can change a nearest-only interaction list between the
     // selection and confirm frames.  That optimistic-concurrency conflict is
     // transient just like a scan not yet refreshed; retry from observation.
     if(!/TARGET_NOT_INTERACTABLE|STATE_CONFLICT/.test(String(error)))throw error;
     await delay(150,undefined,{signal});
    }
   }
   await platform.artifact(runId!,`interact-not-scanned-${step.id}.json`,{
    step, lastError:String(lastError), observation:await platform.observe(id)
   });
   throw lastError;
  };
  const assessments=await Promise.allSettled([
   (async()=>{
   // E005 relocates the latched bed (and patient) into the treatment room,
    // while released operators remain at the temporary south-side snap point.
    // Return through the actual doorway rather than steering through its wall.
    const p2Start=await platform.observe(actors.p2,false,{includeStaticItems:false});
    const p2Position=p2Start.client.players.find((candidate:any)=>candidate.local)?.position;
    // Depending on the snap/release replication order, P2 can already be at
    // treatment or remain in zone_3.  Only route the latter through the door.
    if(p2Position&&p2Position[2]<-15)for(const [index,point] of [[-68.2,0,-22.6],[-68.5,0,-20.75],[-68.5,0,-9.35],[-65.12,0,-9.35],[-62,0,-8.35]].entries())
     await runner.navigate(actors.p2,{id:`patient_a_p2_return_treatment_${index}`,type:'navigate',actor:'p2',target:`patient_a_p2_return_treatment_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:point,arrivalRadius:.7}},signal);
    try{
     // Approach from the open side of the bed.  Its rear collider leaves a
     // 0.8m gap, outside the assessment interaction radius.
     await runner.navigate(actors.p2,{id:'patient_a_p2_to_patient',type:'navigate',actor:'p2',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.2,targetOffset:[-.5,0,.5]}},signal);
    }catch(error){
     const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
     const player=(state.client?.players??[]).find((candidate:any)=>candidate.local),patient=(state.scenarioEntities??[]).find((candidate:any)=>candidate.id==='patient_a');
     // The patient-bed collider intentionally keeps players roughly 0.8m from
     // the patient pivot. This is still inside the assessment interaction range.
     if(!player||!patient||(!state.interactions?.some((interaction:any)=>interaction.interactionId==='assess_vital'&&interaction.entityId==='patient_a')
       &&Math.hypot(player.position[0]-patient.position[0],player.position[2]-patient.position[2])>1))throw error;
    }
    await interactWhenScanned(actors.p2,{id:'patient_a_p2_assess_vital',type:'interact',actor:'p2',target:'assess_vital',args:{entityId:'patient_a'},mode:'input_adapter'});
    // The monitor presentation uses the stable patient_monitor tag, but each
    // placed monitor gets a runtime entity suffix.  Use the scene position of
    // the treatment-room monitor and select the sole nearby runtime instance.
    try {
     await runner.navigate(actors.p2,{id:'patient_a_p2_to_monitor',type:'navigate',actor:'p2',target:'patient_a_monitor_approach',mode:'input_adapter',timeoutMs:30000,args:{targetType:'position',targetPosition:[-59.5,0,-8.35],arrivalRadius:.8}},signal);
    } catch(error) {
     // The monitor's collider stops a player before its visual center.  The
     // authoritative arrival condition is that its nearest-only interaction
     // is already collected, not that the player reaches the center point.
     const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
     if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
       || !state.interactions?.some((interaction:any)=>interaction.interactionId==='select_patient_mode'))throw error;
     await platform.artifact(runId!,'patient-a-monitor-approach-recovered.json',{error:String(error),state});
    }
    await interactWhenScanned(actors.p2,{id:'patient_a_p2_select_monitor',type:'interact',actor:'p2',target:'select_patient_mode',args:{},mode:'input_adapter'});
    try {
     await runner.navigate(actors.p2,{id:'patient_a_p2_select_monitor_patient',type:'navigate',actor:'p2',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.7,targetOffset:[-.5,0,.5]}},signal);
    } catch (error) {
     const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
     if (!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))) throw error;
     await platform.artifact(runId!,'patient-a-monitor-select-patient-approach-recovered.json',{error:String(error),state});
    }
    await interactWhenScanned(actors.p2,{id:'patient_a_p2_select_patient_on_monitor',type:'interact',actor:'p2',target:'monitor_select',args:{entityId:'patient_a'},mode:'input_adapter'});
    try {
     await runner.navigate(actors.p2,{id:'patient_a_p2_return_monitor',type:'navigate',actor:'p2',target:'patient_a_monitor_return',mode:'input_adapter',timeoutMs:30000,args:{targetType:'position',targetPosition:[-59.5,0,-8.35],arrivalRadius:.8}},signal);
    } catch (error) {
     const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
     if (!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
         || !state.interactions?.some((interaction:any)=>interaction.interactionId==='detail_overlay')) throw error;
     await platform.artifact(runId!,'patient-a-monitor-return-recovered.json',{error:String(error),state});
    }
    await interactWhenScanned(actors.p2,{id:'patient_a_p2_view_monitor',type:'interact',actor:'p2',target:'detail_overlay',args:{},mode:'input_adapter'});
   })(),
   (async()=>{
    // P3 can be released at the southern zone-3 snap rather than the
    // treatment-room anchor.  Its direct northbound line is inside the room
    // partition; leave west first and use the corridor doorway.
    for(const [index,point] of [[-68.2,0,-22.6],[-68.5,0,-20.75],[-68.5,0,-9.35],[-65.12,0,-9.35],[-62,0,-8.35]].entries())
     await runner.navigate(actors.p3,{id:`patient_a_p3_return_treatment_${index}`,type:'navigate',actor:'p3',target:`patient_a_p3_return_treatment_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:point,arrivalRadius:.7}},signal);
    try{
     await runner.navigate(actors.p3,{id:'patient_a_p3_to_patient',type:'navigate',actor:'p3',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.25,targetOffset:[-.5,0,.8]}},signal);
    }catch(error){
     const state=await platform.observe(actors.p3,false,{includeStaticItems:false});
     const player=(state.client?.players??[]).find((candidate:any)=>candidate.local),patient=(state.scenarioEntities??[]).find((candidate:any)=>candidate.id==='patient_a');
     // A bed collider may stop the player short of its patient pivot, but only
     // accept that boundary when the actual P3 assessment is already exposed.
     if(!player||!patient||!state.interactions?.some((interaction:any)=>interaction.interactionId==='assess_avpu_gcs'&&interaction.entityId==='patient_a'))throw error;
    }
    await interactWhenScanned(actors.p3,{id:'patient_a_p3_assess_gcs',type:'interact',actor:'p3',target:'assess_avpu_gcs',args:{entityId:'patient_a'},mode:'input_adapter'});
   })()
  ]);
  await platform.artifact(runId!,'patient-a-initial-assessment-results.json',assessments.map(result=>({status:result.status,error:result.status==='rejected'?String(result.reason):undefined})));
  for(const result of assessments)if(result.status==='rejected')throw result.reason;
  // Completing the interaction is not the end of either assignment: P2 must
  // close the vital overlay, P3 must answer the AVPU prompt, and P4's first
  // parallel assignment is the cervical collar.  Leaving any one of these
  // pending makes the global initial-assessment gate wait forever.
  phase='patient_a_initial_followups';
  await platform.command(actors.p2,'input.execute',{sequence:[{operation:'tap',key:'Escape'}]},{signal});
  const overlayDeadline=performance.now()+10000;
  while((await platform.observe(actors.p2)).inputContext==='PatientMonitorDetailOverlay'){
   if(performance.now()>overlayDeadline)throw new Error('VITAL_OVERLAY_DID_NOT_CLOSE');
   await delay(150,undefined,{signal});
  }
  const answerChoiceSequence=async(actor:string,id:string,answers:Record<string,number>,label:string,isDone?:(state:any)=>boolean)=>{
   const completed=new Set<string>(),deadline=performance.now()+30000;
   // Selecting the final answer only queues its server-side assessment
   // resolution.  Keep advancing/observing until the owning quest changes;
   // otherwise a fast local UI can make this loop exit before the final
   // answer has been committed to the multiplayer scenario state.
   while(true){
    const state=await platform.observe(id),nodeId=state.dialogue.nodeId,answer=answers[nodeId];
    if(isDone?.(state))return;
    if(state.inputContext==='DialoguePanelUIController'&&answer!==undefined&&state.dialogue.hasChoices
      &&state.dialogue.choices?.some((choice:any)=>choice.choiceId===`${nodeId}#${answer}`)){
     await runner.step({actors} as any,{id:`${label}_${nodeId}`,type:'dialogueChoose',actor,choiceId:`${nodeId}#${answer}`},signal);
     completed.add(nodeId);continue;
    }
    if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`${label}_advance_${nodeId}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>deadline)throw new Error(`CHOICE_SEQUENCE_NOT_READY:${label}:${[...completed].join(',')}`);
    await delay(150,undefined,{signal});
   }
  };
  await Promise.all([answerChoiceSequence('p2',actors.p2,{
   C_VITAL_RR_HR_A:2,C_VITAL_BP_A:1,C_VITAL_BT_SPO2_A:2
  },'patient_a_p2_vital',state=>state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Wait_Others_Initial_PatientA'&&!quest.placeholder)),
  // Assessment interaction completion precedes the mandatory AVPU/GCS quiz.
  // Handle every choice until the role's wait quest confirms branch completion.
  answerChoiceSequence('p3',actors.p3,{C004:2,C005:2,C006:3,C007:2,C008:2},'patient_a_p3_gcs',state=>
   state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Wait_Others_Initial_PatientA'&&!quest.placeholder))]);
  const collar='static-item:cervical_collar:89259b6810ea4e018b0e90daee6d93db';
  // P4 is also released at zone_3 when the manual bed snap is observed.  Exit
  // that room through the west corridor before it begins its assigned pickup;
  // the straight treatment-room vector crosses the partition collider.
  for(const [index,point] of [[-68.2,0,-22.6],[-68.5,0,-20.75],[-68.5,0,-9.35],[-65.12,0,-9.35],[-62,0,-8.35]].entries())
   await runner.navigate(actors.p4,{id:`patient_a_p4_return_treatment_${index}`,type:'navigate',actor:'p4',target:`patient_a_p4_return_treatment_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:point,arrivalRadius:.7}},signal);
  try {
   await runner.navigate(actors.p4,{id:'patient_a_pick_cervical_collar',type:'navigate',actor:'p4',target:collar,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.8}},signal);
  } catch(error) {
   const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
   if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
     || !state.interactions?.some((interaction:any)=>(interaction.interactionId==='static_pickup'||!interaction.entityId)&&/경추|cervical/i.test(interaction.text)))throw error;
   await platform.artifact(runId!,'patient-a-collar-approach-recovered.json',{error:String(error),state});
  }
  await acquireStaticItem('p4',actors.p4,'cervical_collar',/경추|cervical/i);
  await runner.navigate(actors.p4,{id:'patient_a_p4_to_patient',type:'navigate',actor:'p4',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.7,targetOffset:[-1,0,1]}},signal);
  await interactWhenScanned(actors.p4,{id:'patient_a_p4_apply_cervical_collar',type:'interact',actor:'p4',target:'patient_a_use_cervical_collar',args:{entityId:'patient_a'},mode:'input_adapter'});
  // A dialogue transition can briefly report gameplay before the next line is
  // presented. Require a sustained gameplay context so navigation never starts
  // while the stabilizer acknowledgement is still opening.
  const p4DialogueDeadline=performance.now()+20000;
  let stableGameplaySince=0;
  while(true){
   const state=await platform.observe(actors.p4);
   if(state.inputContext==='DialoguePanelUIController'){
    stableGameplaySince=0;
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await platform.command(actors.p4,'input.execute',{expectedPresentationRevision:state.dialogue.presentationRevision,sequence:[{operation:'tap',key:'KeypadEnter'}]},{signal});
   }else{
    stableGameplaySince ||=performance.now();
    if(performance.now()-stableGameplaySince>=600)break;
   }
   if(performance.now()>p4DialogueDeadline)throw new Error('STABILIZER_DIALOGUE_DID_NOT_CLOSE');
   await delay(150,undefined,{signal});
  }
  phase='patient_a_suction';
  const suctionItems=[
   ['yankauer','scene-item:overworld:yankauer:b8640c3eb4ed',/양커|yankauer/i],
   ['wall_suction','scene-item:overworld:wall-suction:bc8a067e9822',/흡인기|wall.?suction/i],
   ['suction_line','static-item:SuctionLine:16cdf4a913194e51a0768d8da4b90d8e',/석션.?라인|suction.?line/i]
  ] as const;
  for(const [itemId,item,pattern] of suctionItems){
   if(itemId==='wall_suction')for(const [index,targetPosition] of [[-62.2,0,-7.3],[-62.2,0,-10.5],[-59.4,0,-10.5]].entries())
    await runner.navigate(actors.p4,{id:`patient_a_p4_suction_corridor_${index}`,type:'navigate',actor:'p4',target:`patient_a_p4_suction_corridor_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:.6}},signal);
   const targetOffset=itemId==='yankauer'?[-1,0,0]:[-.4,0,.5];
   try {
    await runner.navigate(actors.p4,{id:`patient_a_p4_pick_${itemId}`,type:'navigate',actor:'p4',target:item,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.5,targetOffset}},signal);
   } catch(error) {
    const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
    if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
      || !state.interactions?.some((interaction:any)=>(interaction.interactionId==='static_pickup'||!interaction.entityId)&&pattern.test(interaction.text)))throw error;
    await platform.artifact(runId!,`patient-a-${itemId}-approach-recovered.json`,{error:String(error),state});
   }
   await acquireStaticItem('p4',actors.p4,itemId,pattern);
  }
  // The wall accepts the assembled Yankauer suction tip, not the two loose
  // pickup items.  Craft it through the same inventory UI a player uses:
  // choose the recipe, click it again to craft, then put the cursor-held
  // result into an empty inventory slot so its normal acquisition hook runs.
  const clickUiElement=async(automationId:string)=>{
   const query:any=await platform.command(actors.p4,'ui.query',{automationId},{signal});
   const target=query.elements?.find((element:any)=>element.interactable);
   if(!target?.screenCenter)throw new Error(`UI_TARGET_NOT_FOUND:${automationId}`);
   const pointer={...target.screenCenter,screenWidth:target.screenWidth,screenHeight:target.screenHeight,frame:target.uiRevision};
   await platform.command(actors.p4,'ui.pointer',{...pointer,pressed:true},{signal});
   await platform.command(actors.p4,'ui.pointer',{...pointer,pressed:false},{signal});
  };
  await platform.command(actors.p4,'input.execute',{sequence:[{operation:'tap',key:'E'}]},{signal});
  const craftDeadline=performance.now()+10000;
  while((await platform.observe(actors.p4)).inputContext!=='InventoryUIController'){
   if(performance.now()>craftDeadline)throw new Error('INVENTORY_DID_NOT_OPEN_FOR_YANKAUER_CRAFT');
   await delay(100,undefined,{signal});
  }
  await clickUiElement('Recipe_yankauer_suction_ready');
  await clickUiElement('Recipe_yankauer_suction_ready');
  await clickUiElement('Slot_0');
  await platform.command(actors.p4,'input.execute',{sequence:[{operation:'tap',key:'E'}]},{signal});
  const readyDeadline=performance.now()+10000;
  while(true){
   const state=await platform.observe(actors.p4);
   if(state.client.players.find((player:any)=>player.local).inventory.slots.some((slot:any)=>slot.itemId==='yankauer_suction_ready'))break;
   if(performance.now()>readyDeadline)throw new Error('YANKAUER_SUCTION_READY_NOT_CRAFTED');
   await delay(100,undefined,{signal});
  }
  await runner.navigate(actors.p4,{id:'patient_a_p4_return_wall_suction',type:'navigate',actor:'p4',target:'patient_a_wall_suction_approach',mode:'input_adapter',timeoutMs:30000,args:{targetType:'position',targetPosition:[-59.3,0,-10.9],arrivalRadius:.45}},signal)
   .catch(async error=>{
    const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
    if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
      || !state.interactions?.some((entry:any)=>entry.entityId==='zone_a:wall_suction'&&entry.interactionId==='wall_suction_install'))throw error;
    await platform.artifact(runId!,'patient-a-wall-suction-return-recovered.json',{error:String(error),state});
   });
  await interactWhenScanned(actors.p4,{id:'patient_a_p4_install_wall_suction',type:'interact',actor:'p4',target:'wall_suction_install',args:{entityId:'zone_a:wall_suction'},mode:'input_adapter'});
  const suctionInstallDialogueDeadline=performance.now()+10000;
  while((await platform.observe(actors.p4)).inputContext==='DialoguePanelUIController'){
   const state=await platform.observe(actors.p4);
   if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
    await platform.command(actors.p4,'input.execute',{expectedPresentationRevision:state.dialogue.presentationRevision,sequence:[{operation:'tap',key:'KeypadEnter'}]},{signal});
   if(performance.now()>suctionInstallDialogueDeadline)throw new Error('SUCTION_INSTALL_DIALOGUE_DID_NOT_CLOSE');
   await delay(150,undefined,{signal});
  }
  // F input is acknowledged asynchronously by the authoritative interaction
  // bridge.  Confirm its quest signal before moving away; otherwise the next
  // patient action is attempted while the suction line is still disconnected.
  const yankauerConnectDeadline=performance.now()+8000;
  while(true){
   await interactWhenScanned(actors.p4,{id:'patient_a_p4_connect_yankauer',type:'interact',actor:'p4',target:'connect_yankauer',args:{entityId:'zone_a:wall_suction'},mode:'input_adapter'});
   await delay(500,undefined,{signal});
   const state=await platform.observe(actors.p4);
   const connected=state.localQuests?.flatMap((quest:any)=>quest.tasks??[]).some((task:any)=>task.Identifier==='connect-yankauer-patient-a'&&task.Completed);
   if(connected)break;
   if(performance.now()>yankauerConnectDeadline)throw new Error('YANKAUER_CONNECTION_NOT_CONFIRMED');
  }
  const yankauerConnectDialogueDeadline=performance.now()+10000;
  while((await platform.observe(actors.p4)).inputContext==='DialoguePanelUIController'){
   const state=await platform.observe(actors.p4);
   if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
    await platform.command(actors.p4,'input.execute',{expectedPresentationRevision:state.dialogue.presentationRevision,sequence:[{operation:'tap',key:'KeypadEnter'}]},{signal});
   if(performance.now()>yankauerConnectDialogueDeadline)throw new Error('YANKAUER_CONNECT_DIALOGUE_DID_NOT_CLOSE');
   await delay(150,undefined,{signal});
  }
  await runner.navigate(actors.p4,{id:'patient_a_p4_return_patient_for_suction',type:'navigate',actor:'p4',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.7,targetOffset:[-1,0,1]}},signal);
  const suctionPromptDeadline=performance.now()+10000;
  while(true){
   const state=await platform.observe(actors.p4);
   if(state.inputContext!=='DialoguePanelUIController'){
    // N007_4 can be queued a frame after the connection dialogue clears.
    await delay(300,undefined,{signal});
    if((await platform.observe(actors.p4)).inputContext!=='DialoguePanelUIController')break;
    continue;
   }
   if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
    await platform.command(actors.p4,'input.execute',{expectedPresentationRevision:state.dialogue.presentationRevision,sequence:[{operation:'tap',key:'KeypadEnter'}]},{signal});
   if(performance.now()>suctionPromptDeadline)throw new Error('SUCTION_PROMPT_DIALOGUE_DID_NOT_CLOSE');
   await delay(150,undefined,{signal});
  }
  await interactWhenScanned(actors.p4,{id:'patient_a_p4_suction_patient',type:'interact',actor:'p4',target:'wall_suction_use',args:{entityId:'patient_a'},mode:'input_adapter'});
  // The initial gate is transient: once all owners report, the scenario can
  // replace the wait quest before this observer polls it.  P4 reaching its
  // next IV assignment is authoritative proof that its suction task and the
  // cross-role initial gate both completed.
  // The four owner acknowledgements are replicated independently.  Under a
  // fresh four-client launch, the last acknowledgement can arrive well after
  // P4's local suction UI closes, so retain this observation rather than
  // treating a normal replication delay as a failed gameplay route.
  const initialCompletionDeadline=performance.now()+90000;
  while(true){
   const state=await platform.observe(actors.p4);
   if(state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_IV_Line_PatientA'&&!quest.placeholder))break;
   if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
    await platform.command(actors.p4,'input.execute',{expectedPresentationRevision:state.dialogue.presentationRevision,sequence:[{operation:'tap',key:'KeypadEnter'}]},{signal});
   if(performance.now()>initialCompletionDeadline)throw new Error('INITIAL_ASSIGNMENT_GATE_DID_NOT_ADVANCE');
   await delay(200,undefined,{signal});
  }
  await platform.artifact(runId!,'patient-a-initial-assessments-completed.json',await Promise.all(Object.values(actors).map(id=>platform.observe(id))));
 }
 if(stage==='full'){
  // The next four assignments are intentionally driven by their actual owner
  // inventories.  This is not a synthetic signal path: recipe clicks, pickup
  // consumption and the normal interaction selector remain in the loop.
  phase='patient_a_advanced_assignments';
  // Static equipment can be distributed over three carts separated by room
  // colliders.  A complete live multiplayer route must allow its normal
  // input-navigation recovery to finish rather than aborting mid-quest.
  const runner=new Runner(platform),signal=AbortSignal.timeout(600000);
  const execFileAsync=promisify(execFile);
  const accessibleChecker=fileURLToPath(new URL('../documentation/check_accessible.py',import.meta.url));
  const navigateAccessible=async(actor:string,endX:number,endZ:number,idPrefix:string,arrivalRadius=.25)=>{
   const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
   const start=state.client.players.find((entry:any)=>entry.local)?.position;
   if(!start)return false;
   let result:any;
   try{
    const {stdout}=await execFileAsync('python3',[accessibleChecker,String(start[0]),String(start[2]),String(endX),String(endZ)]);
    result=JSON.parse(stdout);
   }catch(error:any){
    const parsed=String(error?.stdout??'').trim();
    if(parsed)result=JSON.parse(parsed);else throw error;
   }
   await platform.artifact(runId!,`${idPrefix}-accessible-path.json`,result);
   if(result.status!=='ok')return false;
   // Preserve every returned point and use a tight radius so movement does
   // not cut across a gap between marked accessible rectangles.
   for(const [index,[x,z]] of (result.path as [number,number][]).slice(1).entries())
    await runner.navigate(actors[actor],{id:`${idPrefix}_${index}`,type:'navigate',actor,target:`${idPrefix}_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:[x,0,z],arrivalRadius}},signal);
   return true;
  };
  const releaseAccidentalBedControl=async(actor:string)=>{
   let state=await platform.observe(actors[actor],false,{includeStaticItems:false});
   if(!state.vehicles?.some((vehicle:any)=>vehicle.id==='bed_a'&&vehicle.locallyControlled))return;
   const dismountKey=state.inputBindings?.dismount??'LeftShift';
   await platform.command(actors[actor],'input.execute',{sequence:[{operation:'hold',key:dismountKey,durationMs:100}]},{signal});
   await delay(120,undefined,{signal});
   await platform.command(actors[actor],'input.execute',{sequence:[{operation:'tap',key:dismountKey,durationMs:100}]},{signal});
   const deadline=performance.now()+5000;
   while(true){
    state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    if(!state.vehicles?.some((vehicle:any)=>vehicle.id==='bed_a'&&vehicle.locallyControlled))break;
    if(performance.now()>deadline)throw new Error(`BED_CONTROL_NOT_RELEASED:${actor}`);
    await delay(100,undefined,{signal});
   }
   await platform.artifact(runId!,`patient-a-${actor}-released-accidental-bed-control.json`,state);
  };
  const clickUiElement=async(actor:string,automationId:string)=>{
   const query:any=await platform.command(actors[actor],'ui.query',{automationId},{signal});
   // Recipe tiles are custom VisualElements rather than Buttons.  A tooltip
   // can temporarily win the UI hit-test, making `interactable` false even
   // though the tile has a valid visible screen centre and receives pointer
   // events normally. Prefer the strict result, then use that stable centre.
   const target=query.elements?.find((element:any)=>element.interactable)
    ??query.elements?.find((element:any)=>element.visible&&element.enabled&&element.screenCenter);
   if(!target?.screenCenter){
    await platform.artifact(runId!,`advanced-ui-missing-${actor}-${automationId}.json`,query);
    throw new Error(`UI_TARGET_NOT_FOUND:${actor}:${automationId}`);
   }
   const pointer={...target.screenCenter,screenWidth:target.screenWidth,screenHeight:target.screenHeight,frame:target.uiRevision};
   await platform.command(actors[actor],'ui.pointer',{...pointer,pressed:true},{signal});
   await platform.command(actors[actor],'ui.pointer',{...pointer,pressed:false},{signal});
  };
  const craft=async(actor:string,itemId:string)=>{
   // D010 is emitted only after the four initial assignments converge.  Its
   // replication timing is independent per client, so it can open after the
   // preceding P4-only suction acknowledgement has already closed.
   const dialogueDeadline=performance.now()+20000;
   while(true){
    const state=await platform.observe(actors[actor]);
    if(state.inputContext!=='DialoguePanelUIController')break;
    if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_ADVANCED_DIALOGUE_CHOICE:${actor}:${state.dialogue.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`advanced_dialogue_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>dialogueDeadline)throw new Error(`ADVANCED_DIALOGUE_DID_NOT_CLOSE:${actor}:${state.dialogue.nodeId}`);
    await delay(150,undefined,{signal});
   }
   const before=await platform.observe(actors[actor]);
   if(before.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
   await platform.command(actors[actor],'input.execute',{sequence:[{operation:'tap',key:'E'}]},{signal});
   const deadline=performance.now()+10000;
   while((await platform.observe(actors[actor])).inputContext!=='InventoryUIController'){
    if(performance.now()>deadline)throw new Error(`INVENTORY_DID_NOT_OPEN:${actor}:${itemId}`);
    await delay(100,undefined,{signal});
   }
   await clickUiElement(actor,`Recipe_${itemId}`);await clickUiElement(actor,`Recipe_${itemId}`);await clickUiElement(actor,'Slot_0');
   await platform.command(actors[actor],'input.execute',{sequence:[{operation:'tap',key:'E'}]},{signal});
   while(true){
    const state=await platform.observe(actors[actor]);
    if(state.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
    if(performance.now()>deadline)throw new Error(`CRAFT_NOT_CONFIRMED:${actor}:${itemId}`);
    await delay(100,undefined,{signal});
   }
  };
  const acquireWorldItem=async(actor:string,itemId:string,positionOverride?:[number,number],offsetOverride?:[number,number,number])=>{
   const itemLabels:Record<string,RegExp>={
    laryngoscope_blade:/후두경.*블레이드|laryngoscope.*blade/i,
    laryngoscope_handle:/후두경.*(?:핸들|손잡이)|laryngoscope.*handle/i,
    endotracheal_tube:/기관(?:내관|.*튜브)|endotracheal.*tube/i,
    stylet:/스타일렛|stylet/i,
    syringe_5cc:/5cc.*주사기|주사기.*5cc|syringe.*5cc/i,
    plaster:/플라스터|plaster/i,
    flowmeter:/유량계|flowmeter/i,
    humidifier_bottle:/습윤병|humidifier/i,
    sterile_distilled_water:/멸균증류수|sterile.*distilled/i,
    tpiece_set:/T-?piece|T피스/i,
    o2_line:/산소.*연결줄|산소줄|o2.*line/i,
    gauze:/거즈|gauze/i,
    normal_saline_1000ml:/생리식염수.*1L|normal.*saline/i,
    plasma_solution_1000ml:/플라즈마.*솔루션|plasma.*solution/i,
    blood_bag:/혈액백|blood.*bag/i,
    ambubag:/앰부백|ambu.*bag/i,
    reservoir_bag:/보유주머니|reservoir.*bag/i,
    epinephrine_ampule:/에피네프린|epinephrine/i,
    normal_saline_20ml:/생리식염수.*20ml|normal.*saline.*20/i,
    syringe_20cc:/20cc.*주사기|주사기.*20cc|syringe.*20cc/i,
    defibrillatorpad:/전극.*패드|defibrillator.*pad/i,
    intravenous_set:/수액세트|intravenous.*set/i,
    sterile_gloves:/멸균.*장갑|sterile.*gloves/i,
    cannula_18g:/18.*게이지|18g|cannula/i,
    central_line_set:/중심정맥관|C-?line|central.*line/i
   };
   const label=itemLabels[itemId]??new RegExp(itemId.replace(/_/g,'.*'),'i');
   const existing=await platform.observe(actors[actor]);
   if(existing.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
   const catalogue=await platform.observe(actors.p1);
   const preferredPosition:Record<string,[number,number]>={
    // These three copies share the zone-0 supply rack.  Its marked-corridor
    // approach (-64.55, -10.89) remains inside Accessible 144578319 and was
    // selected over the zone-A shelf copies, whose interaction side lies
    // beyond that room's marked movement rectangle.
    flowmeter:[-64.55,-11.9169331],
    humidifier_bottle:[-64.391,-11.93392],
    sterile_distilled_water:[-64.67545,-11.909],
    tpiece_set:[-58.983,-10.8128805],
    o2_line:[-59.0416,-11.0374517],
    gauze:[-59.93185,-4.347145],
    plaster:[-59.816,-4.39],
    normal_saline_1000ml:[-59.1626472,-11.9149933],
    plasma_solution_1000ml:[-69.6654053,-15.1439991],
    blood_bag:[-63.136,-4.636],
    ambubag:[-61.3281441,-4.40166473],
    reservoir_bag:[-61.3518944,-4.570915],
    epinephrine_ampule:[-62.26141,-4.50128174],
    normal_saline_20ml:[-62.0683479,-4.412796],
    syringe_20cc:[-62.2582,-4.595],
    defibrillatorpad:[-59.14906,-4.498703],
    intravenous_set:[-67.09364,-18.3169956],
    sterile_gloves:[-61.0448952,-4.346916],
    cannula_18g:[-67.0394745,-17.7659626],
    central_line_set:[-60.7353935,-4.479912]
   };
   const preferred=positionOverride??preferredPosition[itemId];
   const rankedCandidates=(catalogue.staticPlacedItems??[])
    .filter((item:any)=>item.rewards?.some((reward:any)=>reward.itemId===itemId))
    // Freeze the verified treatment-room placement instead of selecting a
    // different copy after scene-data merges change enumeration order.
    .sort((left:any,right:any)=>preferred
     ?Math.hypot((left.position?.[0]??0)-preferred[0],(left.position?.[2]??0)-preferred[1])
      -Math.hypot((right.position?.[0]??0)-preferred[0],(right.position?.[2]??0)-preferred[1])
     :(right.position?.[0]??-Infinity)-(left.position?.[0]??-Infinity));
   const candidates=preferred?rankedCandidates.slice(0,1):rankedCandidates;
   if(!candidates.length)throw new Error(`STATIC_ITEM_NOT_FOUND:${itemId}`);
   let last:unknown;
   const pickupTrace:any[]=[];
   for(const candidate of candidates){
    try{
     const offset=offsetOverride??(['flowmeter','humidifier_bottle','sterile_distilled_water'].includes(itemId)?[0,0,1.03]:['tpiece_set','o2_line','normal_saline_1000ml'].includes(itemId)?[-.2,0,.82]:['cannula_18g','intravenous_set'].includes(itemId)?[0,0,1.2]:[0,0,-1.5]);
     if(itemId==='sterile_gloves'){
      await navigateAccessible(actor,-64.5,-7.12,`advanced_accessible_${actor}_${itemId}_doorway`);
      await navigateAccessible(actor,-64.5,-5.84,`advanced_accessible_${actor}_${itemId}_counter`);
     }
     if(preferred&&Math.hypot((candidate.position?.[0]??0)-preferred[0],(candidate.position?.[2]??0)-preferred[1])<.05)
      await navigateAccessible(actor,candidate.position[0]+offset[0],candidate.position[2]+offset[2],`advanced_accessible_${actor}_${itemId}`,['flowmeter','humidifier_bottle','sterile_distilled_water','tpiece_set','o2_line','normal_saline_1000ml'].includes(itemId)?.08:.25);
     const afterAccessible=await platform.observe(actors[actor],false,{includeStaticItems:false});
     if(!afterAccessible.interactions?.some((entry:any)=>entry.interactionId==='static_pickup'&&entry.entityId===candidate.id))
      await runner.navigate(actors[actor],{id:`advanced_pick_${actor}_${itemId}`,type:'navigate',actor,target:candidate.id,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.5,targetOffset:offset}},signal);
    }catch(error){
     last=error;
     // Treatment-room partitions and equipment carts can block a direct path.
     // Take the same open west-corridor route that the live bed and initial-role
     // flows use, then retry the scene target.
     if(/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))){
      const arrived=await platform.observe(actors[actor],false,{includeStaticItems:false});
      if(arrived.interactions?.some((entry:any)=>entry.interactionId==='static_pickup'&&entry.entityId===candidate.id)){
       // Navigation can stop on a shelf collider after already entering the
       // exact item's interaction radius. Do not walk away to the fallback.
      }else{
      for(const [index,point] of [[-62,0,-8.35],[-65.12,0,-9.35],[-68.5,0,-9.35],[-68.5,0,-20.75]].entries())
       try {
        await runner.navigate(actors[actor],{id:`advanced_item_corridor_${actor}_${itemId}_${index}`,type:'navigate',actor,target:`advanced_item_corridor_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:point,arrivalRadius:.8}},signal);
       } catch (corridorError) {
        // A cart/bed collider can stop a player on the edge of this waypoint.
        // Continue to the next open waypoint and validate only the eventual
        // item acquisition, which is the gameplay requirement.
        if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(corridorError)))throw corridorError;
        last=corridorError;
       }
      try {
       await runner.navigate(actors[actor],{id:`advanced_pick_retry_${actor}_${itemId}`,type:'navigate',actor,target:candidate.id,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:1,targetOffset:[.4,0,.4]}},signal);
      } catch (retryError) {
       if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(retryError)))throw retryError;
       last=retryError;
      }
      }
     }
    }
    for(let attempt=0;attempt<8;attempt++){
     const facing=await platform.observe(actors[actor],false,{includeStaticItems:false});
     const player=facing.client.players.find((entry:any)=>entry.local);
     if(!player||!candidate.position)break;
     const targetYaw=Math.atan2(candidate.position[0]-player.position[0],candidate.position[2]-player.position[2])*180/Math.PI;
     const angle=((targetYaw-player.yaw+540)%360)-180;
     if(Math.abs(angle)<4)break;
     await platform.command(actors[actor],'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal});
     await delay(100,undefined,{signal});
    }
    const deadline=performance.now()+20000;
    while(performance.now()<deadline){
     const state=await platform.observe(actors[actor]);
     if(state.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
     const pickup=state.interactions?.find((interaction:any)=>interaction.interactionId==='static_pickup'&&label.test(interaction.text));
     const tracePlayer=state.client.players.find((entry:any)=>entry.local);
     pickupTrace.push({atMs:performance.now(),position:tracePlayer?.position,candidates:state.interactions?.filter((interaction:any)=>interaction.interactionId==='static_pickup'&&label.test(interaction.text)).map((interaction:any)=>({index:interaction.index,entityId:interaction.entityId,selected:interaction.selected}))??[],executing:pickup?{index:pickup.index,entityId:pickup.entityId}:null});
     if(pickup){
      try {
       await platform.command(actors[actor],'input.execute',{sequence:[{operation:'interactionExecute',index:pickup.index}]},{signal});
      } catch(error) {
       if(!/TARGET_NOT_INTERACTABLE|STATE_CONFLICT/.test(String(error)))throw error;
       last=error;
      }
     }
     await delay(180,undefined,{signal});
    }
   }
   const finalObservation=await platform.observe(actors[actor]);
   if(finalObservation.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
   const finalPickup=finalObservation.interactions?.find((interaction:any)=>interaction.interactionId==='static_pickup'&&label.test(interaction.text));
   if(finalPickup){
    await platform.command(actors[actor],'input.execute',{sequence:[{operation:'interactionExecute',index:finalPickup.index}]},{signal});
    await delay(300,undefined,{signal});
    const afterFinalPickup=await platform.observe(actors[actor]);
    if(afterFinalPickup.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
   }
   await platform.artifact(runId!,`advanced-item-missing-${actor}-${itemId}.json`,{last,candidates,pickupTrace,observation:finalObservation});
   throw new Error(`STATIC_ITEM_NOT_ACQUIRED:${actor}:${itemId}`);
  };
  const approachPatient=async(actor:string)=>{
   await runner.navigate(actors[actor],{id:`advanced_approach_patient_${actor}`,type:'navigate',actor,target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.8,targetOffset:[-1,0,1]}},signal);
  };
  const interact=async(actor:string,interactionId:string,entityId='patient_a')=>{
   const deadline=performance.now()+40000;let last:unknown;
   while(performance.now()<deadline){
    const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    if(state.inputContext==='DialoguePanelUIController'){
     if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_INTERACTION_CHOICE:${actor}:${interactionId}:${state.dialogue.nodeId}`);
     if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
      await runner.step({actors} as any,{id:`advanced_${actor}_${interactionId}_instruction`,type:'dialogueAdvance',actor},signal);
     await delay(150,undefined,{signal});continue;
    }
    try{return await runner.interact(actors[actor],{id:`advanced_${actor}_${interactionId}`,type:'interact',actor,target:interactionId,args:{entityId},mode:'input_adapter'},signal);}
    catch(error){
     last=error;if(!/TARGET_NOT_INTERACTABLE|STATE_CONFLICT/.test(String(error)))throw error;
     // The doctor can finish its route after the navigation arrival is
     // reported. Re-acquire its live entity at a close radius before polling
     // the interaction registry again.
     if(entityId==='npc-doctor-patient-a-critical')
      await navigateAccessible(actor,-61.5,-10,`advanced_reapproach_${actor}_${interactionId}`);
     if(entityId==='npc-doctor-patient-a-critical')
      await platform.command(actors[actor],'input.execute',{sequence:[{operation:'tap',key:'F'}]},{signal});
     await delay(250,undefined,{signal});
    }
   }
   throw last;
  };
  const approachExactInteraction=async(
   actor:string,interactionId:string,point:[number,number],targetOffset:[number,number,number],label:string
  )=>{
   const isVisible=async()=>{
    const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    return state.interactions?.some((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId===interactionId);
   };
   if(await isVisible())return;
   try{await navigateAccessible(actor,point[0],point[1],`${label}_accessible`,.15);}
   catch(error){
    if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!await isVisible())throw error;
   }
   const visibilityDeadline=performance.now()+3000;
   while(performance.now()<visibilityDeadline){if(await isVisible())return;await delay(100,undefined,{signal});}
   try{
    await runner.navigate(actors[actor],{id:`${label}_entity`,type:'navigate',actor,target:'patient_a',mode:'input_adapter',timeoutMs:15000,args:{targetType:'scenarioEntity',arrivalRadius:.25,targetOffset}},signal);
   }catch(error){
    if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!await isVisible())throw error;
   }
   const deadline=performance.now()+5000;
   while(performance.now()<deadline){if(await isVisible())return;await delay(100,undefined,{signal});}
   throw new Error(`PATIENT_INTERACTION_NOT_VISIBLE:${actor}:${interactionId}`);
  };
  const faceWorldPosition=async(actor:string,target:[number,number,number])=>{
   for(let attempt=0;attempt<12;attempt++){
    const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    const player=state.client.players.find((entry:any)=>entry.local);
    if(!player)return;
    const targetYaw=Math.atan2(target[0]-player.position[0],target[2]-player.position[2])*180/Math.PI;
    const angle=((targetYaw-player.yaw+540)%360)-180;
    if(Math.abs(angle)<4)return;
    await platform.command(actors[actor],'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal});
    await delay(100,undefined,{signal});
   }
  };
  const reenterEastRoomBoundary=async(actor:string,label:string)=>{
   for(let attempt=0;attempt<8;attempt++){
    const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    const player=state.client.players.find((entry:any)=>entry.local);
    if(!player)throw new Error(`LOCAL_PLAYER_NOT_FOUND:${actor}`);
    if(player.position[0]<=-59.3){
     await platform.artifact(runId!,`${label}.json`,{attempt,state});
     return;
    }
    await faceWorldPosition(actor,[-60,player.position[1],player.position[2]]);
    await platform.command(actors[actor],'input.execute',{sequence:[{operation:'hold',key:'W',durationMs:40}]},{signal});
    await delay(80,undefined,{signal});
   }
   throw new Error(`ROOM_EAST_BOUNDARY_NOT_REENTERED:${actor}`);
  };
  // P2 owns both raw intubation recipes.  Do this first so the first live run
  // records the exact item-submission UI state instead of guessing its IDs.
  for(const itemId of ['laryngoscope_blade','laryngoscope_handle','endotracheal_tube','stylet'])
   await acquireWorldItem('p2',itemId);
  await craft('p2','laryngoscope');
  await craft('p2','endotracheal_tube_ready');
  // Crafting the two components triggers N008.  Its next node is the
  // interaction-visibility operation for the doctor's laryngoscope handoff;
  // do not attempt the handoff while that presentation is still pending.
  const handoffDialogueDeadline=performance.now()+20000;
  while(true){
   const state=await platform.observe(actors.p2);
   if(state.inputContext!=='DialoguePanelUIController')break;
   if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_HANDOFF_DIALOGUE_CHOICE:${state.dialogue.nodeId}`);
   if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
    await runner.step({actors} as any,{id:`advanced_handoff_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p2'},signal);
   if(performance.now()>handoffDialogueDeadline)throw new Error(`HANDOFF_DIALOGUE_DID_NOT_CLOSE:${state.dialogue.nodeId}`);
   await delay(150,undefined,{signal});
  }
  // Allow the visibility operation to replicate before approaching the NPC.
  // The registry and nearby selector are observed separately below.
  await delay(800,undefined,{signal});
  await releaseAccidentalBedControl('p2');
  try{
   await runner.navigate(actors.p2,{id:'advanced_p2_approach_doctor',type:'navigate',actor:'p2',target:'npc-doctor-patient-a-critical',mode:'input_adapter',timeoutMs:30000,args:{targetType:'npc',arrivalRadius:.5,targetOffset:[-1.4,0,0]}},signal);
  }catch(error){
   if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error)))throw error;
   const state=await platform.observe(actors.p2);
   await platform.artifact(runId!,'patient-a-doctor-direct-approach-failed.json',{error:String(error),state});
   const player=state.client.players.find((p:any)=>p.local);
   // The central structure cannot be crossed at z=-18.8.  When a moving
   // doctor draws P2 out of the treatment room, join the documented L3
   // crossing first, then follow L4 -> L5 -> the treatment-room entrance.
   const exitPoints=player.position[2]<-19
    ? [[-68.2,0,-22.6],[-68.5,0,-20.75]]
    : [[-68.5,0,player.position[2]]];
   for(const [index,point] of [...exitPoints,[-68.5,0,-9.35],[-65.12,0,-9.35],[-61.5,0,-10]].entries())
    await runner.navigate(actors.p2,{id:`advanced_return_doctor_corridor_${index}`,type:'navigate',actor:'p2',target:`advanced_return_doctor_corridor_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:point,arrivalRadius:.3}},signal)
     .catch(routeError=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(routeError)))throw routeError;});
   // Approach from the open west side. The +Z face is occupied by the patient
   // bed and repeatedly leaves the player pushing against its collider.
   await runner.navigate(actors.p2,{id:'advanced_p2_approach_doctor_retry',type:'navigate',actor:'p2',target:'npc-doctor-patient-a-critical',mode:'input_adapter',timeoutMs:30000,args:{targetType:'npc',arrivalRadius:.5,targetOffset:[-1.4,0,0]}},signal)
    .catch(retryError=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(retryError)))throw retryError;});
  }
  const laryngoscopeSubmissionCursor=await platform.historyCursor();
  const nearby=await platform.observe(actors.p2);
  if(!nearby.interactions?.some((entry:any)=>entry.text==='후두경 전달')){
   for(let attempt=0;attempt<4;attempt++){
    const escape=await platform.observe(actors.p2,false,{includeStaticItems:false});
    const player=escape.client.players.find((entry:any)=>entry.local);
    if(!player)break;
    const angle=((90-player.yaw+540)%360)-180;
    if(Math.abs(angle)<4)break;
    await platform.command(actors.p2,'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal});
    await delay(100,undefined,{signal});
   }
   await platform.command(actors.p2,'input.execute',{sequence:[{operation:'hold',key:'S',durationMs:300}]},{signal});
   await platform.artifact(runId!,'patient-a-p2-left-doctor-bed-pocket.json',await platform.observe(actors.p2,false,{includeStaticItems:false}));
   for(const [index,targetPosition] of [[-64,0,-9.9],[-64,0,-8.3],[-63,0,-9]].entries())
    await runner.navigate(actors.p2,{id:`advanced_doctor_clear_sight_${index}`,type:'navigate',actor:'p2',target:`advanced_doctor_clear_sight_${index}`,mode:'input_adapter',timeoutMs:15000,args:{targetType:'position',targetPosition,arrivalRadius:.25}},signal)
     ;
   await releaseAccidentalBedControl('p2');
   // Navigation faces its offset point, not necessarily the entity. Rotate in
   // bounded input deltas until the camera faces the doctor's live position.
   for(let attempt=0;attempt<8;attempt++){
    const facing=await platform.observe(actors.p2,false,{includeStaticItems:false});
    const player=facing.client.players.find((entry:any)=>entry.local);
    const doctor=facing.entities?.find((entry:any)=>entry.id==='npc-doctor-patient-a-critical');
    if(!player||!doctor)break;
    const targetYaw=Math.atan2(doctor.position[0]-player.position[0],doctor.position[2]-player.position[2])*180/Math.PI;
    const angle=((targetYaw-player.yaw+540)%360)-180;
    if(Math.abs(angle)<5)break;
    await platform.command(actors.p2,'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal});
    await delay(100,undefined,{signal});
   }
   await delay(500,undefined,{signal});
  }
  const laryngoscopeSubmissionDeadline=performance.now()+15000;
  while(true){
   // Successful submission hides its selector before the event collector may
   // ingest the replicated completion. Check evidence even when it is hidden.
   const events=await platform.eventHistory(actors.p2,laryngoscopeSubmissionCursor);
   if(events.some((event:any)=>event.kind==='game'&&event.body?.eventType==='signal.registered'
     &&['pass_laryngoscope','sig.pass_laryngoscope'].includes(event.body?.payload?.signalId))){
    await platform.artifact(runId!,'patient-a-laryngoscope-submission-events.json',events);break;
   }
   const state=await platform.observe(actors.p2);
   if(state.inputContext==='ItemSubmissionUIController'){
    await clickUiElement('p2','ItemSubmissionSubmit');
   }else if(state.inputContext==='Gameplay'){
    const target=state.interactions?.find((entry:any)=>entry.text==='후두경 전달');
    if(!target){
     if(state.vehicles?.some((vehicle:any)=>vehicle.id==='bed_a'&&vehicle.locallyControlled)){
      await releaseAccidentalBedControl('p2');
      await runner.navigate(actors.p2,{id:'advanced_p2_recover_delayed_bed_control',type:'navigate',actor:'p2',target:'advanced_p2_recover_delayed_bed_control',mode:'input_adapter',timeoutMs:15000,args:{targetType:'position',targetPosition:[-63,0,-9],arrivalRadius:.35}},signal)
       .catch(recoveryError=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(recoveryError)))throw recoveryError;});
      const recovered=await platform.observe(actors.p2,false,{includeStaticItems:false});
      const player=recovered.client.players.find((entry:any)=>entry.local);
      const doctor=recovered.entities?.find((entry:any)=>entry.id==='npc-doctor-patient-a-critical');
      if(player&&doctor){
       const targetYaw=Math.atan2(doctor.position[0]-player.position[0],doctor.position[2]-player.position[2])*180/Math.PI;
       const angle=((targetYaw-player.yaw+540)%360)-180;
       await platform.command(actors.p2,'input.execute',{sequence:[{operation:'lookDelta',x:angle/player.rotationSensitivity,y:0}]},{signal});
      }
     }
     if(performance.now()>laryngoscopeSubmissionDeadline)throw new Error('LARYNGOSCOPE_HANDOFF_NOT_VISIBLE');
     await delay(250,undefined,{signal});continue;
    }
    const selected=state.interactions.find((entry:any)=>entry.selected)?.index??-1;
    if(selected!==target.index){
     await platform.command(actors.p2,'input.execute',{sequence:[{operation:'tap',key:selected<target.index?'Equals':'Minus'}]},{signal});
    }else{
     await platform.command(actors.p2,'input.execute',{sequence:[{operation:'tap',key:'F'}]},{signal});
    }
   }
   await delay(400,undefined,{signal});
   if(performance.now()>laryngoscopeSubmissionDeadline)throw new Error('LARYNGOSCOPE_SUBMISSION_NOT_CONFIRMED');
  }
  await platform.artifact(runId!,'patient-a-after-first-intubation-submission-ui.json',await platform.observe(actors.p2));
  phase='patient_a_et_tube_submission';
  const etSubmissionCursor=await platform.historyCursor();
  const etSubmissionDeadline=performance.now()+20000;
  while(true){
   const events=await platform.eventHistory(actors.p2,etSubmissionCursor);
   if(events.some((event:any)=>event.kind==='game'&&event.body?.eventType==='signal.registered'
     &&['pass_et_tube_ready','sig.pass_et_tube_ready'].includes(event.body?.payload?.signalId))){
    await platform.artifact(runId!,'patient-a-et-tube-submission-events.json',events);break;
   }
   const state=await platform.observe(actors.p2);
   if(state.inputContext==='ItemSubmissionUIController'){
    await clickUiElement('p2','ItemSubmissionSubmit');
   }else if(state.inputContext==='DialoguePanelUIController'){
    if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_ET_SUBMISSION_DIALOGUE_CHOICE:${state.dialogue.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`advanced_et_submission_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p2'},signal);
   }else if(state.inputContext==='Gameplay'){
    const target=state.interactions?.find((entry:any)=>entry.interactionId==='patient-a-doctor-submit-et-tube'
      &&entry.entityId==='npc-doctor-patient-a-critical');
    if(target){
     const selected=state.interactions.find((entry:any)=>entry.selected)?.index??-1;
     await platform.command(actors.p2,'input.execute',{sequence:[{operation:'tap',key:selected===target.index?'F':selected<target.index?'Equals':'Minus'}]},{signal});
    }
   }
   if(performance.now()>etSubmissionDeadline)throw new Error('ET_TUBE_SUBMISSION_NOT_CONFIRMED');
   await delay(250,undefined,{signal});
  }
  phase='patient_a_remove_stylet';
  const styletDeadline=performance.now()+30000;
  while(true){
   const state=await platform.observe(actors.p2);
   const quest=state.localQuests?.find((entry:any)=>entry.definitionId==='Quest_Intubation_PatientA');
   if(quest?.tasks?.find((task:any)=>task.Identifier==='remove-stylet-patient-a')?.Completed)break;
   if(state.inputContext==='DialoguePanelUIController'){
    if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_STYLET_DIALOGUE_CHOICE:${state.dialogue.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`advanced_stylet_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p2'},signal);
   }else if(state.inputContext==='Gameplay'){
    if(!state.interactions?.some((entry:any)=>entry.interactionId==='remove_intu_stylet'&&entry.entityId==='patient_a'))
     await approachPatient('p2').catch((error)=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error)))throw error;});
    await interact('p2','remove_intu_stylet').catch((error)=>{
     if(!/TARGET_NOT_INTERACTABLE|STATE_CONFLICT/.test(String(error)))throw error;
    });
   }
   if(performance.now()>styletDeadline)throw new Error('STYLET_REMOVAL_NOT_CONFIRMED');
   await delay(250,undefined,{signal});
  }
  await platform.artifact(runId!,'patient-a-after-stylet-removal.json',await platform.observe(actors.p2));
  phase='patient_a_syringe_submission';
  const closeP2Dialogue=async(label:string)=>{
   const deadline=performance.now()+20000;
   while(true){
    const state=await platform.observe(actors.p2);
    if(state.inputContext!=='DialoguePanelUIController')return;
    if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_${label}_DIALOGUE_CHOICE:${state.dialogue.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`advanced_${label}_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p2'},signal);
    if(performance.now()>deadline)throw new Error(`${label}_DIALOGUE_DID_NOT_CLOSE`);
    await delay(150,undefined,{signal});
   }
  };
  await closeP2Dialogue('SYRINGE');
  await acquireWorldItem('p2','syringe_5cc');
  const syringeSubmissionCursor=await platform.historyCursor();
  const syringeSubmissionDeadline=performance.now()+20000;
  while(true){
   const events=await platform.eventHistory(actors.p2,syringeSubmissionCursor);
   if(events.some((event:any)=>event.kind==='game'&&event.body?.eventType==='signal.registered'
     &&['pass_syringe','sig.pass_syringe'].includes(event.body?.payload?.signalId))){
    await platform.artifact(runId!,'patient-a-syringe-submission-events.json',events);break;
   }
   const state=await platform.observe(actors.p2);
   if(state.inputContext==='ItemSubmissionUIController'){
    await clickUiElement('p2','ItemSubmissionSubmit');
   }else if(state.inputContext==='DialoguePanelUIController'){
    if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_SYRINGE_SUBMISSION_CHOICE:${state.dialogue.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`advanced_syringe_submission_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p2'},signal);
   }else if(state.inputContext==='Gameplay'){
    const target=state.interactions?.find((entry:any)=>entry.interactionId==='patient-a-doctor-submit-5cc-syringe'
      &&entry.entityId==='npc-doctor-patient-a-critical');
    if(!target){
     await runner.navigate(actors.p2,{id:'advanced_p2_return_doctor_for_syringe',type:'navigate',actor:'p2',target:'npc-doctor-patient-a-critical',mode:'input_adapter',timeoutMs:20000,args:{targetType:'npc',arrivalRadius:.8,targetOffset:[0,0,1]}},signal)
      .catch(error=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error)))throw error;});
    }else{
     const selected=state.interactions.find((entry:any)=>entry.selected)?.index??-1;
     await platform.command(actors.p2,'input.execute',{sequence:[{operation:'tap',key:selected===target.index?'F':selected<target.index?'Equals':'Minus'}]},{signal});
    }
   }
   if(performance.now()>syringeSubmissionDeadline)throw new Error('SYRINGE_SUBMISSION_NOT_CONFIRMED');
   await delay(250,undefined,{signal});
  }
  phase='patient_a_intubation_plaster';
  await closeP2Dialogue('INTUBATION_PLASTER');
  await acquireWorldItem('p2','plaster');
  try{
   await runner.navigate(actors.p2,{id:'advanced_p2_close_patient_for_intubation_plaster',type:'navigate',actor:'p2',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.2,targetOffset:[-.5,0,.5]}},signal);
  }catch(error){
   const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
   if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
     || !state.interactions?.some((entry:any)=>entry.interactionId==='patient_a_use_plaster_on_intubation'&&entry.entityId==='patient_a'))throw error;
  }
  await interact('p2','patient_a_use_plaster_on_intubation');
  const intubationDeadline=performance.now()+20000;
  while(true){
   const state=await platform.observe(actors.p2);
   const quest=state.localQuests?.find((entry:any)=>entry.definitionId==='Quest_Intubation_PatientA');
   if(quest?.completed||quest?.progress?.Current===quest?.progress?.Target){
    await platform.artifact(runId!,'patient-a-intubation-completed.json',state);break;
   }
   if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
    await runner.step({actors} as any,{id:`advanced_intubation_complete_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p2'},signal);
   if(performance.now()>intubationDeadline)throw new Error('INTUBATION_QUEST_NOT_COMPLETED');
   await delay(200,undefined,{signal});
  }
  phase='patient_a_parallel_treatment_assignments';
  const assignmentQuests:Record<string,string>={
   p1:'Quest_Oxygen_PatientA',
   p3:'Quest_BleedingControl_PatientA',
   p4:'Quest_IV_Line_PatientA'
  };
  const assignmentStarted=performance.now();
  const assignmentOutcomes=await Promise.allSettled(Object.entries(assignmentQuests).map(async([actor,questId])=>{
   const deadline=performance.now()+30000;
   while(true){
    const state=await platform.observe(actors[actor]);
    const quest=state.localQuests?.find((entry:any)=>entry.definitionId===questId&&!entry.placeholder);
    if(quest)return {questId,readyMs:performance.now()-assignmentStarted,quest};
    if(state.inputContext==='DialoguePanelUIController'){
     if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_PARALLEL_ASSIGNMENT_CHOICE:${actor}:${state.dialogue.nodeId}`);
     if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
      await runner.step({actors} as any,{id:`parallel_assignment_dialogue_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
    }
    if(performance.now()>deadline)throw new Error(`PARALLEL_ASSIGNMENT_NOT_READY:${actor}:${questId}`);
    await delay(150,undefined,{signal});
   }
  }));
  await platform.artifact(runId!,'patient-a-parallel-treatment-assignments.json',assignmentOutcomes.map((result,index)=>({
   actor:Object.keys(assignmentQuests)[index],state:result.status,
   ...(result.status==='fulfilled'?result.value:{error:String(result.reason)})
  })));
  for(const result of assignmentOutcomes)if(result.status==='rejected')throw result.reason;
  phase='patient_a_parallel_first_equipment';
  await Promise.all(Object.keys(actors).map(releaseAccidentalBedControl));
  let treatmentCorridorTail:Promise<void>=Promise.resolve();
  const routeToTreatmentRoom=(actor:string)=>{
   const route=treatmentCorridorTail.then(async()=>{
   const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
   const position=state.client.players.find((entry:any)=>entry.local)?.position;
   if(!position||position[2]>=-12)return;
   // The relocated BC cart makes the east lane impassable. Queue only the
   // narrow corridor traversal; item acquisition remains concurrent.
   const roomX=actor==='p1'?-60.5:actor==='p4'?-62.5:-61.5;
   const exitPoints=position[2]<-19?[[-68.2,0,-22.6],[-68.5,0,-20.75]]:[[-68.5,0,position[2]]];
   const points=[...exitPoints,[-68.5,0,-9.35],[-65.12,0,-9.35],[roomX,0,-10]];
   for(const [index,targetPosition] of points.entries())
    await runner.navigate(actors[actor],{id:`parallel_equipment_route_${actor}_${index}`,type:'navigate',actor,target:`parallel_equipment_route_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:.3}},signal);
   });
   treatmentCorridorTail=route.catch(()=>{});
   return route;
  };
  const equipmentStarted=performance.now();
  const leaveBedPocket=async()=>{
   const dialogueDeadline=performance.now()+20000;
   while(true){
    const state=await platform.observe(actors.p3,false,{includeStaticItems:false});
    if(state.inputContext!=='DialoguePanelUIController')break;
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`parallel_p3_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p3'},signal);
    if(performance.now()>dialogueDeadline)throw new Error(`PARALLEL_P3_DIALOGUE_DID_NOT_CLOSE:${state.dialogue.nodeId}`);
    await delay(150,undefined,{signal});
   }
   for(let attempt=0;attempt<4;attempt++){
    const state=await platform.observe(actors.p3,false,{includeStaticItems:false});
    const player=state.client.players.find((entry:any)=>entry.local);
    if(!player)break;
    const angle=((90-player.yaw+540)%360)-180;
    if(Math.abs(angle)<4)break;
    await platform.command(actors.p3,'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal});
    await delay(100,undefined,{signal});
   }
   await platform.command(actors.p3,'input.execute',{sequence:[{operation:'hold',key:'S',durationMs:350}]},{signal});
   await platform.artifact(runId!,'patient-a-p3-left-bed-pocket.json',await platform.observe(actors.p3,false,{includeStaticItems:false}));
  };
  const equipmentOutcomes=await Promise.allSettled([
   (async()=>{
    await routeToTreatmentRoom('p1');
    for(const itemId of ['flowmeter','humidifier_bottle','sterile_distilled_water'])await acquireWorldItem('p1',itemId);
    await craft('p1','humidifier_sterile_distilled_water_bottle');
    await craft('p1','oxyflowmeter');
    return {actor:'p1',itemId:'oxyflowmeter',readyMs:performance.now()-equipmentStarted};
   })(),
   (async()=>{await delay(3000,undefined,{signal});await leaveBedPocket();await routeToTreatmentRoom('p3');await acquireWorldItem('p3','sterile_gloves');return {actor:'p3',itemId:'sterile_gloves',readyMs:performance.now()-equipmentStarted};})(),
   (async()=>{await routeToTreatmentRoom('p4');await acquireWorldItem('p4','cannula_18g');await acquireWorldItem('p4','intravenous_set',[-69.55681,-15.8991995],[.5,0,0]);return {actor:'p4',itemId:'cannula_18g+intravenous_set',readyMs:performance.now()-equipmentStarted};})()
  ]);
  await platform.artifact(runId!,'patient-a-parallel-first-equipment.json',equipmentOutcomes.map(result=>result.status==='fulfilled'
   ?{state:result.status,...result.value}:{state:result.status,error:String(result.reason)}));
  for(const result of equipmentOutcomes)if(result.status==='rejected')throw result.reason;
  phase='patient_a_parallel_first_treatments';
  const waitQuestProgress=async(actor:string,questId:string,current:number)=>{
   const deadline=performance.now()+20000;let lastQuest:any;
   while(true){
    const state=await platform.observe(actors[actor]);
    const quest=state.localQuests?.find((entry:any)=>entry.definitionId===questId);
    if(quest)lastQuest=quest;
    if((quest?.progress?.Current??0)>=current||quest?.completed)return quest;
    const completedEvent=(await platform.eventHistory(actors[actor])).find((row:any)=>
     row.kind==='game'&&row.body?.eventType==='quest.completed'
     &&row.body?.payload?.scenarioId===graph
     &&row.body?.payload?.definitionId===questId
     &&row.body?.payload?.completed===true
     &&row.body?.payload?.placeholder===false);
    if(completedEvent)return {id:questId,definitionId:questId,scenarioId:graph,
     completed:true,completionEvent:completedEvent.body};
    // Completed player quests can be removed in the same replicated frame as
    // their quest.completed event. Only accept disappearance when this wait
    // asks for that quest's declared final target and the quest was observed
    // immediately beforehand; partial progress waits must still see the
    // concrete counter value.
    if(!quest&&lastQuest?.progress?.Target===current)
     return {...lastQuest,completed:true,transitionedOut:true,
      progress:{...lastQuest.progress,Current:current}};
    if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`parallel_progress_dialogue_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>deadline)throw new Error(`QUEST_PROGRESS_NOT_REACHED:${actor}:${questId}:${current}`);
    await delay(150,undefined,{signal});
   }
  };
  const holdInventoryItem=async(actor:string,itemId:string)=>{
   const gameplayDeadline=performance.now()+20000;
   while(true){
    const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    if(state.inputContext!=='DialoguePanelUIController')break;
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`hold_item_dialogue_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>gameplayDeadline)throw new Error(`ITEM_SELECTION_DIALOGUE_DID_NOT_CLOSE:${actor}:${itemId}`);
    await delay(150,undefined,{signal});
   }
   const before=await platform.observe(actors[actor],false,{includeStaticItems:false});
   const local=before.client.players.find((entry:any)=>entry.local);
   const slot=local?.inventory?.slots?.find((entry:any)=>entry.itemId===itemId);
   if(!slot)throw new Error(`INVENTORY_ITEM_NOT_FOUND:${actor}:${itemId}`);
   // Deterministic equivalent of a normal hotbar selection. This avoids a
   // synthetic key edge depending on Unity script Update ordering and also
   // refreshes a replaced ItemInstance in the same selected slot.
   await platform.command(actors[actor],'input.execute',{sequence:[{operation:'hotbarSelect',index:slot.slot}]},{signal});
   let selectionDeadline=performance.now()+3000;
   while(true){
    const selected=await platform.observe(actors[actor],false,{includeStaticItems:false});
    const selectedLocal=selected.client.players.find((entry:any)=>entry.local);
    if(selectedLocal?.handlingItemId===itemId)return;
    if(performance.now()>selectionDeadline){
     await platform.artifact(runId!,`patient-a-handling-item-failed-${actor}-${itemId}.json`,selected);
     throw new Error(`HANDLING_ITEM_NOT_SELECTED:${actor}:${itemId}:${selectedLocal?.handlingItemId??'none'}`);
    }
    await delay(100,undefined,{signal});
   }
  };
  const equipSterileGloves=async()=>{
   await platform.command(actors.p3,'input.execute',{sequence:[{operation:'tap',key:'E'}]},{signal});
   const deadline=performance.now()+10000;
   while((await platform.observe(actors.p3)).inputContext!=='InventoryUIController'){
    if(performance.now()>deadline)throw new Error('P3_INVENTORY_DID_NOT_OPEN_FOR_GLOVES');
    await delay(100,undefined,{signal});
   }
   await clickUiElement('p3','Slot_0');
   await clickUiElement('p3','EquipmentSlot_Glove');
   await platform.command(actors.p3,'input.execute',{sequence:[{operation:'tap',key:'E'}]},{signal});
   return await waitQuestProgress('p3','Quest_BleedingControl_PatientA',1);
  };
  const firstTreatmentOutcomes=await Promise.allSettled([
   (async()=>{
    await holdInventoryItem('p1','oxyflowmeter');
    const oxygenStart=await platform.observe(actors.p1,false,{includeStaticItems:false});
    await platform.artifact(runId!,'patient-a-oxygen-supply-return-start.json',oxygenStart);
    const oxygenPosition=oxygenStart.client.players.find((entry:any)=>entry.local)?.position;
    // The supply rack can be collected from its south face in zone B.
    // Exit south of the relocated cart before turning into the west aisle;
    // the diagonal to A's doorway crosses both the cart and the partition.
    const oxygenExit=oxygenPosition&&oxygenPosition[2]<-12
     ?[[oxygenPosition[0],0,-15.5],[-68.5,0,-15.5]]:[];
    for(const [index,targetPosition] of [...oxygenExit,[-68.5,0,-9.35],[-65.12,0,-9.35],[-59.8,0,-8.73]].entries())
     await runner.navigate(actors.p1,{id:`patient_a_p1_oxyflowmeter_route_${index}`,type:'navigate',actor:'p1',target:`patient_a_p1_oxyflowmeter_route_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:.35}},signal)
      .catch(async error=>{
       const state=await platform.observe(actors.p1,false,{includeStaticItems:false});
       if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
        || !state.interactions?.some((entry:any)=>entry.entityId==='zone_a:oxyflowmeter'&&entry.interactionId==='oxyflowmeter'))throw error;
      });
    await interact('p1','oxyflowmeter','zone_a:oxyflowmeter');
    await delay(250,undefined,{signal});
    const afterTap=await platform.observe(actors.p1,false,{includeStaticItems:false});
    const oxygenAfterTap=afterTap.localQuests?.find((entry:any)=>entry.definitionId==='Quest_Oxygen_PatientA');
    if((oxygenAfterTap?.progress?.Current??0)<1)
     await platform.command(actors.p1,'input.execute',{sequence:[{operation:'hold',key:afterTap.inputBindings?.interact??'F',durationMs:100}]},{signal});
    return {actor:'p1',quest:await waitQuestProgress('p1','Quest_Oxygen_PatientA',1)};
   })(),
   (async()=>({actor:'p3',quest:await equipSterileGloves()}))(),
   (async()=>{
    for(const [index,targetPosition] of [[-68.5,0,-16.7],[-68.5,0,-9.35],[-65.12,0,-9.35],[-63,0,-9]].entries())
     await runner.navigate(actors.p4,{id:`patient_a_p4_return_from_18g_${index}`,type:'navigate',actor:'p4',target:`patient_a_p4_return_from_18g_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:.35}},signal);
    await runner.navigate(actors.p4,{id:'patient_a_p4_approach_for_iv',type:'navigate',actor:'p4',target:'patient_a',mode:'input_adapter',timeoutMs:20000,args:{targetType:'scenarioEntity',arrivalRadius:.8,targetOffset:[-1.5,0,0]}},signal)
     .catch(error=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error)))throw error;});
    await interact('p4','intravenous_line_cannula');
    return {actor:'p4',quest:await waitQuestProgress('p4','Quest_IV_Line_PatientA',1)};
   })()
  ]);
  await platform.artifact(runId!,'patient-a-parallel-first-treatments.json',firstTreatmentOutcomes.map(result=>result.status==='fulfilled'
   ?{state:result.status,...result.value}:{state:result.status,error:String(result.reason)}));
  for(const result of firstTreatmentOutcomes)if(result.status==='rejected')throw result.reason;
  phase='patient_a_parallel_primary_treatments_completion';
  const closeTransientDialogue=async(actor:string,idPrefix:string)=>{
   const deadline=performance.now()+20000;
   while(true){
    const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
    if(state.inputContext!=='DialoguePanelUIController')return;
    if(state.dialogue.hasChoices)throw new Error(`UNEXPECTED_TRANSIENT_DIALOGUE_CHOICE:${actor}:${state.dialogue.nodeId}`);
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`${idPrefix}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>deadline)throw new Error(`TRANSIENT_DIALOGUE_DID_NOT_CLOSE:${actor}:${state.dialogue.nodeId}`);
    await delay(150,undefined,{signal});
   }
  };
  // P2 has finished its airway assignment and otherwise remains directly
  // north of bed_a.  Clear that player first so P3 can leave the west pocket,
  // followed by P4.  This avoids a three-player collider chain around the bed.
  await closeTransientDialogue('p2','patient_a_p2_clear_dialogue');
  await navigateAccessible('p2',-61.5,-7.1,'patient_a_p2_clear_bed_edge');
  await navigateAccessible('p2',-61.5,-5.8,'patient_a_p2_clear_bed_doorway');
  await navigateAccessible('p2',-60,-5.5,'patient_a_p2_clear_bed');
  // The west side of bed_a is only wide enough for one player collider. Keep
  // collection and patient-side preparation concurrent, but let P3 finish and
  // leave that pocket before P1 crosses it toward the wall and P4 heads south.
  let releasePatientPocket!:()=>void;
  const patientPocketReleased=new Promise<void>(resolve=>{releasePatientPocket=resolve;});
  const primaryCompletionOutcomes=await Promise.allSettled([
   (async()=>{
    await acquireWorldItem('p1','tpiece_set');
    await acquireWorldItem('p1','o2_line');
    await approachPatient('p1').catch(async error=>{
     const state=await platform.observe(actors.p1,false,{includeStaticItems:false});
     if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!state.interactions?.some((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='interact_tpiece'))throw error;
    });
    await interact('p1','interact_tpiece');await waitQuestProgress('p1','Quest_Oxygen_PatientA',2);
    await interact('p1','connect_oxygen_line');await waitQuestProgress('p1','Quest_Oxygen_PatientA',3);
    await patientPocketReleased;
    for(const [index,targetPosition] of [[-62.2,0,-10.4],[-59.6,0,-10.4],[-59.6,0,-9.1]].entries()){
     const beforeStep=await platform.observe(actors.p1,false,{includeStaticItems:false});
     if(beforeStep.interactions?.some((entry:any)=>entry.entityId==='zone_a:oxyflowmeter'&&entry.interactionId==='oxyflowmeter'))break;
     try {
      await runner.navigate(actors.p1,{id:`patient_a_p1_return_oxyflowmeter_${index}`,type:'navigate',actor:'p1',target:`zone_a_oxyflowmeter_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:.5}},signal);
     } catch(error) {
      const state=await platform.observe(actors.p1,false,{includeStaticItems:false});
      if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!state.interactions?.some((entry:any)=>entry.entityId==='zone_a:oxyflowmeter'&&entry.interactionId==='oxyflowmeter'))throw error;
      break;
     }
    }
    await interact('p1','oxyflowmeter','zone_a:oxyflowmeter');
    const quest=await waitQuestProgress('p1','Quest_Oxygen_PatientA',4);
    return {actor:'p1',quest};
   })(),
   (async()=>{
    try {
     await acquireWorldItem('p3','gauze');await acquireWorldItem('p3','plaster');
     await runner.navigate(actors.p3,{id:'patient_a_p3_bleeding_approach',type:'navigate',actor:'p3',target:'patient_a',mode:'input_adapter',timeoutMs:20000,args:{targetType:'scenarioEntity',arrivalRadius:.7,targetOffset:[-1.5,0,0]}},signal)
      .catch(async error=>{const state=await platform.observe(actors.p3,false,{includeStaticItems:false});if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!state.interactions?.some((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='patient_a_use_gauze'))throw error;});
     await interact('p3','patient_a_use_gauze');await waitQuestProgress('p3','Quest_BleedingControl_PatientA',2);
     for(let attempt=0;attempt<10;attempt++){
      const state=await platform.observe(actors.p3,false,{includeStaticItems:false});
      const quest=state.localQuests?.find((entry:any)=>entry.definitionId==='Quest_BleedingControl_PatientA');
      if((quest?.progress?.Current??0)>=3||quest?.completed)break;
      if(state.inputContext==='DialoguePanelUIController'){
       if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
        await runner.step({actors} as any,{id:`p3_plaster_retry_dialogue_${attempt}`,type:'dialogueAdvance',actor:'p3'},signal);
       await delay(300,undefined,{signal});
       continue;
      }
      await interact('p3','patient_a_use_plaster_on_gauze');
      await delay(500,undefined,{signal});
     }
     const quest=await waitQuestProgress('p3','Quest_BleedingControl_PatientA',3);
     for(let attempt=0;attempt<4;attempt++){
      await closeTransientDialogue('p3',`patient_a_p3_clear_dialogue_${attempt}`);
      try {
       if(!await navigateAccessible('p3',-62,-6.2,`patient_a_p3_clear_patient_pocket_${attempt}`))
       throw new Error('P3_PATIENT_POCKET_ACCESSIBLE_ROUTE_NOT_FOUND');
       if(!await navigateAccessible('p3',-63,-5.8,`patient_a_p3_clear_patient_return_${attempt}`))
        throw new Error('P3_PATIENT_RETURN_ACCESSIBLE_ROUTE_NOT_FOUND');
       break;
      } catch(error) {
       if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error)))throw error;
       const state=await platform.observe(actors.p3,false,{includeStaticItems:false});
       if(attempt===3){
        await platform.artifact(runId!,'patient-a-p3-pocket-exit-blocked-after-completion.json',state);
        throw error;
       }
      }
     }
     return {actor:'p3',quest};
    } finally { releasePatientPocket(); }
   })(),
   (async()=>{
    await patientPocketReleased;
    await closeTransientDialogue('p4','patient_a_p4_saline_dialogue');
    // Leave the occupied bed pocket toward the open north side.  The saline
    // copy by the upper treatment counter then has a fully marked Accessible
    // route; the old southward path crossed P3's collider beside the bed.
    await navigateAccessible('p4',-62.7,-7.25,'patient_a_p4_saline_clear_bed_edge');
    await navigateAccessible('p4',-63.5,-7.3,'patient_a_p4_saline_clear_bed');
    await acquireWorldItem('p4','normal_saline_1000ml',[-61.97875,-4.36049652],[0,0,-1.5]);
    await closeTransientDialogue('p4','patient_a_p4_saline_return_dialogue');
    await navigateAccessible('p4',-63.5,-7.3,'patient_a_p4_saline_return_doorway');
    await navigateAccessible('p4',-62.7,-7.25,'patient_a_p4_saline_return_bed_edge');
    await runner.navigate(actors.p4,{id:'patient_a_p4_saline_return_patient',type:'navigate',actor:'p4',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.25,targetOffset:[-1,0,0]}},signal).catch(async error=>{
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
      || !state.interactionRegistry?.some((entry:any)=>entry.entityId==='bed_a'&&entry.interactionId==='hang_normal_saline'&&entry.visible&&entry.canInteract))throw error;
    });
    await navigateAccessible('p4',-63,-8.3,'patient_a_p4_saline_bed_west_clear',.1);
    // Keep the player's capsule clear of the bed's south collider.  The
    // Accessibles marker extends to z=-10.945; z=-10.55 leaves roughly 0.4 m
    // to that boundary while adding the clearance the previous -10.1 line
    // lacked in the real four-player scene.
    await navigateAccessible('p4',-63,-10.55,'patient_a_p4_saline_bed_south_west',.1);
    await navigateAccessible('p4',-60.75,-10.55,'patient_a_p4_saline_bed_south',.1);
    const salineBedState=await platform.observe(actors.p4,false,{includeStaticItems:false});
    const salineBed=salineBedState.vehicles?.find((entry:any)=>entry.id==='bed_a');
    if(!salineBed?.position)throw new Error('BED_A_POSITION_NOT_OBSERVED_FOR_SALINE');
    await faceWorldPosition('p4',salineBed.position);
    // The bed interaction is server-authoritative.  Do not treat command
    // acknowledgement as proof that its treatment display has replicated;
    // retry while the bed action remains available and wait for the patient
    // connection to become genuinely interactable.
    for(let attempt=0;attempt<8;attempt++){
     await closeTransientDialogue('p4',`patient_a_p4_before_hang_saline_${attempt}`);
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     const connection=state.interactionRegistry?.find((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='patient_a_normal_saline_connect');
     if(connection?.visible&&connection.canInteract)break;
     const hang=state.interactionRegistry?.find((entry:any)=>entry.entityId==='bed_a'&&entry.interactionId==='hang_normal_saline');
     const aimedHang=state.interactions?.some((entry:any)=>entry.entityId==='bed_a'&&entry.interactionId==='hang_normal_saline');
     if(hang?.visible&&hang.canInteract&&aimedHang){
      await faceWorldPosition('p4',salineBed.position);
      await interact('p4','hang_normal_saline','bed_a');
     }
     await delay(500,undefined,{signal});
    }
    await closeTransientDialogue('p4','patient_a_p4_after_hang_saline_dialogue');
    const salinePatientState=await platform.observe(actors.p4,false,{includeStaticItems:false});
    const salinePatient=salinePatientState.scenarioEntities?.find((entry:any)=>entry.id==='patient_a');
    if(!salinePatient?.position)throw new Error('PATIENT_A_POSITION_NOT_OBSERVED_FOR_SALINE');
    await faceWorldPosition('p4',salinePatient.position);
    for(let attempt=0;attempt<8;attempt++){
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     const quest=state.localQuests?.find((entry:any)=>entry.definitionId==='Quest_IV_Line_PatientA');
     if((quest?.progress?.Current??0)>=2)break;
     const connection=state.interactionRegistry?.find((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='patient_a_normal_saline_connect');
     if(state.inputContext==='DialoguePanelUIController'){
      await closeTransientDialogue('p4',`patient_a_p4_connect_saline_dialogue_${attempt}`);
      continue;
     }
     if(!connection?.visible||!connection.canInteract){
      await delay(400,undefined,{signal});
      continue;
     }
     await interact('p4','patient_a_normal_saline_connect');
     await delay(400,undefined,{signal});
    }
    await waitQuestProgress('p4','Quest_IV_Line_PatientA',2);
    // Use the nearest supply copies and derive every corridor transition from
    // the current Accessibles geometry.  The former fixed route went to the
    // upper-room copies and its generic recovery path crossed the treatment
    // room partition after the first two pickups.
    await acquireWorldItem('p4','cannula_18g');
    await acquireWorldItem('p4','intravenous_set');
    // This plasma bag is mounted just west of the marked vertical corridor;
    // approach it laterally from x + 1.4 instead of from its unmarked south
    // side.  Item pickup remains the normal StaticPlacedItem interaction.
    await acquireWorldItem('p4','plasma_solution_1000ml',undefined,[1.4,0,0]);
    await navigateAccessible('p4',-65.12,-9.35,'patient_a_p4_second_iv_return_corridor');
    await navigateAccessible('p4',-63,-9,'patient_a_p4_second_iv_return_room');
    await approachPatient('p4').catch(error=>{if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error)))throw error;});
    for(let attempt=0;attempt<8;attempt++){
     await closeTransientDialogue('p4',`patient_a_p4_second_iv_dialogue_${attempt}`);
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     const quest=state.localQuests?.find((entry:any)=>entry.definitionId==='Quest_IV_Line_PatientA');
     if((quest?.progress?.Current??0)>=3)break;
     const insertion=state.interactionRegistry?.find((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='intravenous_line_cannula');
     if(insertion?.visible&&insertion.canInteract){
      const patient=state.scenarioEntities?.find((entry:any)=>entry.id==='patient_a');
      if(patient?.position)await faceWorldPosition('p4',patient.position);
      await interact('p4','intravenous_line_cannula','patient_a');
     }
     await delay(500,undefined,{signal});
    }
    await waitQuestProgress('p4','Quest_IV_Line_PatientA',3);
    const plasmaBedState=await platform.observe(actors.p4,false,{includeStaticItems:false});
    const plasmaBed=plasmaBedState.vehicles?.find((entry:any)=>entry.id==='bed_a');
    if(!plasmaBed?.position)throw new Error('BED_A_POSITION_NOT_OBSERVED_FOR_PLASMA');
    // Approach the bed foot from the cleared south lane.  The patient occludes
    // the west-side ray, Level1 captures the north-side ray, and neither marked
    // side passage has enough clearance to walk all the way around the bed.
    await navigateAccessible('p4',-63,-8.3,'patient_a_p4_plasma_bed_west_clear',.1);
    await navigateAccessible('p4',-63,-10.55,'patient_a_p4_plasma_bed_south_west',.1);
    await navigateAccessible('p4',-60.75,-10.55,'patient_a_p4_plasma_bed_south',.1);
    await faceWorldPosition('p4',plasmaBed.position);
    for(let attempt=0;attempt<8;attempt++){
     await closeTransientDialogue('p4',`patient_a_p4_before_hang_plasma_${attempt}`);
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     const connection=state.interactionRegistry?.find((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='patient_a_plasma_solution_connect');
     if(connection?.visible&&connection.canInteract)break;
     const hang=state.interactionRegistry?.find((entry:any)=>entry.entityId==='bed_a'&&entry.interactionId==='hang_plasma_solution');
     const aimedHang=state.interactions?.some((entry:any)=>entry.entityId==='bed_a'&&entry.interactionId==='hang_plasma_solution');
     if(hang?.visible&&hang.canInteract&&aimedHang){
      await faceWorldPosition('p4',plasmaBed.position);
      await interact('p4','hang_plasma_solution','bed_a');
     }
     await delay(500,undefined,{signal});
    }
    for(let attempt=0;attempt<8;attempt++){
     await closeTransientDialogue('p4',`patient_a_p4_connect_plasma_dialogue_${attempt}`);
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     const quest=state.localQuests?.find((entry:any)=>entry.definitionId==='Quest_IV_Line_PatientA');
     if((quest?.progress?.Current??0)>=4||quest?.completed)break;
     const connection=state.interactionRegistry?.find((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='patient_a_plasma_solution_connect');
     if(connection?.visible&&connection.canInteract){
      const patient=state.scenarioEntities?.find((entry:any)=>entry.id==='patient_a');
      if(patient?.position)await faceWorldPosition('p4',patient.position);
      await interact('p4','patient_a_plasma_solution_connect','patient_a');
     }
     await delay(500,undefined,{signal});
    }
    return {actor:'p4',quest:await waitQuestProgress('p4','Quest_IV_Line_PatientA',4)};
   })()
  ]);
  await platform.artifact(runId!,'patient-a-parallel-primary-treatments-completed.json',primaryCompletionOutcomes.map(result=>result.status==='fulfilled'?{state:result.status,...result.value}:{state:result.status,error:String(result.reason)}));
  for(const result of primaryCompletionOutcomes)if(result.status==='rejected')throw result.reason;
  await platform.artifact(runId!,'patient-a-post-primary-state.json',{
   checkpoint:'parallel_primary_treatments_completed',
   fullPlayPassed:false,
   observations:await Promise.all(Object.values(actors).map(id=>platform.observe(id)))
  });
  phase='patient_a_cline_assist';
  // Nurse D closes the "both IV lines secured" report (D018); its branch then resolves the
  // IV-complete signal and ends. Nurse C (the Q012 owner) is waiting on that signal and
  // receives the C-line assignment (D019) together with Quest_Cline_Assist.
  await closeTransientDialogue('p4','patient_a_iv_completion_dialogue');
  const clineAssignmentDeadline=performance.now()+30000;
  while(true){
   const state=await platform.observe(actors.p3);
   if(state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Cline_Assist'&&!quest.placeholder))break;
   if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
    await runner.step({actors} as any,{id:`patient_a_cline_assignment_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p3'},signal);
   if(performance.now()>clineAssignmentDeadline)throw new Error('CLINE_ASSIGNMENT_DID_NOT_ADVANCE');
   await delay(200,undefined,{signal});
  }
  await acquireWorldItem('p3','central_line_set',[-60.7353935,-4.479912],[0,0,-1.5]);
  await navigateAccessible('p3',-63.5,-7.3,'patient_a_p3_cline_return_doorway');
  await navigateAccessible('p3',-62.7,-7.25,'patient_a_p3_cline_return_bed_edge');
  // The doctor is an acting NPC and is not exported as a scenarioEntity
  // navigation target. Approach the adjacent patient, then prove arrival by
  // the doctor's exact interaction appearing in the live nearby list.
  await runner.navigate(actors.p3,{id:'patient_a_p3_cline_return_doctor',type:'navigate',actor:'p3',target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.35,targetOffset:[-1,0,0]}},signal)
   .catch(async error=>{const state=await platform.observe(actors.p3,false,{includeStaticItems:false});if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!state.interactions?.some((entry:any)=>entry.entityId==='npc-doctor-patient-a-critical'&&entry.interactionId==='patient-a-doctor-submit-central-line-set'))throw error;});
  const clineSubmissionCursor=await platform.historyCursor();
  await interact('p3','patient-a-doctor-submit-central-line-set','npc-doctor-patient-a-critical');
  const clineSubmissionDeadline=performance.now()+20000;
  while(true){
   const state=await platform.observe(actors.p3);
   const events=await platform.eventHistory(actors.p3,clineSubmissionCursor);
   if(events.some((row:any)=>row.kind==='game'&&row.body?.eventType==='signal.registered'
      &&row.body?.payload?.signalId==='sig.pass_central_line_set')){
    await platform.artifact(runId!,'patient-a-cline-submission-events.json',events);break;
   }
   if(state.inputContext==='ItemSubmissionUIController')
    await clickUiElement('p3','ItemSubmissionSubmit');
   else if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
    await runner.step({actors} as any,{id:`patient_a_cline_submission_dialogue_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p3'},signal);
   if(performance.now()>clineSubmissionDeadline)throw new Error('CLINE_SUBMISSION_UNCONFIRMED');
   await delay(200,undefined,{signal});
  }
  const clineQuest=await waitQuestProgress('p3','Quest_Cline_Assist',1);
  await platform.artifact(runId!,'patient-a-cline-assist-completed.json',{quest:clineQuest,observations:await Promise.all(Object.values(actors).map(id=>platform.observe(id)))});
  phase='patient_a_level1_fluids';
  // The Q012 owner (nurse C) also owns the rapid-infuser assignment. Both consumables are on the
  // south supply counter; their interaction side lies in Accessible 698072899.
  await closeTransientDialogue('p3','patient_a_cline_completion_dialogue');
  const level1AssignmentDeadline=performance.now()+30000;
  while(true){
   const state=await platform.observe(actors.p3);
   if(state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Lv1_Fluids'&&!quest.placeholder))break;
   if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
    await runner.step({actors} as any,{id:`patient_a_level1_assignment_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p3'},signal);
   if(performance.now()>level1AssignmentDeadline)throw new Error('LEVEL1_ASSIGNMENT_DID_NOT_ADVANCE');
   await delay(200,undefined,{signal});
  }
  await acquireWorldItem('p3','plasma_solution_1000ml',[-63.0646,-4.405],[0,0,-1.5]);
  await acquireWorldItem('p3','blood_bag',[-63.136,-4.636],[0,0,-1.3]);
  await navigateAccessible('p3',-59.6,-5.5,'patient_a_p3_level1_approach');
  for(const [itemId,interactionId,target] of [
   ['plasma_solution_1000ml','level1_add_plasma_solution',1],
   ['blood_bag','level1_add_blood_bag',2]
  ] as const){
   // The rapid infuser's ordinary-player interaction searches the complete
   // inventory for the requested fluid; it is not a held-item interaction.
   await interact('p3',interactionId,'level1_rapid_infuser_a');
   await waitQuestProgress('p3','Quest_Lv1_Fluids',target);
  }
  await interact('p3','level1_connect_cline','level1_rapid_infuser_a');
  const level1Quest=await waitQuestProgress('p3','Quest_Lv1_Fluids',3);
  await platform.artifact(runId!,'patient-a-level1-fluids-completed.json',{quest:level1Quest,observations:await Promise.all(Object.values(actors).map(id=>platform.observe(id)))});
  phase='patient_a_arrest_pulse_check';
  // A completed task can still leave its explanatory dialogue/choice open.
  // Resolve every branch presentation before waiting for the parallel join;
  // otherwise P004 never reaches the arrest entrypoint.
  const pulseAssignmentDeadline=performance.now()+60000;
  while(true){
   const states=await Promise.all(Object.entries(actors).map(async([actor,id])=>({actor,state:await platform.observe(id,false,{includeStaticItems:false})})));
   if(states.some(({actor,state})=>actor==='p2'&&state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Check_Pulse'&&!quest.placeholder)))break;
   for(const {actor,state} of states){
    if(state.inputContext!=='DialoguePanelUIController')continue;
    if(state.dialogue.hasChoices){
     if(state.dialogue.nodeId!=='C008')throw new Error(`UNEXPECTED_POST_PRIMARY_CHOICE:${actor}:${state.dialogue.nodeId}`);
     await runner.step({actors} as any,{id:`patient_a_post_primary_oxygen_choice_${actor}`,type:'dialogueChoose',actor,choiceId:'C008#2'},signal);
    }else if(state.dialogue.canAdvance||state.dialogue.isTextAnimating){
     await runner.step({actors} as any,{id:`patient_a_arrest_assignment_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
    }
   }
   if(performance.now()>pulseAssignmentDeadline){
    await platform.artifact(runId!,'patient-a-pulse-assignment-stalled.json',{observations:states.map(entry=>entry.state)});
    throw new Error('PULSE_ASSIGNMENT_DID_NOT_ADVANCE');
   }
   await delay(200,undefined,{signal});
  }
  await navigateAccessible('p2',-61.5,-7.1,'patient_a_p2_pulse_bed_edge');
  await navigateAccessible('p2',-62.7,-8.3,'patient_a_p2_pulse_patient_west');
  await runner.navigate(actors.p2,{id:'patient_a_p2_pulse_approach',type:'navigate',actor:'p2',target:'patient_a',mode:'input_adapter',timeoutMs:20000,args:{targetType:'scenarioEntity',arrivalRadius:.8,targetOffset:[-1,0,1]}},signal)
   .catch(async error=>{const state=await platform.observe(actors.p2,false,{includeStaticItems:false});if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))||!state.interactions?.some((entry:any)=>entry.entityId==='patient_a'&&entry.interactionId==='assess_pulse_r1'))throw error;});
  await interact('p2','assess_pulse_r1','patient_a');
  const pulseQuest=await waitQuestProgress('p2','Quest_Check_Pulse',1);
  await platform.artifact(runId!,'patient-a-pulse-r1-completed.json',{quest:pulseQuest,observations:await Promise.all(Object.values(actors).map(id=>platform.observe(id)))});
  phase='patient_a_cpr_round1_assignments';
  const cprRound1Quests:Record<string,string>={p1:'Quest_Ambu_A',p2:'Quest_ChestComp_B',p3:'Quest_Defibrillator_C',p4:'Quest_Epi_D'};
  const cprAssignmentDeadline=performance.now()+60000;
  while(true){
   const states=await Promise.all(Object.entries(actors).map(async([actor,id])=>({actor,state:await platform.observe(id,false,{includeStaticItems:false})})));
   if(states.every(({actor,state})=>state.localQuests?.some((quest:any)=>quest.definitionId===cprRound1Quests[actor]&&!quest.placeholder))){
    await platform.artifact(runId!,'patient-a-cpr-round1-assignments.json',{observations:states.map(entry=>entry.state)});break;
   }
   for(const {actor,state} of states){
    if(state.inputContext==='DialoguePanelUIController'&&!state.dialogue.hasChoices&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`patient_a_cpr_r1_assignment_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
   }
   if(performance.now()>cprAssignmentDeadline)throw new Error('CPR_ROUND1_ASSIGNMENTS_DID_NOT_ADVANCE');
   await delay(200,undefined,{signal});
  }
  phase='patient_a_cpr_round1_parallel';
  let releaseP1CprEquipment!:()=>void;
  const p1CprEquipmentReady=new Promise<void>(resolve=>{releaseP1CprEquipment=resolve;});
  let releaseP3CprEquipment!:()=>void;
  const p3CprEquipmentReady=new Promise<void>(resolve=>{releaseP3CprEquipment=resolve;});
  const cprRound1Outcomes=await Promise.allSettled([
   (async()=>{
    try{
     await acquireWorldItem('p1','ambubag',[-61.3281441,-4.40166473],[0,0,-1.5]);
     await acquireWorldItem('p1','reservoir_bag',[-61.3518944,-4.570915],[0,0,-1.3]);
    }finally{releaseP1CprEquipment();}
    await navigateAccessible('p1',-62.7,-7.25,'patient_a_p1_ambu_return_bed_edge');
    const cartSettledDeadline=performance.now()+90000;
    while(true){
     const state=await platform.observe(actors.p1,false,{includeStaticItems:false});
     if(state.vehicles?.some((vehicle:any)=>vehicle.id==='defibrillator_cart_a'
       &&vehicle.latchedPointId==='defibrillatorcart_to_patient'&&!vehicle.locallyControlled))break;
     if(performance.now()>cartSettledDeadline)throw new Error('DEFIBRILLATOR_CART_DID_NOT_SETTLE_BEFORE_AMBU');
     await delay(100,undefined,{signal});
    }
    await approachExactInteraction('p1','remove_tpiece',[-61.55,-8.35],[-1,0,0],'patient_a_p1_ambu_patient_west');
    for(const [interactionId,target] of [['remove_tpiece',1],['connect_ambubag',2],['connect_o2_to_ambu',3],['start_ambu_r1',4]] as const){
     await approachExactInteraction('p1',interactionId,[-61.55,-8.35],[-1,0,0],`patient_a_p1_ambu_${interactionId}`);
     await interact('p1',interactionId,'patient_a');
     // Connecting the reservoir advances through its instruction and the
     // one-option volume control before the start interaction becomes active.
     // Invoking start_ambu_r1 while that dialogue owns input only executes a
     // stale nearby entry and leaves the quest at 3/4.
     if(interactionId==='connect_o2_to_ambu'){
      const fullDeadline=performance.now()+10000;
      while(true){
       const state=await platform.observe(actors.p1,false,{includeStaticItems:false});
       if(state.inputContext==='DialoguePanelUIController'&&state.dialogue.nodeId==='C009'&&state.dialogue.hasChoices){
        await runner.step({actors} as any,{id:'patient_a_ambu_r1_full',type:'dialogueChoose',actor:'p1',choiceId:'C009#0'},signal);break;
       }
       if(state.inputContext==='DialoguePanelUIController'&&!state.dialogue.hasChoices&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
        await runner.step({actors} as any,{id:`patient_a_ambu_r1_prompt_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p1'},signal);
       if(performance.now()>fullDeadline)throw new Error('AMBU_R1_FULL_CHOICE_NOT_PRESENTED');
       await delay(100,undefined,{signal});
      }
     }
     await waitQuestProgress('p1','Quest_Ambu_A',target);
    }
    return {actor:'p1',quest:await waitQuestProgress('p1','Quest_Ambu_A',4)};
   })(),
   (async()=>{
    await closeTransientDialogue('p2','patient_a_p2_before_chestcomp_r1');
    await interact('p2','click_to_start_comp','patient_a');
    return {actor:'p2',quest:await waitQuestProgress('p2','Quest_ChestComp_B',1)};
   })(),
   (async()=>{
    try{
     await acquireWorldItem('p3','epinephrine_ampule',[-62.26141,-4.50128174],[0,0,-1.3]);
     await acquireWorldItem('p3','syringe_5cc');
     await craft('p3','epinephrine_5cc_syringe');
     await acquireWorldItem('p3','normal_saline_20ml',[-62.0683479,-4.412796],[0,0,-1.4]);
     await acquireWorldItem('p3','syringe_20cc',[-62.2582,-4.595],[0,0,-1.25]);
     await craft('p3','normal_saline_20cc_syringe');
     // Pick the pad up last so ordinary pickup selection leaves it in hand
     // for the round-1 chest interaction without a synthetic slot race.
     await acquireWorldItem('p3','defibrillatorpad',[-59.14906,-4.498703],[0,0,-1.2]);
     await navigateAccessible('p3',-59.7,-5.7,'patient_a_p3_defib_control_approach');
    }finally{releaseP3CprEquipment();}
    const controlState=await platform.observe(actors.p3,false,{includeStaticItems:false});
    const control=controlState.interactions?.find((entry:any)=>/제세동.*카트.*조종|defibrillator.*control/i.test(entry.text??''));
    if(!control)throw new Error('DEFIBRILLATOR_CONTROL_NOT_SCANNED');
    await platform.command(actors.p3,'input.execute',{sequence:[{operation:'interactionExecute',index:control.index}]},{signal});
    const cartControlDeadline=performance.now()+10000;
    while(true){
     const state=await platform.observe(actors.p3,false,{includeStaticItems:false});
     if(state.vehicles?.some((vehicle:any)=>vehicle.id==='defibrillator_cart_a'&&vehicle.kind==='defibrillatorCart'&&vehicle.locallyControlled))break;
     if(performance.now()>cartControlDeadline)throw new Error('DEFIBRILLATOR_CONTROL_NOT_ACQUIRED');
     await delay(100,undefined,{signal});
    }
    // The Level1 infuser occupies [-59.79,-7.23]. Enter the treatment room
    // through the positions.md connector (x <= -61.37), then turn east after
    // clearing it instead of driving straight through the equipment collider.
    await drivePatientBed(platform,actors.p3,'defibrillator_cart_a','defibrillatorcart_to_patient',AbortSignal.timeout(90000),90000,[actors.p3],[[-59.2,0,-5.3],[-62,0,-5.5],[-62,0,-7.1],[-62,0,-9],[-60.5,0,-9]],0,'defibrillatorCart');
    await waitQuestProgress('p3','Quest_Defibrillator_C',1);
    for(let attempt=0;attempt<4;attempt++){
     // Stay north of the patient. The east-side pad position can be pushed
     // beyond the room wall when the snapped cart and other players converge.
     await approachExactInteraction('p3','interact_patient_chest',[-60.55,-7.35],[0,0,1],`patient_a_p3_defib_patient_north_${attempt}`);
     await interact('p3','interact_patient_chest','patient_a');
     try{return {actor:'p3',quest:await waitQuestProgress('p3','Quest_Defibrillator_C',2)};}
     catch(error){if(attempt===3)throw error;await closeTransientDialogue('p3',`patient_a_p3_defib_pad_retry_${attempt}`);}
    }
    throw new Error('DEFIBRILLATOR_PAD_R1_NOT_APPLIED');
   })(),
   (async()=>{
    await closeTransientDialogue('p4','patient_a_p4_before_epi_r1');
    await p1CprEquipmentReady;
    await p3CprEquipmentReady;
    const p1ClearedCounterDeadline=performance.now()+30000;
    while(true){
     const p1State=await platform.observe(actors.p1,false,{includeStaticItems:false});
     const p1Position=p1State.client.players.find((entry:any)=>entry.local)?.position;
     if(p1Position&&p1Position[2]<-7)break;
     if(performance.now()>p1ClearedCounterDeadline)throw new Error('P1_DID_NOT_CLEAR_CPR_SUPPLY_COUNTER');
     await delay(100,undefined,{signal});
    }
    for(const [index,targetPosition] of [[-65.12,0,-9.35],[-68.5,0,-9.35],[-68.5,0,-16.7],[-67,0,-16.7]].entries())
     await runner.navigate(actors.p4,{id:`patient_a_p4_epi_r1_west_supply_${index}`,type:'navigate',actor:'p4',target:`patient_a_p4_epi_r1_west_supply_${index}`,mode:'input_adapter',timeoutMs:25000,args:{targetType:'position',targetPosition,arrivalRadius:.5}},signal);
    await acquireWorldItem('p4','epinephrine_ampule',[-66.96593,-18.0792332],[0,0,1.3]);
    await acquireWorldItem('p4','syringe_5cc',[-67.1505,-18.104],[0,0,1.3]);
    await craft('p4','epinephrine_5cc_syringe');
    await acquireWorldItem('p4','normal_saline_20ml',[-66.87743,-18.2294827],[0,0,1.3]);
    await acquireWorldItem('p4','syringe_20cc',[-67.1077042,-18.074],[0,0,1.3]);
    await craft('p4','normal_saline_20cc_syringe');
    for(const [index,targetPosition] of [[-68.5,0,-16.7],[-68.5,0,-9.35],[-65.12,0,-9.35],[-62.7,0,-7.25]].entries())
     await runner.navigate(actors.p4,{id:`patient_a_p4_epi_r1_west_return_${index}`,type:'navigate',actor:'p4',target:`patient_a_p4_epi_r1_west_return_${index}`,mode:'input_adapter',timeoutMs:25000,args:{targetType:'position',targetPosition,arrivalRadius:.5}},signal);
    await approachExactInteraction('p4','patient_a_use_epinephrine_5cc_syringe',[-60.55,-9.35],[0,0,-1],'patient_a_p4_epi_patient_south');
    for(let attempt=0;attempt<4;attempt++){
     await interact('p4','patient_a_use_epinephrine_5cc_syringe','patient_a');
     try{await waitQuestProgress('p4','Quest_Epi_D',1);break;}catch(error){if(attempt===3)throw error;await closeTransientDialogue('p4',`patient_a_p4_epi_r1_retry_${attempt}`);}
    }
    await closeTransientDialogue('p4','patient_a_p4_after_epi_r1');
    for(let attempt=0;attempt<4;attempt++){
     await interact('p4','patient_a_use_normal_saline_20cc_syringe','patient_a');
     try{await waitQuestProgress('p4','Quest_Epi_D',2);break;}catch(error){if(attempt===3)throw error;await closeTransientDialogue('p4',`patient_a_p4_ns_r1_retry_${attempt}`);}
    }
    return {actor:'p4',quest:await waitQuestProgress('p4','Quest_Epi_D',2)};
   })()
  ]);
  await platform.artifact(runId!,'patient-a-cpr-round1-completed.json',cprRound1Outcomes.map(result=>result.status==='fulfilled'?{state:result.status,...result.value}:{state:result.status,error:String(result.reason)}));
  for(const outcome of cprRound1Outcomes)if(outcome.status==='rejected')throw outcome.reason;
  phase='patient_a_cpr_round1_assessments';
  const cprRound2Quests:Record<string,string|undefined>={p1:'Quest_ChestComp_A',p2:'Quest_Ambu_B',p3:'Quest_Epi_C',p4:undefined};
  const assessmentAnswers:Record<string,Record<string,number>>={
   // Each branch continues directly from its round-1 review into the
   // round-2 role review before publishing the next treatment quest.
   p1:{C010:1,C011:1,C021:1,C022:1,C023:1,C024:2},
   p2:{C012:1,C013:1,C014:1,C015:2,C025:0,C026:1},
   p3:{C016:3,C017:0,C018:3,C027:1,C028:2},
   p4:{C019:1,C020:2,C029:3,C030:0,C031:3}
  };
  const completeAssessmentChoices=async(actor:string,id:string,answers:Record<string,number>)=>{
   const deadline=performance.now()+60000,completed=new Set<string>();
   while(true){
    const state=await platform.observe(id,false,{includeStaticItems:false});
    const assignedQuest=cprRound2Quests[actor];
    if(assignedQuest&&state.localQuests?.some((quest:any)=>quest.definitionId===assignedQuest&&!quest.placeholder))return;
    // The parallel branch assignment remains listed while this client waits
    // for peers. Gameplay input after the final correct feedback is the
    // authoritative local completion state for the quest-less defib branch.
    if(!assignedQuest&&completed.has('C031')&&state.inputContext!=='DialoguePanelUIController')return;
    const nodeId=state.dialogue.nodeId,answer=answers[nodeId];
    if(state.inputContext==='DialoguePanelUIController'&&answer!==undefined&&state.dialogue.hasChoices){
     await runner.step({actors} as any,{id:`patient_a_cpr_r1_assessment_${actor}_${nodeId}`,type:'dialogueChoose',actor,choiceId:`${nodeId}#${answer}`},signal);
     completed.add(nodeId);
    }else if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`patient_a_cpr_r1_assessment_${actor}_advance_${nodeId}`,type:'dialogueAdvance',actor},signal);
    if(performance.now()>deadline)throw new Error(`CPR_ROUND1_ASSESSMENT_DID_NOT_ADVANCE:${actor}:${[...completed].join(',')}`);
    await delay(150,undefined,{signal});
   }
  };
  await Promise.all(Object.entries(actors).map(([actor,id])=>completeAssessmentChoices(
   actor,id,assessmentAnswers[actor])));
  const round2States=await Promise.all(Object.entries(actors).map(async([actor,id])=>({actor,state:await platform.observe(id,false,{includeStaticItems:false})})));
  await platform.artifact(runId!,'patient-a-cpr-round2-assignments.json',{observations:round2States.map(entry=>entry.state)});
  phase='patient_a_cpr_round2_treatments';
  const cprRound2Outcomes=await Promise.allSettled([
   (async()=>{
    await approachExactInteraction('p1','interact_chest',[-61.55,-8.35],[-1,0,0],'patient_a_p1_chest_r2_west');
    await interact('p1','interact_chest');
    return {actor:'p1',quest:await waitQuestProgress('p1','Quest_ChestComp_A',1)};
   })(),
   (async()=>{
    await approachExactInteraction('p2','start_ambu_r2',[-60.55,-7.35],[0,0,1],'patient_a_p2_ambu_r2_north');
    await interact('p2','start_ambu_r2');
    return {actor:'p2',quest:await waitQuestProgress('p2','Quest_Ambu_B',1)};
   })(),
   (async()=>{
    // Q025 publishes the quest before the doctor's four-minute instruction
    // and preparation prompt finish. Clear both presentations before trying
    // to move toward the second physical medication set.
    await closeTransientDialogue('p3','patient_a_p3_epi_r2_assignment');
    await holdInventoryItem('p3','epinephrine_5cc_syringe');
    await approachExactInteraction('p3','patient_a_use_epinephrine_5cc_syringe',[-59.55,-8.35],[1,0,0],'patient_a_p3_epi_r2_east');
    await interact('p3','patient_a_use_epinephrine_5cc_syringe');
    await waitQuestProgress('p3','Quest_Epi_C',1);
    await closeTransientDialogue('p3','patient_a_p3_after_epi_r2');
    await holdInventoryItem('p3','normal_saline_20cc_syringe');
    await interact('p3','patient_a_use_normal_saline_20cc_syringe');
    return {actor:'p3',quest:await waitQuestProgress('p3','Quest_Epi_C',2)};
   })(),
   Promise.resolve({actor:'p4',assessmentCompleted:true})
  ]);
  await platform.artifact(runId!,'patient-a-cpr-round2-completed.json',cprRound2Outcomes.map(result=>result.status==='fulfilled'?{state:result.status,...result.value}:{state:result.status,error:String(result.reason)}));
  for(const outcome of cprRound2Outcomes)if(outcome.status==='rejected')throw outcome.reason;

  phase='patient_a_rosc_pulse';
  const roscPulseAssignmentDeadline=performance.now()+60000;
  while(true){
   const states=await Promise.all(Object.values(actors).map(id=>platform.observe(id,false,{includeStaticItems:false})));
   if(states[0].localQuests?.some((quest:any)=>quest.definitionId==='Quest_Check_Pulse_ROSC'&&!quest.placeholder))break;
   await Promise.all(Object.entries(actors).map(async([actor,id])=>{
    const state=await platform.observe(id,false,{includeStaticItems:false});
    const answer=assessmentAnswers[actor]?.[state.dialogue.nodeId];
    if(state.inputContext==='DialoguePanelUIController'&&answer!==undefined&&state.dialogue.hasChoices)
     await runner.step({actors} as any,{id:`patient_a_rosc_pulse_choice_${actor}_${state.dialogue.nodeId}`,type:'dialogueChoose',actor,choiceId:`${state.dialogue.nodeId}#${answer}`},signal);
    else if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`patient_a_rosc_pulse_prompt_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
   }));
   if(performance.now()>roscPulseAssignmentDeadline)throw new Error('ROSC_PULSE_QUEST_NOT_ASSIGNED');
   await delay(150,undefined,{signal});
  }
  await approachExactInteraction('p1','assess_pulse_r2',[-61.55,-8.35],[-1,0,0],'patient_a_p1_rosc_pulse_west');
  await interact('p1','assess_pulse_r2');
  await waitQuestProgress('p1','Quest_Check_Pulse_ROSC',1);

  phase='patient_a_rosc_assignments';
  const roscQuests:Record<string,string>={p1:'Quest_Return_Triage',p2:'Quest_Cut_Clothing',p3:'Quest_Wait_Others_Rosc_PatientA',p4:'Quest_Check_GCS_ROSC'};
  const roscDeadline=performance.now()+60000;
  while(true){
   const states=await Promise.all(Object.entries(actors).map(async([actor,id])=>({actor,state:await platform.observe(id,false,{includeStaticItems:false})})));
   if(states.every(({actor,state})=>state.localQuests?.some((quest:any)=>quest.definitionId===roscQuests[actor]&&!quest.placeholder)))break;
   await Promise.all(states.map(async({actor,state})=>{
    if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`patient_a_rosc_assignment_prompt_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
   }));
   if(performance.now()>roscDeadline)throw new Error('ROSC_FOLLOWUP_QUESTS_NOT_ASSIGNED');
   await delay(150,undefined,{signal});
  }

  phase='patient_a_rosc_parallel_followup';
  const roscOutcomes=await Promise.allSettled([
   (async()=>{
    for(const [index,targetPosition] of [[-66.6,0,-9.65],[-68.5,0,-9.65],[-72.69,0,-9.65],[-72.69,0,-5.2]].entries())
     await runner.navigate(actors.p1,{id:`patient_a_return_triage_${index}`,type:'navigate',actor:'p1',target:`patient_a_return_triage_${index}`,mode:'input_adapter',timeoutMs:25000,args:{targetType:'position',targetPosition,arrivalRadius:.5}},signal);
    await runner.navigate(actors.p1,{id:'patient_a_return_triage_zone',type:'navigate',actor:'p1',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:20000,args:{arrivalRadius:.5}},signal);
    return {actor:'p1',quest:await waitQuestProgress('p1','Quest_Return_Triage',1)};
   })(),
   (async()=>{
    await approachExactInteraction('p2','remove_patient_clothing',[-60.55,-7.35],[0,0,1],'patient_a_p2_clothing_north');
    await interact('p2','remove_patient_clothing');
    return {actor:'p2',quest:await waitQuestProgress('p2','Quest_Cut_Clothing',1)};
   })(),
   (async()=>{
    await approachExactInteraction('p4','assess_gcs_rosc',[-60.55,-9.35],[0,0,-1],'patient_a_p4_gcs_south');
    await interact('p4','assess_gcs_rosc');
    const answers:Record<string,number>={C032:2,C033:2,C034:5,C035:2};
    const deadline=performance.now()+60000;
    while(true){
     const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
     if(!state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Check_GCS_ROSC'&&!quest.placeholder))return {actor:'p4',completed:true};
     const answer=answers[state.dialogue.nodeId];
     if(state.inputContext==='DialoguePanelUIController'&&answer!==undefined&&state.dialogue.hasChoices)
      await runner.step({actors} as any,{id:`patient_a_rosc_gcs_${state.dialogue.nodeId}`,type:'dialogueChoose',actor:'p4',choiceId:`${state.dialogue.nodeId}#${answer}`},signal);
     else if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
      await runner.step({actors} as any,{id:`patient_a_rosc_gcs_advance_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor:'p4'},signal);
     if(performance.now()>deadline)throw new Error('ROSC_GCS_DID_NOT_COMPLETE');
     await delay(150,undefined,{signal});
    }
   })()
  ]);
  await platform.artifact(runId!,'patient-a-rosc-followup-completed.json',roscOutcomes.map(result=>result.status==='fulfilled'?{state:result.status,...result.value}:{state:result.status,error:String(result.reason)}));
  for(const outcome of roscOutcomes)if(outcome.status==='rejected')throw outcome.reason;

  phase='patient_a_terminal';
  const terminal=await waitForScenarioCompletion(platform,actors,graph,'E038',async(actor,state)=>{
    if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
     await runner.step({actors} as any,{id:`patient_a_terminal_advance_${actor}_${state.dialogue.nodeId}`,type:'dialogueAdvance',actor},signal);
  });
  await auditRuntimeLogs(platform,runId!);
  await platform.artifact(runId!,'patient-a-full-route-completed.json',{fullPlayPassed:true,observations:terminal.map(e=>e.state),terminalEvidence:terminal.map(e=>({actor:e.actor,...e.evidence}))});
  await platform.artifact(runId!,'route-manifest.json',{graph,profile,stage,fullPlayPassed:true,
   promotion:'Four-player quests and terminal lifecycle completed without scenario recovery.',
   sources:sourceHashes.flatMap(result=>result.status==='fulfilled'?[result.value]:[])});
  fullPlayPassed=true;
 }
 if(['triage','move','recognition'].includes(process.argv[4])){
  assert.equal(graph,'patient_b_c_ct');phase='triage_waiting_positions';
  const runner=new Runner(platform),id=actors.p1;
  for(const [actor,z] of [['p4',-7],['p3',-5],['p2',-3]] as const)
   await runner.navigate(actors[actor],{id:`triage_wait_${actor}`,type:'navigate',actor,target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.6,targetOffset:[0,0,z]}},AbortSignal.timeout(16000));
  phase='triage_submission';
  for(const [patient,level] of [['patient_b','level2'],['patient_c','level2'],['patient_dummy_d_b','level5']]){
   const signal=AbortSignal.timeout(45000);
   await runner.navigate(id,{id:`aisle_${patient}`,type:'navigate',actor:'p1',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.7,targetOffset:[-2,0,-3]}},signal);
   await runner.navigate(id,{id:`approach_${patient}`,type:'navigate',actor:'p1',target:patient,mode:'input_adapter',timeoutMs:20000,args:{targetType:'scenarioEntity',arrivalRadius:.7,targetOffset:[0,0,-2]}},signal);
   await platform.artifact(runId,`before-assess-${patient}.json`,await platform.observe(id));
   await runner.interact(id,{id:`assess_${patient}`,type:'interact',target:'triage_assess',args:{entityId:patient},mode:'input_adapter'},signal);
   let target:any;
   try { target=await waitForInteractable(platform,id,`triage-level-${level}`,signal); }
   catch(error){
    const query=await platform.command(id,'ui.query',{automationId:`triage-level-${level}`});
    await platform.artifact(runId,`unavailable-triage-${patient}.json`,query);
    throw error;
   }
   const pointer={...target.screenCenter,screenWidth:target.screenWidth,screenHeight:target.screenHeight,frame:target.uiRevision};
   const submissionCursor=await platform.historyCursor();
   await platform.command(id,'ui.pointer',{...pointer,pressed:true},{signal});
   await platform.command(id,'ui.pointer',{...pointer,pressed:false},{signal});
   const submissionDeadline=performance.now()+10000;
   while(true){
    signal.throwIfAborted();
    const events=await platform.eventHistory(id,submissionCursor);
    const signals=new Set(events.filter(e=>e.kind==='game'&&e.body.eventType==='signal.registered').map(e=>e.body.payload?.signalId));
    if(signals.has(`sig.triage_submitted_${patient}`)&&signals.has(`sig.triage_correct_${patient}`)){
     await platform.artifact(runId,`submission-${patient}.json`,events);break;
    }
    if(performance.now()>submissionDeadline)throw new Error(`TRIAGE_SUBMISSION_UNCONFIRMED:${patient}`);
    await delay(250,undefined,{signal});
   }
   await platform.artifact(runId,`after-assess-${patient}.json`,await platform.observe(id));
  }
  phase='triage_quest_confirmation';
  const outcomes=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>{
   const deadline=performance.now()+20000;
   while(true){
    const snapshot=await platform.observe(id);
    assert.ok(Array.isArray(snapshot.scenario.recoveryNotes)&&snapshot.scenario.recoveryNotes.length===0,'SCENARIO_RECOVERY_USED');
    if(snapshot.scenario.graphId===graph&&snapshot.scenario.nodeId==='P_MOVE'&&
      snapshot.localQuests?.some((q:any)=>q.definitionId==='Quest_Move_BC'&&!q.placeholder&&q.scenarioId===graph)){
     await platform.artifact(runId!,`triage-completed-${actor}.json`,snapshot);return;
    }
    if(performance.now()>deadline){
     await platform.artifact(runId!,`triage-stalled-${actor}.json`,snapshot);
     throw new Error(`TRIAGE_QUEST_DID_NOT_ADVANCE:${actor}`);
    }
    await delay(250);
   }
  }));
  for(const outcome of outcomes)if(outcome.status==='rejected')throw outcome.reason;
 }
 if(['move','recognition'].includes(process.argv[4])){
  const runner=new Runner(platform),id=actors.p1;
  phase='patient_bed_movement';
  for(const [bed,point] of [['bed_b','zone_0:bed_snap_point'],['bed_c','zone_1:bed_snap_point']]){
   let boardingQueue:Promise<void>=Promise.resolve();
   const boardActor=async(actor:string,participant:string,index:number)=>{
    const signal=AbortSignal.timeout(90000);
    if(bed==='bed_c')await delay(index*5000,undefined,{signal});
    phase=`patient_bed_boarding:${bed}:${actor}`;
    if(bed==='bed_c'){
     if(actor==='p3')for(const [index,offset] of [[1.5,0,-2],[-4.3,0,-2]].entries())
      await runner.navigate(participant,{id:`leave_bed_east_side_${actor}_${index}`,type:'navigate',target:'bed_b',mode:'input_adapter',timeoutMs:15000,args:{targetType:'vehicle',arrivalRadius:.6,targetOffset:offset}},signal);
     await runner.navigate(participant,{id:`leave_patient_room_${actor}`,type:'navigate',target:'bed_b',mode:'input_adapter',timeoutMs:15000,args:{targetType:'vehicle',arrivalRadius:.6,targetOffset:[-4.3,0,0]}},signal);
     await runner.navigate(participant,{id:`return_west_corridor_${actor}`,type:'navigate',target:'bed_b',mode:'input_adapter',timeoutMs:15000,args:{targetType:'vehicle',arrivalRadius:.6,targetOffset:[-4.3,0,3.79]}},signal);
     for(const [index,offset] of [[-.175,0,-6.7],[-.175,0,-2.7]].entries())
      await runner.navigate(participant,{id:`return_triage_door_${actor}_${index}`,type:'navigate',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.6,targetOffset:offset}},signal);
    }
    const previousBoarding=boardingQueue;
    let releaseBoarding!:()=>void;
    boardingQueue=new Promise<void>(resolve=>{releaseBoarding=resolve;});
    await previousBoarding;
    try{
    if(actor==='p4')await runner.navigate(participant,{id:`bed_corridor_${bed}_${actor}`,type:'navigate',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.7,targetOffset:[0,0,-3]}},signal);
    await runner.navigate(participant,{id:`bed_aisle_${bed}_${actor}`,type:'navigate',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.7,targetOffset:[-2,0,-3]}},signal);
    await runner.navigate(participant,{id:`approach_${bed}_${actor}`,type:'navigate',target:bed,mode:'input_adapter',timeoutMs:15000,args:{targetType:'vehicle',arrivalRadius:.7,targetOffset:[0,0,-2]}},signal);
    await runner.interact(participant,{id:`control_${bed}_${actor}`,type:'interact',target:'move_bed',args:{entityId:bed},mode:'input_adapter'},signal);
    const controlDeadline=performance.now()+5000;
    while(true){
     const observed=await platform.observe(participant,false,{includeStaticItems:false});
     if(observed.vehicles?.some((v:any)=>v.id===bed&&v.locallyControlled)){
      await platform.artifact(runId!,`boarded-${bed}-${actor}.json`,observed);break;
     }
     if(performance.now()>controlDeadline)throw new Error(`BED_CONTROL_NOT_ACQUIRED:${bed}:${actor}`);
     await delay(100,undefined,{signal});
    }
    }finally{releaseBoarding();}
   };
   if(bed==='bed_c'){
    phase='patient_bed_parallel_return';
    const returned=await Promise.allSettled(Object.entries(actors).map(([actor,participant],index)=>boardActor(actor,participant,index)));
    await platform.artifact(runId,'parallel-bed-c-return.json',returned.map((result,index)=>({actor:Object.keys(actors)[index],status:result.status,error:result.status==='rejected'?String(result.reason):undefined})));
    for(const result of returned)if(result.status==='rejected')throw result.reason;
   }else{
    for(const [index,[actor,participant]] of Object.entries(actors).entries())await boardActor(actor,participant,index);
   }
   phase=`patient_bed_driving:${bed}`;
   await platform.artifact(runId,`before-move-${bed}.json`,await platform.observe(id));
   await drivePatientBed(platform,id,bed,point,AbortSignal.timeout(90000),90000,Object.values(actors),[[-72.7,0,-2],[-72.7,0,-6],[-68.3,0,-9.68],[-68.3,0,bed==='bed_b'?-13.47:-17],[-65,0,bed==='bed_b'?-13.47:-17]]);
   const snapshot=await platform.observe(id);
   assert.equal(snapshot.scenario.recoveryNotes.length,0,'SCENARIO_RECOVERY_USED');
   await platform.artifact(runId,`after-move-${bed}.json`,snapshot);
  }
  phase='patient_bed_quest_confirmation';
  const outcomes=await Promise.allSettled(Object.entries(actors).map(async([actor,instance])=>{
   const deadline=performance.now()+30000,signal=AbortSignal.timeout(30000);
   const expectedQuest=({p1:'Quest_B_Recognition',p2:'Quest_B_Vital',p3:'Quest_B_Pupil_IV',p4:'Quest_B_Oxygen_Bleeding'} as Record<string,string>)[actor];
   while(true){
    const snapshot=await platform.observe(instance);
    assert.ok(Array.isArray(snapshot.scenario.recoveryNotes)&&snapshot.scenario.recoveryNotes.length===0,'SCENARIO_RECOVERY_USED');
    if(snapshot.scenario.graphId===graph&&snapshot.scenario.nodeId==='P_B_CARE'&&snapshot.localQuests?.some((q:any)=>q.scenarioId===graph&&!q.placeholder&&q.definitionId===expectedQuest)){
     await platform.artifact(runId!,`bed-move-completed-${actor}.json`,snapshot);return;
    }
    if(performance.now()>deadline){
     await platform.artifact(runId!,`bed-move-stalled-${actor}.json`,snapshot);
     throw new Error(`BED_MOVE_QUEST_DID_NOT_ADVANCE:${actor}`);
    }
    if(snapshot.inputContext==='DialoguePanelUIController'){
     assert.equal(snapshot.dialogue.graphId,graph,'UNEXPECTED_CARE_DIALOGUE_GRAPH');
     assert.ok(['DOC_C','DOC_D'].includes(snapshot.dialogue.nodeId),'UNEXPECTED_CARE_DIALOGUE_NODE');
     assert.equal(snapshot.dialogue.hasChoices,false,'UNEXPECTED_CARE_DIALOGUE_CHOICE');
     if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
      await runner.step({actors} as any,{id:`care_introduction_${actor}`,type:'dialogueAdvance',actor},signal);
    }
    await delay(250,undefined,{signal});
   }
  }));
  for(const outcome of outcomes)if(outcome.status==='rejected')throw outcome.reason;
 }
 if(process.argv[4]==='recognition'){
  assert.equal(graph,'patient_b_c_ct');
  phase='patient_b_recognition';
  const runner=new Runner(platform),actor='p1',id=actors.p1,signal=AbortSignal.timeout(90000);
  // patient_b remains offset from bed_c after the snap.  Navigating to the bed
  // leaves the player outside the recognition interaction radius, so use the
  // observed scenario entity as the final gameplay target.
  // patient_b is seated on bed_b at zone_0.  The negative-Z side intersects
  // the bed collider; approach from the open corridor side before interacting.
  await runner.navigate(id,{id:'recognition_approach_patient_b',type:'navigate',target:'patient_b',mode:'input_adapter',timeoutMs:20000,args:{targetType:'scenarioEntity',arrivalRadius:.35,targetOffset:[0,0,2]}},signal);
  const performed=new Set<string>();
  while(true){
   signal.throwIfAborted();
   const snapshot=await platform.observe(id,false,{includeStaticItems:false});
   assert.equal(snapshot.scenario.graphId,graph);
   assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
   if(snapshot.localQuests?.some((q:any)=>q.definitionId==='Quest_B_Summary'&&!q.placeholder)){
    assert.equal(performed.size,4,'RECOGNITION_INTERACTIONS_INCOMPLETE');
    await platform.artifact(runId!,'recognition-completed-p1.json',{performed:[...performed],snapshot});break;
   }
   if(snapshot.inputContext==='DialoguePanelUIController'){
    assert.equal(snapshot.dialogue.graphId,graph);
    assert.ok(/^A_REC[1-4]_/.test(snapshot.dialogue.nodeId),'UNEXPECTED_RECOGNITION_DIALOGUE');
    assert.equal(snapshot.dialogue.hasChoices,false);
    if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:'recognition_dialogue',type:'dialogueAdvance',actor},signal);
   }else{
    const interaction=snapshot.interactions?.find((i:any)=>i.entityId==='patient_b'&&/^recognition_[1-4]$/.test(i.interactionId)&&!performed.has(i.interactionId));
    if(interaction){
     await runner.interact(id,{id:interaction.interactionId,type:'interact',target:interaction.interactionId,args:{entityId:'patient_b'},mode:'input_adapter'},signal);
     performed.add(interaction.interactionId);
    }
   }
   await delay(200,undefined,{signal});
  }
 }
 const host=await platform.observe(actors.p1);
 assert.equal(host.scenario.graphId,fullPlayPassed?null:graph);
 assert.ok(host.staticPlacedItems.length>0);
 await platform.artifact(runId,'entry-events.json',await platform.eventHistory(actors.p1));
 console.log(JSON.stringify({runId,graph,entryObserved:true,fullPlayPassed,staticItemCount:host.staticPlacedItems.length}));
}catch(e){
 error=String(e);
 if(runId){
  const cutoff=Date.now()-30000;
  const recentControlFailures=platform.audit.filter((entry:any)=>entry.command?.type==='control.heartbeat'&&entry.error&&Date.parse(entry.at)>=cutoff)
   .slice(-24).map((entry:any)=>({at:entry.at,instanceId:entry.instanceId,error:entry.error,elapsedMs:entry.elapsedMs}));
  await platform.artifact(runId,'entry-error.json',{error,errorCode:(e as any)?.code,phase,recentControlFailures});
  await Promise.allSettled([...platform.instances.values()].map(async i=>{
   await platform.release(i.id).catch(()=>{});
   await platform.artifact(runId!,`failure-${i.id}.json`,await platform.command(i.id,'game.observe',{}, {releaseRead:true}));
  }));
 }
}
finally{
 clearInterval(processKeepalive);
 clearInterval(schedulingTimer);eventLoop.disable();
 await memoryPressure.stop();
 try {
  if(runId){
   const evidenceResults=await Promise.allSettled([...platform.instances.values()].map(async instance=>{
    const rows=await platform.eventHistory(instance.id);
    await platform.artifact(runId!,`quest-evidence-${instance.id}.json`,questEvidence(rows,runId!,instance.id,graph));
   }));
   await platform.artifact(runId,'quest-evidence-export.json',evidenceResults.map((result,index)=>({
    instanceId:[...platform.instances.keys()][index],state:result.status,
    error:result.status==='rejected'?String(result.reason):undefined
   })));
  }
 } finally {
  try { await platform.close(); }
  catch(e) { cleanupError=String(e);error??=cleanupError; }
 }
 if(runId)await platform.artifact(runId,'entry-memory-pressure.json',{samples:memoryPressure.samples});
 if(runId)await platform.artifact(runId,'entry-scheduling.json',{samples:schedulingSamples,commandHistoryWrites:platform.commandHistoryWrites});
 if(runId)await platform.artifact(runId,'entry-shutdown.json',{processes:platform.list(),error,cleanupError});
}
if(error)throw new Error(String(error));
