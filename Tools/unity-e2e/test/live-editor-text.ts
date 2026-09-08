import assert from 'node:assert/strict';
import { Platform, loadConfig } from '../src/core.ts';
import { setTimeout as delay } from 'node:timers/promises';
const p=new Platform(await loadConfig(process.argv[2]??'config.json'));
try {
  await p.editorCommand('editor.play');
  const until=performance.now()+30000;
  while(true){try{await p.attachEditor();break;}catch(error){if(performance.now()>until)throw error;await delay(300);}}
  await p.acquire('editor');
  while (!(await p.command('editor','ui.query',{automationId:'nameField'})).elements.some((e:any)=>e.interactable)) { if(performance.now()>until)throw new Error('Title not ready');await delay(200); }
  const ui=await p.command('editor','ui.query');
  console.log(JSON.stringify(ui.elements.filter((e:any)=>e.visible&&e.elementType==='TextField')));
  const field=ui.elements.find((e:any)=>e.visible&&e.elementType==='TextField');assert.ok(field);
  await p.command('editor','ui.activate',{automationId:field.automationId,mode:'device_input'});
  await p.command('editor','ui.text',{text:'E2Etext',mode:'input_adapter'});
  const result=await p.command('editor','ui.query',{automationId:field.automationId});
  console.log(JSON.stringify(result));assert.ok(result.elements[0].value.includes('E2Etext'));
}finally{await p.close();await p.editorCommand('editor.stop');}
