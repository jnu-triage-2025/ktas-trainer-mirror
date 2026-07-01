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
    /// UIDocument의 최상위 rootVisualElement는 패널 전체를 덮고 기본 pickingMode가 Position이므로,
    /// 내부 컨텐츠 요소만 숨겨도(display:None) rootVisualElement가 포인터 이벤트를 흡수하여
    /// sortingOrder가 더 낮은 다른 UI(예: 인벤토리)의 클릭을 가로챈다.
    ///
    /// 오버레이가 숨김 상태일 때 이 헬퍼로 rootVisualElement 자체를 히트테스트/레이아웃에서 제외하면,
    /// 낮은 sortingOrder UI로 포인터 이벤트가 정상 전달된다. 표시 상태에서는 원래대로 복구한다.
    /// </summary>
    protected static void SetDocumentRootInteractable(UIDocument document, bool visible)
    {
      var docRoot = document != null ? document.rootVisualElement : null;
      if (docRoot == null)
        return;

      docRoot.pickingMode = visible ? PickingMode.Position : PickingMode.Ignore;
      docRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
  }
}

