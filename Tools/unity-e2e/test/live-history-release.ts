import assert from 'node:assert/strict';
import {Platform,loadConfig} from '../src/core.ts';
import {API} from '../src/api.ts';
import {activateVisible} from '../src/ui-navigation.ts';
const p=new Platform(await loadConfig(process.argv[2]??'config.json')),api=new API(p);let runId,state='failed',failure;
try{
 const launched=await p.launch('mac_direct','single');runId=launched.runId;const id=launched.instances[0].instanceId,signal=AbortSignal.timeout(90000);
 async function wait(check:(state:any)=>boolean){while(true){signal.throwIfAborted();try{const s=await p.observe(id);if(check(s))return s;}catch(e:any){if(e.code!=='INSTANCE_UNAVAILABLE')throw e;}await new Promise(r=>setTimeout(r,200));}}
 await wait(s=>s.scene==='IntroScene');await p.acquire(id);await activateVisible(p,id,'btnPlay',signal);await activateVisible(p,id,'btnHost',signal);await wait(s=>s.client.localPlayerReady);
 await p.command(id,'input.execute',{sequence:[{operation:'press',key:'W'}]});
 const before=await p.observe(id);assert.equal(before.input.w,true);await p.artifact(runId,'release-before.json',before);
 const previous=(p as any).store;(p as any).store={assertHealthy(){throw new Error('HISTORY_STORAGE_FAILED_TEST');},append(){throw new Error('HISTORY_STORAGE_FAILED_TEST');},async close(){}};await previous?.close();
 await assert.rejects(p.command(id,'input.execute',{sequence:[{operation:'press',key:'W'}]}),/HISTORY_STORAGE_FAILED_TEST/);
 const released=await api.call('control.emergency_stop',{instanceId:id});assert.ok(released.historyErrors?.length);
 const after=await p.observe(id,true);assert.equal(after.input.w,false);assert.equal(after.control.owner,'None');
 await p.artifact(runId,'release-after.json',after);await p.artifact(runId,'release-command-result.json',released);state='passed';
}catch(error){failure=String(error);}finally{await p.close();if(runId)await p.artifact(runId,'history-release-report.json',{state,failure,processes:p.list()});console.log(JSON.stringify({runId,state,failure,allProcessesExited:p.list().every(i=>i.state==='EXITED')}));process.exitCode=state==='passed'?0:1;}
