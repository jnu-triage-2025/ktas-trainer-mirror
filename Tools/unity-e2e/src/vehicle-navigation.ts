import {Platform,E2EError} from './core.ts';

export async function drivePatientBed(platform:Platform,instanceId:string,bedId:string,pointId:string,signal:AbortSignal,timeoutMs=60000,participantIds:string[]=[instanceId],via:number[][]=[],waypointSettleMs=0){
 if(!participantIds.includes(instanceId)||new Set(participantIds).size!==participantIds.length)throw new E2EError('INVALID_PARTICIPANTS');
 if(via.length>32||via.some(point=>point.length!==3||point.some(value=>!Number.isFinite(value))))throw new E2EError('INVALID_VEHICLE_ROUTE');
 const deadline=performance.now()+timeoutMs;let routeIndex=0,lastPeerValidation=-Infinity;
 let bestDistance=Infinity,bestAngle=Infinity,lastProgress=performance.now();
 try {
  while(performance.now()<deadline){
   signal.throwIfAborted();
   const snapshot=await platform.observe(instanceId,false,{includeStaticItems:false,signal,ttlMs:5000});
   const beds=(snapshot.vehicles??[]).filter((v:any)=>v.id===bedId&&v.kind==='patientBed');
   if(beds.length!==1)throw new E2EError('TARGET_NOT_FOUND',bedId);
   const bed=beds[0];
   if(bed.latchedPointId===pointId)return;
   if(!bed.locallyControlled)throw new E2EError('VEHICLE_NOT_CONTROLLED',bedId);
   const points=(bed.positioningPoints??[]).filter((p:any)=>p.id===pointId);
   if(points.length!==1)throw new E2EError('TARGET_NOT_FOUND',pointId);
   const target=via[routeIndex]??points[0].position;
   const dx=target[0]-bed.position[0],dz=target[2]-bed.position[2];
   const distance=Math.hypot(dx,dz),angle=((Math.atan2(dx,dz)*180/Math.PI-bed.yaw+540)%360)-180;
   if(!Number.isFinite(distance)||!Number.isFinite(angle))throw new E2EError('INVALID_VEHICLE_OBSERVATION');
   // Allow the bed body to clear doorway and furniture waypoints.
   if(routeIndex<via.length&&distance<.8){
    if(waypointSettleMs>0)await new Promise<void>((resolve,reject)=>{
     const timer=setTimeout(resolve,waypointSettleMs);
     signal.addEventListener('abort',()=>{clearTimeout(timer);reject(signal.reason);},{once:true});
    });
    routeIndex++;bestDistance=Infinity;bestAngle=Infinity;lastProgress=performance.now();continue;
   }
   if(distance<bestDistance-.05){bestDistance=distance;bestAngle=Infinity;lastProgress=performance.now();}
   if(Math.abs(angle)<bestAngle-1){bestAngle=Math.abs(angle);lastProgress=performance.now();}
   if(performance.now()-lastProgress>5000)throw new E2EError('NAVIGATION_STUCK',bedId);
   const key=Math.abs(angle)>8?(angle>0?'D':'A'):'W';
   const durationMs=key==='W'&&distance>1.5?200:100;
   const peers=participantIds.filter(id=>id!==instanceId);
   // Inputs still go to every participant on every steering step.  Querying
   // every peer at that same rate, however, overwhelms four local players;
   // sample ownership at a bounded cadence instead.
   if(performance.now()-lastPeerValidation>=2000){
    const observations=await Promise.allSettled(peers.map(participant=>platform.observe(participant,false,{includeStaticItems:false,signal,ttlMs:5000})));
    for(const [index,result] of observations.entries()){
     if(result.status==='rejected')throw result.reason;
     const controlled=(result.value.vehicles??[]).filter((v:any)=>v.id===bedId&&v.kind==='patientBed'&&v.locallyControlled);
     if(controlled.length!==1)throw new E2EError('VEHICLE_NOT_CONTROLLED',peers[index]);
    }
    lastPeerValidation=performance.now();
   }
   const results=await Promise.allSettled(participantIds.map(id=>platform.command(id,'input.execute',{sequence:[{operation:'hold',key,durationMs}]},{signal,ttlMs:4000})));
   for(const result of results)if(result.status==='rejected')throw result.reason;
  }
  throw new E2EError('DEADLINE_EXCEEDED',bedId);
 }finally{await Promise.allSettled(participantIds.map(id=>platform.release(id)));}
}
