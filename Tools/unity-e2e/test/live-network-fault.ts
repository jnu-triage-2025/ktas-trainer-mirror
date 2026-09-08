import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform,loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
const platform=new Platform(await loadConfig(process.argv[2]??'config.json'));
let runId:string|undefined,state='blocked',failure:string|undefined;
const cleanupErrors:string[]=[];
const normal={delayMs:0,jitterMs:0,loss:0,disconnected:false,bytesPerSecond:0};
const mode=process.argv[3]??'disconnect';
if(!['disconnect','combined'].includes(mode))throw new Error('Expected disconnect or combined fault mode');
const rule=mode==='combined'?{...normal,delayMs:100,jitterMs:40,loss:.1,bytesPerSecond:16384}:{...normal,disconnected:true};
const samples:unknown[]=[];
try{
 const launch=await platform.launch('mac_direct','host_plus_3_clients',true);runId=launch.runId;
 const {actors}=await joinInstances(platform,runId,AbortSignal.timeout(180000));
 const host=actors.p1,target=actors.p2;
 const baseline=await platform.observe(target);
 const ownerId=baseline.client.players.find((player:any)=>player.local).ownerId;
 await platform.artifact(runId,'network-baseline.json',{target:baseline,host:await platform.observe(host),proxies:Object.values(actors).slice(1).map(id=>({id,...platform.networkStatus(id)}))});
 await platform.acquire(target);
 await platform.networkFault(target,rule,mode==='combined'?6000:2000);
 // Faults affect game UDP only; authenticated HTTP input and observation remain available.
 await platform.command(target,'input.execute',{sequence:[{operation:'hold',key:'S',durationMs:500}]});
 const during=await platform.observe(target),fault=platform.networkStatus(target);
 assert.equal(fault.active,true,'The observation must complete while the UDP fault is still active');
 assert.ok(during.frame>baseline.frame,'Automation bridge stopped advancing during game-UDP loss');
 assert.ok(fault.counters.droppedFault>0,'No game datagram crossed the fault window');
 await platform.artifact(runId,'network-during-fault.json',{target:during,fault});
 const deadline=performance.now()+15000;
 let previousFrame=during.frame;
 while(platform.networkStatus(target).active){
  if(performance.now()>deadline)throw new Error('FAULT_DID_NOT_EXPIRE');
  if(mode==='combined'){
   const observed=await platform.observe(target),proxy=platform.networkStatus(target);
   assert.ok(observed.frame>previousFrame,'Main loop did not advance between fault samples');previousFrame=observed.frame;
   samples.push({frame:observed.frame,at:new Date().toISOString(),proxy});
  }
  await delay(mode==='combined'?200:50);
 }
 if(mode==='combined'){
  assert.ok(samples.length>=3,'Insufficient observations during the combined fault');
  assert.ok(samples.some((sample:any)=>sample.proxy.queuedPackets>0),'No delayed game datagrams were observed');
  assert.ok(platform.networkStatus(target).counters.sent>fault.counters.sent,'All game datagrams stopped during the combined fault');
 }
 const sentBefore=platform.networkStatus(target).counters.sent;
 await platform.command(target,'input.execute',{sequence:[{operation:'hold',key:'S',durationMs:700}]});
 const local=await platform.observe(target),position=local.client.players.find((player:any)=>player.local).position;
 const startPosition=baseline.client.players.find((player:any)=>player.local).position;
 assert.ok(Math.hypot(position[0]-startPosition[0],position[2]-startPosition[2])>1,'The movement probe did not move far enough to test replication');
 const recoveryDeadline=performance.now()+10000;
 let server:any;
 while(true){
  server=await platform.observe(host);
  const replicated=server.client.players.find((player:any)=>player.ownerId===ownerId);
  if(replicated&&Math.hypot(...position.map((value:number,index:number)=>value-replicated.position[index]))<.5)break;
  if(performance.now()>recoveryDeadline)throw new Error('POSITION_DID_NOT_RESYNCHRONIZE');await delay(100);
 }
 assert.ok(platform.networkStatus(target).counters.sent>sentBefore,'UDP forwarding did not recover');
 assert.equal(server.server.connections,4);
 for(const id of [actors.p3,actors.p4])assert.equal(platform.networkStatus(id).counters.droppedFault,0);
 await platform.artifact(runId,'network-recovered.json',{target:local,host:server,proxies:[actors.p2,actors.p3,actors.p4].map(id=>({id,...platform.networkStatus(id)}))});
 state='passed';
}catch(error){
 state='failed';failure=String(error);
 if(runId)for(const instance of platform.instances.values()){
  try{await platform.artifact(runId,`${instance.id}-network-failure.json`,{observation:await platform.observe(instance.id),network:instance.udpProxy?.snapshot()});}catch{}
 }
}finally{
 try{await platform.close();}catch(error){cleanupErrors.push(String(error));}
 const processes=platform.list();if(processes.some(instance=>!['EXITED','START_FAILED'].includes(instance.state)))cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report={runId,state,mode,rule,failure,cleanupErrors,processes};
 if(runId){await platform.artifact(runId,'network-fault-samples.json',samples);await platform.artifact(runId,'network-report.json',report);}
 console.log(JSON.stringify(report));process.exitCode=state==='passed'&&!cleanupErrors.length?0:1;
}
