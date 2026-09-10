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
          const horizontalDistance = target.screenCenter.x - container.screenCenter.x;
          const verticalDistance = target.screenCenter.y - container.screenCenter.y;
          const horizontal = Math.abs(horizontalDistance) > Math.abs(verticalDistance);
          const direction = (horizontal ? -horizontalDistance : verticalDistance) < 0 ? -1 : 1;
          if (lastDirection && direction !== lastDirection) scrollMagnitude = Math.max(1 / 1024, scrollMagnitude / 2);
          lastDirection = direction;
          if(horizontal && target.scrollContainerId==='InventoryScrollView'){
            const match=await platform.command(id,'ui.query',{automationId:'InventoryHorizontalSlider'},{signal});
            if(match.elements.length===1&&match.elements[0].interactable){
              const slider=match.elements[0];
              const pointer={x:horizontalDistance>0?slider.screenWidth-20:20,y:slider.screenCenter.y,
                screenWidth:slider.screenWidth,screenHeight:slider.screenHeight,frame:slider.uiRevision};
              await platform.command(id,'ui.pointer',{...pointer,pressed:true},{signal});
              await platform.command(id,'ui.pointer',{...pointer,pressed:false},{signal});
              return false;
            }
          }
          await platform.command(id, 'input.execute', { sequence: [{ operation: 'scroll',
            ...(horizontal ? {x:direction * scrollMagnitude} : {y:direction * scrollMagnitude}) }] }, { signal });
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
