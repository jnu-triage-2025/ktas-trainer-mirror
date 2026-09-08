import test from 'node:test';
import assert from 'node:assert/strict';
import {authorize,credentials} from '../src/access.ts';
import {toolNames} from '../src/api.ts';
test('observer credentials cannot impersonate operator credentials',()=>{
 const authenticate=credentials('o'.repeat(32),'v'.repeat(32));
 assert.equal(authenticate('Bearer '+'o'.repeat(32)),'operator');
 assert.equal(authenticate('Bearer '+'v'.repeat(32)),'observer');
 for(const value of [undefined,'','Bearer observer','Bearer '+'x'.repeat(32)])assert.equal(authenticate(value),undefined);
 assert.throws(()=>credentials('o'.repeat(32),'o'.repeat(32)));
 assert.throws(()=>credentials('o'.repeat(32),'short'));
});
test('observer access denies every state-changing tool and unknown tools',()=>{
 const allowed=['assistance.list','instances.list','editor.observe','game.observe','game.catalogue','game.screenshot','ui.query','events.read','conditions.wait','scenario.validate','scenario.status','operations.status','artifacts.list','artifacts.read','network.status'];
 for(const tool of toolNames){assert.equal(authorize('observer',tool),allowed.includes(tool),tool);assert.equal(authorize('operator',tool),true);}
 assert.equal(authorize('observer','future.mutation'),false);
});

test('credentials expire at the exact boundary for both access levels',t=>{
 t.mock.timers.enable({apis:['Date'],now:1000});
 const authenticate=credentials('o'.repeat(32),'v'.repeat(32),2000);
 assert.equal(authenticate('Bearer '+'o'.repeat(32)),'operator');
 t.mock.timers.setTime(1999);
 assert.equal(authenticate('Bearer '+'v'.repeat(32)),'observer');
 t.mock.timers.setTime(2000);
 assert.equal(authenticate('Bearer '+'o'.repeat(32)),undefined);
 assert.equal(authenticate('Bearer '+'v'.repeat(32)),undefined);
 for(const expiry of [NaN,Infinity,2000,1999,2000.5])assert.throws(()=>credentials('o'.repeat(32),undefined,expiry));
});
