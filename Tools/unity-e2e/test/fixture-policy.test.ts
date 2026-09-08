import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtemp, writeFile, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
import { Platform, loadConfig } from '../src/core.ts';

for (const [option, environment] of [['allowProtocolTests','UNITY_E2E_ALLOW_PROTOCOL_TESTS'],['allowChatCommands','UNITY_E2E_ALLOW_CHAT_COMMANDS'],['allowScenarioFixtures','UNITY_E2E_ALLOW_SCENARIO_FIXTURES']] as const)
test(`${option} requires explicit configuration and overrides inherited environment`, async () => {
  const root = await mkdtemp(join(tmpdir(), 'e2e-fixture-policy-'));
  const script = join(root, 'player.cjs');
  await writeFile(script, `require('node:fs').writeFileSync(process.env.UNITY_E2E_PROFILE+'/policy',process.env.${environment});setInterval(()=>{},1000);`);
  const inherited = process.env[environment];
  process.env[environment] = '1';
  try {
    for (const allowed of [undefined, false, true]) {
      const platform = new Platform({ builds: { fake: { executable: process.execPath, args: [script] } }, artifactRoot: root,
        fixture: { [option]: allowed } });
      try {
        const launched = await platform.launch('fake', 'single');
        const instance = [...platform.instances.values()][0];
        let actual: string | undefined;
        const deadline = performance.now() + 10000;
        while (actual === undefined) {
          try { actual = await readFile(join(instance.profile, 'policy'), 'utf8'); }
          catch { if (performance.now() > deadline) throw new Error('CHILD_START_TIMEOUT'); await delay(10); }
        }
        assert.equal(actual, allowed === true ? '1' : '0');
        const evidence = JSON.parse(await readFile(join(root, launched.runId, 'fixture-p1.json'), 'utf8'));
        assert.equal(evidence[option], allowed === true);
      } finally { await platform.close(); }
    }
    const config = join(root, 'invalid.json');
    await writeFile(config, JSON.stringify({ builds: {}, artifactRoot: root, fixture: { [option]: 'true' } }));
    await assert.rejects(loadConfig(config), /INVALID_CONFIG/);
  } finally {
    if (inherited === undefined) delete process.env[environment];
    else process.env[environment] = inherited;
    await rm(root, { recursive: true, force: true });
  }
});
