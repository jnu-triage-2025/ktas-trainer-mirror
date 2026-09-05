using System.Collections.Generic;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class ChatPanelElement : VisualElement
  {
    private const int DefaultMaxLogEntries = DefaultsChatControl.chatLogMaxLines;
    private const int MaxToastEntries = 5;
    private const float ToastVisibleSeconds = 3.5f;
    private const float ToastFadeSeconds = 0.75f;
    private const int MaxInputHistoryEntries = 100;
    /// <summary>후보 목록 오버레이가 한 번에 보여 주는 최대 행 수.</summary>
    private const int MaxCompletionRows = 8;

    private readonly Queue<VisualElement> _logEntries = new();
    private readonly List<ToastEntry> _toasts = new();
    private readonly List<string> _inputHistory = new();

    private ScrollView _logView;
    private VisualElement _logContent;
    private VisualElement _logViewportFrame;
    private VisualElement _scrollbarTrack;
    private VisualElement _scrollbarThumb;
    private ReusableVerticalScrollbar _reusableScrollbar;
    private TextField _inputField;
    private VisualElement _textInputElement;
    private TextElement _inputTextElement;
    private VisualElement _completionOverlay;
    private VisualElement _completionList;
    // resolvedStyle 은 다음 레이아웃까지 이전 값을 돌려주므로, 같은 프레임에 열고 곧바로
    // 묻는 키 처리 순서에서도 어긋나지 않도록 표시 상태를 따로 기억한다.
    private bool _completionListVisible;
    private VisualElement _toastPanel;
    private VisualElement _toastContainer;
    private VisualElement _panel;
    private IVisualElementScheduledItem _toastSchedule;
    private bool _isOpen;
    private int _maxLogEntries = DefaultMaxLogEntries;
    private int _historyCursor = -1;
    private string _historyDraft = string.Empty;
    private bool _isDraggingScrollbar;
    private int _scrollbarPointerId = -1;
    private float _scrollbarDragPointerY;
    private float _scrollbarDragOffsetY;
    private float _scrollOffsetY;
    private bool _scrollToBottomPending;
    private int _scrollToBottomRequest;

    /// <summary>
    /// 채팅 입력창에서 키보드 입력이 발생하면 일어난다. 컨트롤러는 이 이벤트로,
    /// 사용자가 텍스트를 편집하거나 다른 키로 이동할 때 Tab 순환 세션을 무효화한다.
    /// </summary>
    public event System.Action<KeyCode> InputKeyPressed;

    private static Color StyleColorBackground = new Color(0f, 0f, 0f, 0.82f);
    private static Color StyleColorText = Color.white;

    private const float styleLeft = 0f;
    private const float styleRight = 0f;
    private const float styleBottom = 0f;
    private const float stylePaddingLeft = 12f;
    private const float stylePaddingBottom = 12f;
    private const float stylePaddingRight = 12f;

    private class ToastEntry
    {
      public VisualElement Root;
      public float CreatedAt;
    }

    public bool IsOpen => _isOpen;
    public string InputText => _inputField?.text ?? string.Empty;
    public bool IsInputFocused
    {
      get
      {
        if (_inputField == null || _inputField.panel == null)
          return false;

        Focusable focused = _inputField.panel.focusController?.focusedElement;
        VisualElement focusedElement = focused as VisualElement;
        while (focusedElement != null)
        {
          if (focusedElement == _inputField)
            return true;
          focusedElement = focusedElement.parent;
        }

        return false;
      }
    }

    /// <summary>현재 입력창의 커서 위치.</summary>
    public int CursorPosition => _inputField?.cursorIndex ?? 0;

    /// <summary>
    /// 자동완성 후보 목록이 화면에 떠 있으면 true. 이 동안 화살표 키와 Enter 는
    /// 히스토리 탐색과 전송 대신 목록을 조작하는 키로 쓰인다.
    /// </summary>
    public bool IsCompletionListVisible => _completionListVisible;

    public ChatPanelElement()
    {
      name = DefaultsChatControl.ChatRootName;
      AddToClassList("chat-root");
      AddToClassList("collapsed");

      ApplyInlineStyles();

      BuildPanel();
      RegisterCallback<WheelEvent>(HandleLogWheel, TrickleDown.TrickleDown);

      _toastSchedule = schedule.Execute(UpdateToasts).Every(100);
      _toastSchedule.Pause();
    }

    private void BuildPanel()
    {
      _panel = new VisualElement();
      _panel.AddToClassList("chat-panel");
      _panel.style.display = DisplayStyle.None;
      _panel.style.flexDirection = FlexDirection.Column;
      _panel.style.flexGrow = 0;
      _panel.style.flexShrink = 0;
      // 고정 폭 루트를 항상 채워 패널 폭이 일정하게 유지되도록 한다.
      _panel.style.width = Length.Percent(100);
      _panel.style.alignItems = Align.Stretch;
      // 이 스크립팅 API 수준에서는 USS `gap` 과 IStyle.pointerEvents 를 사용할 수 없다.
      // 간격에는 자식의 margin 을, 포인터 동작에는 pickingMode 를 사용한다.
      _panel.pickingMode = PickingMode.Position;
      Add(_panel);

      _logViewportFrame = new VisualElement
      {
        name = "chat-log-frame",
        pickingMode = PickingMode.Position
      };
      _logViewportFrame.AddToClassList("chat-log-frame");
      // 중요한 뷰포트 제약은 의도적으로 인라인으로 지정한다. 외부 USS 가 없거나 늦게
      // 임포트되면 콘텐츠 크기에 맞춘 프레임이 계속 커져서 ScrollView 가 스크롤할
      // 오버플로가 사라진다.
      _logViewportFrame.style.position = Position.Relative;
      _logViewportFrame.style.width = Length.Percent(100);
      _logViewportFrame.style.height = 200;
      _logViewportFrame.style.minHeight = 200;
      _logViewportFrame.style.maxHeight = 200;
      _logViewportFrame.style.flexGrow = 0;
      _logViewportFrame.style.flexShrink = 0;
      _logViewportFrame.style.marginBottom = 8;
      _logViewportFrame.style.overflow = Overflow.Hidden;
      _panel.Add(_logViewportFrame);

      _logView = new ScrollView(ScrollViewMode.Vertical)
      {
        name = DefaultsChatControl.ChatLogName,
        // 채팅 전용 스크롤바를 ScrollView 옆에 직접 그린다. Unity 버전마다 다른
        // 내부 USS 클래스 이름에 의존하지 않기 위해서다.
        verticalScrollerVisibility = ScrollerVisibility.Hidden,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden,
        // 사용자가 마우스 휠로 로그를 스크롤하고 스크롤바를 드래그할 수 있도록
        // 포인터 이벤트를 받아야 한다. 여기서 PickingMode.Ignore 를 쓰면 모든 스크롤
        // 상호작용이 막힌다("히스토리를 스크롤할 수 없음" 버그 보고).
        pickingMode = PickingMode.Position
      };
      // 채팅 기록을 탐색할 때 마우스 휠 스크롤을 조금 빠르게 한다.
      _logView.mouseWheelScrollSize = 40f;
      // 뷰포트와 콘텐츠도 포인터/휠 이벤트를 받아서, 로그 영역 어디에 올려두어도
      // 휠 스크롤이 동작하게 한다.
      _logView.contentViewport.pickingMode = PickingMode.Position;
      _logView.contentContainer.pickingMode = PickingMode.Position;
      _logView.contentViewport.RegisterCallback<GeometryChangedEvent>(_ => RefreshScrollLayout());
      // 네이티브 휠/키보드 스크롤과 커스텀 썸은 하나의 오프셋을 공유한다.
      _logView.verticalScroller.valueChanged += HandleNativeScrollValueChanged;
      _logView.AddToClassList("chat-log");
      _logView.style.position = Position.Absolute;
      _logView.style.left = 0;
      _logView.style.right = 0;
      _logView.style.top = 0;
      _logView.style.bottom = 0;
      _logView.style.width = Length.Percent(100);
      _logView.style.height = Length.Percent(100);
      _logView.style.minHeight = 0;
      _logView.style.maxHeight = 200;
      _logView.style.flexGrow = 0;
      _logView.style.flexShrink = 0;
      _logView.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
      _logView.style.paddingTop = 8;
      _logView.style.paddingBottom = 8;
      _logView.style.paddingLeft = 10;
      _logView.style.paddingRight = 24;
      _logView.style.borderTopWidth = 1;
      _logView.style.borderBottomWidth = 1;
      _logView.style.borderLeftWidth = 1;
      _logView.style.borderRightWidth = 1;
      _logView.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
      _logView.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
      _logView.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
      _logView.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
      _logView.style.borderTopLeftRadius = 6;
      _logView.style.borderTopRightRadius = 6;
      _logView.style.borderBottomLeftRadius = 6;
      _logView.style.borderBottomRightRadius = 6;
      _logView.style.color = StyleColorText;
      _logViewportFrame.Add(_logView);

      _logContent = new VisualElement
      {
        name = "chat-log-content",
        pickingMode = PickingMode.Position
      };
      _logContent.AddToClassList("chat-log-content");
      _logContent.style.width = Length.Percent(100);
      _logContent.style.flexDirection = FlexDirection.Column;
      _logContent.style.flexGrow = 0;
      _logContent.style.flexShrink = 0;
      _logContent.RegisterCallback<GeometryChangedEvent>(_ => RefreshScrollLayout());
      _logView.contentContainer.Add(_logContent);

      // 채팅과 설정 화면이 동일한 모듈형 스크롤바를 사용한다.
      var scrollbar = new ReusableVerticalScrollbar { name = "chat-scrollbar-track" };
      scrollbar.AddToClassList("chat-scrollbar-track");
      scrollbar.Thumb.name = "chat-scrollbar-thumb";
      scrollbar.Thumb.AddToClassList("chat-scrollbar-thumb");
      scrollbar.ScrollNormalizedRequested += normalized =>
        SetScrollOffset(normalized * GetMaximumScrollOffset());
      _scrollbarTrack = scrollbar;
      _scrollbarThumb = scrollbar.Thumb;
      _reusableScrollbar = scrollbar;
      _logViewportFrame.Add(scrollbar);

      var inputRow = new VisualElement();
      inputRow.AddToClassList("chat-input-row");
      inputRow.style.width = Length.Percent(100);
      // 후보 목록은 입력 행을 기준으로 절대 배치하므로 기준 위치가 필요하다.
      inputRow.style.position = Position.Relative;
      _panel.Add(inputRow);

      var inputBg = new VisualElement();
      inputBg.AddToClassList("chat-input-bg");
      inputBg.style.width = Length.Percent(100);
      inputBg.style.backgroundColor = StyleColorBackground;
      inputBg.style.paddingTop = 8;
      inputBg.style.paddingBottom = 8;
      inputBg.style.paddingLeft = 10;
      inputBg.style.paddingRight = 10;
      inputBg.style.borderTopWidth = 1;
      inputBg.style.borderBottomWidth = 1;
      inputBg.style.borderLeftWidth = 1;
      inputBg.style.borderRightWidth = 1;
      inputBg.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
      inputBg.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
      inputBg.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
      inputBg.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
      inputBg.style.borderTopLeftRadius = 6;
      inputBg.style.borderTopRightRadius = 6;
      inputBg.style.borderBottomLeftRadius = 6;
      inputBg.style.borderBottomRightRadius = 6;
      inputRow.Add(inputBg);

      _inputField = new TextField
      {
        name = DefaultsChatControl.ChatInputName,
        multiline = false,
        isDelayed = false,
        maxLength = 256,
        pickingMode = PickingMode.Position
      };
      _inputField.AddToClassList("chat-input");
      _inputField.style.color = StyleColorText;

      _textInputElement = _inputField.Q("unity-text-input");
      // 후보 목록을 캐럿이 있는 열에 맞추려면 입력창과 같은 글꼴로 폭을 재야 한다.
      _inputTextElement = _textInputElement as TextElement ?? _textInputElement?.Q<TextElement>();
      _textInputElement.style.backgroundColor = Color.clear;
      _textInputElement.style.borderTopWidth = 0;
      _textInputElement.style.borderBottomWidth = 0;
      _textInputElement.style.borderLeftWidth = 0;
      _textInputElement.style.borderRightWidth = 0;

      inputBg.Add(_inputField);
      // 트리클다운 단계에서 받아야 텍스트 요소가 키를 편집에 쓰기 전에 가로챌 수 있다.
      // 후보 목록이 열린 동안 화살표와 Enter 가 캐럿을 옮기거나 줄을 넘기지 않게
      // 막는 데 필요하다.
      _inputField.RegisterCallback<KeyDownEvent>(HandleInputKeyDown, TrickleDown.TrickleDown);

      // 입력 행 위에 겹쳐 그리므로 입력창을 만든 뒤에 마지막 자식으로 붙인다.
      BuildCompletionOverlay(inputRow);

      _toastPanel = new VisualElement
      {
        name = "chat-toast-panel",
        pickingMode = PickingMode.Ignore
      };
      _toastPanel.AddToClassList("chat-toast-panel");
      _toastPanel.style.position = Position.Absolute;
      _toastPanel.style.left = styleLeft;
      _toastPanel.style.bottom = styleBottom;
      _toastPanel.style.right = styleRight;
      _toastPanel.style.paddingLeft = stylePaddingLeft;
      _toastPanel.style.paddingBottom = stylePaddingBottom;
      _toastPanel.style.paddingRight = stylePaddingRight;
      _toastPanel.style.width = Length.Percent(100);
      _toastPanel.style.flexDirection = FlexDirection.Column;
      _toastPanel.style.alignItems = Align.FlexStart;
      _toastPanel.style.justifyContent = Justify.FlexEnd;

      _toastContainer = new VisualElement
      {
        name = "chat-toast-container",
        pickingMode = PickingMode.Ignore
      };
      _toastContainer.AddToClassList("chat-toast-container");
      _toastContainer.style.display = DisplayStyle.None;
      _toastContainer.style.visibility = Visibility.Hidden;
      _toastContainer.style.width = Length.Percent(100);
      _toastContainer.style.flexDirection = FlexDirection.Column;
      _toastContainer.style.alignItems = Align.FlexStart;
      _toastContainer.style.backgroundColor = StyleColorBackground;
      _toastContainer.style.color = StyleColorText;

      _toastPanel.Add(_toastContainer);
      Add(_toastPanel);
    }

    private void HandleLogWheel(WheelEvent evt)
    {
      if (!_isOpen || _logView == null || _logViewportFrame == null ||
          !_logViewportFrame.worldBound.Contains(evt.mousePosition))
        return;

      SetScrollOffset(_scrollOffsetY + evt.delta.y * _logView.mouseWheelScrollSize);
      evt.StopImmediatePropagation();
    }

    private void HandleScrollbarTrackPointerDown(PointerDownEvent evt)
    {
      if (evt.button != 0 || evt.target == _scrollbarThumb)
        return;

      float thumbHeight = _scrollbarThumb.resolvedStyle.height;
      float requestedTop = evt.localPosition.y - thumbHeight * 0.5f;
      SetScrollFromThumbTop(requestedTop);
      evt.StopImmediatePropagation();
    }

    private void HandleScrollbarPointerDown(PointerDownEvent evt)
    {
      if (evt.button != 0)
        return;

      _isDraggingScrollbar = true;
      _scrollbarPointerId = evt.pointerId;
      _scrollbarDragPointerY = evt.position.y;
      _scrollbarDragOffsetY = _scrollbarThumb.resolvedStyle.top;
      _scrollbarThumb.CapturePointer(evt.pointerId);
      _scrollbarThumb.AddToClassList("dragging");
      evt.StopImmediatePropagation();
    }

    private void HandleScrollbarPointerMove(PointerMoveEvent evt)
    {
      if (!_isDraggingScrollbar || evt.pointerId != _scrollbarPointerId)
        return;

      SetScrollFromThumbTop(_scrollbarDragOffsetY + evt.position.y - _scrollbarDragPointerY);
      evt.StopImmediatePropagation();
    }

    private void HandleScrollbarPointerUp(PointerUpEvent evt)
    {
      if (!_isDraggingScrollbar || evt.pointerId != _scrollbarPointerId)
        return;

      if (_scrollbarThumb.HasPointerCapture(evt.pointerId))
        _scrollbarThumb.ReleasePointer(evt.pointerId);
      EndScrollbarDrag();
      evt.StopImmediatePropagation();
    }

    private void EndScrollbarDrag()
    {
      _isDraggingScrollbar = false;
      _scrollbarPointerId = -1;
      _scrollbarThumb?.RemoveFromClassList("dragging");
    }

    private void SetScrollFromThumbTop(float requestedTop)
    {
      if (_scrollbarTrack == null || _scrollbarThumb == null || _logView == null)
        return;

      float travel = Mathf.Max(0f,
        _scrollbarTrack.contentRect.height - _scrollbarThumb.resolvedStyle.height);
      float normalized = travel > Mathf.Epsilon
        ? Mathf.Clamp01(requestedTop / travel)
        : 0f;
      SetScrollOffset(normalized * GetMaximumScrollOffset());
    }

    private void SetScrollOffset(float offset)
    {
      if (_logView == null)
        return;

      _scrollOffsetY = Mathf.Clamp(offset, 0f, GetMaximumScrollOffset());
      Vector2 nativeOffset = _logView.scrollOffset;
      if (!Mathf.Approximately(nativeOffset.y, _scrollOffsetY))
        _logView.scrollOffset = new Vector2(nativeOffset.x, _scrollOffsetY);
      UpdateScrollbar();
    }

    private void HandleNativeScrollValueChanged(float value)
    {
      _scrollOffsetY = Mathf.Clamp(value, 0f, GetMaximumScrollOffset());
      UpdateScrollbar();
    }

    private float GetMaximumScrollOffset()
    {
      if (_logView == null || _logContent == null)
        return 0f;

      return Mathf.Max(0f, GetLogContentHeight() - _logView.contentViewport.layout.height);
    }

    private float GetLogContentHeight()
    {
      if (_logContent == null)
        return 0f;

      float height = 0f;
      for (int i = 0; i < _logContent.childCount; i++)
      {
        VisualElement child = _logContent[i];
        if (child == null)
          continue;

        Rect childLayout = child.layout;
        if (!float.IsNaN(childLayout.yMax))
          height = Mathf.Max(height, childLayout.yMax);
      }

      return Mathf.Max(height, _logContent.layout.height);
    }

    private void RefreshScrollLayout()
    {
      if (_scrollToBottomPending)
      {
        ApplyScrollToBottom();
        return;
      }

      // ScrollView 가 콘텐츠 변환을 소유한다. 스타일/레이아웃 재계산 뒤에는 그
      // 기준 오프셋에 맞춰 커스텀 썸을 동기화한다.
      SetScrollOffset(_logView != null ? _logView.scrollOffset.y : _scrollOffsetY);
    }

    private void UpdateScrollbar()
    {
      if (_logView == null || _reusableScrollbar == null)
        return;

      float viewportHeight = _logView.contentViewport.layout.height;
      float contentHeight = GetLogContentHeight();
      if (viewportHeight <= 0f || contentHeight <= 0f)
        return;

      _reusableScrollbar.SetMetrics(viewportHeight, contentHeight, _scrollOffsetY);
    }

    private void ApplyInlineStyles()
    {
      // 루트의 레이아웃/폭은 ChatPanelUI.uss 의 정의를 따른다
      // (.chat-root 는 고정 --panel-width 사용). 여기서는 의도적으로 인라인 폭을
      // 지정하지 않는다. 인라인 폭이 USS 규칙을 덮어쓰면 flex-start 정렬에서
      // 패널이 콘텐츠 길이에 따라 크기가 변하게 된다.
      style.position = Position.Absolute;
      style.left = styleLeft;
      style.right = styleRight;
      style.bottom = styleBottom;
      style.paddingLeft = stylePaddingLeft;
      style.paddingBottom = stylePaddingBottom;
      style.paddingRight = stylePaddingRight;
      style.flexDirection = FlexDirection.Column;
      // 자식(패널/로그/입력창)을 루트 전체 폭으로 늘려서, 메시지나 입력 텍스트 길이와
      // 무관하게 채팅 폭이 일정하게 유지되도록 한다.
      style.alignItems = Align.Stretch;
      style.justifyContent = Justify.FlexEnd;
      style.flexGrow = 0;
      style.flexShrink = 0;
    }

    public void SetMaxLogEntries(int maxEntries)
    {
      _maxLogEntries = Mathf.Max(1, maxEntries);
      TrimLogIfNeeded();
    }

    public void SetOpen(bool open)
    {
      if (_isOpen == open)
        return;

      _isOpen = open;
      if (open)
      {
        RemoveFromClassList("collapsed");
        AddToClassList("expanded");
        if (_panel != null)
          _panel.style.display = DisplayStyle.Flex;
        ClearToasts();
        // 패널을 (다시) 열 때는 가장 최근 메시지를 보여준다.
        ScrollToBottom();
      }
      else
      {
        RemoveFromClassList("expanded");
        AddToClassList("collapsed");
        if (_panel != null)
          _panel.style.display = DisplayStyle.None;
        // 패널이 닫혀 있는 동안에는 새 토스트가 도착할 때까지 숨긴 채 둔다.
        if (_toastContainer != null)
        {
          _toastContainer.style.display = DisplayStyle.None;
          _toastContainer.style.visibility = Visibility.Hidden;
        }
      }
    }

    public void FocusInput()
    {
      if (_inputField == null)
        return;

      _inputField.Focus();
      _inputField.cursorIndex = _inputField.text.Length;
      _inputField.selectIndex = _inputField.cursorIndex;
    }

    // ── 자동완성 후보 목록 오버레이 ─────────────────────────────────────

    /// <summary>
    /// 후보 목록을 담을 컨테이너를 입력 행 위쪽에 절대 배치로 만든다. 목록이 로그
    /// 영역을 가리면서 입력 줄 바로 위에 겹쳐 보이도록 하기 위해서다.
    /// </summary>
    private void BuildCompletionOverlay(VisualElement inputRow)
    {
      _completionOverlay = new VisualElement
      {
        name = "chat-completion-overlay",
        pickingMode = PickingMode.Ignore
      };
      _completionOverlay.AddToClassList("chat-completion-overlay");
      _completionOverlay.style.position = Position.Absolute;
      // 입력 행의 위쪽 모서리에 바닥을 맞춰 캐럿이 있는 줄 바로 위에 놓는다.
      _completionOverlay.style.bottom = Length.Percent(100);
      _completionOverlay.style.left = 0;
      _completionOverlay.style.display = DisplayStyle.None;
      _completionOverlay.style.flexDirection = FlexDirection.Column;
      _completionOverlay.style.alignItems = Align.FlexStart;
      _completionOverlay.style.marginBottom = 2;
      inputRow.Add(_completionOverlay);

      _completionList = new VisualElement
      {
        name = "chat-completion-list",
        pickingMode = PickingMode.Ignore
      };
      _completionList.AddToClassList("chat-completion-list");
      _completionList.style.flexDirection = FlexDirection.Column;
      _completionList.style.backgroundColor = StyleColorBackground;
      _completionList.style.paddingTop = 4;
      _completionList.style.paddingBottom = 4;
      _completionList.style.paddingLeft = 8;
      _completionList.style.paddingRight = 8;
      _completionList.style.borderTopWidth = 1;
      _completionList.style.borderBottomWidth = 1;
      _completionList.style.borderLeftWidth = 1;
      _completionList.style.borderRightWidth = 1;
      _completionList.style.borderTopColor = new Color(1f, 1f, 1f, 0.08f);
      _completionList.style.borderBottomColor = new Color(1f, 1f, 1f, 0.08f);
      _completionList.style.borderLeftColor = new Color(1f, 1f, 1f, 0.08f);
      _completionList.style.borderRightColor = new Color(1f, 1f, 1f, 0.08f);
      _completionList.style.borderTopLeftRadius = 6;
      _completionList.style.borderTopRightRadius = 6;
      _completionList.style.borderBottomLeftRadius = 6;
      _completionList.style.borderBottomRightRadius = 6;
      _completionOverlay.Add(_completionList);
    }

    /// <summary>
    /// 자동완성 후보를 입력창 위에 겹쳐 표시한다. 목록은 <paramref name="anchorColumn"/>
    /// 이 가리키는 글자 위치에 맞춰 가로로 정렬되므로, 완성 중인 낱말 바로 위에 놓인다.
    /// </summary>
    /// <param name="candidates">표시할 후보 목록.</param>
    /// <param name="selectedIndex">현재 입력창에 채워져 있는 후보의 인덱스.</param>
    /// <param name="anchorColumn">완성 중인 토큰이 시작하는 글자 위치.</param>
    public void ShowCompletionCandidates(
      IReadOnlyList<ChatCommandCompletionService.CompletionCandidate> candidates,
      int selectedIndex,
      int anchorColumn)
    {
      if (_completionOverlay == null || _completionList == null)
        return;

      if (candidates == null || candidates.Count == 0)
      {
        HideCompletionCandidates();
        return;
      }

      _completionList.Clear();

      // 후보가 많으면 선택된 항목이 항상 보이도록 창을 밀어 가며 잘라 낸다.
      int visibleCount = Mathf.Min(candidates.Count, MaxCompletionRows);
      int first = Mathf.Clamp(selectedIndex - visibleCount + 1, 0, candidates.Count - visibleCount);
      for (int i = first; i < first + visibleCount; i++)
        _completionList.Add(BuildCompletionRow(candidates[i], i == selectedIndex));

      if (candidates.Count > visibleCount)
      {
        var more = new Label($"... {candidates.Count - visibleCount} more");
        more.AddToClassList("chat-completion-more");
        more.style.color = new Color(1f, 1f, 1f, 0.45f);
        more.style.fontSize = 11;
        _completionList.Add(more);
      }

      _completionOverlay.style.left = MeasureColumnOffset(anchorColumn);
      _completionOverlay.style.display = DisplayStyle.Flex;
      _completionListVisible = true;
    }

    /// <summary>후보 목록 오버레이를 숨긴다.</summary>
    public void HideCompletionCandidates()
    {
      if (_completionOverlay == null)
        return;

      _completionList?.Clear();
      _completionOverlay.style.display = DisplayStyle.None;
      _completionListVisible = false;
    }

    private VisualElement BuildCompletionRow(
      ChatCommandCompletionService.CompletionCandidate candidate,
      bool isSelected)
    {
      var row = new VisualElement { pickingMode = PickingMode.Ignore };
      row.AddToClassList("chat-completion-row");
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.paddingLeft = 4;
      row.style.paddingRight = 4;
      if (isSelected)
      {
        row.AddToClassList("chat-completion-row--selected");
        row.style.backgroundColor = new Color(0.35f, 0.82f, 0.5f, 0.28f);
      }

      var label = new Label(candidate.Text);
      label.AddToClassList("chat-completion-row__label");
      label.style.color = isSelected ? new Color(0.85f, 1f, 0.9f) : StyleColorText;
      label.style.fontSize = 12;
      label.style.marginRight = 10;
      row.Add(label);

      if (!string.IsNullOrEmpty(candidate.Description))
      {
        var description = new Label(candidate.Description);
        description.AddToClassList("chat-completion-row__description");
        description.style.color = new Color(1f, 1f, 1f, 0.45f);
        description.style.fontSize = 11;
        row.Add(description);
      }

      return row;
    }

    /// <summary>
    /// 입력 행의 왼쪽 끝을 기준으로, 입력 문자열의 <paramref name="column"/> 번째
    /// 글자가 그려지는 가로 위치를 구한다. 글꼴 폭을 잴 수 없으면 입력창의 글자
    /// 시작 위치를 그대로 사용한다.
    /// </summary>
    private float MeasureColumnOffset(int column)
    {
      float textOrigin = 0f;
      if (_textInputElement != null && _completionOverlay?.parent != null)
      {
        Rect input = _textInputElement.worldBound;
        Rect row = _completionOverlay.parent.worldBound;
        if (!float.IsNaN(input.x) && !float.IsNaN(row.x))
          textOrigin = input.x - row.x;
      }

      string text = _inputField?.text ?? string.Empty;
      column = Mathf.Clamp(column, 0, text.Length);
      if (column == 0 || _inputTextElement == null)
        return Mathf.Max(0f, textOrigin);

      float width = _inputTextElement
        .MeasureTextSize(text.Substring(0, column), 0f, MeasureMode.Undefined, 0f, MeasureMode.Undefined)
        .x;
      if (float.IsNaN(width))
        width = 0f;

      return Mathf.Max(0f, textOrigin + width);
    }

    public void ApplyInput(string text, int cursorPos)
    {
      if (_inputField == null)
        return;

      _inputField.SetValueWithoutNotify(text ?? string.Empty);
      FocusInput();
      int clampedCursor = Mathf.Clamp(cursorPos, 0, _inputField.text.Length);
      _inputField.cursorIndex = clampedCursor;
      _inputField.selectIndex = clampedCursor;
    }

    public string ConsumeInput()
    {
      if (_inputField == null)
        return string.Empty;

      string text = _inputField.text;
      AddInputHistory(text);
      _inputField.SetValueWithoutNotify(string.Empty);
      ResetHistoryCursor();
      return text;
    }

    public void ClearInput()
    {
      _inputField?.SetValueWithoutNotify(string.Empty);
      ResetHistoryCursor();
      HideCompletionCandidates();
    }

    public void RecallPreviousInput()
    {
      if (_inputField == null || _inputHistory.Count == 0)
        return;

      if (_historyCursor < 0)
      {
        _historyDraft = _inputField.text ?? string.Empty;
        _historyCursor = _inputHistory.Count - 1;
      }
      else if (_historyCursor > 0)
      {
        _historyCursor--;
      }

      _inputField.SetValueWithoutNotify(_inputHistory[_historyCursor]);
      FocusInput();
    }

    public void RecallNextInput()
    {
      if (_inputField == null || _inputHistory.Count == 0 || _historyCursor < 0)
        return;

      if (_historyCursor < _inputHistory.Count - 1)
      {
        _historyCursor++;
        _inputField.SetValueWithoutNotify(_inputHistory[_historyCursor]);
      }
      else
      {
        _historyCursor = -1;
        _inputField.SetValueWithoutNotify(_historyDraft);
      }

      FocusInput();
    }

    public void AppendMessage(string message, bool showToastWhenHidden = true)
    {
      if (string.IsNullOrEmpty(message) || _logView == null)
        return;

      var entry = new Label
      {
        text = message,
        pickingMode = PickingMode.Ignore
      };
      entry.AddToClassList("chat-log__entry");
      entry.style.whiteSpace = WhiteSpace.Normal;
      // flexShrink 는 반드시 0 이어야 한다. 수직 ScrollView 안에서 줄어들 수 있는
      // 항목은 콘텐츠가 뷰포트에 맞게 압축되게 하므로, 콘텐츠가 뷰포트 높이를 넘지
      // 않아 로그를 스크롤할 수 없게 된다("히스토리를 스크롤할 수 없음" 버그 보고).
      entry.style.flexShrink = 0;
      entry.style.width = Length.Percent(100);

      // 새 항목을 추가하기 전에 사용자가 현재 (거의) 아래에 붙어 있는지 먼저 확인한다.
      // 이미 아래에 있을 때만 자동 스크롤하여, 기록을 읽으려고 위로 스크롤한 상태가
      // 방해받지 않도록 한다.
      bool stickToBottom = IsScrolledToBottom();

      _logContent.Add(entry);
      _logEntries.Enqueue(entry);

      TrimLogIfNeeded();

      if (stickToBottom)
        ScrollToBottom();

      if (!_isOpen && showToastWhenHidden)
        ShowToast(message);
    }

    /// <summary>
    /// 수직 스크롤러가 아래(또는 아래 아주 근처)에 있거나, 콘텐츠가 스크롤할 만큼
    /// 높지 않을 때 true 이다. 새 메시지를 자동으로 스크롤해 보여줄지 결정하는 데
    /// 사용한다.
    /// </summary>
    private bool IsScrolledToBottom()
    {
      if (_logView == null)
        return true;

      float range = GetMaximumScrollOffset();
      // 아직 스크롤 범위가 없다. 아래로 취급하여 첫 메시지가 보이게 한다.
      if (range <= Mathf.Epsilon)
        return true;

      // 작은 여유를 두어 사소한 오프셋도 "아래에 있음"으로 취급한다.
      const float bottomThreshold = 4f;
      return _scrollOffsetY >= range - bottomThreshold;
    }

    public void ClearLog()
    {
      while (_logEntries.Count > 0)
      {
        var entry = _logEntries.Dequeue();
        entry?.RemoveFromHierarchy();
      }

      _logContent?.Clear();
      SetScrollOffset(0f);
    }

    private void TrimLogIfNeeded()
    {
      while (_logEntries.Count > _maxLogEntries)
      {
        var oldest = _logEntries.Dequeue();
        oldest?.RemoveFromHierarchy();
      }
    }

    private void ScrollToBottom()
    {
      if (_logView == null)
        return;

      _scrollToBottomPending = true;
      int request = ++_scrollToBottomRequest;
      ScheduleScrollToBottomPass(request, 4);
    }

    private void ScheduleScrollToBottomPass(int request, int passesRemaining)
    {
      _logView.schedule.Execute(() =>
      {
        if (request != _scrollToBottomRequest || _logView == null)
          return;

        ApplyScrollToBottom();

        if (passesRemaining > 1)
        {
          ScheduleScrollToBottomPass(request, passesRemaining - 1);
          return;
        }

        _scrollToBottomPending = false;
      }).ExecuteLater(16);
    }

    private void ApplyScrollToBottom()
    {
      if (_logView == null || _logContent == null || _logContent.childCount == 0)
        return;

      // ScrollTo 는 ScrollView 의 확정된 레이아웃을 사용한다. 명시적 오프셋은 숨겨진
      // 패널이 보이게 되어 스크롤 범위가 다시 계산되는 몇 프레임 동안 커스텀
      // 스크롤바를 동기화 상태로 유지한다.
      VisualElement lastEntry = _logContent[_logContent.childCount - 1];
      _logView.ScrollTo(lastEntry);
      if (_logView.verticalScroller != null)
        _logView.verticalScroller.value = _logView.verticalScroller.highValue;
      SetScrollOffset(GetMaximumScrollOffset());
    }

    private void ShowToast(string message)
    {
      if (string.IsNullOrEmpty(message) || _toastContainer == null)
        return;

      var toastRoot = new VisualElement { pickingMode = PickingMode.Ignore };
      // IStyle 에 `gap` 이 없어 margin 으로 토스트 사이 간격을 만든다.
      toastRoot.style.marginTop = _toasts.Count > 0 ? 6 : 0;
      toastRoot.AddToClassList("chat-toast");
      toastRoot.style.opacity = 1f;

      var label = new Label
      {
        text = message,
        pickingMode = PickingMode.Ignore
      };
      label.AddToClassList("chat-toast__label");
      label.style.whiteSpace = WhiteSpace.Normal;
      label.style.flexShrink = 1;
      label.style.width = Length.Percent(100);

      toastRoot.Add(label);
      _toastContainer.Add(toastRoot);

      _toasts.Add(new ToastEntry
      {
        Root = toastRoot,
        CreatedAt = Time.realtimeSinceStartup
      });

      if (_toasts.Count > MaxToastEntries)
        RemoveToastAt(0);

      _toastContainer.style.display = DisplayStyle.Flex;
      _toastContainer.style.visibility = Visibility.Visible;
      _toastSchedule?.Resume();
    }

    private void UpdateToasts()
    {
      if (_toasts.Count == 0)
      {
        _toastSchedule?.Pause();
        return;
      }

      float now = Time.realtimeSinceStartup;
      for (int i = _toasts.Count - 1; i >= 0; i--)
      {
        ToastEntry toast = _toasts[i];
        float elapsed = now - toast.CreatedAt;

        if (elapsed >= ToastVisibleSeconds + ToastFadeSeconds)
        {
          RemoveToastAt(i);
          continue;
        }

        if (elapsed >= ToastVisibleSeconds)
        {
          float t = Mathf.Clamp01((elapsed - ToastVisibleSeconds) / ToastFadeSeconds);
          toast.Root.style.opacity = 1f - t;
        }
      }
    }

    private void RemoveToastAt(int index)
    {
      if (index < 0 || index >= _toasts.Count)
        return;

      ToastEntry toast = _toasts[index];
      toast.Root?.RemoveFromHierarchy();
      _toasts.RemoveAt(index);

      if (_toasts.Count == 0)
      {
        _toastContainer.style.display = DisplayStyle.None;
        _toastContainer.style.visibility = Visibility.Hidden;
        _toastSchedule?.Pause();
      }
    }

    public void ClearToasts()
    {
      for (int i = _toasts.Count - 1; i >= 0; i--)
        RemoveToastAt(i);

      _toasts.Clear();
      _toastContainer?.Clear();
      if (_toastContainer != null)
      {
        _toastContainer.style.display = DisplayStyle.None;
        _toastContainer.style.visibility = Visibility.Hidden;
      }

      _toastSchedule?.Pause();
    }

    public void PushInput(string text)
    {
      if (_inputField == null || string.IsNullOrEmpty(text))
        return;

      _inputField.SetValueWithoutNotify(text);
      ResetHistoryCursor();
      FocusInput();
    }

    private void HandleInputKeyDown(KeyDownEvent evt)
    {
      // UI Toolkit 은 키 입력 한 번마다 KeyDownEvent 를 두 번 보낸다. 첫 이벤트에는
      // keyCode 만 채워져 있고, 이어지는 이벤트에는 입력된 문자만 채워진 채 keyCode
      // 가 KeyCode.None 이다. 두 번째 이벤트를 그대로 넘기면 컨트롤러가 "Tab 이 아닌
      // 키"로 해석해서, Tab 을 누를 때마다 순환 세션이 즉시 폐기된다. 그래서 탭 문자
      // 이벤트는 KeyCode.Tab 으로 되돌려 준다.
      KeyCode keyCode = evt.keyCode;
      if (keyCode == KeyCode.None && evt.character == '\t')
        keyCode = KeyCode.Tab;
      // 스페이스는 다음 구문을 쓰기 시작한다는 뜻이므로, 문자만 담긴 이벤트로 오더라도
      // 컨트롤러가 후보 목록을 닫을 수 있게 키로 되돌려 준다.
      else if (keyCode == KeyCode.None && evt.character == ' ')
        keyCode = KeyCode.Space;

      if (keyCode == KeyCode.Tab)
        evt.StopPropagation();

      // 후보 목록이 열려 있으면 화살표, Enter, Escape 는 목록을 조작하는 키다. 실제
      // 동작은 PlayerController.Input 이 맡으므로 여기서는 텍스트 필드가 이 키들을 캐럿
      // 이동, 개행, 편집 취소로 소비하지 못하게 막기만 한다.
      if (IsCompletionListVisible && IsCompletionNavigationKey(keyCode, evt.character))
        evt.StopPropagation();

      // 문자만 담긴 이벤트는 어떤 키를 눌렀는지 알려 주지 못하므로 전달하지 않는다.
      // 편집이 일어났는지는 같은 입력에 대해 먼저 도착한 keyCode 이벤트로 판단한다.
      if (keyCode == KeyCode.None)
        return;

      InputKeyPressed?.Invoke(keyCode);
    }

    private static bool IsCompletionNavigationKey(KeyCode keyCode, char character)
    {
      return keyCode == KeyCode.UpArrow
        || keyCode == KeyCode.DownArrow
        || keyCode == KeyCode.Return
        || keyCode == KeyCode.KeypadEnter
        || keyCode == KeyCode.Escape
        || character == '\n'
        || character == '\r';
    }

    private void AddInputHistory(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return;

      string normalized = text.Trim();
      if (_inputHistory.Count > 0 && _inputHistory[_inputHistory.Count - 1] == normalized)
        return;

      _inputHistory.Add(normalized);
      if (_inputHistory.Count > MaxInputHistoryEntries)
        _inputHistory.RemoveAt(0);
    }

    private void ResetHistoryCursor()
    {
      _historyCursor = -1;
      _historyDraft = string.Empty;
    }
  }
}
