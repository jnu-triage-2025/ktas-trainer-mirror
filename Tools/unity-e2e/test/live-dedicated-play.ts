import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig} from '../src/core.ts';
import {Runner,type Definition} from '../src/runner.ts';
import {joinInstances} from '../src/startup.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
const definition:Definition=JSON.parse(readFileSync(new URL('../examples/four-player-ui-isolation.e2e.json',import.meta.url),'utf8'));
definition.id='dedicated_four_player_ui_isolation';definition.topology='dedicated_plus_4_clients';
for(const participant of Object.values(definition.participants))participant.networkRole='client';
let runId:string|undefined, uiRunId:string|undefined, state='blocked', failure:string|undefined;
const cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','dedicated_plus_4_clients');runId=launch.runId;
 const server=launch.instances.find(instance=>instance.role==='dedicated')!;assert.ok(server);
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const runner=new Runner(platform);uiRunId=runner.start(definition,actors).runId;
 await runner.runs.get(uiRunId)!.done;
 const ui=runner.status(uiRunId);assert.equal(ui.state,'passed');assert.deepEqual(ui.cleanupErrors,[]);
 const before=await platform.observe(actors.p2), local=before.client.players.find((player:any)=>player.local);
 assert.ok(local);await platform.acquire(actors.p2);
 await platform.command(actors.p2,'input.execute',{sequence:[{operation:'hold',key:'S',durationMs:1000}]});
 await platform.releaseControl(actors.p2);
 const moved=await platform.observe(actors.p2), destination=moved.client.players.find((player:any)=>player.local).position;
 const distance=Math.hypot(destination[0]-local.position[0],destination[2]-local.position[2]);assert.ok(distance>1,`P2 moved only ${distance}`);
 const deadline=performance.now()+15000;
 const replicas:Record<string,unknown>={};
 for(const [actor,id] of Object.entries(actors)) {
  while(true) {
   const observed=await platform.observe(id),replica=observed.client.players.find((player:any)=>player.ownerId===local.ownerId);
   const error=replica?Math.hypot(...destination.map((value:number,index:number)=>value-replica.position[index])):Infinity;
   if(observed.client.localPlayerReady&&observed.client.players.length===4&&error<.5){replicas[actor]={observed,positionError:error};break;}
   assert.ok(performance.now()<deadline,`${actor} failed to replicate P2 movement`);await delay(100);
  }
 }
 const first=await platform.observe(server.instanceId);await delay(1000);const second=await platform.observe(server.instanceId);
 assert.equal(second.server.started,true);assert.equal(second.server.connections,4);
 assert.ok(second.server.tick>first.server.tick);assert.ok(second.frame>first.frame);
 await platform.artifact(runId,'dedicated-play-evidence.json',{uiRunId,before,moved,distance,replicas,serverBefore:first,serverAfter:second});
 state='passed';
} catch(error) {
 state='failed';failure=String(error);
 if(runId) for(const instance of platform.instances.values()) {
  try{await platform.artifact(runId,`${instance.id}-dedicated-play-failure.json`,await platform.observe(instance.id));}catch{}
 }
} finally {
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();
 if(processes.some(instance=>!['EXITED','START_FAILED'].includes(instance.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,uiRunId,state,failure,cleanupErrors,processes};
 if(runId)await platform.artifact(runId,'dedicated-play-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
