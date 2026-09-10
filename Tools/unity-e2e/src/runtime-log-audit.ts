import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {join} from 'node:path';
import type {Platform} from './core.ts';

export function runtimeDiagnostics(log:string){
 const lines=log.split(/\r?\n/);
 return lines.flatMap((text,index)=>/^(?:[\w.]*Exception(?::|$)|\[Error\]|Assertion failed|UnityEngine\.Debug:Log(?:Error|Exception)\s*\()/.test(text.trim())
  ?[{line:index+1,summary:text.trim(),context:lines.slice(index,index+3)}]:[]);
}

export async function auditRuntimeLogs(platform:Platform,runId:string){
 const instances=platform.list().filter(instance=>instance.runId===runId);
 assert.equal(instances.length,4,'RUNTIME_AUDIT_REQUIRES_FOUR_INSTANCES');
 const results=await Promise.all(instances.map(async instance=>{
  const diagnostics=runtimeDiagnostics(await readFile(join(platform.config.artifactRoot,runId,instance.instanceId,'unity.log'),'utf8'));
  return {instanceId:instance.instanceId,diagnosticCount:diagnostics.length,diagnostics:diagnostics.slice(0,100)};
 }));
 const passed=results.every(result=>result.diagnosticCount===0);
 await platform.artifact(runId,'runtime-log-audit.json',{passed,scope:'Managed exceptions, Unity LogError/LogException, explicit [Error] records and assertion failures before success publication.',results});
 assert.ok(passed,'UNITY_RUNTIME_DIAGNOSTICS_FOUND');
}
