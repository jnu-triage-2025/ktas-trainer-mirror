using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
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
    private Button _keyConfigButton;
    private Button _graphicsSettingsButton;
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

      _root = _document.rootVisualElement?.Q<VisualElement>("game-menu-root");
      _resumeButton           = _document.rootVisualElement?.Q<Button>("resume-button");
      _keyConfigButton        = _document.rootVisualElement?.Q<Button>("key-config-button");
      _graphicsSettingsButton = _document.rootVisualElement?.Q<Button>("graphics-settings-button");
      _titleButton            = _document.rootVisualElement?.Q<Button>("title-button");

      if (_resumeButton != null)
        _resumeButton.clicked += HandleResumeClicked;
      else
        Debug.LogError("[GameMenuUI] Resume button not found in UXML.");

      if (_keyConfigButton != null)
        _keyConfigButton.clicked += HandleKeyConfigClicked;
      else
        Debug.LogError("[GameMenuUI] Key config button not found in UXML.");

      if (_graphicsSettingsButton != null)
        _graphicsSettingsButton.clicked += HandleGraphicsSettingsClicked;
      else
        Debug.LogError("[GameMenuUI] Graphics settings button not found in UXML.");

      if (_titleButton != null)
        _titleButton.clicked += HandleTitleClicked;
      else
        Debug.LogError("[GameMenuUI] Title button not found in UXML.");

      SetVisible(false);
    }

    private void OnDestroy()
    {
      if (_resumeButton != null)           _resumeButton.clicked           -= HandleResumeClicked;
      if (_keyConfigButton != null)        _keyConfigButton.clicked        -= HandleKeyConfigClicked;
      if (_graphicsSettingsButton != null) _graphicsSettingsButton.clicked -= HandleGraphicsSettingsClicked;
      if (_titleButton != null)            _titleButton.clicked            -= HandleTitleClicked;
    }

    public void ShowMenu() => SetVisible(true);
    public void HideMenu() => SetVisible(false);
    public void ToggleMenu() => SetVisible(!_isVisible);

    private void SetVisible(bool visible)
    {
      _isVisible = visible;
      if (_root == null) return;

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

    private void HandleKeyConfigClicked()
    {
      var keyConfigController = Registry.Registry.Get<KeyConfigUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey(typeof(KeyConfigUIController))
      ) ?? FindFirstObjectByType<KeyConfigUIController>();

      if (keyConfigController == null)
      {
        Debug.LogWarning("[GameMenuUI] KeyConfigUIController를 레지스트리에서 찾을 수 없습니다.");
        return;
      }

      UIOverlayStack.Push(keyConfigController);
    }

    private void HandleGraphicsSettingsClicked()
    {
      var graphicsController = Registry.Registry.Get<GraphicsSettingsUIController>(
        RegistryType.UI,
        Registry.Registry.TypeKey(typeof(GraphicsSettingsUIController))
      ) ?? FindFirstObjectByType<GraphicsSettingsUIController>();

      if (graphicsController == null)
      {
        Debug.LogWarning("[GameMenuUI] GraphicsSettingsUIController를 레지스트리에서 찾을 수 없습니다.");
        return;
      }

      UIOverlayStack.Push(graphicsController);
    }

    private void HandleTitleClicked()
    {
      FishNetNetworkManagerInjection.Instance.StopClient();
      if (Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer))
        FishNetNetworkManagerInjection.Instance.StopServer();
      SceneManager.LoadScene(introSceneName);
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
