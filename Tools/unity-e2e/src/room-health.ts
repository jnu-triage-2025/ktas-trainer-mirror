import { E2EError } from './errors.ts';
export class RoomHealth {
  private frames = new Map<string, number>();
  private ticks = new Map<string, number>();
  check(instances: {id:string;role:string}[], observations: any[]) {
    if (instances.length !== observations.length) throw new E2EError('OBSERVATION_GAP');
    for (let n = 0; n < instances.length; n++) {
      const instance = instances[n], value = observations[n];
      if (!Number.isFinite(value?.frame)) throw new E2EError('OBSERVATION_GAP', instance.id);
      if (this.frames.get(instance.id) === value.frame) throw new E2EError('MAIN_LOOP_STALLED', instance.id);
      this.frames.set(instance.id, value.frame);
      if (instance.role !== 'dedicated' && (!value.client?.localPlayerReady || value.client.players?.length !== 4
        || new Set(value.client.players.map((p: any) => p.ownerId)).size !== 4))
        throw new E2EError('PARTICIPANTS_LOST', instance.id);
      if (instance.role === 'host' || instance.role === 'dedicated') {
        if (!value.server?.started || value.server.connections !== 4) throw new E2EError('SERVER_CONNECTIONS_LOST', instance.id);
        if (!Number.isFinite(value.server.tick)) throw new E2EError('OBSERVATION_GAP', instance.id);
        if (this.ticks.get(instance.id) === value.server.tick) throw new E2EError('SERVER_TICK_STALLED', instance.id);
        this.ticks.set(instance.id, value.server.tick);
      }
    }
  }
}
