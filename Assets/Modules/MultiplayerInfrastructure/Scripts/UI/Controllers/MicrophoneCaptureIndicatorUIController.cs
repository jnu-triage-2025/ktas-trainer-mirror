using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 마이크 수집 표시 HUD 컨트롤러.
  ///
  /// 마이크 입력을 실제로 받아오는 동안에만 화면 우측 하단에 마이크 아이콘을 띄운다.
  /// 표시 여부는 마이크를 여닫는 쪽이 <see cref="SetCapturing"/> 로 알려 주며, 그 값을 정적으로
  /// 보관해 두었다가 각 인스턴스가 <c>Update</c> 에서 자기 요소에 반영한다. 덕분에 컨트롤러가
  /// 아직 준비되지 않았거나 씬이 바뀌어 인스턴스가 교체되어도 현재 상태가 그대로 이어진다.
  ///
  /// 비차단 HUD 이므로 <see cref="UIOverlayStack"/>/<see cref="IUIOverlay"/> 를 사용하지 않는다.
  /// 다만 문서 루트는 화면 전체를 덮으므로, 만들어지는 즉시 비상호작용으로 전환하여
  /// 정렬 순서가 더 낮은 핫바 등의 포인터 입력을 가로채지 않게 한다.
  ///
  /// UI 구성은 코드 전용이다: 빈 UIDocument(+ PanelSettings)에 <see cref="MicrophoneCaptureIndicatorElement"/> 를
  /// C# 으로 생성해 부착하며, 시각 스타일은 요소의 인라인 스타일에서 관리한다(uxml/uss/StyleSheet 불필요).
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public sealed class MicrophoneCaptureIndicatorUIController : UIControllerABC
  {
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.MicrophoneCaptureIndicatorSortOrder;

    private static bool _capturing;

    private UIDocument _uiDocument;
    private MicrophoneCaptureIndicatorElement _element;
    private bool _appliedVisible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration()
    {
      _capturing = false;
    }

    /// <summary>마이크 입력을 받아오는 중인지 여부입니다.</summary>
    public static bool IsCapturing => _capturing;

    /// <summary>
    /// 마이크 수집 표시를 켜거나 끕니다. 마이크를 실제로 열고 닫는 쪽에서 호출합니다.
    /// 표시할 컨트롤러가 아직 없어도 상태는 남으므로, 컨트롤러가 준비되는 대로 반영됩니다.
    /// </summary>
    public static void SetCapturing(bool capturing) => _capturing = capturing;

    protected override void Awake()
    {
      base.Awake();
      _uiDocument = GetComponent<UIDocument>();

      // 문서가 만들어지는 즉시 비상호작용으로 등록한다. 이 호출이 UIDocumentInteractionStateGuard
      // 를 붙여 매 프레임 정책을 다시 적용하므로, 루트가 다시 만들어지거나 자식이 추가된 뒤에도
      // 이 HUD 가 다른 UI 의 포인터 입력을 가로챌 틈이 생기지 않는다.
      SetDocumentRootPickingEnabled(_uiDocument, false);
    }

    private void Start()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      _uiDocument.sortingOrder = _sortingOrder;
      BindElement();
      SyncVisibility();
    }

    private void Update()
    {
      // 씬 전환 등으로 문서 루트가 다시 만들어지면 붙여 둔 요소가 패널에서 떨어진다.
      // 표시해야 하는 동안 아직 붙지 않았으면 다시 붙인다.
      if (_capturing && (_element == null || _element.panel == null))
        BindElement();

      SyncVisibility();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();
      if (_uiDocument == null)
        return;

      var root = _uiDocument.rootVisualElement;
      if (root == null)
        return;

      // 재바인딩 시 중복 부착을 막기 위해 기존 요소가 있으면 재사용한다.
      _element = root.Q<MicrophoneCaptureIndicatorElement>(MicrophoneCaptureIndicatorElement.RootName);
      if (_element == null)
      {
        _element = new MicrophoneCaptureIndicatorElement();
        root.Add(_element);
        _appliedVisible = false;
      }

      // 자식을 붙인 뒤에 적용해야 트리 전체가 포인터 입력을 흘려 보낸다.
      SetDocumentRootPickingEnabled(_uiDocument, false);
    }

    private void SyncVisibility()
    {
      if (_element == null)
        return;
      if (_appliedVisible == _capturing)
        return;

      _appliedVisible = _capturing;
      _element.SetVisibleState(_capturing);
    }
  }
}
