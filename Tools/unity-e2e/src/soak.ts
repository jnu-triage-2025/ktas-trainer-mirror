import { setTimeout as delay } from 'node:timers/promises';
import { performance, monitorEventLoopDelay } from 'node:perf_hooks';
import { freemem, loadavg } from 'node:os';
import { readProcessResources } from './process-resources.ts';
import { monitorMemoryPressure } from './memory-pressure.ts';
import { Platform, loadConfig } from './core.ts';
import { joinInstances } from './startup.ts';
import { readFile } from 'node:fs/promises';
import { Runner, validate, type Definition } from './runner.ts';
import { RoomHealth } from './room-health.ts';

// Keeps one room alive; an optional regression definition adds repeated gameplay assertions.
const [configPath, buildId, minutesText = '60', topology = 'host_plus_3_clients', definitionPath] = process.argv.slice(2);
const minutes = Number(minutesText);
if (!configPath || !buildId || !Number.isFinite(minutes) || minutes < 1 || minutes > 1440
  || !['host_plus_3_clients', 'dedicated_plus_4_clients'].includes(topology))
  throw new Error('Usage: node src/soak.ts CONFIG BUILD_ID [MINUTES:1..1440] [TOPOLOGY] [REGRESSION_DEFINITION]');
const definition:Definition|undefined=definitionPath?JSON.parse(await readFile(definitionPath,'utf8')):undefined;
if(definition){
 const checked=validate(definition);
 if(!checked.valid||definition.executionMode!=='regression'||(definition.topology&&definition.topology!==topology))
  throw new Error('Invalid regression definition: '+checked.errors.join('; '));
}
const mode=definition?'active_regression':'idle_room';
const platform = new Platform(await loadConfig(configPath));
const stop = new AbortController();
for (const signal of ['SIGINT', 'SIGTERM'] as const) process.once(signal, () => stop.abort());
let runId: string | undefined, state = 'blocked', failure: unknown, ready = false;
let cycles=0,activeRunner:Runner|undefined;
const monitoringStarted=performance.now(),monitoringStartedAt=new Date().toISOString();
const lag=monitorEventLoopDelay({resolution:20});lag.enable();
const pressure=monitorMemoryPressure(monitoringStarted);
let phase='preparation',lastSampleAt=monitoringStarted,lastCpu=process.cpuUsage();
const resourceSamples:unknown[]=[];
const processSamples:unknown[]=[];
let processSamplePending:Promise<void>|undefined;
const sampleProcesses=()=>{
 if(processSamplePending)return;
 const at=new Date().toISOString(),started=performance.now(),samplePhase=phase;
 const pids=[...platform.instances.values()].filter(i=>i.process?.pid&&i.process.exitCode===null&&i.process.signalCode===null).map(i=>i.process!.pid!);
 processSamplePending=readProcessResources(pids).then(measurement=>{
  processSamples.push({at,phase:samplePhase,durationMs:performance.now()-started,measurement});
  if(processSamples.length>300)processSamples.shift();
 }).finally(()=>{processSamplePending=undefined;});
};
const processSampleTimer=setInterval(sampleProcesses,10000);processSampleTimer.unref();
const sampleResources=()=>{
 const now=performance.now(),cpu=process.cpuUsage();
 resourceSamples.push({at:new Date().toISOString(),elapsedMs:now-monitoringStarted,phase,completedCycles:cycles,
  intervalMs:now-lastSampleAt,samplingDelayMs:Math.max(0,now-lastSampleAt-2000),
  cpuUserMs:(cpu.user-lastCpu.user)/1000,cpuSystemMs:(cpu.system-lastCpu.system)/1000,
  freeMemoryBytes:freemem(),loadAverage:loadavg(),runnerMemory:process.memoryUsage(),
  eventLoopDelayMs:{max:lag.max/1e6,p95:lag.percentile(95)/1e6}});
 lastSampleAt=now;lastCpu=cpu;lag.reset();
 if(resourceSamples.length>300)resourceSamples.shift();
};
const resourceTimer=setInterval(sampleResources,2000);resourceTimer.unref();
stop.signal.addEventListener('abort',()=>{for(const run of activeRunner?.runs.values()??[])run.controller.abort();});
const samples: unknown[] = [], cleanupErrors: string[] = [];
try {
  const launch = await platform.launch(buildId, topology); runId = launch.runId;
  const {actors}=await joinInstances(platform, runId, stop.signal); ready = true;
  if(definition)await platform.artifact(runId,'soak-definition.json',definition);
  const instances = [...platform.instances.values()].filter(i => i.runId === runId);
  const health = new RoomHealth();
  const started = performance.now(), deadline = started + minutes * 60000;
  do {
    stop.signal.throwIfAborted();
    if(definition){
      phase='active_regression';
      activeRunner=new Runner(platform);
      const current=activeRunner.start(structuredClone(definition),actors);
      await activeRunner.runs.get(current.runId)!.done;
      const result=activeRunner.status(current.runId);cycles++;
      await platform.artifact(runId,`soak-cycle-${cycles}.json`,result);
      if(result.state!=='passed'||result.cleanupErrors.length)throw new Error(`Active cycle ${cycles}: ${result.state} ${result.error?.code??''} ${result.cleanupErrors.join('; ')}`);
      if(cycles===1||cycles%10===0)console.log(JSON.stringify({runId,mode,cycles,state:'running',elapsedMs:performance.now()-started}));
      activeRunner=undefined;
    }
    phase='room_health';
    const observations = await Promise.all(instances.map(i => platform.observe(i.id)));
    samples.push({ elapsedMs: performance.now() - started, observations });
    health.check(instances, observations);
    await platform.artifact(runId, 'soak-progress.json', { mode, minutes, cycles, elapsedMs: performance.now() - started, samples: samples.length });
    // Flush bounded chunks so evidence survives interruption without an ever-growing JSON file.
    if (samples.length >= 60) { await platform.artifact(runId, `soak-samples-${Date.now()}.json`, samples); samples.length = 0; }
    if (performance.now() >= deadline) break;
    await delay(Math.min(1000, deadline - performance.now()), undefined, { signal: stop.signal });
  } while (true);
  state = 'passed';
} catch (error) {
  sampleResources();phase='failure_evidence';
  state = stop.signal.aborted ? 'cancelled' : ready ? 'failed' : 'blocked'; failure = String(error);
  if (runId) for (const instance of platform.instances.values()) {
    try { await platform.artifact(runId, `soak-failure-${instance.id}.json`, await platform.command(instance.id, 'game.screenshot', {}, { ttlMs: 2000 })); } catch { }
  }
} finally {
  sampleResources();phase='cleanup';
  try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
  clearInterval(processSampleTimer);await processSamplePending;
  clearInterval(resourceTimer);sampleResources();lag.disable();await pressure.stop();
  const processes = platform.list();
  if (processes.some(i => !['EXITED', 'START_FAILED'].includes(i.state))) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
  if (runId) {
    await platform.artifact(runId,'soak-resources.json',{startedAt:monitoringStartedAt,
      sampleIntervalMs:2000,retentionSamples:300,samples:resourceSamples,memoryPressure:pressure.samples,ownedProcessSamples:processSamples,
      interpretation:'Recent bounded samples; phase is sampled at callback time. Delayed callbacks can cross phase boundaries. Correlation does not establish failure cause.'});
    if (samples.length) await platform.artifact(runId, `soak-samples-final.json`, samples);
    await platform.artifact(runId, 'soak-report.json', { runId, mode, minutes, cycles, state, failure, cleanupErrors, processes });
  }
  console.log(JSON.stringify({ runId, mode, cycles, state, failure, cleanupErrors }));
  process.exitCode = state === 'passed' && !cleanupErrors.length ? 0 : 1;
}
