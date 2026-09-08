import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig, E2EError } from '../src/core.ts';
import { API } from '../src/api.ts';
import { activateVisible } from '../src/ui-navigation.ts';
const config=await loadConfig(process.argv[2]??'config.json');
const platform=new Platform({...config,fixture:{...config.fixture,allowChatCommands:false}}),api=new API(platform);
let runId:string|undefined,state='failed',failure:string|undefined;
const cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','single');runId=launch.runId;const id=launch.instances[0].instanceId;
 const signal=AbortSignal.timeout(60000);
 async function wait(check:(value:any)=>boolean){
  while(true){signal.throwIfAborted();try {const value=await platform.observe(id);if(check(value))return value;}
   catch(error){if(!(error instanceof E2EError)||error.code!=='INSTANCE_UNAVAILABLE')throw error;}await delay(100);}
 }
 await wait(()=>true);await platform.acquire(id);
 await activateVisible(platform,id,'btnTutorial',signal);
 await wait(value=>value.client.localPlayerReady&&value.inputContext==='Gameplay');
 await platform.releaseControl(id);
 const issued=await api.call('credentials.issue',{runId,capabilities:['observe','control'],ttlMs:1500});
 const grant=api.credentials.authenticate('Bearer '+issued.token)!;
 const started=await api.callScoped(grant,'scenario.start',{actors:{p1:id},definition:{version:'1.0',id:'credential_expiry',executionMode:'regression',participants:{p1:{networkRole:'client'}},steps:[
  {id:'hold_movement',type:'input',actor:'p1',sequence:[{operation:'press',key:'W'}]},
  {id:'wait_for_expiry',type:'wait',predicate:'ui.visible',args:{actor:'p1',automationId:'expiry_test_absent'},timeoutMs:10000}
 ]}},'Automation');
 const during=await wait(value=>value.input.w===true);
 const run=api.runner.runs.get(started.runId)!;await run.done;
 const after=await platform.observe(id);
 assert.equal(api.runner.status(started.runId).state,'cancelled');
 assert.equal(after.input.w,false);assert.equal(after.control.owner,'None');
 assert.equal(api.credentials.authenticate('Bearer '+issued.token),undefined);
 await platform.artifact(runId,'credential-expiry-evidence.json',{during,after,scenario:api.runner.status(started.runId),expiresAt:issued.expiresAt});
 state='passed';
} catch(error){failure=String(error);}
finally {
 for(const run of api.runner.runs.values())run.controller.abort();
 await Promise.allSettled([...api.runner.runs.values()].map(run=>run.done));
 try {await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const report={runId,state,failure,cleanupErrors,processes:platform.list()};
 if(runId)await platform.artifact(runId,'credential-expiry-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
