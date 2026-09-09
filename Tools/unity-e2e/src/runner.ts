import { activateVisible } from './ui-navigation.ts';
import { z } from 'zod';
import { Ajv2020 } from 'ajv/dist/2020.js';
import { readFileSync } from 'node:fs';
import { randomUUID, createHash } from 'node:crypto';
import { setTimeout as delay } from 'node:timers/promises';
import { E2EError, Platform } from './core.ts';

export type Step = { id: string; type: string; actor?: string; timeoutMs?: number; predicate?: string;
  args?: Record<string, any>; steps?: Step[]; branches?: Step[][]; participants?: string[];
  sequence?: Record<string, unknown>[]; documentId?: string; automationId?: string; mode?: string;
  text?:string; action?:string;
  owner?: 'RemoteHuman' | 'Automation'; target?: string; choiceId?: string; };
export type Definition = { version: '1.0'; id: string; executionMode: 'regression' | 'exploration';
  topology?: string; content?: { graphId: string; dataPacks: string[]; contentHash?: string };
  participants: Record<string, { networkRole: string }>; steps: Step[]; cleanup?: Step[] };
export type Run = { id: string; state: string; steps: Record<string, unknown>[]; error?: { code: string; message: string; category:string };
  cleanupErrors: string[]; controller: AbortController; done?: Promise<void>; definition: Definition;
  actors: Record<string, string>; eventCursors: Record<string,number>; handoff: boolean; barriers: Map<string, { participants: string[]; arrived: Set<string> }>; };
