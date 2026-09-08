import test from 'node:test';
import assert from 'node:assert/strict';
import { activateVisible } from '../src/ui-navigation.ts';
import type { Platform } from '../src/core.ts';
test('UI navigation scrolls through device input and rechecks before activation', async () => {
 const calls: any[] = []; let scrolled = false;
 const platform = { command: async (_id: string, type: string, payload: any) => {
  calls.push({ type, payload });
  if (type === 'ui.query') return { elements: payload.automationId === 'menu'
   ? [{ documentId: 'doc', interactable: true, screenCenter: { x: 100, y: 100 }, screenWidth: 200, screenHeight: 200, uiRevision: 10 }]
   : [{ documentId: 'doc', visible: true, enabled: true, interactable: scrolled, scrollContainerId: 'menu', screenCenter: { x: 100, y: -10 } }] };
  if (type === 'input.execute') scrolled = true;
  return {};
 }} as unknown as Platform;
 await activateVisible(platform, 'p1', 'settings', new AbortController().signal, 'doc');
 assert.deepEqual(calls.map(c => c.type), ['ui.query', 'ui.query', 'ui.pointer', 'input.execute', 'ui.query', 'ui.activate']);
 assert.deepEqual(calls[3].payload.sequence, [{ operation: 'scroll', y: -1 }]);
 assert.equal(calls.at(-1).payload.documentId, 'doc');
});
test('cancelled UI navigation does not issue commands', async () => {
 const platform = { command: async () => { throw new Error('Unexpected command'); } } as unknown as Platform;
 await assert.rejects(activateVisible(platform, 'p1', 'settings', AbortSignal.abort()), { name: 'AbortError' });
});

test('scroll overshoot reduces the next wheel amount to reach a narrow viewport',async()=>{
 let y=-100;const amounts:number[]=[];
 const p={command:async(_id:string,type:string,payload:any)=>{
  if(type==='ui.query')return {elements:[payload.automationId==='menu'?{documentId:'doc',interactable:true,screenCenter:{x:100,y:200}}:{documentId:'doc',visible:true,enabled:true,interactable:y>120&&y<280,scrollContainerId:'menu',screenCenter:{x:100,y}}]};
  if(type==='input.execute'){const amount=payload.sequence[0].y;amounts.push(amount);y-=450*amount;}
  if(type==='ui.activate')assert.ok(y>120&&y<280);
  return {};
 }} as unknown as Platform;
 await activateVisible(p,'p1','button',AbortSignal.timeout(2000));
 assert.deepEqual(amounts,[-1,.5]);
});

test('large device wheel units converge without clamping the adjustment too early',async()=>{
 let y=-100;const amounts:number[]=[];
 const p={command:async(_id:string,type:string,payload:any)=>{
  if(type==='ui.query')return {elements:[payload.automationId==='menu'?{documentId:'doc',interactable:true,screenCenter:{x:100,y:200}}:{documentId:'doc',visible:true,enabled:true,interactable:y>120&&y<280,scrollContainerId:'menu',screenCenter:{x:100,y}}]};
  if(type==='input.execute'){const amount=payload.sequence[0].y;amounts.push(amount);y=Math.max(-100,Math.min(350,y-20000*amount));}
  if(type==='ui.activate')assert.ok(y>120&&y<280);
  return {};
 }} as unknown as Platform;
 await activateVisible(p,'p1','button',AbortSignal.timeout(5000));
 assert.ok(amounts.some(amount=>Math.abs(amount)<1/16));
});
