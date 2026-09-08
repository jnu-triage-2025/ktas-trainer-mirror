import test from 'node:test';
import assert from 'node:assert/strict';
import { API } from '../src/api.ts';
import type { Platform } from '../src/core.ts';
test('scoped API filters instances and denies cross-run calls and credential delegation', async () => {
 const instances = new Map([['a',{id:'a',runId:'one'}],['b',{id:'b',runId:'two'}]]);
 const api = new API({instances,config:{},list:()=>[...instances.values()]} as unknown as Platform);
 const issued = api.credentials.issue('one',['observe'],10000);
 const grant = api.credentials.authenticate('Bearer '+issued.token)!;
 let calls=0;api.call=async()=>{calls++;return {};};
 assert.equal((await api.callScoped(grant,'instances.list',{},'Automation')).length,1);
 await api.callScoped(grant,'game.observe',{instanceId:'a'},'Automation');
 for(const [name,args] of [['game.observe',{instanceId:'b'}],['input.execute',{instanceId:'a'}],['credentials.issue',{runId:'one'}],['artifacts.read',{runId:'two'}]] as const)
  await assert.rejects(api.callScoped(grant,name,args,'Automation'),/PERMISSION_DENIED/);
 assert.equal(calls,1);
 api.credentials.revoke(issued.id);
 await assert.rejects(api.callScoped(grant,'game.observe',{instanceId:'a'},'Automation'),/PERMISSION_DENIED/);
});
test('chat-enabled runs require fixture authority for input, and network faults require protocol authority', async () => {
 const api=new API({instances:new Map([['a',{id:'a',runId:'one'}]]),config:{fixture:{allowChatCommands:true}}} as unknown as Platform);
 const get=(caps:any)=>{const issued=api.credentials.issue('one',caps,10000);return api.credentials.authenticate('Bearer '+issued.token)!;};
 let calls=0;api.call=async()=>{calls++;return {};};
 await assert.rejects(api.callScoped(get(['control']),'input.execute',{instanceId:'a'},'Automation'),/PERMISSION_DENIED/);
 await api.callScoped(get(['control','fixture']),'input.execute',{instanceId:'a'},'Automation');
 await assert.rejects(api.callScoped(get(['control','fixture']),'network.fault',{instanceId:'a'},'Automation'),/PERMISSION_DENIED/);
 await api.callScoped(get(['protocol']),'network.fault',{instanceId:'a'},'Automation');
 assert.equal(calls,2);
});
test('scenario and artifact lookup checks every actor rather than trusting the requested run ID', async () => {
 const api=new API({instances:new Map([['a',{id:'a',runId:'one'}],['b',{id:'b',runId:'two'}]]),config:{}} as unknown as Platform);
 const issued=api.credentials.issue('one',['observe'],10000), grant=api.credentials.authenticate('Bearer '+issued.token)!;
 api.runner.runs.set('owned',{actors:{p1:'a'}} as any);
 api.runner.runs.set('mixed',{actors:{p1:'a',p2:'b'}} as any);
 api.operations.set('owned-job',{runId:'one',state:'completed',controller:new AbortController()});
 api.operations.set('other-job',{runId:'two',state:'completed',controller:new AbortController()});
 let calls=0;api.call=async()=>{calls++;return {};};
 for(const [name,args] of [['scenario.status',{runId:'owned'}],['artifacts.read',{runId:'owned',name:'report.json'}],['artifacts.list',{runId:'one'}],['operations.status',{operationId:'owned-job'}]] as const)
  await api.callScoped(grant,name,args,'Automation');
 for(const [name,args] of [['scenario.status',{runId:'mixed'}],['artifacts.read',{runId:'mixed',name:'report.json'}],['artifacts.list',{runId:'two'}],['operations.status',{operationId:'other-job'}],['scenario.cancel',{runId:'owned'}]] as const)
  await assert.rejects(api.callScoped(grant,name,args,'Automation'),/PERMISSION_DENIED/);
 assert.equal(calls,4);
});
test('scoped scenario execution is cancelled when its credential is revoked', async () => {
 const api=new API({instances:new Map([['a',{id:'a',runId:'one'}]]),config:{}} as unknown as Platform);
 const issued=api.credentials.issue('one',['observe','control'],10000), grant=api.credentials.authenticate('Bearer '+issued.token)!;
 const controller=new AbortController();let finish!:()=>void;
 const done=new Promise<void>(resolve=>{finish=resolve;});
 api.call=async()=>{api.runner.runs.set('scenario',{actors:{p1:'a'},controller,done} as any);return {runId:'scenario'};};
 await api.callScoped(grant,'scenario.start',{actors:{p1:'a'}},'Automation');
 api.credentials.revoke(issued.id);
 try {
  await new Promise<void>((resolve,reject)=>{
   const deadline=setTimeout(()=>reject(new Error('Revocation did not cancel the scenario')),1000);
   controller.signal.addEventListener('abort',()=>{clearTimeout(deadline);resolve();},{once:true});
  });
  assert.equal(controller.signal.aborted,true);
 } finally {finish();}
});
test('recording authorization resolves the recorded instance and requires observation and control', async () => {
 const api=new API({instances:new Map([['a',{id:'a',runId:'one'}],['b',{id:'b',runId:'two'}]]),config:{},recordingInstance:(id:string)=>id==='owned'?'a':'b'} as unknown as Platform);
 const token=(caps:any)=>{const issued=api.credentials.issue('one',caps,10000);return api.credentials.authenticate('Bearer '+issued.token)!;};
 let calls=0;api.call=async()=>{calls++;return {};};
 const grant=token(['observe','control']);
 await api.callScoped(grant,'recording.start',{instanceId:'a'},'Automation');
 await api.callScoped(grant,'recording.stop',{recordingId:'owned'},'Automation');
 await assert.rejects(api.callScoped(grant,'recording.stop',{recordingId:'other'},'Automation'),/PERMISSION_DENIED/);
 await assert.rejects(api.callScoped(token(['control']),'recording.start',{instanceId:'a'},'Automation'),/PERMISSION_DENIED/);
 assert.equal(calls,2);
});
test('room preparation is scoped and expires into cancellation', async () => {
 const api=new API({instances:new Map(),config:{}} as unknown as Platform);
 const issued=api.credentials.issue('one',['observe','control'],150),grant=api.credentials.authenticate('Bearer '+issued.token)!;
 const controller=new AbortController();let finish!:()=>void;
 const done=new Promise<void>(resolve=>{finish=resolve;});let calls=0;
 api.call=async()=>{calls++;api.operations.set('job',{runId:'one',state:'running',controller,done});return {operationId:'job'};};
 await assert.rejects(api.callScoped(grant,'instances.join',{runId:'other'},'Automation'),/PERMISSION_DENIED/);
 await api.callScoped(grant,'instances.join',{runId:'one'},'Automation');
 try {
  await new Promise<void>((resolve,reject)=>{
   const deadline=setTimeout(()=>reject(new Error('Expiry did not cancel preparation')),1000);
   controller.signal.addEventListener('abort',()=>{clearTimeout(deadline);resolve();},{once:true});
  });
  assert.equal(calls,1);
 } finally {finish();}
});
test('scoped condition wait rejects mixed actors and aborts on revocation', async () => {
 const api=new API({instances:new Map([['a',{id:'a',runId:'one'}],['b',{id:'b',runId:'two'}]]),config:{}} as unknown as Platform);
 const issued=api.credentials.issue('one',['observe'],10000),grant=api.credentials.authenticate('Bearer '+issued.token)!;
 let calls=0;
 api.call=async(_name,_args,_caller,signal)=>{
  calls++;
  return new Promise((resolve,reject)=>{
   const deadline=setTimeout(()=>reject(new Error('Condition wait did not cancel')),1000);
   signal!.addEventListener('abort',()=>{clearTimeout(deadline);reject(new Error('WAIT_ABORTED'));},{once:true});
  });
 };
 await assert.rejects(api.callScoped(grant,'conditions.wait',{actors:{p1:'a',p2:'b'}},'Automation'),/PERMISSION_DENIED/);
 const pending=api.callScoped(grant,'conditions.wait',{actors:{p1:'a'}},'Automation');
 api.credentials.revoke(issued.id);
 await assert.rejects(pending,/WAIT_ABORTED/);
 assert.equal(calls,1);
});

