import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform, loadConfig} from '../src/core.ts';
import {joinInstances} from '../src/startup.ts';
import {waitForInteractable} from '../src/ui-navigation.ts';

const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined;
try {
  const launched=await platform.launch(process.argv[3]??'mac_direct_15fps_10s_lease','host_plus_3_clients');
  runId=launched.runId;
  const joined=await joinInstances(platform,runId,AbortSignal.timeout(180000));
  const host=joined.actors.p1;
  await platform.acquire(host);
  const tap=(key:string)=>platform.command(host,'input.execute',{sequence:[{operation:'tap',key}]});
  for(const item of ['humidifier_bottle','sterile_distilled_water','flowmeter']) {
    await tap('T');
    await platform.command(host,'ui.text',{text:`/give ${item} 1 fish:0`,mode:'input_adapter'});
    await tap('Return');
    await delay(100);
  }
  const before=await platform.observe(host,false,{includeStaticItems:false});
  await platform.artifact(runId,'crafting-before.json',before);
  await tap(before.inputBindings.inventory);
  await delay(500);
  const all=await platform.command(host,'ui.query',{});
  await platform.artifact(runId,'crafting-ui.json',all);
  const first=await platform.command(host,'ui.query',{automationId:'Recipe_humidifier_sterile_distilled_water_bottle'});
  await platform.artifact(runId,'crafting-first-recipe.json',first);
  assert.equal(first.elements.length,1,'Humidifier recipe was not rendered');
  const click=async(name:string)=>{
    const target:any=await waitForInteractable(platform,host,name,AbortSignal.timeout(20000));
    const pointer={...target.screenCenter,screenWidth:target.screenWidth,screenHeight:target.screenHeight,frame:target.uiRevision};
    await platform.command(host,'ui.pointer',{...pointer,pressed:true});
    await platform.command(host,'ui.pointer',{...pointer,pressed:false});
  };
  await click('Recipe_humidifier_sterile_distilled_water_bottle');
  await click('Recipe_humidifier_sterile_distilled_water_bottle');
  await click('Slot_3');
  const after=await platform.observe(host,false,{includeStaticItems:false});
  await platform.artifact(runId,'crafting-after.json',after);
  assert.ok(after.client.players.find((entry:any)=>entry.local)?.inventory?.slots
    ?.some((slot:any)=>slot.itemId==='humidifier_sterile_distilled_water_bottle'),'Crafted result was not stored');
  console.log(JSON.stringify({runId,recipes:all.elements.filter((entry:any)=>entry.automationId?.startsWith('Recipe_')),first,crafted:true}));
} finally {
  await platform.close();
}
