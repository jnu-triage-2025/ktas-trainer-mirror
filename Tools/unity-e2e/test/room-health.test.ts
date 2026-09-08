import test from 'node:test';
import assert from 'node:assert/strict';
import { RoomHealth } from '../src/room-health.ts';
const player = (frame=1,tick=10) => ({frame,client:{localPlayerReady:true,players:[0,1,2,3].map(ownerId=>({ownerId}))},server:{started:true,connections:4,tick}});
const host = [{id:'host',role:'host'}];
test('room health distinguishes frame, server tick, and participant failures',()=>{
  for(const [sample,code] of [[player(1,11),'MAIN_LOOP_STALLED'],[player(2,10),'SERVER_TICK_STALLED'],
    [{...player(2,11),client:{localPlayerReady:false}},'PARTICIPANTS_LOST'],
    [{...player(2,11),server:{started:true,connections:3,tick:11}},'SERVER_CONNECTIONS_LOST']] as const) {
    const health=new RoomHealth();health.check(host,[player()]);
    assert.throws(()=>health.check(host,[sample]),{code});
  }
});
test('room health allows dedicated servers without a local player and detects duplicate owners',()=>{
  const health=new RoomHealth();health.check([{id:'server',role:'dedicated'}],[{frame:1,server:player().server}]);
  const value=player();value.client.players[3].ownerId=2;
  assert.throws(()=>new RoomHealth().check(host,[value]),{code:'PARTICIPANTS_LOST'});
  assert.throws(()=>new RoomHealth().check(host,[]),{code:'OBSERVATION_GAP'});
});
