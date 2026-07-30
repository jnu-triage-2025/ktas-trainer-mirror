using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 모든 UIDocument의 표시 및 포인터 히트테스트 규약입니다.
  /// 상속할 수 없는 NetworkBehaviour 기반 월드 UI도 이 정책을 직접 사용합니다.
  /// </summary>
  public static class UIDocumentInteractionPolicy
  {
    public static void SetVisible(UIDocument document, bool visible)
    {
      var root = document != null ? document.rootVisualElement : null;
      if (root == null)
        return;

      root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      SetSubtreePickingMode(root, visible ? PickingMode.Position : PickingMode.Ignore);
    }

    public static void SetPickingEnabled(UIDocument document, bool enabled)
    {
      var root = document != null ? document.rootVisualElement : null;
      if (root == null)
        return;

      root.style.display = DisplayStyle.Flex;
      SetSubtreePickingMode(root, enabled ? PickingMode.Position : PickingMode.Ignore);
    }

    public static void SetSubtreePickingMode(VisualElement root, PickingMode mode)
    {
      if (root == null)
        return;

      root.pickingMode = mode;
      for (int i = 0; i < root.childCount; i++)
        SetSubtreePickingMode(root[i], mode);
    }
  }
}
