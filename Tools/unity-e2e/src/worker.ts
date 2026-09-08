import {spawn} from 'node:child_process';
import {setTimeout as delay} from 'node:timers/promises';
import {processRequest,type RequestAgent,type WorkerCall} from './request-worker.ts';
import {serviceUrl} from './service-url.ts';
import type {AssistanceRequest} from './requests.ts';
const [executable,...args]=process.argv.slice(2);
if(!executable)throw new Error('Usage: node src/worker.ts AGENT_EXECUTABLE [ARGS...]');
const endpoint=serviceUrl(process.env.E2E_SERVICE_URL??'http://127.0.0.1:17890');
const timeoutMs=Number(process.env.E2E_WORKER_TIMEOUT_MS??600000);
if(!Number.isInteger(timeoutMs)||timeoutMs<1||timeoutMs>900000)throw new Error('INVALID_WORKER_TIMEOUT');
const token=process.env.E2E_CONSOLE_TOKEN;
if(!token||token.length<32)throw new Error('E2E_CONSOLE_TOKEN is required');
const call:WorkerCall=async(tool,args)=>{
 const response=await fetch(endpoint+'/api',{method:'POST',redirect:'error',headers:{Authorization:`Bearer ${token}`,'Content-Type':'application/json'},
  body:JSON.stringify({tool,args,caller:'Automation'}),signal:AbortSignal.timeout(15000)});
 const body=await response.json();if(!response.ok||!body.ok)throw Object.assign(new Error(body.error?.message??'Worker service request failed'),{code:body.error?.code??'SERVICE_HTTP_ERROR'});
 return body.result;
};
const agent:RequestAgent=async(context,signal)=>new Promise((resolve,reject)=>{
 signal.throwIfAborted();
 const child=spawn(executable,args,{shell:false,stdio:['pipe','pipe','pipe'],env:{...process.env,E2E_SERVICE_URL:endpoint,E2E_CONSOLE_TOKEN:context.credential.token}});
 let output='',bytes=0,failure:Error|undefined,killTimer:ReturnType<typeof setTimeout>|undefined;
 const stop=()=>{child.kill('SIGTERM');killTimer??=setTimeout(()=>child.kill('SIGKILL'),2000);killTimer.unref();};
 const abort=()=>{failure=new Error('AI_EXECUTION_ABORTED');stop();};
 signal.addEventListener('abort',abort,{once:true});if(signal.aborted)abort();
 child.stdout.setEncoding('utf8');
 child.stdout.on('data',chunk=>{bytes+=Buffer.byteLength(chunk);if(bytes>65536){failure=new Error('AI_OUTPUT_TOO_LARGE');stop();}else output+=chunk.toString();});
 child.stderr.resume();
 child.stdin.on('error',()=>{});
 child.on('error',error=>{failure=error;});
 child.on('close',code=>{
  signal.removeEventListener('abort',abort);if(killTimer)clearTimeout(killTimer);
  if(failure)return reject(failure);if(code!==0){let reason=`AI_PROCESS_EXIT_${code}`;try{const parsed=JSON.parse(output);if(typeof parsed.error==='string')reason=parsed.error.slice(0,16000);}catch{}return reject(new Error(reason));}
  try {const result=JSON.parse(output);if(typeof result.result!=='string')throw new Error('INVALID_AI_RESULT');resolve(result.result);}
  catch(error){reject(error);}
 });
 child.stdin.end(JSON.stringify({version:1,request:context.request,serviceUrl:endpoint,
  instructions:'Use the run-scoped E2E MCP credential from the environment. Bind scenario.start to request.id. Complete the requested work and any handoff before returning JSON {"result":"..."}. Do not update assistance state; the worker owns that lifecycle.'})+'\n');
});
const stop=new AbortController();for(const signal of ['SIGINT','SIGTERM'] as const)process.once(signal,()=>stop.abort());
while(!stop.signal.aborted){
 const requests:AssistanceRequest[]=await call('assistance.list',{});const queued=requests.find(value=>value.state==='queued');
 if(queued){const result=await processRequest(call,agent,queued,stop.signal,{timeoutMs,capabilities:process.env.E2E_WORKER_ALLOW_FIXTURE==='1'?['observe','control','fixture']:['observe','control']});
  process.stdout.write(JSON.stringify({requestId:queued.id,state:result.state})+'\n');}
 else try{await delay(1000,undefined,{signal:stop.signal});}catch{if(!stop.signal.aborted)throw new Error('WORKER_WAIT_FAILED');}
}
