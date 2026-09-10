import test from 'node:test';
import assert from 'node:assert/strict';
import {runtimeDiagnostics} from '../src/runtime-log-audit.ts';

test('runtime audit finds managed exceptions and explicit failures with line evidence',()=>{
 const result=runtimeDiagnostics('startup\nNullReferenceException: missing\n  at autoDoorSlide.OnTriggerEnter\n\n[Error] failed\nSystem.InvalidOperationException: invalid\nAssertion failed on expression');
 assert.deepEqual(result.map(entry=>entry.line),[2,5,6,7]);
 assert.match(result[0].context.join('\n'),/OnTriggerEnter/);
});
test('runtime audit does not mistake warnings or stack method names for exceptions',()=>{
 assert.deepEqual(runtimeDiagnostics('[Warning] optional audio device unavailable\nFoo.LogException ()\nException count: 0\nordinary output'),[]);
});
test('runtime audit catches Unity error output without a severity prefix',()=>{
 const result=runtimeDiagnostics("Missing scenario handler\nUnityEngine.Debug:LogError (object,UnityEngine.Object)\nUnityEngine.Debug:LogException (System.Exception)");
 assert.deepEqual(result.map(entry=>entry.line),[2,3]);
});
