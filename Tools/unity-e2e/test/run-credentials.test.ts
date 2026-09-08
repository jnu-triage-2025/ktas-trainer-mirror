import test from 'node:test';
import assert from 'node:assert/strict';
import { RunCredentials } from '../src/run-credentials.ts';
test('run grants expire, isolate runs and cannot escalate through returned objects', () => {
 let now = 1000; const credentials = new RunCredentials(() => now);
 const issued = credentials.issue('run-a', ['observe'], 100);
 const grant = credentials.authenticate('Bearer ' + issued.token)!;
 assert.equal(credentials.permits(grant, 'run-a', 'observe'), true);
 assert.equal(credentials.permits(grant, 'run-b', 'observe'), false);
 grant.capabilities.push('fixture');
 assert.equal(credentials.permits(grant, 'run-a', 'fixture'), false);
 assert.equal(JSON.stringify(grant).includes(issued.token), false);
 now = 1100;
 assert.equal(credentials.authenticate('Bearer ' + issued.token), undefined);
 assert.equal(credentials.permits(grant, 'run-a', 'observe'), false);
});
test('revocation invalidates previously authenticated grants and preserves other runs', () => {
 const credentials = new RunCredentials();
 const a = credentials.issue('a', ['control'], 10000), b = credentials.issue('b', ['protocol'], 10000);
 const grant = credentials.authenticate('Bearer ' + a.token)!;
 credentials.revokeRun('a');
 assert.equal(credentials.permits(grant, 'a', 'control'), false);
 assert.ok(credentials.authenticate('Bearer ' + b.token));
 assert.equal(credentials.revoke(b.id), true);
 assert.equal(credentials.authenticate('Bearer ' + b.token), undefined);
});
test('invalid or overlong grants and malformed credentials fail closed', () => {
 const credentials = new RunCredentials();
 for (const ttl of [0, -1, 900001, NaN, Infinity, 1.1]) assert.throws(() => credentials.issue('a', ['observe'], ttl));
 assert.throws(() => credentials.issue('a', [], 10));
 assert.throws(() => credentials.issue('a', ['observe', 'observe'], 10));
 for (const header of [undefined, '', 'Bearer invalid']) assert.equal(credentials.authenticate(header), undefined);
});
