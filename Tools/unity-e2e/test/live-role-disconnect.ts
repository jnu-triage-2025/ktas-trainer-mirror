import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig } from '../src/core.ts';
import { Runner, type Definition, type Run } from '../src/runner.ts';
import { joinInstances } from '../src/startup.ts';

const platform = new Platform(await loadConfig(process.argv[2] ?? 'config.json'));
const definition: Definition = JSON.parse(readFileSync(new URL('../examples/role-branches-fixture.e2e.json', import.meta.url), 'utf8'));
let runId: string | undefined, state = 'blocked', failure: string | undefined;
const cleanupErrors: string[] = [];
let run: Run | undefined;
try {
  const launch = await platform.launch('mac_direct', 'host_plus_3_clients');
  runId = launch.runId;
  const { actors } = await joinInstances(platform, runId, AbortSignal.timeout(180000));
  const runner = new Runner(platform);
  const controller = new AbortController();
  run = { id: runId, state: 'running', steps: [], cleanupErrors, controller,
    definition, actors, eventCursors: {}, handoff: false, barriers: new Map() };
  const signal = AbortSignal.timeout(120000);
  const finishIndex = definition.steps.findIndex(step => step.id === 'finish_first_three');
  assert.ok(finishIndex > 0);
  for (const id of Object.values(actors)) await platform.acquire(id);
  await runner.steps(run, definition.steps.slice(0, finishIndex), signal);
  const before = await Promise.all(Object.values(actors).map(id => platform.observe(id)));
  await platform.artifact(runId, 'role-disconnect-before.json', before);
  assert.equal(before[3].dialogue.nodeId, 'branch_e2e_d');
  await platform.artifact(runId, 'role-disconnect-injection.json', {
    mode: 'process_termination', instanceId: actors.p4,
    reason: 'Terminate the owner of an unfinished role branch without submitting its choice',
    process: await platform.stop(actors.p4)
  });
  const deadline = performance.now() + 15000;
  while ((await platform.observe(actors.p1)).server.connections !== 3) {
    signal.throwIfAborted();
    if (performance.now() > deadline) throw new Error('DISCONNECTED_PLAYER_REMAINS_CONNECTED');
    await delay(200);
  }
  await runner.steps(run, [definition.steps[finishIndex]], signal);
  await runner.steps(run, definition.steps.filter(step => /^p[123]_joined$/.test(step.id)), signal);
  for (const actor of ['p1', 'p2', 'p3']) {
    const observed = await platform.observe(actors[actor]);
    assert.equal(observed.client.players.length, 3);
    assert.notEqual(observed.scenario.stateValues['branch.completion.done_e2e_d'], 'true',
      'Departure must not be recorded as successful completion of the departed role');
    await platform.artifact(runId, `${actor}-role-disconnect-after.json`, observed);
    await platform.artifact(runId, `${actor}-role-disconnect-events.json`, await platform.command(actors[actor], 'events.read'));
  }
  state = 'passed';
} catch (error) {
  state = 'failed'; failure = String(error);
  if (runId) for (const instance of platform.instances.values()) {
    if (instance.state === 'EXITED') continue;
    try { await platform.artifact(runId, `${instance.id}-role-disconnect-failure.json`, await platform.observe(instance.id)); } catch {}
  }
} finally {
  try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
  const processes = platform.list();
  if (processes.some(instance => !['EXITED', 'START_FAILED'].includes(instance.state))) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
  const report = { runId, state, failure, steps: run?.steps, cleanupErrors, processes };
  if (runId) await platform.artifact(runId, 'role-disconnect-report.json', report);
  console.log(JSON.stringify(report));
  process.exitCode = state === 'passed' && !cleanupErrors.length ? 0 : 1;
}
