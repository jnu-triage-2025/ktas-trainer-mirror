import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,writeFile,rm} from 'node:fs/promises';
import {join} from 'node:path';
import {tmpdir} from 'node:os';
import {spawn, type ChildProcess} from 'node:child_process';
import {createServer} from 'node:net';
import {once} from 'node:events';
import {setTimeout as delay} from 'node:timers/promises';
test('service restart exposes interrupted requests and rejects late AI completion', async () => {
 const root=await mkdtemp(join(tmpdir(),'e2e-requests-http-'));
 const reservation=createServer(); reservation.listen(0,'127.0.0.1'); await once(reservation,'listening');
 const port=(reservation.address() as {port:number}).port;
 await new Promise<void>(resolve=>reservation.close(()=>resolve()));
 const executable=join(root,'fake-player');
 await writeFile(executable,'#!/usr/bin/env node\nsetInterval(()=>{},1000);\n',{mode:0o700});
 const config=join(root,'config.json');
 await writeFile(config,JSON.stringify({port,builds:{fake:{executable}},artifactRoot:join(root,'artifacts')}));
 const token='c'.repeat(64), origin=`http://127.0.0.1:${port}`;
 let child:ChildProcess|undefined, exited:Promise<unknown>|undefined;
 const start=async()=>{
  child=spawn(process.execPath,[new URL('../src/main.ts',import.meta.url).pathname,config],{env:{...process.env,E2E_CONSOLE_TOKEN:token,E2E_CREDENTIAL_EXPIRES_AT:undefined},stdio:'ignore'});
  exited=once(child,'exit');
  const deadline=performance.now()+15000;
  while(true){try{await fetch(origin,{signal:AbortSignal.timeout(1000)});break;}catch{assert.ok(performance.now()<deadline);assert.equal(child.exitCode,null);await delay(25);}}
 };
 const stop=async()=>{child?.kill('SIGTERM');await exited;child=undefined;};
 const call=async(tool:string,args:unknown)=>{
  const response=await fetch(origin+'/api',{method:'POST',headers:{authorization:'Bearer '+token,'content-type':'application/json'},body:JSON.stringify({tool,args,caller:'Automation'}),signal:AbortSignal.timeout(5000)});
  return response.json();
 };
 try {
  await start();
  const launch=await call('instances.launch',{buildId:'fake',topology:'single'});assert.equal(launch.ok,true);
  const {runId,instances}=launch.result;
  const submitted=await call('assistance.submit',{instanceId:instances[0].instanceId,prompt:'Open settings'});
  assert.equal(submitted.ok,true);const requestId=submitted.result.id;
  assert.equal((await call('assistance.update',{requestId,revision:0,state:'running'})).ok,true);
  await stop();await start();
  const restored=await call('assistance.list',{runId});assert.equal(restored.ok,true);
  assert.equal(restored.result.length,1);assert.equal(restored.result[0].state,'failed');assert.equal(restored.result[0].revision,2);
  assert.match(restored.result[0].result,/재시작/);
  const late=await call('assistance.update',{requestId,revision:1,state:'completed',result:'late success'});
  assert.equal(late.ok,false);assert.equal(late.error.code,'ASSISTANCE_STATE_CONFLICT');
  assert.equal((await call('assistance.list',{runId})).result[0].state,'failed');
 } finally {await stop();await rm(root,{recursive:true,force:true});}
});
