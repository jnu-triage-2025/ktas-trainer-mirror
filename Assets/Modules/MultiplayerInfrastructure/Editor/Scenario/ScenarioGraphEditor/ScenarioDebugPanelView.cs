using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>
  /// 시나리오 그래프 에디터 하단에 표시되는 디버그(진단) 패널.
  ///
  /// <para>
  /// <see cref="ScenarioGraphDiagnostics"/> 가 생성한 항목을 노드별로 그룹화하여 표시한다.
  /// 각 노드 그룹은 펼치기/접기가 가능하며, 헤더를 클릭하면 콜백(<see cref="OnNodeFocusRequested"/>)을
  /// 통해 그래프 뷰에서 해당 노드를 포커스할 수 있다.
  /// </para>
  ///
  /// <para>패널 전체의 접기/펼치기는 상단 헤더 클릭으로 토글된다.</para>
  /// </summary>
  public sealed class ScenarioDebugPanelView : VisualElement
  {
    // ── 색상 상수 ──────────────────────────────────────────────────────────────

    private static readonly Color ColorPanelBg     = new Color(0.13f, 0.13f, 0.13f, 1f);
    private static readonly Color ColorPanelHeader = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color ColorGroupHeader = new Color(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Color ColorGroupHover  = new Color(0.28f, 0.28f, 0.28f, 1f);
    private static readonly Color ColorError       = new Color(0.95f, 0.35f, 0.35f, 1f);
    private static readonly Color ColorWarning     = new Color(0.97f, 0.78f, 0.28f, 1f);
    private static readonly Color ColorInfo        = new Color(0.55f, 0.85f, 0.97f, 1f);
    private static readonly Color ColorBadgeError  = new Color(0.80f, 0.15f, 0.15f, 1f);
    private static readonly Color ColorBadgeWarn   = new Color(0.70f, 0.50f, 0.05f, 1f);
    private static readonly Color ColorBadgeInfo   = new Color(0.15f, 0.45f, 0.70f, 1f);
    private static readonly Color ColorDimText     = new Color(0.65f, 0.65f, 0.65f, 1f);
    private static readonly Color ColorGraphLevel  = new Color(0.70f, 0.90f, 0.70f, 1f);

    private const float PanelCollapsedHeight = 26f;
    private const float PanelExpandedHeight  = 180f;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────

    private bool _panelExpanded = true;
    private readonly Dictionary<string, bool> _groupExpanded = new();

    private readonly Label _panelToggleLabel;
    private readonly Label _summaryLabel;
    private readonly ScrollView _scrollView;
    private readonly VisualElement _listContainer;

    /// <summary>노드 포커스 요청 콜백. 인자: nodeIdentifier.</summary>
    public Action<string> OnNodeFocusRequested { get; set; }

    // ── 생성자 ────────────────────────────────────────────────────────────────

    public ScenarioDebugPanelView()
    {
      name = "ScenarioDebugPanel";
      style.flexShrink = 0f;
      style.flexDirection = FlexDirection.Column;
      style.backgroundColor = ColorPanelBg;
      style.borderTopWidth = 1f;
      style.borderTopColor = new Color(0.08f, 0.08f, 0.08f, 1f);

      // ── 헤더 바 ──
      var header = new VisualElement();
      header.style.flexDirection = FlexDirection.Row;
      header.style.alignItems = Align.Center;
      header.style.paddingLeft = 10f;
      header.style.paddingRight = 10f;
      header.style.height = PanelCollapsedHeight;
      header.style.backgroundColor = ColorPanelHeader;
      header.style.flexShrink = 0f;
      header.RegisterCallback<MouseEnterEvent>(_ => header.style.backgroundColor = ColorGroupHover);
      header.RegisterCallback<MouseLeaveEvent>(_ => header.style.backgroundColor = ColorPanelHeader);
      header.RegisterCallback<ClickEvent>(_ => TogglePanel());

      _panelToggleLabel = new Label("▼  Debug Panel");
      _panelToggleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _panelToggleLabel.style.fontSize = 11f;
      _panelToggleLabel.style.color = Color.white;

      _summaryLabel = new Label();
      _summaryLabel.style.fontSize = 10f;
      _summaryLabel.style.color = ColorDimText;
      _summaryLabel.style.marginLeft = 10f;
      _summaryLabel.style.flexGrow = 1f;

      header.Add(_panelToggleLabel);
      header.Add(_summaryLabel);
      Add(header);

      // ── 스크롤 영역 ──
      _scrollView = new ScrollView(ScrollViewMode.Vertical);
      _scrollView.style.flexGrow = 1f;
      _scrollView.style.height = PanelExpandedHeight;

      _listContainer = new VisualElement();
      _listContainer.style.flexDirection = FlexDirection.Column;
      _listContainer.style.paddingBottom = 6f;
      _scrollView.Add(_listContainer);
      Add(_scrollView);

      // 초기 빈 상태
      SetSummary(0, 0, 0);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// 진단 결과를 패널에 반영한다. <see cref="ScenarioGraphDiagnostics.Run"/> 결과를 전달한다.
    /// </summary>
    public void Refresh(IReadOnlyList<ScenarioGraphDiagnostics.DiagnosticItem> items)
    {
      _listContainer.Clear();

      if (items == null || items.Count == 0)
      {
        SetSummary(0, 0, 0);
        var emptyLabel = new Label("  진단 항목이 없습니다.")
        {
          style = { color = ColorDimText, fontSize = 10f, marginTop = 6f, marginLeft = 10f }
        };
        _listContainer.Add(emptyLabel);
        return;
      }

      // 노드별 그룹화 (그래프 수준 항목은 "(graph)" 키)
      var grouped = items
        .GroupBy(i => i.NodeIdentifier ?? "(unknown)")
        .OrderBy(g => g.Key)
        .ToList();

      int totalErrors   = items.Count(i => i.Severity == ScenarioGraphDiagnostics.Severity.Error);
      int totalWarnings = items.Count(i => i.Severity == ScenarioGraphDiagnostics.Severity.Warning);
      int totalInfos    = items.Count(i => i.Severity == ScenarioGraphDiagnostics.Severity.Info);
      SetSummary(totalErrors, totalWarnings, totalInfos);

      foreach (var group in grouped)
      {
        _listContainer.Add(BuildGroup(group.Key, group.ToList()));
      }
    }

    // ── 패널 토글 ─────────────────────────────────────────────────────────────

    private void TogglePanel()
    {
      _panelExpanded = !_panelExpanded;
      _panelToggleLabel.text = (_panelExpanded ? "▼" : "▶") + "  Debug Panel";
      _scrollView.style.display = _panelExpanded ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ── 그룹 빌더 ─────────────────────────────────────────────────────────────

    private VisualElement BuildGroup(string nodeId, List<ScenarioGraphDiagnostics.DiagnosticItem> groupItems)
    {
      if (!_groupExpanded.ContainsKey(nodeId))
        _groupExpanded[nodeId] = true;

      bool isGraphLevel = nodeId == "(graph)";
      int errorCount   = groupItems.Count(i => i.Severity == ScenarioGraphDiagnostics.Severity.Error);
      int warnCount    = groupItems.Count(i => i.Severity == ScenarioGraphDiagnostics.Severity.Warning);
      int infoCount    = groupItems.Count(i => i.Severity == ScenarioGraphDiagnostics.Severity.Info);

      var wrapper = new VisualElement();
      wrapper.style.marginBottom = 1f;

      // ── 그룹 헤더 ──
      var groupHeader = new VisualElement();
      groupHeader.style.flexDirection = FlexDirection.Row;
      groupHeader.style.alignItems = Align.Center;
      groupHeader.style.paddingLeft = 6f;
      groupHeader.style.paddingRight = 6f;
      groupHeader.style.paddingTop = 3f;
      groupHeader.style.paddingBottom = 3f;
      groupHeader.style.backgroundColor = ColorGroupHeader;
      groupHeader.RegisterCallback<MouseEnterEvent>(_ => groupHeader.style.backgroundColor = ColorGroupHover);
      groupHeader.RegisterCallback<MouseLeaveEvent>(_ => groupHeader.style.backgroundColor = ColorGroupHeader);

      // 토글 화살표
      var arrowLabel = new Label(_groupExpanded[nodeId] ? "▼" : "▶");
      arrowLabel.style.fontSize = 9f;
      arrowLabel.style.color = ColorDimText;
      arrowLabel.style.marginRight = 4f;
      arrowLabel.style.width = 10f;

      // 노드 ID
      var idLabel = new Label(nodeId);
      idLabel.style.fontSize = 10f;
      idLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      idLabel.style.color = isGraphLevel ? ColorGraphLevel : Color.white;
      idLabel.style.flexGrow = 1f;

      groupHeader.Add(arrowLabel);
      groupHeader.Add(idLabel);

      // 배지들 (Error / Warning / Info 카운트)
      if (errorCount > 0)   groupHeader.Add(BuildBadge($"✕ {errorCount}", ColorBadgeError));
      if (warnCount > 0)    groupHeader.Add(BuildBadge($"⚠ {warnCount}", ColorBadgeWarn));
      if (infoCount > 0)    groupHeader.Add(BuildBadge($"ℹ {infoCount}", ColorBadgeInfo));

      // 포커스 버튼 (그래프 수준 항목 제외)
      if (!isGraphLevel)
      {
        var focusBtn = new Label("[→]");
        focusBtn.style.fontSize = 9f;
        focusBtn.style.color = new Color(0.5f, 0.85f, 1f, 1f);
        focusBtn.style.marginLeft = 6f;
        focusBtn.RegisterCallback<ClickEvent>(evt =>
        {
          evt.StopPropagation();
          OnNodeFocusRequested?.Invoke(nodeId);
        });
        focusBtn.RegisterCallback<MouseEnterEvent>(_ => focusBtn.style.color = Color.white);
        focusBtn.RegisterCallback<MouseLeaveEvent>(_ => focusBtn.style.color = new Color(0.5f, 0.85f, 1f, 1f));
        groupHeader.Add(focusBtn);
      }

      // ── 항목 컨테이너 ──
      var itemsContainer = new VisualElement();
      itemsContainer.style.paddingLeft = 20f;
      itemsContainer.style.paddingTop = 2f;
      itemsContainer.style.paddingBottom = 2f;
      itemsContainer.style.display = _groupExpanded[nodeId] ? DisplayStyle.Flex : DisplayStyle.None;

      foreach (var item in groupItems)
        itemsContainer.Add(BuildItemRow(item));

      // 헤더 클릭 시 항목 토글
      groupHeader.RegisterCallback<ClickEvent>(_ =>
      {
        _groupExpanded[nodeId] = !_groupExpanded[nodeId];
        arrowLabel.text = _groupExpanded[nodeId] ? "▼" : "▶";
        itemsContainer.style.display = _groupExpanded[nodeId] ? DisplayStyle.Flex : DisplayStyle.None;
      });

      wrapper.Add(groupHeader);
      wrapper.Add(itemsContainer);
      return wrapper;
    }

    private static VisualElement BuildItemRow(ScenarioGraphDiagnostics.DiagnosticItem item)
    {
      var row = new VisualElement();
      row.style.flexDirection = FlexDirection.Row;
      row.style.alignItems = Align.Center;
      row.style.paddingTop = 1f;
      row.style.paddingBottom = 1f;

      var (icon, color) = item.Severity switch
      {
        ScenarioGraphDiagnostics.Severity.Error   => ("✕", ColorError),
        ScenarioGraphDiagnostics.Severity.Warning => ("⚠", ColorWarning),
        _                                          => ("ℹ", ColorInfo),
      };

      var iconLabel = new Label(icon);
      iconLabel.style.fontSize = 9f;
      iconLabel.style.color = color;
      iconLabel.style.marginRight = 5f;
      iconLabel.style.width = 10f;

      var msgLabel = new Label(item.Message);
      msgLabel.style.fontSize = 10f;
      msgLabel.style.color = color;
      msgLabel.style.whiteSpace = WhiteSpace.Normal;
      msgLabel.style.flexGrow = 1f;

      row.Add(iconLabel);
      row.Add(msgLabel);
      return row;
    }

    private static VisualElement BuildBadge(string text, Color bgColor)
    {
      var badge = new Label(text);
      badge.style.fontSize = 9f;
      badge.style.color = Color.white;
      badge.style.backgroundColor = bgColor;
      badge.style.paddingLeft = 4f;
      badge.style.paddingRight = 4f;
      badge.style.paddingTop = 1f;
      badge.style.paddingBottom = 1f;
      badge.style.marginLeft = 3f;
      badge.style.borderTopLeftRadius = 3f;
      badge.style.borderTopRightRadius = 3f;
      badge.style.borderBottomLeftRadius = 3f;
      badge.style.borderBottomRightRadius = 3f;
      return badge;
    }

    // ── 요약 표시 ─────────────────────────────────────────────────────────────

    private void SetSummary(int errors, int warnings, int infos)
    {
      if (errors == 0 && warnings == 0 && infos == 0)
      {
        _summaryLabel.text = "이상 없음";
        _summaryLabel.style.color = new Color(0.45f, 0.90f, 0.45f, 1f);
        return;
      }

      var parts = new List<string>();
      if (errors > 0)   parts.Add($"✕ {errors} error{(errors > 1 ? "s" : "")}");
      if (warnings > 0) parts.Add($"⚠ {warnings} warning{(warnings > 1 ? "s" : "")}");
      if (infos > 0)    parts.Add($"ℹ {infos} info");
      _summaryLabel.text = string.Join("   ", parts);
      _summaryLabel.style.color = errors > 0 ? ColorError : ColorWarning;
    }
  }
}
