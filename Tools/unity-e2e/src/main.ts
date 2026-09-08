import { createServer, type RequestListener } from 'node:http';
import { createServer as createSecureServer } from 'node:https';
import { randomBytes } from 'node:crypto';
import { authorize, credentials } from './access.ts';
import { readFile, mkdir, writeFile, chmod } from 'node:fs/promises';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { API } from './api.ts';
import { Platform, loadConfig, E2EError } from './core.ts';

const config = await loadConfig(process.argv[2] ?? 'config.json');
const platform = new Platform(config), api = new API(platform, join(config.artifactRoot, 'assistance-requests.json'));
const token = process.env.E2E_CONSOLE_TOKEN ?? randomBytes(32).toString('hex');
const expiry = process.env.E2E_CREDENTIAL_EXPIRES_AT;
const authenticate = credentials(token,process.env.E2E_OBSERVER_TOKEN,expiry===undefined?undefined:Number(expiry));
const port = config.port ?? 17890;
const origin = config.gateway?.publicOrigin ?? `http://127.0.0.1:${port}`;
const expectedHost = new URL(origin).host;
await mkdir(config.artifactRoot,{recursive:true});
const accessFile=join(config.artifactRoot,'console-access.txt');
const handler:RequestListener = async (request, response) => {
  response.setHeader('Cache-Control','no-store');
  response.setHeader('X-Content-Type-Options','nosniff');
  response.setHeader('Content-Security-Policy', "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; script-src 'self'; connect-src 'self'; frame-ancestors 'none'");
  try {
    if (request.headers.host !== expectedHost || (request.headers.origin && request.headers.origin !== origin)) {
      response.writeHead(403).end(); return;
    }
    if (request.method === 'GET' && ['/', '/console.js'].includes(request.url ?? '')) {
      const path = request.url === '/' ? './console.html' : './console.js';
      response.setHeader('Content-Type', request.url === '/' ? 'text/html; charset=utf-8' : 'text/javascript');
      response.end(await readFile(fileURLToPath(new URL(path, import.meta.url)))); return;
    }
    const access=authenticate(request.headers.authorization);
    const grant = api.credentials.authenticate(request.headers.authorization);
    if (!access && !grant) { response.writeHead(401).end(); return; }
    if (request.method !== 'POST' || request.url !== '/api') { response.writeHead(404).end(); return; }
    let body = ''; for await (const chunk of request) { body += chunk; if (Buffer.byteLength(body) > 1_048_576) throw new E2EError('PAYLOAD_TOO_LARGE'); }
    // A request body may arrive after the credential that opened the connection expired.
    if (!authenticate(request.headers.authorization) && !api.credentials.authenticate(request.headers.authorization)) { response.writeHead(401).end(); return; }
    const message = JSON.parse(body);
    if(access && !authorize(access,message.tool)) {
      response.writeHead(403,{'Content-Type':'application/json'}).end(JSON.stringify({ok:false,error:{code:'PERMISSION_DENIED'}}));return;
    }
    // MCP requests authenticate separately with the same service token but never choose a human owner.
    const caller = message.caller === 'Automation' ? 'Automation' : 'RemoteHuman';
    const value = grant && !access
      ? await api.callScoped(grant, message.tool, message.args ?? {}, caller)
      : await api.call(message.tool, message.args ?? {}, caller);
    response.setHeader('Content-Type', 'application/json'); response.end(JSON.stringify({ ok: true, result: value }));
  } catch (error) {
    response.setHeader('Content-Type','application/json'); response.statusCode = error instanceof E2EError && error.code === 'PERMISSION_DENIED' ? 403 : 400;
    response.end(JSON.stringify({ ok: false, error: { code: error instanceof E2EError ? error.code : 'INVALID_REQUEST', message: String(error) } }));
  }
};
const server = config.gateway
  ? createSecureServer({cert:await readFile(config.gateway.certFile),key:await readFile(config.gateway.keyFile),minVersion:'TLSv1.2'},handler)
  : createServer(handler);
server.requestTimeout = 35000;
await new Promise<void>((resolve,reject)=>{
  server.once('error',reject);
  server.listen(port,config.gateway?.bind??'127.0.0.1',()=>{server.off('error',reject);resolve();});
});
try {
  await writeFile(accessFile,`${origin}/#${token}\n`,{mode:0o600});
  await chmod(accessFile,0o600);
} catch(error) {server.close();throw error;}
process.stderr.write(`Unity E2E console: ${origin}/ · authenticated link file: ${accessFile}\n`);
for (const signal of ['SIGINT','SIGTERM'] as const) process.once(signal, async () => {
  for (const job of api.operations.values()) job.controller.abort();
  await Promise.allSettled([...api.operations.values()].map(job => job.done));
  for (const run of api.runner.runs.values()) run.controller.abort();
  await Promise.allSettled([...api.runner.runs.values()].map(r => r.done));
  await platform.close(); server.close();
});
