import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {joinInstances} from '../src/startup.ts';
import {Runner} from '../src/runner.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,state='failed',failure:string|undefined;
try {
 const launched=await platform.launch('mac_direct','host_plus_3_clients');runId=launched.runId;
 const joined=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const diagnostics=[];
 for(const [actor,id] of Object.entries(joined.actors)){
  await platform.acquire(id);
  // Create both devices with a normal pointer release before inspecting their runtime flags.
  await platform.command(id,'ui.pointer',{x:0,y:0,pressed:false});
  const ui=await platform.command(id,'ui.query',{});
  diagnostics.push({actor,...ui.inputDiagnostics});
  assert.equal(ui.inputDiagnostics.virtualMouseCanRunInBackground,true,actor+' mouse background support');
  assert.equal(ui.inputDiagnostics.virtualKeyboardCanRunInBackground,true,actor+' keyboard background support');
  await platform.releaseControl(id);
 }
 await platform.artifact(runId,'background-device-diagnostics.json',diagnostics);
 const definition=JSON.parse(await readFile(new URL('../examples/four-player-core.e2e.json',import.meta.url),'utf8'));
 const runner=new Runner(platform),run=runner.start(definition,joined.actors);
 await runner.runs.get(run.runId)!.done;
 assert.equal(runner.status(run.runId).state,'passed');
 state='passed';
} catch(error){failure=String(error);}
finally {
 await platform.close();
 const allProcessesExited=platform.list().every(i=>i.state==='EXITED');
 const report={runId,state,failure,allProcessesExited};
 if(runId)await platform.artifact(runId,'background-device-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&allProcessesExited?0:1;
}
