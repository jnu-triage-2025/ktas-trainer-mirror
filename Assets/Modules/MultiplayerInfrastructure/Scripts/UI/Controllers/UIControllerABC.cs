using System.Collections;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// UIControllerABC는 모든 UI 컨트롤러의 상속이 의도되는 추상 메서드입니다. 각 UI 요소들이 싱글톤 패턴이
  /// 의도되지 않았으므로, 외부에서 UI 컨트롤을 취득하는 데 있어서 별개의 레지스트리로부터 접근할 수 있도록 하고 있습니다.
  ///
  /// UIControllerABC는 레지스트리를 통해 외부에서 접근 가능하도록 이 컨트롤을 레지스터하는 역할을 합니다.
  /// </summary>
  public abstract class UIControllerABC : MonoBehaviour
  {
    protected virtual void Awake()
    {
      Registry.Registry.Register(RegistryType.UI, Registry.Registry.TypeKey(GetType()), this);
    }

    protected virtual void OnDestroy()
    {
      Registry.Registry.Unregister(RegistryType.UI, Registry.Registry.TypeKey(GetType()));
    }

    /// <summary>
    /// 여러 UIDocument가 하나의 PanelSettings를 공유하는 구조에서, 닫힌 오버레이가 하위
    /// sortingOrder UI(예: 인벤토리)의 클릭을 가로채지 못하도록 오버레이의 히트테스트를 제어한다.
    ///
    /// display가 아니라 rootVisualElement 서브트리 전체의 pickingMode를 토글하는 이유:
    /// UIDocument가 자체 라이프사이클에서 rootVisualElement.style.display를 강제로 Flex로 되돌리므로
    /// display만으로는 히트테스트를 막을 수 없기 때문이다. (pickingMode는 UIDocument가 관리하지 않는다.)
    ///
    /// 자세한 배경/규약은 Documents/working-guide/features/ui/overlay-uidocument-picking-guide.md 참고.
    /// </summary>
    protected static void SetDocumentRootInteractable(UIDocument document, bool visible)
    {
      var docRoot = document != null ? document.rootVisualElement : null;
      if (docRoot == null)
        return;

      docRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

      // 주의: UIDocument는 자체 라이프사이클에서 rootVisualElement의 style.display를
      // 강제로 Flex로 되돌리므로, display만으로는 히트테스트를 막을 수 없다.
      // 또한 컨트롤러가 Awake에서 캐시한 컨텐츠 root와 실제 렌더되는 요소가 다른 인스턴스로
      // 갈릴 수 있어(_root detached), 컨텐츠 root의 display:None이 누락될 수 있다.
      //
      // pickingMode는 UIDocument가 관리하지 않으므로, 이 값으로 확실히 히트테스트를 제어한다.
      // rootVisualElement 하위 전체를 대상으로:
      // - 숨길 때: Ignore로 내려, 전체화면 backdrop/패널이 포인터 이벤트를 흡수하지 못하게 한다.
      //   (sortingOrder가 낮은 인벤토리 등으로 클릭이 정상 전달된다.)
      // - 표시할 때: Position으로 복구하여 버튼/슬롯 등 내부 인터랙션을 활성화한다.
      //   (라벨/아이콘 등 비인터랙션 요소가 Position이 되어도 클릭 동작에는 영향이 없다.)
      SetSubtreePickingMode(docRoot, visible ? PickingMode.Position : PickingMode.Ignore);
    }

    /// <summary>
    /// 문서 렌더링 상태는 유지하면서 포인터 히트테스트만 전환한다.
    /// 닫힌 채팅처럼 비대화형 알림은 계속 표시해야 하지만, 높은 sortingOrder의
    /// 전체 화면 문서 루트가 아래 UI의 포인터 이벤트를 막아서는 안 되는 경우에 사용한다.
    /// </summary>
    protected static void SetDocumentRootPickingEnabled(UIDocument document, bool enabled)
    {
      var docRoot = document != null ? document.rootVisualElement : null;
      if (docRoot == null)
        return;

      docRoot.style.display = DisplayStyle.Flex;
      SetSubtreePickingMode(docRoot, enabled ? PickingMode.Position : PickingMode.Ignore);
    }

    private static void SetSubtreePickingMode(VisualElement root, PickingMode mode)
    {
      if (root == null)
        return;

      root.pickingMode = mode;
      for (int i = 0; i < root.childCount; i++)
        SetSubtreePickingMode(root[i], mode);
    }

    /// <summary>
    /// 오버레이 컨트롤러의 초기 숨김 처리가 UIDocument의 rootVisualElement 생성 타이밍보다
    /// 이르면(Awake 시점에 rootVisualElement가 아직 null인 경우), SetDocumentRootInteractable가
    /// 조기 return하여 rootVisualElement가 기본값(display:Flex, pickingMode:Position)으로 남는다.
    /// 이 경우 전체 화면을 덮는 오버레이 root가 sortingOrder가 낮은 UI(예: 인벤토리)의
    /// 포인터 이벤트를 계속 가로챈다.
    ///
    /// 이 코루틴은 rootVisualElement가 준비될 때까지 대기한 뒤 숨김 상태로 중립화한다.
    /// 각 오버레이 컨트롤러가 OnEnable 등에서 StartCoroutine으로 호출하면 된다.
    /// </summary>
    protected IEnumerator NeutralizeDocumentRootWhenReady(UIDocument document)
    {
      // rootVisualElement가 준비될 때까지 몇 프레임 대기.
      int guard = 0;
      while ((document == null || document.rootVisualElement == null) && guard < 10)
      {
        guard++;
        yield return null;
      }

      SetDocumentRootInteractable(document, false);
    }
  }
}
