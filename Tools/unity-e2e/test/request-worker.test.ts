import test from 'node:test';
import assert from 'node:assert/strict';
import {AssistanceRequests} from '../src/requests.ts';
import {processRequest,type WorkerCall} from '../src/request-worker.ts';
function fixture(){
 const requests=new AssistanceRequests(),queued=requests.submit('run','one','Open settings'),revoked:string[]=[];
 const call:WorkerCall=async(tool,args)=>{
  if(tool==='assistance.update')return requests.transition(args.requestId as string,args.revision as number,args.state as any,args.result as string);
  if(tool==='assistance.list')return requests.list();
  if(tool==='credentials.issue')return {token:'scoped-test-token',id:'grant',runId:'run',expiresAt:Date.now()+10000};
  if(tool==='credentials.revoke'){revoked.push(args.credentialId as string);return {revoked:true};}
  throw new Error(tool);
 };return {requests,queued,call,revoked};
}
test('worker claims once, passes a scoped credential and revokes it after completion',async()=>{
 const f=fixture();let called=0;
 const agent=async(context:any)=>{called++;assert.equal(context.request.state,'running');assert.equal(context.credential.runId,'run');return 'Settings opened';};
 const results=await Promise.all([processRequest(f.call,agent,f.queued,new AbortController().signal),processRequest(f.call,agent,f.queued,new AbortController().signal)]);
 assert.equal(called,1);assert.deepEqual(results.map(x=>x.state).sort(),['completed','not_claimed']);assert.deepEqual(f.revoked,['grant']);
});
test('worker preserves cancellation and aborts an active adapter',async()=>{
 const f=fixture();let aborted=false;
 const keepalive=setInterval(()=>{},1000);
 const agent=async(_context:any,signal:AbortSignal)=>new Promise<string>((_resolve,reject)=>{
  signal.addEventListener('abort',()=>{aborted=true;reject(signal.reason);},{once:true});
  setTimeout(()=>{const r=f.requests.get(f.queued.id)!;f.requests.transition(r.id,r.revision,'cancelled');},5);
 });
 try{
  const result=await processRequest(f.call,agent,f.queued,new AbortController().signal,{pollMs:5});
  assert.equal(result.state,'cancelled');assert.equal(aborted,true);assert.deepEqual(f.revoked,['grant']);
 }finally{clearInterval(keepalive);}
});
test('worker records adapter failure and revokes its credential',async()=>{
 const f=fixture();const result=await processRequest(f.call,async()=>{throw new Error('agent disconnected');},f.queued,new AbortController().signal);
 assert.equal(result.state,'failed');assert.match(result.result,/agent disconnected/);assert.deepEqual(f.revoked,['grant']);
});
