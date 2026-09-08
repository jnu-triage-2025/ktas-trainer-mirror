import { Platform } from '../src/core.ts';
import { resolve } from 'node:path';
import { writeFile } from 'node:fs/promises';
import { setTimeout as delay } from 'node:timers/promises';
const root=resolve(import.meta.dirname,'../../..');
const platform=new Platform({builds:{mac:{executable:resolve(root,'Build/E2E.app/Contents/MacOS/KTASTrainer')}},artifactRoot:resolve(root,'artifacts/unity-e2e')});
async function until(fn:()=>Promise<boolean>,timeout=60000){const end=Date.now()+timeout;while(Date.now()<end){try{if(await fn())return;}catch{}await delay(500);}throw new Error('Live condition timed out');}
async function click(id:string,name:string){await until(async()=>{const q=await platform.command(id,'ui.query',{automationId:name});return q.elements.some((e:any)=>e.interactable);},10000);await platform.command(id,'ui.activate',{automationId:name,mode:'device_input'});}
try{
 const launch=await platform.launch('mac','host_plus_3_clients'); const ids=launch.instances.map(i=>i.instanceId);console.log('run',launch.runId);
 for(const id of ids){await until(async()=>!!(await platform.observe(id)));await platform.acquire(id);}
 console.log('all bridges ready');
 await click(ids[0],'btnPlay');await click(ids[0],'btnHost');
 await until(async()=>(await platform.observe(ids[0])).server.started);console.log('host started');
 for(const id of ids.slice(1)){await click(id,'btnPlay');await click(id,'btnDirectConnect');await click(id,'btnDirectJoin');}
 for(const id of ids){await until(async()=>{const value=await platform.observe(id);return value.client.localPlayerReady&&value.client.players.length===4;},90000);}
 for (const id of ids) { const state = await platform.observe(id); if (state.control.owner === 'None') await platform.acquire(id); }
 const before=await Promise.all(ids.map(id=>platform.observe(id)));console.log('four players ready',JSON.stringify(before.map(v=>({id:v.instanceId,server:v.server,players:v.client.players}))));
 await platform.command(ids[1],'input.execute',{sequence:[{operation:'lookDelta',x:20,y:0},{operation:'press',key:'W'}]},{ttlMs:5000});
 await delay(300); console.log('during input',JSON.stringify(await platform.observe(ids[1])));
 await delay(400); await platform.release(ids[1]);
 await delay(800);const after=await Promise.all(ids.map(id=>platform.observe(id)));
 const localBefore=before[1].client.players.find((p:any)=>p.local),localAfter=after[1].client.players.find((p:any)=>p.local);
 const distance=Math.hypot(localAfter.position[0]-localBefore.position[0],localAfter.position[2]-localBefore.position[2]);
 console.log('movement',JSON.stringify({distance,before:localBefore,after:localAfter}));
 await platform.artifact(launch.runId,'multiplayer-smoke.json',{before,after,distance});
 if(distance<.1)throw new Error('P2 did not move horizontally via input adapter');
 const shot=await platform.command(ids[1],'game.screenshot');await writeFile(resolve(root,'artifacts/unity-e2e/multiplayer-smoke.jpg'),Buffer.from(shot.data,'base64'));
}finally{await platform.close();}
