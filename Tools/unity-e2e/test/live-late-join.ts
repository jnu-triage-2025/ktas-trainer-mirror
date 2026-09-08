import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig } from '../src/core.ts';
import { joinInstances } from '../src/startup.ts';

const platform = new Platform(await loadConfig(process.argv[2] ?? 'config.json'));
let runId: string | undefined, state = 'blocked', failure: string | undefined;
const cleanupErrors: string[] = [];
async function wait(check: () => Promise<boolean>, timeoutMs = 30000) {
  const deadline = performance.now() + timeoutMs;
  while (!await check()) {
    if (performance.now() >= deadline) throw new Error('LATE_JOIN_CONDITION_TIMEOUT');
    await delay(200);
  }
}
try {
  const launch = await platform.launch('mac_direct', 'host_plus_3_clients');
  runId = launch.runId;
  const { actors, deferredActors } = await joinInstances(platform, runId, AbortSignal.timeout(180000), ['p4']);
  const late = deferredActors.p4;
  assert.equal((await platform.observe(late)).scene, 'IntroScene');
  const before = await platform.observe(actors.p2);
  const player = before.client.players.find((value: any) => value.local);
  await platform.acquire(actors.p2);
  await platform.command(actors.p2, 'input.execute', { sequence: [{ operation: 'hold', key: 'S', durationMs: 1000 }] });
  await platform.releaseControl(actors.p2);
  const moved = await platform.observe(actors.p2);
  const position = moved.client.players.find((value: any) => value.local).position;
  assert.ok(Math.hypot(position[0] - player.position[0], position[2] - player.position[2]) > 1);
  await platform.artifact(runId, 'late-join-before.json', { before, moved, late: await platform.observe(late) });
  await platform.acquire(late);
  for (const automationId of ['btnPlay', 'btnDirectConnect', 'btnDirectJoin']) {
    await wait(async () => {
      const query = await platform.command(late, 'ui.query', { automationId });
      return query.elements.length === 1 && query.elements[0].interactable;
    });
    await platform.command(late, 'ui.activate', { automationId, mode: 'device_input' });
  }
  await wait(async () => {
    const observation = await platform.observe(late);
    const replicated = observation.client.players.find((value: any) => value.ownerId === player.ownerId);
    return observation.client.localPlayerReady && observation.client.players.length === 4 && replicated
      && Math.hypot(...position.map((value: number, index: number) => value - replicated.position[index])) < .5;
  }, 90000);
  await platform.releaseControl(late);
  for (const [actor, id] of Object.entries({ ...actors, ...deferredActors })) {
    await wait(async () => {
      const observation = await platform.observe(id);
      return observation.client.players.length === 4 && new Set(observation.client.players.map((value: any) => value.ownerId)).size === 4;
    });
    await platform.artifact(runId, `${actor}-late-join-after.json`, await platform.observe(id));
  }
  assert.equal((await platform.observe(actors.p1)).server.connections, 4);
  state = 'passed';
} catch (error) {
  state = 'failed'; failure = String(error);
  if (runId) for (const instance of platform.instances.values()) {
    try { await platform.artifact(runId, `${instance.id}-late-join-failure.json`, await platform.observe(instance.id)); } catch {}
  }
} finally {
  try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
  const processes = platform.list();
  if (processes.some(instance => !['EXITED', 'START_FAILED'].includes(instance.state))) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
  const report = { runId, state, failure, cleanupErrors, processes };
  if (runId) await platform.artifact(runId, 'late-join-report.json', report);
  console.log(JSON.stringify(report));
  process.exitCode = state === 'passed' && !cleanupErrors.length ? 0 : 1;
}
