import { Platform } from '../src/core.ts';
import { resolve } from 'node:path';
import { writeFile } from 'node:fs/promises';
import { setTimeout as delay } from 'node:timers/promises';
const root = resolve(import.meta.dirname, '../../..');
const p = new Platform({ builds: { mac: { executable: resolve(root, 'Build/E2E.app/Contents/MacOS/KTASTrainer') } }, artifactRoot: resolve(root, 'artifacts/unity-e2e') });
try {
  const launched = await p.launch('mac', 'single'); const id = launched.instances[0].instanceId;
  console.log('launched', id);
  let observed;
  for (let n = 0; n < 60; n++) {
    try { observed = await p.observe(id); break; } catch { await delay(1000); }
  }
  if (!observed) throw new Error('Bridge did not become ready');
  console.log('observe', JSON.stringify(observed));
  await p.acquire(id);
  console.log('query', JSON.stringify(await p.command(id, 'ui.query', { automationId: 'btnSettings' })));
  await p.command(id, 'ui.activate', { automationId: 'btnSettings', mode: 'device_input' });
  await delay(1000);
  const elements = await p.command(id, 'ui.query', { automationId: 'settings-root' });
  console.log('settings', JSON.stringify(elements));
  const shot = await p.command(id, 'game.screenshot');
  await writeFile(resolve(root, 'artifacts/unity-e2e/settings-smoke.jpg'), Buffer.from(shot.data, 'base64'));
  if (!elements.elements.some((e: any) => e.visible)) throw new Error('Settings did not open via device input');
  const handoff = await p.handoff(id, 'RemoteHuman'); console.log('handoff',JSON.stringify(handoff));
  await delay(2200); console.log('expired',JSON.stringify((await p.observe(id)).control));
} finally { await p.close(); }
