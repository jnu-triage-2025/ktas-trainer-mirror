using System.Collections.Generic;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 모든 UIDocument의 표시 및 포인터 히트테스트 규약입니다.
  /// 상속할 수 없는 NetworkBehaviour 기반 월드 UI도 이 정책을 직접 사용합니다.
  /// </summary>
  public static class UIDocumentInteractionPolicy
  {
    private sealed class State
    {
      public bool IsVisible = true;
      public bool IsPickingEnabled = true;
      public VisualElement LastRoot;
      public readonly Dictionary<VisualElement, PickingMode> PickingModes = new();
      public readonly List<VisualElement> StaleElements = new();
    }

    private static readonly Dictionary<UIDocument, State> States = new();

    public static void SetVisible(UIDocument document, bool visible)
    {
      if (document == null)
        return;

      var state = GetOrCreateState(document);
      state.IsVisible = visible;
      state.IsPickingEnabled = visible;
      Apply(document, state);
    }

    public static void SetPickingEnabled(UIDocument document, bool enabled)
    {
      if (document == null)
        return;

      var state = GetOrCreateState(document);
      state.IsVisible = true;
      state.IsPickingEnabled = enabled;
      Apply(document, state);
    }

    /// <summary>상태를 가진 UIDocument의 root 재생성 및 동적 자식 추가 뒤에 정책을 재적용합니다.</summary>
    public static void Refresh(UIDocument document)
    {
      if (document != null && States.TryGetValue(document, out var state))
        Apply(document, state);
    }

    public static void Forget(UIDocument document)
    {
      if (document != null)
        States.Remove(document);
    }

    /// <summary>월드 표시처럼 영구적으로 비상호작용인 임의의 트리에 사용합니다.</summary>
    public static void SetSubtreePickingMode(VisualElement root, PickingMode mode)
    {
      if (root == null)
        return;

      root.pickingMode = mode;
      for (int i = 0; i < root.childCount; i++)
        SetSubtreePickingMode(root[i], mode);
    }

    private static State GetOrCreateState(UIDocument document)
    {
      if (!States.TryGetValue(document, out var state))
      {
        state = new State();
        States.Add(document, state);
      }

      UIDocumentInteractionStateGuard.Ensure(document);
      return state;
    }

    private static void Apply(UIDocument document, State state)
    {
      var root = document.rootVisualElement;
      if (root == null)
        return;

      if (!ReferenceEquals(state.LastRoot, root))
      {
        state.LastRoot = root;
        state.PickingModes.Clear();
      }

      bool enabled = state.IsVisible && state.IsPickingEnabled;
      root.style.display = state.IsVisible ? DisplayStyle.Flex : DisplayStyle.None;
      if (enabled)
        RestorePickingModes(root, state);
      else
        CaptureAndDisablePicking(root, state);

      PruneDetachedElements(root, state);
    }

    private static void CaptureAndDisablePicking(VisualElement element, State state)
    {
      if (!state.PickingModes.ContainsKey(element))
        state.PickingModes.Add(element, element.pickingMode);

      element.pickingMode = PickingMode.Ignore;
      for (int i = 0; i < element.childCount; i++)
        CaptureAndDisablePicking(element[i], state);
    }

    private static void RestorePickingModes(VisualElement element, State state)
    {
      if (state.PickingModes.TryGetValue(element, out var mode))
        element.pickingMode = mode;

      for (int i = 0; i < element.childCount; i++)
        RestorePickingModes(element[i], state);
    }

    private static void PruneDetachedElements(VisualElement root, State state)
    {
      // 동적으로 사라진 토스트/행을 보관하면 장시간 세션에서 상태 맵이 누적된다.
      // root가 패널에 부착된 경우에만 제거하여 초기 생성 중인 트리를 오인하지 않는다.
      if (root.panel == null)
        return;

      foreach (var pair in state.PickingModes)
      {
        if (pair.Key != null && !ReferenceEquals(pair.Key, root) && pair.Key.panel == null)
          state.StaleElements.Add(pair.Key);
      }

      for (int i = 0; i < state.StaleElements.Count; i++)
        state.PickingModes.Remove(state.StaleElements[i]);
      state.StaleElements.Clear();
    }
  }
}
