import test from 'node:test';
import assert from 'node:assert/strict';
import { recordingDraft } from '../src/recording-draft.ts';
import { validate } from '../src/runner.ts';
const command=(id:number,type:string,payload:any,ms=0,ok=true)=>({id,kind:'command',timestamp:new Date(ms).toISOString(),body:{ok,command:{type,payload}}});
test('recorded UI actions become a reviewable definition with state waits',()=>{
 const d=recordingDraft('test','host',[
  command(1,'ui.activate',{automationId:'settings',mode:'device_input'}),
  command(2,'ui.text',{text:'example',mode:'input_adapter'})
 ],{scene:'Title'},{scene:'Settings'});
 assert.deepEqual(d.blockers,[]);assert.equal(d.requiresReview,true);
 assert.equal(validate(d.definition).valid,true);
 assert.deepEqual(d.definition!.steps.map(s=>s.type),['wait','uiAction','uiText','wait']);
});
test('held-key renewal does not duplicate a recorded press and timing remains reviewable',()=>{
 const d=recordingDraft('test','client',[
  command(1,'input.execute',{sequence:[{operation:'press',key:'W'}]},100),
  command(2,'input.execute',{sequence:[{operation:'press',key:'W'}]},600),
  command(3,'input.execute',{sequence:[{operation:'release',key:'W'}]},1100)
 ],{},{});
 assert.deepEqual(d.definition!.steps[0].sequence,[{operation:'hold',key:'W',durationMs:1000}]);
 assert.equal(d.definition!.steps.length,1);assert.ok(d.reviewItems.some(s=>s.includes('추정')));
});
test('uncertain commands, lost events, incomplete keys and coordinate clicks cannot produce runnable drafts',()=>{
 const inputs=[
  [command(1,'ui.activate',{automationId:'x'},0,false)],
  [{id:1,kind:'observation_gap',body:{}}],
  [command(1,'ui.pointer',{x:10,y:20,pressed:true})],
  [command(1,'input.execute',{sequence:[{operation:'press',key:'W'},{operation:'press',key:'A'}]})],
  [command(1,'input.execute',{sequence:[{operation:'press',key:'W'}]},0),command(2,'input.execute',{sequence:[{operation:'release',key:'W'}]},3000)]
 ];
 for(const entries of inputs){const d=recordingDraft('test','host',entries,{},{});assert.equal(d.definition,null);assert.ok(d.blockers.length);}
});
test('UI context changes are ordered after the input that caused them',()=>{
 const d=recordingDraft('context','client',[
  command(1,'input.execute',{sequence:[{operation:'press',key:'E'}]},100),
  {id:2,kind:'observation',body:{scene:'World',inputContext:'InventoryUIController'}},
  command(3,'input.execute',{sequence:[{operation:'release',key:'E'}]},150)
 ],{scene:'World',inputContext:'Gameplay'},{scene:'World',inputContext:'InventoryUIController',scenario:{graphId:'roles',nodeId:'waiting',side:'client'}});
 assert.equal(validate(d.definition).valid,true);
 const steps=d.definition!.steps;
 assert.equal(steps[2].type,'input');
 assert.equal(steps[3].predicate,'input.context');
 assert.equal(steps[3].args?.context,'InventoryUIController');
 assert.equal(steps.at(-1)!.args?.graphId,'roles');
});

test('overlapping keys preserve down states and staggered releases in one sequence',()=>{
 const input=(id:number,operation:string,key:string,ms:number)=>command(id,'input.execute',{sequence:[{operation,key}]},ms);
 const d=recordingDraft('chord','client',[
  input(1,'press','W',100),input(2,'press','A',300),input(3,'press','W',500),
  {id:4,kind:'observation',body:{inputContext:'Gameplay'}},
  input(5,'release','W',700),input(6,'release','A',900)
 ],{},{});
 assert.deepEqual(d.blockers,[]);assert.equal(validate(d.definition).valid,true);
 const held=new Set<string>();let at=0;const intervals:Array<{at:number;keys:string[]}>=[];
 for(const op of d.definition!.steps[0].sequence!){
  if(op.operation==='wait'){intervals.push({at,keys:[...held].sort()});at+=Number(op.durationMs);}
  else if(op.operation==='press')held.add(String(op.key));else if(op.operation==='release')held.delete(String(op.key));
 }
 assert.deepEqual(intervals,[{at:0,keys:['W']},{at:200,keys:['A','W']},{at:600,keys:['A']}]);
 assert.equal(at,800);assert.equal(held.size,0);
 assert.equal(d.definition!.steps[1].predicate,'input.context');
});

