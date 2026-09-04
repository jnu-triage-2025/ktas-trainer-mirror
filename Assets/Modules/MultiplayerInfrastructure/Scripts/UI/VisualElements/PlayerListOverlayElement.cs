using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 현재 세션에 접속한 플레이어 목록을 보여 주는 오버레이의 시각 요소.
  ///
  /// 지정한 키를 누르고 있는 동안에만 나타나며, 표시 여부와 항목은
  /// <see cref="PlayerListOverlayUIController"/> 가 주입한다.
  ///
  /// 비차단 HUD 이므로 루트와 모든 하위 요소의 pickingMode 를 Ignore 로 두어
  /// 아래에 있는 UI 의 클릭/휠 입력을 가로채지 않는다.
  /// (<see cref="UIOverlayStack"/>/<see cref="IUIOverlay"/> 는 모달 전용이므로 사용하지 않는다.)
  ///
  /// 시각 스타일은 <see cref="TimeDisplayElement"/> 와 같이 인라인 스타일에서 관리한다
  /// (uxml/uss/StyleSheet 불필요).
  /// </summary>
  [UxmlElement]
  public partial class PlayerListOverlayElement : VisualElement
  {
    public const string RootName = "player-list-overlay-root";

    /// <summary>목록에 표시할 접속자 한 명의 스냅샷.</summary>
    public readonly struct Entry
    {
      public Entry(string displayName, string tags, bool isLocal)
      {
        DisplayName = displayName;
        Tags = tags;
        IsLocal = isLocal;
      }

      public string DisplayName { get; }

      /// <summary>플레이어 태그(역할)를 사람이 읽을 수 있게 이어 붙인 문자열. 없으면 빈 문자열.</summary>
      public string Tags { get; }

      /// <summary>이 항목이 로컬 플레이어인지 여부. 강조 표시에 사용한다.</summary>
      public bool IsLocal { get; }
    }

    // 접속 인원이 많아도 화면을 지나치게 가리지 않도록 표시 개수를 제한하고,
    // 나머지는 요약 줄로 보여 준다.
    private const int MaxVisibleEntries = 16;
    private const float TopMargin = 56f;
    private const float PanelWidth = 420f;

    private static readonly Color CardColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.10f);
    private static readonly Color HeaderColor = new Color(0.78f, 0.86f, 0.92f, 0.85f);
    private static readonly Color NameColor = new Color(0.92f, 0.96f, 1f, 1f);
    private static readonly Color LocalNameColor = new Color(0.55f, 0.90f, 0.72f, 1f);
    private static readonly Color TagColor = new Color(0.78f, 0.86f, 0.92f, 0.72f);
    private static readonly Color RowColor = new Color(1f, 1f, 1f, 0.05f);
    private static readonly Color SubduedColor = new Color(0.78f, 0.86f, 0.92f, 0.6f);

    private VisualElement _card;
    private Label _headerLabel;
    private VisualElement _rows;

    public PlayerListOverlayElement()
    {
      name = RootName;
      AddToClassList("player-list-overlay-root");
      pickingMode = PickingMode.Ignore;
      ApplyInlineStyles();
      Build();
      SetVisibleState(false);
    }

    private void ApplyInlineStyles()
    {
      // 가로 중앙 정렬, 세로 top 정렬을 위한 전체폭 컨테이너.
      // 시간 표시 HUD(top 12) 와 겹치지 않도록 상단 여백을 둔다.
      style.position = Position.Absolute;
      style.top = TopMargin;
      style.left = 0;
      style.right = 0;
      style.flexDirection = FlexDirection.Row;
      style.alignItems = Align.FlexStart;
      style.justifyContent = Justify.Center;
    }

    private void Build()
    {
      _card = new VisualElement { pickingMode = PickingMode.Ignore };
      _card.style.maxWidth = PanelWidth;
      _card.style.minWidth = 240;
      _card.style.backgroundColor = CardColor;
      _card.style.borderTopWidth = 1;
      _card.style.borderRightWidth = 1;
      _card.style.borderBottomWidth = 1;
      _card.style.borderLeftWidth = 1;
      _card.style.borderTopColor = BorderColor;
      _card.style.borderRightColor = BorderColor;
      _card.style.borderBottomColor = BorderColor;
      _card.style.borderLeftColor = BorderColor;
      _card.style.borderTopLeftRadius = 8;
      _card.style.borderTopRightRadius = 8;
      _card.style.borderBottomLeftRadius = 8;
      _card.style.borderBottomRightRadius = 8;
      _card.style.paddingLeft = 12;
      _card.style.paddingRight = 12;
      _card.style.paddingTop = 8;
      _card.style.paddingBottom = 8;
      _card.style.flexDirection = FlexDirection.Column;
      Add(_card);

      _headerLabel = new Label(string.Empty) { pickingMode = PickingMode.Ignore };
      _headerLabel.style.color = HeaderColor;
      _headerLabel.style.fontSize = 12;
      _headerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _headerLabel.style.marginBottom = 6;
      _headerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
      _card.Add(_headerLabel);

      _rows = new VisualElement { pickingMode = PickingMode.Ignore };
      _rows.style.flexDirection = FlexDirection.Column;
      _card.Add(_rows);
    }

    /// <summary>표시 여부를 전환한다. 숨김 상태에서는 레이아웃 비용도 없도록 display 를 끈다.</summary>
    public void SetVisibleState(bool visible)
    {
      style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    /// <summary>접속자 목록을 통째로 갱신한다.</summary>
    public void SetEntries(IReadOnlyList<Entry> entries)
    {
      int count = entries?.Count ?? 0;
      _headerLabel.text = $"접속자 ({count})";
      _rows.Clear();

      if (count == 0)
      {
        _rows.Add(CreateNoticeLabel("접속한 플레이어가 없습니다.", 12));
        return;
      }

      int shown = Mathf.Min(count, MaxVisibleEntries);
      for (int i = 0; i < shown; i++)
        _rows.Add(CreateRow(entries[i], i));

      if (count > shown)
        _rows.Add(CreateNoticeLabel($"외 {count - shown}명", 11));
    }

    private VisualElement CreateRow(Entry entry, int index)
    {
      var row = new VisualElement { pickingMode = PickingMode.Ignore };
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.justifyContent = Justify.SpaceBetween;
      row.style.paddingLeft = 8;
      row.style.paddingRight = 8;
      row.style.paddingTop = 3;
      row.style.paddingBottom = 3;
      row.style.borderTopLeftRadius = 4;
      row.style.borderTopRightRadius = 4;
      row.style.borderBottomLeftRadius = 4;
      row.style.borderBottomRightRadius = 4;

      // 줄 구분을 위해 홀수 행에만 옅은 배경을 깐다.
      if ((index & 1) == 1)
        row.style.backgroundColor = RowColor;

      var nameLabel = new Label(entry.DisplayName ?? string.Empty) { pickingMode = PickingMode.Ignore };
      nameLabel.style.color = entry.IsLocal ? LocalNameColor : NameColor;
      nameLabel.style.fontSize = 13;
      nameLabel.style.unityFontStyleAndWeight = entry.IsLocal ? FontStyle.Bold : FontStyle.Normal;
      nameLabel.style.flexShrink = 1;
      nameLabel.style.whiteSpace = WhiteSpace.NoWrap;
      nameLabel.style.overflow = Overflow.Hidden;
      nameLabel.style.textOverflow = TextOverflow.Ellipsis;
      row.Add(nameLabel);

      if (!string.IsNullOrEmpty(entry.Tags))
      {
        var tagLabel = new Label(entry.Tags) { pickingMode = PickingMode.Ignore };
        tagLabel.style.color = TagColor;
        tagLabel.style.fontSize = 11;
        tagLabel.style.marginLeft = 10;
        tagLabel.style.flexShrink = 0;
        tagLabel.style.whiteSpace = WhiteSpace.NoWrap;
        row.Add(tagLabel);
      }

      return row;
    }

    private Label CreateNoticeLabel(string text, int fontSize)
    {
      var label = new Label(text) { pickingMode = PickingMode.Ignore };
      label.style.color = SubduedColor;
      label.style.fontSize = fontSize;
      label.style.paddingLeft = 8;
      label.style.paddingRight = 8;
      label.style.paddingTop = 3;
      label.style.paddingBottom = 3;
      label.style.unityTextAlign = TextAnchor.MiddleCenter;
      return label;
    }
  }
}
