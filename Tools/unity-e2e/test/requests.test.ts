import test from 'node:test';
import assert from 'node:assert/strict';
import { AssistanceRequests } from '../src/requests.ts';
test('assistance requests preserve prompts and use revisions to prevent conflicting execution', () => {
 const requests = new AssistanceRequests();
 const request = requests.submit('run', 'p1', '설정창을 열고 제어권을 넘겨줘');
 assert.equal(request.state, 'queued');
 const running = requests.transition(request.id, 0, 'running');
 assert.throws(() => requests.transition(request.id, 0, 'running'), /STATE_CONFLICT/);
 assert.throws(() => requests.transition(request.id, running.revision, 'completed'), /RESULT_REQUIRED/);
 const completed = requests.transition(request.id, running.revision, 'completed', '설정창 표시와 인계를 확인했습니다.');
 assert.equal(completed.prompt, request.prompt);
 assert.throws(() => requests.transition(request.id, completed.revision, 'running'), /INVALID_ASSISTANCE_TRANSITION/);
});
test('request listings are isolated copies and cancellation cannot be overwritten', () => {
 const requests = new AssistanceRequests();
 const request = requests.submit('run', 'p1', '설정');
 assert.deepEqual(requests.list('other'), []);
 request.state = 'completed';
 assert.equal(requests.get(request.id)!.state, 'queued');
 const cancelled = requests.transition(request.id, 0, 'cancelled');
 assert.throws(() => requests.transition(request.id, cancelled.revision, 'completed', 'done'), /INVALID_ASSISTANCE_TRANSITION/);
 assert.throws(() => requests.submit('run', 'p1', ' '), /INVALID_ASSISTANCE_REQUEST/);
});

test('restart retains terminal evidence and interrupts unfinished requests without replay', async () => {
 const {mkdtemp,readFile,rm,stat} = await import('node:fs/promises');
 const {tmpdir} = await import('node:os');
 const {join} = await import('node:path');
 const root=await mkdtemp(join(tmpdir(),'e2e-requests-')), path=join(root,'requests.json');
 try {
  const first=new AssistanceRequests(path);
  const queued=first.submit('run','p1','pending');
  const running=first.submit('run','p2','working'); first.transition(running.id,0,'running');
  const completed=first.submit('other','p3','done'); first.transition(completed.id,0,'running'); first.transition(completed.id,1,'completed','Verified evidence');
  const second=new AssistanceRequests(path);
  assert.equal(second.get(queued.id)!.state,'failed'); assert.equal(second.get(running.id)!.revision,2);
  assert.match(second.get(running.id)!.result!,/재시작/);
  assert.deepEqual(second.get(completed.id),first.get(completed.id));
  assert.throws(()=>second.transition(running.id,1,'completed','stale worker'),/STATE_CONFLICT/);
  const third=new AssistanceRequests(path); assert.deepEqual(third.list('run'),second.list('run'));
  assert.equal((await stat(path)).mode & 0o777,0o600);
  assert.equal(JSON.parse(await readFile(path,'utf8')).requests.length,3);
 } finally {await rm(root,{recursive:true,force:true});}
});
test('failed durable writes do not publish state and corrupt snapshots are not discarded', async () => {
 const {mkdtemp,readFile,writeFile,rename,mkdir,rm} = await import('node:fs/promises');
 const {tmpdir} = await import('node:os'); const {join} = await import('node:path');
 const root=await mkdtemp(join(tmpdir(),'e2e-requests-')), path=join(root,'requests.json');
 try {
  const requests=new AssistanceRequests(path), request=requests.submit('run','p1','pending');
  await rename(path,path+'.saved'); await mkdir(path);
  assert.throws(()=>requests.transition(request.id,0,'running'));
  assert.deepEqual(requests.get(request.id),request);
  await rm(path,{recursive:true}); await writeFile(path,'corrupted');
  assert.throws(()=>new AssistanceRequests(path));
  assert.equal(await readFile(path,'utf8'),'corrupted');
 } finally {await rm(root,{recursive:true,force:true});}
});
