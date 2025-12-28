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

    private readonly Queue<VisualElement> _logEntries = new();
    private readonly List<ToastEntry> _toasts = new();

    private ScrollView _logView;
    private TextField _inputField;
    private VisualElement _toastContainer;
    private VisualElement _panel;
    private IVisualElementScheduledItem _toastSchedule;
    private bool _isOpen;
    private int _maxLogEntries = DefaultMaxLogEntries;

    private static Color StyleColorBackground = new Color(0f, 0f, 0f, 0.82f);

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
      // USS `gap` and IStyle.pointerEvents are not available in this scripting API level.
      // Use margins on children for spacing and pickingMode for pointer behavior.
      _panel.pickingMode = PickingMode.Position;
      Add(_panel);

      _logView = new ScrollView(ScrollViewMode.Vertical)
      {
        name = DefaultsChatControl.ChatLogName,
        verticalScrollerVisibility = ScrollerVisibility.Auto,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden,
        pickingMode = PickingMode.Ignore
      };
      _logView.AddToClassList("chat-log");
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
      _panel.Add(_logView);

      var inputRow = new VisualElement();
      inputRow.AddToClassList("chat-input-row");
      _panel.Add(inputRow);

      var inputBg = new VisualElement();
      inputBg.AddToClassList("chat-input-bg");
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
      _inputField.style.color = Color.white;
      
      var _textInputField = _inputField.Q("unity-text-input");
      _textInputField.style.backgroundColor = Color.clear;
      _textInputField.style.borderTopWidth = 0;
      _textInputField.style.borderBottomWidth = 0;
      _textInputField.style.borderLeftWidth = 0;
      _textInputField.style.borderRightWidth = 0;

      inputBg.Add(_inputField);

      _toastContainer = new VisualElement
      {
        name = "chat-toast-container",
        pickingMode = PickingMode.Ignore
      };
      _toastContainer.AddToClassList("chat-toast-container");
      _toastContainer.style.position = Position.Absolute;
      _toastContainer.style.left = 0;
      _toastContainer.style.bottom = 0;
      _toastContainer.style.width = Length.Percent(100);
      _toastContainer.style.display = DisplayStyle.None;
      _toastContainer.style.visibility = Visibility.Hidden;
      _toastContainer.style.flexDirection = FlexDirection.Column;
      _toastContainer.style.alignItems = Align.FlexStart;
      Add(_toastContainer);
    }

    private void ApplyInlineStyles()
    {
      style.position = Position.Absolute;
      style.left = 18;
      style.bottom = 18;
      style.width = 420;
      style.flexDirection = FlexDirection.Column;
      style.alignItems = Align.FlexStart;
      style.justifyContent = Justify.FlexEnd;
      // style.gap = 8;
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
      _inputField.SetValueWithoutNotify(string.Empty);
      return text;
    }

    public void ClearInput()
    {
      _inputField?.SetValueWithoutNotify(string.Empty);
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

      _logView.contentContainer.Add(entry);
      _logEntries.Enqueue(entry);

      TrimLogIfNeeded();
      ScrollToBottom();

      if (!_isOpen && showToastWhenHidden)
        ShowToast(message);
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
      toastRoot.style.marginTop = 6;
      toastRoot.AddToClassList("chat-toast");
      toastRoot.style.opacity = 1f;

      var label = new Label
      {
        text = message,
        pickingMode = PickingMode.Ignore
      };
      label.AddToClassList("chat-toast__label");

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
  }
}
