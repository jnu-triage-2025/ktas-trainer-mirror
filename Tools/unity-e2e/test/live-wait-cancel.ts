import assert from 'node:assert/strict';
import {Platform,loadConfig} from '../src/core.ts';
import {activateVisible} from '../src/ui-navigation.ts';
const p=new Platform(await loadConfig(process.argv[2]??'config.json'));
const sleep=(ms:number)=>new Promise(resolve=>setTimeout(resolve,ms));
let runId:string|undefined,state='failed',failure:string|undefined;
const samples:any[]=[];
try{
 const launched=await p.launch('mac_direct','single');runId=launched.runId;
 const id=launched.instances[0].instanceId,signal=AbortSignal.timeout(90000);
 async function wait(check:(v:any)=>boolean){while(true){signal.throwIfAborted();try{if(check(await p.observe(id)))return;}catch(e){if((e as any).code!=='INSTANCE_UNAVAILABLE')throw e;}await sleep(100);}}
 await wait(v=>v.scene==='IntroScene');await p.acquire(id);
 await activateVisible(p,id,'btnPlay',signal);await activateVisible(p,id,'btnHost',signal);await wait(v=>v.client.localPlayerReady);
 for(const stop of ['release_all','handoff']){
  const started=performance.now();
  const command=p.command(id,'input.execute',{sequence:[{operation:'press',key:'W'},{operation:'wait',durationMs:1500},{operation:'press',key:'A'}]})
    .then(()=>({ok:true,code:undefined}),e=>({ok:false,code:e.code}));
  await wait(v=>v.input.w===true);
  if(stop==='release_all')await p.command(id,'input.release_all');else await p.handoff(id,'None');
  const result=await command;assert.equal(result.ok,false);assert.equal(result.code,'CANCELLED');
  // Observe beyond the abandoned wait: its following A press must never execute.
  while(performance.now()-started<1900){
   const v=await p.observe(id);samples.push({stop,frame:v.frame,input:v.input,owner:v.control.owner});
   assert.equal(v.input.w,false);assert.equal(v.input.horizontal,0);
   if(stop==='handoff')assert.equal(v.control.owner,'None');await sleep(40);
  }
 }
 state='passed';
}catch(error){failure=String(error);}
finally{if(runId)await p.artifact(runId,'wait-cancel-samples.json',samples);await p.close();const report={runId,state,failure,allProcessesExited:p.list().every(i=>i.state==='EXITED')};if(runId)await p.artifact(runId,'wait-cancel-report.json',report);console.log(JSON.stringify(report));process.exitCode=state==='passed'&&report.allProcessesExited?0:1;}
