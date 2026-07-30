using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// UIDocument를 제어하는 모든 MonoBehaviour 기반 UI의 공통 부모입니다.
  /// 표시 상태와 포인터 히트테스트 상태를 항상 함께 관리합니다.
  /// 새 UIDocument 기반 UI 컨트롤러는 반드시 이 클래스를 직접 또는 UIControllerABC를 통해 상속해야 합니다.
  /// NetworkBehaviour 등 단일 상속 때문에 불가능한 경우에는 UIDocumentInteractionPolicy를 사용해야 합니다.
  /// </summary>
  public abstract class UIDocumentControllerABC : MonoBehaviour
  {
    /// <summary>
    /// 모달 UIDocument의 표시 및 전체 서브트리 픽킹 상태를 함께 전환합니다.
    /// </summary>
    protected static void SetDocumentRootInteractable(UIDocument document, bool visible)
    {
      SetDocumentVisible(document, visible);
    }

    /// <summary>
    /// 문서는 보이되 포인터 입력을 받지 않아야 하는 HUD에 사용합니다.
    /// 새 UI 컨트롤러는 루트만 Ignore하지 말고 반드시 이 메서드로 전체 트리를 전환해야 합니다.
    /// </summary>
    protected static void SetDocumentRootPickingEnabled(UIDocument document, bool enabled)
    {
      UIDocumentInteractionPolicy.SetPickingEnabled(document, enabled);
    }

    /// <summary>
    /// UIDocument의 화면 표시와 전체 하위 트리 픽킹 상태를 한 번에 변경합니다.
    /// 닫힌 UI는 display=None과 PickingMode.Ignore를 모두 적용해야 합니다.
    /// </summary>
    protected static void SetDocumentVisible(UIDocument document, bool visible)
    {
      UIDocumentInteractionPolicy.SetVisible(document, visible);
    }

    /// <summary>
    /// UIDocument root가 늦게 생성되는 경우에도 닫힌 오버레이가 클릭을 가로채지 않게 합니다.
    /// </summary>
    protected IEnumerator NeutralizeDocumentRootWhenReady(UIDocument document)
    {
      while (document != null && document.rootVisualElement == null)
        yield return null;

      SetDocumentVisible(document, false);
    }

    protected static void SetSubtreePickingMode(VisualElement root, PickingMode mode)
    {
      UIDocumentInteractionPolicy.SetSubtreePickingMode(root, mode);
    }
  }
}
