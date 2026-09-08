import {timingSafeEqual} from 'node:crypto';
export type Access = 'operator' | 'observer';
const observations = new Set([
 'assistance.list','instances.list','editor.observe','game.observe','game.catalogue','game.screenshot','ui.query',
 'events.read','conditions.wait','scenario.validate','scenario.status','operations.status',
 'artifacts.list','artifacts.read','network.status'
]);
export function authorize(access:Access,tool:string){return access==='operator'||observations.has(tool);}
export function credentials(operatorToken:string,observerToken?:string,expiresAt?:number){
 if(expiresAt!==undefined&&(!Number.isSafeInteger(expiresAt)||expiresAt<=Date.now()))throw new Error('Credential expiry must be a future Unix timestamp in milliseconds');
 if(operatorToken.length<32||(observerToken!==undefined&&observerToken.length<32))throw new Error('Console tokens must contain at least 32 characters');
 if(operatorToken===observerToken)throw new Error('Observer and operator credentials must differ');
 const entries:[Access,Buffer][]=[['operator',Buffer.from(`Bearer ${operatorToken}`)]];
 if(observerToken!==undefined)entries.push(['observer',Buffer.from(`Bearer ${observerToken}`)]);
 return (header:string|undefined):Access|undefined=>{
  if(expiresAt!==undefined&&Date.now()>=expiresAt)return undefined;
  const supplied=Buffer.from(header??'');
  for(const [access,expected] of entries)if(expected.length===supplied.length&&timingSafeEqual(expected,supplied))return access;
  return undefined;
 };
}
