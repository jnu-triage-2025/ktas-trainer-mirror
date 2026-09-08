import { monitorEventLoopDelay } from 'node:perf_hooks';
import { arch, cpus, freemem, loadavg, platform as hostPlatform, totalmem } from 'node:os';
import { Platform, type Config } from './core.ts';
import { Runner, validate, type Definition } from './runner.ts';
import { joinInstances } from './startup.ts';
import { monitorMemoryPressure } from './memory-pressure.ts';
export async function regression(config:Config, definition:Definition, buildId:string, topology:string, signal:AbortSignal) {
  const validation=validate(definition);
  if(!validation.valid)throw new Error(validation.errors.join('\n'));
  if(definition.executionMode!=='regression')throw new Error('CLI regression execution cannot hand off control');
  if(definition.topology&&definition.topology!==topology)throw new Error('TOPOLOGY_MISMATCH');
  const platform=new Platform(config),runner=new Runner(platform);
  const started=performance.now(),cpuStart=process.cpuUsage(),lag=monitorEventLoopDelay({resolution:20});
  const hostStart={freeMemoryBytes:freemem(),loadAverage:loadavg()};lag.enable();
  const hostSamples: Array<{elapsedMs:number;freeMemoryBytes:number;loadAverage:number[];intervalMs:number;samplingDelayMs:number;cpuUserMs:number;cpuSystemMs:number}> = [];
  let lastSampleAt=started,lastSampleCpu=process.cpuUsage();
  let minFreeMemoryBytes=hostStart.freeMemoryBytes, peakLoadAverage1m=hostStart.loadAverage[0], sampleCount=0;
  const sampleHost=()=>{
    const now=performance.now(),cpu=process.cpuUsage(),intervalMs=now-lastSampleAt;
    const row={elapsedMs:now-started,freeMemoryBytes:freemem(),loadAverage:loadavg(),intervalMs,
      samplingDelayMs:Math.max(0,intervalMs-2000),cpuUserMs:(cpu.user-lastSampleCpu.user)/1000,cpuSystemMs:(cpu.system-lastSampleCpu.system)/1000};
    lastSampleAt=now;lastSampleCpu=cpu;
    minFreeMemoryBytes=Math.min(minFreeMemoryBytes,row.freeMemoryBytes);
    peakLoadAverage1m=Math.max(peakLoadAverage1m,row.loadAverage[0]);sampleCount++;
    hostSamples.push(row);if(hostSamples.length>300)hostSamples.shift();
    return row;
  };
  sampleHost();const sampleTimer=setInterval(sampleHost,2000);sampleTimer.unref();
  const pressure=monitorMemoryPressure(started);
  let processRunId:string|undefined,result:any;
  const cancel=()=>{for(const run of runner.runs.values())run.controller.abort();};
  signal.addEventListener('abort',cancel,{once:true});
  try {
    signal.throwIfAborted();
    const launched=await platform.launch(buildId,topology);processRunId=launched.runId;
    const joined=await joinInstances(platform,launched.runId,signal);
    signal.throwIfAborted();
    const {runId}=runner.start(definition,joined.actors);
    await runner.runs.get(runId)!.done;result=runner.status(runId);
  } catch(error) {
    result={state:signal.aborted?'cancelled':'blocked',error:String(error),cleanupErrors:[]};
  } finally {
    signal.removeEventListener('abort',cancel);
    clearInterval(sampleTimer);const beforeCleanup=sampleHost();const pressureStopped=pressure.stop();
    try {await platform.close();} catch(error) {result??={state:'blocked',cleanupErrors:[]};result.cleanupErrors.push(String(error));}
    await pressureStopped;
    lag.disable();
    const cpu=process.cpuUsage(cpuStart);
    const latency:Record<string,number[]>={};
    for(const entry of platform.audit){const key=String((entry.command as any)?.type);(latency[key]??=[]).push(Number(entry.elapsedMs));}
    const latencyMs=Object.fromEntries(Object.entries(latency).map(([key,values])=>{values.sort((a,b)=>a-b);return [key,{count:values.length,p50:values[Math.floor((values.length-1)*.5)],p95:values[Math.floor((values.length-1)*.95)],max:values.at(-1)}];}));
    if(processRunId)await platform.artifact(processRunId,'resources.json',{
      elapsedMs:performance.now()-started,host:{platform:hostPlatform(),arch:arch(),cpuModel:cpus()[0]?.model,logicalCpus:cpus().length,totalMemoryBytes:totalmem(),start:hostStart,beforeCleanup,minFreeMemoryBytes,peakLoadAverage1m,sampleCount,samples:hostSamples,memoryPressure:{source:"macOS memory_pressure -Q",samples:pressure.samples,interpretation:"OS-reported percentage; distinct from freeMemoryBytes, not an admission threshold or proof of failure cause"},end:{freeMemoryBytes:freemem(),loadAverage:loadavg()}},
      runner:{cpuUserMs:cpu.user/1000,cpuSystemMs:cpu.system/1000,memory:process.memoryUsage(),eventLoopDelayMs:{max:lag.max/1e6,p95:lag.percentile(95)/1e6}},commandLatencyMs:latencyMs,commandHistoryWrites:platform.commandHistoryWrites
    }).catch(error=>{result.cleanupErrors.push(String(error));});
    const processes=platform.list();
    if(processes.some(i=>!['EXITED','START_FAILED'].includes(i.state)))result.cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
    if(processRunId)await platform.artifact(processRunId,'shutdown.json',{processes,cleanupErrors:result.cleanupErrors});
  }
  return {...result,processRunId};
}
