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
  ///
  /// <para>헤더 위 리사이즈 핸들을 수직 드래그하여 패널 높이를 조절할 수 있다.</para>
  ///
  /// <para>
  /// <b>레이아웃 설계:</b>
  /// 이 패널은 <c>rootVisualElement</c>(FlexDirection.Column)에 직접 추가되며,
  /// <c>style.height</c>를 명시적으로 지정(<c>flexGrow=0, flexShrink=0</c>)해서
  /// 위쪽 mainContainer(<c>flexGrow=1</c>)가 나머지 공간을 차지하도록 한다.
  /// 드래그 시에는 이 패널의 <c>style.height</c>를 직접 변경하여 레이아웃을 갱신한다.
  /// </para>
  /// </summary>
  public sealed class ScenarioDebugPanelView : VisualElement
  {
    // ── 색상 상수 ──────────────────────────────────────────────────────────────

    private static readonly Color ColorPanelBg       = new Color(0.13f, 0.13f, 0.13f, 1f);
    private static readonly Color ColorPanelHeader   = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color ColorGroupHeader   = new Color(0.22f, 0.22f, 0.22f, 1f);
    private static readonly Color ColorGroupHover    = new Color(0.28f, 0.28f, 0.28f, 1f);
    private static readonly Color ColorError         = new Color(0.95f, 0.35f, 0.35f, 1f);
    private static readonly Color ColorWarning       = new Color(0.97f, 0.78f, 0.28f, 1f);
    private static readonly Color ColorInfo          = new Color(0.55f, 0.85f, 0.97f, 1f);
    private static readonly Color ColorBadgeError    = new Color(0.80f, 0.15f, 0.15f, 1f);
    private static readonly Color ColorBadgeWarn     = new Color(0.70f, 0.50f, 0.05f, 1f);
    private static readonly Color ColorBadgeInfo     = new Color(0.15f, 0.45f, 0.70f, 1f);
    private static readonly Color ColorDimText       = new Color(0.65f, 0.65f, 0.65f, 1f);
    private static readonly Color ColorGraphLevel    = new Color(0.70f, 0.90f, 0.70f, 1f);
    // 리사이즈 핸들: 어두운 배경과 구별되는 중간 밝기 + 호버/드래그 시 파란색 강조
    private static readonly Color ColorResizeIdle    = new Color(0.30f, 0.30f, 0.30f, 1f);
    private static readonly Color ColorResizeHover   = new Color(0.40f, 0.65f, 1.00f, 1f);
    private static readonly Color ColorResizeDrag    = new Color(0.50f, 0.80f, 1.00f, 1f);

    // 헤더 높이 (접힌 상태에서 패널 전체 높이와 같음)
    private const float HeaderHeight         = 26f;
    private const float PanelExpandedDefault = 180f;
    private const float PanelExpandedMin     = 60f;
    private const float PanelExpandedMax     = 600f;
    // 리사이즈 핸들 높이: 충분히 클릭 가능하게 6px, 시각적으로는 중앙 2px 줄로만 표현
    private const float ResizeHandleHeight   = 6f;

    // ── 내부 상태 ─────────────────────────────────────────────────────────────

    private bool  _panelExpanded  = true;
    private float _expandedHeight = PanelExpandedDefault;

    private readonly Dictionary<string, bool> _groupExpanded = new();

    private readonly Label         _panelToggleLabel;
    private readonly Label         _summaryLabel;
    private readonly Label         _heightHintLabel;
    private readonly ScrollView    _scrollView;
    private readonly VisualElement _listContainer;
    private readonly VisualElement _resizeHandle;

    /// <summary>노드 포커스 요청 콜백. 인자: nodeIdentifier.</summary>
    public Action<string> OnNodeFocusRequested { get; set; }

    // ── 생성자 ────────────────────────────────────────────────────────────────

    public ScenarioDebugPanelView()
    {
      name = "ScenarioDebugPanel";

      // ── 패널 자체 크기를 명시적으로 고정 ──
      // flexGrow=0, flexShrink=0 으로 mainContainer(flexGrow=1)가 나머지를 차지하게 한다.
      // height 는 ResizeHandle 드래그 시 직접 갱신된다.
      style.flexGrow      = 0f;
      style.flexShrink    = 0f;
      style.flexDirection = FlexDirection.Column;
      style.overflow      = Overflow.Hidden;
      style.backgroundColor = ColorPanelBg;

      // 패널 전체 높이 = 핸들(6) + 헤더(26) + 스크롤 영역(expandedHeight)
      ApplyTotalHeight();

      // ── 리사이즈 핸들 ──
      // 6px 높이의 클릭 영역. 가운데 2px 줄로 시각화해 "경계"임을 암시.
      _resizeHandle = BuildResizeHandle();
      Add(_resizeHandle);

      // 리사이즈 핸들과 패널 본체 사이 구분선
      var topBorder = new VisualElement();
      topBorder.style.height          = 1f;
      topBorder.style.flexShrink      = 0f;
      topBorder.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
      Add(topBorder);

      // ── 헤더 바 ──
      var header = new VisualElement();
      header.style.flexDirection   = FlexDirection.Row;
      header.style.alignItems      = Align.Center;
      header.style.paddingLeft     = 10f;
      header.style.paddingRight    = 10f;
      header.style.height          = HeaderHeight;
      header.style.flexShrink      = 0f;
      header.style.backgroundColor = ColorPanelHeader;
      header.RegisterCallback<MouseEnterEvent>(_ => header.style.backgroundColor = ColorGroupHover);
      header.RegisterCallback<MouseLeaveEvent>(_ => header.style.backgroundColor = ColorPanelHeader);
      header.RegisterCallback<ClickEvent>(_ => TogglePanel());

      _panelToggleLabel = new Label("▼  Debug Panel");
      _panelToggleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
      _panelToggleLabel.style.fontSize = 11f;
      _panelToggleLabel.style.color    = Color.white;

      _summaryLabel = new Label();
      _summaryLabel.style.fontSize   = 10f;
      _summaryLabel.style.color      = ColorDimText;
      _summaryLabel.style.marginLeft = 10f;
      _summaryLabel.style.flexGrow   = 1f;

      _heightHintLabel = new Label();
      _heightHintLabel.style.fontSize   = 9f;
      _heightHintLabel.style.color      = ColorDimText;
      _heightHintLabel.style.marginLeft = 6f;

      header.Add(_panelToggleLabel);
      header.Add(_summaryLabel);
      header.Add(_heightHintLabel);
      Add(header);

      // ── 스크롤 영역 ──
      // flexGrow/flexShrink 를 쓰지 않고 height 를 명시 지정한다.
      // 패널 자체 height 가 고정값이므로 ScrollView 도 고정값이어야 레이아웃이 안정된다.
      _scrollView = new ScrollView(ScrollViewMode.Vertical);
      _scrollView.style.flexGrow   = 0f;
      _scrollView.style.flexShrink = 0f;
      _scrollView.style.height     = _expandedHeight;

      _listContainer = new VisualElement();
      _listContainer.style.flexDirection = FlexDirection.Column;
      _listContainer.style.paddingBottom = 6f;
      _scrollView.Add(_listContainer);
      Add(_scrollView);

      SetSummary(0, 0, 0);
      UpdateHeightHint();
    }

    // ── 리사이즈 핸들 빌더 ────────────────────────────────────────────────────

    private VisualElement BuildResizeHandle()
    {
      // 외부 컨테이너: 클릭 가능 영역 (6px)
      var handle = new VisualElement { name = "DebugPanelResizeHandle" };
      handle.style.height          = ResizeHandleHeight;
      handle.style.flexShrink      = 0f;
      handle.style.flexDirection   = FlexDirection.Column;
      handle.style.justifyContent  = Justify.Center;
      handle.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);

      // 내부 시각화 줄 (2px, 가운데 정렬)
      var line = new VisualElement { name = "DebugPanelResizeLine" };
      line.style.height          = 2f;
      line.style.flexShrink      = 0f;
      line.style.marginLeft      = 40f;
      line.style.marginRight     = 40f;
      line.style.backgroundColor = ColorResizeIdle;
      line.style.borderTopLeftRadius     = 1f;
      line.style.borderTopRightRadius    = 1f;
      line.style.borderBottomLeftRadius  = 1f;
      line.style.borderBottomRightRadius = 1f;
      handle.Add(line);

      handle.RegisterCallback<MouseEnterEvent>(_ =>
      {
        line.style.backgroundColor = ColorResizeHover;
        line.style.height          = 3f;
      });
      handle.RegisterCallback<MouseLeaveEvent>(_ =>
      {
        line.style.backgroundColor = ColorResizeIdle;
        line.style.height          = 2f;
      });

      handle.AddManipulator(new ResizeDragManipulator(this));
      return handle;
    }

    // ── 높이 조절 내부 API ────────────────────────────────────────────────────

    /// <summary>
    /// 드래그 조작기에서 프레임마다 호출된다.
    /// <paramref name="delta"/>: 마우스 Y축 이동량 (위 = 음수, 아래 = 양수).
    /// 위로 드래그하면 패널이 커지고, 아래로 드래그하면 작아진다.
    /// </summary>
    internal void ApplyHeightDelta(float delta)
    {
      // 위로 드래그(delta < 0) → 높이 증가, 아래로 드래그(delta > 0) → 높이 감소
      _expandedHeight = Mathf.Clamp(_expandedHeight - delta, PanelExpandedMin, PanelExpandedMax);

      if (!_panelExpanded)
      {
        // 드래그하면 자동 펼침
        _panelExpanded = true;
        _panelToggleLabel.text        = "▼  Debug Panel";
        _scrollView.style.display     = DisplayStyle.Flex;
      }

      _scrollView.style.height = _expandedHeight;
      ApplyTotalHeight();
      UpdateHeightHint();
    }

    internal void OnResizeDragStart()
    {
      var line = _resizeHandle?.Q("DebugPanelResizeLine");
      if (line != null)
      {
        line.style.backgroundColor = ColorResizeDrag;
        line.style.height          = 3f;
      }
    }

    internal void OnResizeDragEnd()
    {
      var line = _resizeHandle?.Q("DebugPanelResizeLine");
      if (line != null)
      {
        line.style.backgroundColor = ColorResizeIdle;
        line.style.height          = 2f;
      }
    }

    /// <summary>
    /// 패널 전체 height 를 재계산하여 style.height 에 반영한다.
    /// flexGrow=0 이므로 이 값이 실제 레이아웃 높이를 결정한다.
    /// </summary>
    private void ApplyTotalHeight()
    {
      if (_panelExpanded)
      {
        // 핸들(6) + 구분선(1) + 헤더(26) + 스크롤 영역
        style.height = ResizeHandleHeight + 1f + HeaderHeight + _expandedHeight;
      }
      else
      {
        // 핸들(6) + 구분선(1) + 헤더(26) 만
        style.height = ResizeHandleHeight + 1f + HeaderHeight;
      }
    }

    private void UpdateHeightHint()
    {
      if (_heightHintLabel == null) return;
      _heightHintLabel.text = _panelExpanded ? $"{(int)_expandedHeight}px" : string.Empty;
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
      _panelToggleLabel.text        = (_panelExpanded ? "▼" : "▶") + "  Debug Panel";
      _scrollView.style.display     = _panelExpanded ? DisplayStyle.Flex : DisplayStyle.None;
      ApplyTotalHeight();
      UpdateHeightHint();
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
        _summaryLabel.text        = "이상 없음";
        _summaryLabel.style.color = new Color(0.45f, 0.90f, 0.45f, 1f);
        return;
      }

      var parts = new List<string>();
      if (errors > 0)   parts.Add($"✕ {errors} error{(errors > 1 ? "s" : "")}");
      if (warnings > 0) parts.Add($"⚠ {warnings} warning{(warnings > 1 ? "s" : "")}");
      if (infos > 0)    parts.Add($"ℹ {infos} info");
      _summaryLabel.text        = string.Join("   ", parts);
      _summaryLabel.style.color = errors > 0 ? ColorError : ColorWarning;
    }

    // ── 리사이즈 드래그 조작기 ────────────────────────────────────────────────

    /// <summary>
    /// 리사이즈 핸들 요소에 등록되는 마우스 드래그 조작기.
    /// MouseDown → MouseMove(캡처) → MouseUp 순서로 패널 높이를 변경한다.
    /// </summary>
    private sealed class ResizeDragManipulator : MouseManipulator
    {
      private readonly ScenarioDebugPanelView _panel;
      private bool _dragging;

      public ResizeDragManipulator(ScenarioDebugPanelView panel)
      {
        _panel = panel;
        activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
      }

      protected override void RegisterCallbacksOnTarget()
      {
        target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
        target.RegisterCallback<MouseUpEvent>(OnMouseUp);
        target.RegisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
      }

      protected override void UnregisterCallbacksFromTarget()
      {
        target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
        target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
        target.UnregisterCallback<MouseCaptureOutEvent>(OnCaptureLost);
      }

      private void OnMouseDown(MouseDownEvent evt)
      {
        if (!CanStartManipulation(evt)) return;
        _dragging = true;
        target.CaptureMouse();
        _panel.OnResizeDragStart();
        evt.StopPropagation();
      }

      private void OnMouseMove(MouseMoveEvent evt)
      {
        if (!_dragging) return;
        // evt.mouseDelta: UIElements 가 직접 제공하는 프레임 간 마우스 이동량(픽셀).
        // mousePosition 이나 LocalToWorld 는 레이아웃 재계산 후 요소 위치가 바뀌면
        // delta 가 증폭되므로 사용하지 않는다.
        _panel.ApplyHeightDelta(evt.mouseDelta.y);
        evt.StopPropagation();
      }

      private void OnMouseUp(MouseUpEvent evt)
      {
        if (!_dragging) return;
        EndDrag();
        evt.StopPropagation();
      }

      private void OnCaptureLost(MouseCaptureOutEvent evt)
      {
        if (_dragging) EndDrag();
      }

      private void EndDrag()
      {
        _dragging = false;
        if (target.HasMouseCapture())
          target.ReleaseMouse();
        _panel.OnResizeDragEnd();
      }
    }
  }
}
