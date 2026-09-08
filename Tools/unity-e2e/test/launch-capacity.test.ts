import test from 'node:test';import assert from 'node:assert/strict';
import {mkdtemp,rm} from 'node:fs/promises';import {tmpdir} from 'node:os';import {join} from 'node:path';
import {Platform} from '../src/core.ts';
async function fixture(limit=5){const root=await mkdtemp(join(tmpdir(),'e2e-capacity-'));const p=new Platform({artifactRoot:root,maxConcurrentInstances:limit,builds:{test:{executable:process.execPath,args:['-e','setInterval(()=>{},1000)','--']}}});return {p,root};}
test('concurrent groups reserve capacity before any process is created',async()=>{
 const {p,root}=await fixture();let continueLaunch!:()=>void,entered!:()=>void;
 const blocked=new Promise<void>(done=>{continueLaunch=done;}),ready=new Promise<void>(done=>{entered=done;});
 const artifact=p.artifact.bind(p);let first=true;
 p.artifact=async(...args)=>{if(first){first=false;entered();await blocked;}return artifact(...args);};
 const launch=p.launch('test','host_plus_3_clients');
 try{
  await ready;assert.equal(p.instances.size,0);
  await assert.rejects(p.launch('test','host_plus_3_clients'),(error:any)=>error.code==='INSTANCE_CAPACITY_EXCEEDED');
  continueLaunch();const result=await launch;assert.equal(result.instances.length,4);
  await assert.rejects(p.launch('test','host_plus_3_clients'),(error:any)=>error.code==='INSTANCE_CAPACITY_EXCEEDED');
 }finally{continueLaunch();await launch.catch(()=>{});await p.close();await rm(root,{recursive:true,force:true});}
});
test('failed launch returns reserved capacity and terminal instances do not consume it',async()=>{
 const {p,root}=await fixture(1);const artifact=p.artifact.bind(p);
 try{
  p.artifact=async()=>{throw new Error('fixture write failed');};
  await assert.rejects(p.launch('test','single'),/fixture write failed/);
  p.artifact=artifact;const first=await p.launch('test','single');await p.stop(first.instances[0].instanceId);
  const second=await p.launch('test','single');assert.equal(second.instances.length,1);
 }finally{await p.close();await rm(root,{recursive:true,force:true});}
});

test('shutdown prevents a prepared launch from creating processes after close',async()=>{
 const {p,root}=await fixture();let resume!:()=>void,entered!:()=>void;
 const gate=new Promise<void>(done=>{resume=done;}),ready=new Promise<void>(done=>{entered=done;});
 p.artifact=async()=>{entered();await gate;return '';};
 const launch=p.launch('test','host_plus_3_clients');
 try{
  await ready;await p.close();resume();
  await assert.rejects(launch,(error:any)=>error.code==='PLATFORM_CLOSING');
  assert.equal(p.instances.size,0);
  await assert.rejects(p.launch('test','single'),(error:any)=>error.code==='PLATFORM_CLOSING');
 }finally{resume();await launch.catch(()=>{});await p.close();await rm(root,{recursive:true,force:true});}
});
