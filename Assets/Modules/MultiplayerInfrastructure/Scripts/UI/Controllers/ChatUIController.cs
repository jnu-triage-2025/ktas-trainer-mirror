using System;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [RequireComponent(typeof(UIDocument))]
  public class ChatUIController : UIControllerABC, IUIOverlay
  {
    [SerializeField] private string _chatRootName = DefaultsChatControl.ChatRootName;
    [SerializeField] private float _sortingOrder = DefaultsUIDocument.ChatPanelUISortOrder;
    [SerializeField] private StyleSheet _chatStyleSheet;

    private UIDocument _uiDocument;
    private ChatPanelElement _chatPanel;

    public bool IsOpen => _chatPanel != null && _chatPanel.IsOpen;

    public event Action<string> OnSubmitted;
    public event Action OnCancelled;
    public event Action OverlayPushed;
    public event Action OverlayPopped;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      _uiDocument.sortingOrder = _sortingOrder;

      BindElement();
      HideImmediately();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      VisualElement root = _uiDocument.rootVisualElement;
      EnsureStyleSheet(root);
      _chatPanel = root.Q<ChatPanelElement>(_chatRootName);

      if (_chatPanel == null)
      {
        _chatPanel = new ChatPanelElement();
        EnsureStyleSheet(_chatPanel);
        root.Add(_chatPanel);
      }
    }

    private void EnsureStyleSheet(VisualElement ve)
    {
      if (ve == null)
        return;

      if (_chatStyleSheet != null && !ve.styleSheets.Contains(_chatStyleSheet))
        ve.styleSheets.Add(_chatStyleSheet);
    }

    public void AppendMessage(string message, bool showToastWhenHidden = true)
    {
      EnsurePanel();
      _chatPanel?.AppendMessage(message, showToastWhenHidden);
    }

    public void ClearLog()
    {
      _chatPanel?.ClearLog();
    }

    public void Open()
    {
      EnsurePanel();

      if (!UIOverlayStack.IsTop(this))
      {
        UIOverlayStack.Push(this);
        return;
      }

      ShowPanel();
    }

    public void Close()
    {
      if (UIOverlayStack.IsTop(this))
      {
        UIOverlayStack.Pop();
      }
      else
      {
        HidePanel();
      }
    }

    public void HandleSubmitKey()
    {
      if (_chatPanel == null || !IsOpen)
        return;

      string text = _chatPanel.ConsumeInput();
      OnSubmitted?.Invoke(text);
      Close();
    }

    public void HandleCancelKey()
    {
      if (!IsOpen)
        return;

      OnCancelled?.Invoke();
      Close();
    }

    public void OnOverlayPushed()
    {
      ShowPanel();
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      HidePanel();
      OverlayPopped?.Invoke();
    }

    private void ShowPanel()
    {
      EnsurePanel();
      _chatPanel?.SetOpen(true);
      _chatPanel?.FocusInput();
      _chatPanel?.ClearToasts();
      NotifyPlayerOverlay(expanding: true);
    }

    private void HidePanel()
    {
      _chatPanel?.SetOpen(false);
      _chatPanel?.ClearInput();
      NotifyPlayerOverlay(expanding: false);
    }

    private void HideImmediately()
    {
      EnsurePanel();
      _chatPanel?.SetOpen(false);
      _chatPanel?.ClearInput();
      _chatPanel?.ClearToasts();
    }

    private void EnsurePanel()
    {
      if (_chatPanel != null)
        return;

      BindElement();
    }

    private void NotifyPlayerOverlay(bool expanding)
    {
      var playerController = CurrentSessionPlayInfoRegistry.Get<PlayerController>();
      if (!playerController.IsUnityNull())
      {
        if (expanding) playerController.EnterUIOverlayMode();
        else playerController.ExitUIOverlayMode();
      }
    }
  }
}
