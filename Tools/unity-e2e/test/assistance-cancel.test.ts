import test from 'node:test';
import assert from 'node:assert/strict';
import {API} from '../src/api.ts';
import type {Platform} from '../src/core.ts';
function setup() {
 const instances=new Map([['a',{id:'a',runId:'one'}],['b',{id:'b',runId:'one'}],['other',{id:'other',runId:'two'}]]);
 let failArtifact=false;
 const api=new API({instances,config:{},get:(id:string)=>instances.get(id),artifact:async()=>{if(failArtifact)throw new Error('WRITE_FAILED');}} as unknown as Platform);
 let starts=0;
 api.runner.start=()=>{const id=`run-${++starts}`;api.runner.runs.set(id,{state:'running',controller:new AbortController()} as any);return {runId:id};};
 return {api,failWrites:()=>{failArtifact=true;}};
}
test('request cancellation aborts only linked executions and blocks stale completion or restart',async()=>{
 const {api}=setup();const request=api.assistance.submit('one','a','test');api.assistance.transition(request.id,0,'running');
 const linked=await api.call('scenario.start',{definition:{},actors:{p1:'a'},requestId:request.id});
 const unrelated=await api.call('scenario.start',{definition:{},actors:{p1:'b'}});
 await assert.rejects(api.call('assistance.update',{requestId:request.id,revision:1,state:'completed',result:'premature'}),/ASSISTANCE_EXECUTION_ACTIVE/);
 const cancelled=await api.call('assistance.update',{requestId:request.id,revision:1,state:'cancelled'},'RemoteHuman');
 assert.equal(cancelled.state,'cancelled');assert.equal(api.runner.runs.get(linked.runId)!.controller.signal.aborted,true);
 assert.equal(api.runner.runs.get(unrelated.runId)!.controller.signal.aborted,false);
 await assert.rejects(api.call('scenario.start',{definition:{},actors:{p1:'a'},requestId:request.id}),/ASSISTANCE_NOT_RUNNING/);
 await assert.rejects(api.call('assistance.update',{requestId:request.id,revision:1,state:'completed',result:'late'}));
 assert.equal(api.assistance.get(request.id)!.state,'cancelled');
});
test('binding requires a running request and matching target and process scope',async()=>{
 const {api}=setup();const request=api.assistance.submit('one','a','test');
 const start=(actors:any)=>api.call('scenario.start',{definition:{},actors,requestId:request.id});
 await assert.rejects(start({p1:'a'}),/ASSISTANCE_NOT_RUNNING/);api.assistance.transition(request.id,0,'running');
 await assert.rejects(start({p1:'b'}),/ASSISTANCE_ACTOR_MISMATCH/);
 await assert.rejects(start({p1:'a',p2:'other'}),/ASSISTANCE_ACTOR_MISMATCH/);
 const issued=api.credentials.issue('two',['observe','control'],10000),grant=api.credentials.authenticate('Bearer '+issued.token)!;
 await assert.rejects(api.callScoped(grant,'scenario.start',{definition:{},actors:{p1:'other'},requestId:request.id},'Automation'),/PERMISSION_DENIED/);
 assert.equal(api.runner.runs.size,0);
});
test('binding persistence failure aborts the new run; AI failure aborts linked execution',async()=>{
 const {api,failWrites}=setup();const request=api.assistance.submit('one','a','test');api.assistance.transition(request.id,0,'running');
 const first=await api.call('scenario.start',{definition:{},actors:{p1:'a'},requestId:request.id});
 await api.call('assistance.update',{requestId:request.id,revision:1,state:'failed',result:'worker failed'});
 assert.equal(api.runner.runs.get(first.runId)!.controller.signal.aborted,true);
 const next=api.assistance.submit('one','a','next');api.assistance.transition(next.id,0,'running');failWrites();
 await assert.rejects(api.call('scenario.start',{definition:{},actors:{p1:'a'},requestId:next.id}),/WRITE_FAILED/);
 assert.equal(api.runner.runs.get('run-2')!.controller.signal.aborted,true);
});
