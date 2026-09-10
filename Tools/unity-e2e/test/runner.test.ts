import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { API } from '../src/api.ts';
import { Platform, E2EError } from '../src/core.ts';
import { Runner, validate, type Definition } from '../src/runner.ts';

function definition(steps: any[]): Definition { return { version: '1.0', id: 'test', executionMode: 'regression', participants: { p1: { networkRole: 'host' } }, steps }; }
async function fixture() {
  const root = await mkdtemp(join(tmpdir(), 'unity-e2e-'));
  const platform = new Platform({ builds: {}, artifactRoot: root });
  platform.instances.set('one', { id: 'one', runId: 'process-run', nodeId: 'local', role: 'host', token: 'test', profile: '',
    endpoint: '', state: 'READY', epoch: 1, owner: 'None' });
  let released = 0;
  let acquiredResolve!:()=>void;
  const acquired=new Promise<void>(resolve=>{acquiredResolve=resolve;});
  platform.acquire = async (_id,owner='Automation') => { platform.get('one').owner = owner; acquiredResolve(); return {}; };
  platform.handoff = async (_id, owner) => { platform.get('one').owner = owner; released++; return {}; };
  platform.observe = async () => ({ scene: 'Title', control: {owner:platform.get('one').owner,controlEpoch:1}, client: { localPlayerReady: true, players: [{ local: true, position: [0,0,0] }] } });
  platform.command = async () => ({});
  return { platform, runner: new Runner(platform), root, acquired, released: () => released };
}
test('reject unknown steps, predicates and snapshot assertNever', () => {
  for (const step of [{ id:'a', type:'eval' }, { id:'a', type:'assert', predicate:'eval' }, { id:'a', type:'assertNever', predicate:'scene.is', timeoutMs:100 }])
    assert.equal(validate(definition([step])).valid, false);
});
test('regression handoff and duplicate actors are rejected', async () => {
  assert.equal(validate(definition([{ id:'h', type:'handoff', actor:'p1' }])).valid, false);
  const f = await fixture(); const d = definition([{id:'c',type:'checkpoint'}]); d.participants.p2 = {networkRole:'client'};
  assert.throws(() => f.runner.start(d,{p1:'one',p2:'one'}), /DUPLICATE_INSTANCE/);
});
test('false assertion fails and saves original failure while releasing ownership', async () => {
  const f = await fixture(); const {runId} = f.runner.start(definition([{id:'wrong',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'Game'}}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;
  const status = f.runner.status(runId); assert.equal(status.state,'failed'); assert.equal(status.error?.code,'ASSERTION_FAILED');
  assert.equal(f.released(),1); const report = JSON.parse(await readFile(join(f.root,runId,'report.json'),'utf8'));
  assert.equal(report.error.code,'ASSERTION_FAILED'); assert.equal(report.definitionHash.length,64);
});
test('cancellation interrupts state wait and releases inputs', async () => {
  const f = await fixture(); const {runId} = f.runner.start(definition([{id:'wait',type:'wait',predicate:'scene.is',args:{actor:'p1',scene:'Never'},timeoutMs:10000}]),{p1:'one'});
  await new Promise(done=>setTimeout(done,20)); f.runner.cancel(runId); await f.runner.runs.get(runId)!.done;
  assert.equal(f.runner.status(runId).state,'cancelled'); assert.equal(f.released(),1);
});
test('parallel sibling is cancelled when another branch fails', async () => {
  const f = await fixture(); const started = Date.now();
  const {runId} = f.runner.start(definition([{id:'parallel',type:'parallel',branches:[
    [{id:'wait',type:'wait',predicate:'scene.is',args:{actor:'p1',scene:'Never'},timeoutMs:10000}],
    [{id:'bad',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'Wrong'}}]
  ]}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done; assert.equal(f.runner.status(runId).state,'failed'); assert.ok(Date.now()-started<2000);
});
test('handoff terminates exploration before subsequent commands', async () => {
  const f = await fixture(); let called = false;
  f.platform.release = async()=>({}); f.platform.command = async()=>{called=true;return {};};
  const d = definition([{id:'handoff',type:'handoff',actor:'p1'},{id:'must-not-run',type:'input',actor:'p1',sequence:[{operation:'tap',key:'W'}]}]);
  d.executionMode='exploration'; const {runId}=f.runner.start(d,{p1:'one'}); await f.runner.runs.get(runId)!.done;
  assert.equal(f.runner.status(runId).state,'handed_off'); assert.equal(called,false); assert.equal(f.platform.get('one').owner,'RemoteHuman');
});
test('artifact names cannot escape the artifact root', async()=>{
  const f=await fixture(); await assert.rejects(f.platform.artifact('../escape','x.json',{}),/INVALID_ARGUMENT/);
});
test('barriers wait for each participating branch to arrive', async()=>{
  const f=await fixture(); f.platform.instances.set('two',{...f.platform.get('one'),id:'two',role:'client'});
  const d=definition([{id:'parallel',type:'parallel',branches:[
    [{id:'arrive1',type:'barrier',actor:'p1',participants:['p1','p2'],args:{barrierId:'join'},timeoutMs:1000}],
    [{id:'arrive2',type:'barrier',actor:'p2',participants:['p1','p2'],args:{barrierId:'join'},timeoutMs:1000}]
  ]}]);d.participants.p2={networkRole:'client'};
  const {runId}=f.runner.start(d,{p1:'one',p2:'two'});await f.runner.runs.get(runId)!.done;
  assert.equal(f.runner.status(runId).state,'passed');
});
test('unknown fields and malformed input commands fail schema validation',()=>{
  assert.equal(validate({...definition([{id:'x',type:'checkpoint'}]),fixture:{teleport:true}}).valid,false);
  assert.equal(validate(definition([{id:'x',type:'input',actor:'p1',sequence:[{operation:'tap'}]}])).valid,false);
});

test('predicates reject missing expected values and invalid actor references', () => {
  for (const step of [
    {predicate:'scenario.stateValue',args:{actor:'p1',side:'server',key:'ready'}},
    {predicate:'participants.ready',args:{players:['missing']}},
    {predicate:'players.count',args:{actor:'p1',count:-1}},
    {predicate:'ui.visible',args:{actor:'p1'}},
    {predicate:'scene.is',args:{scene:'Title'}},
    {predicate:'player.near',args:{actor:'p1',position:[0,0],radius:1}}
  ]) assert.equal(validate(definition([{id:'bad',type:'assert',...step}])).valid,false);
});
test('event capture failure does not suppress screenshots or the first assertion error', async () => {
  const f=await fixture(); let screens=0;
  f.platform.command=async (_id,type)=>{if(type==='events.read')throw new E2EError('OBSERVATION_GAP');if(type==='game.screenshot')screens++;return {};};
  const {runId}=f.runner.start(definition([{id:'bad',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'Wrong'}}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;
  assert.equal(screens,1);assert.equal(f.runner.status(runId).error?.code,'ASSERTION_FAILED');
  assert.match(f.runner.status(runId).cleanupErrors[0],/OBSERVATION_GAP/);
});
test('lossless event assertion catches an event that is absent from current snapshots', async () => {
  const f=await fixture();
  f.platform.command=async (_id,type)=>type==='events.read'?{cursor:1,events:[{eventSequence:1,eventType:'node.entered',side:'server',payload:{value:'short-node'}}]}:{};
  const {runId}=f.runner.start(definition([{id:'event',type:'assertEventually',predicate:'event.occurred',args:{actor:'p1',eventType:'node.entered',side:'server',match:{value:'short-node'}},timeoutMs:100}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;assert.equal(f.runner.status(runId).state,'passed');
});
test('assertNever detects a forbidden event and marks history gaps inconclusive', async () => {
  for(const gap of [false,true]) {
    const f=await fixture(), original=f.platform.observe;
    f.platform.observe=async id=>({...await original(id),eventCursor:0});
    f.platform.command=async (_id,type)=>{
      if(type!=='events.read')return {};
      if(gap)throw new E2EError('OBSERVATION_GAP');
      return {cursor:1,events:[{eventSequence:1,eventType:'signal.registered',side:'server',payload:{signalId:'forbidden'}}]};
    };
    const {runId}=f.runner.start(definition([{id:'never',type:'assertNever',predicate:'event.occurred',args:{actor:'p1',eventType:'signal.registered',side:'server',match:{signalId:'forbidden'}},timeoutMs:100}]),{p1:'one'});
    await f.runner.runs.get(runId)!.done;assert.equal(f.runner.status(runId).state,gap?'inconclusive':'failed');
  }
});
test('assertNever observes the entire interval before passing', async () => {
  const f=await fixture(),original=f.platform.observe;let reads=0;
  f.platform.observe=async id=>({...await original(id),eventCursor:0});
  f.platform.command=async()=>{reads++;return {cursor:0,events:[]};};
  const started=performance.now();
  const {runId}=f.runner.start(definition([{id:'never',type:'assertNever',predicate:'event.occurred',args:{actor:'p1',eventType:'signal.registered',side:'server'},timeoutMs:120}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;assert.equal(f.runner.status(runId).state,'passed');assert.ok(performance.now()-started>=120);assert.ok(reads>=2);
});

test('console emergency stop cancels the runner before releasing input ownership',async()=>{
 const f=await fixture(),api=new API(f.platform);
 const {runId}=api.runner.start(definition([{id:'wait',type:'wait',predicate:'scene.is',args:{actor:'p1',scene:'Never'},timeoutMs:10000}]),{p1:'one'});
 await f.acquired;
 await api.call('control.emergency_stop',{instanceId:'one'},'RemoteHuman');
 assert.equal(api.runner.status(runId).state,'cancelled');assert.equal(f.platform.get('one').owner,'None');
});
test('explicit handoff cancels active automation and acquires the requested human lease',async()=>{
 const f=await fixture(),api=new API(f.platform);
 const {runId}=api.runner.start(definition([{id:'wait',type:'wait',predicate:'scene.is',args:{actor:'p1',scene:'Never'},timeoutMs:10000}]),{p1:'one'});
 await f.acquired;
 await api.call('control.handoff',{instanceId:'one',owner:'RemoteHuman',controlEpoch:1},'Automation');
 assert.equal(api.runner.status(runId).state,'cancelled');assert.equal(f.platform.get('one').owner,'RemoteHuman');
});

test('explicit control renewal works before or after expiry and refuses human ownership',async()=>{
  for (const owner of ['None','Automation','RemoteHuman'] as const) {
    const f=await fixture();f.platform.get('one').owner=owner;
    let acquisitions=0;
    f.platform.acquire=async()=>{assert.equal(f.platform.get('one').owner,'None');acquisitions++;f.platform.get('one').owner='Automation';return {};};
    const action=f.runner.step({actors:{p1:'one'}} as any,{id:'renew',type:'controlAcquire',actor:'p1'},new AbortController().signal);
    if(owner==='RemoteHuman'){await assert.rejects(action,/CONTROL_DENIED/);assert.equal(acquisitions,0);}
    else {await action;assert.equal(acquisitions,1);assert.equal(f.released(),owner==='Automation'?1:0);}
  }
});


test('navigation cannot pass when the player is directly below the target',async()=>{
  const f=await fixture();
  f.platform.observe=async()=>({client:{players:[{local:true,position:[0,-20,0],canMove:true,walkingSpeed:7.5}]},waypoints:[{id:'target',position:[0,0,0]}]});
  await assert.rejects(f.runner.navigate('one',{id:'approach',type:'navigate',target:'target',timeoutMs:1000},new AbortController().signal),/NAVIGATION_HEIGHT_MISMATCH/);
});


test('hidden UI assertions require an existing unique element',async()=>{
  const f=await fixture(),step={id:'hidden',type:'assert',predicate:'ui.visible',args:{actor:'p1',automationId:'panel',visible:false}} as any;
  for(const [elements,expected] of [[[],false],[[{visible:false}],true],[[{visible:true}],false],[[{visible:false},{visible:false}],false]] as const) {
    f.platform.command=async()=>({elements});
    assert.equal(await f.runner.predicate({actors:{p1:'one'}} as any,step,new AbortController().signal),expected);
  }
});

test('heartbeat keeps its original epoch after a lost reply and never renews a new lease',async(t)=>{
  t.mock.timers.enable({apis:['setInterval']});
  const f=await fixture(),instance=f.platform.get('one');instance.owner='Automation';
  const epochs:number[]=[];
  f.platform.command=async(_id,_type,_payload,options)=>{
    epochs.push(options!.epoch!);
    if(epochs.length===1)throw new E2EError('INSTANCE_UNAVAILABLE');
    return {};
  };
  f.platform.heartbeat(instance,'Automation');
  t.mock.timers.tick(500);await Promise.resolve();await Promise.resolve();
  t.mock.timers.tick(500);await Promise.resolve();await Promise.resolve();
  assert.deepEqual(epochs,[1,1]);
  instance.epoch=2;
  t.mock.timers.tick(500);await Promise.resolve();
  assert.deepEqual(epochs,[1,1]);assert.equal(instance.heartbeat,undefined);
});

test('empty or unavailable inventory cannot prove an unchecked checklist item',async()=>{
  for(const [inventory,passed] of [
    [{available:false,slots:[]},false], [{available:true,slots:[]},false],
    [{available:true,slots:[{slot:0,itemId:'checklist_paper',checklist:{CompletedIdentifiers:[]}}]},true],
    [{available:true,slots:[{slot:0,itemId:'checklist_paper',checklist:{CompletedIdentifiers:['vital_set']}}]},false]
  ] as const) {
    const f=await fixture();
    f.platform.observe=async()=>({control:{owner:f.platform.get('one').owner},client:{players:[{ownerId:1,inventory}]}});
    const run=f.runner.start(definition([{id:'check',type:'assert',predicate:'checklist.itemCompleted',args:{actor:'p1',ownerId:1,itemId:'vital_set',present:false}}]),{p1:'one'});
    await f.runner.runs.get(run.runId)!.done;
    assert.equal(f.runner.status(run.runId).state,passed?'passed':'failed');
    await f.platform.close();
  }
});

test('role allocation requires matching server evidence, not a client mirror',async()=>{
  const expected={graphId:'roles',allocations:{'parallel|1':[0,1,2,3]}};
  for(const [scenarioNetwork,passed] of [
    [{server:null,client:expected},false], [{server:{...expected,allocations:{'parallel|1':[1,0,2,3]}}},false],
    [{server:expected},true]
  ] as const) {
    const f=await fixture(); f.platform.observe=async()=>({control:{owner:f.platform.get('one').owner},scenarioNetwork});
    const run=f.runner.start(definition([{id:'allocation',type:'assert',predicate:'scenario.roleAllocation',args:{actor:'p1',graphId:'roles',nodeId:'parallel',visit:1,ownerIds:[0,1,2,3]}}]),{p1:'one'});
    await f.runner.runs.get(run.runId)!.done;
    assert.equal(f.runner.status(run.runId).state,passed?'passed':'failed');await f.platform.close();
  }
});

test('item navigation rejects ambiguous or vanished pickup targets', async () => {
  const f = await fixture();
  const step = {id:'approach',type:'navigate',actor:'p1',target:'blood_bag',mode:'input_adapter',timeoutMs:1000,args:{targetType:'item',arrivalRadius:1.5}} as const;
  assert.equal(validate(definition([step])).valid,true);
  for (const worldItems of [[],[{id:'a',itemId:'blood_bag',position:[0,0,1]},{id:'b',itemId:'blood_bag',position:[0,0,1]}]]) {
    f.platform.observe = async () => ({worldItems,client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:7.5}]}});
    await assert.rejects(f.runner.navigate('one',step,AbortSignal.timeout(1000)),/TARGET_NOT_FOUND/);
  }
  f.platform.observe = async () => ({worldItems:[{id:'a',itemId:'blood_bag',position:[0,0,1]}],client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:7.5}]}});
  await f.runner.navigate('one',step,AbortSignal.timeout(1000));
});
test('static item navigation uses entity identity and rejects duplicate or vanished targets', async () => {
  const f = await fixture();
  const step = {id:'approach',type:'navigate',actor:'p1',target:'a',mode:'input_adapter',timeoutMs:1000,args:{targetType:'staticItem',arrivalRadius:1.5}} as const;
  assert.equal(validate(definition([step])).valid,true);
  for (const staticPlacedItems of [[],[{id:'a',itemId:'blood_bag',position:[0,0,1]},{id:'a',itemId:'blood_bag',position:[0,0,1]}]]) {
    f.platform.observe = async () => ({staticPlacedItems,client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:7.5}]}});
    await assert.rejects(f.runner.navigate('one',step,AbortSignal.timeout(1000)),/TARGET_NOT_FOUND/);
  }
  f.platform.observe = async () => ({staticPlacedItems:[{id:'a',itemId:'blood_bag',position:[0,0,1]}],client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:7.5}]}});
  await f.runner.navigate('one',step,AbortSignal.timeout(1000));
});
test('scenario entity navigation rejects duplicate and vanished patient identities', async () => {
  const f = await fixture();
  const step = {id:'approach',type:'navigate',actor:'p1',target:'a',mode:'input_adapter',timeoutMs:1000,args:{targetType:'scenarioEntity',arrivalRadius:1.5}} as const;
  assert.equal(validate(definition([step])).valid,true);
  for (const scenarioEntities of [[],[{id:'a',itemId:'blood_bag',position:[0,0,1]},{id:'a',itemId:'blood_bag',position:[0,0,1]}]]) {
    f.platform.observe = async () => ({scenarioEntities,client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:7.5}]}});
    await assert.rejects(f.runner.navigate('one',step,AbortSignal.timeout(1000)),/TARGET_NOT_FOUND/);
  }
  f.platform.observe = async () => ({scenarioEntities:[{id:'a',itemId:'blood_bag',position:[0,0,1]}],client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:7.5}]}});
  await f.runner.navigate('one',step,AbortSignal.timeout(1000));
});
test('scenario node identity includes its graph and input context requires observed state',async()=>{
 const f=await fixture();
 f.platform.observe=async()=>({scenario:{graphId:'wrong',nodeId:'shared_name',side:'client'},inputContext:'Gameplay'});
 const run={actors:{p1:'one'}} as any;
 assert.equal(await f.runner.predicate(run,{id:'node',type:'assert',actor:'p1',predicate:'scenario.node',args:{graphId:'expected',nodeId:'shared_name',side:'client'}}),false);
 assert.equal(await f.runner.predicate(run,{id:'context',type:'assert',actor:'p1',predicate:'input.context',args:{context:'InventoryUIController'}}),false);
 assert.equal(await f.runner.predicate(run,{id:'context',type:'assert',actor:'p1',predicate:'input.context',args:{context:'Gameplay'}}),true);
});

