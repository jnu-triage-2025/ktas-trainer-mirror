import {execFile} from 'node:child_process';

export type MemoryPressure = {available:true;systemWideFreePercent:number}|{available:false;reason:string};
export function readMemoryPressure():Promise<MemoryPressure> {
 if(process.platform!=='darwin')return Promise.resolve({available:false,reason:'unsupported_platform'});
 return new Promise(resolve=>execFile('/usr/bin/memory_pressure',['-Q'],{timeout:2000,killSignal:'SIGKILL',maxBuffer:16384,env:{...process.env,LC_ALL:'C'}},(error,stdout)=>{
  if(error){resolve({available:false,reason:error.killed?'query_timeout':'query_failed'});return;}
  const match=stdout.match(/^System-wide memory free percentage:\s*(\d+(?:\.\d+)?)%\s*$/m);
  const value=match?Number(match[1]):NaN;
  resolve(Number.isFinite(value)&&value>=0&&value<=100?{available:true,systemWideFreePercent:value}:{available:false,reason:'unrecognized_output'});
 }));
}
export function monitorMemoryPressure(started:number,query=readMemoryPressure,intervalMs=10000) {
 const samples:Array<{elapsedMs:number;durationMs:number;measurement:MemoryPressure}>=[];
 let pending:Promise<void>|undefined,stopped=false;
 const sample=()=>{
  if(stopped||pending)return;
  const at=performance.now();
  pending=Promise.resolve().then(query).catch(()=>({available:false as const,reason:'query_failed'})).then(measurement=>{
   samples.push({elapsedMs:at-started,durationMs:performance.now()-at,measurement});
   if(samples.length>300)samples.shift();
  }).finally(()=>{pending=undefined;});
 };
 sample();const timer=setInterval(sample,intervalMs);timer.unref();
 return {samples,stop:()=>{stopped=true;clearInterval(timer);return pending??Promise.resolve();}};
}
