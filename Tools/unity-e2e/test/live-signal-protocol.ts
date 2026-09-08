import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {API} from '../src/api.ts';
import {joinInstances} from '../src/startup.ts';
const config=await loadConfig(process.argv[2]??'config.json');
config.fixture={...config.fixture,allowScenarioFixtures:true,allowProtocolTests:true};
const platform=new Platform(config),api=new API(platform);
let runId:string|undefined,state='failed',failure:string|undefined;
const evidence:Record<string,any>={},cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','host_plus_3_clients');runId=launch.runId;
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const server=actors.p1;
 const credential=api.credentials.issue(runId,['observe','control','fixture','protocol'],120000);
 const grant=api.credentials.authenticate('Bearer '+credential.token)!;
 const send=async(client:string,signalId:string,count=1,parameterJson?:string)=>api.callScoped(grant,'protocol.signal_raise',{instanceId:client,payload:{signalId,count,...(parameterJson===undefined?{}:{parameterJson})}},'Automation');
 evidence.stage='fixture_preparation';
 await platform.acquire(server);
 await assert.rejects(send(server,'sig.e2e.protocol.exact'),(error:any)=>error.code==='REMOTE_CLIENT_REQUIRED');
 await assert.rejects(api.callScoped(grant,'fixture.scenario_start',{instanceId:server,payload:{graphId:'e2e_nonexistent_protocol_fixture',ownerId:0}},'Automation'),(error:any)=>error.code==='FIXTURE_GRAPH_INVALID');
 const owner=(await platform.observe(actors.p2)).client.players.find((p:any)=>p.local).ownerId;
 await api.callScoped(grant,'fixture.scenario_start',{instanceId:server,payload:{graphId:'e2e_signal_protocol',ownerId:owner}},'Automation');
 const preparationDeadline=performance.now()+10000;
 while((await platform.observe(server)).scenario.graphId!=='e2e_signal_protocol') {
  if(performance.now()>preparationDeadline)throw new Error('FIXTURE_NOT_ACTIVE');
  await delay(100);
 }
 await platform.releaseControl(server);
 await platform.acquire(actors.p2);await platform.acquire(actors.p3);
 let cursor=(await platform.observe(server)).eventCursor;
 const events:any[]=[];evidence.events=events;
 async function collectUntil(predicate:()=>boolean) {
  const deadline=performance.now()+10000;
  do {
   const batch=await platform.command(server,'events.read',{cursor});
   for(const event of batch.events??[]){events.push(event);cursor=Math.max(cursor,event.eventSequence);}
   if(predicate())return;
   await delay(100);
  }while(performance.now()<deadline);
  throw new Error('SERVER_EVIDENCE_TIMEOUT');
 }
 const recorded=(signal:string)=>events.filter(e=>e.side==='server'&&e.eventType==='signal.parameter_recorded'&&e.payload.signalId===signal);
 const logContains=(text:string)=>events.some(e=>e.eventType==='log'&&typeof e.payload.message==='string'&&e.payload.message.includes(text));
 evidence.stage='undeclared';
 await send(actors.p2,'sig.e2e.protocol.undeclared');
 await collectUntil(()=>logContains('Undeclared client signal ignored: signal="sig.e2e.protocol.undeclared"'));
 evidence.stage='prefix_escape';
 await send(actors.p2,'sig.e2e.protocol.prefixEscape');
 await collectUntil(()=>logContains('Undeclared client signal ignored: signal="sig.e2e.protocol.prefixEscape"'));
 evidence.stage='invalid_json';
 await send(actors.p2,'sig.e2e.protocol.exact',1,'{invalid');
 await collectUntil(()=>logContains('시그널 (sig.e2e.protocol.exact)의 매개변수 ({invalid)는 올바른 JSON 형식이 아닙니다.'));
 await send(actors.p2,'sig.e2e.protocol.exact',1,'{"probe":"exact"}');
 await collectUntil(()=>recorded('sig.e2e.protocol.exact').length===1);
 await send(actors.p2,'sig.e2e.protocol.prefix.accepted');
 await collectUntil(()=>recorded('sig.e2e.protocol.prefix.accepted').length===1);
 await delay(1200);
 evidence.stage='rate_and_isolation';
 const burst='sig.e2e.protocol.prefix.burst';
 await Promise.all([send(actors.p2,burst,31),send(actors.p3,burst,1)]);
 await collectUntil(()=>recorded(burst).length>=31&&logContains(`시그널 (${burst})을 수락하지 못했습니다: 플레이어별 시그널 갱신 한도(30/1초)에 도달했습니다.`));
 const burstRecords=recorded(burst),byPlayer:Record<string,any[]>={};
 for(const event of burstRecords)(byPlayer[event.payload.playerId]??=[]).push(event.payload);
 assert.deepEqual(Object.values(byPlayer).map(records=>records.length).sort((a,b)=>a-b),[1,30]);
 const thirty=Object.values(byPlayer).find(records=>records.length===30)!;
 assert.ok(thirty.at(-1).serverRealtime-thirty[0].serverRealtime<1,'Burst must fit inside the actual server one-second window');
 evidence.burst={byPlayer,spanSeconds:thirty.at(-1).serverRealtime-thirty[0].serverRealtime};
 await delay(1200);
 evidence.stage='recovery';
 await send(actors.p2,burst);
 await collectUntil(()=>recorded(burst).length===32);
 const recovered=recorded(burst).at(-1).payload;
 assert.equal(recovered.playerId,thirty[0].playerId);
 assert.ok(recovered.serverRealtime-thirty[0].serverRealtime>=1);
 evidence.recovery=recovered;
 const snapshot=await platform.observe(server);
 assert.equal(snapshot.server.connections,4);
 assert.ok(!snapshot.signalParameters.some((v:any)=>['sig.e2e.protocol.undeclared','sig.e2e.protocol.prefixEscape'].includes(v.SignalIdentifier)));
 assert.equal(recorded('sig.e2e.protocol.exact').length,1);
 evidence.events=events;evidence.server=snapshot;evidence.mode='protocol_test';
 evidence.scope='identifier/prefix, invalid JSON, 30-of-31 within server window, second-player isolation, recovery; repeated content signals are counted, not deduplicated';
 await platform.artifact(runId,'signal-protocol-evidence.json',evidence);
 api.credentials.revoke(credential.id);state='passed';
} catch(error) {
 failure=String(error);
 if(runId)await platform.artifact(runId,'signal-protocol-partial-evidence.json',evidence);
 if(runId)for(const instance of platform.instances.values())try{await platform.artifact(runId,`${instance.id}-protocol-failure.json`,await platform.observe(instance.id));}catch{}
} finally {
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();if(processes.some(i=>!['EXITED','START_FAILED'].includes(i.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,state,failure,cleanupErrors,processes};
 if(runId)await platform.artifact(runId,'signal-protocol-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
