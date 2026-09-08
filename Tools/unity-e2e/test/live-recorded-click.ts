import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {Runner,validate} from '../src/runner.ts';
import {activateVisible,waitForInteractable} from '../src/ui-navigation.ts';
const p=new Platform(await loadConfig(process.argv[2]??'config.json'));
const sleep=(ms:number)=>new Promise(resolve=>setTimeout(resolve,ms));
const settings=process.argv[3]==='settings',button=settings?'btnSettings':'btnPlay';
let runId:string|undefined,state='failed',failure:string|undefined;
try{
 const launched=await p.launch('mac_direct','single');runId=launched.runId;
 const id=launched.instances[0].instanceId,signal=AbortSignal.timeout(60000);
 while(true){signal.throwIfAborted();try{if((await p.observe(id)).scene==='IntroScene')break;}catch(e){if((e as any).code!=='INSTANCE_UNAVAILABLE')throw e;}await sleep(100);}
 await p.acquire(id);
 const target=await waitForInteractable(p,id,button,signal);
 assert.ok(target,'Play button must be interactable');
 const recording=await p.recordingStart(id);
 const payload={...target.screenCenter,screenWidth:target.screenWidth,screenHeight:target.screenHeight,frame:target.uiRevision};
 const down=await p.command(id,'ui.pointer',{...payload,pressed:true});
 const up=await p.command(id,'ui.pointer',{...payload,pressed:false});
 await p.artifact(runId,'click-targets.json',{down,up});
 assert.equal(down.target?.automationId,button);assert.deepEqual(up.target,down.target);
 const saved=await p.recordingStop(recording.recordingId),draft=JSON.parse(await readFile(saved.draftPath,'utf8'));
 assert.deepEqual(draft.blockers,[]);assert.equal(validate(draft.definition).valid,true);
 assert.equal(draft.definition.steps.filter((s:any)=>s.type==='uiAction'&&s.automationId===button).length,1);
 await activateVisible(p,id,settings?'close-button':'btnPlayBack',signal);await p.releaseControl(id);
 draft.definition.steps.push({id:'verify_play_menu',type:'wait',predicate:'ui.visible',args:{actor:'p1',automationId:settings?'settings-root':'btnHost'},timeoutMs:10000});
 const runner=new Runner(p),run=runner.start(draft.definition,{p1:id});await runner.runs.get(run.runId)!.done;
 const result=runner.status(run.runId);await p.artifact(runId,'click-replay-result.json',result);
 assert.equal(result.state,'passed');assert.deepEqual(result.cleanupErrors,[]);state='passed';
}catch(error){failure=String(error);}
finally{await p.close();const report={runId,state,failure,allProcessesExited:p.list().every(i=>i.state==='EXITED')};if(runId)await p.artifact(runId,'click-report.json',report);console.log(JSON.stringify(report));process.exitCode=state==='passed'&&report.allProcessesExited?0:1;}
