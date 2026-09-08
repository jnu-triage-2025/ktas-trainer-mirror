import test from 'node:test';
import assert from 'node:assert/strict';
import { Platform, type Instance } from '../src/core.ts';
test('an observation arriving after process exit cannot restore ownership or event polling',async()=>{
 const platform=new Platform({builds:{},artifactRoot:'/unused'});
 const instance:Instance={id:'p1',runId:'run',nodeId:'local',role:'client',endpoint:'http://127.0.0.1:1',token:'x'.repeat(32),profile:'',state:'READY',epoch:1,owner:'Automation'};
 platform.instances.set(instance.id,instance);
 let complete!:(value:unknown)=>void;
 platform.command=async()=>new Promise(resolve=>{complete=resolve;});
 const pending=platform.observe(instance.id);
 instance.state='CRASHED';instance.owner='None';
 complete({frame:100,client:{localPlayerReady:true},control:{controlEpoch:2,owner:'Automation'}});
 await pending;
 assert.equal(instance.state,'CRASHED');assert.equal(instance.owner,'None');assert.equal(instance.epoch,1);
 assert.equal(instance.eventTimer,undefined);assert.equal(instance.heartbeat,undefined);
});
