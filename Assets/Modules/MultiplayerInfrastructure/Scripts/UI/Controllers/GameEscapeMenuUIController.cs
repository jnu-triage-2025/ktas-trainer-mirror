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
      _resumeButton = _document.rootVisualElement?.Q<Button>("resume-button");
      _titleButton = _document.rootVisualElement?.Q<Button>("title-button");

      if (_resumeButton != null)
        _resumeButton.clicked += HandleResumeClicked;
      else
        Debug.LogError("[GameMenuUI] Resume button not found in UXML.");

      if (_titleButton != null)
        _titleButton.clicked += HandleTitleClicked;
      else
        Debug.LogError("[GameMenuUI] Title button not found in UXML.");

      SetVisible(false);
    }

    private void OnDestroy()
    {
      if (_resumeButton != null) _resumeButton.clicked -= HandleResumeClicked;
      if (_titleButton != null) _titleButton.clicked -= HandleTitleClicked;
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

    private void HandleTitleClicked()
    {
      FishNetNetworkManagerInjection.Instance.StopClient();
      if (CurrentSessionPlayInfoRegistry.IsOpeningServer)
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
