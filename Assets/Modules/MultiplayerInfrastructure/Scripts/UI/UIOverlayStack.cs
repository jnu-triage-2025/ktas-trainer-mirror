using System.Collections.Generic;
using UnityEngine;
namespace MultiplayerInfrastructure.UI
{
  public static class UIOverlayStack
  {
    private static Stack<IUIOverlay> Stack { get; } = new ();
    public static event System.Action StackChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      Stack.Clear();
    }

    public static IUIOverlay Top
    {
      get
      {
        PruneDeadOverlays();
        return Stack.Count > 0 ? Stack.Peek() : null;
      }
    }

    public static bool IsTop(IUIOverlay overlay)
    {
      PruneDeadOverlays();
      return Stack.Count > 0 && Stack.Peek() == overlay;
    }
    public static void Push(IUIOverlay overlay)
    {
      int previousCount = Stack.Count;
      IUIOverlay previousTop = Stack.Count > 0 ? Stack.Peek() : null;

      PruneDeadOverlays();
      if (overlay == null) return;

      if (Stack.Count > 0)
      {
        if (Stack.Peek() == overlay)
        {
          return;
        }

        SafeOnOverlayPopped(Stack.Peek());
      }

      Stack.Push(overlay);
      SafeOnOverlayPushed(overlay);
      NotifyStackChangedIfNeeded(previousCount, previousTop);
    }

    public static IUIOverlay Pop()
    {
      int previousCount = Stack.Count;
      IUIOverlay previousTop = Stack.Count > 0 ? Stack.Peek() : null;

      PruneDeadOverlays();
      if (Stack.Count == 0) return null;

      var overlay = Stack.Pop();
      SafeOnOverlayPopped(overlay);

      PruneDeadOverlays();

      if (Stack.Count > 0)
      {
        SafeOnOverlayPushed(Stack.Peek());
      }

      NotifyStackChangedIfNeeded(previousCount, previousTop);

      return overlay;
    }

    public static void Clear()
    {
      int previousCount = Stack.Count;
      IUIOverlay previousTop = Stack.Count > 0 ? Stack.Peek() : null;

      while (Stack.Count > 0)
      {
        var overlay = Stack.Pop();
        SafeOnOverlayPopped(overlay);
      }

      NotifyStackChangedIfNeeded(previousCount, previousTop);
    }

    public static bool IsEmpty()
    {
      PruneDeadOverlays();
      return Stack.Count == 0;
    }

    private static void PruneDeadOverlays()
    {
      if (Stack.Count == 0) return;

      int previousCount = Stack.Count;
      IUIOverlay previousTop = Stack.Peek();

      var alive = new List<IUIOverlay>(Stack.Count);
      while (Stack.Count > 0)
      {
        var overlay = Stack.Pop();
        if (IsAlive(overlay))
          alive.Add(overlay);
      }

      for (int i = alive.Count - 1; i >= 0; i--)
        Stack.Push(alive[i]);

      NotifyStackChangedIfNeeded(previousCount, previousTop);
    }

    private static bool IsAlive(IUIOverlay overlay)
    {
      if (overlay == null) return false;

      if (overlay is Object unityObject)
        return unityObject != null;

      return true;
    }

    private static void SafeOnOverlayPushed(IUIOverlay overlay)
    {
      if (!IsAlive(overlay)) return;
      overlay.OnOverlayPushed();
    }

    private static void SafeOnOverlayPopped(IUIOverlay overlay)
    {
      if (!IsAlive(overlay)) return;
      overlay.OnOverlayPopped();
    }

    private static void NotifyStackChangedIfNeeded(int previousCount, IUIOverlay previousTop)
    {
      int currentCount = Stack.Count;
      IUIOverlay currentTop = currentCount > 0 ? Stack.Peek() : null;
      if (currentCount == previousCount && currentTop == previousTop)
        return;

      StackChanged?.Invoke();
    }
  }
}
