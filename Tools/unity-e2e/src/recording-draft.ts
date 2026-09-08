import type { Definition, Step } from './runner.ts';

type Entry = { id?: unknown; kind: unknown; timestamp?: unknown; body: any };
export function recordingDraft(id:string,role:string,entries:Entry[],before:any,after:any) {
  const steps:Step[]=[], blockers:string[]=[], reviewItems:string[]=[
    '관측된 결과가 실제 검증 의도와 일치하는지 검토하세요. 이 초안은 자동으로 회귀 테스트에 등록되지 않습니다.',
    '입력 사이의 임의 대기 시간은 재생하지 않습니다. 필요한 UI·도메인 상태 대기를 검토하세요.'
  ];
  let pointerDown:{documentId:string;automationId:string;x:number;y:number;at:number}|undefined;
  const held=new Map<string,number>();
  let chord:Record<string,unknown>[]=[], chordStart=0, lastInputAt=0;
  const chordKeys=new Set<string>();
  const add=(step:Omit<Step,'id'>)=>steps.push({id:`recorded_${steps.length+1}`,...step});
  let lastScene:string|undefined,lastContext:string|undefined,pendingState:any;
  const addState=(snapshot:any)=>{
    if(snapshot?.scene && snapshot.scene!==lastScene){
      add({type:'wait',actor:'p1',predicate:'scene.is',args:{scene:snapshot.scene},timeoutMs:15000});lastScene=snapshot.scene;
    }
    if(snapshot?.inputContext && snapshot.inputContext!==lastContext){
      add({type:'wait',actor:'p1',predicate:'input.context',args:{context:snapshot.inputContext},timeoutMs:15000});lastContext=snapshot.inputContext;
    }
  };
  addState(before);
  for(const entry of entries){
    if(entry.kind==='observation'){if(held.size||pointerDown)pendingState=entry.body;else addState(entry.body);continue;}
    if(entry.kind==='observation_gap'){blockers.push(`기록 ${entry.id}: 이벤트 누락이 있습니다.`);continue;}
    if(entry.kind!=='command')continue;
    const command=entry.body?.command, type=command?.type, payload=command?.payload;
    if(!['input.execute','ui.activate','ui.text','ui.pointer','input.release_all'].includes(type))continue;
    if(!entry.body.ok||entry.body.outcome==='unknown'){
      blockers.push(`기록 ${entry.id}: ${type}의 실행 결과가 확인되지 않았습니다.`);continue;
    }
    if(pointerDown&&type!=='ui.pointer'&&type!=='input.release_all')blockers.push(`기록 ${entry.id}: 마우스를 누른 동안 다른 조작이 겹칩니다.`);
    if(type==='input.release_all'){
      if(pointerDown){blockers.push(`기록 ${entry.id}: 클릭이 긴급 해제로 끝났습니다.`);pointerDown=undefined;}
      if(held.size){blockers.push(`기록 ${entry.id}: ${[...held.keys()].join(", ")} 입력이 긴급 해제로 끝났습니다.`);held.clear();chord=[];chordKeys.clear();}
      continue;
    }
    if(type==='ui.pointer'){
      const target=entry.body.pointerTarget,at=Date.parse(String(entry.timestamp));
      if(held.size)blockers.push(`기록 ${entry.id}: 키 유지 중 클릭이 겹칩니다.`);
      if(target?.elementType!=='Button'||typeof target.documentId!=='string'||!target.documentId||typeof target.automationId!=='string'||!target.automationId
        ||!Number.isFinite(payload?.x)||!Number.isFinite(payload?.y)||!Number.isFinite(at)||typeof payload?.pressed!=='boolean'){
        blockers.push(`기록 ${entry.id}: 좌표 클릭을 안정적인 UI 식별자로 연결해야 합니다.`);continue;
      }
      if(payload.pressed){
        if(pointerDown)blockers.push(`기록 ${entry.id}: 드래그는 버튼 클릭으로 변환하지 않습니다.`);
        else pointerDown={documentId:target.documentId,automationId:target.automationId,x:payload.x,y:payload.y,at};
      }else if(!pointerDown)blockers.push(`기록 ${entry.id}: 시작이 없는 포인터 해제입니다.`);
      else {
        if(target.documentId!==pointerDown.documentId||target.automationId!==pointerDown.automationId
          ||Math.hypot(payload.x-pointerDown.x,payload.y-pointerDown.y)>2||at<pointerDown.at||at-pointerDown.at>2000)
          blockers.push(`기록 ${entry.id}: 클릭 대상이나 유지 시간이 달라 수동 검토가 필요합니다.`);
        else add({type:'uiAction',actor:'p1',mode:'device_input',documentId:target.documentId,automationId:target.automationId});
        pointerDown=undefined;
        if(pendingState){addState(pendingState);pendingState=undefined;}
      }
      continue;
    }
    if(type==='ui.activate'){
      if(held.size)blockers.push(`기록 ${entry.id}: 키 유지 중 UI 활성화가 겹칩니다.`);
      add({type:'uiAction',actor:'p1',mode:'device_input',automationId:payload.automationId,documentId:payload.documentId});continue;
    }
    if(type==='ui.text'){
      if(held.size)blockers.push(`기록 ${entry.id}: 키 유지 중 텍스트 입력이 겹칩니다.`);
      add({type:'uiText',actor:'p1',mode:'input_adapter',text:payload.text});continue;
    }
    for(const operation of payload?.sequence??[]){
      if(operation.operation==='press'||operation.operation==='release'){
        const key=operation.key, pressing=operation.operation==='press';
        if(pressing&&held.has(key))continue; // Renewal preserves the existing down transition.
        if(!pressing&&!held.has(key)){blockers.push(`기록 ${entry.id}: 시작이 없는 키 해제입니다.`);continue;}
        const at=Date.parse(String(entry.timestamp));
        if(!held.size){chord=[];chordKeys.clear();chordStart=at;lastInputAt=at;}
        const gap=at-lastInputAt;
        if(!Number.isFinite(gap)||gap<0||at-chordStart>2000){
          blockers.push(`기록 ${entry.id}: 키 조합의 유지 시간을 수동으로 검토해야 합니다.`);
        }else if(gap>0)chord.push({operation:'wait',durationMs:gap});
        chord.push({operation:operation.operation,key});lastInputAt=at;
        if(pressing){held.set(key,at);chordKeys.add(key);}else held.delete(key);
        if(!held.size){
          if(chord.length>128)blockers.push(`기록 ${entry.id}: 키 조합이 입력 시퀀스 길이 한도를 넘었습니다.`);
          const duration=at-chordStart;
          const sequence=chordKeys.size===1&&!chord.some(op=>op.operation==='lookDelta'||op.operation==='scroll')&&Number.isFinite(duration)&&duration>=0&&duration<=2000
            ? [{operation:'hold',key,durationMs:Math.max(1,duration)}] : chord;
          add({type:'input',actor:'p1',sequence});
          reviewItems.push(`기록 ${entry.id}: 키 유지 시간은 외부 명령 기록 시각으로 추정했습니다. 실제 프레임 타이밍을 검토하세요.`);
          if(pendingState){addState(pendingState);pendingState=undefined;}
        }
      }else if(held.size&&['lookDelta','scroll'].includes(operation.operation)){
        const at=Date.parse(String(entry.timestamp)),gap=at-lastInputAt;
        if(!Number.isFinite(gap)||gap<0||at-chordStart>2000)
          blockers.push(`기록 ${entry.id}: 이동 중 마우스 입력 시각을 검토해야 합니다.`);
        else if(gap>0)chord.push({operation:'wait',durationMs:gap});
        chord.push({...operation});lastInputAt=at;
      }else if(['tap','hold','lookDelta','scroll','wait'].includes(operation.operation)){
        if(held.size)blockers.push(`기록 ${entry.id}: 키 유지 중 다른 입력이 겹칩니다.`);
        add({type:'input',actor:'p1',sequence:[operation]});
      }else blockers.push(`기록 ${entry.id}: 지원하지 않는 입력입니다.`);
    }
  }
  if(pointerDown)blockers.push(`클릭의 해제 기록이 없습니다.`);
  if(held.size)blockers.push(`${[...held.keys()].join(", ")} 입력의 해제 기록이 없습니다.`);
  addState(after);
  if(after?.scenario?.graphId&&after.scenario.nodeId){
    add({type:'wait',actor:'p1',predicate:'scenario.node',args:{graphId:after.scenario.graphId,nodeId:after.scenario.nodeId,side:after.scenario.side},timeoutMs:15000});
    reviewItems.push('마지막 시나리오 노드에 도달했다는 사실을 검증 의도로 확정할지 검토하세요.');
  }
  if(!steps.some(step=>['input','uiAction','uiText'].includes(step.type)))blockers.push('변환할 수 있는 조작이 없습니다.');
  const definition:Definition={version:'1.0',id:`recording_${id}`,executionMode:'regression',participants:{p1:{networkRole:role}},steps};
  return {requiresReview:true,blockers,reviewItems,definition:blockers.length?null:definition,partialSteps:blockers.length?steps:undefined,
    source:{recordingId:id,eventCount:entries.filter(e=>e.kind==='game').length,before,after}};
}
