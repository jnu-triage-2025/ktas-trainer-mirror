import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp,writeFile,readFile,rm} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {setTimeout as delay} from 'node:timers/promises';
import {Platform} from '../src/core.ts';
test('editor clients require a live host and share its run and actual game port',async()=>{
 const root=await mkdtemp(join(tmpdir(),'e2e-editor-group-')),script=join(root,'player.cjs');
 await writeFile(script,"require('node:fs').writeFileSync(process.env.UNITY_E2E_PROFILE+'/port',process.env.UNITY_E2E_GAME_PORT);setInterval(()=>{},1000)");
 const p=new Platform({builds:{fake:{executable:process.execPath,args:[script]}},artifactRoot:root});
 try {
  await assert.rejects(p.launch('fake','editor_host_plus_3_clients'),error=>(error as any).code==='EDITOR_HOST_REQUIRED');
  p.instances.set('editor',{id:'editor',runId:'editor-run',role:'editor',state:'READY',owner:'None',epoch:0} as any);
  let started=false;p.observe=async()=>({server:{started,localPort:24567}});
  await assert.rejects(p.launch('fake','editor_host_plus_3_clients'),error=>(error as any).code==='EDITOR_HOST_REQUIRED');
  started=true;const launched=await p.launch('fake','editor_host_plus_3_clients');
  assert.equal(launched.runId,'editor-run');assert.equal(launched.instances.length,3);
  assert.ok(launched.instances.every(instance=>instance.role==='client'));
  const children=[...p.instances.values()].filter(i=>i.role==='client');
  assert.equal(new Set(children.map(i=>i.profile)).size,3);
  for(const child of children) {
   const deadline=performance.now()+10000;
   while(true){try{assert.equal(await readFile(join(child.profile,'port'),'utf8'),'24567');break;}catch(error){if(performance.now()>deadline)throw error;await delay(20);}}
  }
  await assert.rejects(p.launch('fake','editor_host_plus_3_clients'),error=>(error as any).code==='EDITOR_GROUP_ALREADY_LAUNCHED');
 } finally {await p.close();await rm(root,{recursive:true,force:true});}
});

test('lost Editor transition replies are resolved by observation without replay',async()=>{
 const p=new Platform({builds:{},artifactRoot:'/tmp'}),calls:string[]=[];
 let observations=0;
 (p as any).editorRequest=async(type:string)=>{
  calls.push(type);if(type==='editor.play')throw new SyntaxError('truncated reload response');
  return {ok:true,playing:++observations>1,compiling:false,updating:false};
 };
 const result=await p.editorCommand('editor.play');assert.equal(result.playing,true);
 assert.equal(result.acknowledgementReceived,false);assert.equal(calls.filter(type=>type==='editor.play').length,1);
 (p as any).editorRequest=async(type:string)=>({ok:true,playing:type==='editor.stop',compiling:false,updating:false});
 assert.equal((await p.editorCommand('editor.stop')).playing,false);
});
