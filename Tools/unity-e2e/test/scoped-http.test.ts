import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,writeFile,rm} from 'node:fs/promises';
import {join} from 'node:path';
import {tmpdir} from 'node:os';
import {spawn} from 'node:child_process';
import {createServer} from 'node:net';
import {once} from 'node:events';
import {setTimeout as delay} from 'node:timers/promises';
test('HTTP run credentials are scoped, revocable and expire without exposing tokens in logs',async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-scoped-http-'));
 const reservation=createServer();reservation.listen(0,'127.0.0.1');await once(reservation,'listening');
 const port=(reservation.address() as {port:number}).port;await new Promise<void>(resolve=>reservation.close(()=>resolve()));
 const executable=join(root,'fake-player');await writeFile(executable,'#!/usr/bin/env node\nsetInterval(()=>{},1000);\n',{mode:0o700});
 const config=join(root,'config.json');await writeFile(config,JSON.stringify({port,builds:{fake:{executable}},artifactRoot:join(root,'artifacts')}));
 const operator='a'.repeat(64);
 const child=spawn(process.execPath,[new URL('../src/main.ts',import.meta.url).pathname,config],{env:{...process.env,E2E_CONSOLE_TOKEN:operator,E2E_CREDENTIAL_EXPIRES_AT:undefined},stdio:['ignore','ignore','pipe']});
 let logs='';child.stderr.on('data',chunk=>logs+=chunk);const exited=once(child,'exit');
 const origin=`http://127.0.0.1:${port}`;
 const call=async(token:string,tool:string,args:unknown={})=>{
  const response=await fetch(origin+'/api',{method:'POST',headers:{authorization:'Bearer '+token,'content-type':'application/json'},body:JSON.stringify({tool,args})});
  const text=await response.text();return {status:response.status,body:text?JSON.parse(text):null};
 };
 try {
  const deadline=performance.now()+15000;
  while(true){try{await fetch(origin);break;}catch{assert.ok(performance.now()<deadline);await delay(25);}}
  const one=(await call(operator,'instances.launch',{buildId:'fake',topology:'single'})).body.result;
  const two=(await call(operator,'instances.launch',{buildId:'fake',topology:'single'})).body.result;
  assert.ok(one.runId);assert.ok(two.runId);
  const issued=(await call(operator,'credentials.issue',{runId:one.runId,capabilities:['observe'],ttlMs:5000})).body.result;
  assert.ok(issued.token);
  const listed=await call(issued.token,'instances.list');assert.equal(listed.status,200);
  assert.deepEqual(listed.body.result.map((i:any)=>i.runId),[one.runId]);
  for(const [tool,args] of [['game.observe',{instanceId:two.instances[0].instanceId}],['instances.stop',{instanceId:one.instances[0].instanceId}],['credentials.issue',{runId:one.runId,capabilities:['control'],ttlMs:1000}]] as const)
   assert.equal((await call(issued.token,tool,args)).status,403);
  assert.equal((await call(operator,'credentials.revoke',{credentialId:issued.id})).body.result.revoked,true);
  assert.equal((await call(issued.token,'instances.list')).status,401);
  const short=(await call(operator,'credentials.issue',{runId:one.runId,capabilities:['observe'],ttlMs:100})).body.result;
  await delay(150);assert.equal((await call(short.token,'instances.list')).status,401);
  assert.ok(!logs.includes(issued.token));assert.ok(!logs.includes(short.token));assert.ok(!logs.includes(operator));
 } finally {child.kill('SIGTERM');await exited;await rm(root,{recursive:true,force:true});}
});
