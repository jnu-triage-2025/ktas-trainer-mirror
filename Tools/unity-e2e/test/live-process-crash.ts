import assert from 'node:assert/strict';
import { once } from 'node:events';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig, E2EError } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';
const platform = new Platform(await loadConfig(process.argv[2] ?? 'config.json'));
let runId: string | undefined, state = 'failed', failure: string | undefined;
const cleanupErrors: string[] = [];
try {
 const launch = await platform.launch('mac_direct', 'host_plus_3_clients'); runId = launch.runId;
 const { actors } = await joinInstances(platform, runId, AbortSignal.timeout(180000));
 const target = platform.get(actors.p2);
 const before = await platform.observe(actors.p1);
 await platform.acquire(target.id);
 await platform.artifact(runId, 'crash-before.json', { host: before, target: await platform.observe(target.id) });
 assert.ok(target.process?.pid);
 const exited = once(target.process!, 'exit');
 target.process!.kill('SIGKILL'); await exited;
 assert.equal(target.state, 'CRASHED'); assert.equal(target.process!.signalCode, 'SIGKILL');
 assert.equal(target.owner, 'None'); assert.equal(target.eventTimer, undefined); assert.equal(target.heartbeat, undefined);
 await assert.rejects(platform.observe(target.id), (error:unknown) => error instanceof E2EError && error.code === 'INSTANCE_UNAVAILABLE');
 assert.equal(target.state, 'CRASHED', 'A failed observation must not replace known process termination with disconnection');
 await platform.artifact(runId, 'crash-injection.json', { mode: 'process_fault', signal: 'SIGKILL', target: platform.publicInstance(target) });
 const deadline = performance.now() + 15000;
 while ((await platform.observe(actors.p1)).server.connections !== 3) {
  if (performance.now() > deadline) throw new Error('CRASHED_PLAYER_NOT_REMOVED'); await delay(200);
 }
 const host = await platform.observe(actors.p1);
 assert.ok(host.frame > before.frame); assert.ok(host.server.tick > before.server.tick);
 for (const actor of ['p1', 'p3', 'p4']) {
  const observation = await platform.observe(actors[actor]);
  assert.equal(observation.client.localPlayerReady, true); assert.equal(observation.client.players.length, 3);
  await platform.artifact(runId, `${actor}-after-crash.json`, observation);
 }
 state = 'passed';
} catch (error) { failure = String(error); }
finally {
 try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
 const processes = platform.list();
 if ([...platform.instances.values()].some(instance => instance.process && instance.process.exitCode === null && instance.process.signalCode === null)) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report = { runId, state, failure, cleanupErrors, processes };
 if (runId) await platform.artifact(runId, 'crash-report.json', report);
 console.log(JSON.stringify(report)); process.exitCode = state === 'passed' && !cleanupErrors.length ? 0 : 1;
}
