import { E2EError } from './errors.ts';
import { randomUUID } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync, renameSync, rmSync } from 'node:fs';
import { dirname } from 'node:path';
import { z } from 'zod';
export type AssistanceRequest = {
 id: string; runId: string; instanceId: string; prompt: string;
 state: 'queued' | 'running' | 'completed' | 'failed' | 'cancelled';
 revision: number; createdAt: string; updatedAt: string; result?: string;
};
const snapshotSchema = z.object({version:z.literal(1),requests:z.array(z.object({
 id:z.string().uuid(),runId:z.string().min(1),instanceId:z.string().min(1),prompt:z.string().min(1).max(8000),
 state:z.enum(['queued','running','completed','failed','cancelled']),revision:z.number().int().nonnegative(),
 createdAt:z.string().datetime(),updatedAt:z.string().datetime(),result:z.string().max(16000).optional()
}).strict()).max(1000)}).strict();
export class AssistanceRequests {
 private requests = new Map<string, AssistanceRequest>();
 private snapshotPath?: string;
 constructor(snapshotPath?: string) {
  this.snapshotPath = snapshotPath;
  if (!snapshotPath) return;
  let raw: string;
  try { raw = readFileSync(snapshotPath, 'utf8'); }
  catch (error) { if ((error as NodeJS.ErrnoException).code === 'ENOENT') return; throw error; }
  const saved = snapshotSchema.parse(JSON.parse(raw));
  const restored = new Map<string, AssistanceRequest>();
  for (const request of saved.requests) {
   if (restored.has(request.id)) throw new E2EError('DUPLICATE_ASSISTANCE_REQUEST');
   restored.set(request.id, ['queued','running'].includes(request.state) ? {...request, state:'failed',
    revision:request.revision+1, updatedAt:new Date().toISOString(),
    result:'서비스가 재시작되어 요청 처리가 중단되었습니다. 이전 실행 결과를 확인한 뒤 새 요청을 등록하세요.'} : request);
  }
  this.persist(restored);
  this.requests = restored;
 }
 private persist(next: Map<string, AssistanceRequest>) {
  if (!this.snapshotPath) return;
  mkdirSync(dirname(this.snapshotPath), {recursive:true});
  const temporary = `${this.snapshotPath}.${randomUUID()}.tmp`;
  try {
   writeFileSync(temporary, JSON.stringify({version:1,requests:[...next.values()]}), {mode:0o600,flag:'wx',flush:true});
   renameSync(temporary, this.snapshotPath);
  } finally { rmSync(temporary, {force:true}); }
 }
 private commit(request: AssistanceRequest) {
  const next = new Map(this.requests); next.set(request.id, request);
  this.persist(next); this.requests = next;
  return structuredClone(request);
 }
 submit(runId: string, instanceId: string, prompt: string) {
  if (!runId || !instanceId || typeof prompt !== 'string' || !prompt.trim() || prompt.length > 8000) throw new E2EError('INVALID_ASSISTANCE_REQUEST');
  if (this.requests.size >= 1000) throw new E2EError('ASSISTANCE_CAPACITY_EXCEEDED');
  const now = new Date().toISOString();
  const request: AssistanceRequest = { id: randomUUID(), runId, instanceId, prompt: prompt.trim(),
   state: 'queued', revision: 0, createdAt: now, updatedAt: now };
  return this.commit(request);
 }
 list(runId?: string) { return [...this.requests.values()].filter(value => runId === undefined || value.runId === runId).map(value => structuredClone(value)); }
 get(id: string) { const request = this.requests.get(id); return request ? structuredClone(request) : undefined; }
 transition(id: string, revision: number, state: AssistanceRequest['state'], result?: string) {
  const request = this.requests.get(id);
  if (!request) throw new E2EError('UNKNOWN_ASSISTANCE_REQUEST');
  if (request.revision !== revision) throw new E2EError('ASSISTANCE_STATE_CONFLICT');
  const permitted = request.state === 'queued' ? ['running', 'cancelled'] : request.state === 'running' ? ['completed', 'failed', 'cancelled'] : [];
  if (!permitted.includes(state)) throw new E2EError('INVALID_ASSISTANCE_TRANSITION');
  if ((state === 'completed' || state === 'failed') && (!result?.trim() || result.length > 16000)) throw new E2EError('ASSISTANCE_RESULT_REQUIRED');
  return this.commit({...request, state, result, revision:request.revision+1, updatedAt:new Date().toISOString()});
 }
}
