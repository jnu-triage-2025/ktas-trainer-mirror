using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.FishNetSupports;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System;
using MultiplayerInfrastructure.Definitions;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class GameEscapeMenuUIController : UIControllerABC, IUIOverlay
  {
    [SerializeField] private string introSceneName = "IntroScene";
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.EscapeMenuUISortOrder;

    private UIDocument _document;
    private VisualElement _root;
    private Button _resumeButton;
    private Button _settingsButton;
    private Button _titleButton;
    private bool _isVisible;

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    protected override void Awake()
    {
      base.Awake();
      _document = GetComponent<UIDocument>();
      if (_document == null)
      {
        Debug.LogError("[GameMenuUI] UIDocument is missing.");
        return;
      }
      _document.sortingOrder = _sortingOrder;

      BindViewToCurrentDocumentRoot();
      SetVisible(false);
    }

    private void OnEnable()
    {
      // UIDocument가 rootVisualElement를 (재)생성한 뒤 현재 root 기준으로 다시 바인딩한다.
      // (network-spawned 프리팹의 비활성/재활성으로 Awake 캐시가 detached되는 문제 방지.)
      BindViewToCurrentDocumentRoot();

      if (!_isVisible)
        StartCoroutine(NeutralizeDocumentRootWhenReady(_document));
    }

    protected override void OnDestroy()
    {
      base.OnDestroy();
      DetachButtonHandlers();
    }

    /// <summary>
    /// 현재 UIDocument.rootVisualElement 기준으로 컨텐츠 root와 버튼을 (재)바인딩한다.
    ///
    /// UIDocument는 자체 라이프사이클(OnEnable)에서 rootVisualElement를 재생성해 패널에 부착한다.
    /// 이 컨트롤러가 network-spawned 프리팹의 자식이면 스폰/재활성 과정에서 rootVisualElement가
    /// 교체되므로, Awake에서 한 번만 캐시하면 패널에 부착되지 않은 detached 트리를 참조하게 되어
    /// style을 바꿔도 화면에 반영되지 않는다(=Esc 메뉴가 뜨지 않는 증상). 따라서 매 OnEnable/표시
    /// 시점마다 현재 root 기준으로 재바인딩한다. (InventoryUIController와 동일한 규약.)
    /// </summary>
    private void BindViewToCurrentDocumentRoot()
    {
      if (_document == null)
        _document = GetComponent<UIDocument>();

      var docRoot = _document != null ? _document.rootVisualElement : null;
      if (docRoot == null)
        return;

      var newRoot = docRoot.Q<VisualElement>("game-menu-root");

      // 이미 현재 live root에 붙어 있는 동일한 컨텐츠 root라면 재바인딩 불필요.
      if (_root != null && _root == newRoot && _root.panel != null)
        return;

      // 이전(죽은) 트리의 버튼 구독 해제 후 재바인딩.
      DetachButtonHandlers();

      _root           = newRoot;
      _resumeButton   = docRoot.Q<Button>("resume-button");
      _settingsButton = docRoot.Q<Button>("settings-button");
      _titleButton    = docRoot.Q<Button>("title-button");

      if (_root == null)
        Debug.LogError("[GameMenuUI] 'game-menu-root'를 UXML에서 찾을 수 없습니다.");

      if (_resumeButton != null)
        _resumeButton.clicked += HandleResumeClicked;
      else
        Debug.LogError("[GameMenuUI] Resume button not found in UXML.");

      if (_settingsButton != null)
        _settingsButton.clicked += HandleSettingsClicked;
      else
        Debug.LogError("[GameMenuUI] Settings button not found in UXML.");

      if (_titleButton != null)
        _titleButton.clicked += HandleTitleClicked;
      else
        Debug.LogError("[GameMenuUI] Title button not found in UXML.");
    }

    private void DetachButtonHandlers()
    {
      if (_resumeButton != null)   _resumeButton.clicked   -= HandleResumeClicked;
      if (_settingsButton != null) _settingsButton.clicked -= HandleSettingsClicked;
      if (_titleButton != null)    _titleButton.clicked    -= HandleTitleClicked;
    }

    public void ShowMenu() => SetVisible(true);
    public void HideMenu() => SetVisible(false);
    public void ToggleMenu() => SetVisible(!_isVisible);

    private void SetVisible(bool visible)
    {
      _isVisible = visible;

      // 표시할 때는 캐시된 컨텐츠 root가 현재 live 패널에서 분리(detached)되었을 수 있으므로
      // 재바인딩을 시도한다(UIDocument rootVisualElement 재생성 대응).
      if (visible && (_root == null || _root.panel == null))
        BindViewToCurrentDocumentRoot();

      // rootVisualElement 중립화는 _root(자식) 유무와 무관하게 항상 수행한다.
      SetDocumentRootInteractable(_document, visible);

      if (_root == null)
        return;

      _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      _root.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    private void HandleResumeClicked()
    {
      // Ensure overlay lifecycle events fire so player controls resume properly
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
      else
        HideMenu();
    }

    private void HandleSettingsClicked()
    {
      var settingsController = Registry.Registry.Get<SettingsUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey(typeof(SettingsUIController))
      ) ?? FindFirstObjectByType<SettingsUIController>();

      if (settingsController == null)
      {
        Debug.LogWarning("[GameMenuUI] SettingsUIController를 레지스트리/씬에서 찾을 수 없습니다. 'MI: SettingsUI'가 씬에 존재/활성 상태인지 확인하세요.");
        return;
      }

      UIOverlayStack.Push(settingsController);
    }

    private void HandleTitleClicked()
    {
      var fishNetSupport = FishNetSupport.Instance ?? FindFirstObjectByType<FishNetSupport>();
      if (fishNetSupport != null)
        fishNetSupport.StopClient();

      if (Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer))
      {
        if (fishNetSupport != null)
          fishNetSupport.StopServer();
      }

      LoadingScreen.LoadSceneAsync(introSceneName);
    }

    public void OnOverlayPushed()
    {
      ShowMenu();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      HideMenu();
      OverlayPopped?.Invoke();
    }
  }
}
