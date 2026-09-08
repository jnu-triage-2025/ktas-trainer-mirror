import assert from 'node:assert/strict';
import {Platform,loadConfig} from '../src/core.ts';
import {Runner,type Definition} from '../src/runner.ts';
import {joinInstances} from '../src/startup.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,state='failed',failure:string|undefined;
try {
 const launched=await platform.launch('mac_direct','host_plus_3_clients');runId=launched.runId;
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const runner=new Runner(platform);const checked=new Set<string>(),pressedObserved=new Set<string>();
 const originalStep=runner.step.bind(runner);
 runner.step=async(run,step,signal)=>{
  await originalStep(run,step,signal);
  if(step.type==='input'&&step.id.startsWith('press_')){
   const snapshot=await platform.observe(actors[step.actor!]);
   assert.equal(snapshot.input.w,true);pressedObserved.add(step.actor!);
  }
 };
 const originalEvidence=runner.evidence.bind(runner);
 runner.evidence=async(run,label)=>{
  if(label==='failure'){
   for(const [actor,id] of Object.entries(actors)){
    const snapshot=await platform.observe(id);
    assert.equal(snapshot.input.w,false,actor+' input must be released before evidence collection');
    checked.add(actor);await platform.artifact(runId!,actor+'-before-evidence-state.json',snapshot);
   }
  }
  await originalEvidence(run,label);
 };
 const definition:Definition={version:'1.0',id:'failure_release',executionMode:'regression',participants:Object.fromEntries(Object.entries(actors).map(([actor,id])=>[actor,{networkRole:platform.get(id).role}])),steps:[
  {id:'press_all',type:'parallel',branches:Object.keys(actors).map(actor=>[{id:'press_'+actor,type:'input',actor,sequence:[{operation:'press',key:'W'}]}])},
  {id:'intentional_failure',type:'assert',predicate:'scene.is',args:{actor:'p1',scene:'__intentional_failure__'}}
 ]};
 const run=runner.start(definition,actors);await runner.runs.get(run.runId)!.done;
 const result=runner.status(run.runId);assert.equal(result.error?.code,'ASSERTION_FAILED');assert.deepEqual(result.cleanupErrors,[]);
 assert.equal(pressedObserved.size,4);assert.equal(checked.size,4);
 for(const id of Object.values(actors))assert.equal((await platform.observe(id)).control.owner,'None');
 await platform.artifact(runId,'injected-failure-result.json',result);state='passed';
}catch(error){failure=String(error);}
finally{
 await platform.close();const report={runId,state,failure,allProcessesExited:platform.list().every(i=>i.state==='EXITED')};
 if(runId)await platform.artifact(runId,'runner-failure-release-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&report.allProcessesExited?0:1;
}
