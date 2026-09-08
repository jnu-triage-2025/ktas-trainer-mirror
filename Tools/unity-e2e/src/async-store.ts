import {Worker} from 'node:worker_threads';
import {E2EError} from './errors.ts';
export class AsyncEventStore {
  private worker:Worker;
  private nextId=0;
  private pending=new Map<number,{resolve:(value:any)=>void;reject:(error:Error)=>void;bytes:number;timer:ReturnType<typeof setTimeout>}>();
  private queuedBytes=0;
  private failure?:Error;
  private closing?:Promise<void>;
  private limits:{requests:number;bytes:number;timeoutMs:number};
  constructor(root:string,limits={requests:2048,bytes:32*1024*1024,timeoutMs:30000}){
    this.limits=limits;
    this.worker=new Worker(new URL('./store-worker.ts',import.meta.url),{workerData:{root},execArgv:[]});
    this.worker.on('message',message=>{
      const request=this.pending.get(message.id);if(!request)return;
      this.pending.delete(message.id);this.queuedBytes-=request.bytes;clearTimeout(request.timer);
      if (!this.pending.size) this.worker.unref();
      if(message.error){const error=new E2EError(message.error.code,message.error.message);request.reject(error);if(message.error.code!=='OBSERVATION_GAP')this.fail(error);}
      else request.resolve(message.value);
    });
    this.worker.on('error',error=>this.fail(error));
    this.worker.on('exit',code=>{if(!this.closing||code!==0||this.pending.size)this.fail(new E2EError('HISTORY_WORKER_EXITED'));});
    this.worker.unref();
  }
  assertHealthy(){if(this.failure)throw this.failure;if(this.closing)throw new E2EError('HISTORY_CLOSED');}
  private fail(error:Error){
    this.failure??=error;
    for(const request of this.pending.values()){clearTimeout(request.timer);request.reject(this.failure);}
    this.pending.clear();this.queuedBytes=0;
    void this.worker.terminate();
  }
  private request(operation:string,args:unknown[]):Promise<any>{
    this.assertHealthy();
    const bytes=Buffer.byteLength(JSON.stringify(args));
    if(this.pending.size>=this.limits.requests||this.queuedBytes+bytes>this.limits.bytes){
      const error=new E2EError('HISTORY_QUEUE_FULL');this.fail(error);throw error;
    }
    const id=++this.nextId;
    this.worker.ref();
    const result=new Promise((resolve,reject)=>{
      const timer=setTimeout(()=>this.fail(new E2EError('HISTORY_STORAGE_TIMEOUT')),this.limits.timeoutMs);
      this.pending.set(id,{resolve,reject,bytes,timer});this.queuedBytes+=bytes;
      try{this.worker.postMessage({id,operation,args});}catch(error){this.fail(error as Error);}
    });
    // Heartbeat audit writes may be detached; errors remain sticky and surface on the next call or close.
    void result.catch(()=>{});
    return result;
  }
  append(runId:string,instanceId:string,kind:string,body:unknown){return this.request('append',[runId,instanceId,kind,body]);}
  appendMany(runId:string,instanceId:string,kind:string,bodies:unknown[]){return this.request('appendMany',[runId,instanceId,kind,bodies]);}
  cursor():Promise<number>{return this.request('cursor',[]);}
  read(instanceId:string,after:number,runId:string):Promise<{id:number;kind:string;timestamp:string;body:any}[]>{return this.request('read',[instanceId,after,runId]);}
  close():Promise<void>{
    if(this.closing)return this.closing;
    let drain:Promise<any>;
    try{drain=this.request('close',[]);}catch(error){drain=Promise.reject(error);}
    this.closing=(async()=>{try{await drain;}finally{await this.worker.terminate();}})();
    return this.closing;
  }
}
