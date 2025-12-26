using System;

namespace MultiplayerInfrastructure.UI
{
  public interface IUIOverlay
  {
    event Action OverlayPushed;
    event Action OverlayPopped;

    void OnOverlayPushed();
    void OnOverlayPopped();
  }
}
