using System.Collections.Generic;
using TriageTrainer.Definitions;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.UI
{
  [RequireComponent((typeof(UIDocument)))]
  public class ChatUIController_ChatLogView : MonoBehaviour
  {
    [SerializeField] private string _chatLogScrollViewIdentifier = "chat-log";
    [SerializeField, Min(1)] private int maxLines = DefaultsChatControl.chatLogMaxLines;
    [SerializeField] private string _toastContainerIdentifier = "chat-toast-container";
    [SerializeField, Min(0.5f)] private float _toastVisibleSeconds = 3.5f;
    [SerializeField, Min(0.1f)] private float _toastFadeSeconds = 0.75f;
    [SerializeField, Min(1)] private int _maxToastEntries = 5;

    private UIDocument _uiDocument;
    private ScrollView _scrollView;
    private VisualElement _content;
    private VisualElement _toastContainer;
    private bool _pendingScroll;
    private bool _geometryHooked;
    private ChatUIController _chatUIController;
    private readonly List<ToastEntry> _activeToasts = new();
    private readonly Queue<Label> _entryQueue = new();

    private class ToastEntry
    {
      public VisualElement Root;
      public float CreatedAt;
    }

    void OnEnable()
    {
      _uiDocument = GetComponent<UIDocument>();
      _chatUIController = GetComponent<ChatUIController>();
      BindElements();
    }

    void Update()
    {
      bool chatOpen = _chatUIController != null && _chatUIController.IsOpened;
      if (chatOpen && _activeToasts.Count > 0)
        DismissAllToasts();

      if (_activeToasts.Count == 0 || _toastContainer == null)
        return;

      float now = Time.time;
      for (int i = _activeToasts.Count - 1; i >= 0; i--)
      {
        ToastEntry toast = _activeToasts[i];
        float elapsed = now - toast.CreatedAt;

        if (elapsed >= _toastVisibleSeconds + _toastFadeSeconds)
        {
          RemoveToastAt(i);
          continue;
        }

        if (elapsed >= _toastVisibleSeconds)
        {
          float t = Mathf.Clamp01((elapsed - _toastVisibleSeconds) / _toastFadeSeconds);
          toast.Root.style.opacity = 1f - t;
        }
      }
    }

    private void BindElements()
    {
      VisualElement root = _uiDocument.rootVisualElement;
      _scrollView = root.Q<ScrollView>(_chatLogScrollViewIdentifier);
      if (_scrollView != null)
      {
        _scrollView.mode = ScrollViewMode.Vertical;
        _scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
        _scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
        _content = _scrollView.contentContainer;

        if (!_geometryHooked)
        {
          _geometryHooked = true;
          _scrollView.RegisterCallback<GeometryChangedEvent>(_ =>
          {
            if (_pendingScroll)
            {
              _pendingScroll = false;
              ScrollToBottomImmediate();
            }
          });
        }
      }

      if (_toastContainer == null)
        EnsureToastContainer(root);
    }

    public void AddLine(string line)
    {
      if (_scrollView == null)
        BindElements();
      if (string.IsNullOrEmpty(line) || _scrollView == null) return;
      if (_content == null)
        _content = _scrollView.contentContainer;

      Label entry = new Label
      {
        text = line,
        pickingMode = PickingMode.Ignore
      };
      
      entry.AddToClassList("chat-log__entry");
      _content.Add(entry);
      _entryQueue.Enqueue(entry);

      if (_entryQueue.Count > maxLines)
      {
        Label oldest = _entryQueue.Dequeue();
        oldest?.RemoveFromHierarchy();
      }

      ScrollToBottom();

      if (_chatUIController != null && !_chatUIController.IsOpened)
        ShowTransientToast(line);
    }

    private void ScrollToBottom()
    {
      if (_scrollView == null) return;

      _pendingScroll = true;
      
      _scrollView.schedule.Execute(ScrollToBottomImmediate);
      _scrollView.schedule.Execute(ScrollToBottomImmediate).ExecuteLater(1);
    }

    private void ScrollToBottomImmediate()
    {
      if (_scrollView == null) return;

      // Try to scroll to the last child for reliability.
      if (_content != null && _content.childCount > 0)
      {
        VisualElement last = _content[_content.childCount - 1];
        _scrollView.ScrollTo(last);
      }

      var scroller = _scrollView.verticalScroller;
      if (scroller != null)
      {
        float targetY = Mathf.Max(0f, scroller.highValue);
        _scrollView.scrollOffset = new Vector2(_scrollView.scrollOffset.x, targetY);
        scroller.value = scroller.highValue;
      }

      _pendingScroll = false;
    }

    public void Clear()
    {
      while (_entryQueue.Count > 0)
      {
        var entry = _entryQueue.Dequeue();
        entry?.RemoveFromHierarchy();
      }
      _scrollView?.Clear();
      DismissAllToasts();
    }

    public void OnChatVisibilityChanged(bool isVisible)
    {
      if (isVisible)
        DismissAllToasts();
    }

    private void EnsureToastContainer(VisualElement root)
    {
      if (root == null)
        return;

      _toastContainer = root.Q<VisualElement>(_toastContainerIdentifier);
      if (_toastContainer == null)
      {
        _toastContainer = new VisualElement
        {
          name = _toastContainerIdentifier,
          pickingMode = PickingMode.Ignore
        };
        _toastContainer.AddToClassList("chat-toast-container");
        root.Add(_toastContainer);
      }

      _toastContainer.style.display = DisplayStyle.None;
      _toastContainer.style.visibility = Visibility.Hidden;
    }

    private void ShowTransientToast(string line)
    {
      if (string.IsNullOrEmpty(line))
        return;

      if (_toastContainer == null)
        EnsureToastContainer(_uiDocument?.rootVisualElement);
      if (_toastContainer == null)
        return;

      VisualElement toastRoot = new VisualElement { pickingMode = PickingMode.Ignore };
      toastRoot.AddToClassList("chat-toast");
      toastRoot.style.opacity = 1f;

      Label toastLabel = new Label
      {
        text = line,
        pickingMode = PickingMode.Ignore
      };
      toastLabel.AddToClassList("chat-toast__label");

      toastRoot.Add(toastLabel);
      _toastContainer.Add(toastRoot);

      _activeToasts.Add(new ToastEntry
      {
        Root = toastRoot,
        CreatedAt = Time.time
      });

      if (_activeToasts.Count > _maxToastEntries)
        RemoveToastAt(0);

      _toastContainer.style.display = DisplayStyle.Flex;
      _toastContainer.style.visibility = Visibility.Visible;
    }

    private void RemoveToastAt(int index)
    {
      if (index < 0 || index >= _activeToasts.Count)
        return;

      ToastEntry toast = _activeToasts[index];
      toast.Root?.RemoveFromHierarchy();
      _activeToasts.RemoveAt(index);

      if (_activeToasts.Count == 0 && _toastContainer != null)
      {
        _toastContainer.style.display = DisplayStyle.None;
        _toastContainer.style.visibility = Visibility.Hidden;
      }
    }

    private void DismissAllToasts()
    {
      for (int i = _activeToasts.Count - 1; i >= 0; i--)
        RemoveToastAt(i);

      _activeToasts.Clear();
      if (_toastContainer != null)
      {
        _toastContainer.Clear();
        _toastContainer.style.display = DisplayStyle.None;
        _toastContainer.style.visibility = Visibility.Hidden;
      }
    }
  }
}
