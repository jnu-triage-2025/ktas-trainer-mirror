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
import {execFile} from 'node:child_process';
import {promisify} from 'node:util';
import {resolve} from 'node:path';
const execFileAsync=promisify(execFile);
const accessiblePathScript=resolve(import.meta.dirname,'../documentation/check_accessible.py');
const graph=process.argv[3]??'patient_b_c_ct';
assert.ok(['patient_a_critical','patient_b_c_ct'].includes(graph));
const profile=process.argv[5]??'mac_direct';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,error:unknown,errorStack:unknown,cleanupError:unknown,phase='launch';
const memoryPressure=monitorMemoryPressure(performance.now());
const eventLoop=monitorEventLoopDelay({resolution:20});eventLoop.enable();
const schedulingSamples:any[]=[];let lastSample=performance.now();
const schedulingTimer=setInterval(()=>{
 const now=performance.now();
 schedulingSamples.push({at:new Date().toISOString(),phase,intervalMs:now-lastSample,eventLoopMaxMs:eventLoop.max/1e6});
 if(schedulingSamples.length>300)schedulingSamples.shift();
 lastSample=now;eventLoop.reset();
},1000);schedulingTimer.unref();
async function withHardDeadline<T>(work:Promise<T>,timeoutMs:number,label:string):Promise<T>{
 let timer:ReturnType<typeof setTimeout>|undefined;
 try{
  return await Promise.race([work,new Promise<T>((_,reject)=>{
   timer=setTimeout(()=>reject(new Error(`E2E_HARD_DEADLINE:${label}`)),timeoutMs);
  })]);
 }finally{
  if(timer!==undefined)clearTimeout(timer);
 }
}
try {
 const launch=await platform.launch(profile,'host_plus_3_clients');runId=launch.runId;
 const sourcePaths=[`../../../Assets/Modules/TriageTrainer/Resources/Scenario/${graph}.scenario.json`,
  `../../../Assets/Modules/TriageTrainer/Resources/Quest/${graph}.quests.quest.json`,
  './live-patient-b-c-ct.ts','../src/runner.ts','../src/vehicle-navigation.ts'];
 const sourceHashes=await Promise.allSettled(sourcePaths.map(async path=>({path,sha256:createHash('sha256').update(await readFile(new URL(path,import.meta.url))).digest('hex')})));
 for(const result of sourceHashes)if(result.status==='rejected')throw result.reason;
 await platform.artifact(runId,'route-manifest.json',{graph,profile,stage:process.argv[4]??'entry',
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
 // Make recognition deterministic for automation. Profiles can retain debug
 // gamerules, while this route validates the ordinary click interaction.
 await chat(actors.p1,'/gamerule DisableInteractionInRecognitionCheck false');
 await chat(actors.p1,'/gamerule UseMicInRecognitionCheck false');
 await platform.artifact(runId,'recognition-gamerules.json',{
  UseMicInRecognitionCheck:false,DisableInteractionInRecognitionCheck:false
 });
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
 if(['arrival','triage','move','recognition','b-care'].includes(process.argv[4])){
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
 if(['triage','move','recognition','b-care'].includes(process.argv[4])){
  assert.equal(graph,'patient_b_c_ct');phase='triage_waiting_positions';
  const runner=new Runner(platform),id=actors.p1;
  // P2 takes the narrowest waiting slot nearest the triage aisle.  Moving it
  // first prevents P3/P4 from occupying the doorway while it is still on its
  // approach route.
  for(const [actor,z] of [['p2',-3],['p3',-5]] as const)
   await runner.navigate(actors[actor],{id:`triage_wait_${actor}`,type:'navigate',actor,target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:40000,args:{arrivalRadius:.6,targetOffset:[0,0,z]}},AbortSignal.timeout(45000));
  for(const [index,targetPosition] of [[-72.7,0,-4.2],[-72.7,0,-6],[-76.3,0,-8]].entries())
   await runner.navigate(actors.p4,{id:`triage_wait_p4_${index}`,type:'navigate',actor:'p4',target:`triage_wait_p4_${index}`,mode:'input_adapter',timeoutMs:25000,args:{targetType:'position',targetPosition,arrivalRadius:.5,stuckWindowMs:8000}},AbortSignal.timeout(80000));
  // P_TRIAGE starts with role presentation dialogue.  Every player must
  // acknowledge it through the normal dialogue input before movement is
  // allowed; advance the independent panels concurrently, then gate the
  // first physical assessment on the host becoming playable.
  const triageDialogueOutcomes=await Promise.allSettled(Object.entries(actors).map(async([actor,participant])=>{
   const signal=AbortSignal.timeout(30000);
   while(true){
    signal.throwIfAborted();
    const snapshot=await platform.observe(participant,false,{includeStaticItems:false,signal,ttlMs:5000});
    assert.equal(snapshot.scenario.graphId,graph);
    assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(snapshot.inputContext==='Gameplay')return;
    if(snapshot.inputContext!=='DialoguePanelUIController'||snapshot.dialogue.hasChoices)throw new Error(`UNHANDLED_TRIAGE_UI:${actor}`);
    if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`triage_dialogue_${actor}`,type:'dialogueAdvance',actor},signal);
    await delay(150,undefined,{signal});
   }
  }));
  await platform.artifact(runId!,'triage-dialogue.json',triageDialogueOutcomes.map((result,index)=>({actor:Object.keys(actors)[index],state:result.status,error:result.status==='rejected'?String(result.reason):undefined})));
  for(const result of triageDialogueOutcomes)if(result.status==='rejected')throw result.reason;
  const triageReadyDeadline=performance.now()+10000;
  while(true){
   const snapshot=await platform.observe(id,false,{includeStaticItems:false,ttlMs:5000});
   if(snapshot.scenario.nodeId==='P_TRIAGE'&&snapshot.client.players.some((player:any)=>player.local&&player.canMove&&!player.movementSuppressed&&!player.scriptedMovement)){
    await platform.artifact(runId!,'triage-host-playable.json',snapshot);break;
   }
   if(performance.now()>triageReadyDeadline){
    await platform.artifact(runId!,'triage-host-not-playable.json',snapshot);
    throw new Error('TRIAGE_HOST_NOT_PLAYABLE');
   }
   await delay(200);
  }
  phase='triage_submission';
  // Current content deliberately presents the low-acuity D patient between
  // B and C; follow its authored interaction gates rather than sorting by
  // entity identifier.
  for(const [patient,level] of [['patient_b','level2'],['patient_dummy_d_b','level5'],['patient_c','level2']]){
   const signal=AbortSignal.timeout(45000);
   await runner.navigate(id,{id:`aisle_${patient}`,type:'navigate',actor:'p1',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.7,targetOffset:[-2,0,-3]}},signal);
   // Spawned patient colliders and nearby beds changed with the refreshed
   // content.  Pick the first normal walking approach that exposes this
   // patient's own live triage interaction instead of treating a serialized
   // patient origin as sufficient evidence of reachability.
   let triageReady=false,lastApproachError:unknown;
   for(const [approachIndex,targetOffset] of [[0,0,-2],[-.7,0,-2],[.7,0,-2],[0,0,-1],[0,0,-3]].entries()){
    try {
     await runner.navigate(id,{id:`approach_${patient}_${approachIndex}`,type:'navigate',actor:'p1',target:patient,mode:'input_adapter',timeoutMs:8000,args:{targetType:'scenarioEntity',arrivalRadius:.7,targetOffset}},signal);
     const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     if(snapshot.inputContext==='DialoguePanelUIController'&&(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)){
      await runner.step({actors} as any,{id:`triage_prompt_${patient}_${approachIndex}`,type:'dialogueAdvance',actor:'p1'},signal);
      await delay(150,undefined,{signal});
     }
     const ready=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     if(ready.inputContext==='Gameplay'&&ready.interactions?.some((entry:any)=>entry.entityId===patient&&entry.interactionId==='triage_assess')){triageReady=true;break;}
    } catch(error) { lastApproachError=error; }
   }
   if(!triageReady)throw lastApproachError??new Error(`TRIAGE_INTERACTION_UNREACHABLE:${patient}`);
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
   // A correct classification presents its confirmation dialogue before the
   // next patient becomes reachable.  Advance it as a player would, and do
   // not begin the following route while movement is intentionally locked.
   const confirmationDeadline=performance.now()+15000;
   while(true){
    const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(snapshot.inputContext==='Gameplay'&&snapshot.client.players.some((player:any)=>player.local&&player.canMove))break;
    if(snapshot.inputContext!=='DialoguePanelUIController'||snapshot.dialogue.hasChoices)throw new Error(`UNHANDLED_TRIAGE_CONFIRMATION_UI:${patient}`);
    if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:`triage_confirmation_${patient}`,type:'dialogueAdvance',actor:'p1'},signal);
    if(performance.now()>confirmationDeadline)throw new Error(`TRIAGE_CONFIRMATION_NOT_CLOSED:${patient}`);
    await delay(150,undefined,{signal});
   }
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
 if(['move','recognition','b-care'].includes(process.argv[4])){
  const runner=new Runner(platform),id=actors.p1;
  phase='patient_bed_movement';
  for(const [bed,point] of [['bed_b','zone_0:bed_snap_point'],['bed_c','zone_1:bed_snap_point']]){
   if(bed==='bed_c')await platform.artifact(runId,'before-board-bed_c.json',await platform.observe(id));
   let boardingQueue:Promise<void>=Promise.resolve();
   let cConnectorQueue:Promise<void>=Promise.resolve();
   let cRouteReady=0;
   let releaseCRoute!:()=>void;
   const cRouteBarrier=new Promise<void>(resolve=>{releaseCRoute=resolve;});
   const waitForCBarrier=(barrier:Promise<void>,label:string,signal:AbortSignal)=>Promise.race([
    barrier,
    delay(150000,undefined,{signal}).then(()=>{throw new Error(`C_BED_BARRIER_TIMEOUT:${label}`);})
   ]);
   const boardActor=async(actor:string,participant:string,index:number)=>{
    const signal=AbortSignal.timeout(bed==='bed_c'?165000:90000);
    phase=`patient_bed_boarding:${bed}:${actor}`;
    if(bed==='bed_c'){
     // All four return routes run concurrently so the authored MOVE_*_WAIT
     // 180-second validators measure gameplay rather than runner staging.
     // Their final offsets are independent and player capsules do not collide
     // under the E2E input adapter; the shared barrier still batches all F keys.
     // Fixed OverworldScene corridor coordinates clear the parked B bed and
     // its doorway.  The final C-bed approach stays dynamic because the bed
     // can be displaced by ordinary physics while B is being parked.
     const routeStart=await platform.observe(participant,false,{includeStaticItems:false,signal,ttlMs:5000});
     const routePlayer=routeStart.client.players.find((entry:any)=>entry.local);
     const routeBed=routeStart.vehicles?.find((entry:any)=>entry.id===bed);
     const alreadyAtBed=routePlayer&&routeBed&&Math.hypot(routePlayer.position[0]-routeBed.position[0],routePlayer.position[2]-routeBed.position[2])<5;
     if(!alreadyAtBed){
     if(routePlayer?.position[0]>-68)for(const [routeIndex,targetPosition] of [[-62.5,0,-15.47],[-68.3,0,-15.47]].entries())
      await runner.navigate(participant,{id:`leave_bed_east_side_${actor}_${routeIndex}`,type:'navigate',target:`bed_c_escape_${routeIndex}`,mode:'input_adapter',timeoutMs:15000,args:{targetType:'position',targetPosition,arrivalRadius:.6}},signal);
     // Enter the clear west lane before heading north; the direct diagonal
     // crosses B's parked collider.  These are serialized OverworldScene
     // corridor coordinates.  Give every avatar a separate parallel lane:
     // sharing the exact narrow waypoint made their capsules deadlock before
     // the all-player boarding barrier could release.
     // -70.3 is blocked by the room geometry for the east-side returner P3;
     // it follows P1 after its stagger, so the proven -68.3 lane is safe.
     const cLaneX=({p1:-69.3,p2:-70.1,p3:-68.3,p4:-70.9} as Record<string,number>)[actor];
     // The navigation controller physically stops at z=-4.76 in front of the
     // C-bed collision volume.  It is within normal interaction range; asking
     // it to penetrate to the serialized aisle center at -3.57 deadlocks.
     // The marked geometry has only one connector across the wall between
     // z=-13.21 and z=-11.12: the x=-68.3 vertical lane.  Serialize only this
     // narrow crossing, then fan back out to distinct lanes above it.
     const previousConnector=cConnectorQueue;
     let releaseConnector!:()=>void;
     cConnectorQueue=new Promise<void>(resolve=>{releaseConnector=resolve;});
     await previousConnector;
     try{
      for(const [routeIndex,targetPosition] of [[-68.3,0,-13.47],[-68.435,0,-11.015],[-68.3,0,-9.68]].entries())
       await runner.navigate(participant,{id:`bed_c_connector_${actor}_${routeIndex}`,type:'navigate',target:`bed_c_connector_${routeIndex}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:.5}},signal);
     }finally{releaseConnector();}
     const cCorridor=[[cLaneX,0,-9.68],[cLaneX,0,-6],[cLaneX,0,-4.76]];
     for(const [routeIndex,targetPosition] of cCorridor.entries())
      await runner.navigate(participant,{id:`bed_c_corridor_${actor}_${routeIndex}`,type:'navigate',target:`bed_c_corridor_${routeIndex}`,mode:'input_adapter',timeoutMs:15000,args:{targetType:'position',targetPosition,arrivalRadius:.6}},signal);
     }
     // Use a live final offset only after the fixed-coordinate corridor has
     // cleared.  The bed can settle slightly under physics, so a serialized
     // origin alone is not reliable for the F interaction radius.
     {
      // Vehicle targetOffset rotates with the bed.  Compute a world-space
      // north stance instead so an arbitrary bed yaw cannot turn this into a
      // lateral push outside the ENTRY door opening.
      const live=await platform.observe(participant,false,{includeStaticItems:false,signal,ttlMs:5000});
      const liveBed=live.vehicles?.find((entry:any)=>entry.id===bed);
      assert.ok(liveBed,'C_BED_NOT_OBSERVED_FOR_STANCE');
      const finalPosition=[liveBed.position[0],0,liveBed.position[2]-1];
      // The patient-bed capsule occupies the last ~0.7m of the requested
      // offset.  This radius still leaves the avatar in the ordinary F
      // interaction range, while preventing a controller deadlock caused by
      // attempting to walk through that capsule.
      try {
       await runner.navigate(participant,{id:`approach_${bed}_${actor}`,type:'navigate',target:`${bed}_world_north_stance`,mode:'input_adapter',timeoutMs:15000,args:{targetType:'position',targetPosition:finalPosition,arrivalRadius:.25,stuckWindowMs:10000}},signal);
      } catch(error) {
       // The final patient-bed collider deliberately blocks movement before
       // the interaction radius.  A normal F press is still available from
       // this observed stopping point; do not reject the route solely for it.
       if(!['NAVIGATION_STUCK','MOVEMENT_BLOCKED','DEADLINE_EXCEEDED'].includes((error as any)?.code))throw error;
       await platform.artifact(runId!,`bed-c-final-approach-stuck-${actor}.json`,{error:String(error),snapshot:await platform.observe(participant,false,{includeStaticItems:false,signal,ttlMs:5000})});
      }
     }
     if(++cRouteReady===1){
      // Resolve each client's live move_bed selection and press its ordinary
      // interaction entry first, without activating it.  Only after all four
      // selections are stable do we send the ordinary interaction keys in one
      // concurrent MCP batch, preventing the bed from moving during selection.
      const selectMoveBed=async(instance:string)=>{
       for(let attempt=0;attempt<32;attempt++){
        const current=await platform.observe(instance,false,{includeStaticItems:false,signal,ttlMs:5000});
        if(current.vehicles?.some((entry:any)=>entry.id===bed&&entry.locallyControlled))return null;
        const target=current.interactions?.find((entry:any)=>entry.entityId===bed&&entry.interactionId==='move_bed');
        const selected=current.interactions?.find((entry:any)=>entry.selected);
        if(!target){
         const player=current.client.players.find((entry:any)=>entry.local);
         const vehicle=current.vehicles?.find((entry:any)=>entry.id===bed);
         if(!player||!vehicle||!(player.rotationSensitivity>0))throw new Error(`BED_INTERACTION_NOT_AVAILABLE:${instance}`);
         const targetYaw=Math.atan2(vehicle.position[0]-player.position[0],vehicle.position[2]-player.position[2])*180/Math.PI;
         const angle=((targetYaw-player.yaw+540)%360)-180;
         await platform.command(instance,'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal,ttlMs:5000});
         await delay(120,undefined,{signal});
         continue;
        }
        if(selected?.entityId===bed&&selected.interactionId==='move_bed')return current.inputBindings.interact;
        const key=!selected||target.index>selected.index?'Equals':'Minus';
        await platform.command(instance,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
        await delay(120,undefined,{signal});
       }
       throw new Error(`BED_INTERACTION_NOT_SELECTED:${instance}`);
      };
      const selectedKey=await selectMoveBed(participant);
      if(selectedKey)await platform.command(participant,'input.execute',{sequence:[{operation:'tap',key:selectedKey}]},{signal,ttlMs:5000});
      releaseCRoute();
     }
     await waitForCBarrier(cRouteBarrier,'route',signal);
    }
    // C must be joined concurrently.  Once a single player has vehicle
    // control, regular navigation input may move the bed away from the other
    // three participants before they can press F.
    let releaseBoarding:(()=>void)|undefined;
    if(bed!=='bed_c'){
     const previousBoarding=boardingQueue;
     boardingQueue=new Promise<void>(resolve=>{releaseBoarding=resolve;});
     await previousBoarding;
    }
    try{
    if(bed!=='bed_c'){
     if(actor==='p4')await runner.navigate(participant,{id:`bed_corridor_${bed}_${actor}`,type:'navigate',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.7,targetOffset:[0,0,-3]}},signal);
     await runner.navigate(participant,{id:`bed_aisle_${bed}_${actor}`,type:'navigate',target:'scen_b:quest_arrival_triage_area',mode:'input_adapter',timeoutMs:15000,args:{arrivalRadius:.7,targetOffset:[-2,0,-3]}},signal);
     await runner.navigate(participant,{id:`approach_${bed}_${actor}`,type:'navigate',target:bed,mode:'input_adapter',timeoutMs:15000,args:{targetType:'vehicle',arrivalRadius:.7,targetOffset:[0,0,-2]}},signal);
    }
    if(bed!=='bed_c')await runner.interact(participant,{id:`control_${bed}_${actor}`,type:'interact',target:'move_bed',args:{entityId:bed},mode:'input_adapter'},signal);
    const controlDeadline=performance.now()+(bed==='bed_c'?12000:5000);
    let lastBoardRetry=performance.now(),boardAttempts=1;
    while(true){
     const observed=await platform.observe(participant,false,{includeStaticItems:false});
     if(observed.vehicles?.some((v:any)=>v.id===bed&&v.locallyControlled)){
      await platform.artifact(runId!,`boarded-${bed}-${actor}.json`,observed);break;
     }
     if(performance.now()>controlDeadline)throw new Error(`BED_CONTROL_NOT_ACQUIRED:${bed}:${actor}`);
     // Vehicle admission is asynchronous.  When the same ordinary interaction
     // remains available, retry a bounded number of real F presses instead of
     // assuming the first acknowledged input joined this player to the bed.
     if(boardAttempts<(bed==='bed_c'?6:3)&&performance.now()-lastBoardRetry>=1200){
      if(bed==='bed_c'){
       // F preserves the player's locally selected normal interaction.  It is
       // safer than globally reserving move_bed while other clients are still
       // receiving the same boarding presentation update.
       await platform.command(participant,'input.execute',{sequence:[{operation:'tap',key:'F'}]},{signal,ttlMs:5000});
      } else if(observed.interactions?.some((entry:any)=>entry.entityId===bed&&entry.interactionId==='move_bed')){
       await runner.interact(participant,{id:`control_retry_${bed}_${actor}_${boardAttempts}`,type:'interact',target:'move_bed',args:{entityId:bed},mode:'input_adapter'},signal);
      }
      boardAttempts++;lastBoardRetry=performance.now();
     }
     await delay(100,undefined,{signal});
    }
    }finally{releaseBoarding?.();}
   };
   if(bed==='bed_c'){
    phase='patient_bed_parallel_return';
    // Bed C can begin drifting as soon as a prior passenger reattaches.  Board
    // P2 first because its return path is shortest and otherwise it can lose
    // the live interaction while the other three complete their detours.
    // D's proven north-east admission point is physically blocked once A/C
    // occupy the west side.  Stage D immediately after B, then park A/C at
    // their independent west-side points before the shared F batch.
    const cBoardOrder=(['p2'] as const).map(actor=>[actor,actors[actor]] as const);
    const returned=await Promise.allSettled(cBoardOrder.map(([actor,participant],index)=>boardActor(actor,participant,index)));
    await platform.artifact(runId,'parallel-bed-c-return.json',returned.map((result,index)=>({actor:cBoardOrder[index][0],status:result.status,error:result.status==='rejected'?String(result.reason):undefined})));
    for(const result of returned)if(result.status==='rejected')throw result.reason;
   }else{
    phase='patient_bed_parallel_boarding';
    const bBoardOrder=[['p1',actors.p1] as const];
    const boarded=await Promise.allSettled(bBoardOrder.map(([actor,participant],index)=>boardActor(actor,participant,index)));
    await platform.artifact(runId,'parallel-bed-b-board.json',boarded.map((result,index)=>({actor:bBoardOrder[index][0],status:result.status,error:result.status==='rejected'?String(result.reason):undefined})));
    for(const result of boarded)if(result.status==='rejected')throw result.reason;
   }
   phase=`patient_bed_driving:${bed}`;
   const driver=bed==='bed_c'?actors.p2:id;
   const participants=bed==='bed_c'?[actors.p2]:[actors.p1];
   const driveTimeout=170000;
   await platform.artifact(runId,`before-move-${bed}.json`,await platform.observe(driver));
   await withHardDeadline(
    // Both beds are already south of the ENTRY threshold after all four
    // players board.  Returning to z=-2 forces a slow 180-degree reversal;
    // proceed through the documented door centre to L5/L4 instead.
    drivePatientBed(platform,driver,bed,point,AbortSignal.timeout(driveTimeout),driveTimeout,participants,[[-72.7,0,-6],[-68.3,0,-9.68],[-68.3,0,bed==='bed_b'?-13.47:-17],[-65,0,bed==='bed_b'?-13.47:-17]]),
    driveTimeout+5000,`drive_${bed}`);
   const snapshot=await platform.observe(driver);
   assert.equal(snapshot.scenario.recoveryNotes.length,0,'SCENARIO_RECOVERY_USED');
   await platform.artifact(runId,`after-move-${bed}.json`,snapshot);
   // Do not pre-position p3/p4 here.  Snapping the second bed can immediately
   // open their patient-monitor presentation, so navigation races the UI and
   // is rejected as MOVEMENT_BLOCKED.  The role-specific care routines below
   // consume that UI first and perform any patient approach they actually need.
  }
  phase='patient_bed_quest_confirmation';
  const outcomes=await Promise.allSettled(Object.entries(actors).map(async([actor,instance])=>{
   const deadline=performance.now()+60000,signal=AbortSignal.timeout(60000);
   const expectedQuest=({p1:'Quest_B_Recognition',p2:'Quest_C_Recognition',p3:null,p4:null} as Record<string,string|null>)[actor];
   while(true){
    // A stalled game.observe must not defeat this branch's deadline.  In
    // particular, all four clients can be transitioning out of vehicle
    // control at once here.
    const snapshot=await platform.observe(instance,false,{includeStaticItems:false,signal,ttlMs:5000});
    assert.ok(Array.isArray(snapshot.scenario.recoveryNotes)&&snapshot.scenario.recoveryNotes.length===0,'SCENARIO_RECOVERY_USED');
    if(snapshot.scenario.graphId===graph&&snapshot.scenario.nodeId==='P_B_C_CARE'&&(!expectedQuest||snapshot.localQuests?.some((q:any)=>q.scenarioId===graph&&!q.placeholder&&q.definitionId===expectedQuest))){
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
 if(['recognition','b-care'].includes(process.argv[4]??'')){
  assert.equal(graph,'patient_b_c_ct');
  phase='patient_b_recognition';
  const runner=new Runner(platform),actor='p1',id=actors.p1,signal=AbortSignal.timeout(90000);
  // patient_b remains offset from bed_c after the snap.  Navigating to the bed
  // leaves the player outside the recognition interaction radius, so use the
  // observed scenario entity as the final gameplay target.
  // Recognition range is measured from the player's eye-height to the
  // patient's capsule collider, rather than from either object's origin.
  // Stop well inside the 1.3m range, while retaining the unobstructed west
  // approach so the actor does not collide with the bed.
  // C-bed dismount can leave P1 east of bed B.  The direct segment to the
  // west interaction point crosses the bed collider.  These two points and
  // the final segment are wholly inside Accessible region 213115292 (verified
  // with documentation/check_accessible.py); go around the bed's south end.
  for(const [index,targetPosition] of [[-62.5,0,-15.47],[-68.3,0,-15.47]].entries())
   await runner.navigate(id,{id:`recognition_b_clear_bed_${index}`,type:'navigate',target:`recognition_b_clear_bed_${index}`,mode:'input_adapter',timeoutMs:15000,args:{targetType:'position',targetPosition,arrivalRadius:.45,stuckWindowMs:8000}},signal);
  await runner.navigate(id,{id:'recognition_approach_patient_b_west',type:'navigate',target:'patient_b',mode:'input_adapter',timeoutMs:20000,args:{targetType:'scenarioEntity',arrivalRadius:.15,targetOffset:[-.7,0,0],stuckWindowMs:8000}},signal);
  let performed=0;
  while(true){
   signal.throwIfAborted();
   const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
   assert.equal(snapshot.scenario.graphId,graph);
   assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
   if(snapshot.localQuests?.some((q:any)=>q.definitionId==='Quest_B_Summary'&&!q.placeholder)){
    assert.equal(performed,4,'RECOGNITION_INTERACTIONS_INCOMPLETE');
    await platform.artifact(runId!,'recognition-completed-p1.json',{performed,snapshot});break;
   }
   if(snapshot.inputContext==='DialoguePanelUIController'){
    assert.equal(snapshot.dialogue.graphId,graph);
    assert.ok(/^A_REC[1-4]_/.test(snapshot.dialogue.nodeId),'UNEXPECTED_RECOGNITION_DIALOGUE');
    assert.equal(snapshot.dialogue.hasChoices,false);
    if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
     await runner.step({actors} as any,{id:'recognition_dialogue',type:'dialogueAdvance',actor},signal);
   }else{
    const interaction=snapshot.interactions?.find((i:any)=>i.entityId==='patient_b'
      && (/^recognition_[1-4]$/.test(i.interactionId)||i.interactionId==='recognition_check'));
    if(interaction){
     await runner.interact(id,{id:interaction.interactionId,type:'interact',target:interaction.interactionId,args:{entityId:'patient_b'},mode:'input_adapter'},signal);
     performed++;
    }
   }
   await delay(200,undefined,{signal});
  }
 }
 if(process.argv[4]==='b-care'){
  phase='patient_b_summary_and_vital';
  const runner=new Runner(platform);
  const correctChoices:Record<string,string>={
   A_AVPU:'A_AVPU_CORRECT',A_GCS_E:'A_E_CORRECT',A_GCS_V:'A_V_CORRECT',A_GCS_M:'A_M_CORRECT',
   A_RIGHT_PRESS:'A_RIGHT_RESULT',A_LEFT_PRESS:'A_LEFT_RESULT',
   A_LEFT_GRADE:'A_LEFT_CORRECT',A_RIGHT_GRADE:'A_RIGHT_CORRECT',
   B_RR_PULSE:'B_RR_CORRECT',B_BP:'B_BP_CORRECT',B_TEMP:'B_TEMP_CORRECT',
   C_B_RR_PULSE:'C_B_RR_CORRECT',C_B_BP:'C_B_BP_CORRECT',C_B_TEMP:'C_B_TEMP_CORRECT',
   C_A_AVPU:'C_A_AVPU_CORRECT',C_A_GCS_E:'C_A_E_CORRECT',C_A_GCS_V:'C_A_V_CORRECT',C_A_GCS_M:'C_A_M_CORRECT',
   C_A_RIGHT_PRESS:'C_A_RIGHT_RESULT',C_A_LEFT_PRESS:'C_A_LEFT_RESULT',
   C_A_LEFT_GRADE:'C_A_LEFT_CORRECT',C_A_RIGHT_GRADE:'C_A_RIGHT_CORRECT'
  };
  type CareActor='p1'|'p2'|'p3'|'p4';
  const finishDialogue=async(actor:CareActor,terminal:string,interaction?:{id:string;entity:string})=>{
   const id=actors[actor],signal=AbortSignal.timeout(180000);
   let approachedPatient=false;
   let lastClosedCheckpointAdvanceAt=-Infinity;
   let patientCRecognitionStep=1,patientCInteractionNotBefore=-Infinity;
   while(true){
    signal.throwIfAborted();const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    if(snapshot.localQuests?.some((q:any)=>q.definitionId===terminal&&!q.placeholder))return snapshot;
    if(interaction?.entity==='patient_c'&&patientCRecognitionStep<=5){
     const expectedSignal=patientCRecognitionStep<=4
       ?`sig.patient_c_recognition_${patientCRecognitionStep}`:'sig.patient_c_strength_checked';
     if(snapshot.signalParameters?.some((entry:any)=>entry.SignalIdentifier===expectedSignal)){
      patientCRecognitionStep++;
      patientCInteractionNotBefore=performance.now()+2500;
      await delay(150,undefined,{signal});continue;
     }
    }
    if(!interaction&&(actor==='p3'||actor==='p4')&&snapshot.inputContext==='PatientMonitorDetailOverlay'){
     const closeQuery=await platform.command(id,'ui.query',{automationId:'PatientMonitorDetailCloseButton'},{signal,ttlMs:5000});
     const close:any=closeQuery.elements?.find((entry:any)=>entry.interactable);
     if(close){
      const pointer={...close.screenCenter,screenWidth:close.screenWidth,screenHeight:close.screenHeight,frame:close.uiRevision};
      await platform.command(id,'ui.pointer',{...pointer,pressed:true},{signal,ttlMs:5000});
      await platform.command(id,'ui.pointer',{...pointer,pressed:false},{signal,ttlMs:5000});
      await delay(150,undefined,{signal});continue;
     }
    }
    if(['DOC_C','DOC_D','C_DOC_C','C_DOC_D'].includes(snapshot.dialogue.nodeId??'')
      && !snapshot.localQuests?.some((q:any)=>!q.placeholder))return snapshot;
    if(snapshot.inputContext==='DialoguePanelUIController'){
     assert.equal(snapshot.dialogue.graphId,graph,'UNEXPECTED_B_CARE_DIALOGUE_GRAPH');
     if(snapshot.dialogue.hasChoices){
      const choice=correctChoices[snapshot.dialogue.nodeId];
      assert.ok(choice,`UNMAPPED_B_CARE_CHOICE:${snapshot.dialogue.nodeId}`);
      try {
       await runner.step({actors} as any,{id:`b_care_choice_${actor}_${snapshot.dialogue.nodeId}`,type:'dialogueChoose',actor,choiceId:choice},signal);
      } catch(error) {
       // The outer observation and Runner's revision-checked observation can
       // straddle one Unity presentation frame.  A stale dialogue surface is
       // not a wrong answer: observe again, while all other failures remain
       // fatal and the branch deadline still bounds retries.
       if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT'].includes((error as any)?.code)) throw error;
      }
     } else if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
      try {
       await runner.step({actors} as any,{id:`b_care_advance_${actor}`,type:'dialogueAdvance',actor},signal);
      } catch(error) {
       if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT'].includes((error as any)?.code)) throw error;
      }
    } else if(interaction&&interaction.entity.startsWith('patient_')&&!approachedPatient&&snapshot.client.players.find((player:any)=>player.local)?.canMove) {
     const local=snapshot.client.players.find((player:any)=>player.local);
     const patient=snapshot.scenarioEntities?.find((entry:any)=>entry.id===interaction.entity);
     const patientDistance=local&&patient?Math.hypot(local.position[0]-patient.position[0],local.position[2]-patient.position[2]):Infinity;
     const alreadyReachable=snapshot.interactions?.some((entry:any)=>entry.entityId===interaction.entity
       && (entry.interactionId===interaction.id||(interaction.entity==='patient_c'&&/^(recognition_[1-4]|strength_check)$/.test(entry.interactionId))))
       && patientDistance<=1.1;
     if(alreadyReachable){
      // The bed move can leave the player at a valid west-side interaction
      // point even though the patient origin remains behind the bed capsule.
      // Preserve that normal gameplay position instead of walking into the
      // collider merely to satisfy a redundant navigation hint.
      approachedPatient=true;
      continue;
     }
     // Refreshed content keeps the live patient interaction but no longer
     // publishes its spawned id to the navigation resolver.  Navigation is
     // only an optional positioning aid here: the authoritative interaction
     // list below decides whether the player is actually in range.
     try {
      await runner.navigate(id,{id:`b_care_approach_${actor}_${interaction.entity}`,type:'navigate',actor,target:interaction.entity,mode:'input_adapter',timeoutMs:30000,args:{targetType:'scenarioEntity',arrivalRadius:.15,targetOffset:[interaction.entity==='patient_c'?0.7:-0.7,0,0],stuckWindowMs:10000}},signal);
     } catch(error) {
      // The C bed's capsule can stop the controller at the same position from
      // which the role-specific recognition actions are already interactable.
      // Treat this as a positioning hint failure and let the authoritative
      // interaction list decide reachability on the following iteration.
      if(!['TARGET_NOT_FOUND','NAVIGATION_STUCK'].includes((error as any)?.code))throw error;
     }
     approachedPatient=true;
    } else if(interaction&&/_FINAL_SUMMARY$/.test(snapshot.dialogue.nodeId??'')&&performance.now()-lastClosedCheckpointAdvanceAt>=1500) {
     const advanceKey=snapshot.inputBindings?.dialogueAdvance;
     assert.ok(advanceKey&&advanceKey!=='None','DIALOGUE_ADVANCE_BINDING_UNAVAILABLE');
     await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:advanceKey}]},{signal,ttlMs:5000});
     lastClosedCheckpointAdvanceAt=performance.now();
    } else if(interaction){
     // Refreshed patient C content exposes four distinct recognition actions,
     // followed by strength_check.  They must be invoked in the authored
     // order; the old monitor_select surface is unrelated and leaves the
     // C_A_REC*_WAIT validators without their required signals.
     const expectedPatientCInteraction=interaction.entity==='patient_c'
       ?patientCRecognitionStep<=4?`recognition_${patientCRecognitionStep}`:'strength_check'
       :undefined;
     if(interaction.entity==='patient_c'&&performance.now()<patientCInteractionNotBefore){
      await delay(150,undefined,{signal});continue;
     }
     const liveInteraction=(interaction.entity==='patient_c'&&expectedPatientCInteraction
       ?snapshot.interactions?.find((i:any)=>i.entityId==='patient_c'&&i.interactionId===expectedPatientCInteraction)
       :undefined)
      ??snapshot.interactions?.find((i:any)=>i.entityId===interaction.entity&&i.interactionId===interaction.id);
     if(!liveInteraction){
      await delay(150,undefined,{signal});continue;
     }
     // The summary and strength checks deliberately share this presentation
     // surface.  Reuse it whenever the current quest exposes it.
     try {
      if(liveInteraction.selected){
       // Once the exact authored action is selected, press the observed
       // interaction binding directly. Re-entering Runner's selection loop
       // can straddle a low-frame-rate presentation refresh and lose the
       // recognition signal even though the prompt remains selected.
       const key=snapshot.inputBindings?.interact;
       assert.ok(key&&key!=='None','INTERACT_BINDING_UNAVAILABLE');
       await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
      }else await runner.interact(id,{id:`b_care_${liveInteraction.interactionId}_${actor}`,type:'interact',target:liveInteraction.interactionId,args:{entityId:liveInteraction.entityId},mode:'input_adapter'},signal);
     } catch(error) {
      // Presentation may briefly retain an interaction while the authoritative
      // dialogue transition removes it.  Re-observe rather than treating this
      // one-frame race as a gameplay failure.
      if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT'].includes((error as any)?.code)) throw error;
     }
    }
    await delay(150,undefined,{signal});
   }
  };
  // All four care branches are activated together.  Start A/B immediately,
  // but do not await them before C/D collect supplies: their validators have
  // their own 180-second clocks.
  // Role checklist data describes required equipment; it does not mint items.
  // Collect every missing supply via an observed StaticPlacedItem identity and
  // verify the local authoritative inventory after each physical F pickup.
  const collectedStaticIds=new Set<string>();
  const collectStaticSupplies=async(actor:CareActor,required:string[])=>{
   const id=actors[actor],signal=AbortSignal.timeout(300000);
   const count=(snapshot:any,itemId:string)=>snapshot.client.players.find((player:any)=>player.local)?.inventory?.slots
    ?.filter((slot:any)=>slot.itemId===itemId).reduce((sum:number,slot:any)=>sum+slot.count,0)??0;
   for(const itemId of required){
    let before=await platform.observe(id,false,{includeStaticItems:true,signal,ttlMs:5000});
    if(count(before,itemId)>0)continue;
    const player=before.client.players.find((entry:any)=>entry.local);
    const candidates=(before.staticPlacedItems??[]).filter((item:any)=>!collectedStaticIds.has(item.id)
      &&item.rewards?.some((reward:any)=>reward.itemId===itemId));
    assert.ok(player&&candidates.length,`STATIC_ITEM_NOT_FOUND:${actor}:${itemId}`);
    candidates.sort((left:any,right:any)=>Math.hypot(left.position[0]-player.position[0],left.position[2]-player.position[2])-
      Math.hypot(right.position[0]-player.position[0],right.position[2]-player.position[2]));
    const provenSupplyIds:Record<string,string>={
     penlight:'scene-item:overworld:penlight:05ef54805bed',
     cannula_20g:'scene-item:overworld:20g:ed9ab7ffd101',
     intravenous_set:'scene-item:overworld:iv-set:aad9ca6418d4',
     humidifier_bottle:'static-item:humidifier-(6):11f75762c9c64a2c8f92151a112c70fb',
     sterile_distilled_water:'static-item:sdw-(7):7add661060d24178ac4f3cb75ef0b676',
     flowmeter:'static-item:flowmeter-(2):b591cc4885c3468883dc8b2d6f881e33',
     nasal_cannula:'scene-item:overworld:nasal-cannula:1',
     gauze:'scene-item:overworld:Gauze (1):8bc71022241c9ddfb269600f411f40ad',
     plaster:'scene-item:overworld:Plaster (1):e188803cb8cfc43cc94540b506a2bfdf'
    };
    const provenId=provenSupplyIds[itemId];
    if(provenId)candidates.sort((left:any,right:any)=>Number(right.id===provenId)-Number(left.id===provenId));
    const p4AlternateIds:Record<string,string>={
     humidifier_bottle:'static-item:humidifier-(5):1445d0e2887943f7abe58b98ec170b15',
     sterile_distilled_water:'static-item:sdw-(6):2339681910724e91986b2d16a90322f8',
     flowmeter:'static-item:flowmeter-(7):ea4c2017eef440bb91150dce4d081cc3',
     nasal_cannula:'scene-item:overworld:nasal-cannula:2',
     gauze:'scene-item:overworld:Gauze (1):37df6e79de8fa5d7844f2d07c3b1f8a9',
     plaster:'scene-item:overworld:Plaster:ef13cffc7230c58874812ac73087ed68'
    };
    const actorPreferredId=actor==='p4'?p4AlternateIds[itemId]:undefined;
    if(actorPreferredId)candidates.sort((left:any,right:any)=>Number(right.id===actorPreferredId)-Number(left.id===actorPreferredId));
    const p3PreferredIds:Record<string,string[]>={
     penlight:['scene-item:overworld:penlight:704d1914e8cb'],
     cannula_20g:['scene-item:overworld:20g:ed9ab7ffd101','scene-item:overworld:20g:1690cdfed9f9'],
     intravenous_set:['scene-item:overworld:iv-set:b07a50308367','scene-item:overworld:iv-set:aad9ca6418d4'],
     normal_saline_1000ml:['static-item:ns1-(2):226cbd60336d46e38d5de7e04ab12d28','static-item:ns1-(3):dcee4b8ca853454499903e17d21f8a08']
    };
    const p3PreferredId=actor==='p3'?p3PreferredIds[itemId]?.find(value=>!collectedStaticIds.has(value)):undefined;
    if(p3PreferredId)candidates.sort((left:any,right:any)=>Number(right.id===p3PreferredId)-Number(left.id===p3PreferredId));
    const p2AlternateId=itemId==='intravenous_set'?'scene-item:overworld:iv-set:b07a50308367':undefined;
    if(actor==='p2'&&p2AlternateId)candidates.sort((left:any,right:any)=>Number(right.id===p2AlternateId)-Number(left.id===p2AlternateId));
    let target:any, lastNavigationError:unknown;
    // A successful room egress can leave the player in interaction radius
    // while the prop origin is still separated by a bed collider.  The live
    // interaction address is stronger evidence than a redundant move request.
    const directlyReachable=candidates.find((candidate:any)=>before.interactions?.some((entry:any)=>entry.interactionId==='static_pickup'&&entry.entityId===candidate.id));
    if(directlyReachable)target=directlyReachable;
    for(const candidate of target?[]:candidates){
     try{
      const current=await platform.observe(id,false,{includeStaticItems:true,signal,ttlMs:5000});
      const local=current.client.players.find((entry:any)=>entry.local);
      assert.ok(local,'LOCAL_PLAYER_NOT_FOUND');
      // Reuse the west-side pickup stance proven by p1.  Approaching this
      // penlight from the east selects the adjacent patient monitor instead.
      if(itemId==='penlight'&&actor==='p2'){
       for(const [routeIndex,[x,z]] of [[-62.5,-15.47],[-68.3,-15.47],[-68.3,-17.35],[-66.42,-17.35]].entries())
        await runner.navigate(id,{id:`supply_stance_${actor}_${itemId}_${routeIndex}`,type:'navigate',actor,
         target:`penlight_c_bed_stance_${routeIndex}`,mode:'input_adapter',timeoutMs:25000,
         args:{targetType:'position',targetPosition:[x,0,z],arrivalRadius:.25,stuckWindowMs:7000}},signal);
       const cBedPenlight=candidates.find((entry:any)=>entry.id==='scene-item:overworld:penlight:704d1914e8cb');
       if(cBedPenlight)target=cBedPenlight;
       else target=candidate;
       break;
      }
      if(itemId==='penlight'&&candidate.id==='scene-item:overworld:penlight:05ef54805bed'){
       await runner.navigate(id,{id:`supply_stance_${actor}_${itemId}`,type:'navigate',actor,
        target:'penlight_west_stance',mode:'input_adapter',timeoutMs:25000,
        args:{targetType:'position',targetPosition:[-64.84981,0,-13.27001],arrivalRadius:.2,stuckWindowMs:7000}},signal);
       target=candidate;break;
      }
      try {
       const {stdout}=await execFileAsync('python3',[accessiblePathScript,
        String(local.position[0]),String(local.position[2]),String(candidate.position[0]),String(candidate.position[2])],{signal});
       const route=JSON.parse(stdout);
       if(route.status==='ok')for(const [routeIndex,[x,z]] of route.path.slice(1,-1).entries())
        await runner.navigate(id,{id:`supply_route_${actor}_${itemId}_${routeIndex}`,type:'navigate',actor,
         target:`supply_route_${routeIndex}`,mode:'input_adapter',timeoutMs:20000,
         args:{targetType:'position',targetPosition:[x,0,z],arrivalRadius:.35,stuckWindowMs:8000}},signal);
      } catch(error) {
       // An outside/disconnected result is not treated as an invented route.
       // Preserve the candidate and let the final authoritative navigation
       // attempt either reach it from the current marked region or reject it.
       await platform.artifact(runId!,`supply-accessible-route-unavailable-${actor}-${itemId}-${candidate.id.replace(/[^a-zA-Z0-9_-]/g,'_')}.json`,{candidate,error:String(error)});
      }
      await runner.navigate(id,{id:`supply_approach_${actor}_${itemId}`,type:'navigate',actor,target:candidate.id,mode:'input_adapter',timeoutMs:25000,args:{targetType:'staticItem',arrivalRadius:.4,stuckWindowMs:6000}},signal);
      target=candidate;break;
     }catch(error){
      if(!['NAVIGATION_STUCK','MOVEMENT_BLOCKED','TARGET_NOT_FOUND','DEADLINE_EXCEEDED'].includes((error as any)?.code))throw error;
      lastNavigationError=error;
      const stopped=await platform.observe(id,false,{includeStaticItems:true,signal,ttlMs:5000});
      const stoppedPlayer=stopped.client.players.find((entry:any)=>entry.local);
      const stoppedDistance=stoppedPlayer?Math.hypot(candidate.position[0]-stoppedPlayer.position[0],candidate.position[2]-stoppedPlayer.position[2]):Infinity;
      await platform.artifact(runId!,`supply-route-blocked-${actor}-${itemId}-${candidate.id.replace(/[^a-zA-Z0-9_-]/g,'_')}.json`,{candidate,error:String(error),code:(error as any)?.code,stoppedDistance,stopped});
      // The world item's serialized origin can sit behind a cart collider even
      // though its pickup volume is already in reach.  The live interaction
      // list is the authoritative gameplay test and supersedes origin distance.
      if(stopped.interactions?.some((entry:any)=>entry.interactionId==='static_pickup'&&entry.entityId===candidate.id)){
       target=candidate;break;
      }
      // Several serialized prop origins sit just outside the marked walkable
      // rectangle (for example the cart-side penlight and nasal cannula).  A
      // player stopped at that rectangle's boundary is already within the
      // intended pickup radius.  Do not drive through the boundary or discard
      // that exact entity merely because its origin itself is not walkable.
      if(stoppedDistance<=1.35){target=candidate;break;}
      await delay(200,undefined,{signal});
     }
    }
    if(!target)throw lastNavigationError??new Error(`STATIC_ITEM_UNREACHABLE:${actor}:${itemId}`);
    if(itemId==='nasal_cannula'&&target.id==='scene-item:overworld:nasal-cannula:1'){
     // The cart occludes this remaining cannula from its east face.  Use the
     // clear south face, outside bed B's capsule, before resolving the prompt.
     await runner.navigate(id,{id:`supply_stance_${actor}_${itemId}_south`,type:'navigate',actor,
      target:'nasal_cannula_1_south_stance',mode:'input_adapter',timeoutMs:20000,
      args:{targetType:'position',targetPosition:[-66.13,0,-13.45],arrivalRadius:.25,stuckWindowMs:8000}},signal);
    }
    let pickedInput=false,lastPickError:unknown;
    // Four concurrent Unity clients can need several presentation frames to
    // apply a long interaction-list selection.  Keep retrying the same exact
    // entity identity under the branch deadline rather than mistaking this
    // harmless UI race for an unavailable pickup.
    for(let attempt=0;attempt<24&&!pickedInput;attempt++)try{
     // Address the observed static entity directly.  Overlapping cart/bed
     // prompts can hide it from the single highlighted prompt even though the
     // input adapter can select that exact authoritative interaction.
     await runner.interact(id,{id:`supply_pickup_${actor}_${itemId}_${attempt}`,type:'interact',target:'static_pickup',args:{entityId:target.id},mode:'input_adapter'},signal);
     pickedInput=true;
    }catch(error){
     if(!['STATE_CONFLICT','TARGET_NOT_INTERACTABLE'].includes((error as any)?.code))throw error;
     lastPickError=error;await delay(200,undefined,{signal});
    }
    if(!pickedInput){
     const [tx,,tz]=target.position;
     const pickupFaces=[[tx,0,tz-.95],[tx+.95,0,tz],[tx,0,tz+.95],[tx-.95,0,tz]];
     for(const [faceIndex,targetPosition] of pickupFaces.entries()){
      try{
       await runner.navigate(id,{id:`supply_face_${actor}_${itemId}_${faceIndex}`,type:'navigate',actor,
        target:`${target.id}_face_${faceIndex}`,mode:'input_adapter',timeoutMs:12000,
        args:{targetType:'position',targetPosition,arrivalRadius:.35,stuckWindowMs:5000}},signal);
      }catch(error){if(!['NAVIGATION_STUCK','MOVEMENT_BLOCKED','DEADLINE_EXCEEDED'].includes((error as any)?.code))throw error;}
      const face=await platform.observe(id,false,{includeStaticItems:true,signal,ttlMs:5000});
      if(!face.interactions?.some((entry:any)=>entry.entityId===target.id&&entry.interactionId==='static_pickup'))continue;
      try{
       await runner.interact(id,{id:`supply_pickup_face_${actor}_${itemId}_${faceIndex}`,type:'interact',target:'static_pickup',args:{entityId:target.id},mode:'input_adapter'},signal);
       pickedInput=true;break;
      }catch(error){if(!['STATE_CONFLICT','TARGET_NOT_INTERACTABLE'].includes((error as any)?.code))throw error;lastPickError=error;}
     }
    }
    if(!pickedInput){
     // Preserve the exact live list and serialized candidates when a pickup
     // vanishes between navigation and selection.  This is required to choose
     // the next real StaticPlacedItem route rather than retrying a stale id.
     await platform.artifact(runId!,`supply-pickup-selection-failed-${actor}-${itemId}-${target.id.replace(/[^a-zA-Z0-9_-]/g,'_')}.json`,{
      target,candidates,lastPickError:String(lastPickError),
      snapshot:await platform.observe(id,false,{includeStaticItems:true,signal,ttlMs:5000})
     });
     throw lastPickError??new Error(`STATIC_ITEM_SELECT_FAILED:${actor}:${itemId}:${target.id}`);
    }
    const deadline=performance.now()+10000;
    let lastConfirmationRetry=performance.now();
    while(true){
     const after=await platform.observe(id,false,{includeStaticItems:true,signal,ttlMs:5000});
     if(count(after,itemId)>0){
      collectedStaticIds.add(target.id);
      await platform.artifact(runId!,`picked-${actor}-${itemId}.json`,{target,after});break;
     }
     if(performance.now()>deadline){
      collectedStaticIds.add(target.id);
      await platform.artifact(runId!,`supply-pickup-unconfirmed-${actor}-${itemId}-${target.id.replace(/[^a-zA-Z0-9_-]/g,'_')}.json`,{target,after});
      await collectStaticSupplies(actor,[itemId]);
      break;
     }
     if(performance.now()-lastConfirmationRetry>=1000&&after.interactions?.some((entry:any)=>entry.entityId===target.id&&entry.interactionId==='static_pickup')){
      try { await runner.interact(id,{id:`supply_pickup_confirm_${actor}_${itemId}`,type:'interact',target:'static_pickup',args:{entityId:target.id},mode:'input_adapter'},signal); }
      catch(error) { if(!['STATE_CONFLICT','TARGET_NOT_INTERACTABLE'].includes((error as any)?.code))throw error; }
      lastConfirmationRetry=performance.now();
     }
     await delay(150,undefined,{signal});
    }
   }
  };
  const completeTreatment=async(actor:CareActor,activeQuest:string,terminal:string,patient:'patient_b'|'patient_c',bed:'bed_b'|'bed_c',interactionIds:string[])=>{
   const id=actors[actor],signal=AbortSignal.timeout(350000);
   const selectableItems=interactionIds.includes('pupil_check')
    ?['penlight','cannula_20g','normal_saline_1000ml','intravenous_set']
    :['nasal_cannula','oxyflowmeter','humidifier_sterile_distilled_water_bottle','gauze','plaster'];
   let selectionIndex=0,interactionIndex=0,lastSelectionAt=-Infinity;
   if(actor==='p3'||actor==='p4'){
    // Return from the equipment shelf through the marked west lane.  A direct
    // vehicle offset crosses the partition or the neighbouring bed.
    const egressStart=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    const egressPlayer=egressStart.client.players.find((entry:any)=>entry.local);
    const egressBed=egressStart.vehicles?.find((entry:any)=>entry.id===bed);
    assert.ok(egressPlayer,'CARE_EGRESS_PLAYER_NOT_OBSERVED');
    const egress=bed==='bed_b'
     ?[[-68.3,0,-11],[-68.3,0,-14]]
     :egressPlayer.position[0]>-64
      ?[[-62.6,0,egressPlayer.position[2]],[-62.6,0,-14],[-68.3,0,-14],[-68.3,0,-17]]
      :egressPlayer.position[0]>-68
       ?[[-68.3,0,-17]]
      :[[egressPlayer.position[0],0,-14],[-68.3,0,-14],[-68.3,0,-17]];
    const alreadyAtBed=egressBed&&Math.hypot(egressPlayer.position[0]-egressBed.position[0],egressPlayer.position[2]-egressBed.position[2])<=2;
    if(!alreadyAtBed)for(const [index,targetPosition] of egress.entries())
     await runner.navigate(id,{id:`care_egress_${actor}_${bed}_${index}`,type:'navigate',actor,target:`care_egress_${actor}_${index}`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition,arrivalRadius:index===egress.length-1?0.25:0.6,stuckWindowMs:8000}},signal);
   }
   if(actor==='p3'){
    // The patient capsule occludes the bed from the west treatment stance.
    // Install the saline from the clear aisle end before approaching the
    // patient, then reserve intravenous_set for the patient-side connection.
    const bedStance=bed==='bed_b'?[-64,0,-14.9]:[-64,0,-15.55];
    try {
     await runner.navigate(id,{id:`saline_bed_stance_${bed}`,type:'navigate',actor,target:`${bed}_saline_stance`,mode:'input_adapter',timeoutMs:20000,args:{targetType:'position',targetPosition:bedStance,arrivalRadius:.3,stuckWindowMs:8000}},signal);
    } catch(error) {
     if(!['NAVIGATION_STUCK','MOVEMENT_BLOCKED'].includes((error as any)?.code))throw error;
    }
    const installDeadline=performance.now()+15000;
    let installed=false;
    while(performance.now()<installDeadline){
     const state=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     const local=state.client.players.find((entry:any)=>entry.local);
     const vehicle=state.vehicles?.find((entry:any)=>entry.id===bed);
     if(local&&vehicle&&local.rotationSensitivity>0){
      const yaw=Math.atan2(vehicle.position[0]-local.position[0],vehicle.position[2]-local.position[2])*180/Math.PI;
      const angle=((yaw-local.yaw+540)%360)-180;
      if(Math.abs(angle)>8){
       await platform.command(id,'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/local.rotationSensitivity,y:0}]},{signal,ttlMs:5000});
       await delay(150,undefined,{signal});continue;
      }
     }
     const slots=local?.inventory?.slots??[];
     const saline=slots.find((slot:any)=>slot.itemId==='normal_saline_1000ml');
     if(!saline){installed=true;break;}
     if(saline.slot>=0&&saline.slot<9)
      await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:`Alpha${saline.slot+1}`}]},{signal,ttlMs:5000});
     await delay(250,undefined,{signal});
     const equipped=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     const hang=equipped.interactions?.find((entry:any)=>entry.interactionId==='hang_normal_saline');
     if(hang)try{
      await runner.interact(id,{id:`hang_normal_saline_${bed}`,type:'interact',target:'hang_normal_saline',args:hang.entityId?{entityId:hang.entityId}:undefined,mode:'input_adapter'},signal);
     }catch(error){if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT'].includes((error as any)?.code))throw error;}
     await delay(250,undefined,{signal});
    }
    if(!installed){
     const state=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     await platform.artifact(runId!,`saline-install-failed-${bed}.json`,state);
     throw new Error(`NORMAL_SALINE_NOT_INSTALLED:${bed}`);
    }
   }
   let arrived=false,lastApproachError:unknown;
   for(let attempt=0;attempt<3&&!arrived;attempt++)try{
    // Bed geometry blocks the east-side approach.  Both treatment roles can
    // reach their distinct patient interactions from the clear west-side
    // lane, and no player collision is simulated by the input adapter.
    // Patient B is attached to bed_b and is absent from the refreshed
    // scenario-entity navigation catalogue.  The bed vehicle remains
    // authoritative; its zone_0 destination is the scene point
   // (-64, 0, -13.47), and the west-side live offset preserves the usable
   // treatment corridor after the move.
    // Keep both treatment roles on the clear west-side centerline. The former
    // north/south split stopped each controller at a bed corner, where the
    // patient prompt flickered across the authoritative reach boundary.
    // Player colliders do not block one another in this input-adapter route.
    const treatmentOffset=[-.7,0,0];
   await runner.navigate(id,{id:`care_approach_${actor}_${patient}_${attempt}`,type:'navigate',actor,target:bed,mode:'input_adapter',timeoutMs:30000,args:{targetType:'vehicle',arrivalRadius:.3,targetOffset:treatmentOffset}},signal);
    arrived=true;
   }catch(error){
    if(!['TARGET_NOT_FOUND','NAVIGATION_STUCK'].includes((error as any)?.code))throw error;
    lastApproachError=error;
    const stopped=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    if(stopped.interactions?.some((entry:any)=>interactionIds.includes(entry.interactionId)
      && (entry.entityId===patient||entry.interactionId==='oxyflowmeter'))){arrived=true;break;}
    await delay(250,undefined,{signal});
   }
   if(!arrived)throw lastApproachError??new Error(`PATIENT_APPROACH_FAILED:${actor}`);
   let activated=false;
   while(true){
    signal.throwIfAborted();
    const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(snapshot.localQuests?.some((quest:any)=>quest.definitionId===activeQuest&&!quest.placeholder))activated=true;
    if(activated&&snapshot.localQuests?.some((quest:any)=>quest.definitionId===terminal&&!quest.placeholder)){
     await platform.artifact(runId!,`care-completed-${actor}-${activeQuest}.json`,snapshot);return snapshot;
    }
    if(snapshot.inputContext==='DialoguePanelUIController'){
     assert.equal(snapshot.dialogue.graphId,graph,'UNEXPECTED_TREATMENT_DIALOGUE_GRAPH');
     assert.equal(snapshot.dialogue.hasChoices,false,'UNEXPECTED_TREATMENT_DIALOGUE_CHOICE');
     if(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
      try {
       await runner.step({actors} as any,{id:`care_advance_${actor}`,type:'dialogueAdvance',actor},signal);
      } catch(error) {
       if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT'].includes((error as any)?.code))throw error;
      }
    } else {
     // Treatment surfaces remain visible even when the currently equipped
     // item cannot satisfy them.  Rotate the authored supplies independently
     // of visibility, then use the settled slot between rotations.
     if(performance.now()-lastSelectionAt>=600){
      const slots=snapshot.client.players.find((entry:any)=>entry.local)?.inventory?.slots??[];
      const available=selectableItems.map(itemId=>slots.find((slot:any)=>slot.itemId===itemId)).filter(Boolean);
      if(available.length){
       const slot=available[selectionIndex++%available.length].slot;
       if(slot>=0&&slot<9)await platform.command(id,'input.execute',{sequence:[{operation:'tap',key:`Alpha${slot+1}`}]},{signal,ttlMs:5000});
      }
      lastSelectionAt=performance.now();
      await delay(150,undefined,{signal});continue;
     }
     const eligibleInteractions=(snapshot.interactions??[]).filter((entry:any)=>interactionIds.includes(entry.interactionId)
       && (entry.entityId===patient||entry.interactionId==='oxyflowmeter'
         ||entry.interactionId==='hang_normal_saline'));
     eligibleInteractions.sort((left:any,right:any)=>interactionIds.indexOf(left.interactionId)-interactionIds.indexOf(right.interactionId));
     const interaction=eligibleInteractions.length?eligibleInteractions[interactionIndex++%eligibleInteractions.length]:undefined;
     if(interaction) try {
      if(interaction.selected){
       const key=snapshot.inputBindings?.interact;
       assert.ok(key&&key!=='None','INTERACT_BINDING_UNAVAILABLE');
       await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
      }else await runner.interact(id,{id:`care_${actor}_${interaction.interactionId}`,type:'interact',target:interaction.interactionId,args:{entityId:interaction.entityId},mode:'input_adapter'},signal);
     } catch(error) {
      if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT'].includes((error as any)?.code))throw error;
     }
    }
    await delay(150,undefined,{signal});
   }
  };
  const craftInventoryItem=async(actor:CareActor,resultItemId:string)=>{
   const id=actors[actor],signal=AbortSignal.timeout(30000);
   const click=async(automationId:string)=>{
    const target:any=await waitForInteractable(platform,id,automationId,signal);
    const pointer={...target.screenCenter,screenWidth:target.screenWidth,screenHeight:target.screenHeight,frame:target.uiRevision};
    await platform.command(id,'ui.pointer',{...pointer,pressed:true},{signal,ttlMs:5000});
    await platform.command(id,'ui.pointer',{...pointer,pressed:false},{signal,ttlMs:5000});
   };
   let state=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
   if(state.inputContext!=='InventoryUIController'){
    const key=state.inputBindings?.inventory;
    assert.ok(key&&key!=='None','INVENTORY_BINDING_UNAVAILABLE');
    await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
    const deadline=performance.now()+10000;
    do {
     state=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     if(state.inputContext==='InventoryUIController')break;
     if(performance.now()>deadline)throw new Error('INVENTORY_DID_NOT_OPEN_FOR_CRAFT');
     await delay(100,undefined,{signal});
    }while(true);
   }
   await click(`Recipe_${resultItemId}`);
   await click(`Recipe_${resultItemId}`);
   await delay(150,undefined,{signal});
   state=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
   const used=new Set((state.client.players.find((player:any)=>player.local)?.inventory?.slots??[]).map((slot:any)=>slot.slot));
   const emptyCandidates=Array.from({length:36},(_,index)=>index).filter(index=>!used.has(index));
   assert.ok(emptyCandidates.length>0,'EMPTY_INVENTORY_SLOT_NOT_FOUND');
   let empty:number|undefined;
   for(const index of emptyCandidates){
    const query=await platform.command(id,'ui.query',{automationId:`Slot_${index}`},{signal,ttlMs:5000});
    if(query.elements.length===1&&query.elements[0].interactable){empty=index;break;}
   }
   empty??=emptyCandidates.at(-1);
   await click(`Slot_${empty}`);
   const deadline=performance.now()+10000;
   while(true){
    state=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    if(state.client.players.find((player:any)=>player.local)?.inventory?.slots?.some((slot:any)=>slot.itemId===resultItemId))break;
    if(performance.now()>deadline)throw new Error(`CRAFT_RESULT_NOT_STORED:${resultItemId}`);
    await delay(100,undefined,{signal});
   }
   await platform.artifact(runId!,`crafted-${actor}-${resultItemId}.json`,state);
  };
  phase='patient_b_c_parallel_initial_assessment';
  const [bStrength,cStrength,bVital,cVital]=await withHardDeadline(Promise.all([
   finishDialogue('p1','Quest_B_Wait',{id:'recognition_check',entity:'patient_b'}),
   finishDialogue('p2','Quest_C_Wait',{id:'recognition_1',entity:'patient_c'}),
   finishDialogue('p3','Quest_B_Wait'),
   finishDialogue('p4','Quest_C_Wait')
  ]),185000,'patient_b_c_parallel_initial_assessment');
  await platform.artifact(runId!,'b-care-strength-p1.json',bStrength);
  await platform.artifact(runId!,'c-care-strength-p2.json',cStrength);
  await platform.artifact(runId!,'b-care-vital-p3.json',bVital);
  await platform.artifact(runId!,'c-care-vital-p4.json',cVital);

  const advanceToTreatment=async(actor:CareActor,definitionId:string)=>{
   const id=actors[actor],signal=AbortSignal.timeout(30000);
   while(true){
    const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    if(snapshot.localQuests?.some((quest:any)=>quest.definitionId===definitionId&&!quest.placeholder))return snapshot;
    if(snapshot.inputContext==='DialoguePanelUIController'&&!/(?:^|_)DOC_[A-D]$/.test(snapshot.dialogue.nodeId??'')
      &&(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)){
     try { await runner.step({actors} as any,{id:`treatment_order_${actor}`,type:'dialogueAdvance',actor},signal); }
     catch(error) { if(!['TARGET_NOT_INTERACTABLE','STATE_CONFLICT','INPUT_BUSY'].includes((error as any)?.code))throw error; }
    }
    await delay(150,undefined,{signal});
   }
  };
  const prepareOxygen=async(actor:CareActor)=>{
   await collectStaticSupplies(actor,['humidifier_bottle','sterile_distilled_water','flowmeter','nasal_cannula','gauze','plaster']);
   await craftInventoryItem(actor,'humidifier_sterile_distilled_water_bottle');
   await craftInventoryItem(actor,'oxyflowmeter');
   const state=await platform.observe(actors[actor],false,{includeStaticItems:false});
   assert.equal(state.inputContext,'InventoryUIController','INVENTORY_CLOSED_BEFORE_CRAFT_COMPLETED');
   const inventoryKey=state.inputBindings?.inventory;
   assert.ok(inventoryKey&&inventoryKey!=='None','INVENTORY_BINDING_UNAVAILABLE');
   await platform.command(actors[actor],'input.execute',{sequence:[{operation:'tap',key:inventoryKey}]});
  };
  phase='patient_b_c_parallel_treatment';
  const doctorPumpAbort=new AbortController();
  // Dialogue presentation state is per client.  Although the text names one
  // role, every connected client must acknowledge its own copy before its
  // local quest projection can move on.
  const doctorNodes=new Set(['DOC_C','DOC_D','C_DOC_C','C_DOC_D']);
  const doctorPump=Promise.all((Object.keys(actors) as CareActor[]).map(async actor=>{
   const id=actors[actor];let lastAdvanceAt=-Infinity;
   while(!doctorPumpAbort.signal.aborted){
    const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal:doctorPumpAbort.signal,ttlMs:5000});
    if(snapshot.inputContext==='DialoguePanelUIController'&&doctorNodes.has(snapshot.dialogue.nodeId??'')
      &&(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)&&performance.now()-lastAdvanceAt>=350){
     const key=snapshot.inputBindings?.dialogueAdvance;
     if(key){
      try { await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal:doctorPumpAbort.signal,ttlMs:5000}); }
      catch(error) { if(!['INPUT_BUSY','STATE_CONFLICT','INSTANCE_UNAVAILABLE'].includes((error as any)?.code))throw error; }
     }
     lastAdvanceAt=performance.now();
    }
    await delay(200,undefined,{signal:doctorPumpAbort.signal});
   }
  }));
  const treatmentResult=await withHardDeadline(Promise.all([
   (async()=>{
    await advanceToTreatment('p3','Quest_B_Pupil_IV');
    await collectStaticSupplies('p3',['penlight','cannula_20g','intravenous_set','normal_saline_1000ml']);
    const b=await completeTreatment('p3','Quest_B_Pupil_IV','Quest_B_Wait','patient_b','bed_b',['pupil_check','intravenous_line_cannula','hang_normal_saline','normal_saline_connect']);
    await advanceToTreatment('p3','Quest_C_Pupil_IV');
    await collectStaticSupplies('p3',['penlight','cannula_20g','intravenous_set','normal_saline_1000ml']);
    const c=await completeTreatment('p3','Quest_C_Pupil_IV','Quest_C_Wait','patient_c','bed_c',['pupil_check','intravenous_line_cannula','hang_normal_saline','normal_saline_connect']);
    return [b,c];
   })(),
   (async()=>{
    await advanceToTreatment('p4','Quest_B_Oxygen_Bleeding');
    await prepareOxygen('p4');
    const b=await completeTreatment('p4','Quest_B_Oxygen_Bleeding','Quest_B_Wait','patient_b','bed_b',['patient_bc_nasal_cannula','oxyflowmeter','item_apply']);
    await advanceToTreatment('p4','Quest_C_Oxygen_Bleeding');
    await prepareOxygen('p4');
    const c=await completeTreatment('p4','Quest_C_Oxygen_Bleeding','Quest_C_Wait','patient_c','bed_c',['patient_bc_nasal_cannula','oxyflowmeter','item_apply']);
    return [b,c];
   })()
  ]),600000,'patient_b_c_parallel_treatment');
  doctorPumpAbort.abort();await Promise.allSettled([doctorPump]);
  const [[bPupilIv,cPupilIv],[bOxygenBleeding,cOxygenBleeding]]=treatmentResult;
  await platform.artifact(runId!,'b-care-pupil-iv-p3.json',bPupilIv);
  await platform.artifact(runId!,'c-care-pupil-iv-p3.json',cPupilIv);
  await platform.artifact(runId!,'b-care-oxygen-bleeding-p4.json',bOxygenBleeding);
  await platform.artifact(runId!,'c-care-oxygen-bleeding-p4.json',cOxygenBleeding);

  phase='ct_transport_quest';
  await Promise.all((Object.keys(actors) as CareActor[]).map(async actor=>{
   const id=actors[actor],signal=AbortSignal.timeout(180000);let lastAdvanceAt=-Infinity;
   while(true){
    signal.throwIfAborted();
    const snapshot=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(snapshot.inputContext==='Gameplay'&&snapshot.localQuests?.some((quest:any)=>quest.definitionId==='Quest_Transport_BC_To_CT'&&!quest.placeholder))return;
    if(snapshot.inputContext==='DialoguePanelUIController'
      &&(snapshot.dialogue.canAdvance||snapshot.dialogue.isTextAnimating)
      &&performance.now()-lastAdvanceAt>=350){
     const key=snapshot.inputBindings?.dialogueAdvance;
     assert.ok(key,`CT_DIALOGUE_ADVANCE_BINDING_MISSING:${actor}`);
     await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
     lastAdvanceAt=performance.now();
    }
    await delay(200,undefined,{signal});
   }
  }));

  const ctActors=Object.entries(actors) as ['p1'|'p2'|'p3'|'p4',string][];
  const boardCtBed=async(bed:'bed_b'|'bed_c')=>{
   const signal=AbortSignal.timeout(90000);
   const z=bed==='bed_b'?-13.47:-17;
   const stances=bed==='bed_b'
    ?[[-64.3,0,z-.85],[-64.1,0,z-.85],[-63.9,0,z-.85],[-63.7,0,z-.85]]
    // Keep every participant inside the observed interaction radius.  The old
    // outer slots (-64.75/-63.25) could render the bed yet omit move_bed.
    :[[-64.25,0,-15.9],[-64.08,0,-15.9],[-63.91,0,-15.9],[-63.74,0,-15.9]];
   const selectMoveBed=async(actor:string,id:string)=>{
    for(let attempt=0;attempt<36;attempt++){
     const current=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
     if(current.vehicles?.some((entry:any)=>entry.id===bed&&entry.locallyControlled))return null;
     const target=current.interactions?.find((entry:any)=>entry.entityId===bed&&entry.interactionId==='move_bed');
     const selected=current.interactions?.find((entry:any)=>entry.selected);
     if(target&&selected?.entityId===bed&&selected.interactionId==='move_bed')return current.inputBindings.interact;
     if(target){
      const key=!selected||target.index>selected.index?'Equals':'Minus';
      await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
     }else{
     const player=current.client.players.find((entry:any)=>entry.local);
     const vehicle=current.vehicles?.find((entry:any)=>entry.id===bed);
     if(!player||!vehicle)throw new Error(`CT_BED_NOT_OBSERVED:${bed}:${actor}`);
      const dx=player.position[0]-vehicle.position[0],dz=player.position[2]-vehicle.position[2];
      const distance=Math.hypot(dx,dz);
      if(distance>1.22){
       const scale=1.12/distance;
       try{
        await runner.navigate(id,{id:`ct_close_${bed}_${actor}_${attempt}`,type:'navigate',actor,target:`${bed}_interaction_radius`,mode:'input_adapter',timeoutMs:5000,args:{targetType:'position',targetPosition:[vehicle.position[0]+dx*scale,0,vehicle.position[2]+dz*scale],arrivalRadius:.16,stuckWindowMs:2500}},signal);
       }catch(error){if(!['NAVIGATION_STUCK','MOVEMENT_BLOCKED'].includes((error as any)?.code))throw error;}
      }
      const targetYaw=Math.atan2(vehicle.position[0]-player.position[0],vehicle.position[2]-player.position[2])*180/Math.PI;
      const angle=((targetYaw-player.yaw+540)%360)-180;
      await platform.command(id,'input.execute',{sequence:[{operation:'lookDelta',x:Math.max(-30,Math.min(30,angle))/player.rotationSensitivity,y:0}]},{signal,ttlMs:5000});
     }
     await delay(120,undefined,{signal});
    }
    throw new Error(`CT_BED_INTERACTION_NOT_SELECTED:${bed}:${actor}`);
   };
   // Approach from the north row.  The treatment finish positions are on
   // both sides of bed C, so a direct segment to bed B would cross a patient
   // capsule.  Keep each player on the observed east/west aisle first.
   for(const [index,[actor,id]] of ctActors.entries()){
    const current=await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000});
    const player=current.client.players.find((entry:any)=>entry.local);
    assert.ok(player,`CT_LOCAL_PLAYER_NOT_OBSERVED:${actor}`);
    const cLane=index===0||index===2?-65.3:-62.6;
    const preRoute=bed==='bed_b'
     ?[[player.position[0]<-64?-65.35:-62.65,0,player.position[2]],[player.position[0]<-64?-65.35:-62.65,0,z-1.32]]
     :[[-68.3,0,z+1.53],[cLane,0,z+1.53]];
    for(const [routeIndex,targetPosition] of preRoute.entries())
     await runner.navigate(id,{id:`ct_stage_route_${bed}_${actor}_${routeIndex}`,type:'navigate',actor,target:`${bed}_ct_route_${routeIndex}`,mode:'input_adapter',timeoutMs:25000,args:{targetType:'position',targetPosition,arrivalRadius:.4,stuckWindowMs:9000}},signal);
    try{
     await runner.navigate(id,{id:`ct_stage_${bed}_${actor}`,type:'navigate',actor,target:`${bed}_ct_stance`,mode:'input_adapter',timeoutMs:25000,args:{targetType:'position',targetPosition:stances[index],arrivalRadius:bed==='bed_c'?.3:.4,stuckWindowMs:9000}},signal);
    }catch(error){if(bed!=='bed_c'||!['NAVIGATION_STUCK','MOVEMENT_BLOCKED'].includes((error as any)?.code))throw error;}
    if(bed==='bed_b'){
     const key=await selectMoveBed(actor,id);
     if(key)await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
     const joinDeadline=performance.now()+8000;
     while(!(await platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000})).vehicles?.some((vehicle:any)=>vehicle.id===bed&&vehicle.locallyControlled)){
      if(performance.now()>joinDeadline)throw new Error(`CT_BED_CONTROL_NOT_ACQUIRED:${bed}:${actor}`);
      await delay(120,undefined,{signal});
     }
    }
   }
   if(bed==='bed_c'){
    const keys=await Promise.all(ctActors.map(([actor,id])=>selectMoveBed(actor,id)));
    await Promise.all(ctActors.map(([,id],index)=>platform.command(id,'input.execute',{sequence:[{operation:'tap',key:keys[index]??'F'}]},{signal,ttlMs:5000})));
   }
   const deadline=performance.now()+45000;let lastRepairAt=-Infinity;
   while(true){
    const observations=await Promise.all(ctActors.map(([,id])=>platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000})));
    const controlled=observations.flatMap((snapshot,index)=>snapshot.vehicles?.some((vehicle:any)=>vehicle.id===bed&&vehicle.locallyControlled)?[ctActors[index][1]]:[]);
    if(controlled.length===ctActors.length){
     // A simultaneous four-client join can be visible for one projection
     // frame before the authoritative participant set settles.  Steering in
     // that frame can move the bed and immediately drop every participant.
     await delay(1000,undefined,{signal});
     const settled=await Promise.all(ctActors.map(([,id])=>platform.observe(id,false,{includeStaticItems:false,signal,ttlMs:5000})));
     if(settled.every(snapshot=>snapshot.vehicles?.some((vehicle:any)=>vehicle.id===bed&&vehicle.locallyControlled))){
      await platform.artifact(runId!,`ct-boarded-${bed}.json`,settled);return controlled;
     }
    }
    if(performance.now()-lastRepairAt>=1200){
     for(const [index,[actor,id]] of ctActors.entries()){
      if(observations[index].vehicles?.some((vehicle:any)=>vehicle.id===bed&&vehicle.locallyControlled))continue;
      try{
       const key=await selectMoveBed(actor,id);
       if(key)await platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal,ttlMs:5000});
      }catch(error){if(!String(error).includes('CT_BED_INTERACTION_NOT_SELECTED'))throw error;}
     }
     lastRepairAt=performance.now();
    }
    if(performance.now()>deadline)throw new Error(`CT_BED_CONTROL_NOT_ACQUIRED:${bed}`);
    await delay(150,undefined,{signal});
   }
  };
  const ctRouteB=[[-68.125,0,-15.31],[-68.7125,0,-13.87],[-76.5175,0,-13.87],[-77.2375,0,-21.76],[-80.1175,0,-21.76]];
  // Bed C starts in the L4 care rectangle.  Make the two turns through the
  // vertical/cross-corridor overlaps explicitly instead of cutting across the
  // wall corner with bed B's diagonal entry segment.
  const ctRouteC=[[-68.2,0,-15.5],[-68.2,0,-13.87],[-68.7,0,-13.87],[-76.5175,0,-13.87],[-77.2375,0,-21.76],[-80.1175,0,-21.76]];
  const ctTarget=[-80,0,-18.3];
  for(const [bedIndex,bed] of ['bed_b','bed_c'].entries() as ArrayIterator<[number,'bed_b'|'bed_c']>){
   phase=`ct_transport_${bed}`;
   const controllers=await boardCtBed(bed);
   const signalId=`sig.ct_patient_arrived_${bed==='bed_b'?'patient_b':'patient_c'}`;
   try{
    await drivePatientBed(platform,controllers[0],bed,'ct:patient_target_pos_b',AbortSignal.timeout(180000),180000,controllers,bed==='bed_b'?ctRouteB:ctRouteC,250,ctTarget);
   }catch(error){
    // The CT trigger detaches the bed and its participants as part of a valid
    // arrival.  That can race the driver's final position observation.
    if((error as any)?.code!=='VEHICLE_NOT_CONTROLLED')throw error;
    const arrival=await platform.observe(actors.p1,false,{includeStaticItems:false,ttlMs:5000});
    if(!arrival.signalParameters?.some((entry:any)=>entry.SignalIdentifier===signalId))throw error;
   }
   const deadline=performance.now()+15000;
   while(true){
    const snapshot=await platform.observe(actors.p1,false,{includeStaticItems:false,ttlMs:5000});
    assert.deepEqual(snapshot.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
    if(snapshot.signalParameters?.some((entry:any)=>entry.SignalIdentifier===signalId)){
     await platform.artifact(runId!,`ct-arrived-${bed}.json`,snapshot);break;
    }
    if(performance.now()>deadline)throw new Error(`CT_ARRIVAL_SIGNAL_MISSING:${bed}`);
    await delay(200);
   }
   if(bedIndex===0){
    // A tap first arms the normal release edge; the second tap dismounts.
    await Promise.all(ctActors.map(([,id])=>platform.command(id,'input.execute',{sequence:[{operation:'tap',key:'LeftShift'},{operation:'tap',key:'LeftShift',delayMs:150}]},{ttlMs:5000})));
    await delay(500);
    const returnRoute=[[-80.12,0,-21.76],[-77.24,0,-21.76],[-76.52,0,-13.87],[-68.71,0,-13.87]];
    // The CT doorway is a single narrow shared resource.  Sending all four
    // capsules through together can make the inner pair deadlock each other.
    for(const [index,[actor,id]] of ctActors.entries()){
     const lane=(index-1.5)*.22;
     for(const [routeIndex,point] of returnRoute.entries()){
      const target=[point[0]+(routeIndex===2||routeIndex===3?0:lane),0,point[2]+(routeIndex===2||routeIndex===3?lane:0)];
      await runner.navigate(id,{id:`ct_return_${actor}_${routeIndex}`,type:'navigate',actor,target:`ct_return_${routeIndex}`,mode:'input_adapter',timeoutMs:30000,args:{targetType:'position',targetPosition:target,arrivalRadius:.45,stuckWindowMs:10000}},AbortSignal.timeout(120000));
     }
    }
   }
  }
  phase='ct_scenario_completion';
  const completionDeadline=performance.now()+30000;
  let completed:any;
  while(true){
   completed=await platform.observe(actors.p1,false,{includeStaticItems:false,ttlMs:5000});
   assert.deepEqual(completed.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
   const signals=new Set((completed.signalParameters??[]).map((entry:any)=>entry.SignalIdentifier));
   if(signals.has('sig.ct_patient_arrived_patient_b')&&signals.has('sig.ct_patient_arrived_patient_c')
     &&(!completed.localQuests?.some((quest:any)=>quest.scenarioId===graph&&!quest.placeholder)))break;
   if(performance.now()>completionDeadline)throw new Error('CT_SCENARIO_DID_NOT_COMPLETE');
   await delay(250);
  }
  await platform.artifact(runId!,'full-play-completed.json',completed);
  await platform.artifact(runId!,'route-manifest.json',{graph,profile,stage:'b-care',fullPlayPassed:true,
   promotion:'Four-player assessment, treatment, and CT transport completed without scenario recovery.',
   sources:sourceHashes.flatMap(result=>result.status==='fulfilled'?[result.value]:[]),
   execution:'Fixed role commands and accessibility-checked routes with observed completion gates; independent care branches execute concurrently.'});
 }
 const host=await platform.observe(actors.p1);
 assert.equal(host.scenario.graphId,graph);
 assert.ok(host.staticPlacedItems.length>0);
 await platform.artifact(runId,'entry-events.json',await platform.eventHistory(actors.p1));
 console.log(JSON.stringify({runId,graph,entryObserved:true,fullPlayPassed:process.argv[4]==='b-care',staticItemCount:host.staticPlacedItems.length}));
}catch(e){
 error=String(e);
 errorStack=e instanceof Error?e.stack:undefined;
 if(runId){
  const cutoff=Date.now()-30000;
  const recentControlFailures=platform.audit.filter((entry:any)=>entry.command?.type==='control.heartbeat'&&entry.error&&Date.parse(entry.at)>=cutoff)
   .slice(-24).map((entry:any)=>({at:entry.at,instanceId:entry.instanceId,error:entry.error,elapsedMs:entry.elapsedMs}));
  await platform.artifact(runId,'entry-error.json',{error,errorStack,errorCode:(e as any)?.code,phase,recentControlFailures});
  await Promise.allSettled([...platform.instances.values()].map(async i=>{
   await platform.release(i.id).catch(()=>{});
   await platform.artifact(runId!,`failure-${i.id}.json`,await platform.command(i.id,'game.observe',{}, {releaseRead:true}));
  }));
 }
}
finally{
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
