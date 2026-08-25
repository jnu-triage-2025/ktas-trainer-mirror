using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>동적 VisualElement와 UIDocument root 재생성 뒤에도 입력 정책을 유지합니다.</summary>
  [DisallowMultipleComponent]
  public sealed class UIDocumentInteractionStateGuard : MonoBehaviour
  {
    private UIDocument _document;

    public static void Ensure(UIDocument document)
    {
      if (document == null)
        return;

      var guard = document.GetComponent<UIDocumentInteractionStateGuard>()
        ?? document.gameObject.AddComponent<UIDocumentInteractionStateGuard>();
      guard._document = document;
    }

    private void Update()
    {
      UIDocumentInteractionPolicy.Refresh(_document);
    }

    private void OnDestroy()
    {
      UIDocumentInteractionPolicy.Forget(_document);
    }
  }
}
