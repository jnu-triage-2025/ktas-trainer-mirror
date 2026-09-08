import test from 'node:test';
import assert from 'node:assert/strict';
import { joinInstances } from '../src/startup.ts';
import type { Platform } from '../src/core.ts';

test('deferred joining rejects hosts, unknown actors and duplicates before controlling a game',async()=>{
  const platform={instances:new Map([
    ['host',{id:'host',runId:'run',role:'host'}],
    ['client',{id:'client',runId:'run',role:'client'}]
  ])} as unknown as Platform;
  for(const deferred of [['p1'],['p3'],['p2','p2']])
    await assert.rejects(joinInstances(platform,'run',new AbortController().signal,deferred),/Only existing client actors/);
});

test('startup failure retains its phase and state when screen capture also fails',async()=>{
  const instance={id:'p1',runId:'run',role:'client',owner:'None'};
  const artifacts=new Map<string,any>();let released=false;
  const platform={
    instances:new Map([['p1',instance]]),
    observe:async()=>({scene:'IntroScene',frame:12}),
    acquire:async()=>{instance.owner='Automation';},
    command:async()=>{throw new Error('capture unavailable');},
    artifact:async(_run:string,name:string,data:any)=>{artifacts.set(name,data);},
    get:()=>instance,
    releaseControl:async()=>{released=true;}
  } as unknown as Platform;
  await assert.rejects(joinInstances(platform,'run',new AbortController().signal),/MISSING_SERVER/);
  const failure=artifacts.get('startup-failure.json');
  assert.equal(failure.phase,'server_selection');
  assert.equal(failure.activeInstance,'p1');
  assert.equal(failure.observations.p1.scene,'IntroScene');
  assert.match(failure.error,/MISSING_SERVER/);assert.equal(released,false);
  assert.match(artifacts.get('startup-failure-ui-p1.json').error, /capture unavailable/);
});

 test('startup failure stores UI geometry without replacing the original failure', async () => {
  const artifacts = new Map<string, any>();
  const ui = { elements: [{ automationId: 'btnDirectJoin', visible: true, interactable: false,
    panelBounds: { x: 0, y: 900, width: 200, height: 40 } }] };
  const platform = {
    instances: new Map([['p1', { id: 'p1', runId: 'run', role: 'client', owner: 'None' }]]),
    observe: async () => ({ scene: 'IntroScene', frame: 1 }),
    command: async (_id: string, type: string) => { if (type === 'ui.query') return ui; throw new Error('No screen'); },
    artifact: async (_run: string, name: string, value: unknown) => { artifacts.set(name, value); }
  } as unknown as Platform;
  await assert.rejects(joinInstances(platform, 'run', new AbortController().signal), /MISSING_SERVER/);
  assert.deepEqual(artifacts.get('startup-failure-ui-p1.json'), ui);
  assert.match(artifacts.get('startup-failure.json').error, /MISSING_SERVER/);
 });

test('unresponsive startup samples only the active participant and skips sampling on cancellation', async () => {
  for (const cancelled of [false,true]) {
    const sampled:string[]=[], artifacts=new Map<string,any>(),controller=new AbortController();
    const instances=new Map(['p1','p2','p3','p4'].map(id=>[id,{id,runId:'run',role:'client',owner:'None'}]));
    const platform={instances,
      observe:async()=>{if(cancelled)controller.abort();throw new Error('bridge unavailable');},
      diagnoseProcess:async(id:string)=>{sampled.push(id);return {captured:true};},
      command:async()=>{throw new Error('capture unavailable');},
      artifact:async(_run:string,name:string,value:unknown)=>{artifacts.set(name,value);}
    } as unknown as Platform;
    await assert.rejects(joinInstances(platform,'run',controller.signal),/bridge unavailable/);
    assert.deepEqual(sampled,cancelled?[]:['p1']);
    const failure=artifacts.get('startup-failure.json');
    assert.equal(Object.keys(failure.observations).length,4);
    assert.equal(failure.observations.p2.diagnostics.reason,cancelled?'startup_cancelled':'not_active_failure_participant');
  }
});
