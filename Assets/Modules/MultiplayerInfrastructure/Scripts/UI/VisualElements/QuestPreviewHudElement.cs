using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Quest;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  [UxmlElement]
  public partial class QuestPreviewHudElement : VisualElement
  {
    private static readonly Color AccentColor = new Color(0.35f, 0.8f, 0.62f);
    private static readonly Color CardColor = new Color(0f, 0f, 0f, 0.78f);
    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color TitleColor = new Color(0.86f, 0.94f, 0.86f, 1f);
    private static readonly Color ContentColor = new Color(0.95f, 0.98f, 0.96f, 1f);
    private static readonly Color DescriptionColor = new Color(0.8f, 0.88f, 0.84f, 0.92f);
    private static readonly Color HintColor = new Color(0.8f, 0.88f, 0.84f, 0.92f);

    private VisualElement _cards;
    private VisualElement _detailHint;

    public QuestPreviewHudElement()
    {
      name = DefaultsQuestControl.QuestPreviewRootName;
      AddToClassList("quest-preview-root");
      ApplyInlineStyles();
      Build();
    }

    public void SetTrackedQuests(IReadOnlyList<QuestData> tracked)
    {
      SetQuests(tracked, null);
    }

    public void SetQuests(IReadOnlyList<QuestData> tracked, IReadOnlyList<QuestData> completed)
    {
      _cards?.Clear();

      int completedCount = completed?.Count ?? 0;
      int trackedCount = tracked?.Count ?? 0;
      if (completedCount == 0 && trackedCount == 0)
      {
        style.display = DisplayStyle.None;
        return;
      }

      style.display = DisplayStyle.Flex;

      var completedIds = new HashSet<string>();
      int displayed = 0;
      for (int i = 0; i < completedCount && displayed < DefaultsQuestControl.MaxTrackedQuests; i++)
      {
        var quest = completed[i];
        if (quest == null)
          continue;

        completedIds.Add(quest.Id);
        _cards.Add(CreateCompletedCard(quest));
        displayed++;
      }

      for (int i = 0; i < trackedCount && displayed < DefaultsQuestControl.MaxTrackedQuests; i++)
      {
        var quest = tracked[i];
        if (quest == null || completedIds.Contains(quest.Id))
          continue;

        var card = CreateCard(quest);
        _cards.Add(card);
        displayed++;
      }
    }

    private void ApplyInlineStyles()
    {
      style.position = Position.Absolute;
      style.top = 16;
      style.right = 16;
      style.width = 360;
      style.flexDirection = FlexDirection.Column;
      style.alignItems = Align.FlexEnd;
      style.justifyContent = Justify.FlexStart;
      style.display = DisplayStyle.None;
    }

    private void Build()
    {
      _cards = new VisualElement();
      _cards.name = DefaultsQuestControl.QuestPreviewListName;
      _cards.style.flexDirection = FlexDirection.Column;
      _cards.style.alignItems = Align.FlexEnd;
      Add(_cards);

      _detailHint = new Label($"[{DefaultsKeyConfiguration.OpenQuestUI}]를 눌러 자세히")
      {
        pickingMode = PickingMode.Ignore
      };
      _detailHint.style.color = HintColor;
      _detailHint.style.fontSize = 11;
      _detailHint.style.marginTop = 2;
      _detailHint.style.marginRight = 4;
      Add(_detailHint);
    }

    private VisualElement CreateCard(QuestData quest)
    {
      var card = new VisualElement { pickingMode = PickingMode.Ignore };
      card.style.width = 320;
      card.style.backgroundColor = CardColor;
      card.style.borderLeftWidth = 2;
      card.style.borderLeftColor = AccentColor;
      card.style.borderTopWidth = 1;
      card.style.borderRightWidth = 1;
      card.style.borderBottomWidth = 1;
      card.style.borderTopColor = BorderColor;
      card.style.borderRightColor = BorderColor;
      card.style.borderBottomColor = BorderColor;
      card.style.borderTopLeftRadius = 8;
      card.style.borderTopRightRadius = 8;
      card.style.borderBottomLeftRadius = 8;
      card.style.borderBottomRightRadius = 8;
      card.style.paddingLeft = 10;
      card.style.paddingRight = 10;
      card.style.paddingTop = 6;
      card.style.paddingBottom = 6;
      card.style.marginBottom = 10;
      card.style.flexDirection = FlexDirection.Column;

      var title = new Label(quest.Title ?? string.Empty) { pickingMode = PickingMode.Ignore };
      title.style.color = TitleColor;
      title.style.fontSize = 15;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      title.style.opacity = 0.92f;
      card.Add(title);

      var content = new Label(GetCurrentObjective(quest)) { pickingMode = PickingMode.Ignore };
      content.style.color = ContentColor;
      content.style.fontSize = 13;
      content.style.marginTop = 2;
      content.style.whiteSpace = WhiteSpace.NoWrap;
      content.style.overflow = Overflow.Hidden;
      content.style.textOverflow = TextOverflow.Ellipsis;
      card.Add(content);

      return card;
    }

    private VisualElement CreateCompletedCard(QuestData quest)
    {
      var card = CreateCard(quest);
      card.Clear();

      var title = new Label(quest.Title ?? string.Empty) { pickingMode = PickingMode.Ignore };
      title.style.color = TitleColor;
      title.style.fontSize = 15;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      card.Add(title);

      // TextCore의 취소선 태그를 사용한다. 별도 absolute VisualElement로 선을 그리면
      // 내용 기반 부모의 너비가 0으로 계산되는 프레임에서 선이 보이지 않을 수 있다.
      var objective = new Label($"<s>{GetCurrentObjective(quest)}</s>") { pickingMode = PickingMode.Ignore };
      objective.enableRichText = true;
      objective.style.color = AccentColor;
      objective.style.fontSize = 13;
      objective.style.marginTop = 2;
      objective.style.alignSelf = Align.FlexStart;
      objective.style.maxWidth = 296;
      objective.style.whiteSpace = WhiteSpace.NoWrap;
      objective.style.overflow = Overflow.Hidden;
      objective.style.textOverflow = TextOverflow.Ellipsis;
      card.Add(objective);
      return card;
    }

    public static string GetCurrentObjective(QuestData quest)
    {
      // 내 몫은 끝났지만 함께 진행하는 참여자를 기다리는 동안에는 남은 목표 대신 대기 문구를 보여 준다.
      if (quest?.GroupWait != null && quest.GroupWait.IsWaitingForOthers)
        return quest.GroupWait.WaitingDisplayText;

      var tasks = QuestManager.GetQuestTasks(quest);
      if (tasks != null)
      {
        for (int i = 0; i < tasks.Count; i++)
        {
          var task = tasks[i];
          if (task != null && !task.Completed)
          {
            string taskText = FormatCriterion(task);
            if (!string.IsNullOrWhiteSpace(taskText))
              return taskText;
          }
        }
      }

      return quest?.QuestContent?.Trim() ?? string.Empty;
    }

    private static void AddCriteria(VisualElement card, IReadOnlyList<QuestCompletionCriteria> criteria, bool isOrdinal)
    {
      if (criteria == null || criteria.Count == 0)
        return;

      int visibleCount = isOrdinal ? GetOrdinalVisibleTaskCount(criteria) : criteria.Count;
      for (int i = 0; i < visibleCount; i++)
      {
        var each = criteria[i];
        if (each == null)
          continue;

        AddCriterion(card, each, 0, isOrdinal && IsCurrentTask(criteria, i));
      }
    }

    private static void AddCriterion(VisualElement parent, QuestCompletionCriteria criterion, int depth, bool isCurrent)
    {
      string displayText = FormatCriterion(criterion);
      if (!string.IsNullOrWhiteSpace(displayText))
      {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.style.position = Position.Relative;
        row.style.alignSelf = Align.FlexStart;
        row.style.maxWidth = Mathf.Max(120, 296 - depth * 10);
        row.style.marginTop = 2;
        row.style.marginLeft = depth * 10;

        var label = new Label(displayText) { pickingMode = PickingMode.Ignore };
        label.style.color = criterion.Completed || isCurrent ? AccentColor : DescriptionColor;
        label.style.fontSize = 11;
        label.style.unityFontStyleAndWeight = isCurrent ? FontStyle.Bold : FontStyle.Normal;
        label.style.whiteSpace = WhiteSpace.Normal;
        row.Add(label);

        if (criterion.Completed)
        {
          var strike = new VisualElement { pickingMode = PickingMode.Ignore };
          strike.style.position = Position.Absolute;
          strike.style.left = 0;
          strike.style.right = 0;
          strike.style.top = new Length(50, LengthUnit.Percent);
          strike.style.height = 1;
          strike.style.backgroundColor = AccentColor;
          row.Add(strike);
        }

        parent.Add(row);
      }

      if (criterion.Conditions == null)
        return;

      for (int i = 0; i < criterion.Conditions.Count; i++)
      {
        var child = criterion.Conditions[i];
        if (child != null)
          AddCriterion(parent, child, depth + 1, isCurrent);
      }
    }

    private static int GetOrdinalVisibleTaskCount(IReadOnlyList<QuestCompletionCriteria> tasks)
    {
      for (int i = 0; i < tasks.Count; i++)
      {
        if (tasks[i] != null && !tasks[i].Completed)
          return i + 1;
      }

      return tasks.Count;
    }

    private static bool IsCurrentTask(IReadOnlyList<QuestCompletionCriteria> tasks, int index)
    {
      if (tasks == null || index < 0 || index >= tasks.Count)
        return false;

      for (int i = 0; i < tasks.Count; i++)
      {
        if (tasks[i] != null && !tasks[i].Completed)
          return i == index;
      }

      return false;
    }

    private static string FormatCriterion(QuestCompletionCriteria criterion)
    {
      if (!string.IsNullOrWhiteSpace(criterion.DisplayTextContent))
        return criterion.DisplayTextContent.Trim();

      string progress = criterion.Progress?.ToDisplayText() ?? $"0/{Mathf.Max(1, criterion.Count)}";
      return criterion.Type switch
      {
        QuestCompletionCriteriaType.InventoryContains when !string.IsNullOrWhiteSpace(criterion.ItemId) => $"{criterion.ItemId} ({progress})",
        QuestCompletionCriteriaType.InteractionSignalReceived when !string.IsNullOrWhiteSpace(criterion.SignalId) => $"{criterion.SignalId} ({progress})",
        QuestCompletionCriteriaType.WaypointReached when !string.IsNullOrWhiteSpace(criterion.WaypointIdentifier) => $"Waypoint: {criterion.WaypointIdentifier}",
        QuestCompletionCriteriaType.AllOf => "모든 조건 달성",
        QuestCompletionCriteriaType.AnyOf => "조건 중 하나 달성",
        _ => string.Empty
      };
    }

    private static string FormatProgress(QuestData quest)
    {
      if (quest?.Progress == null)
        return "0/1";

      return quest.Progress.ToDisplayText();
    }

  }
}
