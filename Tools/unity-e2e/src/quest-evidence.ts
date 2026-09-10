type HistoryRow = {id:number;kind:string;body:any};

// This is an evidence index, not an acceptance verdict. Completion events alone
// do not prove normal gameplay, role coverage, or terminal scenario completion.
export function questEvidence(rows:HistoryRow[], runId:string, instanceId:string, scenarioId:string) {
 const events=rows.filter(row=>row.kind==='game'&&row.body?.runId===runId&&row.body?.instanceId===instanceId);
 const completions=events.filter(row=>row.body.eventType==='quest.completed'
  &&row.body.payload?.scenarioId===scenarioId&&row.body.payload.completed===true
  &&row.body.payload.placeholder===false&&typeof row.body.payload.definitionId==='string'
  &&row.body.payload.definitionId.length>0);
 const executions=new Set(events.filter(row=>row.body.payload?.graphId===scenarioId
  &&typeof row.body.payload.executionId==='string').map(row=>row.body.payload.executionId));
 return {
  runId,instanceId,scenarioId,fullPlayPassed:false,
  scope:'Observed local quest completions only; role coverage, recovery-free gameplay and terminal completion still require verification.',
  completedDefinitionIds:[...new Set(completions.map(row=>row.body.payload.definitionId))].sort(),
  completions:completions.map(row=>({historyId:row.id,event:row.body})),
  scenarioLifecycle:events.filter(row=>['scenario.started','scenario.ended'].includes(row.body.eventType)
   &&(row.body.payload?.graphId===scenarioId||(row.body.eventType==='scenario.ended'
    &&row.body.payload?.graphId==null&&executions.has(row.body.payload?.executionId))))
   .map(row=>({historyId:row.id,event:row.body}))
 };
}
