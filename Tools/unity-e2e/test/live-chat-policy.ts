import assert from 'node:assert/strict';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig, E2EError } from '../src/core.ts';
const config = await loadConfig(process.argv[2] ?? 'config.json');
const reports: unknown[] = [];
let failed = false;
for (const allowed of [false, true]) {
 const platform = new Platform({ ...config, fixture: { ...config.fixture, allowChatCommands: allowed } });
 let runId: string | undefined, state = 'failed', failure: string | undefined;
 const cleanupErrors: string[] = [];
 try {
  const launch = await platform.launch('mac_direct', 'single'); runId = launch.runId;
  const id = launch.instances[0].instanceId;
  async function wait(check: (value: any) => boolean) {
   const deadline = performance.now() + 30000;
   while (true) {
    try { const value = await platform.observe(id); if (check(value)) return value; }
    catch (error) { if (!(error instanceof E2EError) || error.code !== 'INSTANCE_UNAVAILABLE') throw error; }
    if (performance.now() > deadline) throw new Error('CHAT_POLICY_STATE_TIMEOUT');
    await delay(150);
   }
  }
  await wait(value => value.scene === 'IntroScene'); await platform.acquire(id);
  await platform.command(id, 'ui.activate', { automationId: 'btnTutorial', mode: 'device_input' });
  const before = await wait(value => value.client.localPlayerReady && value.inputContext === 'Gameplay');
  const player = before.client.players.find((value: any) => value.local);
  assert.ok(!player.tags.includes('e2e_policy_probe'));
  await platform.command(id, 'input.execute', { sequence: [{ operation: 'tap', key: 'T' }] });
  await wait(value => value.inputContext === 'ChatUIController');
  await platform.command(id, 'ui.text', { text: `/tag add fish:${player.ownerId} e2e_policy_probe`, mode: 'input_adapter' });
  await platform.command(id, 'input.execute', { sequence: [{ operation: 'tap', key: 'Return' }] });
  let events: any;
  if (allowed) {
   await wait(value => value.client.players.find((p: any) => p.local).tags.includes('e2e_policy_probe'));
   events = await platform.command(id, 'events.read');
   assert.ok(!events.events.some((event: any) => event.eventType === 'permission.denied'));
  } else {
   const deadline = performance.now() + 5000;
   do {
    events = await platform.command(id, 'events.read');
    if (events.events.some((event: any) => event.eventType === 'permission.denied' && event.payload.capability === 'chat_commands')) break;
    if (performance.now() > deadline) throw new Error('MISSING_PERMISSION_DENIAL');
    await delay(100);
   } while (true);
   assert.ok(!(await platform.observe(id)).client.players.find((p: any) => p.local).tags.includes('e2e_policy_probe'));
  }
  await platform.artifact(runId, 'chat-policy-evidence.json', { allowed, before, after: await platform.observe(id), events });
  await platform.artifact(runId, 'chat-policy-screen.json', await platform.command(id, 'game.screenshot'));
  state = 'passed';
 } catch (error) { failure = String(error); failed = true; }
 finally {
  try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
  const processes = platform.list();
  if (processes.some(value => !['EXITED', 'START_FAILED'].includes(value.state))) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
  if (cleanupErrors.length) failed = true;
  const report = { runId, allowed, state, failure, cleanupErrors, processes }; reports.push(report);
  if (runId) await platform.artifact(runId, 'chat-policy-report.json', report);
 }
}
console.log(JSON.stringify(reports)); process.exitCode = failed ? 1 : 0;
