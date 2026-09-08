import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { EventStore } from '../src/store.ts';
import { Worker } from 'node:worker_threads';
test('event deduplication preserves separate editor play sessions',()=>{
  const store=new EventStore(mkdtempSync(join(tmpdir(),'e2e-store-')));
  try {
    const event={eventSequence:1,eventType:'node.entered'};
    store.append('first','editor','game',event);store.append('first','editor','game',event);
    store.append('second','editor','game',event);
    assert.equal(store.read('editor',0,'first').length,1);
    assert.equal(store.read('editor',0,'second').length,1);
  }finally{store.close();}
});

test('history exports fail explicitly instead of silently truncating evidence',()=>{
  const store=new EventStore(mkdtempSync(join(tmpdir(),'e2e-store-limit-')),2);
  try {
    for(let i=1;i<=3;i++)store.append('run','one','game',{eventSequence:i});
    assert.throws(()=>store.read('one',0,'run'),/History exceeds/);
    assert.equal(store.read('one',1,'run').length,2);
  } finally {store.close();}
});

test('a brief independent writer lock does not lose an evidence record',async()=>{
  const root=mkdtempSync(join(tmpdir(),'e2e-store-writer-'));
  const store=new EventStore(root);
  const worker=new Worker(`
    const {parentPort,workerData}=require('node:worker_threads');
    const {DatabaseSync}=require('node:sqlite');
    const database=new DatabaseSync(workerData);
    database.exec('BEGIN IMMEDIATE');
    parentPort.postMessage('locked');
    setTimeout(()=>{database.exec('COMMIT');database.close();},40);
  `,{eval:true,workerData:join(root,'history.sqlite')});
  try {
    await new Promise<void>((resolve,reject)=>{worker.once('message',()=>resolve());worker.once('error',reject);});
    store.append('run','player','command',{commandId:'retained'});
    assert.equal(store.read('player',0,'run')[0].body.commandId,'retained');
  }finally{await worker.terminate();store.close();}
});

test('event batches preserve ordering and deduplication and roll back failed batches',()=>{
  const root=mkdtempSync(join(tmpdir(),'e2e-store-batch-'));
  let store=new EventStore(root);
  try {
    store.appendMany('run','player','game',[{eventSequence:1},{eventSequence:1},{eventSequence:2}]);
    const cyclic:any={eventSequence:4};cyclic.self=cyclic;
    assert.throws(()=>store.appendMany('run','player','game',[{eventSequence:3},cyclic]));
    store.close();store=new EventStore(root);
    assert.deepEqual(store.read('player',0,'run').map(row=>row.body.eventSequence),[1,2]);
    store.appendMany('run','player','game',[{eventSequence:2},{eventSequence:3}]);
    assert.deepEqual(store.read('player',0,'run').map(row=>row.body.eventSequence),[1,2,3]);
  } finally {store.close();}
});
