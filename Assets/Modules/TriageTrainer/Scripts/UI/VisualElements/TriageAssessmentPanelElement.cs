using System;
using System.Collections.Generic;
using TriageTrainer.Entity.Patient;
using UnityEngine;
using UnityEngine.UIElements;

namespace TriageTrainer.UI
{
  /// <summary>
  /// 트리아지 평가 패널: 가로로 늘어진 KTAS 색상 사각형들을 표시한다.
  ///
  /// <para>
  /// 각 사각형은 (1) 해당 트리아지 등급 색으로 칠해지고 (2) 색 명칭과 이름(예: "KTAS 2", "2단계 긴급")이
  /// 표기된다. 사각형을 클릭하면 <see cref="LevelSelected"/> 이벤트로 선택된 등급을 통지한다.
  /// </para>
  /// </summary>
  public sealed class TriageAssessmentPanelElement : VisualElement
  {
    /// <summary>사각형 클릭으로 등급이 선택되었을 때.</summary>
    public event Action<TriageLevel> LevelSelected;

    /// <summary>취소(닫기) 요청 시.</summary>
    public event Action CancelRequested;

    private readonly VisualElement _row;
    private readonly Dictionary<TriageLevel, VisualElement> _squaresByLevel = new();

    public TriageAssessmentPanelElement()
    {
      // 전체화면 반투명 backdrop.
      style.position = Position.Absolute;
      style.left = 0;
      style.top = 0;
      style.right = 0;
      style.bottom = 0;
      style.flexDirection = FlexDirection.Column;
      style.alignItems = Align.Center;
      style.justifyContent = Justify.Center;
      style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);

      var title = new Label("트리아지 분류");
      title.style.color = Color.white;
      title.style.fontSize = 26;
      title.style.unityFontStyleAndWeight = FontStyle.Bold;
      title.style.marginBottom = 6;
      title.style.unityTextOutlineWidth = 0.75f;
      title.style.unityTextOutlineColor = new Color(0f, 0f, 0f, 0.8f);
      Add(title);

      var hint = new Label("환자 상태에 맞는 등급을 선택하세요.");
      hint.style.color = new Color(0.9f, 0.9f, 0.9f, 1f);
      hint.style.fontSize = 15;
      hint.style.marginBottom = 20;
      Add(hint);

      _row = new VisualElement();
      _row.style.flexDirection = FlexDirection.Row;
      _row.style.alignItems = Align.Center;
      _row.style.justifyContent = Justify.Center;
      Add(_row);

      var cancel = new Button(() => CancelRequested?.Invoke()) { text = "취소" };
      cancel.style.marginTop = 24;
      cancel.style.paddingLeft = 20;
      cancel.style.paddingRight = 20;
      cancel.style.paddingTop = 8;
      cancel.style.paddingBottom = 8;
      cancel.style.fontSize = 16;
      Add(cancel);

      BuildSquares();
    }

    private void BuildSquares()
    {
      _row.Clear();
      _squaresByLevel.Clear();

      foreach (var level in TriageLevelInfo.SelectableLevels)
      {
        var square = BuildSquare(level);
        _squaresByLevel[level] = square;
        _row.Add(square);
      }
    }

    /// <summary>현재(기존) 평가 등급을 선택 상태로 강조 표시한다. Unassessed 면 강조 없음.</summary>
    public void SetCurrentSelection(TriageLevel current)
    {
      foreach (var kvp in _squaresByLevel)
      {
        bool selected = kvp.Key == current;
        var sq = kvp.Value;
        var border = selected ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        float width = selected ? 4 : 2;
        sq.style.borderLeftColor = border;
        sq.style.borderRightColor = border;
        sq.style.borderTopColor = border;
        sq.style.borderBottomColor = border;
        sq.style.borderLeftWidth = width;
        sq.style.borderRightWidth = width;
        sq.style.borderTopWidth = width;
        sq.style.borderBottomWidth = width;
      }
    }

    private VisualElement BuildSquare(TriageLevel level)
    {
      var color = TriageLevelInfo.GetColor(level);
      var textColor = TriageLevelInfo.GetTextColor(level);

      var square = new VisualElement();
      square.style.width = 150;
      square.style.height = 150;
      square.style.marginLeft = 8;
      square.style.marginRight = 8;
      square.style.backgroundColor = color;
      square.style.flexDirection = FlexDirection.Column;
      square.style.alignItems = Align.Center;
      square.style.justifyContent = Justify.Center;
      square.style.borderTopLeftRadius = 10;
      square.style.borderTopRightRadius = 10;
      square.style.borderBottomLeftRadius = 10;
      square.style.borderBottomRightRadius = 10;
      square.style.borderLeftWidth = 2;
      square.style.borderRightWidth = 2;
      square.style.borderTopWidth = 2;
      square.style.borderBottomWidth = 2;
      var border = new Color(1f, 1f, 1f, 0.5f);
      square.style.borderLeftColor = border;
      square.style.borderRightColor = border;
      square.style.borderTopColor = border;
      square.style.borderBottomColor = border;

      var shortLabel = new Label(TriageLevelInfo.GetShortLabel(level));
      shortLabel.style.color = textColor;
      shortLabel.style.fontSize = 20;
      shortLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      shortLabel.pickingMode = PickingMode.Ignore;
      square.Add(shortLabel);

      var nameLabel = new Label(TriageLevelInfo.GetDisplayName(level));
      nameLabel.style.color = textColor;
      nameLabel.style.fontSize = 15;
      nameLabel.style.marginTop = 6;
      nameLabel.style.whiteSpace = WhiteSpace.Normal;
      nameLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
      nameLabel.pickingMode = PickingMode.Ignore;
      square.Add(nameLabel);

      square.RegisterCallback<ClickEvent>(_ => LevelSelected?.Invoke(level));

      // 호버 강조.
      square.RegisterCallback<MouseEnterEvent>(_ => square.style.scale = new Scale(new Vector3(1.06f, 1.06f, 1f)));
      square.RegisterCallback<MouseLeaveEvent>(_ => square.style.scale = new Scale(Vector3.one));

      return square;
    }
  }
}
