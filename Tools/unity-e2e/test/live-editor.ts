import { Platform } from '../src/core.ts';
import { resolve } from 'node:path';
import { setTimeout as delay } from 'node:timers/promises';
const root=resolve(import.meta.dirname,'../../..');
const p=new Platform({builds:{},artifactRoot:resolve(root,'artifacts/unity-e2e'),editorConnectionFile:resolve(root,'Temp/e2e-editor-connection.json')});
try{
 console.log('editor',await p.editorCommand('editor.observe'));
 console.log('play',await p.editorCommand('editor.play'));
 let attached=false;
 for(let n=0;n<40;n++){try{await p.attachEditor();attached=true;break;}catch{await delay(500);}}
 if(!attached)throw new Error('Editor runtime bridge unavailable');
 console.log('attached',p.list());
 await p.acquire('editor');
 await p.command('editor','ui.activate',{automationId:'btnSettings',mode:'device_input'});
 await delay(500);const query=await p.command('editor','ui.query',{automationId:'settings-root'});console.log('settings',query);
 if(!query.elements.some((e:any)=>e.visible))throw new Error('Editor settings not visible');
}finally{await p.close();await p.editorCommand('editor.stop');}
