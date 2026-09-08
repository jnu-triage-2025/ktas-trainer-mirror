import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtemp} from 'node:fs/promises';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {Platform} from '../src/core.ts';
test('optional HTTP diagnostics preserve command results and omit malformed timings',async(t)=>{
 const p=new Platform({builds:{},artifactRoot:await mkdtemp(join(tmpdir(),'e2e-http-timing-'))});
 p.instances.set('one',{id:'one',runId:'run',nodeId:'local',role:'client',endpoint:'http://127.0.0.1:1',token:'test',profile:'',state:'READY',epoch:1,owner:'None'});
 let headers:Record<string,string>={};
 t.mock.method(globalThis,'fetch',async()=>new Response(JSON.stringify({ok:true,result:{value:42},controlEpoch:1,frame:1}),{headers}));
 for(const [input,expected] of [
  [{},{}],
  [{'X-E2E-Serialize-Ms':'1.25','X-E2E-Serve-Ms':'50','X-E2E-Resume-Ms':'0'},{serializeMs:1.25,serveMs:50,resumeMs:0}],
  [{'X-E2E-Serialize-Ms':'NaN','X-E2E-Serve-Ms':'-1','X-E2E-Resume-Ms':''},{}]
 ] as Array<[Record<string,string>,Record<string,number>]>) {
  headers=input;assert.deepEqual(await p.command('one','game.test'),{value:42});
  assert.deepEqual(p.audit.at(-1)?.bridgeHttpTimings,expected);
 }
 await p.close();
});
