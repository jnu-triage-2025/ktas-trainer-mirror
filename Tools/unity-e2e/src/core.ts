import { E2EError } from './errors.ts';
export { E2EError } from './errors.ts';
import { createSocket } from 'node:dgram';
import { AsyncEventStore } from './async-store.ts';
import { recordingDraft } from './recording-draft.ts';
import { UdpFaultProxy, type FaultRule } from './udp-fault-proxy.ts';
import { randomBytes, randomUUID } from 'node:crypto';
import { mkdir, writeFile, readFile, readdir, realpath, stat } from 'node:fs/promises';
import { spawn, type ChildProcess } from 'node:child_process';
import { resolve, join, relative, isAbsolute, dirname } from 'node:path';
import { createServer } from 'node:net';
import { setTimeout as delay } from 'node:timers/promises';

export type Owner = 'Automation' | 'RemoteHuman' | 'None';
export type Instance = {
  id: string; runId: string; nodeId: string; role: string; endpoint: string;
  token: string; profile: string; state: string; process?: ChildProcess;
  epoch: number; owner: Owner; heartbeat?: ReturnType<typeof setInterval>;
  controlLeaseMs?: number;
  frame?: number; lastFrameAt?: number; exitCode?: number | null;
  udpProxy?: UdpFaultProxy; proxyPort?:number;
  stopping?: boolean; eventTimer?: ReturnType<typeof setInterval>; eventTask?: Promise<void>; eventCursor?: number;
};
export type Build = { executable: string; args?: string[]; frameRateLimit?: number; renderScale?: number; controlLeaseMs?: number };
export type Config = { builds: Record<string, Build>; artifactRoot: string; maxConcurrentInstances?: number; port?: number; editorConnectionFile?: string; fixture?: { disableTts?: boolean; allowChatCommands?: boolean; allowScenarioFixtures?: boolean; allowProtocolTests?: boolean };
  gateway?: {bind:string;publicOrigin:string;certFile:string;keyFile:string} };
