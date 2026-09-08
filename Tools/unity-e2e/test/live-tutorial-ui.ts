import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform,loadConfig,E2EError} from '../src/core.ts';
import {Runner} from '../src/runner.ts';
import {activateVisible} from '../src/ui-navigation.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,state='failed',failure:string|undefined;
try {
 const launch=await platform.launch('mac_direct','single');runId=launch.runId;
 const id=launch.instances[0].instanceId;
 async function wait(check:(value:any)=>boolean,timeout=30000){
  const deadline=performance.now()+timeout;
  while(true){try{const value=await platform.observe(id);if(check(value))return value;}catch(error){if(!(error instanceof E2EError)||error.code!=='INSTANCE_UNAVAILABLE')throw error;}
   if(performance.now()>deadline)throw new Error('TUTORIAL_STATE_DEADLINE');await delay(150);}
 }
 await wait(value=>value.scene==='IntroScene');await platform.acquire(id);
 await activateVisible(platform,id,'btnTutorial',AbortSignal.timeout(20000));
 const ready=await wait(value=>value.client.localPlayerReady&&value.scenario.graphId==='tutorial');
 await platform.artifact(runId,'tutorial-natural-entry.json',ready);
 const npc=ready.entities.find((entity:any)=>entity.id==='npc-tutorial-guide-hat');
 assert.ok(npc?.groundProbe?.found,'Tutorial NPC has no ground in the actual tutorial scene');
 await platform.command(id,'input.execute',{sequence:[{operation:'lookDelta',x:10,y:0},{operation:'hold',key:'W',durationMs:1500},{operation:'hold',key:'S',durationMs:1500}]},{ttlMs:10000});
 await wait(value=>value.scenario.nodeId==='V_TUT_HAT_TALK_START');
 const hints=await platform.command(id,'ui.query',{});
 await platform.artifact(runId,'tutorial-interaction-hint-ui.json',hints);
 assert.ok(hints.elements.some((element:any)=>element.visible&&element.text?.includes('모자에게 가까이 다가가세요.')),'Tutorial interaction hint was not visible');
 const catalogue=await platform.command(id,'game.catalogue');
 assert.equal(catalogue.eventHandlerDiagnostics.find((entry:any)=>entry.eventIdentifier==='show_tutorial_interaction_hint')?.handlerRegistered,true);
 await platform.artifact(runId,'tutorial-interaction-hint-screen.json',await platform.command(id,'game.screenshot'));
 const runner=new Runner(platform);
 await runner.navigate(id,{id:'walk_to_guide',type:'navigate',target:'npc-tutorial-guide-hat',mode:'input_adapter',timeoutMs:30000,args:{targetType:'npc',arrivalRadius:1.5}},AbortSignal.timeout(35000));
 await platform.artifact(runId,'tutorial-before-interaction.json',await platform.observe(id));
 await runner.interact(id,{id:'talk_to_guide',type:'interact',target:'npc-tutorial-guide-hat__interaction-talk-start',mode:'input_adapter'},AbortSignal.timeout(10000));
 await wait(value=>value.dialogue.nodeId==='C_TUT_GREETING'&&value.dialogue.hasChoices&&!value.dialogue.isTextAnimating);
 await runner.step({actors:{p1:id}} as any,{id:'choose_no',type:'dialogueChoose',actor:'p1',choiceId:'C_TUT_GREETING#2'},AbortSignal.timeout(10000));
 const result=await wait(value=>value.scenario.nodeId==='D_TUT_HAT_STARTING_NO_1');
 await platform.artifact(runId,'tutorial-choice-result.json',result);
 await platform.artifact(runId,'tutorial-choice-screen.json',await platform.command(id,'game.screenshot'));
 state='passed';
}catch(error){failure=String(error);if(runId)for(const instance of platform.instances.values()){
 try{await platform.artifact(runId,'tutorial-failure.json',await platform.observe(instance.id));}catch{}
 try{await platform.artifact(runId,'tutorial-failure-screen.json',await platform.command(instance.id,'game.screenshot'));}catch{}
}}finally{
 await platform.close();const report={runId,state,failure,processes:platform.list()};
 if(runId)await platform.artifact(runId,'tutorial-ui-report.json',report);
 console.log(JSON.stringify(report));process.exitCode=state==='passed'?0:1;
}
