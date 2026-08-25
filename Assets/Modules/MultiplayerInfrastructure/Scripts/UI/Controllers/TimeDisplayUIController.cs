using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 시간 표시(스톱워치/카운트다운) HUD 컨트롤러.
  ///
  /// 화면 상단 중앙에 hh:mm:ss 를 표기한다. 값의 진행/방향/표시 여부는
  /// <see cref="ScenarioTimeState"/> 정적 저장소에서 매 프레임 읽어 렌더한다.
  /// 저장소는 <see cref="ScenarioTimeRelay"/> 가 서버 권한으로 동기화하므로
  /// 모든 클라이언트가 동일한 값을 본다.
  ///
  /// 비차단 HUD 이므로 <see cref="UIOverlayStack"/>/<see cref="IUIOverlay"/> 를 사용하지 않는다.
  ///
  /// UI 구성은 코드 전용이다: 빈 UIDocument(+ PanelSettings)에 <see cref="TimeDisplayElement"/> 를
  /// C# 으로 생성해 부착하며, 시각 스타일은 요소의 인라인 스타일에서 관리한다(uxml/uss/StyleSheet 불필요).
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class TimeDisplayUIController : UIControllerABC
  {
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.TimeDisplayHudSortOrder;

    private UIDocument _uiDocument;
    private TimeDisplayElement _element;
    private bool _hooked;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;
      BindElement();
      HookState();
      SyncVisibility();
    }

    private void OnEnable()
    {
      HookState();
    }

    private void OnDisable()
    {
      UnhookState();
    }

    protected override void OnDestroy()
    {
      UnhookState();
      base.OnDestroy();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      var root = _uiDocument.rootVisualElement;
      if (root == null)
        return;

      SetDocumentRootPickingEnabled(_uiDocument, false);

      // 코드 전용 구성: 요소를 C# 으로 생성해 부착한다.
      // (재바인딩 시 중복 부착을 막기 위해 기존 요소가 있으면 재사용한다.)
      _element = root.Q<TimeDisplayElement>(TimeDisplayElement.RootName);
      if (_element == null)
      {
        _element = new TimeDisplayElement();
        root.Add(_element);
      }
    }

    private void HookState()
    {
      if (_hooked)
        return;

      ScenarioTimeState.Changed += HandleStateChanged;
      _hooked = true;
    }

    private void UnhookState()
    {
      if (!_hooked)
        return;

      ScenarioTimeState.Changed -= HandleStateChanged;
      _hooked = false;
    }

    private void HandleStateChanged()
    {
      SyncVisibility();
    }

    private void SyncVisibility()
    {
      if (_element == null)
        BindElement();

      if (_element == null)
        return;

      // 상태 변화(생성/시작/표시 대상 교체 등) 시 캐시를 무효화해 다음 렌더에서 즉시 반영한다.
      _element.InvalidateRenderCache();
      _element.SetVisibleState(ScenarioTimeState.IsVisible);
    }

    private void Update()
    {
      if (_element == null || !ScenarioTimeState.IsVisible)
        return;

      _element.Render(
        ScenarioTimeState.ShownDirection(),
        ScenarioTimeState.ShownWholeSeconds(),
        ScenarioTimeState.ShownCountdownFinished());
    }
  }
}
