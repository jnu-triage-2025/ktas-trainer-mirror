import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {Runner,validate} from '../src/runner.ts';
import {joinInstances} from '../src/startup.ts';
import {activateVisible} from '../src/ui-navigation.ts';
const p=new Platform(await loadConfig(process.argv[2]??'config.json'));
const sleep=(ms:number)=>new Promise(resolve=>setTimeout(resolve,ms));
let runId:string|undefined,state='failed',failure:string|undefined;
const samples:any[]=[];
try{
 const four=process.argv[3]?.startsWith('four')??false,look=process.argv[3]==='four-look';
 const launched=await p.launch('mac_direct',four?'host_plus_3_clients':'single');runId=launched.runId;
 const signal=AbortSignal.timeout(180000);
 const actors=four?(await joinInstances(p,runId,signal)).actors:{p1:launched.instances[0].instanceId};
 const id=four?actors.p2:actors.p1;
 async function wait(check:(v:any)=>boolean){while(true){signal.throwIfAborted();try{if(check(await p.observe(id)))return;}catch(e){if((e as any).code!=='INSTANCE_UNAVAILABLE')throw e;}await sleep(100);}}
 if(four)await p.acquire(id);else{
  await wait(v=>v.scene==='IntroScene');await p.acquire(id);
  await activateVisible(p,id,'btnPlay',signal);await activateVisible(p,id,'btnHost',signal);await wait(v=>v.client.localPlayerReady);
 }
 const recording=await p.recordingStart(id);
 const input=(operation:string,key:string)=>p.command(id,'input.execute',{sequence:[{operation,key}]});
 await input('press','W');await sleep(250);await input('press','A');await sleep(150);
 if(look)await p.command(id,'input.execute',{sequence:[{operation:'lookDelta',x:12,y:0}]});
 await sleep(200);
 await input('release','W');await sleep(250);await input('release','A');
 const saved=await p.recordingStop(recording.recordingId),draft=JSON.parse(await readFile(saved.draftPath,'utf8'));
 assert.deepEqual(draft.blockers,[]);assert.equal(validate(draft.definition).valid,true);
 assert.ok(draft.definition.steps.some((s:any)=>s.sequence?.some((op:any)=>op.operation==='wait')));
 await p.releaseControl(id);
 const beforeReplay=await p.observe(id);
 const runner=new Runner(p),run=runner.start(draft.definition,{p1:id}),task=runner.runs.get(run.runId)!;
 let finished=false;const done=task.done!.finally(()=>{finished=true;});
 const capture=async()=>{
  const observed=await Promise.all(Object.values(actors).map(async instanceId=>({instanceId,snapshot:await p.observe(instanceId)})));
  const v=observed.find(value=>value.instanceId===id)!.snapshot;
  samples.push({frame:v.frame,input:v.input,camera:v.camera,others:observed.filter(value=>value.instanceId!==id).map(value=>({instanceId:value.instanceId,frame:value.snapshot.frame,input:value.snapshot.input}))});
 };
 while(!finished){await capture();await sleep(20);}
 await done;await capture();const result=runner.status(run.runId);
 assert.equal(result.state,'passed');assert.deepEqual(result.cleanupErrors,[]);
 const states=samples.map(s=>[s.input.w,s.input.horizontal<0]);
 let cursor=0;for(const expected of [[true,false],[true,true],[false,true],[false,false]]){
  const next=states.findIndex((actual,index)=>index>=cursor&&actual[0]===expected[0]&&actual[1]===expected[1]);
  assert.ok(next>=cursor,'Missing ordered chord state '+JSON.stringify(expected));cursor=next+1;
 }
 if(look){
  assert.ok(draft.definition.steps.some((s:any)=>s.sequence?.some((op:any)=>op.operation==='lookDelta')));
  const yaw=beforeReplay.camera.eulerAngles[1];
  assert.ok(samples.some(sample=>sample.input.w&&Math.abs(((sample.camera.eulerAngles[1]-yaw+540)%360)-180)>0.5),'Camera must turn while W is held');
 }
 if(four){
  for(const other of Object.values(actors).filter(value=>value!==id)){
   const rows=samples.flatMap(sample=>sample.others.filter((value:any)=>value.instanceId===other));
   assert.ok(new Set(rows.map(row=>row.frame)).size>=3,'Insufficient peer frame coverage');
   for(const row of rows){assert.equal(row.input.w,false);assert.equal(row.input.horizontal,0);assert.equal(row.input.vertical,0);}
  }
  for(const other of Object.values(actors)){
   const v=await p.observe(other);
   if(other!==id){assert.equal(v.input.w,false);assert.equal(v.input.horizontal,0);assert.equal(v.input.vertical,0);}
   await p.artifact(runId,'chord-final-'+other+'.json',v);
  }
  const host=await p.observe(actors.p1);assert.equal(host.server.connections,4);
 }
 await p.artifact(runId,'chord-replay-samples.json',samples);await p.artifact(runId,'chord-replay-result.json',result);state='passed';
}catch(error){failure=String(error);}
finally{if(runId)await p.artifact(runId,'chord-replay-samples.json',samples);await p.close();const report={runId,state,failure,allProcessesExited:p.list().every(i=>i.state==='EXITED')};if(runId)await p.artifact(runId,'chord-report.json',report);console.log(JSON.stringify(report));process.exitCode=state==='passed'&&report.allProcessesExited?0:1;}
