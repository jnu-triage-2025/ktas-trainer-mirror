import test from 'node:test';
import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {once} from 'node:events';
import {createServer} from 'node:http';
import {mkdtemp,writeFile,readFile,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join,resolve} from 'node:path';
import {randomBytes} from 'node:crypto';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform} from '../src/core.ts';
import {API} from '../src/api.ts';

for (const mode of ['hard_exit','graceful_stop'] as const) test(`worker process cleanup: ${mode}`,{timeout:20000},async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-worker-process-'));
 const platform=new Platform({builds:{},artifactRoot:root});
 platform.instances.set('one',{id:'one',runId:'run',nodeId:'local',role:'host',token:'fixture',profile:'',endpoint:'',state:'READY',epoch:0,owner:'None'});
 const api=new API(platform),token=randomBytes(32).toString('hex'),queued=api.assistance.submit('run','one','Open settings');
 const server=createServer(async(req,res)=>{
  if(req.headers.authorization!==`Bearer ${token}`){res.writeHead(401).end();return;}
  try{let body='';for await(const chunk of req)body+=chunk;const value=JSON.parse(body);
   const result=await api.call(value.tool,value.args,'Automation');res.end(JSON.stringify({ok:true,result}));
  }catch(error){res.statusCode=400;res.end(JSON.stringify({ok:false,error:{code:(error as any).code,message:String(error)}}));}
 });
 await new Promise<void>(resolve=>server.listen(0,'127.0.0.1',resolve));
 const address=server.address();assert.ok(address&&typeof address!=='string');
 const marker=join(root,'adapter.json'),adapter=join(root,'adapter.mjs');
 await writeFile(adapter,`import {writeFileSync} from 'node:fs';\nlet input='';for await(const chunk of process.stdin)input+=chunk;\nJSON.parse(input);process.on('SIGTERM',()=>{});writeFileSync(process.argv[2],JSON.stringify({pid:process.pid,token:process.env.E2E_CONSOLE_TOKEN}),{mode:0o600});setInterval(()=>{},1000);\n`);
 const worker=spawn(process.execPath,[resolve(import.meta.dirname,'../src/worker.ts'),process.execPath,adapter,marker],{
  env:{...process.env,E2E_SERVICE_URL:`http://127.0.0.1:${address.port}`,E2E_CONSOLE_TOKEN:token,E2E_WORKER_TIMEOUT_MS:mode==='hard_exit'?'2000':'10000'},stdio:'ignore'});
 let adapterPid:number|undefined;
 try{
  let started:any;for(let i=0;i<100;i++){try{started=JSON.parse(await readFile(marker,'utf8'));break;}catch{}await delay(20);}
  assert.ok(started,'Adapter must start before worker is killed');adapterPid=started.pid;
  assert.notEqual(started.token,token);assert.ok(api.credentials.authenticate('Bearer '+started.token));
  const exited=once(worker,'exit');worker.kill(mode==='hard_exit'?'SIGKILL':'SIGTERM');const [exitCode,exitSignal]=await exited;
  if(mode==='hard_exit')assert.equal(exitSignal,'SIGKILL');else assert.equal(exitCode,0);
  for(let i=0;i<150&&api.assistance.get(queued.id)?.state==='running';i++)await delay(20);
  assert.equal(api.assistance.get(queued.id)?.state,'failed');
  assert.equal(api.credentials.authenticate('Bearer '+started.token),undefined);
  if(mode==='hard_exit')process.kill(adapterPid!,0);
  else {
   assert.match(api.assistance.get(queued.id)!.result!,/AI_EXECUTION_ABORTED/);
   assert.throws(()=>process.kill(adapterPid!,0),(error:any)=>error.code==='ESRCH');
  }
 }finally{
  if(worker.exitCode===null&&worker.signalCode===null){const done=once(worker,'exit');worker.kill('SIGKILL');await done;}
  if(adapterPid)try{process.kill(adapterPid,'SIGKILL');}catch{}
  await new Promise<void>(resolve=>server.close(()=>resolve()));await rm(root,{recursive:true,force:true});
 }
});
