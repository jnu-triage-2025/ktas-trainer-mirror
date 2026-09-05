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
      // 채팅 UI 는 네트워크 서비스가 스폰되기 전에 시작될 수 있다. 커맨드 소스가 null 인
      // 완성 서비스를 영구적으로 캐시하지 않는다. 서비스가 나타나면 HandleTabKey 가
      // 이 조회를 다시 시도한다.
      if (commandService != null)
        _completionService = new ChatCommandCompletionService(commandService);
    }

    private bool IsCompletionListOpen => _chatPanel != null && _chatPanel.IsCompletionListVisible;

    private void HandleInputKeyPressed(KeyCode keyCode)
    {
      // 실제 Tab 동작은 Return/Escape/히스토리 처리와 일관성을 유지하기 위해
      // PlayerController.Input 에서 처리한다. UI 이벤트는 포커스 이동을 막고,
      // 사용자가 편집을 시작했거나 다른 탐색 키를 눌렀을 때 완성 세션에 알리는 역할만 한다.
      if (keyCode == KeyCode.Tab)
        return;

      // 후보 목록이 열려 있는 동안 화살표, Enter, Escape 는 목록을 고르고 확정하고
      // 닫는 키이므로 여기서는 손대지 않는다. 실제 동작은 PlayerController.Input 이
      // 맡는다. 스페이스를 포함한 그 밖의 입력은 다음 구문을 쓰기 시작한 것으로
      // 보고 목록을 닫는다.
      if (IsCompletionListOpen && IsCompletionNavigationKey(keyCode))
        return;

      ResetCompletionSession();
    }

    private static bool IsCompletionNavigationKey(KeyCode keyCode)
    {
      return keyCode == KeyCode.UpArrow
        || keyCode == KeyCode.DownArrow
        || keyCode == KeyCode.Return
        || keyCode == KeyCode.KeypadEnter
        || keyCode == KeyCode.Escape;
    }

    /// <summary>
    /// Tab 순환 세션을 끝내고 후보 목록 오버레이도 함께 닫는다.
    /// </summary>
    private void ResetCompletionSession()
    {
      _completionService?.ResetSession();
      _chatPanel?.HideCompletionCandidates();
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
      ResetCompletionSession();
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

      // 후보 목록이 열려 있으면 Enter 는 전송이 아니라 고른 후보의 확정이다. 후보
      // 텍스트는 Tab 이나 화살표로 고를 때 이미 입력창에 채워져 있으므로 목록만 닫는다.
      if (IsCompletionListOpen)
      {
        ResetCompletionSession();
        return;
      }

      string text = _chatPanel.ConsumeInput();
      ResetCompletionSession();
      OnSubmitted?.Invoke(text);
      Close();
    }

    public void HandleCancelKey()
    {
      if (!IsOpen)
        return;

      // 후보 목록이 열려 있으면 Escape 는 채팅창이 아니라 목록을 닫는다. 입력창에
      // 채워진 텍스트는 그대로 남는다.
      if (IsCompletionListOpen)
      {
        ResetCompletionSession();
        return;
      }

      OnCancelled?.Invoke();
      ResetCompletionSession();
      Close();
    }

    /// <summary>
    /// 채팅 입력에 포커스가 있는 동안 Tab 누름을 처리한다. 후보가 없으면 조용히
    /// 반환하여 일반 채팅에서 Tab 이 아무 부작용도 일으키지 않도록 한다.
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

      if (!result.HasValue)
      {
        _chatPanel.HideCompletionCandidates();
        return;
      }

      _chatPanel.ApplyInput(result.Value.text, result.Value.cursorPos);
      // 후보가 하나뿐이면 더 넘길 곳이 없으므로 목록을 띄우지 않는다.
      var candidates = _completionService.ActiveCandidates;
      if (candidates == null || candidates.Count < 2)
      {
        _chatPanel.HideCompletionCandidates();
        return;
      }

      _chatPanel.ShowCompletionCandidates(
        candidates,
        _completionService.ActiveCandidateIndex,
        _completionService.ActiveTokenStart);
    }

    public void HandleHistoryPreviousKey()
    {
      if (!IsOpen)
        return;

      if (IsCompletionListOpen)
      {
        MoveCompletionSelection(-1);
        return;
      }

      _chatPanel?.RecallPreviousInput();
      ResetCompletionSession();
    }

    public void HandleHistoryNextKey()
    {
      if (!IsOpen)
        return;

      if (IsCompletionListOpen)
      {
        MoveCompletionSelection(1);
        return;
      }

      _chatPanel?.RecallNextInput();
      ResetCompletionSession();
    }

    /// <summary>
    /// 열려 있는 후보 목록에서 선택을 한 칸 옮기고, 옮겨진 후보를 입력창에 채운다.
    /// </summary>
    private void MoveCompletionSelection(int delta)
    {
      var result = _completionService?.MoveSelection(delta);
      if (!result.HasValue)
      {
        ResetCompletionSession();
        return;
      }

      _chatPanel.ApplyInput(result.Value.text, result.Value.cursorPos);
      _chatPanel.ShowCompletionCandidates(
        _completionService.ActiveCandidates,
        _completionService.ActiveCandidateIndex,
        _completionService.ActiveTokenStart);
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
      SetDocumentVisible(_uiDocument, true);
      _chatPanel?.SetOpen(true);
      _chatPanel?.FocusInput();
      _chatPanel?.ClearToasts();
    }

    private void HidePanel()
    {
      ResetCompletionSession();
      _chatPanel?.SetOpen(false);
      _chatPanel?.ClearInput();
      // 닫힌 채팅도 수신 메시지 토스트를 표시해야 한다. 문서는 남기되 전체
      // 트리의 입력 처리를 비활성화해 게임 조작을 가로채지 않도록 한다.
      SetDocumentRootPickingEnabled(_uiDocument, false);
    }

    private void HideImmediately()
    {
      ResetCompletionSession();
      EnsurePanel();
      _chatPanel?.SetOpen(false);
      _chatPanel?.ClearInput();
      _chatPanel?.ClearToasts();
      SetDocumentRootPickingEnabled(_uiDocument, false);
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
