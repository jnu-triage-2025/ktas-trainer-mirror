import test from 'node:test';
import assert from 'node:assert/strict';
import {verifyPatientRoles} from '../src/patient-roles.ts';
const state=()=>Object.fromEntries([0,1,2,3].map(i=>[`p${i+1}`,{client:{players:[0,1,2,3].map(j=>({ownerId:j,userIdentifier:`u${j}`,local:i===j,tags:[`nurse_${'abcd'[j]}`]}))}}]));
test('all four clients must agree on four distinct exclusive roles',()=>{
 assert.equal(verifyPatientRoles(state()).length,4);
 for(const mutate of [
  (s:any)=>s.p2.client.players[0].tags=['nurse_b'],
  (s:any)=>s.p3.client.players[2].tags.push('nurse_d'),
  (s:any)=>s.p4.client.players[3].userIdentifier='u0',
  (s:any)=>s.p1.client.players.pop(),
  (s:any)=>s.p1.client.players[1].local=true
 ]){const s=state();mutate(s);assert.throws(()=>verifyPatientRoles(s),{code:'PATIENT_ROLE_NOT_READY'});}
});
