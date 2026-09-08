import assert from 'node:assert/strict';
import {Platform,loadConfig} from '../src/core.ts';
import {Runner,type Definition} from '../src/runner.ts';
import {activateVisible} from '../src/ui-navigation.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,state='failed',failure:string|undefined;
try {
 const launched=await platform.launch('mac_direct','single');runId=launched.runId;
 const id=launched.instances[0].instanceId,signal=AbortSignal.timeout(90000);
 async function wait(check:(snapshot:any)=>boolean){while(true){signal.throwIfAborted();try{if(check(await platform.observe(id)))return;}catch(error){if((error as any).code!=='INSTANCE_UNAVAILABLE')throw error;}await new Promise(resolve=>setTimeout(resolve,200));}}
 await wait(snapshot=>snapshot.scene==='IntroScene');await platform.acquire(id);
 await activateVisible(platform,id,'btnPlay',signal);await activateVisible(platform,id,'btnHost',signal);
 await wait(snapshot=>snapshot.client.localPlayerReady);await platform.releaseControl(id);
 const runner=new Runner(platform);let checked=false,pressedObserved=false;
 const originalStep=runner.step.bind(runner);
 runner.step=async(run,step,signal)=>{
  await originalStep(run,step,signal);
  if(step.id==='press'){
   const snapshot=await platform.observe(id);
   assert.equal(snapshot.input.w,true);pressedObserved=true;
  }
 };
 const originalEvidence=runner.evidence.bind(runner);
 runner.evidence=async(run,label)=>{
  if(label==='failure'){
   const snapshot=await platform.observe(id);
   assert.equal(snapshot.input.w,false,'Input must already be released when evidence collection starts');
   checked=true;await platform.artifact(runId!,'before-evidence-state.json',snapshot);
  }
  await originalEvidence(run,label);
 };
 const definition:Definition={version:'1.0',id:'failure_release',executionMode:'regression',participants:{p1:{networkRole:'client'}},steps:[
  {id:'press',type:'input',actor:'p1',sequence:[{operation:'press',key:'W'}]},
  {id:'intentional_failure',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'__intentional_failure__'}}
 ]};
 const run=runner.start(definition,{p1:id});await runner.runs.get(run.runId)!.done;
 const result=runner.status(run.runId);assert.equal(result.error?.code,'ASSERTION_FAILED');assert.deepEqual(result.cleanupErrors,[]);
 assert.ok(pressedObserved&&checked);assert.equal((await platform.observe(id)).control.owner,'None');
 await platform.artifact(runId,'injected-failure-result.json',result);state='passed';
}catch(error){failure=String(error);}
finally{
 await platform.close();const report={runId,state,failure,allProcessesExited:platform.list().every(i=>i.state==='EXITED')};
 if(runId)await platform.artifact(runId,'runner-failure-release-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&report.allProcessesExited?0:1;
}
