import test from 'node:test';
import assert from 'node:assert/strict';
import {questEvidence} from '../src/quest-evidence.ts';
const row=(id:number,body:any={})=>({id,kind:'game',body:{runId:'run',instanceId:'p1',eventType:'quest.completed',
 payload:{scenarioId:'scenario',definitionId:'quest',completed:true,placeholder:false},...body}});
test('quest evidence excludes wrong actor/run/scenario and incomplete or placeholder quests',()=>{
 const rows=[row(1),row(2,{instanceId:'p2'}),row(3,{runId:'old'}),
 row(4,{payload:{scenarioId:'other',definitionId:'other',completed:true,placeholder:false}}),
 ...[true,undefined].map((placeholder,i)=>row(5+i,{payload:{scenarioId:'scenario',definitionId:'fake',completed:true,placeholder}})),
 row(7,{payload:{scenarioId:'scenario',definitionId:'incomplete',completed:false,placeholder:false}})];
 const result=questEvidence(rows,'run','p1','scenario');
 assert.deepEqual(result.completedDefinitionIds,['quest']);
 assert.deepEqual(result.completions.map(x=>x.historyId),[1]);
 assert.equal(result.fullPlayPassed,false);
});
test('repeated completion keeps occurrence evidence and terminal events do not imply full pass',()=>{
 const result=questEvidence([row(1),row(2),row(3,{eventType:'scenario.ended',payload:{graphId:'scenario'}})],'run','p1','scenario');
 assert.deepEqual(result.completedDefinitionIds,['quest']);assert.equal(result.completions.length,2);
 assert.equal(result.scenarioLifecycle.length,1);assert.equal(result.fullPlayPassed,false);
 assert.deepEqual(questEvidence([],'run','p1','scenario').completedDefinitionIds,[]);
});
