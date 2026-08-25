using MultiplayerInfrastructure.UI;

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

      UIOverlayStack.StackChanged += SyncOverlayDrivenPlayerState;
      _overlaySyncSubscribed = true;
    }

    private void UnsubscribeOverlayStackChanged()
    {
      if (!_overlaySyncSubscribed)
        return;

      UIOverlayStack.StackChanged -= SyncOverlayDrivenPlayerState;
      _overlaySyncSubscribed = false;
    }

    private void SyncOverlayDrivenPlayerState()
    {
      if (UIOverlayStack.IsEmpty())
      {
        if (!canMove || !IsCursorLocked)
          ExitUIOverlayMode();
        return;
      }

      if (canMove || IsCursorLocked)
        EnterUIOverlayMode();
    }
  }
}
