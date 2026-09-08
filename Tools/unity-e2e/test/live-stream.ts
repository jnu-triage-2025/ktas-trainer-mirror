import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
const platform = new Platform(await loadConfig(process.argv[2] ?? 'config.json'));
let runId: string | undefined, state = 'failed', failure: string | undefined;
const samples: any[] = [], cleanupErrors: string[] = [];
try {
 const launch = await platform.launch('mac_direct', 'host_plus_3_clients'); runId = launch.runId;
 console.log(JSON.stringify({ runId, phase: 'joining' }));
 const { actors } = await joinInstances(platform, runId, AbortSignal.timeout(180000));
 console.log(JSON.stringify({ runId, phase: 'capturing' }));
 const ids = Object.values(actors);
 const started = performance.now(), end = started + 30000;
 const capture = async (id: string, rate: number) => {
  let lastFrame = -1;
  while (performance.now() < end) {
   const at = performance.now();
   const shot = await platform.command(id, 'game.screenshot', {}, { ttlMs: 1500 });
   assert.ok(shot.frame > lastFrame, 'Capture must advance'); lastFrame = shot.frame;
   assert.ok(Buffer.from(shot.data, 'base64').length > 100);
   samples.push({ kind: 'capture', id, at: at - started, elapsedMs: performance.now() - at, frame: shot.frame, captureTimings: shot.captureTimings, transportTimings: shot.transportTimings });
   await delay(Math.max(0, 1000 / rate - (performance.now() - at)));
  }
 };
 const health = async () => {
  while (performance.now() < end) {
   for (const id of ids) {
    const value = await platform.observe(id);
    samples.push({ kind: 'health', id, at: performance.now() - started, scene: value.scene, frame: value.frame, players: value.client.players.length });
    assert.equal(value.client.localPlayerReady, true); assert.equal(value.client.players.length, 4);
   }
   await delay(1000);
  }
 };
 const jobs = await Promise.allSettled([capture(actors.p1, 12), ...ids.filter(id => id !== actors.p1).map(id => capture(id, 1)), health()]);
 for (const job of jobs) if (job.status === 'rejected') throw job.reason;
 const main = samples.filter(s => s.kind === 'capture' && s.id === actors.p1);
 const fps = (main.length - 1) * 1000 / (main.at(-1).at - main[0].at);
 assert.ok(fps >= 10, `Main capture rate ${fps.toFixed(2)} FPS is below 10`);
 state = 'passed';
} catch (error) { failure = String(error); }
finally {
 try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
 if ([...platform.instances.values()].some(i => i.process && i.process.exitCode === null && i.process.signalCode === null)) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const captureSummary = [...new Set(samples.filter(s => s.kind === 'capture').map(s => s.id))].map(id => {
  const values = samples.filter(s => s.kind === 'capture' && s.id === id);
  const durations = values.map(s => s.elapsedMs).sort((a, b) => a - b);
  const mean = (values: number[]) => values.reduce((sum, value) => sum + value, 0) / values.length;
  const timingKeys = ['queueMs', 'frameWaitMs', 'readbackMs', 'encodeMs', 'base64Ms', 'headersWaitMs', 'bodyReadMs', 'auditMs'];
  return { instanceId: id, count: values.length,
    fps: values.length > 1 ? (values.length - 1) * 1000 / (values.at(-1).at - values[0].at) : null,
    meanMs: mean(durations), p95Ms: durations[Math.ceil(durations.length * .95) - 1],
    phaseMeanMs: Object.fromEntries(timingKeys.map(key => {
      const timings = values.map(s => s.captureTimings?.[key] ?? s.transportTimings?.[key]).filter(value => typeof value === 'number');
      return [key, timings.length ? mean(timings) : null];
    })) };
 });
 const report = { runId, state, failure, captureSummary, scope: 'Bridge capture throughput and four-player health; browser decode/display excluded', samples, cleanupErrors, processes: platform.list() };
 if (runId) await platform.artifact(runId, 'stream-report.json', report);
 console.log(JSON.stringify({ ...report, samples: samples.length }));
 process.exitCode = state === 'passed' && !cleanupErrors.length ? 0 : 1;
}
