using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private bool _overlaySyncSubscribed;

    private void OnStartClient_UIOverlaySync()
    {
      if (!IsOwner)
        return;

      UIOverlayStack.Clear();
      SubscribeOverlayStackChanged();
      SyncOverlayDrivenPlayerState();
    }

    private void OnStopClient_UIOverlaySync()
    {
      if (!IsOwner)
        return;

      UnsubscribeOverlayStackChanged();
      UIOverlayStack.Clear();
      SyncOverlayDrivenPlayerState();
    }

    private void SubscribeOverlayStackChanged()
    {
      if (_overlaySyncSubscribed)
        return;

      UIOverlayStack.StackChanged += HandleOverlayStackChanged;
      _overlaySyncSubscribed = true;
    }

    private void UnsubscribeOverlayStackChanged()
    {
      if (!_overlaySyncSubscribed)
        return;

      UIOverlayStack.StackChanged -= HandleOverlayStackChanged;
      _overlaySyncSubscribed = false;
    }

    private void HandleOverlayStackChanged()
    {
      string topName = UIOverlayStack.Top?.GetType().Name ?? "none";
      Debug.Log(
        $"[PlayerController][UIOverlayStack] Changed. count={UIOverlayStack.Count}, " +
        $"top={topName}, canMove={canMove}, cursorLocked={IsCursorLocked}.",
        this);
      SyncOverlayDrivenPlayerState();
    }

    private void SyncOverlayDrivenPlayerState()
    {
      bool wasMoveEnabled = canMove;
      bool wasCursorLocked = IsCursorLocked;
      string topName = UIOverlayStack.Top?.GetType().Name ?? "none";

      if (UIOverlayStack.IsEmpty())
      {
        if (!canMove || !IsCursorLocked)
          ExitUIOverlayMode();
      }
      else if (canMove || IsCursorLocked)
        EnterUIOverlayMode();

      if (wasMoveEnabled != canMove || wasCursorLocked != IsCursorLocked)
      {
        Debug.Log(
          $"[PlayerController][UIInputState] Corrected overlay-driven state. top={topName}, " +
          $"canMove={wasMoveEnabled}->{canMove}, cursorLocked={wasCursorLocked}->{IsCursorLocked}.",
          this);
      }
    }

    /// <summary>
    /// 게임 모드 전환 등 다른 시스템이 이동·커서 상태를 바꾼 경우에도 오버레이 상태를 다시 적용한다.
    /// </summary>
    private void EnsureOverlayDrivenPlayerState() => SyncOverlayDrivenPlayerState();
  }
}
