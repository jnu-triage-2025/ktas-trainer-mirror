import { E2EError } from './errors.ts';
import { DatabaseSync } from 'node:sqlite';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

export class EventStore {
  private database: DatabaseSync;
  private maxReadEntries: number;
  constructor(root: string, maxReadEntries = 100_000) {
    this.maxReadEntries = maxReadEntries;
    if (!Number.isInteger(maxReadEntries) || maxReadEntries < 1 || maxReadEntries > 100_000) throw new E2EError('INVALID_ARGUMENT');
    mkdirSync(root, { recursive: true });
    this.database = new DatabaseSync(resolve(root, 'history.sqlite'));
    // Allow brief contention with another service without blocking the two-second input lease.
    this.database.exec(`PRAGMA busy_timeout=250;
      PRAGMA journal_mode=WAL;
      CREATE TABLE IF NOT EXISTS events (
        id INTEGER PRIMARY KEY AUTOINCREMENT, run_id TEXT NOT NULL,
        instance_id TEXT NOT NULL, kind TEXT NOT NULL, timestamp TEXT NOT NULL, body TEXT NOT NULL
      );
      CREATE INDEX IF NOT EXISTS events_run_id ON events(run_id, id);
      CREATE INDEX IF NOT EXISTS events_run_sequence ON events(run_id,instance_id,json_extract(body,'$.eventSequence')) WHERE kind='game';`);
  }
  append(runId: string, instanceId: string, kind: string, body: unknown) {
    if (kind === 'game') {
      const sequence = (body as {eventSequence?:number})?.eventSequence;
      if (sequence !== undefined && this.database.prepare("SELECT 1 FROM events WHERE run_id=? AND instance_id=? AND kind='game' AND json_extract(body,'$.eventSequence')=? LIMIT 1").get(runId,instanceId,sequence)) return;
    }
    this.database.prepare('INSERT INTO events(run_id,instance_id,kind,timestamp,body) VALUES(?,?,?,?,?)')
      .run(runId, instanceId, kind, new Date().toISOString(), JSON.stringify(body));
  }
  appendMany(runId: string, instanceId: string, kind: string, bodies: unknown[]) {
    if (!bodies.length) return;
    this.database.exec('BEGIN IMMEDIATE');
    try {
      for (const body of bodies) this.append(runId, instanceId, kind, body);
      this.database.exec('COMMIT');
    } catch (error) {
      this.database.exec('ROLLBACK');
      throw error;
    }
  }
  cursor(): number { return Number(this.database.prepare('SELECT COALESCE(MAX(id),0) AS id FROM events').get()!.id); }
  read(instanceId: string, after: number, runId:string) {
    const rows = this.database.prepare('SELECT id,kind,timestamp,body FROM events WHERE run_id=? AND instance_id=? AND id>? ORDER BY id LIMIT ?')
      .all(runId,instanceId, after,this.maxReadEntries + 1);
    if (rows.length > this.maxReadEntries) throw new E2EError('OBSERVATION_GAP','History exceeds the bounded export limit; full records remain in history.sqlite');
    return rows.map(row => ({ id:Number(row.id), kind:String(row.kind), timestamp:String(row.timestamp), body:JSON.parse(String(row.body)) }));
  }
  close() { this.database.close(); }
}
