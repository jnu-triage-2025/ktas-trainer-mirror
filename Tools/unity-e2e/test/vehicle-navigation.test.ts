import test from 'node:test';
import assert from 'node:assert/strict';
import {drivePatientBed} from '../src/vehicle-navigation.ts';
import type {Platform} from '../src/core.ts';

test('defibrillator cart latching and CT coordinate completion remain independent',async()=>{
 for(const coordinateTarget of [false,true]){
  let moved=false,released=false;
  const vehicle={id:'cart',kind:'defibrillatorCart',position:[0,0,0],yaw:0,
   locallyControlled:true,latchedPointId:coordinateTarget?'point':null as string|null,
   positioningPoints:coordinateTarget?[]:[{id:'point',position:[0,0,2]}]};
  const p={observe:async()=>({vehicles:[vehicle]}),command:async()=>{
   moved=true;vehicle.position=[0,0,2];
   if(!coordinateTarget){vehicle.latchedPointId='point';vehicle.locallyControlled=false;}
  },release:async()=>{released=true;}} as unknown as Platform;
  await drivePatientBed(p,'p1','cart','point',AbortSignal.timeout(1000),1000,['p1'],[],0,
   'defibrillatorCart',coordinateTarget?[0,0,2]:undefined);
  assert.equal(moved,true);assert.equal(released,true);
 }
});

test('bed driving uses steering input and waits for observed latching',async()=>{
 const keys:string[]=[];let released=false;
 const bed={id:'bed',kind:'patientBed',position:[0,0,0],yaw:90,locallyControlled:true,latchedPointId:null as string|null,positioningPoints:[{id:'point',position:[0,0,1]}]};
 const p={observe:async()=>({vehicles:[bed]}),command:async(_id:string,_type:string,args:any)=>{
  const key=args.sequence[0].key;keys.push(key);
  if(key==='A')bed.yaw=0;
  else if(keys.length===2)bed.position=[0,0,1];
  else {bed.latchedPointId='point';bed.locallyControlled=false;}
 },release:async()=>{released=true;}} as unknown as Platform;
 await drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000));
 assert.deepEqual(keys,['A','W','W']);assert.equal(released,true);
});

test('bed driving rejects missing ownership and ambiguous targets without input',async()=>{
 for(const vehicles of [[],[{id:'bed',kind:'patientBed',locallyControlled:false}],
  [{id:'bed',kind:'patientBed'},{id:'bed',kind:'patientBed'}]]){
  let released=false;
  const p={observe:async()=>({vehicles}),command:async()=>assert.fail('unexpected input'),release:async()=>{released=true;}} as unknown as Platform;
  await assert.rejects(drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000)));
  assert.equal(released,true);
 }
});

test('cooperative bed failure waits for peers and releases every participant',async()=>{
 const sent:string[]=[],released:string[]=[];let peerFinished=false;
 const bed={id:'bed',kind:'patientBed',position:[0,0,0],yaw:0,locallyControlled:true,positioningPoints:[{id:'point',position:[0,0,2]}]};
 const p={observe:async()=>({vehicles:[bed]}),command:async(id:string)=>{
  sent.push(id);if(id==='p1')throw new Error('control lost');
  await new Promise(resolve=>setTimeout(resolve,10));peerFinished=true;
 },release:async(id:string)=>{assert.equal(peerFinished,true);released.push(id);}} as unknown as Platform;
 await assert.rejects(drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000),1000,['p1','p2']),/control lost/);
 assert.deepEqual(sent,['p1','p2']);assert.deepEqual(released,['p1','p2']);
});

test('cooperative bed movement refuses a participant who left the bed',async()=>{
 const released:string[]=[];
 const bed={id:'bed',kind:'patientBed',position:[0,0,0],yaw:0,locallyControlled:true,positioningPoints:[{id:'point',position:[0,0,2]}]};
 const p={observe:async(id:string)=>({vehicles:[{...bed,locallyControlled:id==='p1'}]}),command:async()=>assert.fail('unexpected input'),release:async(id:string)=>{released.push(id);}} as unknown as Platform;
 await assert.rejects(drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000),1000,['p1','p2']),/p2/);
 assert.deepEqual(released,['p1','p2']);
});

test('four participants receive the same steering and stop after the bed latches',async()=>{
 const participants=['p1','p2','p3','p4'],sent:Array<{id:string,key:string}>=[],released:string[]=[];
 const bed={id:'bed',kind:'patientBed',position:[0,0,0],yaw:0,locallyControlled:true,latchedPointId:null as string|null,positioningPoints:[{id:'point',position:[1,0,0]}]};
 const p={observe:async()=>({vehicles:[bed]}),command:async(id:string,_type:string,args:any)=>{
  sent.push({id,key:args.sequence[0].key});if(sent.length===4){bed.latchedPointId='point';bed.locallyControlled=false;}
 },release:async(id:string)=>{released.push(id);}} as unknown as Platform;
 await drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000),1000,participants);
 assert.deepEqual(sent,participants.map(id=>({id,key:'D'})));assert.deepEqual(released,participants);
});

test('peer observation failure waits for all concurrent reads before releasing input',async()=>{
 const started:string[]=[],released:string[]=[];let finishPeer!:()=>void;
 const peerGate=new Promise<void>(resolve=>{finishPeer=resolve;});
 let peerFinished=false;
 const bed={id:'bed',kind:'patientBed',position:[0,0,0],yaw:0,locallyControlled:true,positioningPoints:[{id:'point',position:[0,0,2]}]};
 const p={observe:async(id:string)=>{
  if(id==='p1')return {vehicles:[bed]};
  started.push(id);
  if(id==='p2'){await peerGate;throw new Error('peer observation failed');}
  finishPeer();peerFinished=true;return {vehicles:[bed]};
 },command:async()=>assert.fail('no input after observation failure'),release:async(id:string)=>{
  assert.equal(peerFinished,true);released.push(id);
 }} as unknown as Platform;
 await assert.rejects(drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000),1000,['p1','p2','p3']),/peer observation failed/);
 assert.deepEqual(started,['p2','p3']);assert.deepEqual(released,['p1','p2','p3']);
});

test('bed route visits the corridor before its destination and still requires latching',async()=>{
 const keys:string[]=[];let sample=0;
 const states=[{position:[0,0,0],yaw:90},{position:[2,0,0],yaw:90},{position:[2,0,0],yaw:0},{position:[2,0,4],yaw:0,latchedPointId:'point'}];
 const p={observe:async()=>({vehicles:[{id:'bed',kind:'patientBed',locallyControlled:true,positioningPoints:[{id:'point',position:[2,0,4]}],...states[Math.min(sample++,3)]}]}),
 command:async(_id:string,_type:string,payload:any)=>{keys.push(payload.sequence[0].key);},release:async()=>{}} as unknown as Platform;
 await drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000),1000,['p1'],[[2,0,0]]);
 assert.deepEqual(keys,['W','W']);
 await assert.rejects(drivePatientBed(p,'p1','bed','point',AbortSignal.timeout(1000),1000,['p1'],[[NaN,0,0]]),{code:'INVALID_VEHICLE_ROUTE'});
});
