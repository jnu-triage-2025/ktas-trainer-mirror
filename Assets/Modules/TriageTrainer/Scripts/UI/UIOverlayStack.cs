using System.Collections.Generic;
namespace TriageTrainer.UI
{
  public static class UIOverlayStack
  {
    private static Stack<IUIOverlay> Stack { get; } = new ();
    public static IUIOverlay Top => Stack.Count > 0 ? Stack.Peek() : null;
    public static bool IsTop(IUIOverlay overlay) => Stack.Count > 0 && Stack.Peek() == overlay;
    public static void Push(IUIOverlay overlay)
    {
      if (overlay == null) return;

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
        overlay.OnOverlayPopped();
      }
    }

    public static bool IsEmpty() => Stack.Count == 0;
  }
}
