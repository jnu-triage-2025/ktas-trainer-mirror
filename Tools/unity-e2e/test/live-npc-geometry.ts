import { Platform, loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
import { setTimeout as delay } from 'node:timers/promises';
import assert from 'node:assert/strict';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
try {
  const launched=await platform.launch('mac_direct','host_plus_3_clients');
  const joined=await joinInstances(platform,launched.runId,AbortSignal.timeout(180000));
  const host=joined.actors.p1, client=joined.actors.p2;
  const catalogue=await platform.command(host,'game.catalogue');
  assert.ok(catalogue.graphs.some((g:any)=>g.graphId==='tutorial'));
  await platform.artifact(launched.runId,'catalogue.json',catalogue);
  for(const id of Object.values(joined.actors))await platform.acquire(id);
  const tap=(id:string,key:string)=>platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]});
  await tap(host,'T');
  const chatState=await platform.observe(host);
  if (chatState.inputContext !== 'ChatUIController') throw new Error(`Chat did not open: ${chatState.inputContext}`);
  await platform.command(host,'ui.text',{text:'/scenario execute @a tutorial',mode:'input_adapter'});
  await platform.artifact(launched.runId,'chat-input.json',await platform.command(host,'ui.query'));
  await tap(host,'Return');
  await platform.artifact(launched.runId,'content-setup.json',{mode:'setup_bypass',path:'normal_chat_input',command:'/scenario execute @a tutorial'});
  const end=performance.now()+15000;
  while((await platform.observe(host)).scenario.graphId!=='tutorial'){
    if(performance.now()>end)throw new Error('Tutorial did not start');await delay(200);
  }
  await platform.releaseControl(client);
  await platform.acquire(client);
  const before=await platform.observe(client);
  await platform.command(client,'input.execute',{sequence:[{operation:'lookDelta',x:10,y:0},{operation:'hold',key:'W',durationMs:1500},{operation:'hold',key:'S',durationMs:1500}]},{ttlMs:10000});
  const after=await platform.observe(client);
  console.log(JSON.stringify({runId:launched.runId,before:before.scenario,after:after.scenario,dialogue:after.dialogue}));
  for(const [name,id] of Object.entries(joined.actors)) {
    await platform.artifact(launched.runId,`${name}-content-state.json`,await platform.observe(id));
    await platform.artifact(launched.runId,`${name}-content-events.json`,await platform.command(id,'events.read'));
  }
  for(const [actor,id] of Object.entries(joined.actors)) {
    const observation=await platform.observe(id);
    await platform.artifact(launched.runId,`${actor}-npc-ground.json`,observation);
    console.log(JSON.stringify({actor,entities:observation.entities,localPlayer:observation.client.players.find((player:any)=>player.local)?.position}));
  }
} finally {await platform.close();}
