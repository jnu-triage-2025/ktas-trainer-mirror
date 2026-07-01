using System.Collections.Generic;
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

    private readonly Queue<VisualElement> _logEntries = new();
    private readonly List<ToastEntry> _toasts = new();
    private readonly List<string> _inputHistory = new();

    private ScrollView _logView;
    private TextField _inputField;
    private VisualElement _toastPanel;
    private VisualElement _toastContainer;
    private VisualElement _panel;
    private IVisualElementScheduledItem _toastSchedule;
    private bool _isOpen;
    private int _maxLogEntries = DefaultMaxLogEntries;
    private int _historyCursor = -1;
    private string _historyDraft = string.Empty;

    private static Color StyleColorBackground = new Color(0f, 0f, 0f, 0.82f);
    private static Color StyleColorText = Color.white;

    const float styleLeft = 0f;
    const float styleRight = 0f;
    const float styleBottom = 0f;
    const float stylePaddingLeft = 12f;
    const float stylePaddingBottom = 12f;
    const float stylePaddingRight = 12f;

    private class ToastEntry
    {
      public VisualElement Root;
      public float CreatedAt;
    }

    public bool IsOpen => _isOpen;
    public string InputText => _inputField?.text ?? string.Empty;

    public ChatPanelElement()
    {
      name = DefaultsChatControl.ChatRootName;
      AddToClassList("chat-root");
      AddToClassList("collapsed");

      ApplyInlineStyles();

      BuildPanel();

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
      // Always fill the fixed-width root so the panel width is constant.
      _panel.style.width = Length.Percent(100);
      _panel.style.alignItems = Align.Stretch;
      // USS `gap` and IStyle.pointerEvents are not available in this scripting API level.
      // Use margins on children for spacing and pickingMode for pointer behavior.
      _panel.pickingMode = PickingMode.Position;
      Add(_panel);

      _logView = new ScrollView(ScrollViewMode.Vertical)
      {
        name = DefaultsChatControl.ChatLogName,
        verticalScrollerVisibility = ScrollerVisibility.Auto,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden,
        // Must accept pointer events so the user can scroll the log with the
        // mouse wheel and drag the scrollbar. PickingMode.Ignore here would
        // block all scroll interaction (the reported "can't scroll history" bug).
        pickingMode = PickingMode.Position
      };
      // Speed up mouse-wheel scrolling a bit for chat history browsing.
      _logView.mouseWheelScrollSize = 40f;
      // Ensure the viewport and content also receive pointer/wheel events so
      // wheel scrolling works when hovering anywhere over the log area.
      _logView.contentViewport.pickingMode = PickingMode.Position;
      _logView.contentContainer.pickingMode = PickingMode.Position;
      _logView.AddToClassList("chat-log");
      _logView.style.width = Length.Percent(100);
      _logView.style.flexShrink = 0;
      _logView.style.backgroundColor = new Color(0f, 0f, 0f, 0.65f);
      _logView.style.paddingTop = 8;
      _logView.style.paddingBottom = 8;
      _logView.style.paddingLeft = 10;
      _logView.style.paddingRight = 10;
      _logView.style.minHeight = 140;
      _logView.style.height = 200;
      _logView.style.marginBottom = 8;
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
      _panel.Add(_logView);

      var inputRow = new VisualElement();
      inputRow.AddToClassList("chat-input-row");
      inputRow.style.width = Length.Percent(100);
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
      
      var _textInputField = _inputField.Q("unity-text-input");
      _textInputField.style.backgroundColor = Color.clear;
      _textInputField.style.borderTopWidth = 0;
      _textInputField.style.borderBottomWidth = 0;
      _textInputField.style.borderLeftWidth = 0;
      _textInputField.style.borderRightWidth = 0;

      inputBg.Add(_inputField);

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

    private void ApplyInlineStyles()
    {
      // Layout/width for the root is defined authoritatively in ChatPanelUI.uss
      // (.chat-root uses a fixed --panel-width). We intentionally do NOT set an
      // inline width here: an inline width would override the USS rule and, with
      // flex-start alignment, let the panel resize based on content length.
      style.position = Position.Absolute;
      style.left = styleLeft;
      style.right = styleRight;
      style.bottom = styleBottom;
      style.paddingLeft = stylePaddingLeft;
      style.paddingBottom = stylePaddingBottom;
      style.paddingRight = stylePaddingRight;
      style.flexDirection = FlexDirection.Column;
      // Stretch children (panel/log/input) to the full root width so the chat
      // width stays constant regardless of message or input text length.
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
        if (_panel != null) _panel.style.display = DisplayStyle.Flex;
        ClearToasts();
        // When the panel is (re)opened, show the most recent messages.
        ScrollToBottom();
      }
      else
      {
        RemoveFromClassList("expanded");
        AddToClassList("collapsed");
        if (_panel != null) _panel.style.display = DisplayStyle.None;
        // Keep toasts hidden when the panel is closed until new ones arrive.
        if (_toastContainer != null)
        {
          _toastContainer.style.display = DisplayStyle.None;
          _toastContainer.style.visibility = Visibility.Hidden;
        }
      }
    }

    public void FocusInput()
    {
      if (_inputField == null) return;

      _inputField.Focus();
      _inputField.cursorIndex = _inputField.text.Length;
      _inputField.selectIndex = _inputField.cursorIndex;
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
      // flexShrink MUST be 0. Inside the vertical ScrollView, a shrinkable
      // entry lets the content compress to fit the viewport, so the content
      // never exceeds the viewport height and the log becomes non-scrollable
      // (the reported "can't scroll history" bug).
      entry.style.flexShrink = 0;
      entry.style.width = Length.Percent(100);

      // Capture whether the user is currently pinned to (near) the bottom
      // BEFORE we add the new entry. Only auto-scroll when they were already
      // at the bottom, so scrolling up to read history is not interrupted.
      bool stickToBottom = IsScrolledToBottom();

      _logView.contentContainer.Add(entry);
      _logEntries.Enqueue(entry);

      TrimLogIfNeeded();

      if (stickToBottom)
        ScrollToBottom();

      if (!_isOpen && showToastWhenHidden)
        ShowToast(message);
    }

    /// <summary>
    /// True when the vertical scroller is at (or very near) the bottom,
    /// or when the content is not tall enough to scroll at all.
    /// Used to decide whether new messages should auto-scroll into view.
    /// </summary>
    private bool IsScrolledToBottom()
    {
      if (_logView == null)
        return true;

      var scroller = _logView.verticalScroller;
      if (scroller == null)
        return true;

      float range = scroller.highValue - scroller.lowValue;
      // No scrollable range yet: treat as bottom so the first messages show.
      if (range <= Mathf.Epsilon)
        return true;

      // Allow a small threshold so minor offsets still count as "at bottom".
      const float bottomThreshold = 4f;
      return scroller.value >= scroller.highValue - bottomThreshold;
    }

    public void ClearLog()
    {
      while (_logEntries.Count > 0)
      {
        var entry = _logEntries.Dequeue();
        entry?.RemoveFromHierarchy();
      }

      _logView?.contentContainer.Clear();
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

      _logView.schedule.Execute(() =>
      {
        var content = _logView.contentContainer;
        if (content.childCount > 0)
        {
          var last = content[content.childCount - 1];
          _logView.ScrollTo(last);
        }

        if (_logView.verticalScroller != null)
          _logView.verticalScroller.value = _logView.verticalScroller.highValue;
      });
    }

    private void ShowToast(string message)
    {
      if (string.IsNullOrEmpty(message) || _toastContainer == null)
        return;

      var toastRoot = new VisualElement { pickingMode = PickingMode.Ignore };
      // Provide spacing between toasts via margin since `gap` is not available on IStyle here.
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
