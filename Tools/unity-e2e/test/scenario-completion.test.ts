import test from 'node:test';
import assert from 'node:assert/strict';
import {completionEvidence,waitForScenarioCompletion} from '../src/scenario-completion.ts';
import type {Platform} from '../src/core.ts';

const state=()=>({scenario:{executionId:'run',graphId:null,nodeId:null,state:'Inactive',recoveryNotes:[]},localQuests:[]});
const rows=()=>[
 {id:1,kind:'game',body:{instanceId:'p1',eventType:'node.entered',payload:{executionId:'run',graphId:'graph',key:'nodeId',value:'last'}}},
 {id:2,kind:'game',body:{instanceId:'p1',eventType:'scenario.ended',payload:{executionId:'run',graphId:null}}}
];
test('completion correlates cleared graph ending with the terminal node of the same execution',()=>{
 assert.ok(completionEvidence(state(),rows(),'p1','graph','last'));
});
test('terminal node entry alone is not scenario completion',()=>{
 assert.equal(completionEvidence(state(),rows().slice(0,1),'p1','graph','last'),null);
 const active=state();active.scenario.state='ExecutingDelay';
 assert.equal(completionEvidence(active,rows(),'p1','graph','last'),null);
});
test('completion rejects stale execution, another actor, wrong graph, and reversed lifecycle',()=>{
 for(const edit of [(r:any[])=>r[1].body.payload.executionId='old',
  (r:any[])=>r[1].body.instanceId='p2',(r:any[])=>r[0].body.payload.graphId='other',
  (r:any[])=>r[1].id=0]){
  const r=rows();edit(r);assert.equal(completionEvidence(state(),r,'p1','graph','last'),null);
 }
});
test('completion rejects recovery, remaining scenario quests, and missing observations',()=>{
 const recovered=state();(recovered.scenario.recoveryNotes as string[]).push('skip');
 const pending=state();(pending.localQuests as any[]).push({scenarioId:'graph',placeholder:false});
 for(const s of [recovered,pending,{scenario:state().scenario},{localQuests:[]}])
  assert.equal(completionEvidence(s,rows(),'p1','graph','last'),null);
});
test('all four actors must finish; one pending actor cannot be hidden by the host',async()=>{
 const actors={p1:'p1',p2:'p2',p3:'p3',p4:'p4'};
 for(const pending of [false,true]){
  const platform={observe:async(id:string)=>{
   const s=state();if(pending&&id==='p4')s.scenario.state='ExecutingDelay';return s;
  },eventHistory:async(id:string)=>rows().map(row=>({...row,body:{...row.body,instanceId:id}}))} as unknown as Platform;
  if(pending)await assert.rejects(waitForScenarioCompletion(platform,actors,'graph','last',undefined,0),/p4/);
  else assert.equal((await waitForScenarioCompletion(platform,actors,'graph','last')).length,4);
 }
});
