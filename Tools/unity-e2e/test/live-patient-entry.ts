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
const graph=process.argv[3]??'patient_a_critical';
assert.ok(['patient_a_critical','patient_b_c_ct'].includes(graph));
const profile=process.argv[5]??'mac_direct';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,error:unknown,cleanupError:unknown,phase='launch';
const memoryPressure=monitorMemoryPressure(performance.now());
const eventLoop=monitorEventLoopDelay({resolution:20});eventLoop.enable();
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
  './live-patient-entry.ts','../src/runner.ts','../src/vehicle-navigation.ts'];
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
 if(['arrival','triage','move','recognition'].includes(process.argv[4])){
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
