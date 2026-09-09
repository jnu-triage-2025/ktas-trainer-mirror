import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,writeFile,readFile,rm} from 'node:fs/promises';
import {join} from 'node:path';
import {tmpdir} from 'node:os';
import {spawn,execFile} from 'node:child_process';
import {createServer} from 'node:net';
import {request} from 'node:https';
import {once} from 'node:events';
import {promisify} from 'node:util';
import {setTimeout as delay} from 'node:timers/promises';
import {loadConfig} from '../src/core.ts';
import {fileURLToPath} from 'node:url';
import {Client} from '@modelcontextprotocol/sdk/client/index.js';
import {StdioClientTransport} from '@modelcontextprotocol/sdk/client/stdio.js';

test('TLS console validates its certificate, origin and observer permissions',async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-gateway-'));
 const reservation=createServer();reservation.listen(0,'127.0.0.1');await once(reservation,'listening');
 const port=(reservation.address() as {port:number}).port;await new Promise<void>(resolve=>reservation.close(()=>resolve()));
 const certFile=join(root,'cert.pem'),keyFile=join(root,'key.pem');
 await promisify(execFile)('openssl',['req','-x509','-newkey','rsa:2048','-nodes','-keyout',keyFile,'-out',certFile,'-days','1','-subj','/CN=127.0.0.1','-addext','subjectAltName=IP:127.0.0.1']);
 const configFile=join(root,'config.json'),origin=`https://127.0.0.1:${port}`;
 const config={port,builds:{},artifactRoot:join(root,'artifacts'),gateway:{bind:'127.0.0.1',publicOrigin:origin,certFile,keyFile}};
 await writeFile(configFile,JSON.stringify(config));
 const operator='t'.repeat(64),observer='w'.repeat(64),ca=await readFile(certFile);
 const child=spawn(process.execPath,[fileURLToPath(new URL('../src/main.ts',import.meta.url)),configFile],{env:{...process.env,E2E_CONSOLE_TOKEN:operator,E2E_OBSERVER_TOKEN:observer},stdio:'ignore'});
 const exited=once(child,'exit');
 const call=(tool:string,token:string,requestOrigin=origin)=>new Promise<{status:number,body:any}>((resolve,reject)=>{
  const req=request(origin+'/api',{method:'POST',ca,headers:{authorization:'Bearer '+token,origin:requestOrigin,'content-type':'application/json'}},res=>{
   let body='';res.on('data',chunk=>body+=chunk);res.on('end',()=>resolve({status:res.statusCode!,body:body?JSON.parse(body):null}));
  });req.on('error',reject);req.end(JSON.stringify({tool,args:{}}));
 });
 try{
  const deadline=performance.now()+15000;
  while(true){try{assert.equal((await call('instances.list',operator)).status,200);break;}catch(error){if(performance.now()>deadline)throw error;await delay(25);}}
  assert.equal((await call('instances.list',observer)).status,200);
  assert.equal((await call('input.execute',observer)).status,403);
  assert.equal((await call('instances.list',operator,'https://untrusted.invalid')).status,403);
  assert.equal((await call('instances.list','invalid')).status,401);
  const transport=new StdioClientTransport({command:process.execPath,args:[fileURLToPath(new URL('../src/mcp.ts',import.meta.url))],env:{...process.env as Record<string,string>,NODE_EXTRA_CA_CERTS:certFile,E2E_SERVICE_URL:origin,E2E_CONSOLE_TOKEN:observer},stderr:'pipe'});
  const client=new Client({name:'tls-gateway-test',version:'1.0.0'});
  try {
   await client.connect(transport);
   const listed=await client.callTool({name:'instances.list',arguments:{}});
   assert.equal(listed.isError,false);
   const denied=await client.callTool({name:'input.release_all',arguments:{instanceId:'p1'}});
   assert.equal(denied.isError,true);
   assert.equal(JSON.parse((denied.content as any[])[0].text).code,'PERMISSION_DENIED');
  }finally{await client.close();await transport.close();}
  for(const publicOrigin of [`http://127.0.0.1:${port}`,origin+'/path',origin+'?x=1',`https://user@127.0.0.1:${port}`]){
   await writeFile(configFile,JSON.stringify({...config,gateway:{...config.gateway,publicOrigin}}));
   await assert.rejects(loadConfig(configFile),/INVALID_GATEWAY_CONFIG/);
  }
 }finally{child.kill('SIGTERM');await exited;await rm(root,{recursive:true,force:true});}
});
