import test from 'node:test';
import assert from 'node:assert/strict';
import {validate} from '../src/runner.ts';

test('input timing waits require a bounded integer duration without a key',()=>{
  const definition=(operation:Record<string,unknown>)=>({version:'1.0',id:'timing',executionMode:'regression',participants:{p1:{networkRole:'host'}},steps:[{id:'chord',type:'input',actor:'p1',sequence:[{operation:'press',key:'W'},operation,{operation:'release',key:'W'}]}]});
  for(const durationMs of [1,100,2000]) assert.equal(validate(definition({operation:'wait',durationMs})).valid,true);
  for(const durationMs of [undefined,0,-1,2001,1.5,'100']) assert.equal(validate(definition({operation:'wait',durationMs})).valid,false);
});