const schemaValidate = new Ajv2020({ allErrors: true, strict: false }).compile(JSON.parse(readFileSync(new URL('../scenario.schema.json', import.meta.url), 'utf8')));
const predicates = new Set(['input.context','dialogue.node','scenario.roleAllocation','inventory.itemCount','checklist.itemCompleted','interaction.available','scenario.mode','ui.valueEquals','quest.playerFlag','player.tag', 'event.occurred','participants.ready','ui.visible','ui.interactable','scene.is','dialogue.canAdvance','dialogue.hasChoices','scenario.node','players.count','player.near','scenario.stateValue','scenario.branch','quest.roleFlag']);
const types = new Set(['interact','uiText','input','uiAction','wait','assert','assertEventually','assertNever','parallel','barrier','handoff','checkpoint','navigate','dialogueAdvance','dialogueChoose','controlAcquire']);
const textArg = z.string().min(1), sideArg = z.enum(['server','client']);
const commonArgs = { actor: textArg.optional() };
const predicateArgs: Record<string, z.ZodTypeAny> = {
  'dialogue.node': z.object({...commonArgs,nodeId:textArg,graphId:textArg.optional()}).strict(),
  'scenario.roleAllocation': z.object({...commonArgs,graphId:textArg,nodeId:textArg,visit:z.number().int().positive(),ownerIds:z.array(z.number().int().min(-1)).nonempty()}).strict(),
  'inventory.itemCount': z.object({...commonArgs,ownerId:z.number().int().nonnegative(),itemId:textArg,count:z.number().int().nonnegative()}).strict(),
  'checklist.itemCompleted': z.object({...commonArgs,ownerId:z.number().int().nonnegative(),itemId:textArg,present:z.boolean(),slot:z.number().int().nonnegative().optional()}).strict(),
  'interaction.available':z.object({...commonArgs,interactionId:textArg,entityId:textArg.optional()}).strict(),
  'quest.playerFlag': z.object({...commonArgs,ownerId:z.number().int().nonnegative(),flag:textArg,present:z.boolean()}).strict(),
  'player.tag': z.object({...commonArgs,ownerId:z.number().int().nonnegative(),tag:textArg,present:z.boolean()}).strict(),
  'event.occurred': z.object({ ...commonArgs, eventType:textArg, side:z.enum(['server','client','process','automation']), match:z.record(z.union([z.string(),z.number().finite(),z.boolean(),z.null()])).optional(), contains:z.record(textArg).optional() }).strict(),
  'participants.ready': z.object({ players: z.array(textArg).nonempty().optional() }).strict(),
  'scenario.mode':z.object({...commonArgs,mode:z.enum(['Local','ServerAuthoritative','ClientPresentation'])}).strict(),
  'ui.valueEquals':z.object({...commonArgs,automationId:textArg,documentId:textArg.optional(),value:z.union([z.string(),z.number().finite(),z.boolean(),z.null()])}).strict(),
  'ui.visible': z.object({ ...commonArgs, automationId: textArg, documentId: textArg.optional(), visible:z.boolean().optional() }).strict(),
  'ui.interactable': z.object({ ...commonArgs, automationId: textArg, documentId: textArg.optional() }).strict(),
  'input.context': z.object({...commonArgs,context:textArg}).strict(),
  'scene.is': z.object({ ...commonArgs, scene: textArg }).strict(),
  'dialogue.canAdvance': z.object(commonArgs).strict(),
  'dialogue.hasChoices': z.object(commonArgs).strict(),
  'scenario.node': z.object({ ...commonArgs, nodeId: textArg, graphId:textArg.optional(), side: sideArg }).strict(),
  'scenario.stateValue': z.object({ ...commonArgs, key: textArg, value: z.union([z.string(),z.number().finite(),z.boolean(),z.null()]), side: sideArg }).strict(),
  'scenario.branch': z.object({ ...commonArgs, clientId: z.number().int().nonnegative(), branchId: textArg, side: sideArg }).strict(),
  'quest.roleFlag': z.object({ ...commonArgs, role: textArg, flag: textArg, side: sideArg }).strict(),
  'players.count': z.object({ ...commonArgs, count: z.number().int().nonnegative().max(64) }).strict(),
  'player.near': z.object({ ...commonArgs, position: z.tuple([z.number().finite(),z.number().finite(),z.number().finite()]), radius: z.number().finite().positive() }).strict()
};
export function validate(definition: unknown): { valid: boolean; errors: string[] } {
  const errors: string[] = [];
  if (!schemaValidate(definition)) return { valid: false, errors: schemaValidate.errors!.map(e => `${e.instancePath}: ${e.message}`) };
  if (!definition || typeof definition !== 'object') return { valid: false, errors: ['Definition must be an object'] };
  const d = definition as Definition;
  if (d.version !== '1.0') errors.push('version must be 1.0');
  if (typeof d.id !== 'string' || !d.id) errors.push('id is required');
  if (!['regression','exploration'].includes(d.executionMode)) errors.push('executionMode is invalid');
  if (!d.participants || typeof d.participants !== 'object' || Array.isArray(d.participants) || !Object.keys(d.participants).length) errors.push('participants are required');
  const ids = new Set<string>(); let count = 0;
  function visit(steps: Step[], depth: number, cleanup = false) {
    if (!Array.isArray(steps) || !steps.length) { errors.push('steps must be a nonempty array'); return; }
    if (depth > 8) { errors.push('nesting exceeds 8'); return; }
    for (const step of steps) {
      if (++count > 1000) { errors.push('step limit exceeded'); return; }
      if (!step || typeof step !== 'object') { errors.push('invalid step'); continue; }
      if (!step.id || ids.has(step.id)) errors.push(`Missing or duplicate step id: ${step.id}`); ids.add(step.id);
      if (!types.has(step.type)) errors.push(`${step.id}: unsupported type ${step.type}`);
      if (step.actor && !d.participants?.[step.actor]) errors.push(`${step.id}: unknown actor ${step.actor}`);
      if (['interact','uiText','input','uiAction','handoff','navigate','dialogueAdvance','dialogueChoose','controlAcquire'].includes(step.type) && !step.actor) errors.push(`${step.id}: actor required`);
      if (step.timeoutMs !== undefined && (!Number.isInteger(step.timeoutMs) || step.timeoutMs < 1 || step.timeoutMs > 300000)) errors.push(`${step.id}: invalid timeout`);
      if (['wait','assert','assertEventually','assertNever'].includes(step.type)) {
        if (!predicates.has(step.predicate ?? '')) errors.push(`${step.id}: unknown predicate`);
        else {
          const parsed = predicateArgs[step.predicate!].safeParse(step.args ?? {});
          if (!parsed.success) errors.push(`${step.id}: invalid predicate arguments: ${parsed.error.message}`);
          if (step.predicate === 'participants.ready') {
            if (step.args?.players?.some((actor: string) => !d.participants[actor])) errors.push(`${step.id}: unknown participant`);
          } else if (!d.participants[step.args?.actor ?? step.actor ?? '']) errors.push(`${step.id}: predicate actor required`);
        }
        if (step.type !== 'assert' && !step.timeoutMs) errors.push(`${step.id}: timeoutMs required`);
        if (step.type === 'assertNever' && step.predicate !== 'event.occurred') errors.push(`${step.id}: no lossless predicate backend available; assertNever cannot use snapshot polling`);
      }
      if (step.type === 'navigate' && !z.object({targetType:z.enum(['waypoint','npc','item','staticItem','scenarioEntity','vehicle']).optional(),targetOffset:z.tuple([z.number().finite().min(-10).max(10),z.number().finite().min(-10).max(10),z.number().finite().min(-10).max(10)]).optional(),arrivalRadius:z.number().finite().positive().max(10).optional(),stuckWindowMs:z.number().int().min(500).max(30000).optional()}).strict().safeParse(step.args??{}).success) errors.push(`${step.id}: invalid navigation arguments`);
      if (step.type === 'interact' && !z.object({entityId:textArg.optional()}).strict().safeParse(step.args??{}).success) errors.push(`${step.id}: invalid interaction arguments`);
      if (step.type === 'interact' && (!step.target || step.mode !== 'input_adapter')) errors.push(`${step.id}: interact requires target and input_adapter`);
      if (step.type === 'navigate' && (!step.timeoutMs || !step.target || step.mode !== 'input_adapter')) errors.push(`${step.id}: navigate requires target, timeoutMs and input_adapter`);
      if (step.type === 'dialogueChoose' && !step.choiceId) errors.push(`${step.id}: choiceId required`);
      if (step.type === 'input' && !step.action && (!Array.isArray(step.sequence) || step.sequence.length === 0 || step.sequence.length > 128)) errors.push(`${step.id}: sequence required`);
      if (step.type === 'input' && step.action && step.sequence) errors.push(`${step.id}: choose action or sequence`);
      if (step.type === 'uiText' && (typeof step.text !== 'string' || step.mode !== 'input_adapter')) errors.push(`${step.id}: text and input_adapter required`);
      if (step.type === 'uiAction' && (step.mode !== 'device_input' || !step.automationId)) errors.push(`${step.id}: device_input and automationId required`);
      if (step.type === 'handoff' && (d.executionMode !== 'exploration' || cleanup || depth > 0)) errors.push(`${step.id}: handoff requires a top-level exploration step`);
      if (step.type === 'parallel') {
        if (!Array.isArray(step.branches) || !step.branches.length || step.branches.length > 8) errors.push(`${step.id}: branches required (max 8)`);
        else for (const branch of step.branches) visit(branch, depth + 1, cleanup);
      }
      if (step.type === 'barrier' && (!step.timeoutMs || !step.actor || !step.args?.barrierId || !step.participants?.length || !step.participants.includes(step.actor) || step.participants.some(p => !d.participants?.[p]))) errors.push(`${step.id}: participants and timeout required`);
    }
  }
  visit(d.steps, 0); if (d.cleanup?.length) visit(d.cleanup, 0, true);
  return { valid: errors.length === 0, errors };
}
export class Runner {
  platform: Platform; runs = new Map<string, Run>();
  constructor(platform: Platform) { this.platform = platform; }
  start(definition: Definition, actors: Record<string, string>) {
    const validation = validate(definition); if (!validation.valid) throw new E2EError('INVALID_SCENARIO', validation.errors.join('\n'));
    for (const name of Object.keys(definition.participants)) { if (!actors[name]) throw new E2EError('MISSING_ACTOR'); this.platform.get(actors[name]); }
    if (new Set(Object.values(actors)).size !== Object.keys(actors).length) throw new E2EError('DUPLICATE_INSTANCE');
    for (const [name, spec] of Object.entries(definition.participants))
      if (this.platform.get(actors[name]).role !== spec.networkRole) throw new E2EError('NETWORK_ROLE_MISMATCH', name);
    if (Object.keys(actors).some(name => !definition.participants[name])) throw new E2EError('UNKNOWN_ACTOR');
    for (const active of this.runs.values()) if (active.state === 'running' && Object.values(actors).some(id => Object.values(active.actors).includes(id))) throw new E2EError('INSTANCE_BUSY');
    const run: Run = { id: randomUUID(), state: 'running', steps: [], cleanupErrors: [], controller: new AbortController(),
      definition: structuredClone(definition), actors: { ...actors }, eventCursors: {}, handoff: false, barriers: new Map() };
    this.runs.set(run.id, run); run.done = this.execute(run); return { runId: run.id };
  }
  status(id: string) {
    const run = this.runs.get(id); if (!run) throw new E2EError('UNKNOWN_RUN');
    return { runId: id, state: run.state, steps: run.steps, error: run.error, cleanupErrors: run.cleanupErrors };
  }
  cancel(id: string) { const run = this.runs.get(id); if (!run) throw new E2EError('UNKNOWN_RUN'); run.controller.abort(); return this.status(id); }
  async execute(run: Run) {
    const acquired: string[] = [];
    let historyStart:number|undefined;
    try {
      historyStart=await this.platform.historyCursor();
      await this.platform.artifact(run.id, 'definition.json', run.definition);
      await this.preflight(run);
      for (const id of Object.values(run.actors)) {
        const observation = await this.platform.observe(id);
        if (observation.control.owner === 'Automation') {
          // Quiesce exploratory input and retire its epoch before the fixed run begins.
          await this.platform.handoff(id, 'Automation', 'Automation');
        } else {
          await this.platform.acquire(id);
        }
        acquired.push(id);
      }
      await this.steps(run, run.definition.steps, run.controller.signal);
      run.state = run.handoff ? 'handed_off' : 'passed';
    } catch (error) {
      const e = error as Error; const code = error instanceof E2EError ? error.code : 'AUTOMATION_ERROR';
      const category = code === 'DEADLINE_EXCEEDED' ? 'unclassified'
        : ['INSTANCE_UNAVAILABLE','BRIDGE_HTTP_ERROR'].includes(code) ? 'infrastructure'
        : ['CONTENT_MISMATCH','CONTENT_REFERENCE_INVALID','DATAPACK_MISMATCH','NETWORK_ROLE_MISMATCH'].includes(code) ? 'content_or_spec'
        : ['ASSERTION_FAILED','NAVIGATION_STUCK','MOVEMENT_BLOCKED'].includes(code) ? 'product' : 'automation';
      run.error = { code, message: e.message, category };
      run.state = run.controller.signal.aborted ? 'cancelled' : ['INSTANCE_UNAVAILABLE','CONTROL_DENIED'].includes(code) ? 'blocked'
        : code === 'OBSERVATION_GAP' ? 'inconclusive' : 'failed';
      // Stop held input before potentially slow diagnostics; retain the original failure.
      await Promise.all(acquired.map(async id => {
        if (this.platform.get(id).owner !== 'Automation') return;
        try { await this.platform.command(id,'input.release_all'); }
        catch (error) { run.cleanupErrors.push(`Immediate input release ${id}: ${String(error)}`); }
      }));
      if (['INSTANCE_UNAVAILABLE','CONTROL_DENIED','STALE_CONTROL_EPOCH'].includes(code)) {
        try {
          const definitions = new Map<string,Step>();
          const collect = (steps:Step[]) => { for (const step of steps) { definitions.set(step.id,step);collect(step.steps??[]);for(const branch of step.branches??[])collect(branch); } };
          collect(run.definition.steps);
          const failedActor = [...run.steps].reverse().filter(step=>step.state==='failed').map(step=>{
            const definition=definitions.get(String(step.id));return definition?.actor??definition?.args?.actor;
          }).find(actor=>typeof actor==='string'&&run.actors[actor]);
          const actor = failedActor??Object.keys(run.actors)[0];
          const diagnostics = actor ? [{actor,instanceId:run.actors[actor],selection:failedActor?'failed_step':'first_participant',...await this.platform.diagnoseProcess(run.actors[actor])}] : [];

          await this.platform.artifact(run.id,'process-diagnostics.json',diagnostics);
        } catch(error) { run.cleanupErrors.push('Process diagnostics: '+String(error)); }
      }
      await this.evidence(run, 'failure');
    } finally {
      if (run.definition.cleanup?.length && !run.handoff) {
        try { await this.steps(run, run.definition.cleanup, AbortSignal.timeout(30000)); }
        catch (error) { run.cleanupErrors.push(String(error)); }
      }
      for (const id of acquired) {
        if (this.platform.get(id).owner === 'Automation') {
          try { await this.platform.releaseControl(id); } catch (error) { run.cleanupErrors.push(String(error)); }
        }
      }
      try {
        if(historyStart===undefined)throw new E2EError('OBSERVATION_GAP','Run history boundary unavailable');
        const inputs:{id:number;body:unknown}[]=[];
        for (const [actor,id] of Object.entries(run.actors)) {
          const history=await this.platform.eventHistory(id,historyStart);
          await this.platform.artifact(run.id,`history-${actor}.json`,history);
          inputs.push(...history.filter(entry=>entry.kind==='command'));
        }
        const hash = createHash('sha256').update(JSON.stringify(run.definition)).digest('hex');
        await this.platform.artifact(run.id, 'report.json', { ...this.status(run.id), definitionHash: hash, actors: run.actors,
          historyAfterCursor:historyStart, inputs:inputs.sort((a,b)=>a.id-b.id).map(entry=>entry.body) });
      } catch (error) { run.cleanupErrors.push(String(error)); if (run.state === 'passed') run.state = 'inconclusive'; }
    }
  }
  async preflight(run: Run) {
    const environments: Record<string,unknown> = {};
    let expectedHash = run.definition.content?.contentHash;
    for (const [actor,id] of Object.entries(run.actors)) {
      const state = await this.platform.observe(id);
      run.eventCursors[actor] = state.eventCursor ?? 0;
      environments[actor] = { instance:this.platform.publicInstance(this.platform.get(id)), state };
      const content = run.definition.content;
      if (!content) continue;
      const catalogue = await this.platform.command(id,'game.catalogue');
      await this.platform.artifact(run.id,`catalogue-${actor}.json`,catalogue);
      if (expectedHash && catalogue.contentHash !== expectedHash) throw new E2EError('CONTENT_MISMATCH',actor);
      expectedHash = catalogue.contentHash;
      if (JSON.stringify([...(state.activeDataPacks ?? [])].sort()) !== JSON.stringify([...content.dataPacks].sort())) throw new E2EError('DATAPACK_MISMATCH',actor);
      const graphs = (catalogue.graphs ?? []).filter((graph:any)=>graph.graphId===content.graphId);
      if (graphs.length!==1) throw new E2EError('CONTENT_REFERENCE_INVALID',content.graphId);
      const nodes = graphs[0].nodes;
      const visit = (steps:Step[]) => { for (const step of steps) {
        if (step.predicate==='scenario.node' && !nodes.some((node:any)=>node.nodeId===step.args?.nodeId)) throw new E2EError('CONTENT_REFERENCE_INVALID',step.id);
        if (step.type==='dialogueChoose' && !nodes.some((node:any)=>node.choices.includes(step.choiceId))) throw new E2EError('CONTENT_REFERENCE_INVALID',step.id);
        for (const branch of step.branches ?? []) visit(branch);
      } };
      visit(run.definition.steps); visit(run.definition.cleanup ?? []);
    }
    await this.platform.artifact(run.id,'environment.json',environments);
  }
  async evidence(run: Run, label: string) {
    for (const [actor, id] of Object.entries(run.actors)) {
      const captures: [string, () => Promise<unknown>][] = [
        ['state', () => this.platform.observe(id)],
        ['events', () => this.platform.command(id, 'events.read')],
        ['screen', () => this.platform.command(id, 'game.screenshot', {}, { ttlMs: 2000 })]
      ];
      for (const [kind, capture] of captures) {
        try { await this.platform.artifact(run.id, `${label}-${actor}-${kind}.json`, await capture()); }
        catch (error) { run.cleanupErrors.push(`Evidence ${actor}/${kind}: ${String(error)}`); }
      }
    }
  }
  async steps(run: Run, steps: Step[], signal: AbortSignal) {
    for (const step of steps) {
      signal.throwIfAborted();
      if (run.handoff) return;
      const entry = { id: step.id, type: step.type, started: new Date().toISOString(), state: 'running' };
      run.steps.push(entry);
      try { await this.step(run, step, signal); entry.state = 'passed'; }
      catch (error) { entry.state = 'failed'; throw error; }
    }
  }
  async step(run: Run, step: Step, signal: AbortSignal) {
    const actor = step.actor ? run.actors[step.actor] : undefined;
    if (step.predicate === 'event.occurred') { await this.eventCondition(run,step,signal); return; }
    switch (step.type) {
      case 'controlAcquire':
        await this.platform.releaseControl(actor!);
        await this.platform.acquire(actor!); break;
      case 'dialogueAdvance': {
        const value = await this.platform.observe(actor!, false, { signal, ttlMs:5000 });
        if (!value.dialogue.canAdvance && !value.dialogue.isTextAnimating) throw new E2EError('TARGET_NOT_INTERACTABLE');
        const key=value.inputBindings?.dialogueAdvance;
        if(!key||key==='None')throw new E2EError('UNSUPPORTED_CAPABILITY','dialogueAdvance');
        await this.platform.command(actor!, 'input.execute', { expectedPresentationRevision:value.dialogue.presentationRevision, sequence: [{ operation: 'tap', key }] }, { signal }); break;
      }
      case 'dialogueChoose': {
        const value = await this.platform.observe(actor!, false, { signal, ttlMs:5000 });
        const choices = value.dialogue.choices ?? [];
        const matches = choices.filter((choice: any) => (choice.choiceId === step.choiceId || choice.nextNodeId === step.choiceId));
        if (matches.length !== 1 || !value.dialogue.hasChoices || value.dialogue.isTextAnimating) throw new E2EError('TARGET_NOT_INTERACTABLE');
        const index = matches[0].index;
        let selected = value.dialogue.selectedIndex;
        // Choice panels intentionally open without a selected row.  Prime the
        // normal input-adapter selection before walking to the requested
        // choice; otherwise every valid dialogue is misclassified as absent.
        if (!Number.isInteger(selected) || selected < 0) {
          await this.platform.command(actor!, 'input.execute', { expectedPresentationRevision:value.dialogue.presentationRevision, sequence: [{ operation: 'tap', key: 'Equals' }] }, { signal });
          const primed = await this.platform.observe(actor!, false, { signal, ttlMs:5000 });
          if (primed.dialogue.graphId !== value.dialogue.graphId || primed.dialogue.nodeId !== value.dialogue.nodeId
            || primed.dialogue.presentationRevision !== value.dialogue.presentationRevision
            || !Number.isInteger(primed.dialogue.selectedIndex) || primed.dialogue.selectedIndex < 0)
            throw new E2EError('STATE_CONFLICT');
          selected = primed.dialogue.selectedIndex;
        }
        for (let n = 0; n < Math.abs(index - selected); n++)
          await this.platform.command(actor!, 'input.execute', { expectedPresentationRevision:value.dialogue.presentationRevision, sequence: [{ operation: 'tap', key: index > selected ? 'Equals' : 'Minus' }] }, { signal });
        const confirmed = await this.platform.observe(actor!, false, { signal, ttlMs:5000 });
        if (confirmed.dialogue.graphId !== value.dialogue.graphId || confirmed.dialogue.nodeId !== value.dialogue.nodeId
          || confirmed.dialogue.presentationRevision !== value.dialogue.presentationRevision
          || confirmed.dialogue.selectedIndex !== index || confirmed.dialogue.choices[index]?.choiceId !== matches[0].choiceId)
          throw new E2EError('STATE_CONFLICT');
        const key=confirmed.inputBindings?.dialogueConfirm;
        if(!key||key==='None')throw new E2EError('UNSUPPORTED_CAPABILITY','dialogueConfirm');
        await this.platform.command(actor!, 'input.execute', { expectedPresentationRevision:value.dialogue.presentationRevision, sequence: [{ operation: 'tap', key }] }, { signal }); break;
      }
      case 'interact': await this.interact(actor!,step,signal); break;
      case 'navigate': await this.navigate(actor!, step, signal); break;
      case 'uiText': await this.platform.command(actor!,'ui.text',{text:step.text,mode:step.mode},{signal}); break;
      case 'input': {
        let sequence = step.sequence;
        if (step.action) {
          const state = await this.platform.observe(actor!);
          const key = state.inputBindings?.[step.action];
          if (!key || key === 'None') throw new E2EError('UNSUPPORTED_CAPABILITY',step.action);
          sequence = [{operation:'tap',key}];
        }
        await this.platform.command(actor!, 'input.execute', { sequence }, { signal, ttlMs: Math.min(step.timeoutMs ?? 10000, 30000) }); break;
      }
      case 'uiAction': await activateVisible(this.platform, actor!, step.automationId!, signal, step.documentId, step.mode); break;
      case 'assert': if (!(await this.predicate(run, step, signal))) throw new E2EError('ASSERTION_FAILED', step.id); break;
      case 'wait': case 'assertEventually': {
        const until = performance.now() + step.timeoutMs!;
        while (!(await this.predicate(run, step, signal))) {
          if (performance.now() >= until) throw new E2EError('DEADLINE_EXCEEDED', step.id);
          await delay(100, undefined, { signal });
        }
        break;
      }
      case 'parallel': {
        const group = new AbortController(); const combined = AbortSignal.any([signal, group.signal]);
        const tasks = step.branches!.map(branch => this.steps(run, branch, combined));
        try { await Promise.all(tasks); }
        catch (error) { group.abort(); await Promise.allSettled(tasks); await Promise.allSettled(Object.values(run.actors).map(id => this.platform.release(id))); throw error; }
        break;
      }
      case 'barrier': {
        const id = step.args!.barrierId;
        let barrier = run.barriers.get(id);
        const participants = [...step.participants!].sort();
        if (!barrier) { barrier = { participants, arrived: new Set() }; run.barriers.set(id, barrier); }
        if (JSON.stringify(barrier.participants) !== JSON.stringify(participants)) throw new E2EError('BARRIER_PARTICIPANTS_MISMATCH');
        if (barrier.arrived.has(step.actor!)) throw new E2EError('DUPLICATE_BARRIER_ARRIVAL');
        barrier.arrived.add(step.actor!);
        const until = performance.now() + step.timeoutMs!;
        while (!participants.every(actor => barrier!.arrived.has(actor))) {
          if (performance.now() >= until) throw new E2EError('DEADLINE_EXCEEDED', step.id);
          await delay(20, undefined, { signal });
        }
        break;
      }
      case 'handoff':
        // Stop issuing new runner steps before changing the epoch.
        await Promise.all(Object.values(run.actors).map(id => this.platform.release(id)));
        await this.platform.handoff(actor!, 'RemoteHuman'); run.handoff = true; break;
      case 'checkpoint': await this.evidence(run, step.id); break;
      default: throw new E2EError('UNSUPPORTED_CAPABILITY');
    }
  }
  async eventCondition(run:Run, step:Step, signal:AbortSignal) {
    const actor = step.args?.actor ?? step.actor, id = run.actors[actor];
    const never = step.type === 'assertNever';
    let cursor = never ? (await this.platform.observe(id)).eventCursor : run.eventCursors?.[actor];
    if (!Number.isSafeInteger(cursor)) throw new E2EError('UNSUPPORTED_CAPABILITY','Event cursor unavailable');
    const until = performance.now() + (step.timeoutMs ?? 0);
    while (true) {
      signal.throwIfAborted();
      const batch = await this.platform.command(id,'events.read',{cursor},{signal});
      for (const event of batch.events) {
        if (event.eventType !== step.args!.eventType || event.side !== step.args!.side) continue;
        if (!Object.entries(step.args!.match ?? {}).every(([key,value]) => event.payload?.[key] === value)) continue;
        if (!Object.entries(step.args!.contains ?? {}).every(([key,value]) => typeof value === 'string' && typeof event.payload?.[key] === 'string' && event.payload[key].includes(value))) continue;
        if (never) throw new E2EError('ASSERTION_FAILED',step.id);
        return;
      }
      cursor = batch.cursor;
      if (performance.now() >= until) {
        if (never) return;
        throw new E2EError(step.type === 'assert' ? 'ASSERTION_FAILED' : 'DEADLINE_EXCEEDED',step.id);
      }
      await delay(Math.min(100,Math.max(1,until-performance.now())),undefined,{signal});
    }
  }
  async interact(id:string,step:Step,signal:AbortSignal) {
    let current=await this.platform.observe(id);
    const targetEntity=step.args?.entityId;
    // Drive one selection at a time and re-observe.  Multiple input commands
    // can otherwise be consumed in one Unity frame, leaving the hint on its
    // original entry despite a nominally correct sequence of key taps.
    for(let attempt=0;attempt<32;attempt++) {
      const matches=(current.interactions??[]).filter((interaction:any)=>interaction.interactionId===step.target&&(!targetEntity||interaction.entityId===targetEntity));
      const selected=(current.interactions??[]).filter((interaction:any)=>interaction.selected);
      if(matches.length!==1||selected.length!==1)throw new E2EError('TARGET_NOT_INTERACTABLE');
      if(selected[0].interactionId===step.target&&selected[0].entityId===matches[0].entityId)break;
      const difference=matches[0].index-selected[0].index;
      if(difference===0)throw new E2EError('STATE_CONFLICT');
      await this.platform.command(id,'input.execute',{sequence:[{operation:'tap',key:difference>0?'Equals':'Minus'}]},{signal});
      await delay(120,undefined,{signal});
      current=await this.platform.observe(id);
      if(attempt===31)throw new E2EError('STATE_CONFLICT');
    }
    if(!current.interactions?.some((i:any)=>i.selected&&i.interactionId===step.target&&(!targetEntity||i.entityId===targetEntity)))throw new E2EError('STATE_CONFLICT');
    const key=current.inputBindings?.interact;
    if(!key||key==='None')throw new E2EError('UNSUPPORTED_CAPABILITY');
    await this.platform.command(id,'input.execute',{sequence:[{operation:'tap',key}]},{signal});
  }
  async navigate(id: string, step: Step, signal: AbortSignal) {
    const end = performance.now() + step.timeoutMs!;
    const radius = step.args?.arrivalRadius ?? .5;
    const stuckWindow = step.args?.stuckWindowMs ?? 3000;
    if (!(radius > 0 && radius <= 10) || !(stuckWindow >= 500 && stuckWindow <= 30000)) throw new E2EError('INVALID_ARGUMENT');
    let best = Infinity, bestTurnError = Infinity, lastProgress = performance.now();
    try {
      while (performance.now() < end) {
        signal.throwIfAborted();
        // Navigation is a polling loop, so an unavailable bridge response must be
        // bounded by the step's abort signal just like its input command.  Without
        // this, a single stale observe request can outlive the navigation deadline.
        const value = await this.platform.observe(id,false,{includeStaticItems:step.args?.targetType==='staticItem',signal,ttlMs:5000});
        const player = value.client.players.find((p: any) => p.local);
        const targets = step.args?.targetType === 'vehicle'
          ? (value.vehicles ?? []).filter((vehicle:any)=>vehicle.id===step.target)
          : step.args?.targetType === 'scenarioEntity'
          ? (value.scenarioEntities ?? []).filter((entity:any)=>entity.id===step.target)
          : step.args?.targetType === 'staticItem'
          ? (value.staticPlacedItems ?? []).filter((item:any)=>item.id===step.target)
          : step.args?.targetType === 'item'
          ? (value.worldItems ?? []).filter((item: any) => item.itemId === step.target)
          : (step.args?.targetType === 'npc' ? value.entities ?? [] : value.waypoints ?? []).filter((w: any) => w.id === step.target);
        if (targets.length !== 1 || !player) throw new E2EError('TARGET_NOT_FOUND');
        if (!player.canMove || player.movementSuppressed || player.scriptedMovement || player.walkingSpeed <= 0) throw new E2EError('MOVEMENT_BLOCKED');
        const offset=step.args?.targetOffset??[0,0,0];
        const [x, y, z] = targets[0].position.map((coordinate:number,index:number)=>coordinate+offset[index]);
        const dx = x - player.position[0], dz = z - player.position[2];
        const distance = Math.hypot(dx, dz);
        if (distance <= radius) {
          if (Math.abs(y - player.position[1]) > 2) throw new E2EError('NAVIGATION_HEIGHT_MISMATCH');
          return;
        }
        const targetYaw = Math.atan2(dx, dz) * 180 / Math.PI;
        const angle = ((targetYaw - player.yaw + 540) % 360) - 180;
        if (distance < best - .05) { best = distance; bestTurnError = Infinity; lastProgress = performance.now(); }
        if (Math.abs(angle) < bestTurnError - 1) lastProgress = performance.now();
        bestTurnError = Math.abs(angle);
        if (performance.now() - lastProgress > stuckWindow) throw new E2EError('NAVIGATION_STUCK');
        if (!(player.rotationSensitivity > 0)) throw new E2EError('UNSUPPORTED_CAPABILITY');
        const sequence: any[] = [{ operation: 'lookDelta', x: Math.max(-30, Math.min(30, angle)) / player.rotationSensitivity, y: 0 }];
        if (Math.abs(angle) < 25) {
          const travelMs = (distance - radius) / player.walkingSpeed * 1000;
          const durationMs = Math.max(1, Math.min(Math.abs(angle) < 10 ? 250 : 100, Math.floor(travelMs)));
          sequence.push({ operation: 'hold', key: 'W', durationMs });
        }
        // A four-player low-frame-rate run can keep a valid five-second control
        // lease while Unity needs longer than three seconds to service one input.
        // Do not let this per-command timeout preempt the active control lease.
        await this.platform.command(id, 'input.execute', { sequence }, { signal, ttlMs: 5000 });
      }
      throw new E2EError('DEADLINE_EXCEEDED');
    } finally { await this.platform.release(id).catch(() => {}); }
  }
  async predicate(run: Run, step: Step, signal?: AbortSignal) {
    const args = step.args ?? {};
    const name = args.actor ?? step.actor;
    if (step.predicate === 'participants.ready') {
      const names = args.players ?? Object.keys(run.actors);
      for (const actor of names) {
        const value = await this.platform.observe(run.actors[actor]);
        if (!value.client?.localPlayerReady) return false;
      }
      return true;
    }
    const id = run.actors[name]; if (!id) throw new E2EError('MISSING_ACTOR');
    if (step.predicate?.startsWith('ui.')) {
      const value = await this.platform.command(id, 'ui.query', { documentId: args.documentId, automationId: args.automationId }, { signal });
      if (step.predicate === 'ui.visible') return value.elements.length === 1 && value.elements[0].visible === (args.visible ?? true);
      if (step.predicate === 'ui.valueEquals') return value.elements.length === 1 && value.elements[0].value === args.value;
      return value.elements.length === 1 && value.elements[0][step.predicate === 'ui.visible' ? 'visible' : 'interactable'] === true;
    }
    const value = await this.platform.observe(id);
    switch (step.predicate) {
      case 'interaction.available': return (value.interactions??[]).filter((i:any)=>i.interactionId===args.interactionId&&(!args.entityId||i.entityId===args.entityId)).length===1;
      case 'input.context': return value.inputContext === args.context;
      case 'scene.is': return value.scene === args.scene;
      case 'dialogue.node': return value.dialogue.nodeId===args.nodeId && (!args.graphId||value.dialogue.graphId===args.graphId);
      case 'dialogue.canAdvance': return value.dialogue.canAdvance === true;
      case 'dialogue.hasChoices': return value.dialogue.hasChoices === true;
      case 'scenario.mode': return value.scenario.executionMode === args.mode;
      case 'scenario.node': return value.scenario.nodeId === args.nodeId && value.scenario.side === args.side && (!args.graphId || value.scenario.graphId === args.graphId);
      case 'scenario.stateValue': return value.scenario.side === args.side && value.scenario.stateValues?.[args.key] === args.value;
      case 'scenario.branch': return value.scenario.side === args.side && (value.scenario.branchesByClientId?.[args.clientId] ?? []).includes(args.branchId);
      case 'quest.roleFlag': return value.scenario.side === args.side && (value.scenario.questFlagsByRole?.[args.role] ?? []).includes(args.flag);
      case 'scenario.roleAllocation': {
        const server=value.scenarioNetwork?.server;
        const owners=server?.allocations?.[`${args.nodeId}|${args.visit}`];
        return server?.graphId===args.graphId && Array.isArray(owners) && owners.length===args.ownerIds.length
          && owners.every((owner:number,index:number)=>owner===args.ownerIds[index]);
      }
      case 'inventory.itemCount': {
        const inventory = value.client.players.find((p:any)=>p.ownerId===args.ownerId)?.inventory;
        return inventory?.available === true && Array.isArray(inventory.slots)
          && inventory.slots.filter((s:any)=>s.itemId===args.itemId).reduce((count:number,s:any)=>count+s.count,0)===args.count;
      }
      case 'checklist.itemCompleted': {
        const inventory = value.client.players.find((p:any)=>p.ownerId===args.ownerId)?.inventory;
        if (inventory?.available !== true || !Array.isArray(inventory.slots)) return false;
        const papers = inventory.slots.filter((s:any)=>s.itemId==='checklist_paper'&&(args.slot===undefined||s.slot===args.slot));
        return papers.length===1 && !papers[0].observationError && Array.isArray(papers[0].checklist?.CompletedIdentifiers)
          && papers[0].checklist.CompletedIdentifiers.includes(args.itemId)===args.present;
      }
      case 'quest.playerFlag': {
        const player = value.client.players.find((p:any)=>p.ownerId===args.ownerId);
        return !!player && Array.isArray(player.questFlags) && player.questFlags.includes(args.flag) === args.present;
      }
      case 'player.tag': {
        const player = value.client.players.find((p:any)=>p.ownerId===args.ownerId);
        return !!player && Array.isArray(player.tags) && player.tags.includes(args.tag) === args.present;
      }
      case 'players.count': return value.client.players.length === args.count;
      case 'player.near': {
        const player = value.client.players.find((p: any) => p.local);
        return !!player && Math.hypot(...player.position.map((v: number, i: number) => v - args.position[i])) <= args.radius;
      }
      default: throw new E2EError('UNKNOWN_PREDICATE');
    }
  }
}
