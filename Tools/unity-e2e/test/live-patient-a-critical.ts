import {readFile} from 'node:fs/promises';
import {createHash} from 'node:crypto';
import {Platform,loadConfig} from '../src/core.ts';
import {waitForInteractable} from '../src/ui-navigation.ts';
import {Runner} from '../src/runner.ts';
import {monitorMemoryPressure} from '../src/memory-pressure.ts';
import {verifyPatientRoles} from '../src/patient-roles.ts';
import {questEvidence} from '../src/quest-evidence.ts';
import {drivePatientBed} from '../src/vehicle-navigation.ts';
import {joinInstances} from '../src/startup.ts';
import {setTimeout as delay} from 'node:timers/promises';
import assert from 'node:assert/strict';
import {monitorEventLoopDelay} from 'node:perf_hooks';
// This runner is intentionally isolated from the live BC investigation.  Do
// not widen it to another graph: its artifacts are used to freeze the A route.
const requestedGraph=process.argv[3]??'patient_a_critical';
assert.equal(requestedGraph,'patient_a_critical','This isolated runner is only for patient_a_critical');
const graph:string=requestedGraph;
const stage=process.argv[4]??'entry';
const profile=process.argv[5]??'mac_direct';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,error:unknown,cleanupError:unknown,phase='launch';
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
  './live-patient-a-critical.ts','../src/runner.ts','../src/vehicle-navigation.ts'];
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
  const dialogueOutcomes=await Promise.allSettled(Object.entries(actors).map(async([actor,id])=>{
   const signal=AbortSignal.timeout(30000);
   while(true){
    signal.throwIfAborted();const state=await platform.observe(id);
    if(state.inputContext==='Gameplay')break;
    if(state.inputContext!=='DialoguePanelUIController'||state.dialogue.hasChoices)throw new Error('UNHANDLED_ARRIVAL_UI');
    if(state.dialogue.canAdvance||state.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`arrival_announcement_${actor}`,type:'dialogueAdvance',actor},signal);
    await delay(200,undefined,{signal});
   }
  }));
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
  const runner=new Runner(platform),signal=AbortSignal.timeout(600000);
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
    const pickup=(state.interactions??[]).find((interaction:any)=>!interaction.entityId&&pattern.test(interaction.text));
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
  // The treatment-counter copy is obstructed by its own counter collider.
  // Use the second scene-placed vital set, whose observed global position is
  // reachable in normal gameplay and grants the same quest item.
  const vital='static-item:vital_set-(4):fcbf41499f824b13a4341e3ac2dd453f';
  try {
   await runner.navigate(actors.p2,{id:'patient_a_pick_vital_set',type:'navigate',actor:'p2',target:vital,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.55,targetOffset:[.5,0,.5]}},signal);
  } catch (error) {
   const state=await platform.observe(actors.p2,false,{includeStaticItems:false});
   if (!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
       || !state.interactions?.some((interaction:any)=>!interaction.entityId&&/활력|vital/i.test(interaction.text))) throw error;
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
   const deadline=performance.now()+5000;
   while(performance.now()<deadline){
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
    if(p2Position&&p2Position[2]<-15)for(const [index,point] of [[-67.5,0,-18.8],[-67.5,0,-9.7],[-62,0,-8.35]].entries())
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
    for(const [index,point] of [[-67.5,0,-18.8],[-67.5,0,-9.7],[-62,0,-8.35]].entries())
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
   while(completed.size<Object.keys(answers).length){
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
  await answerChoiceSequence('p2',actors.p2,{
   C_VITAL_RR_HR_A:2,C_VITAL_BP_A:1,C_VITAL_BT_SPO2_A:2
  },'patient_a_p2_vital',state=>state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Wait_Others_Initial_PatientA'&&!quest.placeholder));
  const avpuDeadline=performance.now()+10000;
  while(true){
   const state=await platform.observe(actors.p3);
   if(state.inputContext==='DialoguePanelUIController'&&state.dialogue.hasChoices
     &&state.dialogue.choices?.some((choice:any)=>choice.choiceId==='C004#2')){
    await runner.step({actors} as any,{id:'patient_a_p3_choose_avpu_pain',type:'dialogueChoose',actor:'p3',choiceId:'C004#2'},signal);
    break;
   }
   if(state.inputContext==='DialoguePanelUIController'&&(state.dialogue.canAdvance||state.dialogue.isTextAnimating))
    await runner.step({actors} as any,{id:'patient_a_p3_advance_gcs_prompt',type:'dialogueAdvance',actor:'p3'},signal);
  if(performance.now()>avpuDeadline)throw new Error('AVPU_CHOICE_NOT_READY');
   await delay(150,undefined,{signal});
  }
  await answerChoiceSequence('p3',actors.p3,{C005:2,C006:3,C007:2,C008:2},'patient_a_p3_gcs',state=>
   state.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Wait_Others_Initial_PatientA'&&!quest.placeholder));
  const collar='static-item:cervical_collar:89259b6810ea4e018b0e90daee6d93db';
  // P4 is also released at zone_3 when the manual bed snap is observed.  Exit
  // that room through the west corridor before it begins its assigned pickup;
  // the straight treatment-room vector crosses the partition collider.
  for(const [index,point] of [[-67.5,0,-18.8],[-67.5,0,-9.7],[-62,0,-8.35]].entries())
   await runner.navigate(actors.p4,{id:`patient_a_p4_return_treatment_${index}`,type:'navigate',actor:'p4',target:`patient_a_p4_return_treatment_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:point,arrivalRadius:.7}},signal);
  try {
   await runner.navigate(actors.p4,{id:'patient_a_pick_cervical_collar',type:'navigate',actor:'p4',target:collar,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.8}},signal);
  } catch(error) {
   const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
   if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
     || !state.interactions?.some((interaction:any)=>!interaction.entityId&&/경추|cervical/i.test(interaction.text)))throw error;
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
   ['wall_suction','scene-item:overworld:wall-suction:bc8a067e9822',/흡인기|wall.?suction/i],
   ['suction_line','static-item:SuctionLine:16cdf4a913194e51a0768d8da4b90d8e',/석션.?라인|suction.?line/i],
   // The scenario-specific treatment-room pickup is placed on an open floor
   // location, away from the suction-catheter collider.
   ['yankauer','scene-item:overworld:yankauer:b8640c3eb4ed',/양커|yankauer/i]
  ] as const;
  for(const [itemId,item,pattern] of suctionItems){
   try {
    await runner.navigate(actors.p4,{id:`patient_a_p4_pick_${itemId}`,type:'navigate',actor:'p4',target:item,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:1}},signal);
   } catch(error) {
    const state=await platform.observe(actors.p4,false,{includeStaticItems:false});
    if(!/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))
      || !state.interactions?.some((interaction:any)=>!interaction.entityId&&pattern.test(interaction.text)))throw error;
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
  await runner.navigate(actors.p4,{id:'patient_a_p4_return_wall_suction',type:'navigate',actor:'p4',target:'patient_a_wall_suction_approach',mode:'input_adapter',timeoutMs:30000,args:{targetType:'position',targetPosition:[-59.3,0,-10.9],arrivalRadius:.45}},signal);
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
  const initialCompletionDeadline=performance.now()+30000;
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
  const runner=new Runner(platform),signal=AbortSignal.timeout(180000);
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
  const acquireWorldItem=async(actor:string,itemId:string)=>{
   const existing=await platform.observe(actors[actor]);
   if(existing.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
   const catalogue=await platform.observe(actors.p1);
   const candidates=(catalogue.staticPlacedItems??[]).filter((item:any)=>item.rewards?.some((reward:any)=>reward.itemId===itemId));
   if(!candidates.length)throw new Error(`STATIC_ITEM_NOT_FOUND:${itemId}`);
   let last:unknown;
   for(const candidate of candidates){
    try{
     await runner.navigate(actors[actor],{id:`advanced_pick_${actor}_${itemId}`,type:'navigate',actor,target:candidate.id,mode:'input_adapter',timeoutMs:30000,args:{targetType:'staticItem',arrivalRadius:.9,targetOffset:[.4,0,.4]}},signal);
    }catch(error){
     last=error;
     // The treatment-room partition blocks a direct path to the equipment
     // carts south of the room.  Take the same open west-corridor route that
     // the live bed and initial-role flows use, then retry the scene target.
     if(/NAVIGATION_STUCK|MOVEMENT_BLOCKED/.test(String(error))&&candidate.position?.[2]<-15){
      for(const [index,point] of [[-62,0,-8.35],[-67.5,0,-9.7],[-67.5,0,-18.8]].entries())
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
    const deadline=performance.now()+8000;
    while(performance.now()<deadline){
     const state=await platform.observe(actors[actor]);
     if(state.client.players.find((p:any)=>p.local)?.inventory.slots.some((slot:any)=>slot.itemId===itemId))return;
     const selected=state.interactions?.find((interaction:any)=>interaction.selected);
     if(selected&&!selected.entityId)await platform.command(actors[actor],'input.execute',{sequence:[{operation:'tap',key:'F'}]},{signal});
     await delay(180,undefined,{signal});
    }
   }
   await platform.artifact(runId!,`advanced-item-missing-${actor}-${itemId}.json`,{last,candidates,observation:await platform.observe(actors[actor])});
   throw new Error(`STATIC_ITEM_NOT_ACQUIRED:${actor}:${itemId}`);
  };
  const approachPatient=async(actor:string)=>{
   await runner.navigate(actors[actor],{id:`advanced_approach_patient_${actor}`,type:'navigate',actor,target:'patient_a',mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.8,targetOffset:[-1,0,1]}},signal);
  };
  const interact=async(actor:string,interactionId:string,entityId='patient_a')=>{
   const deadline=performance.now()+12000;let last:unknown;
   while(performance.now()<deadline){
    try{return await runner.interact(actors[actor],{id:`advanced_${actor}_${interactionId}`,type:'interact',actor,target:interactionId,args:{entityId},mode:'input_adapter'},signal);}
    catch(error){last=error;if(!/TARGET_NOT_INTERACTABLE|STATE_CONFLICT/.test(String(error)))throw error;await delay(150,undefined,{signal});}
   }
   throw last;
  };
  // P2 owns both raw intubation recipes.  Do this first so the first live run
  // records the exact item-submission UI state instead of guessing its IDs.
  for(const itemId of ['laryngoscope_blade','laryngoscope_handle','endotracheal_tube','stylet'])
   await acquireWorldItem('p2',itemId);
  await craft('p2','laryngoscope');
  await craft('p2','endotracheal_tube_ready');
  await runner.navigate(actors.p2,{id:'advanced_p2_approach_doctor',type:'navigate',actor:'p2',target:'npc-doctor-patient-a-critical',mode:'input_adapter',timeoutMs:30000,args:{targetType:'npc',arrivalRadius:.8,targetOffset:[0,0,1]}},signal);
  await interact('p2','patient-a-doctor-submit-laryngoscope','npc-doctor-patient-a-critical');
  await platform.artifact(runId!,'patient-a-after-first-intubation-submission-ui.json',await platform.observe(actors.p2));
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
 assert.equal(host.scenario.graphId,graph);
 assert.ok(host.staticPlacedItems.length>0);
 await platform.artifact(runId,'entry-events.json',await platform.eventHistory(actors.p1));
 console.log(JSON.stringify({runId,graph,entryObserved:true,fullPlayPassed:false,staticItemCount:host.staticPlacedItems.length}));
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