test('omitted request run ID is restricted to the credential run even for restored history', async () => {
 const api=new API({instances:new Map(),config:{}} as unknown as Platform);
 api.assistance.submit('one','old-a','first');api.assistance.submit('two','old-b','second');
 const issued=api.credentials.issue('one',['observe'],10000), grant=api.credentials.authenticate('Bearer '+issued.token)!;
 assert.equal((await api.call('assistance.list',{})).length,2);
 const scoped=await api.callScoped(grant,'assistance.list',{},'Automation');
 assert.equal(scoped.length,1);assert.equal(scoped[0].runId,'one');
 await assert.rejects(api.callScoped(grant,'assistance.list',{runId:'two'},'Automation'),/PERMISSION_DENIED/);
});

test('scenario fixtures require an explicit option plus same-run control and fixture permissions', async () => {
 const instances=new Map([['server',{id:'server',runId:'one'}],['other',{id:'other',runId:'two'}]]);
 let calls=0;
 const platform={instances,config:{fixture:{allowScenarioFixtures:false}},command:async()=>{calls++;return {accepted:true};}} as unknown as Platform;
 const api=new API(platform),args={instanceId:'server',payload:{graphId:'e2e_graph',ownerId:1}};
 const grant=(capabilities:any)=>{const issued=api.credentials.issue('one',capabilities,10000);return api.credentials.authenticate('Bearer '+issued.token)!;};
 await assert.rejects(api.call('fixture.scenario_start',args),/FIXTURE_DISABLED/);
 platform.config.fixture!.allowScenarioFixtures=true;
 await assert.rejects(api.callScoped(grant(['control']),'fixture.scenario_start',args,'Automation'),/PERMISSION_DENIED/);
 await assert.rejects(api.callScoped(grant(['fixture']),'fixture.scenario_start',args,'Automation'),/PERMISSION_DENIED/);
 await assert.rejects(api.callScoped(grant(['control','fixture']),'fixture.scenario_start',{...args,instanceId:'other'},'Automation'),/PERMISSION_DENIED/);
 for(const graphId of ['graph; other','graph\nother','graph|other']) await assert.rejects(api.call('fixture.scenario_start',{...args,payload:{graphId,ownerId:1}}),(error:any)=>error.code==='INVALID_ARGUMENT');
 assert.equal(calls,0);
 await api.callScoped(grant(['control','fixture']),'fixture.scenario_start',args,'Automation');assert.equal(calls,1);
});

