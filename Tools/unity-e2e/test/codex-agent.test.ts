import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,writeFile,readFile,rm} from 'node:fs/promises';
import {join,resolve} from 'node:path';
import {tmpdir} from 'node:os';
import {spawn} from 'node:child_process';

test('Codex adapter restricts tools and propagates structured success and failure without an external model',{skip:process.platform==='win32'},async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-codex-adapter-')),fake=join(root,'codex'),capture=join(root,'args.json');
 await writeFile(fake,`#!/usr/bin/env node\nconst fs=require('node:fs');let input='';process.stdin.on('data',x=>input+=x);process.stdin.on('end',()=>{const args=process.argv.slice(2);fs.writeFileSync(process.env.E2E_TEST_CAPTURE,JSON.stringify({args,prompt:input}));fs.writeFileSync(args[args.indexOf('-o')+1],JSON.stringify({success:process.env.E2E_TEST_SUCCESS==='1',result:'검증 결과'}));});\n`,{mode:0o700});
 try{for(const success of [true,false]){
  const child=spawn(process.execPath,[resolve(import.meta.dirname,'../src/codex-agent.ts')],{env:{...process.env,E2E_CODEX_EXECUTABLE:fake,E2E_TEST_CAPTURE:capture,E2E_TEST_SUCCESS:success?'1':'0',E2E_CONSOLE_TOKEN:'test-scoped-credential',E2E_AI_MODEL:'test-model'},stdio:['pipe','pipe','pipe']});
  let stdout='',stderr='';child.stdout.setEncoding('utf8');child.stdout.on('data',x=>stdout+=x);child.stderr.on('data',x=>stderr+=x);
  child.stdin.end(JSON.stringify({version:1,request:{id:'request',instanceId:'one',runId:'run',prompt:'설정창 열기'}}));
  const code=await new Promise<number|null>((done,reject)=>{child.once('error',reject);child.once('close',done);});
  assert.equal(code,success?0:1,stderr);assert.deepEqual(JSON.parse(stdout),success?{result:'검증 결과'}:{error:'검증 결과'});
  const recorded=JSON.parse(await readFile(capture,'utf8'));const args:string[]=recorded.args;
  assert.ok(args.includes('--ignore-user-config'));assert.ok(args.includes('--ephemeral'));
  assert.equal(args[args.indexOf('--sandbox')+1],'read-only');
  for(const feature of ['shell_tool','multi_agent','apps'])assert.ok(args.some((value,index)=>value==='--disable'&&args[index+1]===feature));
  const mcp=args[args.indexOf('-c')+1];assert.ok(mcp.includes('game.observe'));assert.ok(!mcp.includes('credentials.issue'));assert.ok(!mcp.includes('instances.launch'));assert.ok(!mcp.includes('test-scoped-credential'));
  assert.match(recorded.prompt,/requestId=request/);assert.match(recorded.prompt,/success=false/);
 }}finally{await rm(root,{recursive:true,force:true});}
});
