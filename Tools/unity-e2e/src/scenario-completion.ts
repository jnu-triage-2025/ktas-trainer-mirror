import assert from 'node:assert/strict';
import {setTimeout as delay} from 'node:timers/promises';
import type {Platform} from './core.ts';

export function completionEvidence(state:any,rows:any[],instanceId:string,graph:string,terminalNode:string){
 const executionId=state.scenario?.executionId;
 if(!executionId||state.scenario.graphId!==null||state.scenario.nodeId!==null||state.scenario.state!=='Inactive'
   ||!Array.isArray(state.scenario.recoveryNotes)||state.scenario.recoveryNotes.length
   ||!Array.isArray(state.localQuests)||state.localQuests.some((q:any)=>q.scenarioId===graph&&!q.placeholder))return null;
 const events=rows.filter(row=>row.kind==='game'&&row.body?.instanceId===instanceId
   &&row.body.payload?.executionId===executionId);
 const terminal=events.find(row=>row.body.eventType==='node.entered'&&row.body.payload.graphId===graph
   &&row.body.payload.key==='nodeId'&&row.body.payload.value===terminalNode);
 // EndScenario clears graphId before emitting the lifecycle event. Match the
 // execution and the preceding terminal node, never an unrelated old ending.
 const ended=terminal&&events.find(row=>row.id>terminal.id&&row.body.eventType==='scenario.ended');
 return ended?{executionId,terminalEvent:terminal,endEvent:ended}:null;
}

export async function waitForScenarioCompletion(platform:Platform,actors:Record<string,string>,graph:string,
 terminalNode:string,onObservation?:(actor:string,state:any)=>Promise<void>,timeoutMs=60000){
 const deadline=performance.now()+timeoutMs;
 while(true){
  const states=await Promise.all(Object.entries(actors).map(async([actor,id])=>{
   const state=await platform.observe(id,false,{includeStaticItems:false,ttlMs:5000});
   assert.deepEqual(state.scenario.recoveryNotes,[],'SCENARIO_RECOVERY_USED');
   const evidence=state.scenario.state==='Inactive'
    ?completionEvidence(state,await platform.eventHistory(id),id,graph,terminalNode):null;
   if(!evidence)await onObservation?.(actor,state);
   return {actor,state,evidence};
  }));
  if(states.every(entry=>entry.evidence))return states;
  if(performance.now()>deadline)throw new Error(`SCENARIO_TERMINATION_NOT_CONFIRMED:${states.map(e=>`${e.actor}:${e.state.scenario.nodeId}:${e.state.scenario.state}`).join(',')}`);
  await delay(250);
 }
}
