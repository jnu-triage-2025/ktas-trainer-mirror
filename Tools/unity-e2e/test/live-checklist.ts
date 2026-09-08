import { Platform, loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
import { setTimeout as delay } from 'node:timers/promises';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { Runner } from '../src/runner.ts';
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
  const paperCommands=[];
  for(let ownerId=0;ownerId<4;ownerId++){
    const command=`/give checklist_paper 1 fish:${ownerId}`;
    await input([{operation:'tap',key:'T'}]);
    await platform.command(host,'ui.text',{text:command,mode:'input_adapter'});
    await input([{operation:'tap',key:'Return'}]);
    paperCommands.push(command);
  }
  await platform.artifact(runId,'checklist-fixture.json',{mode:'setup_bypass',commands:paperCommands});
  await platform.releaseControl(host);
  const definition=JSON.parse(await readFile('examples/role-branches-fixture.e2e.json','utf8'));
  definition.id='checklist_roles_prepared';
  definition.steps=definition.steps.slice(0,definition.steps.findIndex((step:any)=>step.id==='p4_finish_control'));
  const runner=new Runner(platform);
  const prepared=runner.start(definition,joined.actors);
  await runner.runs.get(prepared.runId)!.done;
  assert.equal(runner.status(prepared.runId).state,'passed');
  const completed=(snapshot:any)=>{
    const local=snapshot.client.players.find((p:any)=>p.local);
    const paper=local.inventory.slots.filter((slot:any)=>slot.itemId==='checklist_paper');
    assert.equal(paper.length,1);
    return paper[0].checklist.CompletedIdentifiers;
  };
  for(const id of Object.values(joined.actors))assert.deepEqual(completed(await platform.observe(id)),[]);
  await platform.acquire(host);
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
  await input([{operation:'hold',key:'S',durationMs:500}]);
  await platform.releaseControl(host);
  const client=joined.actors.p2;
  await platform.acquire(client);
  await runner.navigate(client,{id:'approach_blood',type:'navigate',target:'blood_bag',mode:'input_adapter',timeoutMs:15000,args:{targetType:'item',arrivalRadius:1.6}},AbortSignal.timeout(16000));
  await platform.command(client,'input.execute',{sequence:[{operation:'hold',key:'W',durationMs:300}]});
  const end=performance.now()+5000;
  while(!(completed(await platform.observe(client))??[]).includes('blood_bag')){
    if(performance.now()>end)throw new Error('CHECKLIST_NOT_COMPLETED');
    await delay(100);
  }
  for(const [actor,id] of Object.entries(joined.actors)){
    const snapshot=await platform.observe(id);
    await platform.artifact(runId,`${actor}-checklist-result.json`,snapshot);
    assert.deepEqual(completed(snapshot),actor==='p2'?['blood_bag']:[]);
    const count=snapshot.client.players.find((p:any)=>p.local).inventory.slots.filter((slot:any)=>slot.itemId==='blood_bag').reduce((sum:number,slot:any)=>sum+slot.count,0);
    assert.equal(count,actor==='p2'?1:0);
  }
  const result={runId,checklistIsolationPassed:true,rolePreparationRunId:prepared.runId};
  await platform.artifact(runId,'checklist-result.json',result);
  console.log(JSON.stringify(result));
} catch(error) {
  if(runId){
    await platform.artifact(runId,'checklist-error.json',{error:String(error)});
    for(const instance of platform.instances.values()){
      try{await platform.artifact(runId,`${instance.id}-failure.json`,await platform.observe(instance.id));}catch{}
    }
  }
  throw error;
} finally {await platform.close();}
