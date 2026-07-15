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

    private VisualElement _cards;

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
      card.style.paddingLeft = 12;
      card.style.paddingRight = 12;
      card.style.paddingTop = 10;
      card.style.paddingBottom = 10;
      card.style.marginBottom = 10;
      card.style.flexDirection = FlexDirection.Column;

      var title = new Label(quest.Title ?? string.Empty) { pickingMode = PickingMode.Ignore };
      title.style.color = TitleColor;
      title.style.fontSize = 12;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      title.style.opacity = 0.92f;
      card.Add(title);

      var description = new Label(quest.Description ?? string.Empty) { pickingMode = PickingMode.Ignore };
      description.style.color = DescriptionColor;
      description.style.fontSize = 11;
      description.style.marginTop = 4;
      description.style.whiteSpace = WhiteSpace.Normal;
      description.style.display = string.IsNullOrWhiteSpace(quest.Description) ? DisplayStyle.None : DisplayStyle.Flex;
      card.Add(description);

      var content = new Label(quest.QuestContent ?? string.Empty) { pickingMode = PickingMode.Ignore };
      content.style.color = ContentColor;
      content.style.fontSize = 13;
      content.style.marginTop = 6;
      content.style.whiteSpace = WhiteSpace.Normal;
      card.Add(content);

      AddCriteria(card, quest.CompletionCriteria);

      var progress = new Label($"진행도: {FormatProgress(quest)}") { pickingMode = PickingMode.Ignore };
      progress.style.color = quest.Completed ? AccentColor : ContentColor;
      progress.style.fontSize = 11;
      progress.style.marginTop = 4;
      card.Add(progress);

      var status = new Label(quest.Completed ? "상태: 완료" : "상태: 진행 중") { pickingMode = PickingMode.Ignore };
      status.style.color = quest.Completed ? AccentColor : DescriptionColor;
      status.style.fontSize = 11;
      status.style.marginTop = 2;
      card.Add(status);

      var waypointLabel = new Label(FormatWaypointText(quest.WaypointIdentifier)) { pickingMode = PickingMode.Ignore };
      waypointLabel.style.color = AccentColor;
      waypointLabel.style.fontSize = 11;
      waypointLabel.style.marginTop = 4;
      waypointLabel.style.display = string.IsNullOrWhiteSpace(quest.WaypointIdentifier) ? DisplayStyle.None : DisplayStyle.Flex;
      card.Add(waypointLabel);

      return card;
    }

    private VisualElement CreateCompletedCard(QuestData quest)
    {
      var card = CreateCard(quest);
      card.Clear();

      var completed = new Label("퀘스트 완료") { pickingMode = PickingMode.Ignore };
      completed.style.color = AccentColor;
      completed.style.fontSize = 16;
      completed.style.unityFontStyleAndWeight = FontStyle.Bold;
      completed.style.unityTextAlign = TextAnchor.MiddleCenter;
      completed.style.paddingTop = 8;
      completed.style.paddingBottom = 8;
      card.Add(completed);
      return card;
    }

    private static void AddCriteria(VisualElement card, IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      if (criteria == null || criteria.Count == 0)
        return;

      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        if (each == null)
          continue;

        AddCriterion(card, each, 0);
      }
    }

    private static void AddCriterion(VisualElement parent, QuestCompletionCriteria criterion, int depth)
    {
      string displayText = FormatCriterion(criterion);
      if (!string.IsNullOrWhiteSpace(displayText))
      {
        var row = new VisualElement { pickingMode = PickingMode.Ignore };
        row.style.position = Position.Relative;
        row.style.alignSelf = Align.FlexStart;
        row.style.maxWidth = Mathf.Max(120, 296 - depth * 10);
        row.style.marginTop = 4;
        row.style.marginLeft = depth * 10;

        var label = new Label(displayText) { pickingMode = PickingMode.Ignore };
        label.style.color = criterion.Completed ? AccentColor : DescriptionColor;
        label.style.fontSize = 11;
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
          AddCriterion(parent, child, depth + 1);
      }
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

    private static string FormatWaypointText(string waypointIdentifier)
    {
      return $"Waypoint: {waypointIdentifier}";
    }
  }
}
