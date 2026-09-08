import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp,writeFile,readFile } from 'node:fs/promises';
import { join } from 'node:path';
import { tmpdir } from 'node:os';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform } from '../src/core.ts';
import { API } from '../src/api.ts';
test('launch routes only opted-in clients through individual UDP proxies and closes them',async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-network-')),script=join(root,'fake-player.cjs');
 await writeFile(script,`require('node:fs').writeFileSync(process.env.UNITY_E2E_PROFILE+'/port.json',JSON.stringify(process.env.UNITY_E2E_GAME_PORT));setInterval(()=>{},1000);`);
 const platform=new Platform({builds:{fake:{executable:process.execPath,args:[script]}},artifactRoot:root});
 const api=new API(platform);
 try{
  const launched=await api.call('instances.launch',{buildId:'fake',topology:'host_plus_3_clients',networkProxy:true});
  const instances=[...platform.instances.values()],host=instances[0],clients=instances.slice(1);
  assert.equal(host.proxyPort,undefined);assert.equal(new Set(clients.map(i=>i.proxyPort)).size,3);
  for(const instance of instances){
    let seen:string|undefined;const deadline=performance.now()+10000;
    while(seen===undefined){try{seen=JSON.parse(await readFile(join(instance.profile,'port.json'),'utf8'));}catch{if(performance.now()>deadline)throw new Error('CHILD_START_TIMEOUT');await delay(10);}}
    assert.equal(Number(seen),instance.proxyPort??launched.gamePort);
  }
  const rule={delayMs:20,jitterMs:5,loss:0,disconnected:false,bytesPerSecond:0};
  await api.call('network.fault',{instanceId:clients[0].id,rule,durationMs:1000});
  assert.equal((await api.call('network.status',{instanceId:clients[0].id})).active,true);
  assert.equal((await api.call('network.status',{instanceId:clients[1].id})).active,false);
  await assert.rejects(api.call('network.fault',{instanceId:host.id,rule,durationMs:1000}),/NETWORK_PROXY_UNAVAILABLE/);
  assert.ok((await platform.eventHistory(clients[0].id)).some(entry=>entry.kind==='network_fault'));
 }finally{await platform.close();}
 for(const instance of platform.instances.values()){
  assert.equal(instance.state,'EXITED');
  if(instance.udpProxy){assert.equal(instance.udpProxy.snapshot().active,false);assert.equal(instance.udpProxy.snapshot().queuedPackets,0);}
 }
});
