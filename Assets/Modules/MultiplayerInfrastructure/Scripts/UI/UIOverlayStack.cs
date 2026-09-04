using System.Collections.Generic;
using UnityEngine;
namespace MultiplayerInfrastructure.UI
{
  public static class UIOverlayStack
  {
    private static Stack<IUIOverlay> Stack { get; } = new();
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
      if (overlay == null)
        return;

      if (Stack.Count > 0)
      {
        if (Stack.Peek() == overlay)
        {
          return;
        }

        // 같은 오버레이가 스택 중간에 남아 있는 채로 다시 push 되면 항목이 중복된다.
        // 그 상태에서는 Pop 을 한 번 해도 아래쪽 중복 항목이 최상단으로 올라와
        // IsTop 이 계속 참으로 남고, 플레이어 입력이 영구히 잠긴다.
        // (대화창 위에 커맨드 채팅을 연 뒤 다음 대화 노드가 표시되는 경로에서 발생한다.)
        RemoveFromStack(overlay);

        if (Stack.Count > 0)
          SafeOnOverlayPopped(Stack.Peek());
      }

      Stack.Push(overlay);
      SafeOnOverlayPushed(overlay);
      NotifyStackChangedIfNeeded(previousCount, previousTop);
    }

    /// <summary>
    /// 스택에 남아 있는 <paramref name="overlay"/> 항목을 모두 제거한다.
    /// 최상단 항목이 아닌 중복 항목을 걷어내는 용도이므로 Pop 콜백을 호출하지 않는다.
    /// (최상단 항목을 실제로 내리는 처리는 호출부가 별도로 수행한다.)
    /// </summary>
    private static void RemoveFromStack(IUIOverlay overlay)
    {
      if (Stack.Count == 0)
        return;

      var kept = new List<IUIOverlay>(Stack.Count);
      while (Stack.Count > 0)
      {
        var each = Stack.Pop();
        if (each == overlay)
          continue;
        kept.Add(each);
      }

      for (int i = kept.Count - 1; i >= 0; i--)
        Stack.Push(kept[i]);
    }

    public static IUIOverlay Pop()
    {
      int previousCount = Stack.Count;
      IUIOverlay previousTop = Stack.Count > 0 ? Stack.Peek() : null;

      PruneDeadOverlays();
      if (Stack.Count == 0)
        return null;

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

    /// <summary>
    /// 스택의 어느 위치에 있든 지정한 오버레이를 제거한다.
    ///
    /// 대화창 위에 채팅처럼 다른 오버레이가 열린 상태에서 대화 시나리오가 끝날 수 있다.
    /// 이때 최상단만 Pop 하면 숨겨진 대화창 항목이 스택에 남아 이후 입력을 계속 막으므로,
    /// 종료 주체가 자기 항목을 명시적으로 제거할 수 있어야 한다.
    /// </summary>
    /// <returns>지정한 오버레이를 실제로 제거했으면 true.</returns>
    public static bool Remove(IUIOverlay overlay)
    {
      if (overlay == null)
        return false;

      int previousCount = Stack.Count;
      IUIOverlay previousTop = Stack.Count > 0 ? Stack.Peek() : null;
      PruneDeadOverlays();

      if (Stack.Count == 0)
        return false;

      bool removedTop = Stack.Peek() == overlay;
      bool removed = false;
      var kept = new List<IUIOverlay>(Stack.Count);
      while (Stack.Count > 0)
      {
        var each = Stack.Pop();
        if (each == overlay)
        {
          removed = true;
          continue;
        }

        kept.Add(each);
      }

      for (int i = kept.Count - 1; i >= 0; i--)
        Stack.Push(kept[i]);

      if (!removed)
        return false;

      SafeOnOverlayPopped(overlay);
      if (removedTop && Stack.Count > 0)
        SafeOnOverlayPushed(Stack.Peek());

      NotifyStackChangedIfNeeded(previousCount, previousTop);
      return true;
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
      if (Stack.Count == 0)
        return;

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
      if (overlay == null)
        return false;

      if (overlay is Object unityObject)
        return unityObject != null;

      return true;
    }

    private static void SafeOnOverlayPushed(IUIOverlay overlay)
    {
      if (!IsAlive(overlay))
        return;
      overlay.OnOverlayPushed();
    }

    private static void SafeOnOverlayPopped(IUIOverlay overlay)
    {
      if (!IsAlive(overlay))
        return;
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
