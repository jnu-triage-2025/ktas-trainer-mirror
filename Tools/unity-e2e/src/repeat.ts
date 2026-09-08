import { readFile } from 'node:fs/promises';
import { randomUUID } from 'node:crypto';
import { Platform, loadConfig } from './core.ts';
import { regression } from './regression.ts';
const [configPath,definitionPath,buildId,countText='20']=process.argv.slice(2);
const count=Number(countText);
if(!configPath||!definitionPath||!buildId||!Number.isInteger(count)||count<1||count>100)throw new Error('Usage: node src/repeat.ts CONFIG DEFINITION BUILD_ID [COUNT:1..100]');
const config=await loadConfig(configPath),definition=JSON.parse(await readFile(definitionPath,'utf8'));
const suiteId=randomUUID(),results:any[]=[],stop=new AbortController(),artifacts=new Platform(config);
for(const signal of ['SIGINT','SIGTERM'] as const)process.once(signal,()=>stop.abort());
await artifacts.artifact(suiteId,'definition.json',definition);
for(let iteration=1;iteration<=count;iteration++) {
  if(stop.signal.aborted)break;
  const started=new Date().toISOString();
  const result=await regression(config,structuredClone(definition),buildId,definition.topology??'host_plus_3_clients',stop.signal);
  results.push({iteration,started,finished:new Date().toISOString(),...result});
  const passed=result.state==='passed'&&!result.cleanupErrors.length;
  await artifacts.artifact(suiteId,'suite.json',{suiteId,requested:count,completed:results.length,passed:results.filter(r=>r.state==='passed'&&!r.cleanupErrors.length).length,results});
  process.stdout.write(JSON.stringify({suiteId,iteration,state:result.state,runId:result.runId,cleanupErrors:result.cleanupErrors})+'\n');
  if(!passed)break;
}
process.exitCode=results.length===count&&results.every(r=>r.state==='passed'&&!r.cleanupErrors.length)?0:1;