test('incomplete or ambiguous chords never produce runnable definitions',()=>{
 const input=(id:number,operation:string,key:string,ms:number)=>command(id,'input.execute',{sequence:[{operation,key}]},ms);
 const start=[input(1,'press','W',100),input(2,'press','A',200)];
 for(const ending of [
  [input(3,'release','W',300)],
  [command(3,'input.release_all',{},300)],
  [input(3,'release','W',50),input(4,'release','A',400)],
  [input(3,'release','W',2201),input(4,'release','A',2300)],
  [command(3,'ui.text',{text:'x'},250),input(4,'release','W',300),input(5,'release','A',400)]
 ]){
  const d=recordingDraft('incomplete','host',[...start,...ending],{},{});
  assert.equal(d.definition,null);assert.ok(d.blockers.length>0);
 }
});

test('camera motion and wheel input retain their order while a movement key is held',()=>{
 const d=recordingDraft('moving_camera','client',[
  command(1,'input.execute',{sequence:[{operation:'press',key:'W'}]},100),
  command(2,'input.execute',{sequence:[{operation:'lookDelta',x:12,y:-3}]},250),
  command(3,'input.execute',{sequence:[{operation:'scroll',y:1}]},400),
  command(4,'input.execute',{sequence:[{operation:'release',key:'W'}]},600)
 ],{},{});
 assert.deepEqual(d.blockers,[]);assert.equal(validate(d.definition).valid,true);
 assert.equal(d.definition!.steps.length,1);
 assert.deepEqual(d.definition!.steps[0].sequence,[
  {operation:'press',key:'W'},{operation:'wait',durationMs:150},{operation:'lookDelta',x:12,y:-3},
  {operation:'wait',durationMs:150},{operation:'scroll',y:1},{operation:'wait',durationMs:200},{operation:'release',key:'W'}
 ]);
});

test('out-of-order camera motion cannot turn into a runnable movement draft',()=>{
 const d=recordingDraft('bad_camera','host',[
  command(1,'input.execute',{sequence:[{operation:'press',key:'W'}]},100),
  command(2,'input.execute',{sequence:[{operation:'lookDelta',x:12,y:0}]},50),
  command(3,'input.execute',{sequence:[{operation:'release',key:'W'}]},600)
 ],{},{});
 assert.equal(d.definition,null);assert.ok(d.blockers.length);
});

const pointer=(id:number,pressed:boolean,target:any={elementType:'Button',documentId:'menu',automationId:'settings'},x=100,ms=id*100)=>{
 const entry=command(id,'ui.pointer',{x,y:200,pressed},ms);
 return {...entry,body:{...entry.body,pointerTarget:target}};
};
test('a confirmed stationary button click becomes one semantic action before its resulting state',()=>{
 const d=recordingDraft('click','host',[pointer(1,true),{kind:'observation',body:{inputContext:'Settings'}},pointer(2,false)],{},{});
 assert.deepEqual(d.blockers,[]);assert.equal(validate(d.definition).valid,true);
 assert.deepEqual(d.definition!.steps[0],{id:'recorded_1',type:'uiAction',actor:'p1',mode:'device_input',documentId:'menu',automationId:'settings'});
 assert.equal(d.definition!.steps[1].predicate,'input.context');
});
test('ambiguous targets, drags and incomplete clicks cannot produce runnable drafts',()=>{
 for(const entries of [
  [pointer(1,true,null),pointer(2,false)],
  [pointer(1,true),pointer(2,false,{elementType:'Button',documentId:'menu',automationId:'other'})],
  [pointer(1,true),pointer(2,true),pointer(3,false)],
  [pointer(1,true),pointer(2,false,undefined,110)],
  [pointer(1,true)],
  [pointer(1,true),pointer(2,false,undefined,100,3000)],
 ]){const d=recordingDraft('bad_click','host',entries,{},{});assert.equal(d.definition,null);assert.ok(d.blockers.length);}
});
