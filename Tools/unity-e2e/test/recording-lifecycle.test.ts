import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp,readFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { Platform } from '../src/core.ts';
import { EventStore } from '../src/store.ts';
async function fixture(){
 const root=await mkdtemp(join(tmpdir(),'e2e-recording-'));
 const platform=new Platform({builds:{},artifactRoot:root});
 platform.instances.set('one',{id:'one',runId:'recorded-run',nodeId:'local',role:'client',endpoint:'',token:'test',profile:'',state:'EXITED',epoch:0,owner:'None'});
 platform.observe=async()=>({scene:'World',inputContext:'Gameplay'});
 const store=new EventStore(join(root,'service'));
 const append=()=>store.append('recorded-run','one','command',{ok:true,command:{type:'input.execute',payload:{sequence:[{operation:'tap',key:'E'}]}}});
 return {platform,store,append};
}
test('recording preserves raw evidence when its final observation is unavailable',async()=>{
 const f=await fixture();
 try{
  const {recordingId}=await f.platform.recordingStart('one');f.append();
  f.platform.observe=async()=>{throw new Error('connection lost');};
  const result=await f.platform.recordingStop(recordingId);
  const raw=JSON.parse(await readFile(result.path,'utf8'));
  const draft=JSON.parse(await readFile(result.draftPath,'utf8'));
  assert.equal(raw.entries.length,1);assert.match(raw.observationError,/connection lost/);
  assert.equal(draft.definition,null);assert.ok(result.blockers.length);
 }finally{f.store.close();await f.platform.close();}
});
test('failed artifact writes do not discard the recording handle',async()=>{
 const f=await fixture();
 try{
  const {recordingId}=await f.platform.recordingStart('one');f.append();
  const artifact=f.platform.artifact.bind(f.platform);
  f.platform.artifact=async()=>{throw new Error('disk unavailable');};
  await assert.rejects(f.platform.recordingStop(recordingId),/disk unavailable/);
  f.platform.artifact=artifact;
  const result=await f.platform.recordingStop(recordingId);
  const draft=JSON.parse(await readFile(result.draftPath,'utf8'));
  assert.equal(draft.definition.steps.filter((s:any)=>s.type==='input').length,1);
  await assert.rejects(f.platform.recordingStop(recordingId),/UNKNOWN_RECORDING/);
 }finally{f.store.close();await f.platform.close();}
});
