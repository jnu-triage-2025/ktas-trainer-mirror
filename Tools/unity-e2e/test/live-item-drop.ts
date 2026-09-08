import { Platform, loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
import { setTimeout as delay } from 'node:timers/promises';
import assert from 'node:assert/strict';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined;
try {
  const launched=await platform.launch('mac_direct','host_plus_3_clients');runId=launched.runId;
  const joined=await joinInstances(platform,runId,AbortSignal.timeout(180000));
  const host=joined.actors.p1;
  await platform.acquire(host);
  const input=(sequence:any[])=>platform.command(host,'input.execute',{sequence});
  await input([{operation:'hold',key:'W',durationMs:1000}]);
  await input([{operation:'tap',key:'T'}]);
  await platform.command(host,'ui.text',{text:'/give blood_bag 1 fish:0',mode:'input_adapter'});
  await input([{operation:'tap',key:'Return'}]);
  await platform.artifact(runId,'fixture.json',{mode:'setup_bypass',command:'/give blood_bag 1 fish:0',purpose:'Prepare an item to drop with normal gameplay input'});
  const before=await platform.observe(host);
  await platform.artifact(runId,'before-drop.json',before);
  assert.equal(before.client.players.find((p:any)=>p.local).inventory.slots.filter((s:any)=>s.itemId==='blood_bag').reduce((n:number,s:any)=>n+s.count,0),1);
  await input([{operation:'tap',key:'Alpha1'},{operation:'tap',key:'Q'}]);
  const samples=[];
  for(let i=0;i<10;i++){samples.push(await platform.observe(host));await delay(100);}
  await platform.artifact(runId,'after-drop-samples.json',samples);
  const last=samples.at(-1)!;
  const remaining=last.client.players.find((p:any)=>p.local).inventory.slots.filter((s:any)=>s.itemId==='blood_bag').reduce((n:number,s:any)=>n+s.count,0);
  const worldItems=last.worldItems.filter((item:any)=>item.itemId==='blood_bag');
  console.log(JSON.stringify({runId,remaining,worldItems}));
  assert.equal(remaining,0,'Dropped item returned to the inventory without movement');
  assert.equal(worldItems.length,1,'Dropped item must remain available for another player');
} catch(error) {
  if(runId)await platform.artifact(runId,'drop-error.json',{error:String(error)});
  throw error;
} finally {await platform.close();}
