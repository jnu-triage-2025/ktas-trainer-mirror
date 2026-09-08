import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig,E2EError} from '../src/core.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,state='failed',failure:string|undefined;
const cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','single');runId=launch.runId;
 const id=launch.instances[0].instanceId;
 const deadline=performance.now()+30000;
 while(true){
  try{if((await platform.observe(id)).scene==='IntroScene')break;}
  catch(error){if(!(error instanceof E2EError)||error.code!=='INSTANCE_UNAVAILABLE')throw error;}
  assert.ok(performance.now()<deadline,'Title readiness deadline');await delay(150);
 }
 await platform.acquire(id);
 const shot=await platform.command(id,'game.screenshot');
 await platform.command(id,'ui.pointer',{x:20,y:20,pressed:true,screenWidth:shot.width,screenHeight:shot.height,frame:shot.frame});
 const pressed=await platform.command(id,'ui.query',{});
 await platform.artifact(runId,'pointer-pressed.json',pressed);
 assert.equal(pressed.inputDiagnostics.requestedPressed,true);
 // An incompatible frame must reject the positional request, including pointerup.
 await assert.rejects(platform.command(id,'ui.pointer',{x:20,y:20,pressed:false,screenWidth:shot.width,screenHeight:shot.height,frame:2147483647}),error=>error instanceof E2EError&&error.code==='STATE_CONFLICT');
 await platform.release(id);
 const released=await platform.command(id,'ui.query',{});
 await platform.artifact(runId,'pointer-released.json',released);
 assert.equal(released.inputDiagnostics.requestedPressed,false);
 assert.equal(released.inputDiagnostics.virtualMousePressed,false);
 state='passed';
}catch(error){failure=String(error);}
finally{
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));state='failed';}
 const report={runId,state,failure,cleanupErrors,processes:platform.list()};
 if(runId)await platform.artifact(runId,'pointer-release-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'?0:1;
}
