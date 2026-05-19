using System.Collections.Generic;
using UnityEngine;
namespace MultiplayerInfrastructure.UI
{
  public static class UIOverlayStack
  {
    private static Stack<IUIOverlay> Stack { get; } = new ();
    public static IUIOverlay Top
    {
      get
      {
        PruneInvalidOverlays();
        return Stack.Count > 0 ? Stack.Peek() : null;
      }
    }
    public static bool IsTop(IUIOverlay overlay)
    {
      PruneInvalidOverlays();
      return Stack.Count > 0 && Stack.Peek() == overlay;
    }

    private static bool IsOverlayAlive(IUIOverlay overlay)
    {
      if (ReferenceEquals(overlay, null))
        return false;

      if (overlay is Object unityObject)
        return unityObject != null;

      return true;
    }

    private static void PruneInvalidOverlays()
    {
      if (Stack.Count == 0)
        return;

      var validOverlays = new List<IUIOverlay>(Stack.Count);
      while (Stack.Count > 0)
      {
        var overlay = Stack.Pop();
        if (IsOverlayAlive(overlay))
          validOverlays.Add(overlay);
      }

      for (int i = validOverlays.Count - 1; i >= 0; i--)
      {
        Stack.Push(validOverlays[i]);
      }
    }

    public static void Push(IUIOverlay overlay)
    {
      if (overlay == null) return;
      PruneInvalidOverlays();

      if (Stack.Count > 0)
      {
        if (Stack.Peek() == overlay)
        {
          return;
        }

        Stack.Peek().OnOverlayPopped();
      }

      Stack.Push(overlay);
      overlay.OnOverlayPushed();
    }

    public static IUIOverlay Pop()
    {
      PruneInvalidOverlays();
      if (Stack.Count == 0) return null;

      var overlay = Stack.Pop();
      overlay.OnOverlayPopped();

      if (Stack.Count > 0)
      {
        Stack.Peek().OnOverlayPushed();
      }

      return overlay;
    }

    public static void Clear()
    {
      while (Stack.Count > 0)
      {
        var overlay = Stack.Pop();
        if (IsOverlayAlive(overlay))
          overlay.OnOverlayPopped();
      }
    }

    public static bool IsEmpty()
    {
      PruneInvalidOverlays();
      return Stack.Count == 0;
    }
  }
}
