import test from 'node:test';
import assert from 'node:assert/strict';
import {serviceUrl} from '../src/service-url.ts';
test('MCP service URL accepts TLS origins and rejects remote plaintext and ambiguous paths',()=>{
 assert.equal(serviceUrl('https://example.com:17890/'),'https://example.com:17890');
 assert.equal(serviceUrl('http://127.0.0.1:17890'),'http://127.0.0.1:17890');
 for(const url of ['http://example.com','https://user:password@example.com','https://example.com/api','https://example.com/?token=x','https://example.com/#token','file:///tmp/server'])assert.throws(()=>serviceUrl(url));
});
