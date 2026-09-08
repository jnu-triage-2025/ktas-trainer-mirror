import assert from 'node:assert/strict';
import { randomUUID } from 'node:crypto';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig } from '../src/core.ts';
const p=new Platform(await loadConfig(process.argv[2]??'config.json'));
try {
  const launched=await p.launch('mac','single'),id=launched.instances[0].instanceId;
  const until=performance.now()+30000;
  while(true){try{await p.observe(id);break;}catch(error){if(performance.now()>until)throw error;await delay(200);}}
  await p.acquire(id);
  const hold=p.command(id,'input.execute',{sequence:[{operation:'hold',key:'W',durationMs:1500}]},{ttlMs:5000});
  const cancelled=assert.rejects(hold,/CANCELLED/);
  await delay(200);
  assert.equal((await p.observe(id)).input.w,true);
  await assert.rejects(p.command(id,'input.execute',{sequence:[{operation:'tap',key:'S'}]}),/INPUT_BUSY/);
  assert.equal((await p.observe(id)).input.w,true,'Rejected concurrency must not release the active macro');
  await p.release(id);await cancelled;
  assert.equal((await p.observe(id)).input.w,false);
  await assert.rejects(p.command(id,'input.execute',{sequence:[{operation:'press',key:'W'},{operation:'invalid'}]}),/INVALID_ARGUMENT/);
  assert.equal((await p.observe(id)).input.w,false,'Invalid sequence must have no partial effects');
  const instance=p.get(id),message={protocolVersion:'1.0',runId:instance.runId,instanceId:id,commandId:randomUUID(),controlEpoch:instance.epoch,owner:'Automation',type:'input.execute',ttlMs:2000,payload:{sequence:[{operation:'press',key:'W'}]}};
  const send=async()=>{const response=await fetch(instance.endpoint+'/command',{method:'POST',headers:{Authorization:`Bearer ${instance.token}`,'Content-Type':'application/json'},body:JSON.stringify(message)});return response.json();};
  const original=await send();assert.equal(original.ok,true);await p.release(id);
  assert.deepEqual(await send(),original);assert.equal((await p.observe(id)).input.w,false,'Duplicate must return old result without replay');
  const oldEpoch=instance.epoch;await p.handoff(id,'RemoteHuman');
  await assert.rejects(p.command(id,'input.execute',{sequence:[{operation:'press',key:'W'}]},{epoch:oldEpoch}),/STALE_CONTROL_EPOCH/);
  await delay(2300);const state=await p.observe(id);assert.equal(state.control.owner,'None');assert.equal(state.input.w,false);
  await p.artifact(launched.runId,'protocol-smoke.json',{passed:true,checks:['concurrent_input_denied','emergency_cancels_macro','invalid_sequence_atomic','duplicate_no_replay','stale_epoch_denied','lease_releases_input'],state});
  const stopped=await p.stop(id);assert.ok('state' in stopped);assert.equal(stopped.state,'EXITED');
  console.log(JSON.stringify({runId:launched.runId,passed:true,stopped}));
}finally{await p.close();}
