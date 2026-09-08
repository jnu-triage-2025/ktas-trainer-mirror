import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {API} from '../src/api.ts';
import {joinInstances} from '../src/startup.ts';
import {activateVisible} from '../src/ui-navigation.ts';
const config=await loadConfig(process.argv[2]??'config.json');
config.fixture={...config.fixture,allowScenarioFixtures:true,allowProtocolTests:true};
const platform=new Platform(config),api=new API(platform),evidence:Record<string,any>={};
let runId:string|undefined,state='failed',failure:string|undefined;
const cleanupErrors:string[]=[];
async function waitFor(id:string,predicate:(value:any)=>boolean,timeout=15000) {
 const deadline=performance.now()+timeout;
 while(true){const value=await platform.observe(id);if(predicate(value))return value;if(performance.now()>deadline)throw new Error('LATE_SIGNAL_STATE_TIMEOUT');await delay(100);}
}
try {
 const launch=await platform.launch('mac_direct','host_plus_3_clients');runId=launch.runId;
 const {actors,deferredActors}=await joinInstances(platform,runId,AbortSignal.timeout(180000),['p4']);
 const host=actors.p1,existing=actors.p2,late=deferredActors.p4;
 const issued=api.credentials.issue(runId,['observe','control','fixture','protocol'],120000);
 const grant=api.credentials.authenticate('Bearer '+issued.token)!;
 const send=(id:string,signalId:string,parameterJson:string)=>api.callScoped(grant,'protocol.signal_raise',{instanceId:id,payload:{signalId,parameterJson}},'Automation');
 const owner=(await platform.observe(existing)).client.players.find((value:any)=>value.local).ownerId;
 await platform.acquire(host);
 await api.callScoped(grant,'fixture.scenario_start',{instanceId:host,payload:{graphId:'e2e_signal_protocol',ownerId:owner}},'Automation');
 await waitFor(host,value=>value.scenario.graphId==='e2e_signal_protocol');
 await platform.releaseControl(host);
 await platform.acquire(existing);
 const originalSignal='sig.e2e.protocol.prefix.before_late_join',lateSignal='sig.e2e.protocol.prefix.from_late_join';
 await send(existing,originalSignal,'{"phase":"before_join"}');
 const before=await waitFor(host,value=>value.signalParameters.some((record:any)=>record.SignalIdentifier===originalSignal));
 evidence.before=before;assert.equal(before.server.connections,3);assert.ok(before.scenario.executionId);
 const original=before.signalParameters.find((record:any)=>record.SignalIdentifier===originalSignal);
 await platform.releaseControl(existing);
 await platform.acquire(late);
 assert.equal((await platform.observe(late)).scene,'IntroScene');
 await assert.rejects(send(late,lateSignal,'{}'),(error:any)=>error.code==='CLIENT_NOT_CONNECTED');
 for(const button of ['btnPlay','btnDirectConnect','btnDirectJoin'])await activateVisible(platform,late,button,AbortSignal.timeout(15000));
 const joined=await waitFor(late,value=>value.client.localPlayerReady&&value.client.players.length===4&&value.signalParameters.some((record:any)=>record.SignalIdentifier===originalSignal),90000);
 evidence.joined=joined;
 assert.deepEqual(joined.signalParameters.find((record:any)=>record.SignalIdentifier===originalSignal),original,'Late join snapshot differs from authoritative signal parameter');
 const latePlayer=joined.client.players.find((value:any)=>value.local);assert.ok(latePlayer.userIdentifier);
 await send(late,lateSignal,'{"phase":"after_join"}');
 const accepted=await waitFor(host,value=>value.signalParameters.some((record:any)=>record.SignalIdentifier===lateSignal));
 const lateRecord=accepted.signalParameters.find((record:any)=>record.SignalIdentifier===lateSignal);
 assert.equal(lateRecord.PlayerIdentifier,latePlayer.userIdentifier);assert.equal(lateRecord.ParameterJson,'{"phase":"after_join"}');
 assert.equal(accepted.scenario.executionId,before.scenario.executionId,'Joining unexpectedly restarted the running scenario');
 assert.equal(accepted.server.connections,4);evidence.accepted=accepted;
 evidence.replicas={};
 for(const [actor,id] of Object.entries({...actors,...deferredActors})){
  const replica=await waitFor(id,value=>value.signalParameters.some((record:any)=>record.SignalIdentifier===lateSignal));
  assert.deepEqual(replica.signalParameters.find((record:any)=>record.SignalIdentifier===lateSignal),lateRecord);
  assert.deepEqual(replica.signalParameters.find((record:any)=>record.SignalIdentifier===originalSignal),original);
  assert.equal(replica.client.players.length,4);evidence.replicas[actor]=replica;
 }
 evidence.scope='Active-execution late join, authoritative signal snapshot, connected sender attribution, current epoch RPC acceptance and all-four parameter convergence; does not verify full late-join content presentation';
 api.credentials.revoke(issued.id);state='passed';
} catch(error){failure=String(error);}
finally {
 try{if(runId)await platform.artifact(runId,'signal-late-join-evidence.json',evidence);}catch(error){cleanupErrors.push('EVIDENCE_WRITE_FAILED: '+String(error));}
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();if(processes.some(i=>!['EXITED','START_FAILED'].includes(i.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,state,failure,cleanupErrors,processes};
 if(runId)await platform.artifact(runId,'signal-late-join-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
