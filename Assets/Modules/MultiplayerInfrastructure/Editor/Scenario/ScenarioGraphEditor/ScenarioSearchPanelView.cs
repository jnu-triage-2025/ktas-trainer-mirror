using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>
  /// 시나리오 그래프 에디터의 플로팅 검색 패널.
  ///
  /// <para>
  /// 그래프 캔버스 위에 절대 위치로 오버레이된다.
  /// Cmd/Ctrl+F 로 열고, Escape 로 닫는다.
  /// </para>
  ///
  /// <para>
  /// 검색 결과를 클릭하면 <see cref="OnResultSelected"/> 콜백을 통해
  /// 에디터 윈도우에서 해당 노드를 포커스한다.
  /// </para>
  /// </summary>
  public sealed class ScenarioSearchPanelView : VisualElement
  {
    // ── 색상 / 크기 상수 ──────────────────────────────────────────────────────

    private static readonly Color ColorBg          = new Color(0.15f, 0.15f, 0.15f, 0.97f);
    private static readonly Color ColorBorder       = new Color(0.08f, 0.08f, 0.08f, 1f);
    private static readonly Color ColorHeader       = new Color(0.20f, 0.20f, 0.20f, 1f);
    private static readonly Color ColorRow          = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color ColorRowHover     = new Color(0.26f, 0.26f, 0.26f, 1f);
    private static readonly Color ColorRowSelected  = new Color(0.20f, 0.38f, 0.62f, 1f);
    private static readonly Color ColorNodeId       = new Color(0.85f, 0.85f, 0.85f, 1f);
    private static readonly Color ColorNodeType     = new Color(0.55f, 0.85f, 0.97f, 1f);
    private static readonly Color ColorField        = new Color(0.60f, 0.60f, 0.60f, 1f);
    private static readonly Color ColorMatch        = new Color(0.97f, 0.85f, 0.35f, 1f);
    private static readonly Color ColorDim          = new Color(0.50f, 0.50f, 0.50f, 1f);
    private static readonly Color ColorCloseBtn     = new Color(0.55f, 0.55f, 0.55f, 1f);

    private const float PanelWidth       = 480f;
    private const float PanelMaxHeight   = 420f;
    private const float InputHeight      = 20f;
    private const float RowHeight        = 40f;
    private const float PanelOffsetRight = 16f;
    private const float PanelOffsetTop   = 8f;   // toolbar 아래 여백

    // ── 상태 ──────────────────────────────────────────────────────────────────

    private readonly TextField  _searchField;
    private readonly Label      _countLabel;
    private readonly ScrollView _resultScroll;
    private readonly VisualElement _resultList;

    private IReadOnlyList<ScenarioNodeSearcher.SearchResult> _results
        = Array.Empty<ScenarioNodeSearcher.SearchResult>();
    private int _selectedIndex = -1;

    /// <summary>결과 항목 클릭 시 호출. 인자: nodeIdentifier.</summary>
    public Action<string> OnResultSelected { get; set; }

    /// <summary>검색어가 변경될 때마다 호출. 인자: query.</summary>
    public Action<string> OnQueryChanged { get; set; }

    /// <summary>패널이 닫힐 때 호출.</summary>
    public Action OnClosed { get; set; }

    // ── 생성자 ────────────────────────────────────────────────────────────────

    public ScenarioSearchPanelView()
    {
      name = "ScenarioSearchPanel";

      // ── 패널 크기/위치 (절대 오버레이) ──
      style.position        = Position.Absolute;
      style.width           = PanelWidth;
      style.right           = PanelOffsetRight;
      style.top             = PanelOffsetTop;
      style.flexDirection   = FlexDirection.Column;
      style.backgroundColor = ColorBg;
      style.borderTopWidth = style.borderBottomWidth =
        style.borderLeftWidth = style.borderRightWidth = 1f;
      style.borderTopColor = style.borderBottomColor =
        style.borderLeftColor = style.borderRightColor = ColorBorder;
      style.borderTopLeftRadius     = 5f;
      style.borderTopRightRadius    = 5f;
      style.borderBottomLeftRadius  = 5f;
      style.borderBottomRightRadius = 5f;
      // 깊이: 그래프 캔버스보다 앞에 오도록
      style.display = DisplayStyle.None;

      // ── 헤더 (검색 입력 + 닫기 버튼) ──
      var header = new VisualElement();
      header.style.flexDirection   = FlexDirection.Row;
      header.style.alignItems      = Align.Center;
      header.style.height          = InputHeight + 8f;
      header.style.paddingLeft     = 10f;
      header.style.paddingRight    = 6f;
      header.style.paddingTop      = 4f;
      header.style.paddingBottom   = 4f;
      header.style.backgroundColor = ColorHeader;
      header.style.borderBottomWidth = 1f;
      header.style.borderBottomColor = ColorBorder;

      // 검색 TextField
      _searchField = new TextField();
      _searchField.style.flexGrow  = 1f;
      _searchField.style.height    = InputHeight;
      _searchField.style.fontSize  = 12f;
      // 내부 input 요소 스타일
      _searchField.Q<VisualElement>("unity-text-input").style.backgroundColor
          = new Color(0.10f, 0.10f, 0.10f, 1f);
      _searchField.RegisterValueChangedCallback(evt => HandleQueryChanged(evt.newValue));
      _searchField.RegisterCallback<KeyDownEvent>(OnSearchFieldKeyDown);
      header.Add(_searchField);

      // 결과 수 레이블
      _countLabel = new Label();
      _countLabel.style.fontSize   = 10f;
      _countLabel.style.color      = ColorDim;
      _countLabel.style.marginLeft = 8f;
      _countLabel.style.minWidth   = 48f;
      _countLabel.style.unityTextAlign = TextAnchor.MiddleRight;
      header.Add(_countLabel);

      // 닫기 버튼
      var closeBtn = new Label("✕");
      closeBtn.style.fontSize    = 12f;
      closeBtn.style.color       = ColorCloseBtn;
      closeBtn.style.marginLeft  = 8f;
      closeBtn.style.paddingLeft = closeBtn.style.paddingRight = 4f;
      closeBtn.RegisterCallback<ClickEvent>(_ => Close());
      closeBtn.RegisterCallback<MouseEnterEvent>(_ => closeBtn.style.color = Color.white);
      closeBtn.RegisterCallback<MouseLeaveEvent>(_ => closeBtn.style.color = ColorCloseBtn);
      header.Add(closeBtn);
      Add(header);

      // ── 결과 목록 ──
      _resultScroll = new ScrollView(ScrollViewMode.Vertical);
      _resultScroll.style.maxHeight = PanelMaxHeight - (InputHeight + 8f);
      _resultScroll.style.flexGrow  = 1f;

      _resultList = new VisualElement();
      _resultList.style.flexDirection = FlexDirection.Column;
      _resultScroll.Add(_resultList);
      Add(_resultScroll);

      // 전체 클릭 전파 차단 (아래 GraphView 에 전달되지 않도록)
      RegisterCallback<MouseDownEvent>(evt => evt.StopPropagation());
      RegisterCallback<WheelEvent>(evt => evt.StopPropagation());
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>패널을 열고 검색 필드에 포커스를 준다.</summary>
    public void Open()
    {
      style.display = DisplayStyle.Flex;
      // 다음 프레임에 포커스 (UIElements 제약)
      schedule.Execute(() => _searchField.Focus()).ExecuteLater(1);
    }

    /// <summary>패널을 닫는다.</summary>
    public void Close()
    {
      style.display = DisplayStyle.None;
      _searchField.SetValueWithoutNotify(string.Empty);
      ClearResults();
      OnClosed?.Invoke();
    }

    public bool IsOpen => style.display == DisplayStyle.Flex;

    /// <summary>
    /// 외부에서 검색 결과를 주입한다.
    /// <see cref="OnQueryChanged"/> 콜백을 받은 에디터 윈도우가 검색 후 호출한다.
    /// </summary>
    public void SetResults(IReadOnlyList<ScenarioNodeSearcher.SearchResult> results, string query)
    {
      _results       = results ?? Array.Empty<ScenarioNodeSearcher.SearchResult>();
      _selectedIndex = -1;
      RebuildResultList(query);
      UpdateCountLabel();
    }

    // ── 내부 로직 ─────────────────────────────────────────────────────────────

    private void HandleQueryChanged(string query)
    {
      if (string.IsNullOrWhiteSpace(query))
      {
        ClearResults();
        UpdateCountLabel();
        return;
      }
      OnQueryChanged?.Invoke(query);
    }

    private void OnSearchFieldKeyDown(KeyDownEvent evt)
    {
      switch (evt.keyCode)
      {
        case KeyCode.Escape:
          Close();
          evt.StopPropagation();
          break;
        case KeyCode.Return:
        case KeyCode.KeypadEnter:
          SelectCurrent();
          evt.StopPropagation();
          break;
        case KeyCode.DownArrow:
          MoveSelection(+1);
          evt.StopPropagation();
          break;
        case KeyCode.UpArrow:
          MoveSelection(-1);
          evt.StopPropagation();
          break;
      }
    }

    private void MoveSelection(int dir)
    {
      if (_results.Count == 0) return;
      _selectedIndex = Mathf.Clamp(_selectedIndex + dir, 0, _results.Count - 1);
      RefreshSelectionHighlight();
      ScrollToSelected();
    }

    private void SelectCurrent()
    {
      if (_selectedIndex < 0 || _selectedIndex >= _results.Count) return;
      OnResultSelected?.Invoke(_results[_selectedIndex].NodeIdentifier);
    }

    private void RefreshSelectionHighlight()
    {
      var rows = _resultList.Children();
      int i = 0;
      foreach (var row in rows)
      {
        row.style.backgroundColor = (i == _selectedIndex) ? ColorRowSelected : ColorRow;
        i++;
      }
    }

    private void ScrollToSelected()
    {
      if (_selectedIndex < 0) return;
      var rows = _resultList.Children();
      int i = 0;
      foreach (var row in rows)
      {
        if (i == _selectedIndex)
        {
          // ScrollView.ScrollTo(element) 로 해당 행이 보이도록 스크롤한다.
          _resultScroll.ScrollTo(row);
          break;
        }
        i++;
      }
    }

    private void ClearResults()
    {
      _results       = Array.Empty<ScenarioNodeSearcher.SearchResult>();
      _selectedIndex = -1;
      _resultList.Clear();
    }

    private void UpdateCountLabel()
    {
      if (_results.Count == 0)
        _countLabel.text = string.Empty;
      else
        _countLabel.text = $"{_results.Count}개";
    }

    // ── 결과 행 빌더 ──────────────────────────────────────────────────────────

    private void RebuildResultList(string query)
    {
      _resultList.Clear();

      if (_results.Count == 0)
      {
        if (!string.IsNullOrWhiteSpace(query))
        {
          var empty = new Label("  결과 없음");
          empty.style.color    = ColorDim;
          empty.style.fontSize = 11f;
          empty.style.paddingTop = empty.style.paddingBottom = 8f;
          empty.style.paddingLeft = 12f;
          _resultList.Add(empty);
        }
        return;
      }

      for (int i = 0; i < _results.Count; i++)
      {
        var result = _results[i];
        var capturedIndex = i;
        var capturedId    = result.NodeIdentifier;

        var row = new VisualElement();
        row.style.flexDirection  = FlexDirection.Row;
        row.style.alignItems     = Align.Center;
        row.style.minHeight      = RowHeight;
        row.style.paddingLeft    = 10f;
        row.style.paddingRight   = 10f;
        row.style.paddingTop     = 4f;
        row.style.paddingBottom  = 4f;
        row.style.backgroundColor = ColorRow;
        row.style.borderBottomWidth = 1f;
        row.style.borderBottomColor = ColorBorder;

        // 호버 / 선택 하이라이트
        row.RegisterCallback<MouseEnterEvent>(_ =>
        {
          if (capturedIndex != _selectedIndex)
            row.style.backgroundColor = ColorRowHover;
        });
        row.RegisterCallback<MouseLeaveEvent>(_ =>
        {
          row.style.backgroundColor = (capturedIndex == _selectedIndex)
              ? ColorRowSelected : ColorRow;
        });
        row.RegisterCallback<MouseDownEvent>(evt =>
        {
          _selectedIndex = capturedIndex;
          RefreshSelectionHighlight();
          OnResultSelected?.Invoke(capturedId);
          evt.StopPropagation();
        });

        // 왼쪽: 노드 ID + 타입
        var left = new VisualElement();
        left.style.flexDirection = FlexDirection.Column;
        left.style.flexGrow      = 1f;
        left.style.overflow      = Overflow.Hidden;

        var nodeIdLabel = new Label(result.NodeIdentifier);
        nodeIdLabel.style.fontSize   = 11f;
        nodeIdLabel.style.color      = ColorNodeId;
        nodeIdLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        nodeIdLabel.style.overflow   = Overflow.Hidden;

        var typeLabel = new Label(result.NodeTypeName);
        typeLabel.style.fontSize = 9f;
        typeLabel.style.color    = ColorNodeType;

        left.Add(nodeIdLabel);
        left.Add(typeLabel);
        row.Add(left);

        // 오른쪽: 필드명 + 값(일치 강조)
        var right = new VisualElement();
        right.style.flexDirection  = FlexDirection.Column;
        right.style.alignItems     = Align.FlexEnd;
        right.style.maxWidth       = 200f;
        right.style.overflow       = Overflow.Hidden;

        var fieldLabel = new Label(result.FieldName);
        fieldLabel.style.fontSize  = 9f;
        fieldLabel.style.color     = ColorField;
        fieldLabel.style.overflow  = Overflow.Hidden;

        var valueLabel = new Label(TruncateMiddle(result.FieldValue, 36));
        valueLabel.style.fontSize  = 10f;
        valueLabel.style.color     = ColorMatch;
        valueLabel.style.overflow  = Overflow.Hidden;
        valueLabel.style.unityTextAlign = TextAnchor.MiddleRight;

        right.Add(fieldLabel);
        right.Add(valueLabel);
        row.Add(right);

        _resultList.Add(row);
      }
    }

    /// <summary>
    /// 긴 문자열을 중간을 생략하여 최대 <paramref name="max"/>자로 줄인다.
    /// 예: "some very long text" → "some ver…long text"
    /// </summary>
    private static string TruncateMiddle(string s, int max)
    {
      if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
      int half = max / 2;
      return s.Substring(0, half) + "…" + s.Substring(s.Length - half);
    }
  }
}
