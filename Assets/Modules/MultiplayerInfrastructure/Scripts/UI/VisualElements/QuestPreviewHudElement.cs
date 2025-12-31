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
      _cards?.Clear();

      if (tracked == null || tracked.Count == 0)
      {
        style.display = DisplayStyle.None;
        return;
      }

      style.display = DisplayStyle.Flex;

      int count = Mathf.Min(tracked.Count, DefaultsQuestControl.MaxTrackedQuests);
      for (int i = 0; i < count; i++)
      {
        var quest = tracked[i];
        if (quest == null)
          continue;

        var card = CreateCard(quest);
        _cards.Add(card);
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
      // style.pointerEvents = PointerEvents.None;
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
      // card.style.pointerEvents = PointerEvents.None;

      var title = new Label(quest.Title ?? string.Empty) { pickingMode = PickingMode.Ignore };
      title.style.color = TitleColor;
      title.style.fontSize = 12;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      title.style.opacity = 0.92f;
      card.Add(title);

      var content = new Label(quest.QuestContent ?? string.Empty) { pickingMode = PickingMode.Ignore };
      content.style.color = ContentColor;
      content.style.fontSize = 13;
      content.style.marginTop = 6;
      content.style.whiteSpace = WhiteSpace.Normal;
      card.Add(content);

      return card;
    }
  }
}
