import test from 'node:test';
import assert from 'node:assert/strict';
import {parseProcessResources,readProcessResources} from '../src/process-resources.ts';
test('process samples preserve multi-core CPU values and convert RSS to bytes',()=>{
 assert.deepEqual(parseProcessResources(' 123 245.3 1024\n 456 0.0 32\n'),[
  {pid:123,cpuPercent:245.3,rssBytes:1048576},{pid:456,cpuPercent:0,rssBytes:32768}]);
 assert.throws(()=>parseProcessResources('123 unknown 42'));
 assert.throws(()=>parseProcessResources('123 10 -1'));
});
test('empty process selection does not query unrelated processes',async()=>{
 assert.deepEqual(await readProcessResources([]),{available:true,processes:[]});
});
