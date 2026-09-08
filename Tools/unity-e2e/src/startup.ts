import { activateVisible } from './ui-navigation.ts';
import { Platform, E2EError } from './core.ts';
import { setTimeout as delay } from 'node:timers/promises';

export async function joinInstances(platform: Platform, runId: string, signal: AbortSignal, deferredActors: string[] = []) {
  const instances = [...platform.instances.values()].filter(i => i.runId === runId);
  if (!instances.length) throw new E2EError('UNKNOWN_RUN');
  const players = instances.filter(i => i.role !== 'dedicated');
  const actorEntries = players.map((instance, index) => [`p${index + 1}`, instance] as const);
  if (new Set(deferredActors).size !== deferredActors.length || deferredActors.some(actor =>
    !actorEntries.some(([name, instance]) => name === actor && instance.role === 'client')))
    throw new E2EError('INVALID_ARGUMENT', 'Only existing client actors may defer joining');
  const joiningPlayers = actorEntries.filter(([name]) => !deferredActors.includes(name)).map(([, instance]) => instance);
  async function wait(check: () => Promise<boolean>, timeout = 90000) {
    const deadline = performance.now() + timeout;
    while (true) {
      signal.throwIfAborted();
      if (performance.now() >= deadline) throw new E2EError('DEADLINE_EXCEEDED');
      try { if (await check()) return; }
      catch (error) {
        if (!(error instanceof E2EError) || error.code !== 'INSTANCE_UNAVAILABLE') throw error;
      }
      await delay(250, undefined, { signal });
    }
  }
  const observeProgress = (id: string) => platform.observe(id,false,{includeStaticItems:false});
  async function waitPlayer(id: string) {
    let stableSince=0,lastFrame=-1,lastScene='';
    await wait(async()=>{
      const value=await observeProgress(id),now=performance.now();
      if(!value.client.localPlayerReady||value.frame===lastFrame||value.scene!==lastScene)stableSince=0;
      lastFrame=value.frame;lastScene=value.scene;
      if(!value.client.localPlayerReady)return false;
      if(!stableSince)stableSince=now;
      return now-stableSince>=2000;
    });
  }
  const click = (id: string, name: string) => activateVisible(platform, id, name, signal);
  let phase = 'bridge_ready', activeInstance = '';
  const acquired: string[] = [];
  let primaryFailure: unknown;
  try {
    for (const i of instances) { activeInstance = i.id; await wait(async () => !!(await observeProgress(i.id))); }
    phase = 'server_selection';
    const host = instances.find(i => i.role === 'host') ?? instances.find(i => i.role === 'editor');
    const server = host ?? instances.find(i => i.role === 'dedicated');
    if (!server) throw new E2EError('MISSING_SERVER');
    phase = 'create_room'; activeInstance = server.id;
    if (host && !(await observeProgress(host.id)).server.started) { await platform.acquire(host.id); acquired.push(host.id); await click(host.id, 'btnPlay'); await click(host.id, 'btnHost'); }
    phase = 'server_started';
    await wait(async () => (await observeProgress(server.id)).server.started === true);
    if (host) {
      phase = 'host_spawned';
      await waitPlayer(host.id);
      await platform.releaseControl(host.id);
    }
    for (const i of joiningPlayers.filter(i => i !== host)) {
      phase = 'join_room'; activeInstance = i.id; i.state = 'JOINING';
      await platform.acquire(i.id); acquired.push(i.id);
      await click(i.id, 'btnPlay'); await click(i.id, 'btnDirectConnect'); await click(i.id, 'btnDirectJoin');
      phase = 'client_spawned';
      await waitPlayer(i.id);
      await platform.releaseControl(i.id);
    }
    phase = 'all_players_spawned';
    for (const i of joiningPlayers) { activeInstance = i.id; await wait(async () => {
      const value = await observeProgress(i.id);
      return value.client.localPlayerReady && value.client.players.length === joiningPlayers.length
        && new Set(value.client.players.map((p: any) => p.ownerId)).size === joiningPlayers.length;
    }); }
    const snapshots = await Promise.all(instances.map(i => platform.observe(i.id)));
    await platform.artifact(runId, 'startup.json', { mode: 'device_input', deferredActors, snapshots });
    return { ready: true, runId,
      actors: Object.fromEntries(actorEntries.filter(([name]) => !deferredActors.includes(name)).map(([name, i]) => [name, i.id])),
      deferredActors: Object.fromEntries(actorEntries.filter(([name]) => deferredActors.includes(name)).map(([name, i]) => [name, i.id])) };
  } catch (error) {
    primaryFailure = error;
    const observations: Record<string,unknown> = {};
    for (const instance of instances) {
      try { observations[instance.id] = await platform.observe(instance.id); }
      catch (failure) {
        let diagnostics: unknown = {captured:false,reason:signal.aborted?'startup_cancelled':'not_active_failure_participant'};
        if (!signal.aborted && instance.id === activeInstance) {
          try { diagnostics = await platform.diagnoseProcess(instance.id); } catch (error) { diagnostics = {error:String(error)}; }
        }
        observations[instance.id] = { error:String(failure), diagnostics };
      }
      try {
        await platform.artifact(runId, `startup-failure-ui-${instance.id}.json`,
          await platform.command(instance.id, 'ui.query', {}, { ttlMs: 2000 }));
      } catch (failure) {
        await platform.artifact(runId, `startup-failure-ui-${instance.id}.json`,
          { error: String(failure) }).catch(() => {});
      }
      try { await platform.artifact(runId, `startup-failure-${instance.id}.json`,
        await platform.command(instance.id,'game.screenshot',{}, {ttlMs:2000})); } catch { }
    }
    await platform.artifact(runId,'startup-failure.json', {
      phase, activeInstance, error:String(error), observations
    }).catch(() => {});
    throw error;
  } finally {
    const cleanupErrors: string[] = [];
    for (const id of acquired) if (platform.get(id).owner === 'Automation') {
      try { await platform.releaseControl(id); } catch (error) { cleanupErrors.push(String(error)); }
    }
    if (cleanupErrors.length) {
      await platform.artifact(runId, 'startup-cleanup.json', { primaryFailure: String(primaryFailure ?? ''), cleanupErrors }).catch(() => {});
      if (!primaryFailure) throw new E2EError('CLEANUP_FAILED', cleanupErrors.join('; '));
    }
  }
}
