import { AssistanceRequests } from './requests.ts';
import { RunCredentials, type RunGrant, type Capability } from './run-credentials.ts';
import { toolSchemas } from './tool-schema.ts';
import { randomUUID } from 'node:crypto';
import { joinInstances } from './startup.ts';
import { Platform, E2EError, type Owner } from './core.ts';
import { Runner, validate } from './runner.ts';

export const toolNames = ['protocol.signal_raise','fixture.scenario_start','assistance.submit','assistance.list','assistance.update','credentials.issue','credentials.revoke','network.status','network.fault','operations.status','operations.cancel','instances.join','editor.observe','editor.play','editor.stop','instances.attach_editor','instances.list','instances.launch','instances.stop','game.observe','game.catalogue','game.screenshot','ui.query','ui.activate',
  'ui.pointer','ui.text','input.execute','input.release_all','conditions.wait','events.read','scenario.validate','scenario.start',
  'scenario.status','scenario.cancel','control.emergency_stop','control.handoff','control.acquire','control.heartbeat','recording.start','recording.stop','artifacts.list','artifacts.read'] as const;
export class API {
  platform: Platform; runner: Runner; credentials = new RunCredentials(); assistance: AssistanceRequests;
  private requestRuns = new Map<string, Set<string>>();
  private requestTimers = new Map<string, ReturnType<typeof setTimeout>>();
  private requestCredentials = new Map<string, Set<string>>();
  operations = new Map<string, { runId?: string; state: string; result?: any; error?: string; controller: AbortController; done?: Promise<void> }>();
  constructor(platform: Platform, assistanceSnapshotPath?: string) { this.platform = platform; this.runner = new Runner(platform); this.assistance = new AssistanceRequests(assistanceSnapshotPath); }
  async callScoped(grant: RunGrant, name: string, args: Record<string, any>, caller: Owner) {
    const denied = () => { throw new E2EError('PERMISSION_DENIED'); };
    const ownsActors = (actors: Record<string, string> | undefined, capability: Capability) => {
      const ids = Object.values(actors ?? {});
      return ids.length > 0 && ids.every(id => {
        const instance = this.platform.instances.get(id);
        return !!instance && this.credentials.permits(grant, instance.runId, capability);
      });
    };
    if (name === 'protocol.signal_raise') {
      if (!ownsActors({client:args.instanceId},'control') || !ownsActors({client:args.instanceId},'protocol')) return denied();
      return this.call(name,args,caller);
    }
    if (name === 'fixture.scenario_start') {
      if (!ownsActors({server:args.instanceId},'control') || !ownsActors({server:args.instanceId},'fixture')) return denied();
      return this.call(name,args,caller);
    }
    if (name === 'assistance.list') {
      const runId = args.runId ?? grant.runId;
      if (!this.credentials.permits(grant, runId, 'observe')) return denied();
      return this.call(name, {...args, runId}, caller);
    }
    if (name === 'assistance.submit' || name === 'assistance.update') {
      const id = name === 'assistance.submit' ? args.instanceId : this.assistance.get(args.requestId)?.instanceId;
      if (!id || !ownsActors({ target: id }, 'observe') || !ownsActors({ target: id }, 'control')) return denied();
      return this.call(name, args, caller);
    }
    if (name === 'conditions.wait') {
      const authorized = () => ownsActors(args.actors, 'observe');
      if (!authorized()) return denied();
      const controller = new AbortController();
      const timer = setInterval(() => { if (!authorized()) controller.abort(); }, 100);
      timer.unref();
      try {
        const result = await this.call(name, args, caller, controller.signal);
        if (!authorized()) return denied();
        return result;
      } finally { clearInterval(timer); }
    }
    if (name === 'instances.join') {
      const authorized = () => this.credentials.permits(grant, args.runId, 'observe')
        && this.credentials.permits(grant, args.runId, 'control');
      if (!authorized()) return denied();
      const result = await this.call(name, args, caller);
      const job = this.operations.get(result.operationId);
      if (job) {
        const timer = setInterval(() => { if (!authorized()) job.controller.abort(); }, 100);
        timer.unref();
        void job.done?.finally(() => clearInterval(timer)).catch(() => {});
      }
      return result;
    }
    if (name === 'scenario.start') {
      if (args.requestId) {
        const request = this.assistance.get(args.requestId);
        if (!request || !this.credentials.permits(grant,request.runId,'observe')
          || !this.credentials.permits(grant,request.runId,'control')) return denied();
      }
      const authorized = () => ownsActors(args.actors, 'observe') && ownsActors(args.actors, 'control')
        && (!this.platform.config.fixture?.allowChatCommands || ownsActors(args.actors, 'fixture'));
      if (!authorized()) return denied();
      const result = await this.call(name, args, caller);
      const run = this.runner.runs.get(result.runId);
      if (run) {
        const timer = setInterval(() => { if (!authorized()) run.controller.abort(); }, 100);
        timer.unref();
        void run.done?.finally(() => clearInterval(timer)).catch(() => {});
      }
      return result;
    }
    if (name === 'recording.stop') {
      const id = this.platform.recordingInstance(args.recordingId);
      if (!id || !ownsActors({ recorded: id }, 'control') || !ownsActors({ recorded: id }, 'observe')) return denied();
      return this.call(name, args, caller);
    }
    if (name === 'scenario.validate') {
      if (!this.credentials.permits(grant, grant.runId, 'observe')) return denied();
      return this.call(name, args, caller);
    }
    if (name === 'scenario.status' || name === 'scenario.cancel') {
      const run = this.runner.runs.get(args.runId);
      if (!ownsActors(run?.actors, name === 'scenario.status' ? 'observe' : 'control')) return denied();
      return this.call(name, args, caller);
    }
    if (name === 'artifacts.list' || name === 'artifacts.read') {
      if (!this.credentials.permits(grant, args.runId, 'observe')
        && !ownsActors(this.runner.runs.get(args.runId)?.actors, 'observe')) return denied();
      return this.call(name, args, caller);
    }
    if (name === 'operations.status' || name === 'operations.cancel') {
      const job = this.operations.get(args.operationId);
      if (!job?.runId || !this.credentials.permits(grant, job.runId, name === 'operations.status' ? 'observe' : 'control')) return denied();
      return this.call(name, args, caller);
    }
    if (name === 'instances.list') {
      if (!this.credentials.permits(grant, grant.runId, 'observe')) return denied();
      return this.platform.list().filter(instance => instance.runId === grant.runId);
    }
    const read = ['game.observe','game.catalogue','game.screenshot','ui.query','events.read','network.status'];
    const control = ['ui.activate','ui.pointer','ui.text','input.execute','input.release_all',
      'control.acquire','control.handoff','control.heartbeat','control.emergency_stop','instances.stop','recording.start'];
    let capability: Capability;
    if (read.includes(name)) capability = 'observe';
    else if (control.includes(name)) capability = 'control';
    else if (name === 'network.fault') capability = 'protocol';
    else return denied();
    const instance = this.platform.instances.get(args.instanceId);
    if (!instance || !this.credentials.permits(grant, instance.runId, capability)) return denied();
    if (name === 'recording.start' && !this.credentials.permits(grant, instance.runId, 'observe')) return denied();
    if (['ui.activate','ui.pointer','ui.text','input.execute'].includes(name)
      && this.platform.config.fixture?.allowChatCommands
      && !this.credentials.permits(grant, instance.runId, 'fixture')) return denied();
    return this.call(name, args, caller);
  }
  private async stopAutomationFor(instanceId:string) {
    const instance = this.platform.get(instanceId);
    const pending: Promise<void>[] = [];
    for (const job of this.operations.values()) if (job.state === 'running' && job.runId === instance.runId) {
      job.controller.abort(); if (job.done) pending.push(job.done);
    }
    for (const run of this.runner.runs.values()) if (run.state === 'running' && Object.values(run.actors).includes(instanceId)) {
      run.controller.abort(); if (run.done) pending.push(run.done);
    }
    const results = await Promise.allSettled(pending);
    const failures = results.filter((result): result is PromiseRejectedResult => result.status === 'rejected');
    if (failures.length) throw new E2EError('CLEANUP_FAILED', failures.map(result => String(result.reason)).join('; '));
  }
  async call(name: string, args: Record<string, any>, caller: Owner = 'Automation', signal?: AbortSignal): Promise<any> {
    const schema = toolSchemas[name];
    if (!schema) throw new E2EError('UNKNOWN_TOOL');
    const parsed = schema.safeParse(args);
    if (!parsed.success) throw new E2EError('INVALID_ARGUMENT', parsed.error.message);
    args = parsed.data;
    const p = this.platform;
    switch (name) {
      case 'protocol.signal_raise': {
        if (p.config.fixture?.allowProtocolTests !== true) throw new E2EError('PROTOCOL_TESTS_DISABLED');
        return p.command(args.instanceId,name,args.payload,{owner:caller,epoch:args.controlEpoch,ttlMs:args.ttlMs??3000});
      }
      case 'fixture.scenario_start': {
        if (p.config.fixture?.allowScenarioFixtures !== true) throw new E2EError('FIXTURE_DISABLED');
        return p.command(args.instanceId,name,args.payload,{owner:caller,epoch:args.controlEpoch,ttlMs:args.ttlMs??3000});
      }
      case 'assistance.submit': {
        const instance = p.get(args.instanceId);
        if (['EXITED','CRASHED','START_FAILED'].includes(instance.state)) throw new E2EError('INSTANCE_UNAVAILABLE');
        const request = this.assistance.submit(instance.runId, instance.id, args.prompt);
        await p.artifact(instance.runId, `assistance-${request.id}-${request.revision}.json`, request);
        return request;
      }
      case 'assistance.list': return this.assistance.list(args.runId);
      case 'assistance.update': {
        if (caller !== 'Automation' && args.state !== 'cancelled') throw new E2EError('PERMISSION_DENIED');
        if (args.timeoutMs !== undefined && args.state !== 'running') throw new E2EError('INVALID_ARGUMENT');
        const current = this.assistance.get(args.requestId);
        if (args.state === 'completed' && current?.state === 'running' && current.revision === args.revision
          && [...(this.requestRuns.get(args.requestId) ?? [])].some(id => this.runner.runs.get(id)?.state === 'running'))
          throw new E2EError('ASSISTANCE_EXECUTION_ACTIVE');
        const request = this.assistance.transition(args.requestId, args.revision, args.state, args.result);
        if (request.state === 'cancelled' || request.state === 'failed') for (const runId of this.requestRuns.get(request.id) ?? []) {
          const run = this.runner.runs.get(runId);
          if (run?.state === 'running') run.controller.abort();
        }
        if (request.state === 'running' && args.timeoutMs !== undefined) {
          const timer = setTimeout(() => {
            const current = this.assistance.get(request.id);
            if (current?.state !== 'running' || current.revision !== request.revision) return;
            for (const id of this.requestCredentials.get(request.id) ?? []) this.credentials.revoke(id);
            for (const id of this.requestRuns.get(request.id) ?? []) this.runner.runs.get(id)?.controller.abort();
            void this.call('assistance.update',{requestId:request.id,revision:request.revision,state:'failed',
              result:'AI 요청 처리 기한이 만료되었습니다. 실행 결과를 확인한 뒤 새 요청을 등록하세요.'})
              .catch(() => { console.error('Assistance deadline state persistence failed'); });
          },args.timeoutMs);timer.unref();this.requestTimers.set(request.id,timer);
        }
        if (['completed','failed','cancelled'].includes(request.state)) {
          const timer=this.requestTimers.get(request.id);if(timer)clearTimeout(timer);this.requestTimers.delete(request.id);
          for (const id of this.requestCredentials.get(request.id) ?? []) this.credentials.revoke(id);
          this.requestCredentials.delete(request.id);
        }
        await p.artifact(request.runId, `assistance-${request.id}-${request.revision}.json`, request);
        return request;
      }

      case 'credentials.issue': {
        if (![...p.instances.values()].some(instance => instance.runId === args.runId && !['EXITED','CRASHED','START_FAILED'].includes(instance.state))) throw new E2EError('UNKNOWN_RUN');
        if (args.requestId) {
          const request=this.assistance.get(args.requestId);
          if (!request || request.state !== 'running' || request.runId !== args.runId) throw new E2EError('INVALID_ASSISTANCE_REQUEST');
        }
        const credential=this.credentials.issue(args.runId,args.capabilities,args.ttlMs);
        if (args.requestId) {
          const ids=this.requestCredentials.get(args.requestId)??new Set<string>();ids.add(credential.id);this.requestCredentials.set(args.requestId,ids);
        }
        return credential;
      }
      case 'credentials.revoke': return { revoked: this.credentials.revoke(args.credentialId) };

      case 'operations.status': { const job = this.operations.get(args.operationId); if (!job) throw new E2EError('UNKNOWN_OPERATION'); return { state: job.state, result: job.result, error: job.error }; }
      case 'operations.cancel': { const job = this.operations.get(args.operationId); if (!job) throw new E2EError('UNKNOWN_OPERATION'); job.controller.abort(); return { state: 'cancelling' }; }
      case 'instances.join': {
        for (const job of this.operations.values()) if (job.state === 'running') throw new E2EError('PREPARATION_BUSY');
        const id = randomUUID(), job: { runId?: string; state: string; result?: any; error?: string; controller: AbortController; done?: Promise<void> } = { runId:args.runId, state: 'running', controller: new AbortController() };
        this.operations.set(id, job);
        job.done = joinInstances(p, args.runId, job.controller.signal).then(result => { job.result = result; job.state = 'completed'; })
          .catch(error => { job.error = String(error); job.state = job.controller.signal.aborted ? 'cancelled' : 'failed'; });
        return { operationId: id };
      }
      case 'editor.observe': case 'editor.play': case 'editor.stop': return p.editorCommand(name);
      case 'instances.attach_editor': return p.attachEditor();
      case 'instances.list': return p.list();
      case 'instances.launch': return p.launch(args.buildId, args.topology,args.networkProxy);
      case 'network.status': return p.networkStatus(args.instanceId);
      case 'network.fault': return p.networkFault(args.instanceId,args.rule,args.durationMs);
      case 'instances.stop': return p.stop(args.instanceId);
      case 'game.observe': return p.observe(args.instanceId,false,args.payload);
      case 'control.acquire': {
        if (caller === 'RemoteHuman') {
          await this.stopAutomationFor(args.instanceId);
          await p.observe(args.instanceId);
          const instance = p.get(args.instanceId);
          if (instance.owner === 'RemoteHuman') return p.command(instance.id, 'control.heartbeat', {}, { owner: 'RemoteHuman' });
          if (instance.owner === 'Automation') return p.handoff(instance.id, 'RemoteHuman', 'Automation');
        }
        return p.acquire(args.instanceId, caller);
      }
      case 'control.emergency_stop': {
        const stopping = this.stopAutomationFor(args.instanceId);
        const releasing = (async () => {
          const state = await p.observe(args.instanceId,true);
          return p.releaseControl(args.instanceId,state.control.owner);
        })();
        const [stopped,released] = await Promise.allSettled([stopping,releasing]);
        if (released.status === 'rejected') throw released.reason;
        if (stopped.status === 'rejected') throw new E2EError('CLEANUP_FAILED',
          'Input release completed, but automation cleanup failed: '+String(stopped.reason));
        return released.value;
      }
      case 'control.handoff': {
        const before = await p.observe(args.instanceId);
        if (args.controlEpoch !== undefined && args.controlEpoch !== before.control.controlEpoch) throw new E2EError('STALE_CONTROL_EPOCH');
        if (before.control.owner !== caller) throw new E2EError('CONTROL_DENIED');
        await this.stopAutomationFor(args.instanceId);
        const current = await p.observe(args.instanceId);
        if (current.control.owner === 'None') return args.owner === 'None' ? current.control : p.acquire(args.instanceId,args.owner);
        return p.handoff(args.instanceId, args.owner, caller);
      }
      case 'scenario.validate': return validate(args.definition);
      case 'scenario.start': {
        const request = args.requestId ? this.assistance.get(args.requestId) : undefined;
        if (args.requestId && !request) throw new E2EError('UNKNOWN_ASSISTANCE_REQUEST');
        if (request) {
          if (request.state !== 'running') throw new E2EError('ASSISTANCE_NOT_RUNNING');
          const actorIds = Object.values(args.actors) as string[];
          if (!actorIds.includes(request.instanceId) || actorIds.some(id => p.get(id).runId !== request.runId))
            throw new E2EError('ASSISTANCE_ACTOR_MISMATCH');
        }
        const result = this.runner.start(args.definition, args.actors);
        if (request) {
          const linked = this.requestRuns.get(request.id) ?? new Set<string>();
          linked.add(result.runId);this.requestRuns.set(request.id,linked);
          try { await p.artifact(result.runId,'assistance-request.json',{requestId:request.id,processRunId:request.runId}); }
          catch (error) { this.runner.runs.get(result.runId)?.controller.abort();throw error; }
        }
        return result;
      }
      case 'scenario.status': return this.runner.status(args.runId);
      case 'scenario.cancel': return this.runner.cancel(args.runId);
      case 'recording.start': return p.recordingStart(args.instanceId);
      case 'recording.stop': return p.recordingStop(args.recordingId);
      case 'artifacts.read': return p.readArtifact(args.runId,args.name);
      case 'artifacts.list': return p.artifacts(args.runId);
      case 'conditions.wait': {
        const validation = validate({ version: '1.0', id: 'wait', executionMode: 'regression',
          participants: Object.fromEntries(Object.keys(args.actors ?? {}).map(id => [id, { networkRole: 'client' }])),
          steps: [{ id: 'condition', type: 'wait', predicate: args.predicate, args: args.args, timeoutMs: args.timeoutMs }] });
        if (!validation.valid) throw new E2EError('INVALID_ARGUMENT', validation.errors.join('; '));
        const run: any = { actors: args.actors, eventCursors:{} };
        if (args.predicate === 'event.occurred') for (const [actor,id] of Object.entries(args.actors))
          run.eventCursors[actor] = (await p.observe(id as string)).eventCursor;
        await this.runner.step(run, { id: 'condition', type: 'wait', predicate: args.predicate, args: args.args, timeoutMs: args.timeoutMs }, AbortSignal.any([AbortSignal.timeout(args.timeoutMs), ...(signal ? [signal] : [])]));
        return { satisfied: true };
      }
      default:
        if (!['game.catalogue','game.screenshot','ui.query','ui.activate','ui.pointer','ui.text','input.execute','input.release_all','events.read','control.heartbeat'].includes(name)) throw new E2EError('UNKNOWN_TOOL');
        return p.command(args.instanceId, name, args.payload ?? {}, { owner: caller, epoch: args.controlEpoch, ttlMs: Math.min(args.ttlMs ?? (name === 'game.catalogue' ? 30000 : 3000), 30000) });
    }
  }
}
