import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {Worker} from 'node:worker_threads';
import {AsyncEventStore} from '../src/async-store.ts';
import {EventStore} from '../src/store.ts';

test('worker persistence drains on close and ordered reads preserve deduplication',async()=>{
 const root=mkdtempSync(join(tmpdir(),'e2e-async-history-')),store=new AsyncEventStore(root);
 store.appendMany('run','p','game',[{eventSequence:1},{eventSequence:1},{eventSequence:2}]);
 assert.deepEqual((await store.read('p',0,'run')).map(r=>r.body.eventSequence),[1,2]);
 store.append('run','p','command',{final:true});await store.close();
 const saved=new EventStore(root);try{assert.equal(saved.read('p',0,'run').length,3);}finally{saved.close();}
});
test('a locked database waits in the worker while the main event loop continues',async()=>{
 const root=mkdtempSync(join(tmpdir(),'e2e-async-lock-')),store=new AsyncEventStore(root);
 await store.cursor();
 const lock=new Worker(`const {parentPort,workerData}=require('node:worker_threads');const {DatabaseSync}=require('node:sqlite');const db=new DatabaseSync(workerData);db.exec('BEGIN IMMEDIATE');parentPort.postMessage('locked');setTimeout(()=>{db.exec('COMMIT');db.close();},180);`,{eval:true,workerData:join(root,'history.sqlite')});
 await new Promise<void>((resolve,reject)=>{lock.once('message',()=>resolve());lock.once('error',reject);});
 let ticks=0;const timer=setInterval(()=>ticks++,10);
 try{await store.append('run','p','command',{retained:true});assert.ok(ticks>=3,`Main loop only ticked ${ticks} times`);}
 finally{clearInterval(timer);await lock.terminate();await store.close();}
});
test('worker failure and bounded queue overflow cannot silently lose evidence',async()=>{
 const root=mkdtempSync(join(tmpdir(),'e2e-async-fail-')),store=new AsyncEventStore(root,{requests:1,bytes:1024,timeoutMs:30000});
 const first=store.cursor();
 assert.throws(()=>store.cursor(),/HISTORY_QUEUE_FULL/);
 await assert.rejects(first,/HISTORY_QUEUE_FULL/);await assert.rejects(store.close(),/HISTORY_QUEUE_FULL/);
 const crashed=new AsyncEventStore(root);await crashed.cursor();
 const gone=new Promise(resolve=>(crashed as any).worker.once('exit',resolve));
 await (crashed as any).worker.terminate();await gone;
 assert.throws(()=>crashed.cursor(),/HISTORY_WORKER_EXITED/);await assert.rejects(crashed.close(),/HISTORY_WORKER_EXITED/);
});
