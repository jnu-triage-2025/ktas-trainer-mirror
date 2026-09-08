import { createSocket, type Socket } from 'node:dgram';
import { performance } from 'node:perf_hooks';

export type FaultRule={delayMs:number;jitterMs:number;loss:number;disconnected:boolean;bytesPerSecond:number};
const clearRule:FaultRule={delayMs:0,jitterMs:0,loss:0,disconnected:false,bytesPerSecond:0};
export class UdpFaultProxy {
  private downstream:Socket;
  private upstream:Socket;
  private peerPort?:number;
  private closed=false;
  private rule:FaultRule={...clearRule};
  private expiresAt=0;
  private randomState:number;
  private timers=new Map<ReturnType<typeof setTimeout>,number>();
  private queuedBytes=0;
  private availableAt={up:0,down:0};
  readonly counters={received:0,sent:0,droppedFault:0,droppedQueue:0,rejectedPeer:0,sendErrors:0};
  private constructor(seed:number){
    this.randomState=seed>>>0||1;
    this.downstream=createSocket('udp4');this.upstream=createSocket('udp4');
    this.downstream.on('error',()=>{this.counters.sendErrors++;});
    this.upstream.on('error',()=>{this.counters.sendErrors++;});
    this.downstream.on('message',(message,peer)=>{
      if(peer.address!=='127.0.0.1'||(this.peerPort!==undefined&&peer.port!==this.peerPort)){this.counters.rejectedPeer++;return;}
      this.peerPort=peer.port;this.forward(message,'up');
    });
    this.upstream.on('message',message=>{if(this.peerPort!==undefined)this.forward(message,'down');});
  }
  static async create(targetPort:number,seed=1){
    if(!Number.isInteger(targetPort)||targetPort<1025||targetPort>65535||!Number.isInteger(seed))throw new Error('INVALID_PROXY_ARGUMENT');
    const proxy=new UdpFaultProxy(seed);
    try {
      await new Promise<void>((resolve,reject)=>{
        const fail=(error:Error)=>reject(error);proxy.downstream.once('error',fail);
        proxy.downstream.bind(0,'127.0.0.1',()=>{proxy.downstream.off('error',fail);resolve();});
      });
      await new Promise<void>((resolve,reject)=>{
        const fail=(error:Error)=>reject(error);proxy.upstream.once('error',fail);
        proxy.upstream.connect(targetPort,'127.0.0.1',()=>{proxy.upstream.off('error',fail);resolve();});
      });
      return proxy;
    }catch(error){await proxy.close();throw error;}
  }
  get port(){const address=this.downstream.address();if(typeof address==='string')throw new Error('INVALID_PROXY_ADDRESS');return address.port;}
  configure(rule:FaultRule,durationMs:number){
    if(this.closed||!Number.isFinite(durationMs)||durationMs<1||durationMs>300000
      ||!Number.isFinite(rule.delayMs)||rule.delayMs<0||rule.delayMs>1000
      ||!Number.isFinite(rule.jitterMs)||rule.jitterMs<0||rule.jitterMs>1000
      ||!Number.isFinite(rule.loss)||rule.loss<0||rule.loss>1||typeof rule.disconnected!=='boolean'
      ||!Number.isFinite(rule.bytesPerSecond)||rule.bytesPerSecond<0||(rule.bytesPerSecond>0&&rule.bytesPerSecond<1024)||rule.bytesPerSecond>10000000)
      throw new Error('INVALID_FAULT_RULE');
    this.rule={...rule};this.expiresAt=performance.now()+durationMs;
    this.availableAt={up:0,down:0};
  }
  private random(){let x=this.randomState;x^=x<<13;x^=x>>>17;x^=x<<5;this.randomState=x>>>0;return this.randomState/4294967296;}
  private forward(message:Buffer,direction:'up'|'down'){
    if(this.closed)return;
    this.counters.received++;
    const now=performance.now(),rule=now<this.expiresAt?this.rule:clearRule;
    if(rule.disconnected||this.random()<rule.loss){this.counters.droppedFault++;return;}
    const delay=Math.max(0,rule.delayMs+(this.random()*2-1)*rule.jitterMs);
    let due=now+delay;
    if(rule.bytesPerSecond)due=Math.max(due,this.availableAt[direction]);
    if(this.timers.size>=256||this.queuedBytes+message.length>1024*1024||due-now>5000){this.counters.droppedQueue++;return;}
    if(rule.bytesPerSecond)this.availableAt[direction]=due+message.length/rule.bytesPerSecond*1000;
    const send=()=>{
      if(this.closed)return;
      const callback=(error:Error|null)=>{if(error)this.counters.sendErrors++;else this.counters.sent++;};
      if(direction==='up')this.upstream.send(message,callback);
      else if(this.peerPort!==undefined)this.downstream.send(message,this.peerPort,'127.0.0.1',callback);
    };
    if(due<=now){send();return;}
    this.queuedBytes+=message.length;
    const timer=setTimeout(()=>{this.queuedBytes-=this.timers.get(timer)??0;this.timers.delete(timer);send();},due-now);
    this.timers.set(timer,message.length);
  }
  snapshot(){return {active:!this.closed&&performance.now()<this.expiresAt,rule:{...this.rule},remainingMs:Math.max(0,this.expiresAt-performance.now()),queuedPackets:this.timers.size,queuedBytes:this.queuedBytes,counters:{...this.counters}};}
  async close(){
    if(this.closed)return;this.closed=true;
    for(const timer of this.timers.keys())clearTimeout(timer);this.timers.clear();this.queuedBytes=0;
    await Promise.all([this.downstream,this.upstream].map(socket=>new Promise<void>(resolve=>{try{socket.close(()=>resolve());}catch{resolve();}})));
  }
}