test('runner retires exploratory Automation ownership before replay', async () => {
  const f = await fixture();
  f.platform.get('one').owner = 'Automation';
  const calls: string[] = [];
  f.platform.acquire = async () => { throw new E2EError('CONTROL_DENIED'); };
  f.platform.handoff = async (_id, owner, from) => {
    calls.push(`${from ?? 'Automation'}->${owner}`);
    f.platform.get('one').epoch++;
    f.platform.get('one').owner = owner;
    return {};
  };
  const {runId} = f.runner.start(definition([{id:'ready',type:'assert',actor:'p1',predicate:'scene.is',args:{scene:'Title'}}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;
  assert.equal(f.runner.status(runId).state,'passed');
  assert.deepEqual(calls,['Automation->Automation','Automation->None']);
  assert.equal(f.platform.get('one').epoch,3);
});

test('runner does not take ownership from a human', async () => {
  const f = await fixture();
  f.platform.get('one').owner = 'RemoteHuman';
  f.platform.acquire = async () => { throw new E2EError('CONTROL_DENIED'); };
  const {runId} = f.runner.start(definition([{id:'never',type:'checkpoint'}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;
  assert.equal(f.runner.status(runId).state,'blocked');
  assert.equal(f.runner.status(runId).error?.code,'CONTROL_DENIED');
  assert.equal(f.released(),0);
  assert.equal(f.platform.get('one').owner,'RemoteHuman');
});

test('event substring matching requires a string and preserves exact field filters', async () => {
 for (const [payload,expected] of [[{message:'prefix duplicate role random-id',severity:'Warning'},'passed'],[{message:42,severity:'Warning'},'failed'],[{message:'unrelated',severity:'Warning'},'failed'],[{message:'duplicate role',severity:'Log'},'failed']] as const) {
  const f=await fixture();
  f.platform.command=async (_id,type)=>type==='events.read'?{cursor:1,events:[{eventSequence:1,eventType:'log',side:'process',payload}]}:{};
  const {runId}=f.runner.start(definition([{id:'rejection',type:'assertEventually',predicate:'event.occurred',args:{actor:'p1',eventType:'log',side:'process',match:{severity:'Warning'},contains:{message:'duplicate role'}},timeoutMs:100}]),{p1:'one'});
  await f.runner.runs.get(runId)!.done;assert.equal(f.runner.status(runId).state,expected);
 }
});

test('runtime failure samples only the failed participant',async()=>{
 const f=await fixture();
 f.platform.instances.set('two',{...f.platform.get('one'),id:'two',role:'client'});
 const sampled:string[]=[];
 f.platform.diagnoseProcess=async id=>{sampled.push(id);return {captured:true,durationMs:1,bytes:100};};
 f.platform.command=async (_id,type)=>{if(type==='input.execute')throw new E2EError('INSTANCE_UNAVAILABLE');return {};};
 const d=definition([{id:'failing_input',type:'input',actor:'p2',sequence:[{operation:'tap',key:'E'}]}]);
 d.participants.p2={networkRole:'client'};
 const {runId}=f.runner.start(d,{p1:'one',p2:'two'});
 await f.runner.runs.get(runId)!.done;
 assert.deepEqual(sampled,['two']);
 const diagnostics=JSON.parse(await readFile(join(f.root,runId,'process-diagnostics.json'),'utf8'));
 assert.equal(diagnostics[0].selection,'failed_step');
 await f.platform.close();
});

test('human control aborts all related jobs before waiting for any cleanup',async()=>{
 const f=await fixture(),api=new API(f.platform);
 const first=new AbortController(),second=new AbortController(),run=new AbortController(),unrelated=new AbortController();
 let finish!:()=>void;const pending=new Promise<void>(resolve=>{finish=resolve;});
 api.operations.set('first',{runId:'process-run',state:'running',controller:first,done:pending});
 api.operations.set('second',{runId:'process-run',state:'running',controller:second,done:Promise.resolve()});
 api.operations.set('other',{runId:'other-run',state:'running',controller:unrelated,done:Promise.resolve()});
 api.runner.runs.set('scenario',{state:'running',actors:{p1:'one'},controller:run,done:Promise.resolve()} as any);
 let observed=false;f.platform.observe=async()=>{observed=true;return {control:{owner:'None'}};};
 const acquire=api.call('control.acquire',{instanceId:'one'},'RemoteHuman');
 assert.equal(first.signal.aborted,true);assert.equal(second.signal.aborted,true);assert.equal(run.signal.aborted,true);
 assert.equal(unrelated.signal.aborted,false);assert.equal(observed,false);
 finish();await acquire;assert.equal(observed,true);
});

test('service deadline ends an abandoned AI request and revokes only its credentials',async()=>{
 const f=await fixture(),api=new API(f.platform);
 const queued=api.assistance.submit('process-run','one','Open settings');
 const running=await api.call('assistance.update',{requestId:queued.id,revision:0,state:'running',timeoutMs:30});
 const owned=await api.call('credentials.issue',{runId:'process-run',requestId:queued.id,capabilities:['observe','control'],ttlMs:10000});
 const unrelated=await api.call('credentials.issue',{runId:'process-run',capabilities:['observe'],ttlMs:10000});
 const controller=new AbortController();
 api.runner.runs.set('linked',{state:'running',actors:{p1:'one'},controller} as any);
 (api as any).requestRuns.set(queued.id,new Set(['linked']));
 await new Promise(resolve=>setTimeout(resolve,60));
 const final=api.assistance.get(queued.id)!;
 assert.equal(final.state,'failed');assert.match(final.result!,/기한이 만료/);assert.equal(controller.signal.aborted,true);
 assert.equal(api.credentials.authenticate('Bearer '+owned.token),undefined);
 assert.ok(api.credentials.authenticate('Bearer '+unrelated.token));
 await assert.rejects(api.call('assistance.update',{requestId:queued.id,revision:running.revision,state:'completed',result:'late'}),/STATE_CONFLICT/);
});

test('terminal AI requests clear their deadline and reject further credential issuance',async()=>{
 const f=await fixture(),api=new API(f.platform),queued=api.assistance.submit('process-run','one','Open settings');
 const running=await api.call('assistance.update',{requestId:queued.id,revision:0,state:'running',timeoutMs:30});
 await api.call('assistance.update',{requestId:queued.id,revision:running.revision,state:'completed',result:'done'});
 await new Promise(resolve=>setTimeout(resolve,50));
 assert.equal(api.assistance.get(queued.id)!.state,'completed');
 await assert.rejects(api.call('credentials.issue',{runId:'process-run',requestId:queued.id,capabilities:['observe'],ttlMs:1000}),/INVALID_ASSISTANCE_REQUEST/);
});

test('emergency stop releases input before pending cleanup and preserves cleanup failures',async()=>{
 const f=await fixture(),api=new API(f.platform),controller=new AbortController();
 let rejectCleanup!:(reason:Error)=>void;const pending=new Promise<void>((_resolve,reject)=>{rejectCleanup=reject;});
 api.operations.set('join',{runId:'process-run',state:'running',controller,done:pending});
 let released=false;f.platform.observe=async()=>({control:{owner:'Automation'}});
 f.platform.releaseControl=async()=>{released=true;return {alreadyReleased:true,controlEpoch:2};};
 const result=api.call('control.emergency_stop',{instanceId:'one'});
 await new Promise(resolve=>setImmediate(resolve));assert.equal(controller.signal.aborted,true);assert.equal(released,true);
 rejectCleanup(new Error('cleanup storage failed'));
 await assert.rejects(result,(error:any)=>error.code==='CLEANUP_FAILED'&&/Input release completed/.test(error.message));
});


test('runtime failure releases held input before slow process diagnostics',async()=>{
 const f=await fixture();let releasedInput=false,finishDiagnostics!:()=>void,entered!:()=>void;
 const diagnosing=new Promise<void>(resolve=>{entered=resolve;});
 f.platform.command=async (_id,type)=>{
  if(type==='input.execute')throw new E2EError('INSTANCE_UNAVAILABLE');
  if(type==='input.release_all')releasedInput=true;
  return {};
 };
 f.platform.diagnoseProcess=async()=>{
  assert.equal(releasedInput,true);entered();
  await new Promise<void>(resolve=>{finishDiagnostics=resolve;});return {captured:true,durationMs:1,bytes:100};
 };
 const {runId}=f.runner.start(definition([{id:'input_failure',type:'input',actor:'p1',sequence:[{operation:'press',key:'W'}]}]),{p1:'one'});
 await diagnosing;assert.equal(releasedInput,true);finishDiagnostics();
 await f.runner.runs.get(runId)!.done;
 assert.equal(f.runner.status(runId).error?.code,'INSTANCE_UNAVAILABLE');
 await f.platform.close();
});

test('each run exports only history after its start cursor and keeps the original store intact',async()=>{
 const f=await fixture();let boundary=10;const reads:number[]=[];
 const stored=[{id:9,kind:'command',timestamp:'old',body:{command:{type:'old'}}},{id:11,kind:'command',timestamp:'new',body:{command:{type:'new'}}}];
 f.platform.historyCursor=async()=>boundary;
 f.platform.eventHistory=async(_id,after=0)=>{reads.push(after);return stored.filter(entry=>entry.id>after);};
 const run=f.runner.start(definition([{id:'check',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'Title'}}]),{p1:'one'});
 await f.runner.runs.get(run.runId)!.done;
 const report=JSON.parse(await readFile(join(f.root,run.runId,'report.json'),'utf8'));
 assert.equal(report.state,'passed');assert.equal(report.historyAfterCursor,10);
 assert.deepEqual(report.inputs,[stored[1].body]);assert.deepEqual(reads,[10]);assert.equal(stored.length,2);
 boundary=11;
 const second=f.runner.start(definition([{id:'check',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'Title'}}]),{p1:'one'});
 await f.runner.runs.get(second.runId)!.done;
 const secondReport=JSON.parse(await readFile(join(f.root,second.runId,'report.json'),'utf8'));
 assert.deepEqual(secondReport.inputs,[]);assert.deepEqual(reads,[10,11]);
});

test('heartbeat permits two bounded renewals while an acknowledgement is delayed',async(t)=>{
 t.mock.timers.enable({apis:['setInterval']});
 const f=await fixture(),instance=f.platform.get('one');instance.owner='Automation';
 const pending:Array<{resolve:(v:any)=>void;epoch:number}>=[];
 f.platform.command=(_id,_type,_payload,options)=>new Promise(resolve=>pending.push({resolve,epoch:options!.epoch!}));
 f.platform.heartbeat(instance,'Automation');
 t.mock.timers.tick(500);t.mock.timers.tick(500);t.mock.timers.tick(500);
 assert.equal(pending.length,2);assert.deepEqual(pending.map(x=>x.epoch),[1,1]);
 pending[1].resolve({});await Promise.resolve();await Promise.resolve();
 t.mock.timers.tick(500);assert.equal(pending.length,3);
 instance.epoch=2;t.mock.timers.tick(500);assert.equal(instance.heartbeat,undefined);
 pending[0].resolve({});pending[2].resolve({});await Promise.resolve();await Promise.resolve();
 t.mock.timers.tick(1000);assert.equal(pending.length,3);
});

test('navigation counts turning progress but still detects a motionless blocked player',async()=>{
 for(const turning of [true,false]){
  const f=await fixture();let yaw=0,commands=0;
  f.platform.observe=async()=>({waypoints:[{id:'target',position:[10,0,0]}],client:{players:[{local:true,position:turning&&commands>=9?[10,0,0]:[0,0,0],yaw,canMove:true,walkingSpeed:5,rotationSensitivity:1}]}});
  f.platform.command=async()=>{await new Promise(resolve=>setTimeout(resolve,100));commands++;if(turning)yaw+=10;return {};};
  const action=f.runner.navigate('one',{id:'turn',type:'navigate',target:'target',timeoutMs:3000,args:{stuckWindowMs:500}},AbortSignal.timeout(4000));
  if(turning)await action;else await assert.rejects(action,/NAVIGATION_STUCK/);
  await f.platform.close();
 }
});

test('navigation offsets target position and validates bounded finite offsets',async()=>{
 const f=await fixture();const step={id:'offset',type:'navigate',mode:'input_adapter',actor:'p1',target:'target',timeoutMs:1000,args:{targetOffset:[-10,0,0]}} as any;
 assert.equal(validate(definition([step])).valid,true);
 f.platform.observe=async()=>({waypoints:[{id:'target',position:[10,0,0]}],client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:5}]}});
 await f.runner.navigate('one',step,AbortSignal.timeout(1000));
 for(const offset of [[11,0,0],[0,0],[0,NaN,0]])assert.equal(validate(definition([{...step,args:{targetOffset:offset}}])).valid,false);
 await f.platform.close();
});

test('navigation retains current static item discovery only when its target needs it',async()=>{
 const f=await fixture();const requests:any[]=[];
 f.platform.observe=async(_id:string,_release?:boolean,options?:any)=>{
  requests.push(options);
  return {waypoints:[{id:'target',position:[0,0,0]}],staticPlacedItems:options?.includeStaticItems?[{id:'target',position:[0,0,0]}]:null,
   client:{players:[{local:true,position:[0,0,0],canMove:true,walkingSpeed:5}]}};
 };
 for(const targetType of ['waypoint','staticItem'])await f.runner.navigate('one',{id:'target',type:'navigate',mode:'input_adapter',target:'target',timeoutMs:1000,args:{targetType}} as any,AbortSignal.timeout(1000));
 assert.equal(requests.length,2);
 assert.equal(requests[0].includeStaticItems,false);assert.equal(requests[1].includeStaticItems,true);
 for(const request of requests){assert.equal(request.ttlMs,5000);assert.ok(request.signal instanceof AbortSignal);}
 await f.platform.close();
});

test('navigation bounds forward input by distance, speed and remaining turn',async()=>{
 for(const [distance,yaw,expected] of [[10,0,250],[1,0,100],[.6,0,19],[10,15,100]]){
  const f=await fixture();let sent=false;const durations:number[]=[];
  f.platform.observe=async()=>({waypoints:[{id:'target',position:[0,0,distance]}],client:{players:[{local:true,position:[0,0,sent?distance:0],yaw,canMove:true,walkingSpeed:5,rotationSensitivity:1}]}});
  f.platform.command=async(_id,type,payload:any)=>{if(type==='input.execute'){durations.push(payload.sequence.find((x:any)=>x.operation==='hold').durationMs);sent=true;}return {};};
  await f.runner.navigate('one',{id:'move',type:'navigate',target:'target',timeoutMs:1000,args:{arrivalRadius:.5}},AbortSignal.timeout(1000));
  assert.deepEqual(durations,[expected]);await f.platform.close();
 }
});

test('navigation recognizes a turn back after overshooting an aligned target',async()=>{
 const f=await fixture();let commands=0;
 f.platform.observe=async()=>({waypoints:[{id:'target',position:[0,0,2]}],client:{players:[{local:true,position:[0,0,commands===0?0:commands>=7?2:4],yaw:Math.max(0,commands-1)*30,canMove:true,walkingSpeed:5,rotationSensitivity:1}]}});
 f.platform.command=async(_id,type)=>{if(type==='input.execute'){await new Promise(resolve=>setTimeout(resolve,150));commands++;}return {};};
 await f.runner.navigate('one',{id:'overshoot',type:'navigate',target:'target',timeoutMs:3000,args:{stuckWindowMs:500}},AbortSignal.timeout(4000));
 assert.equal(commands,7);await f.platform.close();
});

test('interaction selection initializes an empty selection and confirms the selected identity before input', async () => {
 const f=await fixture();
 let selected=-1;
 const inputs:string[]=[];
 f.platform.observe=async()=>({inputBindings:{interact:'E'},interactions:[
  {interactionId:'other',entityId:'one',index:0,selected:selected===0},
  {interactionId:'use',entityId:'two',index:1,selected:selected===1},
  {interactionId:'use',entityId:'three',index:2,selected:selected===2}
 ]});
 f.platform.command=async(_id,_type,payload)=>{
  const operation=(payload?.sequence as {operation:string,key?:string,y?:number}[])[0];
  inputs.push(operation.operation==='interactionExecute'?`execute:${(operation as any).index}`:operation.operation==='interactionSelect'?`select:${(operation as any).index}`:operation.key!);
  if(operation.operation==='interactionExecute')selected=(operation as any).index;
  if(operation.operation==='interactionSelect')selected=(operation as any).index;
  return {};
 };
 await f.runner.interact('one',{id:'use',type:'interact',target:'use',args:{entityId:'three'}},AbortSignal.timeout(3000));
 assert.deepEqual(inputs,['select:2','E']);
});
