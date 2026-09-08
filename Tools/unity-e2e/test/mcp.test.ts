import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { mkdtemp, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { randomBytes } from 'node:crypto';
import { createServer } from 'node:net';
import { setTimeout as delay } from 'node:timers/promises';
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';

test('official MCP client discovers tools and invokes authenticated service',async()=>{
 const directory=await mkdtemp(join(tmpdir(),'unity-e2e-mcp-'));
 const socket=createServer();await new Promise<void>(done=>socket.listen(0,'127.0.0.1',done));
 const address=socket.address();assert.ok(address&&typeof address!=='string');const port=address.port;
 await new Promise<void>(done=>socket.close(()=>done()));
 const token=randomBytes(32).toString('hex'),url=`http://127.0.0.1:${port}`;
 const config=join(directory,'config.json');await writeFile(config,JSON.stringify({port,builds:{},artifactRoot:join(directory,'artifacts')}));
 const service=spawn(process.execPath,[resolve(import.meta.dirname,'../src/main.ts'),config],{env:{...process.env,E2E_CONSOLE_TOKEN:token},stdio:'ignore'});
 const transport=new StdioClientTransport({command:process.execPath,args:[resolve(import.meta.dirname,'../src/mcp.ts')],env:{...process.env as Record<string,string>,E2E_SERVICE_URL:url,E2E_CONSOLE_TOKEN:token},stderr:'pipe'});
 const client=new Client({name:'e2e-test',version:'1.0.0'});
 try{
  let ready=false;for(let n=0;n<50;n++){try{const response=await fetch(url);if(response.ok){ready=true;break;}}catch{}await delay(100);}
  assert.equal(ready,true);
  const unauth=await fetch(url+'/api',{method:'POST',body:'{}'});assert.equal(unauth.status,401);
  const badOrigin=await fetch(url+'/api',{method:'POST',headers:{Authorization:`Bearer ${token}`,Origin:'https://invalid.example'},body:'{}'});assert.equal(badOrigin.status,403);
  await client.connect(transport);
  const listed=await client.listTools();assert.ok(listed.tools.some(t=>t.name==='control.handoff'));assert.ok(listed.tools.some(t=>t.name==='instances.join'));
  assert.ok(listed.tools.find(t=>t.name==='input.execute')?.inputSchema.required?.includes('payload'));
  const result=await client.callTool({name:'instances.list',arguments:{}});
  assert.equal(result.isError,false);assert.deepEqual(JSON.parse((result.content as any[])[0].text),[]);
  const validation=await client.callTool({name:'scenario.validate',arguments:{definition:{}}});
  assert.equal(JSON.parse((validation.content as any[])[0].text).valid,false);
 }finally{
  await client.close();await transport.close();service.kill('SIGTERM');
  await Promise.race([new Promise<void>(done=>service.once('exit',()=>done())),delay(5000,undefined,{ref:false})]);
  if(service.exitCode===null&&service.signalCode===null)service.kill('SIGKILL');
 }
});
