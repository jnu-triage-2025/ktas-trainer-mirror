using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>런타임 그래프에는 영향을 주지 않는 에디터 전용 연결선 중계점.</summary>
  internal sealed class ScenarioRerouteHandle : GraphElement
  {
    public string RouteKey { get; }
    public int RouteIndex { get; }
    public Port InputPort { get; }
    public Port OutputPort { get; }

    public ScenarioRerouteHandle(string routeKey, int routeIndex, Vector2 position)
    {
      RouteKey = routeKey;
      RouteIndex = routeIndex;
      tooltip = "드래그하여 연결선을 정리합니다. 선택 후 Delete로 중계점을 제거합니다.";
      capabilities = Capabilities.Selectable | Capabilities.Movable | Capabilities.Deletable;
      layer = 1;

      InputPort = new ScenarioReroutePort(Direction.Input);
      OutputPort = new ScenarioReroutePort(Direction.Output);
      InputPort.portName = string.Empty;
      OutputPort.portName = string.Empty;
      Add(InputPort);
      Add(OutputPort);

      var visual = new VisualElement { pickingMode = PickingMode.Ignore };
      visual.style.position = Position.Absolute;
      visual.style.left = 1f;
      visual.style.top = 1f;
      visual.style.width = 16f;
      visual.style.height = 16f;
      visual.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
      visual.style.borderLeftColor = new Color(0.25f, 0.75f, 1f, 1f);
      visual.style.borderRightColor = new Color(0.25f, 0.75f, 1f, 1f);
      visual.style.borderTopColor = new Color(0.25f, 0.75f, 1f, 1f);
      visual.style.borderBottomColor = new Color(0.25f, 0.75f, 1f, 1f);
      visual.style.borderLeftWidth = 1f;
      visual.style.borderRightWidth = 1f;
      visual.style.borderTopWidth = 1f;
      visual.style.borderBottomWidth = 1f;
      visual.style.borderTopLeftRadius = 8f;
      visual.style.borderTopRightRadius = 8f;
      visual.style.borderBottomLeftRadius = 8f;
      visual.style.borderBottomRightRadius = 8f;
      Add(visual);

      style.width = 18f;
      style.height = 18f;
      SetPosition(new Rect(position - new Vector2(9f, 9f), new Vector2(18f, 18f)));
    }
  }

  /// <summary>중계점 중심에 정확히 겹치는 비가시성 내부 포트.</summary>
  internal sealed class ScenarioReroutePort : Port
  {
    public ScenarioReroutePort(Direction direction)
      : base(Orientation.Horizontal, direction, Port.Capacity.Single, typeof(bool))
    {
      pickingMode = PickingMode.Ignore;
      style.position = Position.Absolute;
      style.left = 8.5f;
      style.top = 8.5f;
      style.width = 1f;
      style.height = 1f;
      style.opacity = 0f;
      style.marginLeft = 0f;
      style.marginRight = 0f;
      style.marginTop = 0f;
      style.marginBottom = 0f;

      m_ConnectorBox.style.position = Position.Absolute;
      m_ConnectorBox.style.left = 0f;
      m_ConnectorBox.style.top = 0f;
      m_ConnectorBox.style.width = 1f;
      m_ConnectorBox.style.height = 1f;
      m_ConnectorBox.style.marginLeft = 0f;
      m_ConnectorBox.style.marginRight = 0f;
      m_ConnectorBox.style.marginTop = 0f;
      m_ConnectorBox.style.marginBottom = 0f;
    }
  }

  internal sealed class ScenarioRoutedEdge : Edge
  {
    public string RouteKey { get; set; }
    public Port LogicalOutputPort { get; set; }
    public int SegmentIndex { get; set; }

    protected override EdgeControl CreateEdgeControl() => new ScenarioRoutedEdgeControl();
  }

  /// <summary>
  /// 정상적인 좌→우 선분은 Unity GraphView의 기본 베지어를 그대로 사용한다.
  /// 역방향이거나 세로 거리에 비해 가로 여유가 부족한 선분만, 화면 밖으로
  /// 크게 감기는 고정 접선 대신 두 끝점 사이의 완만한 베지어 호를 사용한다.
  /// </summary>
  internal sealed class ScenarioRoutedEdgeControl : EdgeControl
  {
    protected override void ComputeControlPoints()
    {
      base.ComputeControlPoints();
      var points = controlPoints;
      if (points == null || points.Length < 4)
        return;

      var delta = to - from;
      // 가로 여유가 충분한 정방향 연결은 기본 GraphView 곡선을 변경하지 않는다.
      // 수직 거리에 비해 x 간격이 좁으면 정방향이어도 기본 접선이 과도하게
      // 바깥으로 부풀 수 있으므로 아래의 제한된 곡선을 사용한다.
      var requiredForwardSpan = Mathf.Clamp(Mathf.Abs(delta.y) * 0.25f, 32f, 120f);
      if (delta.x >= requiredForwardSpan)
        return;

      var distance = delta.magnitude;
      if (distance < 0.01f)
        return;

      // 역방향 또는 가로 여유가 좁은 연결에서는 고정된 좌→우 접선 때문에
      // 생기는 거대한 고리를 피하면서도 3차 베지어 곡선을 유지한다.
      var normal = new Vector2(-delta.y, delta.x).normalized;
      var bend = Mathf.Min(48f, Mathf.Max(12f, distance * 0.12f));

      points[0] = from;
      points[1] = from + delta / 3f + normal * bend;
      points[2] = from + delta * (2f / 3f) + normal * bend;
      points[3] = to;
    }
  }
}
