import { Platform, loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
import { Runner } from '../src/runner.ts';
import { setTimeout as delay } from 'node:timers/promises';
import assert from 'node:assert/strict';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
try {
  const launched=await platform.launch('mac','host_plus_3_clients');
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
  const runner=new Runner(platform),run={actors:joined.actors} as any;
  // This is a new fixture phase after evidence collection; explicitly acquire a fresh lease.
  await platform.releaseControl(host);
  await platform.acquire(host);
  // The world-space fixture places P2 near the tutorial NPC; navigation itself still uses normal input.
  await tap(host,'T');
  await platform.command(host,'ui.text',{text:'/tp fish:1 76.43 1 19.6',mode:'input_adapter'});await tap(host,'Return');
  await platform.artifact(launched.runId,'npc-approach-fixture.json',{mode:'setup_bypass',command:'/tp fish:1 76.43 1 19.6',reason:'Prepare a nearby starting position for the existing tutorial NPC'});
  await platform.releaseControl(client);
  await platform.acquire(client);
  await runner.navigate(client,{id:'approach',type:'navigate',target:'npc-tutorial-guide-hat',mode:'input_adapter',timeoutMs:15000,args:{targetType:'npc',arrivalRadius:1.5}},AbortSignal.timeout(20000));
  await platform.artifact(launched.runId,'before-interaction.json',await platform.observe(client));
  await runner.interact(client,{id:'talk',type:'interact',target:'npc-tutorial-guide-hat__interaction-talk-start',mode:'input_adapter'},AbortSignal.timeout(10000));
  const choiceEnd=performance.now()+15000;
  while(true){const state=await platform.observe(client);if(state.dialogue.hasChoices&&!state.dialogue.isTextAnimating)break;if(performance.now()>choiceEnd)throw new Error('Choice not ready');await delay(200);}
  await platform.artifact(launched.runId,'choice-visible.json',await platform.observe(client));
  await runner.step(run,{id:'choose',type:'dialogueChoose',actor:'p2',choiceId:'D_TUT_HAT_STARTING_NO_1'},AbortSignal.timeout(10000));
  const result=await platform.observe(client);
  assert.equal(result.scenario.nodeId,'D_TUT_HAT_STARTING_NO_1');
  const events=await platform.command(host,'events.read');
  assert.ok(events.events.some((event:any)=>event.eventType==='signal.registered'&&event.payload.signalId==='sig.tutorial_hat_talk_start'));
  await platform.artifact(launched.runId,'choice-result.json',result);
  await platform.artifact(launched.runId,'choice-screen.json',await platform.command(client,'game.screenshot'));
  console.log(JSON.stringify({runId:launched.runId,interactionAndChoicePassed:true}));

} catch(error) {
  for(const instance of platform.instances.values()) {
    try{await platform.artifact(instance.runId,`${instance.id}-failure.json`,await platform.observe(instance.id));}catch{}
    try{await platform.artifact(instance.runId,`${instance.id}-failure-screen.json`,await platform.command(instance.id,'game.screenshot',{}, {ttlMs:2000}));}catch{}
  }
  throw error;
} finally {await platform.close();}
