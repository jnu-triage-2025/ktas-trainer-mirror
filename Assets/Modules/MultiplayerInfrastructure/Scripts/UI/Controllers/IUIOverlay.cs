using System;

namespace MultiplayerInfrastructure.UI
{
  public interface IUIOverlay
  {
    public event Action OverlayPushed;
    public event Action OverlayPopped;

    public void OnOverlayPushed();
    public void OnOverlayPopped();
  }
}
