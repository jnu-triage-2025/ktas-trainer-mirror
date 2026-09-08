import assert from 'node:assert/strict';
import {readFileSync} from 'node:fs';
import {Platform,loadConfig} from '../src/core.ts';
import {API} from '../src/api.ts';
import {Runner,type Definition} from '../src/runner.ts';
import {joinInstances} from '../src/startup.ts';
const config=await loadConfig(process.argv[2]??'config.json');
config.fixture={...config.fixture,allowScenarioFixtures:true};
const platform=new Platform(config),api=new API(platform);
const definition:Definition=JSON.parse(readFileSync(new URL('../examples/four-player-core.e2e.json',import.meta.url),'utf8'));
definition.id='dedicated_four_player_dialogue';definition.topology='dedicated_plus_4_clients';
for(const participant of Object.values(definition.participants))participant.networkRole='client';
definition.steps=definition.steps.filter(step=>!['p1_mode','server_accepted'].includes(step.id));
const preparationIndex=definition.steps.findIndex(step=>step.id==='open_chat');
const uiDefinition={...definition,id:definition.id+'_ui',steps:definition.steps.slice(0,preparationIndex)};
definition.steps=definition.steps.slice(preparationIndex+3);
let runId:string|undefined, scenarioRunId:string|undefined, state='blocked', failure:string|undefined;
const cleanupErrors:string[]=[];
try {
 const launch=await platform.launch('mac_direct','dedicated_plus_4_clients');runId=launch.runId;
 const server=launch.instances.find(instance=>instance.role==='dedicated')!;assert.ok(server);
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const runner=new Runner(platform);
 const uiRun=runner.start(uiDefinition,actors).runId;await runner.runs.get(uiRun)!.done;
 const ui=runner.status(uiRun);await platform.artifact(runId,'dedicated-dialogue-ui.json',ui);
 assert.equal(ui.state,'passed',JSON.stringify(ui.error));assert.deepEqual(ui.cleanupErrors,[]);
 const target=(await platform.observe(actors.p2)).client.players.find((player:any)=>player.local).ownerId;
 await platform.acquire(actors.p1);
 await assert.rejects(platform.command(actors.p1,'fixture.scenario_start',{graphId:'e2e_authoritative_dialogue',ownerId:target}),error=>(error as any).code==='NOT_SERVER');
 await platform.releaseControl(actors.p1);
 await platform.acquire(server.instanceId);
 const credential=api.credentials.issue(runId,['control','fixture'],60000), grant=api.credentials.authenticate('Bearer '+credential.token)!;
 const prepared=await api.callScoped(grant,'fixture.scenario_start',{instanceId:server.instanceId,payload:{graphId:'e2e_authoritative_dialogue',ownerId:target}},'Automation');
 await platform.artifact(runId,'dedicated-dialogue-preparation.json',{mode:'setup_bypass',prepared,target});
 api.credentials.revoke(credential.id);await platform.releaseControl(server.instanceId);
 scenarioRunId=runner.start(definition,actors).runId;
 await runner.runs.get(scenarioRunId)!.done;
 const execution=runner.status(scenarioRunId);
 await platform.artifact(runId,'dedicated-dialogue-execution.json',execution);
 assert.equal(execution.state,'passed',JSON.stringify(execution.error));assert.deepEqual(execution.cleanupErrors,[]);
 const serverState=await platform.observe(server.instanceId);
 assert.equal(serverState.server.started,true);assert.equal(serverState.server.connections,4);
 assert.equal(serverState.scenario.executionMode,'ServerAuthoritative');
 assert.equal(serverState.scenario.side,'server');
 assert.equal(serverState.scenario.graphId,'e2e_authoritative_dialogue');
 assert.equal(serverState.scenario.nodeId,'accepted');
 const clientState=await platform.observe(actors.p2);
 assert.equal(clientState.scenario.executionMode,'ClientPresentation');
 assert.equal(clientState.scenario.nodeId,'accepted');
 await platform.artifact(runId,'dedicated-dialogue-evidence.json',{scenarioRunId,serverState,clientState});
 state='passed';
} catch(error) {
 state='failed';failure=String(error);
 if(runId) for(const instance of platform.instances.values()) {
  try{await platform.artifact(runId,`${instance.id}-dedicated-dialogue-failure.json`,await platform.observe(instance.id));}catch{}
 }
} finally {
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();
 if(processes.some(instance=>!['EXITED','START_FAILED'].includes(instance.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,scenarioRunId,state,failure,cleanupErrors,processes};
 if(runId)await platform.artifact(runId,'dedicated-dialogue-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