export class Platform {
  config: Config;
  private store?: AsyncEventStore;
  private historyRunId?: string;
  // A Platform owns one live run at a time.  Keep event history with that
  // run so prior exploratory runs cannot grow one shared database forever.
  private history() { return this.store ??= new AsyncEventStore(join(this.config.artifactRoot, safeId(this.historyRunId ?? 'service'))); }
  private launchingRuns = new Set<string>();
  private launchReservations = new Map<string, number>();
  private closing = false;
  instances = new Map<string, Instance>();
  audit: Record<string, unknown>[] = [];
  commandHistoryWrites = { count:0, totalMs:0, maxMs:0, slow:[] as {at:string;type:string;instanceId:string;durationMs:number;events:number}[] };
  private recorders = new Map<string, { instanceId: string; start: number; before: any }>();
  constructor(config: Config) {
    if(config.maxConcurrentInstances !== undefined && (!Number.isSafeInteger(config.maxConcurrentInstances)||config.maxConcurrentInstances<1)) throw new E2EError('INVALID_CONFIG');
    this.config = config;
  }
  publicInstance(i: Instance) {
    return { instanceId: i.id, runId: i.runId, nodeId: i.nodeId, role: i.role,
      state: i.state, processId: i.process?.pid, controlEpoch: i.epoch, owner: i.owner,
      endpoint: i.endpoint, proxyPort:i.proxyPort, exitCode: i.exitCode, exitSignal:i.process?.signalCode };
  }
  list() { return [...this.instances.values()].map(i => this.publicInstance(i)); }
  async editorConnection() {
    if (!this.config.editorConnectionFile) throw new E2EError('EDITOR_NOT_CONFIGURED');
    return JSON.parse(await readFile(this.config.editorConnectionFile, 'utf8'));
  }
  async editorCommand(type: string) {
    if (type !== 'editor.play' && type !== 'editor.stop') return this.editorRequest(type);
    let acknowledgement: unknown;
    try { acknowledgement = await this.editorRequest(type); }
    catch (error) { if (error instanceof E2EError && error.code !== 'EDITOR_UNAVAILABLE') throw error; }
    const desired = type === 'editor.play', deadline = performance.now() + 15000;
    while (performance.now() < deadline) {
      try {
        const observed = await this.editorRequest('editor.observe');
        if (observed.playing === desired && !observed.compiling && !observed.updating)
          return {...observed, transitionConfirmed:true, acknowledgementReceived:acknowledgement !== undefined};
      } catch { /* Domain reload temporarily closes the control listener. Do not replay the transition. */ }
      await delay(200);
    }
    throw new E2EError('EDITOR_TRANSITION_UNCONFIRMED');
  }
  private async editorRequest(type: string) {
    const connection = await this.editorConnection();
    if (!Number.isInteger(connection.editorPort) || connection.editorPort < 1025 || connection.editorPort > 65535) throw new E2EError('INVALID_CONFIG');
    const response = await fetch(`http://127.0.0.1:${connection.editorPort}/command`, {
      method: 'POST', headers: { Authorization: `Bearer ${connection.token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({ type }), signal: AbortSignal.timeout(10000)
    });
    if (!response.ok) throw new E2EError('EDITOR_UNAVAILABLE');
    const result = await response.json(); if (!result.ok) throw new E2EError(result.error); return result;
  }
  async attachEditor() {
    const connection = await this.editorConnection();
    if (this.instances.has(connection.instanceId)) {
      if (this.get(connection.instanceId).runId !== connection.runId) throw new E2EError('EDITOR_SESSION_CHANGED','Detach the old editor instance before attaching the new play session');
      await this.observe(connection.instanceId); return this.publicInstance(this.get(connection.instanceId));
    }
    return this.attach(connection);
  }
  get(id: string) { const i = this.instances.get(id); if (!i) throw new E2EError('INSTANCE_UNAVAILABLE'); return i; }
  async command(id: string, type: string, payload: Record<string, unknown> = {}, options: { owner?: Owner; epoch?: number; ttlMs?: number; signal?: AbortSignal; releaseRead?: boolean } = {}) {
    const i = this.get(id);
    if (options.releaseRead && type !== 'game.observe') throw new E2EError('INVALID_ARGUMENT');
    const releasing = options.releaseRead || type === 'input.release_all' || (type === 'control.handoff' && payload.owner === 'None');
    if (!releasing) this.store?.assertHealthy();
    const historyErrors:string[] = [];
    const persist = async (write:()=>Promise<unknown>) => {
      try { const pending=write(); if (type !== 'control.heartbeat') await pending; }
      catch(error) { if (!releasing) throw error; historyErrors.push(String(error)); }
    };
    const ttlMs = options.ttlMs ?? (type === 'game.catalogue' ? 30000 : 3000);
    const started = Date.now();
    const startedMonotonic = performance.now();
    const startedAt = new Date(started).toISOString();
    const body = { protocolVersion: '1.0', runId: i.runId, instanceId: i.id,
      commandId: randomUUID(), controlEpoch: options.epoch ?? i.epoch, owner: options.owner ?? 'Automation', type, ttlMs, payload };
    let response;
    try {
      response = await fetch(i.endpoint + '/command', { method: 'POST',
        headers: { Authorization: `Bearer ${i.token}`, 'Content-Type': 'application/json', Connection: 'close' },
        body: JSON.stringify(body), signal: AbortSignal.any([AbortSignal.timeout(ttlMs), ...(options.signal ? [options.signal] : [])]) });
    } catch (error) {
      if (!options.signal?.aborted && !i.stopping && !['EXITED','CRASHED','START_FAILED'].includes(i.state)) i.state = 'DISCONNECTED';
      const code = options.signal?.aborted ? 'CANCELLED' : 'INSTANCE_UNAVAILABLE';
      const entry = { startedAt, at: new Date().toISOString(), instanceId: id, command: body, ok: false,
        error: { code, message: String(error) }, outcome: 'unknown', elapsedMs: Date.now() - started };
      this.audit.push(entry);
      await persist(()=>this.history().append(i.runId, id, 'command', entry));
      if (this.audit.length > 100_000) this.audit.shift();
      throw new E2EError(code, String(error));
    }
    if (!response.ok) {
      const entry = { startedAt, at: new Date().toISOString(), instanceId:id, command:body, ok:false,
        error:{code:'BRIDGE_HTTP_ERROR',httpStatus:response.status}, outcome:'unknown', elapsedMs:Date.now()-started };
      this.audit.push(entry); await persist(()=>this.history().append(i.runId,id,'command',entry));
      if (this.audit.length > 100_000) this.audit.shift();
      throw new E2EError('BRIDGE_HTTP_ERROR', `Bridge HTTP ${response.status}`);
    }
    const headersAt = performance.now();
    const bridgeHttpTimings=Object.fromEntries(['serialize','serve','resume'].flatMap(key=>{
      const raw=response.headers.get(`x-e2e-${key}-ms`),value=raw===null||raw.trim()===''?NaN:Number(raw);
      return Number.isFinite(value)&&value>=0?[[`${key}Ms`,value]]:[];
    }));
    let reply: { ok: boolean; error?: { code: string }; result: any; controlEpoch: number; frame: number; bridgeTimings?: {queueMs:number;processingMs:number} };
    try {
      reply = await response.json();
      if (!reply || typeof reply.ok !== 'boolean') throw new SyntaxError('Invalid bridge envelope');
    } catch (error) {
      const code = options.signal?.aborted ? 'CANCELLED' : error instanceof SyntaxError ? 'BRIDGE_INVALID_RESPONSE' : 'INSTANCE_UNAVAILABLE';
      const entry = { startedAt, at:new Date().toISOString(), instanceId:id, command:body, ok:false,
        error:{code}, outcome:'unknown', elapsedMs:Date.now()-started,
        transportTimings:{headersWaitMs:headersAt-startedMonotonic,bodyReadMs:performance.now()-headersAt} };
      this.audit.push(entry);
      await persist(()=>this.history().append(i.runId,id,'command',entry));
      if (this.audit.length > 100_000) this.audit.shift();
      throw new E2EError(code);
    }
    const bodyReadAt = performance.now();
    const entry = { startedAt, at: new Date().toISOString(), instanceId: id, command: body, ok: reply.ok, error: reply.error, pointerTarget:type==='ui.pointer'&&reply.ok?reply.result?.target:undefined, bridgeTimings: reply.bridgeTimings, bridgeHttpTimings, elapsedMs: Date.now() - started, transportTimings: {headersWaitMs:headersAt-startedMonotonic,bodyReadMs:bodyReadAt-headersAt} };
    this.audit.push(entry);
    const historyStarted = performance.now();
    const receivedEvents = type === 'events.read' && reply.ok ? reply.result.events ?? [] : [];
    try {
      await persist(()=>this.history().append(i.runId, id, 'command', entry));
      if (receivedEvents.length) await this.history().appendMany(i.runId, id, 'game', receivedEvents);
    } finally {
      const durationMs = performance.now() - historyStarted, timing = this.commandHistoryWrites;
      timing.count++; timing.totalMs += durationMs; timing.maxMs = Math.max(timing.maxMs, durationMs);
      if (durationMs >= 100) {
        timing.slow.push({at:new Date().toISOString(),type,instanceId:id,durationMs,events:receivedEvents.length});
        if (timing.slow.length > 32) timing.slow.shift();
      }
    }
    if (this.audit.length > 100_000) this.audit.shift();
    if (!reply.ok) throw new E2EError(reply.error?.code ?? 'INTERNAL_ERROR');
    if(!options.releaseRead && type === 'game.observe' && [...this.recorders.values()].some(record => record.instanceId === id))
      await this.history().append(i.runId,id,'observation',{scene:reply.result.scene,inputContext:reply.result.inputContext});
    if (type.startsWith('control.') && !['EXITED','CRASHED','START_FAILED'].includes(i.state) && reply.controlEpoch >= i.epoch) { i.epoch = reply.controlEpoch; i.owner = reply.result.owner; }
    if (type === 'game.screenshot' && reply.result) reply.result.transportTimings = {
      headersWaitMs: headersAt - startedMonotonic,
      bodyReadMs: bodyReadAt - headersAt,
      auditMs: performance.now() - bodyReadAt
    };
    if (historyErrors.length && reply.result && typeof reply.result === 'object') reply.result.historyErrors=historyErrors;
    return reply.result;
  }
  async observe(id: string, releaseRead = false, options: {includeStaticItems?:boolean; signal?:AbortSignal; ttlMs?:number} = {}) {
    const i = this.get(id);
    const {signal, ttlMs, ...payload} = options;
    const value = await this.command(id, 'game.observe', payload, {releaseRead, signal, ttlMs});
    if (i.frame !== value.frame) { i.frame = value.frame; i.lastFrameAt = Date.now(); }
    if (!i.stopping && !['EXITED','CRASHED','START_FAILED'].includes(i.state)) {
      if (i.lastFrameAt && Date.now() - i.lastFrameAt > 5000) i.state = 'UNRESPONSIVE';
      else i.state = value.client?.localPlayerReady || (i.role === 'dedicated' && value.server?.started) ? 'READY' : 'GAME_READY';
    }
    if (!['EXITED','CRASHED','START_FAILED'].includes(i.state) && value.control.controlEpoch >= i.epoch) { i.epoch = value.control.controlEpoch; i.owner = value.control.owner; }
    if (!releaseRead) this.collectEvents(i);
    return value;
  }
  private collectEvents(i: Instance) {
    if (i.eventTimer || i.stopping || ['EXITED','CRASHED','START_FAILED'].includes(i.state)) return;
    const timer = setInterval(() => {
      if (i.eventTask || i.stopping) return;
      i.eventTask = (async () => {
        try {
          const batch = await this.command(i.id,'events.read',{cursor:i.eventCursor ?? 0},{ttlMs:1500});
          i.eventCursor = batch.cursor;
        } catch (error) {
          if (error instanceof E2EError && error.code === 'OBSERVATION_GAP') {
            await this.history().append(i.runId,i.id,'observation_gap',{after:i.eventCursor ?? 0,reason:error.code});
            try { i.eventCursor = (await this.command(i.id,'game.observe')).eventCursor; } catch { }
          }
        }
      })().finally(() => { i.eventTask = undefined; });
    },1000);
    i.eventTimer = timer; timer.unref();
  }
  historyCursor() { return this.history().cursor(); }
  eventHistory(instanceId: string, after = 0) { return this.history().read(instanceId,after,this.get(instanceId).runId); }
  async acquire(id: string, owner: Owner = 'Automation') {
    const i = this.get(id);
    await this.observe(id);
    const result = await this.command(id, 'control.acquire', { owner });
    this.heartbeat(i, owner);
    return result;
  }
  heartbeat(i: Instance, owner: Owner) {
    if (i.heartbeat) clearInterval(i.heartbeat);
    // Human leases are renewed by the browser, so closing it releases inputs.
    if (owner !== 'Automation') { i.heartbeat = undefined; return; }
    let inFlight = 0;
    const controlEpoch = i.epoch;
    const leaseMs = i.controlLeaseMs ?? 2000;
    const stop = () => { clearInterval(timer); if (i.heartbeat === timer) i.heartbeat = undefined; };
    const timer = setInterval(async () => {
      if (i.stopping || i.owner !== owner || i.epoch !== controlEpoch || i.heartbeat !== timer) { stop(); return; }
      // Under four local Unity clients a frame can take several seconds. Keep
      // a small, bounded renewal window: this preserves a lease through one
      // lost reply without allowing an unbounded backlog of bridge requests.
      if (inFlight >= 2) return; inFlight++;
      try { await this.command(i.id, 'control.heartbeat', {}, { owner, epoch:controlEpoch, ttlMs: Math.max(1000, Math.min(4000, leaseMs - 1000)) }); }
      catch (error) {
        // A lost reply does not prove expiry. The next scheduled pulse must still use the same epoch.
        if (!(error instanceof E2EError) || error.code !== 'INSTANCE_UNAVAILABLE') stop();
      }
      finally { inFlight--; }
    }, leaseMs <= 2000 ? 500 : Math.max(500, Math.min(1500, Math.floor(leaseMs / 3))));
    i.heartbeat = timer;
    timer.unref();
  }
  async handoff(id: string, owner: Owner, from: Owner = 'Automation') {
    const i = this.get(id);
    const result = await this.command(id, 'control.handoff', { owner }, { owner: from });
    this.heartbeat(i, owner);
    return result;
  }
  async release(id: string, owner: Owner = 'Automation') {
    return this.command(id, 'input.release_all', {}, { owner });
  }
  async releaseControl(id: string, owner: Owner = 'Automation') {
    const instance = this.get(id);
    if (instance.heartbeat) { clearInterval(instance.heartbeat); instance.heartbeat = undefined; }
    const state = await this.observe(id,true);
    if (state.control.owner === 'None') return { alreadyReleased: true, controlEpoch: state.control.controlEpoch, ...(state.historyErrors ? {historyErrors:state.historyErrors} : {}) };
    if (state.control.owner !== owner) throw new E2EError('CONTROL_DENIED');
    try { return await this.handoff(id, 'None', owner); }
    catch (error) {
      if (!(error instanceof E2EError) || error.code !== 'STALE_CONTROL_EPOCH') throw error;
      const current = await this.observe(id,true);
      if (current.control.owner !== 'None') throw error;
      return { alreadyReleased: true, controlEpoch: current.control.controlEpoch, ...(current.historyErrors ? {historyErrors:current.historyErrors} : {}) };
    }
  }
  async attach(args: { instanceId: string; runId: string; port: number; token: string; role?: string }) {
    if (this.instances.has(args.instanceId) || !Number.isInteger(args.port) || args.port < 1025 || args.port > 65535 || args.token.length < 32)
      throw new E2EError('INVALID_ARGUMENT');
    const i: Instance = { id: args.instanceId, runId: args.runId, nodeId: 'local', role: args.role ?? 'editor',
      endpoint: `http://127.0.0.1:${args.port}`, token: args.token, profile: '', state: 'BRIDGE_CONNECTED', epoch: 0, owner: 'None' };
    this.instances.set(i.id, i);
    try { await this.observe(i.id); return this.publicInstance(i); }
    catch (error) { this.instances.delete(i.id); throw error; }
  }
  async launch(buildId: string, topology: string, networkProxy = false) {
    if(this.closing)throw new E2EError('PLATFORM_CLOSING');
    if(networkProxy && topology === 'single')throw new E2EError('UNSUPPORTED_TOPOLOGY');
    const build = this.config.builds[buildId];
    if (!build) throw new E2EError('UNKNOWN_BUILD');
    const executable = await realpath(build.executable);
    const editorHost = topology === 'editor_host_plus_3_clients' ? [...this.instances.values()].find(instance => instance.role === 'editor') : undefined;
    if (topology === 'editor_host_plus_3_clients' && !editorHost) throw new E2EError('EDITOR_HOST_REQUIRED');
    const editorState = editorHost ? await this.observe(editorHost.id) : undefined;
    if (editorHost && (!editorState.server?.started || !Number.isInteger(editorState.server.localPort) || editorState.server.localPort < 1025 || editorState.server.localPort > 65535)) throw new E2EError('EDITOR_HOST_REQUIRED');
    if (editorHost && (this.launchingRuns.has(editorHost.runId) || [...this.instances.values()].some(instance => instance !== editorHost && instance.runId === editorHost.runId))) throw new E2EError('EDITOR_GROUP_ALREADY_LAUNCHED');
    const roles = topology === 'editor_host_plus_3_clients' ? ['client','client','client'] : topology === 'host_plus_3_clients' ? ['host','client','client','client']
      : topology === 'dedicated_plus_4_clients' ? ['dedicated','client','client','client','client']
      : topology === 'single' ? ['client'] : null;
    if (!roles) throw new E2EError('UNSUPPORTED_TOPOLOGY');
    if(this.closing)throw new E2EError('PLATFORM_CLOSING');
    const active=[...this.instances.values()].filter(instance => !['EXITED','CRASHED','START_FAILED'].includes(instance.state)
      && (instance.role === 'editor' || !this.launchReservations.has(instance.runId))).length;
    const reserved=[...this.launchReservations.values()].reduce((sum,count)=>sum+count,0);
    const limit=this.config.maxConcurrentInstances??5;
    if(active+reserved+roles.length>limit) throw new E2EError('INSTANCE_CAPACITY_EXCEEDED',`Requested ${roles.length} instances with ${active+reserved} active or reserved; limit is ${limit}`);
    if (editorHost) this.launchingRuns.add(editorHost.runId);
    const runId = editorHost?.runId ?? randomUUID();
    this.historyRunId = runId;
    this.launchReservations.set(runId,roles.length);
    const created: Instance[] = [];
    try {
      const gamePort = editorHost ? editorState.server.localPort : await freeUdpPort();
      for (const [index, role] of roles.entries()) {
        const id = `${runId}-p${index + 1}`, port = await freePort();
        const profile = resolve(this.config.artifactRoot, runId, id, 'profile');
        await mkdir(profile, { recursive: true });
        const preferences = {
          'IntroScene.PlayerName': `E2E-P${index + 1}`,
          ...(this.config.fixture?.disableTts ? {'MultiplayerInfrastructure.TTSEngineDisabled':1} : {}),
          'MultiplayerInfrastructure.GraphicsSettings.v2': JSON.stringify({
            Profile: 1, ResolutionWidth: 960, ResolutionHeight: 540, WindowMode: 1,
            FullScreenMode: 3, FrameRateLimit: build.frameRateLimit ?? 30, VSync: false, RenderScale: build.renderScale ?? 0.75,
            TextureMipmapLimit: 2, TextureStreaming: true, TextureStreamingBudgetMb: 256
          })
        };
        await writeFile(join(profile, 'preferences.json'), JSON.stringify(preferences));
        await this.artifact(runId, `fixture-p${index + 1}.json`, { mode: 'setup_bypass', type: 'isolated_profile', preferences,
          allowChatCommands: this.config.fixture?.allowChatCommands === true, allowScenarioFixtures: this.config.fixture?.allowScenarioFixtures === true, allowProtocolTests: this.config.fixture?.allowProtocolTests === true });
        if(this.closing)throw new E2EError('PLATFORM_CLOSING');
        const i: Instance = { id, runId, nodeId: 'local', role, endpoint: `http://127.0.0.1:${port}`,
          token: randomBytes(32).toString('hex'), profile, state: 'STARTING', epoch: 0, owner: 'None', controlLeaseMs: build.controlLeaseMs ?? 2000 };
        if(networkProxy && role === 'client'){i.udpProxy=await UdpFaultProxy.create(gamePort,index+1);i.proxyPort=i.udpProxy.port;}
        if(this.closing){await i.udpProxy?.close();throw new E2EError('PLATFORM_CLOSING');}
        this.instances.set(id, i); created.push(i);
        i.process = spawn(executable, [...(build.args ?? []), ...(role === 'dedicated' ? ['--dedicatedserver', '--bind', '127.0.0.1', '--port', String(gamePort), '--datapackspath', join(profile,'DataPacks'), '--nolanbroadcast'] : []), '--e2e', '-screen-width', '960', '-screen-height', '540',
          '-screen-fullscreen', '0', '-logFile', join(profile, '..', 'unity.log')], {
          shell: false, stdio: ['ignore','ignore','ignore'], env: { ...process.env,
            UNITY_E2E_ALLOW_SCENARIO_FIXTURES: this.config.fixture?.allowScenarioFixtures === true ? '1' : '0',
            UNITY_E2E_ALLOW_PROTOCOL_TESTS: this.config.fixture?.allowProtocolTests === true ? '1' : '0',
            UNITY_E2E_ALLOW_CHAT_COMMANDS: this.config.fixture?.allowChatCommands === true ? '1' : '0',
            UNITY_E2E_CONTROL_LEASE_MS: String(build.controlLeaseMs ?? 2000),
            UNITY_E2E_TOKEN: i.token, UNITY_E2E_PORT: String(port), UNITY_E2E_INSTANCE: id,
            UNITY_E2E_RUN: runId, UNITY_E2E_PROFILE: profile, UNITY_E2E_GAME_PORT: String(i.proxyPort ?? gamePort) } });
        i.process.on('error', () => { i.state = 'START_FAILED'; void i.udpProxy?.close(); });
        i.process.on('exit', code => {
          i.exitCode = code; i.state = i.stopping ? 'EXITED' : 'CRASHED'; i.owner = 'None';
          void i.udpProxy?.close();
          if (i.heartbeat) clearInterval(i.heartbeat);
          if (i.eventTimer) clearInterval(i.eventTimer);
          i.heartbeat = undefined; i.eventTimer = undefined;
        });
      }
      // Launch acknowledgement deliberately does not imply game readiness or a joined room.
      await this.artifact(runId, 'launch.json', { runId, topology, gamePort, buildId, executable, networkProxy, buildArguments:build.args ?? [], instances: created.map(i => this.publicInstance(i)) });
      return { runId, instances: created.map(i => this.publicInstance(i)), topology, gamePort };
    } catch (error) { await Promise.allSettled(created.map(i => this.stop(i.id))); throw error; }
    finally { this.launchReservations.delete(runId); if (editorHost) this.launchingRuns.delete(editorHost.runId); }
  }
  networkStatus(id:string) {
    const proxy=this.get(id).udpProxy;if(!proxy)throw new E2EError('NETWORK_PROXY_UNAVAILABLE');
    return proxy.snapshot();
  }
  async networkFault(id:string,rule:FaultRule,durationMs:number) {
    const instance=this.get(id),proxy=instance.udpProxy;
    if(!proxy||instance.stopping||['EXITED','CRASHED','START_FAILED'].includes(instance.state))throw new E2EError('NETWORK_PROXY_UNAVAILABLE');
    await this.artifact(instance.runId,`network-fault-${randomUUID()}.json`,{instanceId:id,rule,durationMs,at:new Date().toISOString(),stage:'requested'});
    proxy.configure(rule,durationMs);
    const result=proxy.snapshot();await this.history().append(instance.runId,id,'network_fault',result);
    return result;
  }
  async diagnoseProcess(id: string) {
    const instance = this.get(id), child = instance.process;
    // Only sample a still-running process launched by this manager, never the user's Editor.
    if (process.platform !== 'darwin' || !child?.pid || child.exitCode !== null || child.signalCode !== null)
      return { captured: false, reason: 'UNAVAILABLE_OR_EXTERNAL_PROCESS' };
    const path = await this.artifact(instance.runId, `process-sample-${instance.id}.txt`, '');
    const started = performance.now();
    return new Promise<{captured:boolean;reason?:string;durationMs:number;exitCode?:number|null;exitSignal?:string|null;bytes?:number}>(done => {
      const sampler = spawn('/usr/bin/sample', [String(child.pid), '2', '10', '-file', path],
        { shell:false, stdio:['ignore','ignore','pipe'], timeout:45000 });
      let errors = '';
      sampler.stderr?.on('data', chunk => { errors = (errors + String(chunk)).slice(-2000); });
      sampler.once('error', error => done({captured:false,reason:String(error),durationMs:performance.now()-started}));
      sampler.once('close', async (code, signal) => {
        const bytes = await stat(path).then(value=>value.size,()=>0);
        done({captured:code===0&&bytes>0,durationMs:performance.now()-started,exitCode:code,exitSignal:signal,bytes,
          ...(code===0&&bytes>0?{}:{reason:code===0?'EMPTY_SAMPLE':errors||`sample exit ${code}, signal ${signal}`})});
      });
    });
  }
  async stop(id: string) {
    const i = this.get(id);
    i.stopping = true;
    await i.udpProxy?.close();
    if (i.eventTimer) clearInterval(i.eventTimer);
    // A bridge event read can be stuck while a player is shutting down; do
    // not let cleanup retain all four Unity processes indefinitely.
    if (i.eventTask) await Promise.race([i.eventTask.catch(()=>{}), delay(3000, undefined, { ref: false })]);
    if (i.heartbeat) clearInterval(i.heartbeat);
    if (i.owner !== 'None') {
      try { await Promise.race([this.handoff(id, 'None', i.owner), delay(3000, undefined, { ref: false })]); }
      catch { /* lease still expires in the bridge */ }
    }
    if (!i.process) { this.instances.delete(id); return { detached: true }; }
    if (!i.process.pid) return this.publicInstance(i);
    if (i.process.exitCode !== null || i.process.signalCode !== null) return this.publicInstance(i);
    i.state = 'STOPPING'; i.process.kill('SIGTERM');
    await Promise.race([new Promise<void>(done => i.process!.once('exit', () => done())), delay(5000, undefined, { ref: false })]);
    if (i.process.exitCode === null && i.process.signalCode === null) {
      const exited = new Promise<void>(done => i.process!.once('exit', () => done()));
      i.process.kill('SIGKILL');
      await exited;
    }
    return this.publicInstance(i);
  }
  async artifact(runId: string, name: string, value: unknown) {
    const directory = resolve(this.config.artifactRoot, safeId(runId));
    await mkdir(directory, { recursive: true });
    const path = join(directory, safeId(name));
    await writeFile(path, typeof value === 'string' ? value : JSON.stringify(value, null, 2));
    return path;
  }
  async artifacts(runId: string) {
    const directory = resolve(this.config.artifactRoot, safeId(runId));
    return (await readdir(directory, { withFileTypes: true })).filter(x => x.isFile()).map(x => ({ name: x.name }));
  }
  async readArtifact(runId:string, name:string) {
    const directory = await realpath(resolve(this.config.artifactRoot,safeId(runId)));
    const path = await realpath(join(directory,safeId(name)));
    if (dirname(path) !== directory || (await stat(path)).size > 16*1024*1024) throw new E2EError('INVALID_ARTIFACT');
    const text = await readFile(path,'utf8');
    return {name,content:name.endsWith('.json')?JSON.parse(text):text};
  }
  recordingInstance(id: string) { return this.recorders.get(id)?.instanceId; }
  async recordingStart(instanceId: string) {
    this.get(instanceId); const id = randomUUID();
    const before = await this.observe(instanceId);
    this.recorders.set(id, { instanceId, start: await this.history().cursor(), before }); return { recordingId: id };
  }
  async recordingStop(id: string) {
    const record = this.recorders.get(id); if (!record) throw new E2EError('UNKNOWN_RECORDING');
    const instance = this.get(record.instanceId);
    let after:any, observationError:string|undefined;
    try { after = await this.observe(record.instanceId); } catch(error) { observationError=String(error); }
    const entries = await this.history().read(record.instanceId, record.start,instance.runId);
    const path = await this.artifact(instance.runId, `recording-${id}.json`, { mode: 'raw_input', requiresReview: true, before:record.before, after, observationError, entries });
    const draft = recordingDraft(id,instance.role,entries,record.before,after);
    if(observationError){ draft.blockers.push(`종료 상태를 관측하지 못했습니다: ${observationError}`); draft.partialSteps=draft.definition?.steps ?? draft.partialSteps; draft.definition=null; }
    const draftPath = await this.artifact(instance.runId, `recording-${id}-draft.json`, draft);
    this.recorders.delete(id);
    return { path, draftPath, runId:instance.runId, draftName:`recording-${id}-draft.json`, requiresReview:true, blockers:draft.blockers };
  }
  async close() { this.closing = true; await Promise.allSettled([...this.instances.keys()].map(id => this.stop(id))); try { await this.store?.close(); } finally { this.store = undefined; } }
}
export function safeId(value: string) {
  if (!/^[a-zA-Z0-9_.-]{1,180}$/.test(value) || value === '.' || value === '..') throw new E2EError('INVALID_ARGUMENT'); return value;
}
async function freePort() {
  const socket = createServer();
  await new Promise<void>((done, reject) => { socket.once('error', reject); socket.listen(0, '127.0.0.1', done); });
  const address = socket.address(); if (!address || typeof address === 'string') throw new E2EError('PORT_UNAVAILABLE');
  await new Promise<void>(done => socket.close(() => done())); return address.port;
}
export async function loadConfig(path: string): Promise<Config> {
  const config = JSON.parse(await readFile(path, 'utf8')) as Config;
  if (!config.builds || !config.artifactRoot) throw new E2EError('INVALID_CONFIG');
  if (config.fixture?.disableTts !== undefined && typeof config.fixture.disableTts !== 'boolean') throw new E2EError('INVALID_CONFIG');
  if (config.fixture?.allowChatCommands !== undefined && typeof config.fixture.allowChatCommands !== 'boolean') throw new E2EError('INVALID_CONFIG');
  if (config.fixture?.allowProtocolTests !== undefined && typeof config.fixture.allowProtocolTests !== 'boolean') throw new E2EError('INVALID_CONFIG');
  if (config.fixture?.allowScenarioFixtures !== undefined && typeof config.fixture.allowScenarioFixtures !== 'boolean') throw new E2EError('INVALID_CONFIG');
  const base = dirname(resolve(path));
  config.artifactRoot = resolve(base, config.artifactRoot);
  for (const build of Object.values(config.builds)) {
    if(build.controlLeaseMs!==undefined&&(!Number.isInteger(build.controlLeaseMs)||build.controlLeaseMs<2000||build.controlLeaseMs>10000))throw new E2EError('INVALID_CONFIG');
    if(build.renderScale!==undefined&&(!Number.isFinite(build.renderScale)||build.renderScale<0.25||build.renderScale>1))throw new E2EError('INVALID_CONFIG');
    if(build.frameRateLimit!==undefined&&(!Number.isInteger(build.frameRateLimit)||build.frameRateLimit<10||build.frameRateLimit>120))throw new E2EError('INVALID_CONFIG');
    if (typeof build.executable !== 'string' || (build.args && (!Array.isArray(build.args) || build.args.some(arg => typeof arg !== 'string')))) throw new E2EError('INVALID_CONFIG');
    build.executable = resolve(base,build.executable);
  }
  if (config.editorConnectionFile) config.editorConnectionFile = resolve(base,config.editorConnectionFile);
  if (config.gateway) {
    const gateway=config.gateway;
    if (![gateway.bind,gateway.publicOrigin,gateway.certFile,gateway.keyFile].every(value=>typeof value==='string'&&value.length>0))throw new E2EError('INVALID_GATEWAY_CONFIG');
    let origin:URL;try{origin=new URL(gateway.publicOrigin);}catch{throw new E2EError('INVALID_GATEWAY_CONFIG');}
    if(origin.protocol!=='https:'||origin.username||origin.password||origin.pathname!=='/'||origin.search||origin.hash
      ||Number(origin.port||443)!==(config.port??17890))throw new E2EError('INVALID_GATEWAY_CONFIG');
    gateway.publicOrigin=origin.origin;
    gateway.certFile=resolve(base,gateway.certFile);gateway.keyFile=resolve(base,gateway.keyFile);
  }
  return config;
}

async function freeUdpPort() {
  const socket = createSocket('udp4');
  await new Promise<void>((done, reject) => { socket.once('error', reject); socket.bind(0, '127.0.0.1', done); });
  const port = socket.address().port; await new Promise<void>(done => socket.close(done)); return port;
}
