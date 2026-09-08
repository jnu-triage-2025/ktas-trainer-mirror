import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { createServer } from 'node:http';
import { Platform } from '../src/core.ts';

test('bridge client sends authenticated envelope and reports errors without retry',async()=>{
  const messages:any[]=[];
  const server=createServer(async(req,res)=>{
    let data='';for await(const chunk of req)data+=chunk;
    const body=JSON.parse(data);messages.push(body);
    assert.equal(req.headers.authorization,`Bearer ${'x'.repeat(32)}`);
    assert.equal(req.headers.connection, 'close');
    res.setHeader('content-type','application/json');
    res.end(JSON.stringify(body.type==='game.observe'?{ok:true,controlEpoch:3,result:{frame:10,control:{owner:'None',controlEpoch:3},client:{localPlayerReady:false}}}:{ok:false,error:{code:'STALE_CONTROL_EPOCH'},bridgeTimings:{queueMs:12,processingMs:3}}));
  });
  await new Promise<void>(done=>server.listen(0,'127.0.0.1',done));
  const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-bridge-'))});
  try{
    const address=server.address();assert.ok(address&&typeof address!=='string');
    await p.attach({instanceId:'one',runId:'run',port:address.port,token:'x'.repeat(32)});
    await assert.rejects(p.command('one','input.execute',{sequence:[]}),/STALE_CONTROL_EPOCH/);
    assert.equal(messages.length,2);assert.equal(messages[1].controlEpoch,3);assert.equal(messages[1].runId,'run');
    const failure=(await p.eventHistory('one')).findLast(entry=>entry.kind==='command'&&entry.body.ok===false)!.body;
    assert.equal(failure.error.code,'STALE_CONTROL_EPOCH');
    assert.deepEqual(failure.bridgeTimings,{queueMs:12,processingMs:3});
    assert.ok(Date.parse(failure.startedAt)<=Date.parse(failure.at));
    assert.ok(failure.transportTimings.headersWaitMs>=0);
    assert.ok(failure.transportTimings.bodyReadMs>=0);
    assert.equal(p.list()[0].state,'GAME_READY');assert.ok(!JSON.stringify(p.list()).includes('x'.repeat(32)));
  }finally{await p.close();await new Promise<void>(done=>server.close(()=>done()));}
});

test('a lost mutation response is recorded as unknown and never replayed',async()=>{
  let mutations=0;
  const server=createServer(async(req,res)=>{
    let data='';for await(const chunk of req)data+=chunk;
    const body=JSON.parse(data);
    if(body.type==='game.observe')res.end(JSON.stringify({ok:true,controlEpoch:1,result:{frame:1,control:{owner:'None',controlEpoch:1},client:{}}}));
    else {mutations++;req.socket.destroy();}
  });
  await new Promise<void>(done=>server.listen(0,'127.0.0.1',done));
  const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-lost-response-'))});
  try {
    const address=server.address();assert.ok(address&&typeof address!=='string');
    await p.attach({instanceId:'one',runId:'run',port:address.port,token:'x'.repeat(32)});
    await assert.rejects(p.command('one','input.execute',{sequence:[{operation:'tap',key:'W'}]}),/fetch failed/);
    assert.equal(mutations,1);
    const event=(await p.eventHistory('one')).at(-1)!.body;
    assert.equal(event.outcome,'unknown');assert.equal(event.error.code,'INSTANCE_UNAVAILABLE');
    assert.equal(event.command.type,'input.execute');
  } finally {await p.close();await new Promise<void>(done=>server.close(()=>done()));}
});

test('HTTP queue rejection is retained as evidence without replaying input',async()=>{
  let rejected=0;
  const server=createServer(async(req,res)=>{
    let data='';for await(const chunk of req)data+=chunk;
    if(JSON.parse(data).type==='game.observe')res.end(JSON.stringify({ok:true,controlEpoch:1,result:{frame:1,control:{owner:'None',controlEpoch:1},client:{}}}));
    else {rejected++;res.writeHead(429);res.end();}
  });
  await new Promise<void>(done=>server.listen(0,'127.0.0.1',done));
  const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-http-rejection-'))});
  try {
    const address=server.address();assert.ok(address&&typeof address!=='string');
    await p.attach({instanceId:'one',runId:'run',port:address.port,token:'x'.repeat(32)});
    await assert.rejects(p.command('one','input.execute',{sequence:[]}),/429/);
    assert.equal(rejected,1);
    const event=(await p.eventHistory('one')).at(-1)!.body;
    assert.equal(event.error.httpStatus,429);assert.equal(event.outcome,'unknown');
  } finally {await p.close();await new Promise<void>(done=>server.close(()=>done()));}
});

