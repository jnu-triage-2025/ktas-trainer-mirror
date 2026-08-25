using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class QuestPanelElement : VisualElement
  {
    private const float PanelWidth = 520f;
    private const float EntrySpacing = 10f;
    private static readonly Color AccentColor = new Color(0.35f, 0.8f, 0.62f);
    private static readonly Color CardColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color CardBorder = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color TextPrimary = new Color(0.95f, 0.98f, 0.96f, 1f);
    private static readonly Color TextSecondary = new Color(0.85f, 0.9f, 0.9f, 0.9f);
    private static readonly Color CompletedColor = new Color(0.4f, 0.9f, 0.58f, 1f);

    private readonly Dictionary<string, QuestEntryElement> _entries = new();

    private ScrollView _questList;
    private Label _headerTitle;
    private Label _headerCount;
    private VisualElement _panel;
    private bool _isOpen;

    public event Action<string, bool> OnTrackToggled;

    public bool IsOpen => _isOpen;

    public QuestPanelElement()
    {
      name = DefaultsQuestControl.QuestRootName;
      AddToClassList("quest-root");
      AddToClassList("collapsed");
      ApplyInlineStyles();
      BuildPanel();
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
        style.display = DisplayStyle.Flex;
        if (_panel != null)
          _panel.style.display = DisplayStyle.Flex;
      }
      else
      {
        RemoveFromClassList("expanded");
        AddToClassList("collapsed");
        style.display = DisplayStyle.None;
        if (_panel != null)
          _panel.style.display = DisplayStyle.None;
      }
    }

    public void SetQuests(IReadOnlyList<QuestData> quests)
    {
      if (quests != null)
      {
        foreach (var quest in quests)
        {
          if (quest == null || string.IsNullOrWhiteSpace(quest.Id))
            continue;
          if (_entries.TryGetValue(quest.Id, out var existing))
          {
            existing.Bind(quest);
          }
          else
          {
            var entry = new QuestEntryElement(quest);
            entry.OnTrackClicked += HandleTrackClicked;
            _entries[quest.Id] = entry;
            _questList?.Add(entry);
            entry.style.marginTop = EntrySpacing;
          }
        }
      }

      // Remove entries that are no longer present.
      var idsToRemove = new List<string>();
      foreach (var kvp in _entries)
      {
        bool stillExists = quests != null && ContainsQuest(quests, kvp.Key);
        if (!stillExists)
          idsToRemove.Add(kvp.Key);
      }

      foreach (var id in idsToRemove)
      {
        if (_entries.TryGetValue(id, out var entry))
        {
          entry.RemoveFromHierarchy();
          entry.OnTrackClicked -= HandleTrackClicked;
        }
        _entries.Remove(id);
      }

      UpdateCountLabel();
    }

    public void UpdateTracked(IReadOnlyList<QuestData> trackedQuests)
    {
      var trackedSet = new HashSet<string>();
      if (trackedQuests != null)
      {
        foreach (var quest in trackedQuests)
        {
          if (quest != null && !string.IsNullOrWhiteSpace(quest.Id))
            trackedSet.Add(quest.Id);
        }
      }

      foreach (var kvp in _entries)
      {
        kvp.Value.SetTracked(trackedSet.Contains(kvp.Key));
      }

      UpdateCountLabel(trackedSet.Count);
    }

    private static bool ContainsQuest(IReadOnlyList<QuestData> quests, string id)
    {
      for (int i = 0; i < quests.Count; i++)
      {
        if (quests[i] != null && quests[i].Id == id)
          return true;
      }
      return false;
    }

    private void HandleTrackClicked(string questId, bool targetState)
    {
      OnTrackToggled?.Invoke(questId, targetState);
    }

    private void ApplyInlineStyles()
    {
      style.position = Position.Absolute;
      style.left = 18;
      style.top = 18;
      style.width = PanelWidth;
      style.flexDirection = FlexDirection.Column;
      style.alignItems = Align.Stretch;
      style.flexGrow = 0;
      style.flexShrink = 0;
      style.display = DisplayStyle.None;
      style.backgroundColor = Color.clear;
      style.paddingLeft = 0;
      style.paddingRight = 0;
      style.paddingTop = 0;
      style.paddingBottom = 0;
    }

    private void BuildPanel()
    {
      _panel = new VisualElement();
      _panel.AddToClassList("quest-panel");
      _panel.style.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
      _panel.style.borderTopLeftRadius = 10;
      _panel.style.borderTopRightRadius = 10;
      _panel.style.borderBottomLeftRadius = 10;
      _panel.style.borderBottomRightRadius = 10;
      _panel.style.borderLeftWidth = 1;
      _panel.style.borderRightWidth = 1;
      _panel.style.borderTopWidth = 1;
      _panel.style.borderBottomWidth = 1;
      _panel.style.borderLeftColor = CardBorder;
      _panel.style.borderRightColor = CardBorder;
      _panel.style.borderTopColor = CardBorder;
      _panel.style.borderBottomColor = CardBorder;
      _panel.style.paddingLeft = 14;
      _panel.style.paddingRight = 14;
      _panel.style.paddingTop = 12;
      _panel.style.paddingBottom = 12;
      _panel.style.flexDirection = FlexDirection.Column;
      Add(_panel);

      var headerRow = new VisualElement { pickingMode = PickingMode.Ignore };
      headerRow.style.flexDirection = FlexDirection.Row;
      headerRow.style.alignItems = Align.Center;
      headerRow.style.justifyContent = Justify.SpaceBetween;
      headerRow.style.marginBottom = 10;
      _panel.Add(headerRow);

      _headerTitle = new Label("Quests") { pickingMode = PickingMode.Ignore };
      _headerTitle.style.color = TextPrimary;
      _headerTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
      _headerTitle.style.fontSize = 16;
      headerRow.Add(_headerTitle);

      _headerCount = new Label("0/3 preview") { pickingMode = PickingMode.Ignore };
      _headerCount.style.color = TextSecondary;
      _headerCount.style.fontSize = 12;
      headerRow.Add(_headerCount);

      _questList = new ScrollView(ScrollViewMode.Vertical)
      {
        name = DefaultsQuestControl.QuestListName,
        verticalScrollerVisibility = ScrollerVisibility.Auto,
        horizontalScrollerVisibility = ScrollerVisibility.Hidden,
        pickingMode = PickingMode.Position
      };
      _questList.style.height = 460;
      _questList.style.paddingLeft = 4;
      _questList.style.paddingRight = 8;
      _questList.style.paddingTop = 4;
      _questList.style.paddingBottom = 8;
      _questList.style.backgroundColor = Color.clear;
      _questList.style.borderTopWidth = 0;
      _questList.style.borderBottomWidth = 0;
      _questList.style.borderLeftWidth = 0;
      _questList.style.borderRightWidth = 0;
      _panel.Add(_questList);
    }

    private void UpdateCountLabel(int trackedCount = -1)
    {
      if (_headerCount == null)
        return;

      int count = trackedCount >= 0 ? trackedCount : CalculateTrackedCount();
      _headerCount.text = $"{count}/{DefaultsQuestControl.MaxTrackedQuests} preview";
      _headerCount.style.color = count > DefaultsQuestControl.MaxTrackedQuests ? new Color(0.95f, 0.5f, 0.5f, 1f) : TextSecondary;
    }

    private int CalculateTrackedCount()
    {
      int count = 0;
      foreach (var entry in _entries.Values)
      {
        if (entry.IsTracked)
          count++;
      }
      return count;
    }

    private class QuestEntryElement : VisualElement
    {
      private readonly Label _titleLabel;
      private readonly Label _descriptionLabel;
      private readonly Label _contentLabel;
      private readonly Label _progressLabel;
      private readonly Label _statusLabel;
      private readonly Label _waypointLabel;
      private readonly Button _trackButton;
      private QuestData _boundQuest;

      public event Action<string, bool> OnTrackClicked;
      public bool IsTracked { get; private set; }

      public QuestEntryElement(QuestData quest)
      {
        style.flexDirection = FlexDirection.Column;
        style.backgroundColor = CardColor;
        style.borderTopLeftRadius = 8;
        style.borderTopRightRadius = 8;
        style.borderBottomLeftRadius = 8;
        style.borderBottomRightRadius = 8;
        style.borderLeftWidth = 1;
        style.borderRightWidth = 1;
        style.borderTopWidth = 1;
        style.borderBottomWidth = 1;
        style.borderLeftColor = CardBorder;
        style.borderRightColor = CardBorder;
        style.borderTopColor = CardBorder;
        style.borderBottomColor = CardBorder;
        style.paddingLeft = 12;
        style.paddingRight = 12;
        style.paddingTop = 10;
        style.paddingBottom = 10;
        style.unityFontStyleAndWeight = FontStyle.Normal;
        style.marginBottom = EntrySpacing;

        var titleRow = new VisualElement { pickingMode = PickingMode.Ignore };
        titleRow.style.flexDirection = FlexDirection.Row;
        titleRow.style.alignItems = Align.Center;
        titleRow.style.justifyContent = Justify.SpaceBetween;
        Add(titleRow);

        _titleLabel = new Label { pickingMode = PickingMode.Ignore };
        _titleLabel.style.color = TextPrimary;
        _titleLabel.style.fontSize = 14;
        _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleRow.Add(_titleLabel);

        _trackButton = new Button { text = "Preview", pickingMode = PickingMode.Position };
        _trackButton.style.backgroundColor = new Color(1f, 1f, 1f, 0.08f);
        _trackButton.style.color = TextPrimary;
        _trackButton.style.fontSize = 11;
        _trackButton.style.paddingLeft = 10;
        _trackButton.style.paddingRight = 10;
        _trackButton.style.paddingTop = 4;
        _trackButton.style.paddingBottom = 4;
        _trackButton.style.borderTopLeftRadius = 6;
        _trackButton.style.borderTopRightRadius = 6;
        _trackButton.style.borderBottomLeftRadius = 6;
        _trackButton.style.borderBottomRightRadius = 6;
        _trackButton.style.borderBottomWidth = 1;
        _trackButton.style.borderTopWidth = 1;
        _trackButton.style.borderLeftWidth = 1;
        _trackButton.style.borderRightWidth = 1;
        _trackButton.style.borderBottomColor = CardBorder;
        _trackButton.style.borderTopColor = CardBorder;
        _trackButton.style.borderLeftColor = CardBorder;
        _trackButton.style.borderRightColor = CardBorder;
        _trackButton.clicked += HandleTrackClicked;
        titleRow.Add(_trackButton);

        _descriptionLabel = new Label { pickingMode = PickingMode.Ignore };
        _descriptionLabel.style.color = TextSecondary;
        _descriptionLabel.style.fontSize = 12;
        _descriptionLabel.style.marginTop = 6;
        Add(_descriptionLabel);

        _contentLabel = new Label { pickingMode = PickingMode.Ignore };
        _contentLabel.style.color = AccentColor;
        _contentLabel.style.fontSize = 12;
        _contentLabel.style.marginTop = 4;
        _contentLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
        _contentLabel.style.whiteSpace = WhiteSpace.Normal;
        Add(_contentLabel);

        _progressLabel = new Label { pickingMode = PickingMode.Ignore };
        _progressLabel.style.color = TextPrimary;
        _progressLabel.style.fontSize = 11;
        _progressLabel.style.marginTop = 4;
        Add(_progressLabel);

        _statusLabel = new Label { pickingMode = PickingMode.Ignore };
        _statusLabel.style.color = TextSecondary;
        _statusLabel.style.fontSize = 11;
        _statusLabel.style.marginTop = 2;
        Add(_statusLabel);

        _waypointLabel = new Label { pickingMode = PickingMode.Ignore };
        _waypointLabel.style.color = TextSecondary;
        _waypointLabel.style.fontSize = 11;
        _waypointLabel.style.marginTop = 4;
        Add(_waypointLabel);

        Bind(quest);
      }

      public void Bind(QuestData quest)
      {
        _boundQuest = quest;
        if (quest == null)
          return;

        _titleLabel.text = quest.Title;
        _descriptionLabel.text = quest.Description;
        _contentLabel.text = quest.QuestContent;
        _progressLabel.text = $"진행도: {FormatProgress(quest)}";
        _statusLabel.text = quest.Completed ? "완료" : "진행 중";
        _statusLabel.style.color = quest.Completed ? CompletedColor : TextSecondary;
        SetTracked(quest.IsTracked);
        _waypointLabel.text = FormatWaypointText(quest.WaypointIdentifier);
        _waypointLabel.style.display = string.IsNullOrWhiteSpace(quest.WaypointIdentifier) ? DisplayStyle.None : DisplayStyle.Flex;
        style.opacity = quest.Completed ? 0.78f : 1f;
      }

      public void SetTracked(bool isTracked)
      {
        IsTracked = isTracked;
        bool trackable = _boundQuest == null || _boundQuest.IsTrackable;
        _trackButton.SetEnabled(trackable);
        _trackButton.text = !trackable ? "고정 불가" : isTracked ? "Untrack" : "Preview";
        _trackButton.style.backgroundColor = isTracked ? AccentColor : new Color(1f, 1f, 1f, 0.08f);
        _trackButton.style.color = isTracked ? Color.black : TextPrimary;
        style.borderLeftColor = isTracked ? AccentColor : CardBorder;
      }

      private void HandleTrackClicked()
      {
        if (_boundQuest == null || string.IsNullOrWhiteSpace(_boundQuest.Id))
          return;

        OnTrackClicked?.Invoke(_boundQuest.Id, !IsTracked);
      }

      private static string FormatWaypointText(string identifier)
      {
        return $"Waypoint: {identifier}";
      }

      private static string FormatProgress(QuestData quest)
      {
        if (quest?.Progress == null)
          return "0/1";

        return quest.Progress.ToDisplayText();
      }
    }
  }
}
