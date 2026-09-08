import assert from 'node:assert/strict';
import {spawn} from 'node:child_process';
import {randomBytes,randomUUID,createHash} from 'node:crypto';
import {mkdir,readFile,writeFile} from 'node:fs/promises';
import {createServer,connect} from 'node:net';
import {resolve,join} from 'node:path';
import {setTimeout as delay} from 'node:timers/promises';

const executable=resolve(process.argv[2]??'../../Build/E2E-removal-check.app/Contents/MacOS/KTASTrainer');
const runId=randomUUID(), directory=resolve(process.argv[3]??'../../artifacts/unity-e2e',runId);
await mkdir(join(directory,'profile'),{recursive:true});
const reservation=createServer();
await new Promise<void>((resolve,reject)=>{reservation.once('error',reject);reservation.listen(0,'127.0.0.1',resolve);});
const port=(reservation.address() as {port:number}).port;
await new Promise<void>((resolve,reject)=>reservation.close(error=>error?reject(error):resolve()));
const reachable=()=>new Promise<boolean>(resolve=>{
 const socket=connect({host:'127.0.0.1',port});
 const finish=(value:boolean)=>{socket.destroy();resolve(value);};
 socket.setTimeout(500,()=>finish(false));socket.once('connect',()=>finish(true));socket.once('error',()=>finish(false));
});
const player=spawn(executable,['--e2e','-screen-fullscreen','0','-screen-width','960','-screen-height','540','-logFile',join(directory,'player.log')],{
 env:{...process.env,UNITY_E2E_TOKEN:randomBytes(32).toString('hex'),UNITY_E2E_PORT:String(port),UNITY_E2E_INSTANCE:'ordinary',UNITY_E2E_RUN:runId,UNITY_E2E_PROFILE:join(directory,'profile'),UNITY_E2E_ALLOW_PROTOCOL_TESTS:'1',UNITY_E2E_ALLOW_SCENARIO_FIXTURES:'1'},stdio:'ignore'
});
let exited=false,spawnError:Error|undefined;
const exit=new Promise<void>(resolve=>{player.once('error',error=>{spawnError=error;exited=true;resolve();});player.once('exit',()=>{exited=true;resolve();});});
let state='failed',failure:string|undefined,samples=0;
try {
 const deadline=performance.now()+20000;
 while(performance.now()<deadline){
  assert.equal(exited,false,spawnError?.message??'Ordinary player exited before observation completed');
  assert.equal(await reachable(),false,'Ordinary build opened the requested automation port');
  samples++;await delay(250);
 }
 const log=await readFile(join(directory,'player.log'),'utf8');
 assert.match(log,/Initialize engine version: 6000\.2\.8f1/,'Unity engine initialization must be observed');
 state='passed';
} catch(error){failure=String(error);}
finally {
 if(!exited)player.kill('SIGTERM');
 await Promise.race([exit,delay(5000)]);
 if(!exited){player.kill('SIGKILL');await exit;}
 const report={runId,state,failure,samples,processExited:exited,exitCode:player.exitCode,exitSignal:player.signalCode,executableSha256:createHash('sha256').update(await readFile(executable)).digest('hex'),scope:'Ordinary macOS process stays alive for 20 seconds after launch with valid automation options; sampled requested TCP port remains closed. Does not prove all gameplay or absence of every possible listener.'};
 await writeFile(join(directory,'ordinary-runtime-report.json'),JSON.stringify(report,null,2)+'\n');
 console.log(JSON.stringify({runId,state,samples,processExited:exited}));process.exitCode=state==='passed'?0:1;
}
