import { createHash, randomBytes, randomUUID, timingSafeEqual } from 'node:crypto';
export const capabilities = ['observe', 'control', 'fixture', 'protocol'] as const;
export type Capability = typeof capabilities[number];
export type RunGrant = { id: string; runId: string; expiresAt: number; capabilities: Capability[] };
type StoredGrant = RunGrant & { digest: Buffer };
export class RunCredentials {
 private grants = new Map<string, StoredGrant>();
 private now: () => number;
 constructor(now: () => number = Date.now) { this.now = now; }
 issue(runId: string, requested: Capability[], ttlMs: number) {
  if (!runId || runId.length > 128 || !Number.isSafeInteger(ttlMs) || ttlMs < 1 || ttlMs > 900000
   || !requested.length || new Set(requested).size !== requested.length
   || requested.some(value => !capabilities.includes(value))) throw new Error('INVALID_CREDENTIAL_GRANT');
  this.prune();
  if (this.grants.size >= 1024) throw new Error('CREDENTIAL_CAPACITY_EXCEEDED');
  const token = randomBytes(32).toString('hex');
  const grant: StoredGrant = { id: randomUUID(), runId, expiresAt: this.now() + ttlMs,
   capabilities: [...requested], digest: createHash('sha256').update(token).digest() };
  this.grants.set(grant.id, grant);
  return { token, ...this.publicGrant(grant) };
 }
 authenticate(header: string | undefined): RunGrant | undefined {
  this.prune();
  if (!header?.startsWith('Bearer ') || header.length !== 71) return undefined;
  const supplied = createHash('sha256').update(header.slice(7)).digest();
  for (const grant of this.grants.values())
   if (timingSafeEqual(supplied, grant.digest)) return this.publicGrant(grant);
  return undefined;
 }
 revoke(id: string) { return this.grants.delete(id); }
 revokeRun(runId: string) {
  for (const [id, grant] of this.grants) if (grant.runId === runId) this.grants.delete(id);
 }
 permits(grant: RunGrant, runId: string, capability: Capability) {
  const current = this.grants.get(grant.id);
  return !!current && current.expiresAt > this.now() && current.runId === runId && current.capabilities.includes(capability);
 }
 private prune() { for (const [id, grant] of this.grants) if (grant.expiresAt <= this.now()) this.grants.delete(id); }
 private publicGrant(grant: StoredGrant): RunGrant {
  return { id: grant.id, runId: grant.runId, expiresAt: grant.expiresAt, capabilities: [...grant.capabilities] };
 }
}