test('heartbeat acknowledgement does not wait for a delayed audit write',async()=>{
 const server=createServer(async(req,res)=>{for await(const _ of req){}res.end(JSON.stringify({ok:true,controlEpoch:7,result:{owner:'Automation'}}));});
 await new Promise<void>(done=>server.listen(0,'127.0.0.1',done));
 const address=server.address();assert.ok(address&&typeof address!=='string');
 const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-heartbeat-write-'))});
 let finish!:()=>void;const pending=new Promise<void>(resolve=>{finish=resolve;});let writes=0;
 (p as any).store={assertHealthy(){},append(){writes++;return pending;},async close(){await pending;}};
 p.instances.set('one',{id:'one',runId:'run',nodeId:'local',role:'client',endpoint:`http://127.0.0.1:${address.port}`,token:'test',profile:'',state:'READY',epoch:7,owner:'Automation'});
 let timer:ReturnType<typeof setTimeout>|undefined;
 try {
   const result=await Promise.race([p.command('one','control.heartbeat',{}, {epoch:7}),new Promise((_,reject)=>{timer=setTimeout(()=>reject(new Error('Heartbeat waited on storage')),1000);})]);
   assert.deepEqual(result,{owner:'Automation'});assert.equal(writes,1);assert.equal(p.get('one').epoch,7);
 } finally {if(timer)clearTimeout(timer);finish();p.instances.clear();await p.close();await new Promise<void>(done=>server.close(()=>done()));}
});

test('failed history storage still permits input release and reports the storage failure',async()=>{
 const messages:any[]=[];
 const server=createServer(async(req,res)=>{
  let data='';for await(const chunk of req)data+=chunk;const body=JSON.parse(data);messages.push(body);
  res.end(JSON.stringify(body.type==='game.observe'
   ?{ok:true,controlEpoch:1,result:{frame:1,control:{owner:'Automation',controlEpoch:1},client:{}}}
   :{ok:true,controlEpoch:2,result:{owner:'None',controlEpoch:2}}));
 });
 await new Promise<void>(done=>server.listen(0,'127.0.0.1',done));
 const address=server.address();assert.ok(address&&typeof address!=='string');
 const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-release-history-'))});
 p.instances.set('one',{id:'one',runId:'run',nodeId:'local',role:'client',token:'x'.repeat(32),profile:'',endpoint:`http://127.0.0.1:${address.port}`,state:'READY',epoch:1,owner:'Automation'});
 (p as any).store={assertHealthy(){throw new Error('HISTORY_STORAGE_FAILED');},append(){throw new Error('HISTORY_STORAGE_FAILED');},async close(){}};
 try{
  await assert.rejects(p.command('one','input.execute',{sequence:[]}),/HISTORY_STORAGE_FAILED/);
  assert.equal(messages.length,0);
  const released=await p.releaseControl('one');
  assert.equal(released.owner,'None');assert.match(released.historyErrors[0],/HISTORY_STORAGE_FAILED/);
  assert.deepEqual(messages.map(message=>message.type),['game.observe','control.handoff']);
  assert.equal(messages[1].payload.owner,'None');assert.equal(p.get('one').owner,'None');
  await assert.rejects(p.command('one','control.acquire',{}, {releaseRead:true}),/INVALID_ARGUMENT/);
 }finally{await p.close();await new Promise<void>(done=>server.close(()=>done()));}
});


test('invalid bridge response retains unknown outcome without exposing response contents',async()=>{
 let mutations=0;
 const server=createServer(async(req,res)=>{
  let data='';for await(const chunk of req)data+=chunk;
  const body=JSON.parse(data);
  if(body.type==='game.observe')res.end(JSON.stringify({ok:true,controlEpoch:1,result:{frame:1,control:{owner:'None',controlEpoch:1},client:{}}}));
  else {mutations++;res.end('private-response-content invalid json');}
 });
 await new Promise<void>(done=>server.listen(0,'127.0.0.1',done));
 const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-invalid-body-'))});
 try {
  const address=server.address();assert.ok(address&&typeof address!=='string');
  await p.attach({instanceId:'one',runId:'run',port:address.port,token:'x'.repeat(32)});
  await assert.rejects(p.command('one','input.execute',{sequence:[]}),error=>(error as any).code==='BRIDGE_INVALID_RESPONSE');
  const event=(await p.eventHistory('one')).at(-1)!.body;
  assert.equal(event.outcome,'unknown');assert.equal(event.error.code,'BRIDGE_INVALID_RESPONSE');
  assert.ok(event.transportTimings.bodyReadMs>=0);assert.equal(mutations,1);
  assert.ok(!JSON.stringify(event).includes('private-response-content'));
 } finally {await p.close();await new Promise<void>(done=>server.close(()=>done()));}
});
