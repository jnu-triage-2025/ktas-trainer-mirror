import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {runInNewContext} from 'node:vm';
import {setImmediate as nextTurn} from 'node:timers/promises';

test('console routes slot, wheel and locked mouse input and releases on unlock',async()=>{
 const listeners=new Map<string,Function[]>(),elements=new Map<string,any>();
 const element=(id:string):any=>{
  if(!elements.has(id))elements.set(id,{id,value:'',textContent:'',dataset:{},tagName:'DIV',
   addEventListener:(event:string,callback:Function)=>{const key=id+':'+event;listeners.set(key,[...(listeners.get(key)??[]),callback]);},
   replaceChildren(){this.replacements=(this.replacements??0)+1;},append(){},removeAttribute(name:string){delete this[name];},async decode(){},focus(){document.activeElement=this;},setPointerCapture(){},querySelectorAll(){return [];},getBoundingClientRect(){return {left:90,top:190,width:960,height:540};}});
  return elements.get(id);
 };
 const document:any={getElementById:element,createElement:()=>element('created-'+elements.size),hidden:false,activeElement:{tagName:'DIV'},pointerLockElement:null,hasFocus:()=>false,visibilityState:'visible',
  addEventListener:(event:string,callback:Function)=>listeners.set('document:'+event,[callback])};
 const requests:any[]=[], timers:Function[]=[], assistance:any[]=[];
 let availableInstances=[{instanceId:'p1',runId:'run',role:'host',state:'READY'}];
 let rejectPointerRelease=false;
 let delayEmergency:Promise<void>|undefined;
 let delayAcquire:Promise<void>|undefined;
 let delayShot:Promise<void>|undefined;
 const flush=async()=>{for(let index=0;index<5;index++)await nextTurn();};
 const emit=async(target:string,event:string,value:any={})=>{
  for(const callback of listeners.get(target+':'+event)??[])callback({preventDefault(){},...value});await flush();
 };
 runInNewContext(await readFile(new URL('../src/console.js',import.meta.url),'utf8'),{
  document,window:{addEventListener(){}},location:{hash:''},sessionStorage:{getItem:()=> 'test-token'},history:{},setInterval(fn:Function){timers.push(fn);},
  fetch:async(_url:string,options:any)=>{
   const request=JSON.parse(options.body);requests.push(request);
   if(request.tool==='game.screenshot')await delayShot;
   if(request.tool==='control.emergency_stop')await delayEmergency;
   if(request.tool==='control.acquire')await delayAcquire;
   if(request.tool==='assistance.submit')assistance.push({instanceId:request.args.instanceId,prompt:request.args.prompt,state:'queued'});
   const result=request.tool==='assistance.list'?assistance:request.tool==='instances.list'?availableInstances:request.tool==='control.acquire'?{controlEpoch:7}:request.tool==='game.screenshot'?{mimeType:'image/jpeg',data:'',width:960,height:540,frame:100}:{};
   return {ok:true,status:200,json:async()=>rejectPointerRelease&&request.tool==='ui.pointer'&&!request.args.payload.pressed?{ok:false,error:{code:'STATE_CONFLICT'}}:{ok:true,result}};
  }
 });
 await flush();element('acquire').onclick();await flush();
 element('assistance-prompt').value='설정창을 열어 줘 <script>test</script>';
 element('assistance-submit').onclick();await flush();
 assert.equal(assistance[0].instanceId,'p1');assert.equal(assistance[0].prompt,'설정창을 열어 줘 <script>test</script>');
 assert.equal(element('assistance-prompt').value,'');
 assert.match(element('assistance-status').textContent,/대기/);
 const assistanceRenders=element('assistance-list').replacements;
 element('assistance-refresh').onclick();await flush();
 assert.equal(element('assistance-list').replacements,assistanceRenders,'Unchanged polling must preserve actionable request buttons');
 element('screen').requestPointerLock=()=>Promise.reject({name:'WrongDocumentError'});
 element('lock').onclick();await flush();
 assert.match(element('camera-status').textContent,/앞으로/);
 assert.ok(element('camera-status').textContent.includes('카메라'));
 let nativeDragPrevented=false;
 await emit('screen','dragstart',{preventDefault(){nativeDragPrevented=true;}});
 assert.equal(nativeDragPrevented,true);
 await emit('document','keydown',{code:'Digit2'});await emit('document','keyup',{code:'Digit2'});
 document.pointerLockElement=element('screen');
 await emit('screen','wheel',{deltaY:120});
 await emit('screen','pointerdown',{button:2,pointerId:1});
 document.pointerLockElement=null;await emit('document','pointerlockchange');
 const sequences=requests.filter(request=>request.tool==='input.execute').flatMap(request=>request.args.payload.sequence);
 assert.deepEqual(sequences,[{operation:'press',key:'Alpha2'},{operation:'release',key:'Alpha2'},{operation:'scroll',y:-1},{operation:'press',key:'Mouse1'}]);
 const release=requests.findLast(request=>request.tool==='input.release_all');
 assert.equal(release.args.instanceId,'p1');assert.equal(release.args.controlEpoch,7);
 // Dragging a settings slider sends intermediate pressed positions, then a release.
 element('capture').onclick();await flush();
 await emit('screen','pointerdown',{button:0,pointerId:2,clientX:190,clientY:290});
 await emit('screen','pointermove',{buttons:1,clientX:250,clientY:290});
 await emit('screen','pointermove',{buttons:1,clientX:350,clientY:290});
 // The first 50ms timer is the coalesced UI pointer sender.
 await timers[2]();await flush();
 await emit('screen','pointerup',{button:0,pointerId:2,clientX:400,clientY:290});
 const pointers=requests.filter(request=>request.tool==='ui.pointer').map(request=>request.args.payload);
 assert.deepEqual(pointers.map(p=>({x:p.x,y:Math.round(p.y),pressed:p.pressed})),[
  {x:100,y:440,pressed:true},{x:260,y:440,pressed:true},{x:310,y:440,pressed:false}
 ]);
 assert.ok(pointers.every(p=>p.screenWidth===960&&p.screenHeight===540&&p.frame===100));
 await emit('screen','pointermove',{buttons:1,clientX:450,clientY:290});
 element('release').onclick();await flush();await timers[2]();await flush();
 assert.equal(requests.filter(request=>request.tool==='ui.pointer').length,3);
 element('acquire').onclick();await flush();
 rejectPointerRelease=true;
 await emit('screen','pointerup',{button:0,pointerId:3,clientX:1500,clientY:900});
 const safeRelease=requests.at(-1);
 assert.equal(safeRelease.tool,'input.release_all');
 assert.equal(safeRelease.args.instanceId,'p1');
 assert.equal(safeRelease.args.controlEpoch,7);
 // Editing the scenario must not move the game or change its inventory.
 document.activeElement={tagName:'TEXTAREA'};
 await emit('document','keydown',{code:'Digit3'});
 assert.equal(requests.filter(request=>request.tool==='input.execute').length,4);
 document.activeElement=element('screen');document.pointerLockElement=element('screen');
 await emit('screen','mousemove',{movementX:0,movementY:0});
 assert.match(element('camera-input-status').textContent,/입력 1회 · 이동 감지 0회/);
 await emit('screen','mousemove',{movementX:NaN,movementY:1});
 assert.match(element('camera-input-status').textContent,/읽지 못/);
 await emit('screen','mousemove',{movementX:30,movementY:-20});
 await emit('screen','mousemove',{movementX:10,movementY:5});
 await timers[3]();await flush();
 assert.deepEqual(requests.at(-1).args.payload.sequence,[{operation:'lookDelta',x:4,y:1.5}]);
 const countAfterLook=requests.length;await timers[3]();await flush();
 assert.equal(requests.length,countAfterLook);
 document.pointerLockElement=element('unrelated');
 await emit('screen','mousemove',{movementX:100,movementY:100});
 await timers[3]();await flush();
 assert.equal(requests.length,countAfterLook,'Only the game screen pointer lock may rotate the camera');
 document.pointerLockElement=null;await emit('document','pointerlockchange');
 assert.match(element('camera-status').textContent,/해제/);
 let finishShot!:()=>void;delayShot=new Promise(resolve=>{finishShot=resolve;});
 const beforeShots=requests.filter(request=>request.tool==='game.screenshot').length;
 element('capture').onclick();await flush();element('capture').onclick();await flush();
 assert.equal(requests.filter(request=>request.tool==='game.screenshot').length,beforeShots+1);
 element('instances').value='p2';await element('instances').onchange();await flush();
 assert.equal(element('screen').src,undefined);
 finishShot();await flush();
 assert.equal(element('screen').src,undefined,'A late frame from the previous player must not appear');
 // A stalled or failed emergency response must not allow new gameplay input.
 element('acquire').onclick();await flush();
 let failEmergency!:(error:Error)=>void;
 delayEmergency=new Promise((_,reject)=>{failEmergency=reject;});
 const inputCount=()=>requests.filter(request=>request.tool==='input.execute').length;
 const beforeEmergency=inputCount();
 element('release').onclick();await flush();
 await emit('document','keydown',{code:'KeyW'});
 assert.equal(inputCount(),beforeEmergency,'Emergency stop must disable input before the server replies');
 failEmergency(new Error('CONNECTION_LOST'));await flush();
 await emit('document','keydown',{code:'KeyA'});
 assert.equal(inputCount(),beforeEmergency,'A failed stop must not restore local control');
 delayEmergency=undefined;
 let finishAcquire!:()=>void;
 delayAcquire=new Promise(resolve=>{finishAcquire=resolve;});
 element('acquire').onclick();await flush();
 element('release').onclick();await flush();
 finishAcquire();await flush();
 await emit('document','keydown',{code:'KeyD'});
 assert.equal(inputCount(),beforeEmergency,'An acquire response issued before the stop must not restore input');
 availableInstances=[];element('refresh').onclick();await flush();
 element('assistance-refresh').onclick();await flush();
 const history=requests.findLast(request=>request.tool==='assistance.list');
 assert.equal(history.args.runId,undefined);
 assert.ok([...elements.values()].some(entry=>entry.textContent.includes('설정창을 열어 줘 <script>test</script>')));

});
