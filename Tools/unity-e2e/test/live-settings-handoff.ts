import assert from 'node:assert/strict';
import { readFile, writeFile } from 'node:fs/promises';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig, E2EError } from '../src/core.ts';
import { Runner } from '../src/runner.ts';
const platform = new Platform(await loadConfig(process.argv[2] ?? 'config.json'));
let runId: string | undefined, state = 'failed', failure: string | undefined, scenario: unknown;
const cleanupErrors: string[] = [];
try {
 const launch = await platform.launch('mac_direct', 'single'); runId = launch.runId;
 const id = launch.instances[0].instanceId, deadline = performance.now() + 30000;
 while (true) {
  try { await platform.observe(id); break; }
  catch (error) { if (!(error instanceof E2EError) || error.code !== 'INSTANCE_UNAVAILABLE') throw error; }
  if (performance.now() > deadline) throw new Error('BRIDGE_STARTUP_TIMEOUT'); await delay(200);
 }
 const definition = JSON.parse(await readFile(new URL('../examples/title-settings-handoff.e2e.json', import.meta.url), 'utf8'));
 const runner = new Runner(platform), started = runner.start(definition, { p1: id });
 await runner.runs.get(started.runId)!.done;
 scenario = runner.status(started.runId);
 assert.equal((scenario as any).state, 'handed_off', JSON.stringify(scenario));
 const observation = await platform.observe(id);
 assert.equal(observation.control.owner, 'RemoteHuman');
 const ui = await platform.command(id, 'ui.query', { automationId: 'settings-root' });
 assert.equal(ui.elements.length, 1); assert.equal(ui.elements[0].visible, true);
 const screenshot = await platform.command(id, 'game.screenshot');
 const path = await platform.artifact(runId, 'settings-handoff-evidence.json', { observation, ui, scenario });
 await writeFile(path.replace(/settings-handoff-evidence.json$/, 'settings-handoff.jpg'), Buffer.from(screenshot.data, 'base64'));
 state = 'passed';
} catch (error) { failure = String(error); }
finally {
 try { await platform.close(); } catch (error) { cleanupErrors.push(String(error)); }
 if ([...platform.instances.values()].some(i => i.process && i.process.exitCode === null && i.process.signalCode === null)) cleanupErrors.push('PROCESS_CLEANUP_INCOMPLETE');
 const report = { runId, state, failure, scenario, cleanupErrors, processes: platform.list() };
 if (runId) await platform.artifact(runId, 'settings-handoff-report.json', report);
 console.log(JSON.stringify(report)); process.exitCode = state === 'passed' && !cleanupErrors.length ? 0 : 1;
}
