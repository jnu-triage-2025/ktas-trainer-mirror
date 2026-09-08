import type { AssistanceRequest } from './requests.ts';
import type { Capability } from './run-credentials.ts';
export type WorkerCall = (tool:string,args:Record<string,unknown>)=>Promise<any>;
export type AgentContext = {request:AssistanceRequest;credential:{token:string;id:string;expiresAt:number;runId:string};};
export type RequestAgent = (context:AgentContext,signal:AbortSignal)=>Promise<string>;
export async function processRequest(call:WorkerCall,agent:RequestAgent,queued:AssistanceRequest,
  signal:AbortSignal,options:{timeoutMs?:number;pollMs?:number;capabilities?:Capability[]}={}) {
 const timeoutMs=options.timeoutMs??600000,pollMs=options.pollMs??1000;
 if(!Number.isInteger(timeoutMs)||timeoutMs<1||timeoutMs>900000||!Number.isInteger(pollMs)||pollMs<1||pollMs>60000)throw new Error('INVALID_WORKER_OPTIONS');
 signal.throwIfAborted();
 let request:AssistanceRequest;
 try {request=await call('assistance.update',{requestId:queued.id,revision:queued.revision,state:'running',timeoutMs});}
 catch(error) {if((error as any)?.code==='ASSISTANCE_STATE_CONFLICT')return {state:'not_claimed'};throw error;}
 const controller=new AbortController();
 const cancel=()=>controller.abort(signal.reason);signal.addEventListener('abort',cancel,{once:true});
 if(signal.aborted)cancel();
 const timeout=setTimeout(()=>controller.abort(new Error('AI_REQUEST_TIMEOUT')),timeoutMs);timeout.unref();
 let grant:any,timer:ReturnType<typeof setInterval>|undefined,watching=false,watch:Promise<void>|undefined;
 try {
  grant=await call('credentials.issue',{runId:request.runId,requestId:request.id,capabilities:options.capabilities??['observe','control'],ttlMs:timeoutMs});
  controller.signal.throwIfAborted();
  timer=setInterval(()=>{
   if(watching)return;watching=true;
   watch=(async()=>{
    try {const requests:AssistanceRequest[]=await call('assistance.list',{runId:request.runId});
     const current=requests.find(value=>value.id===request.id);
     if(!current||current.state!=='running'||current.revision!==request.revision)controller.abort(new Error('AI_REQUEST_NO_LONGER_RUNNING'));
    } catch(error){controller.abort(error);} finally{watching=false;}
   })();
  },pollMs);timer.unref();
  const result=await agent({request,credential:grant},controller.signal);
  controller.signal.throwIfAborted();
  if(typeof result!=='string'||!result.trim()||result.length>16000)throw new Error('INVALID_AI_RESULT');
  return await call('assistance.update',{requestId:request.id,revision:request.revision,state:'completed',result});
 } catch(error) {
  const current:AssistanceRequest|undefined=(await call('assistance.list',{runId:request.runId})).find((value:AssistanceRequest)=>value.id===request.id);
  if(current?.state==='running'&&current.revision===request.revision)
   return await call('assistance.update',{requestId:request.id,revision:request.revision,state:'failed',result:String(error).slice(0,16000)});
  return {state:current?.state??'missing'};
 } finally {
  if(timer)clearInterval(timer);clearTimeout(timeout);signal.removeEventListener('abort',cancel);
  controller.abort();await watch;
  if(grant)await call('credentials.revoke',{credentialId:grant.id});
 }
}
