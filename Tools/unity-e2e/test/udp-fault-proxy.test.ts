import test from 'node:test';
import assert from 'node:assert/strict';
import { createSocket, type Socket } from 'node:dgram';
import { setTimeout as delay, setImmediate as nextTurn } from 'node:timers/promises';
import { UdpFaultProxy } from '../src/udp-fault-proxy.ts';
const rule={delayMs:0,jitterMs:0,loss:0,disconnected:false,bytesPerSecond:0};
const bind=(socket:Socket)=>new Promise<void>(resolve=>socket.bind(0,'127.0.0.1',resolve));
const close=(socket:Socket)=>new Promise<void>(resolve=>socket.close(()=>resolve()));
async function fixture(){
 const server=createSocket('udp4'),client=createSocket('udp4');await bind(server);await bind(client);
 server.on('message',(message,peer)=>server.send(message,peer.port,peer.address));
 const address=server.address();assert.notEqual(typeof address,'string');
 const proxy=await UdpFaultProxy.create((address as {port:number}).port,123);
 const received:string[]=[];client.on('message',message=>received.push(message.toString()));
 const send=(message:string)=>client.send(message,proxy.port,'127.0.0.1',error=>{if(error)throw error;});
 const wait=async(predicate:()=>boolean)=>{const end=performance.now()+5000;while(!predicate()){if(performance.now()>end)throw new Error('TEST_DEADLINE');await delay(5);}};
 return {proxy,received,send,wait,close:async()=>{await proxy.close();await close(client);await close(server);}};
}
test('UDP proxy forwards real datagrams, injects loss, and recovers after expiry',async()=>{
 const f=await fixture();try{
  f.send('baseline');await f.wait(()=>f.received.includes('baseline'));
  f.proxy.configure({...rule,loss:1},100);
  f.send('lost');await f.wait(()=>f.proxy.snapshot().counters.droppedFault===1);
  assert.ok(!f.received.includes('lost'));
  await delay(120);f.send('recovered');await f.wait(()=>f.received.includes('recovered'));
  assert.equal(f.proxy.snapshot().active,false);
 }finally{await f.close();}
});
test('UDP delay affects both directions and shutdown clears delayed traffic',async()=>{
 const f=await fixture();try{
  f.proxy.configure({...rule,delayMs:40},5000);
  const started=performance.now();f.send('delayed');await f.wait(()=>f.received.includes('delayed'));
  assert.ok(performance.now()-started>=70);
  f.proxy.configure({...rule,delayMs:1000},5000);f.send('pending');
  await f.wait(()=>f.proxy.snapshot().queuedPackets>0);
  await f.proxy.close();assert.equal(f.proxy.snapshot().queuedPackets,0);assert.equal(f.proxy.snapshot().queuedBytes,0);
 }finally{await f.close();}
});
test('invalid fault settings are rejected before changing the active rule',async()=>{
 const f=await fixture();try{
  for(const invalid of [{...rule,loss:1.1},{...rule,jitterMs:-1},{...rule,bytesPerSecond:1}])assert.throws(()=>f.proxy.configure(invalid,1000),/INVALID_FAULT_RULE/);
  assert.equal(f.proxy.snapshot().active,false);
 }finally{await f.close();}
});
test('UDP proxy rejects a second sender without replacing the established peer',async()=>{
 const f=await fixture(),other=createSocket('udp4');await bind(other);
 try{
  f.send('owner');await f.wait(()=>f.received.includes('owner'));
  other.send('intruder',f.proxy.port,'127.0.0.1');
  await f.wait(()=>f.proxy.snapshot().counters.rejectedPeer===1);
  f.send('still-owner');await f.wait(()=>f.received.includes('still-owner'));
  assert.deepEqual(f.received,['owner','still-owner']);
 }finally{await close(other);await f.close();}
});
test('UDP bandwidth limit spaces payload delivery and rejects excessive future backlog',async()=>{
 const f=await fixture();try{
  f.proxy.configure({...rule,bytesPerSecond:1024},10000);
  const started=performance.now();
  f.send('a'.repeat(1024));f.send('b'.repeat(1024));
  await f.wait(()=>f.received.length===2);
  assert.ok(performance.now()-started>=900,'1024-byte payloads must be spaced at 1024 bytes/sec');
  f.send('c'.repeat(8192));f.send('backlog');
  await f.wait(()=>f.proxy.snapshot().counters.droppedQueue>0);
  const state=f.proxy.snapshot();
  assert.ok(state.queuedPackets<=256);assert.ok(state.queuedBytes<=1024*1024);
 }finally{await f.close();}
});
test('UDP delayed traffic has a bounded byte queue under a burst',async()=>{
 const f=await fixture();try{
  f.proxy.configure({...rule,delayMs:1000},10000);
  // Pace sends so the operating system receive buffer does not hide proxy admission.
  for(let index=0;index<140;index++){
   f.send(String(index).padEnd(8192,'x'));
   const deadline=performance.now()+5000;
   while(f.proxy.snapshot().counters.received<index+1){assert.ok(performance.now()<deadline);await nextTurn();}
  }
  const state=f.proxy.snapshot();
  assert.ok(state.counters.droppedQueue>0);
  assert.ok(state.queuedBytes<=1024*1024);assert.ok(state.queuedPackets<=256);
  await f.proxy.close();assert.equal(f.proxy.snapshot().queuedBytes,0);
 }finally{await f.close();}
});
