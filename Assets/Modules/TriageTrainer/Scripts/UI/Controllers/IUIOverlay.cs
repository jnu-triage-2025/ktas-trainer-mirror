using System;

namespace TriageTrainer.UI
{
  public interface IUIOverlay
  {
    event Action OverlayPushed;
    event Action OverlayPopped;

    void OnOverlayPushed();
    void OnOverlayPopped();
  }
}
