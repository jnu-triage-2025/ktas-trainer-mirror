import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {API} from '../src/api.ts';
import {joinInstances} from '../src/startup.ts';
const config=await loadConfig(process.argv[2]??'config.json');
config.fixture={...config.fixture,allowScenarioFixtures:true,allowProtocolTests:true};
const platform=new Platform(config),api=new API(platform),evidence:Record<string,any>={};
let runId:string|undefined,state='failed',failure:string|undefined;
const cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','host_plus_3_clients',true);runId=launch.runId;
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const server=actors.p1,ownerClient=actors.p2,sender=actors.p3;
 const issued=api.credentials.issue(runId,['observe','control','fixture','protocol'],120000);
 const grant=api.credentials.authenticate('Bearer '+issued.token)!;
 const owner=(await platform.observe(ownerClient)).client.players.find((p:any)=>p.local).ownerId;
 const start=()=>api.callScoped(grant,'fixture.scenario_start',{instanceId:server,payload:{graphId:'e2e_signal_protocol',ownerId:owner}},'Automation');
 async function waitFor(id:string,predicate:(value:any)=>boolean) {
  const deadline=performance.now()+10000;
  while(true){const value=await platform.observe(id);if(predicate(value))return value;if(performance.now()>deadline)throw new Error('STATE_WAIT_TIMEOUT');await delay(50);}
 }
 await platform.acquire(server);await start();
 await platform.acquire(ownerClient);await platform.acquire(sender);
 const ready=await waitFor(ownerClient,value=>value.dialogue.canAdvance===true&&!value.dialogue.isTextAnimating);
 const previous=await platform.observe(server);
 assert.equal(previous.scenario.graphId,'e2e_signal_protocol');assert.ok(previous.scenario.executionId);
 evidence.previous=previous;evidence.ownerClient=ownerClient;evidence.sender=sender;
 const signal='sig.e2e.protocol.prefix.delayed_execution';
 // This marker is evidence only: it is not an execution-generation field in the game RPC.
 const parameterJson=JSON.stringify({originExecution:previous.scenario.executionId});
 await api.callScoped(grant,'network.fault',{instanceId:sender,durationMs:2500,rule:{delayMs:1000,jitterMs:0,loss:0,disconnected:false,bytesPerSecond:0}},'Automation');
 evidence.sent=await api.callScoped(grant,'protocol.signal_raise',{instanceId:sender,payload:{signalId:signal,parameterJson}},'Automation');
 const queueDeadline=performance.now()+500;
 while(platform.networkStatus(sender).queuedPackets===0&&performance.now()<queueDeadline)await delay(10);
 evidence.networkAfterSend=platform.networkStatus(sender);
 assert.ok(evidence.networkAfterSend.queuedPackets>0,'No game datagrams were queued for delayed delivery');
 // Finish the first execution through its real dialogue input, while the other client's UDP is delayed.
 await platform.command(ownerClient,'input.execute',{expectedPresentationRevision:ready.dialogue.presentationRevision,sequence:[{operation:'tap',key:'KeypadEnter'}]});
 await waitFor(server,value=>value.scenario.state!=='ExecutingDialogue');
 await start();
 const restarted=await waitFor(server,value=>value.scenario.graphId==='e2e_signal_protocol'&&value.scenario.executionId!==previous.scenario.executionId);
 evidence.restarted=restarted;evidence.networkAtRestart=platform.networkStatus(sender);
 assert.ok(!restarted.signalParameters.some((value:any)=>value.SignalIdentifier===signal),'Delayed signal arrived before the new execution was observed; probe is inconclusive');
 const deadline=performance.now()+5000;
 let after=restarted;
 do {await delay(100);after=await platform.observe(server);}while(!after.signalParameters.some((value:any)=>value.SignalIdentifier===signal)&&performance.now()<deadline);
 evidence.after=after;evidence.networkAfter=platform.networkStatus(sender);
 const stale=after.signalParameters.find((value:any)=>value.SignalIdentifier===signal&&value.ParameterJson===parameterJson);
 evidence.findings=stale?[{code:'STALE_EXECUTION_SIGNAL_ACCEPTED',previousExecutionId:previous.scenario.executionId,currentExecutionId:after.scenario.executionId,record:stale}]:[];
 await platform.artifact(runId,'signal-execution-evidence.json',evidence);
 assert.equal(after.server.connections,4);
 assert.equal(stale,undefined,'STALE_EXECUTION_SIGNAL_ACCEPTED');
 const events=await platform.command(server,'events.read',{cursor:previous.eventCursor});
 evidence.events=events.events;
 assert.ok(events.events.some((event:any)=>event.eventType==='log'&&event.payload.message?.includes(`Rejected stale client signal raise: signal="${signal}"`)), 'EXPLICIT_STALE_REJECTION_EVIDENCE_REQUIRED');
 assert.equal(after.scenario.executionId,restarted.scenario.executionId);
 const currentSignal='sig.e2e.protocol.prefix.current_execution';
 await api.callScoped(grant,'protocol.signal_raise',{instanceId:sender,payload:{signalId:currentSignal}},'Automation');
 evidence.currentAccepted=await waitFor(server,value=>value.signalParameters.some((record:any)=>record.SignalIdentifier===currentSignal));
 await platform.artifact(runId,'signal-execution-evidence.json',evidence);
 api.credentials.revoke(issued.id);state='passed';
} catch(error) {
 failure=String(error);
 if(runId)await platform.artifact(runId,'signal-execution-evidence.json',evidence);
} finally {
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();if(processes.some(i=>!['EXITED','START_FAILED'].includes(i.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,state,failure,cleanupErrors,processes};
 if(runId)await platform.artifact(runId,'signal-execution-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
