using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Definitions;
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
    private ChatCommandCompletionService _completionService;
    private bool _chatPanelInputEventsBound;
    private readonly List<PendingMessage> _pendingMessages = new List<PendingMessage>();

    private readonly struct PendingMessage
    {
      public readonly string Message;
      public readonly bool ShowToastWhenHidden;

      public PendingMessage(string message, bool showToastWhenHidden)
      {
        Message = message;
        ShowToastWhenHidden = showToastWhenHidden;
      }
    }

    public bool IsOpen => _chatPanel != null && _chatPanel.IsOpen;

    public event Action<string> OnSubmitted;
    public event Action OnCancelled;
    public event Action OverlayPushed;
    public event Action OverlayPopped;

    private void Start()
    {
      _uiDocument = GetComponent<UIDocument>();
      if (_uiDocument == null)
        return;
      _uiDocument.sortingOrder = _sortingOrder;

      BindElement();
      ResolveCompletionService();
      FlushPendingMessages();
      HideImmediately();
    }

    private void BindElement()
    {
      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();
      if (_uiDocument == null)
        return;

      VisualElement root = _uiDocument.rootVisualElement;
      if (root == null)
        return;
      EnsureStyleSheet(root);
      _chatPanel = root.Q<ChatPanelElement>(_chatRootName);

      if (_chatPanel == null)
      {
        _chatPanel = new ChatPanelElement();
        EnsureStyleSheet(_chatPanel);
        root.Add(_chatPanel);
      }

      if (!_chatPanelInputEventsBound)
      {
        _chatPanel.InputKeyPressed += HandleInputKeyPressed;
        _chatPanelInputEventsBound = true;
      }
    }

    private void ResolveCompletionService()
    {
      if (_completionService != null)
        return;

      var commandService = FindFirstObjectByType<ChatCommandService>(FindObjectsInactive.Include);
      // The chat UI can start before the network service is spawned. Do not
      // permanently cache a completion service with a null command source;
      // HandleTabKey will retry this lookup after the service appears.
      if (commandService != null)
        _completionService = new ChatCommandCompletionService(commandService);
    }

    private void HandleInputKeyPressed(KeyCode keyCode)
    {
      // The actual Tab action is handled from PlayerController.Input so it is
      // consistent with Return/Escape/history handling. The UI event only
      // prevents focus traversal and tells the completion session when the
      // user started editing or used another navigation key.
      if (keyCode != KeyCode.Tab)
        _completionService?.ResetSession();
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
      if (_chatPanel == null)
      {
        _pendingMessages.Add(new PendingMessage(message, showToastWhenHidden));
        return;
      }

      FlushPendingMessages();
      _chatPanel.AppendMessage(message, showToastWhenHidden);
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

    public void OpenWithCommandStart()
    {
      Open();
      ResolveCompletionService();
      _completionService?.ResetSession();
      _chatPanel?.PushInput("/");
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
      _completionService?.ResetSession();
      OnSubmitted?.Invoke(text);
      Close();
    }

    public void HandleCancelKey()
    {
      if (!IsOpen)
        return;

      OnCancelled?.Invoke();
      _completionService?.ResetSession();
      Close();
    }

    /// <summary>
    /// Handles a Tab press while the chat input is focused. Returns silently
    /// when there are no candidates so Tab remains harmless in normal chat.
    /// </summary>
    public void HandleTabKey()
    {
      if (_chatPanel == null || !IsOpen)
        return;

      if (!_chatPanel.IsInputFocused)
        return;

      ResolveCompletionService();
      var result = _completionService?.HandleTabPress(
        _chatPanel.InputText,
        _chatPanel.CursorPosition);

      if (result.HasValue)
        _chatPanel.ApplyInput(result.Value.text, result.Value.cursorPos);
    }

    public void HandleHistoryPreviousKey()
    {
      if (!IsOpen)
        return;

      _chatPanel?.RecallPreviousInput();
      _completionService?.ResetSession();
    }

    public void HandleHistoryNextKey()
    {
      if (!IsOpen)
        return;

      _chatPanel?.RecallNextInput();
      _completionService?.ResetSession();
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
      SetDocumentRootPickingEnabled(_uiDocument, true);
      if (_uiDocument?.rootVisualElement != null)
        _uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
      _chatPanel?.SetOpen(true);
      _chatPanel?.FocusInput();
      _chatPanel?.ClearToasts();
    }

    private void HidePanel()
    {
      _completionService?.ResetSession();
      _chatPanel?.SetOpen(false);
      _chatPanel?.ClearInput();
      SetDocumentRootPickingEnabled(_uiDocument, false);
      if (_uiDocument?.rootVisualElement != null)
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void HideImmediately()
    {
      _completionService?.ResetSession();
      EnsurePanel();
      _chatPanel?.SetOpen(false);
      _chatPanel?.ClearInput();
      _chatPanel?.ClearToasts();
      SetDocumentRootPickingEnabled(_uiDocument, false);
      if (_uiDocument?.rootVisualElement != null)
        _uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    private void EnsurePanel()
    {
      if (_chatPanel != null)
        return;

      BindElement();
    }

    private void FlushPendingMessages()
    {
      if (_chatPanel == null || _pendingMessages.Count == 0)
        return;

      for (int i = 0; i < _pendingMessages.Count; i++)
      {
        var pending = _pendingMessages[i];
        _chatPanel.AppendMessage(pending.Message, pending.ShowToastWhenHidden);
      }
      _pendingMessages.Clear();
    }

    protected override void OnDestroy()
    {
      if (_chatPanelInputEventsBound && _chatPanel != null)
        _chatPanel.InputKeyPressed -= HandleInputKeyPressed;

      base.OnDestroy();
    }

  }
}
