import { McpServer } from '@modelcontextprotocol/sdk/server/mcp.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import { toolSchemas, toolDescriptions } from './tool-schema.ts';
import { toolNames } from './api.ts';
import { serviceUrl } from './service-url.ts';

const endpoint = serviceUrl(process.env.E2E_SERVICE_URL ?? 'http://127.0.0.1:17890');
const token = process.env.E2E_CONSOLE_TOKEN;
if (!token || token.length < 32) throw new Error('E2E_CONSOLE_TOKEN is required');
const server = new McpServer({ name: 'unity-multiplayer-e2e', version: '0.1.0' });
for (const name of toolNames) {
  server.tool(name, toolDescriptions[name] ?? `Unity E2E ${name}`,
    toolSchemas[name].shape,
    async args => {
      try {
        const response = await fetch(endpoint + '/api', { method: 'POST', redirect:'error', headers: { Authorization: `Bearer ${token}`, 'Content-Type':'application/json' },
          body: JSON.stringify({ tool: name, args, caller: 'Automation' }), signal: AbortSignal.timeout((args.timeoutMs ?? args.ttlMs ?? 10000) + 1000) });
        if(response.status===401||response.status===403) return {content:[{type:'text' as const,text:JSON.stringify({code:response.status===401?'UNAUTHORIZED':response.status===403?'PERMISSION_DENIED':'SERVICE_HTTP_ERROR',status:response.status})}],isError:true};
        const body = await response.json();
        if (name === 'game.screenshot' && body.ok) return { content: [{ type: 'image' as const, data: body.result.data, mimeType: body.result.mimeType }] };
        return { content: [{ type: 'text' as const, text: JSON.stringify(body.ok ? body.result : body.error) }], isError: !body.ok };
      } catch (error) { return { content: [{ type: 'text' as const, text: String(error) }], isError: true }; }
    });
}
await server.connect(new StdioServerTransport());
