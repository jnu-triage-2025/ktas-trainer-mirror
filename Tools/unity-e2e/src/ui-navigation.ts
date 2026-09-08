import { Platform, E2EError } from './core.ts';
import { setTimeout as delay } from 'node:timers/promises';

export async function waitForInteractable(platform: Platform, id: string, name: string, signal: AbortSignal, documentId?: string) {
  let interactable:any;
  async function wait(check: () => Promise<boolean>, timeout: number) {
    const deadline = performance.now() + timeout;
    while (performance.now() < deadline) {
      signal.throwIfAborted();
      if (await check()) return;
      await delay(250, undefined, { signal });
    }
    throw new E2EError('DEADLINE_EXCEEDED', `UI target did not become interactable: ${name}`);
  }
    let scrollAttempts = 0, lastDirection = 0, scrollMagnitude = 1;
    await wait(async () => {
      const value = await platform.command(id, 'ui.query', { automationId: name, documentId }, { signal });
      if (value.elements.length !== 1) return false;
      const target = value.elements[0];
      if (target.interactable) { interactable=target; return true; }
      if (target.visible && target.enabled && target.scrollContainerId && scrollAttempts < 24) {
        const containers = await platform.command(id, 'ui.query', { automationId: target.scrollContainerId }, { signal });
        const container = containers.elements.find((element: any) => element.documentId === target.documentId);
        if (container?.interactable && container.screenCenter) {
          scrollAttempts++;
          await platform.command(id, 'ui.pointer', { ...container.screenCenter, pressed: false,
            screenWidth: container.screenWidth, screenHeight: container.screenHeight, frame: container.uiRevision }, { signal });
          const direction = target.screenCenter.y < container.screenCenter.y ? -1 : 1;
          if (lastDirection && direction !== lastDirection) scrollMagnitude = Math.max(1 / 1024, scrollMagnitude / 2);
          lastDirection = direction;
          await platform.command(id, 'input.execute', { sequence: [{ operation: 'scroll', y: direction * scrollMagnitude }] }, { signal });
        }
      }
      return false;
    }, 15000);
    return interactable;
}

export async function activateVisible(platform: Platform, id: string, name: string, signal: AbortSignal, documentId?: string, mode = 'device_input') {
  await waitForInteractable(platform,id,name,signal,documentId);
  await platform.command(id, 'ui.activate', { automationId: name, documentId, mode }, { signal });
}
