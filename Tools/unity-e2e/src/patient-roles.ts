import {E2EError} from './errors.ts';
const roles=['nurse_a','nurse_b','nurse_c','nurse_d'];
export function verifyPatientRoles(snapshots:Record<string,any>) {
 const identities=roles.map((role,index)=>{
  const actor=`p${index+1}`,snapshot=snapshots[actor];
  const local=snapshot?.client?.players?.filter((p:any)=>p.local===true)??[];
  if(local.length!==1||!Number.isInteger(local[0].ownerId)||typeof local[0].userIdentifier!=='string'||!local[0].userIdentifier)
   throw new E2EError('PATIENT_ROLE_NOT_READY',actor);
  return {actor,role,ownerId:local[0].ownerId,userIdentifier:local[0].userIdentifier};
 });
 if(new Set(identities.map(x=>x.ownerId)).size!==4||new Set(identities.map(x=>x.userIdentifier)).size!==4)
  throw new E2EError('PATIENT_ROLE_NOT_READY','Four distinct players required');
 for(const {actor} of identities){
  const players=snapshots[actor].client.players;
  if(players.length!==4)throw new E2EError('PATIENT_ROLE_NOT_READY',actor);
  for(const expected of identities){
   const matches=players.filter((p:any)=>p.ownerId===expected.ownerId&&p.userIdentifier===expected.userIdentifier);
   if(matches.length!==1||!Array.isArray(matches[0].tags))throw new E2EError('PATIENT_ROLE_NOT_READY',actor);
   const actual=matches[0].tags.filter((tag:string)=>roles.includes(tag));
   if(actual.length!==1||actual[0]!==expected.role)throw new E2EError('PATIENT_ROLE_NOT_READY',`${actor}:${expected.actor}`);
  }
 }
 return identities;
}
