import test from 'node:test';
import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import {monitorMemoryPressure,type MemoryPressure} from '../src/memory-pressure.ts';
test('memory diagnostics never overlap and shutdown retains the in-flight sample',async()=>{
 let calls=0,finish!:(value:MemoryPressure)=>void;
 const monitor=monitorMemoryPressure(performance.now(),()=>{calls++;return new Promise(resolve=>{finish=resolve;});},5);
 await delay(30);assert.equal(calls,1);
 const stopped=monitor.stop();finish({available:true,systemWideFreePercent:58});await stopped;
 await delay(15);assert.equal(calls,1);assert.equal(monitor.samples.length,1);
 assert.deepEqual(monitor.samples[0].measurement,{available:true,systemWideFreePercent:58});
});
test('diagnostic failure remains unavailable rather than zero free memory',async()=>{
 const monitor=monitorMemoryPressure(performance.now(),async()=>{throw new Error('unavailable');});
 await monitor.stop();assert.deepEqual(monitor.samples[0].measurement,{available:false,reason:'query_failed'});
});
