import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig} from '../src/core.ts';
const config=await loadConfig(process.argv[2]??'config.json');
config.fixture={...config.fixture,allowScenarioFixtures:false,allowProtocolTests:false};
const platform=new Platform(config);
let runId:string|undefined,state='failed',failure:string|undefined;
const cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','single');runId=launch.runId;
 const id=launch.instances[0].instanceId,deadline=performance.now()+30000;
 while(true){try{await platform.observe(id);break;}catch(error){if(performance.now()>deadline)throw error;await delay(200);}}
 await platform.acquire(id);
 const before=await platform.observe(id);
 // Call the Unity bridge directly: the service-side option check must not be the only guard.
 await assert.rejects(platform.command(id,'fixture.scenario_start',{graphId:'e2e_authoritative_dialogue',ownerId:0}),error=>(error as any).code==='FIXTURE_DISABLED');
 await assert.rejects(platform.command(id,'protocol.signal_raise',{signalId:'sig.e2e.protocol.exact',count:1}),error=>(error as any).code==='PROTOCOL_TESTS_DISABLED');
 const after=await platform.observe(id);
 assert.equal(after.scene,before.scene);assert.equal(after.scenario.graphId,before.scenario.graphId);
 assert.equal(after.scenario.state,before.scenario.state);
 await platform.artifact(runId,'fixture-disabled-evidence.json',{before,after,expectedErrors:['FIXTURE_DISABLED','PROTOCOL_TESTS_DISABLED'],scope:'Direct Unity bridge rejection without service API guard'});
 state='passed';
} catch(error){failure=String(error);}
finally {
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();if(processes.some(i=>!['EXITED','START_FAILED'].includes(i.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,state,failure,cleanupErrors,processes};
 if(runId)await platform.artifact(runId,'fixture-disabled-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
