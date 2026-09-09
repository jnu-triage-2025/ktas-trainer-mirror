import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,writeFile,readFile,stat,rm} from 'node:fs/promises';
import {join} from 'node:path';
import {tmpdir} from 'node:os';
import {spawn} from 'node:child_process';
import {createServer} from 'node:net';
import {once} from 'node:events';
import {setTimeout as delay} from 'node:timers/promises';
import {request as httpRequest} from 'node:http';
import {fileURLToPath} from 'node:url';

test('HTTP observer cannot elevate caller or mutate and startup logs omit credentials',async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-access-'));
 const reservation=createServer();reservation.listen(0,'127.0.0.1');await once(reservation,'listening');
 const port=(reservation.address() as {port:number}).port;await new Promise<void>(resolve=>reservation.close(()=>resolve()));
 const config=join(root,'config.json');await writeFile(config,JSON.stringify({port,builds:{},artifactRoot:join(root,'artifacts')}));
 const operator='o'.repeat(64),observer='v'.repeat(64);
 const expiresAt=Date.now()+5000;
 const child=spawn(process.execPath,[fileURLToPath(new URL('../src/main.ts',import.meta.url)),config],{env:{...process.env,E2E_CONSOLE_TOKEN:operator,E2E_OBSERVER_TOKEN:observer,E2E_CREDENTIAL_EXPIRES_AT:String(expiresAt)},stdio:['ignore','ignore','pipe']});
 let logs='';child.stderr.on('data',chunk=>logs+=chunk.toString());const exited=once(child,'exit');
 const origin=`http://127.0.0.1:${port}`;
 const call=(token:string,tool:string,caller='RemoteHuman')=>fetch(origin+'/api',{method:'POST',headers:{authorization:'Bearer '+token,'content-type':'application/json'},body:JSON.stringify({tool,caller,args:{}})});
 try{
  const deadline=performance.now()+15000;
  while(true){try{await fetch(origin);break;}catch{assert.ok(performance.now()<deadline,'service startup timeout');await delay(25);}}
  assert.equal((await call(observer,'instances.list')).status,200);
  for(const tool of ['instances.launch','input.execute','scenario.start','network.fault']){
   const response=await call(observer,tool,'Automation');assert.equal(response.status,403);assert.equal((await response.json()).error.code,'PERMISSION_DENIED');
  }
  assert.equal((await call(operator,'instances.list')).status,200);
  assert.equal((await call('invalid','instances.list')).status,401);
  const linkFile=join(root,'artifacts','console-access.txt');
  assert.equal((await readFile(linkFile,'utf8')).trim(),origin+'/#'+operator);
  if(process.platform!=='win32')assert.equal((await stat(linkFile)).mode&0o777,0o600);
  assert.ok(!logs.includes(operator));assert.ok(!logs.includes(observer));
  // Start an authenticated request before expiry, but finish its body afterwards.
  const slow=httpRequest(origin+'/api',{method:'POST',headers:{authorization:'Bearer '+operator,'content-type':'application/json'}});
  const slowResponse=new Promise<number|undefined>((resolve,reject)=>{
   slow.on('error',reject);slow.on('response',response=>{response.resume();resolve(response.statusCode);});
  });
  slow.write('{"tool":"instances.list",');
  await delay(Math.max(0,expiresAt-Date.now())+50);
  slow.end('"args":{}}');
  assert.equal(await slowResponse,401);
  assert.equal((await call(operator,'instances.list')).status,401);
  assert.equal((await call(observer,'instances.list')).status,401);
 }finally{child.kill('SIGTERM');await exited;await rm(root,{recursive:true,force:true});}
});
