import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig,E2EError} from '../src/core.ts';
const config=await loadConfig(process.argv[2]??'config.json');
config.builds.lease_test={...config.builds.mac_direct,controlLeaseMs:5000};
const platform=new Platform(config);
let runId:string|undefined,failure:string|undefined;
try {
 const launch=await platform.launch('lease_test','single');runId=launch.runId;
 const id=launch.instances[0].instanceId;
 const deadline=performance.now()+30000;
 while(true){
  try{if((await platform.observe(id)).scene==='IntroScene')break;}
  catch(error){if(!(error instanceof E2EError)||error.code!=='INSTANCE_UNAVAILABLE')throw error;}
  assert.ok(performance.now()<deadline,'TITLE_NOT_READY');await delay(150);
 }
 await platform.acquire(id);
 assert.equal((await platform.observe(id)).control.leaseMs,5000);
 await platform.command(id,'input.execute',{sequence:[{operation:'press',key:'W'}]});
 await platform.command(id,'ui.pointer',{x:20,y:20,pressed:true});
 const held=await platform.observe(id),pointer=await platform.command(id,'ui.query',{});
 assert.equal(held.input.w,true);assert.equal(pointer.inputDiagnostics.virtualMousePressed,true);
 await delay(2500);
 const released=await platform.observe(id),releasedPointer=await platform.command(id,'ui.query',{});
 assert.equal(released.control.owner,'Automation');assert.equal(released.control.controlEpoch,held.control.controlEpoch);
 assert.equal(released.input.w,false);assert.equal(releasedPointer.inputDiagnostics.requestedPressed,false);
 assert.equal(releasedPointer.inputDiagnostics.virtualMousePressed,false);
 await platform.artifact(runId,'input-expired-with-live-owner.json',{released,pointer:releasedPointer.inputDiagnostics});
 const instance=platform.get(id);if(instance.heartbeat)clearInterval(instance.heartbeat);instance.heartbeat=undefined;
 await delay(5500);
 const expired=await platform.observe(id);
 assert.equal(expired.control.owner,'None');assert.ok(expired.control.controlEpoch>held.control.controlEpoch);
 await assert.rejects(platform.command(id,'input.execute',{sequence:[{operation:'press',key:'W'}]}),error=>error instanceof E2EError&&['CONTROL_DENIED','STALE_CONTROL_EPOCH'].includes(error.code));
 await platform.artifact(runId,'owner-expired.json',expired);
}catch(error){failure=String(error);}
finally{
 await platform.close();
 const report={runId,state:failure?'failed':'passed',failure,processes:platform.list()};
 if(runId)await platform.artifact(runId,'input-lease-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=failure?1:0;
}
