import {parentPort,workerData} from 'node:worker_threads';
import {EventStore} from './store.ts';
const store=new EventStore(workerData.root);
parentPort!.on('message',({id,operation,args})=>{
  try {
    let value;
    switch(operation){
      case 'append':value=store.append(args[0],args[1],args[2],args[3]);break;
      case 'appendMany':value=store.appendMany(args[0],args[1],args[2],args[3]);break;
      case 'read':value=store.read(args[0],args[1],args[2]);break;
      case 'cursor':value=store.cursor();break;
      case 'close':store.close();parentPort!.postMessage({id,value:null});parentPort!.close();return;
      default:throw new Error('UNKNOWN_HISTORY_OPERATION');
    }
    parentPort!.postMessage({id,value});
  } catch(error:any){parentPort!.postMessage({id,error:{code:error.code??'HISTORY_STORAGE_FAILED',message:String(error)}});}
});