test('raw signal protocol calls require explicit opt-in, both capabilities, and bounded payloads', async () => {
 const config:any={};let sent=0;
 const api=new API({config,instances:new Map([['a',{id:'a',runId:'one'}],['b',{id:'b',runId:'two'}]]),command:async()=>{sent++;return {sent:1};}} as unknown as Platform);
 const grant=(caps:any)=>{const issued=api.credentials.issue('one',caps,10000);return api.credentials.authenticate('Bearer '+issued.token)!;};
 const args={instanceId:'a',payload:{signalId:'protocol.example'}};
 await assert.rejects(api.call('protocol.signal_raise',args),/PROTOCOL_TESTS_DISABLED/);
 config.fixture={allowProtocolTests:true};
 for(const caps of [['control'],['protocol'],['observe','fixture']])
  await assert.rejects(api.callScoped(grant(caps),'protocol.signal_raise',args,'Automation'),/PERMISSION_DENIED/);
 const authorized=grant(['control','protocol']);
 await assert.rejects(api.callScoped(authorized,'protocol.signal_raise',{...args,instanceId:'b'},'Automation'),/PERMISSION_DENIED/);
 for(const payload of [{signalId:'x',count:65},{signalId:'x',count:0},{signalId:'x',count:1.5},{signalId:'x',parameterJson:'x'.repeat(16385)},{signalId:'x',ownerId:2}])
  await assert.rejects(api.call('protocol.signal_raise',{...args,payload}),(error:any)=>error.code==='INVALID_ARGUMENT');
 await api.callScoped(authorized,'protocol.signal_raise',args,'Automation');
 assert.equal(sent,1);
});
