import {execFile} from 'node:child_process';
export function parseProcessResources(output:string) {
 return output.trim().split('\n').filter(Boolean).map(line=>{
  const [pid,cpuPercent,rssKiB]=line.trim().split(/\s+/).map(Number);
  if(!Number.isSafeInteger(pid)||pid<=0||!Number.isFinite(cpuPercent)||cpuPercent<0||!Number.isFinite(rssKiB)||rssKiB<0)
   throw new Error('INVALID_PROCESS_SAMPLE');
  return {pid,cpuPercent,rssBytes:rssKiB*1024};
 });
}
export function readProcessResources(pids:number[]):Promise<unknown> {
 const ids=[...new Set(pids)].filter(pid=>Number.isSafeInteger(pid)&&pid>0);
 if(!ids.length)return Promise.resolve({available:true,processes:[]});
 if(!['darwin','linux'].includes(process.platform))return Promise.resolve({available:false,reason:'unsupported_platform'});
 return new Promise(resolve=>execFile('/bin/ps',['-p',ids.join(','),'-o','pid=,pcpu=,rss='],
  {timeout:2000,killSignal:'SIGKILL',maxBuffer:16384,env:{...process.env,LC_ALL:'C'}},(error,stdout)=>{
   if(error){resolve({available:false,reason:error.killed?'query_timeout':'query_failed'});return;}
   try {const processes=parseProcessResources(stdout);resolve({available:true,processes:processes.filter(row=>ids.includes(row.pid)),missingPids:ids.filter(pid=>!processes.some(row=>row.pid===pid))});}
   catch {resolve({available:false,reason:'unrecognized_output'});}
  })).catch(()=>({available:false,reason:'query_failed'}));
}
