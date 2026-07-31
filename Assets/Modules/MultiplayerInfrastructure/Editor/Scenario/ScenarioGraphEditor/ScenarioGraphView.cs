using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioGraphView : GraphView
  {
    private static readonly Rect DefaultMiniMapRect = new Rect(5f, 5f, 175f, 135f);
    private const float DefaultMinimumScale = 0.05f;
    private const float DefaultMaximumScale = 4f;
    private const float FrameAllPadding = 80f;
    private const float FrameAllViewportFill = 0.9f;
    private readonly ScenarioGraphAuthoringWindow window;
    private readonly ScenarioGraphMiniMap miniMap;
    private readonly Dictionary<string, List<SerializableVector2>> edgeRoutes = new Dictionary<string, List<SerializableVector2>>();
    private int graphContentVersion;
    public Action<ScenarioNodeView> onNodeSelected;

    public ScenarioGraphView(ScenarioGraphAuthoringWindow window)
    {
      this.window = window;
      style.flexGrow = 1f;

      SetupZoom(DefaultMinimumScale, DefaultMaximumScale);
      this.AddManipulator(new ContentZoomer());
      this.AddManipulator(new ContentDragger());
      this.AddManipulator(new SelectionDragger());
      // Unity 6 RectangleSelector는 클릭 대상이 배경인지 검사하지 않는다.
      // SelectionDragger가 먼저 노드 드래그를 처리할 기회를 준 다음,
      // GraphElement에서 시작한 이벤트만 RectangleSelector 앞에서 차단한다.
      RegisterCallback<MouseDownEvent>(PreventRectangleSelectionFromGraphElements);
      this.AddManipulator(new RectangleSelector());

      var grid = new GridBackground();
      Insert(0, grid);
      grid.StretchToParentSize();

      miniMap = new ScenarioGraphMiniMap(this, DefaultMiniMapRect);
      miniMap.RectChanged = SaveMiniMapLayout;
      hierarchy.Add(miniMap);

      graphViewChanged = OnGraphViewChanged;
    }

    public void ResetMiniMap()
    {
      miniMap.Reset(DefaultMiniMapRect);
    }

    public void SetMiniMapRect(Rect rect)
    {
      miniMap.Reset(rect);
    }

    /// <summary>
    /// Lowers GraphView's zoom-out limit only as far as the current node bounds require.
    /// This keeps normal zoom sensitivity while allowing Frame All and manual zooming to
    /// reach the same complete graph extent that the Preview displays.
    /// </summary>
    public void UpdateZoomRangeToFitAllNodes()
    {
      var nodes = graphElements.OfType<Node>().ToList();
      if (nodes.Count == 0)
      {
        SetupZoom(DefaultMinimumScale, DefaultMaximumScale);
        return;
      }

      // Graph를 연 직후에는 UI Toolkit layout이 아직 NaN일 수 있다.
      // NaN을 SetupZoom에 전달하면 GraphView.minScale도 NaN이 되고,
      // EdgeControl.ComputeLayout의 padding 계산까지 전파되어 모든 연결선
      // layout이 NaN이 된다.
      if (!IsFinitePositive(layout.width) || !IsFinitePositive(layout.height))
      {
        SetupZoom(DefaultMinimumScale, DefaultMaximumScale);
        return;
      }

      var bounds = nodes[0].GetPosition();
      for (var index = 1; index < nodes.Count; index++)
      {
        var nodeRect = nodes[index].GetPosition();
        bounds = Rect.MinMaxRect(
          Mathf.Min(bounds.xMin, nodeRect.xMin),
          Mathf.Min(bounds.yMin, nodeRect.yMin),
          Mathf.Max(bounds.xMax, nodeRect.xMax),
          Mathf.Max(bounds.yMax, nodeRect.yMax));
      }

      var viewportWidth = Mathf.Max(1f, layout.width - FrameAllPadding * 2f);
      var viewportHeight = Mathf.Max(1f, layout.height - FrameAllPadding * 2f);
      var requiredScale = Mathf.Min(
        viewportWidth / Mathf.Max(1f, bounds.width),
        viewportHeight / Mathf.Max(1f, bounds.height)) * FrameAllViewportFill;
      if (!IsFinitePositive(requiredScale))
      {
        SetupZoom(DefaultMinimumScale, DefaultMaximumScale);
        return;
      }
      SetupZoom(Mathf.Min(DefaultMinimumScale, requiredScale), DefaultMaximumScale);
    }

    private static bool IsFinitePositive(float value)
    {
      return !float.IsNaN(value) && !float.IsInfinity(value) && value > 0f;
    }

    public void FrameAllNodes()
    {
      UpdateZoomRangeToFitAllNodes();
      FrameAll();
    }

    private void SaveMiniMapLayout(Rect rect)
    {
      window.SaveMiniMapLayout(rect);
    }

    private static void PreventRectangleSelectionFromGraphElements(MouseDownEvent evt)
    {
      if (evt.button != 0)
        return;

      var target = evt.target as VisualElement;
      var graphElement = target as GraphElement ??
                         target?.GetFirstAncestorOfType<GraphElement>();
      if (graphElement != null)
        evt.StopImmediatePropagation();
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
      var compatible = new List<Port>();

      ports.ForEach(port =>
      {
        if (port == startPort)
          return;

        if (port.node == startPort.node)
          return;

        if (port.direction == startPort.direction)
          return;

        compatible.Add(port);
      });

      return compatible;
    }

    public void ClearGraph()
    {
      graphContentVersion++;
      graphElements.ForEach(RemoveElement);
      edgeRoutes.Clear();
    }

    public void SetEdgeRoutes(Dictionary<string, List<SerializableVector2>> routes)
    {
      edgeRoutes.Clear();
      if (routes == null) return;
      foreach (var pair in routes)
        edgeRoutes[pair.Key] = pair.Value?.ToList() ?? new List<SerializableVector2>();
    }

    public Dictionary<string, List<SerializableVector2>> CaptureEdgeRoutes()
    {
      var result = edgeRoutes.ToDictionary(
        pair => pair.Key,
        pair => pair.Value?.ToList() ?? new List<SerializableVector2>());

      foreach (var reroute in graphElements.OfType<ScenarioRerouteHandle>())
      {
        if (!result.TryGetValue(reroute.RouteKey, out var points))
          continue;
        if (reroute.RouteIndex < 0 || reroute.RouteIndex >= points.Count)
          continue;
        points[reroute.RouteIndex] = new SerializableVector2(reroute.GetPosition().center);
      }
      return result;
    }

    public void RemoveEdgeRoutesForNode(string nodeIdentifier)
    {
      if (string.IsNullOrEmpty(nodeIdentifier)) return;
      var prefix = nodeIdentifier + ":";
      foreach (var key in edgeRoutes.Keys.Where(each => each.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        edgeRoutes.Remove(key);
    }

    public void RenameEdgeRoutes(string oldIdentifier, string newIdentifier)
    {
      if (string.IsNullOrEmpty(oldIdentifier) || string.IsNullOrEmpty(newIdentifier)) return;
      var prefix = oldIdentifier + ":";
      foreach (var key in edgeRoutes.Keys.Where(each => each.StartsWith(prefix, StringComparison.Ordinal)).ToList())
      {
        var renamedKey = newIdentifier + key.Substring(oldIdentifier.Length);
        edgeRoutes[renamedKey] = edgeRoutes[key];
        edgeRoutes.Remove(key);
      }
    }

    public ScenarioNodeView AddNodeView(IScenarioNode data)
    {
      var nodeView = new ScenarioNodeView(window, this, data);
      nodeView.RegisterCallback<GeometryChangedEvent>(_ => RefreshEdgeGeometry());
      nodeView.Query<Port>().ForEach(
        port => port.RegisterCallback<GeometryChangedEvent>(_ => RefreshEdgeGeometry()));
      AddElement(nodeView);
      window.RegisterNodeView(nodeView);
      return nodeView;
    }

    public void RestoreEdges(Dictionary<string, ScenarioNodeView> nodeViews)
    {
      foreach (var node in nodeViews.Values)
      {
        if (node.Data is not ScenarioParallelNode &&
            !string.IsNullOrEmpty(node.Data.NextIdentifier) &&
            nodeViews.TryGetValue(node.Data.NextIdentifier, out var target) &&
            node.DefaultOutputPort != null)
        {
          CreateEdge(node.DefaultOutputPort, target.InputPort);
        }

        if (node.Data is ScenarioChoiceNode choice)
        {
          foreach (var option in choice.Options)
          {
            if (string.IsNullOrEmpty(option.NextNodeIdentifier)) continue;
            if (!nodeViews.TryGetValue(option.NextNodeIdentifier, out var targetView)) continue;
            var port = node.GetPortForOption(option);
            if (port != null)
            {
              CreateEdge(port, targetView.InputPort);
            }
          }
        }

        if (node.Data is ScenarioParallelNode parallel)
        {
          foreach (var branch in parallel.Branches)
          {
            if (string.IsNullOrEmpty(branch.Identifier)) continue;
            if (!nodeViews.TryGetValue(branch.Identifier, out var targetView)) continue;
            var port = node.GetPortForBranch(branch);
            if (port != null)
            {
              CreateEdge(port, targetView.InputPort);
            }
          }
        }

        if (node.Data is ScenarioQuizNode quiz)
        {
          foreach (var port in node.outputContainer.Children().OfType<Port>())
          {
            if (port.userData is not string marker)
              continue;

            var targetIdentifier = marker switch
            {
              "quiz.correct" => quiz.OnCorrectNextIdentifier,
              "quiz.incorrect" => quiz.OnIncorrectNextIdentifier,
              _ => null
            };

            if (string.IsNullOrEmpty(targetIdentifier))
              continue;

            if (!nodeViews.TryGetValue(targetIdentifier, out var targetView))
              continue;

            CreateEdge(port, targetView.InputPort);
          }
        }
      }
    }

    /// <summary>
    /// 노드와 포트의 UI Toolkit geometry가 확정된 뒤 연결선을 복원한다.
    /// 저장 위치를 적용한 직후 Edge를 만들면 포트의 이전/초기 좌표를 사용하여
    /// 연결선 끝점이 노드에 붙지 않는 경우가 있다.
    /// </summary>
    public void RestoreEdgesAfterLayout(Dictionary<string, ScenarioNodeView> nodeViews)
    {
      var expectedVersion = graphContentVersion;
      schedule.Execute(() =>
      {
        if (expectedVersion != graphContentVersion)
          return;

        RestoreEdges(nodeViews);
        schedule.Execute(() =>
        {
          if (expectedVersion != graphContentVersion)
            return;
          RefreshEdgeGeometry();
        }).ExecuteLater(1);
      }).ExecuteLater(1);
    }

    public void RefreshEdgeGeometry()
    {
      foreach (var edge in graphElements.OfType<Edge>())
      {
        // Unity 6 Edge.UpdateEdgeControl()은 m_EndPointsDirty가 false이면
        // 포트 좌표를 다시 읽지 않는다. 동일 포트를 setter에 다시 넣으면
        // 공개 API 경로로 dirty 플래그가 설정되고 끝점이 즉시 재계산된다.
        edge.output = edge.output;
        edge.input = edge.input;
        edge.UpdateEdgeControl();
        edge.MarkDirtyRepaint();
      }
    }

    public void RebuildAllEdges()
    {
      var edges = graphElements.OfType<Edge>().ToList();
      foreach (var edge in edges)
      {
        RemoveElement(edge);
      }
      var reroutes = graphElements.OfType<ScenarioRerouteHandle>().ToList();
      foreach (var reroute in reroutes)
        RemoveElement(reroute);
      RestoreEdges(window.GraphData.Nodes.ToDictionary(kvp => kvp.Key, kvp => window.GetNodeView(kvp.Key)));
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
      if (change.edgesToCreate != null)
      {
        foreach (var edge in change.edgesToCreate)
        {
          if (edge.output?.node is ScenarioNodeView outputNode &&
              edge.input?.node is ScenarioNodeView inputNode)
          {
            outputNode.HandlePortConnection(edge.output, inputNode);
          }
        }
        if (change.edgesToCreate.Count > 0)
          schedule.Execute(RebuildAllEdges);
      }

      if (change.elementsToRemove != null)
      {
        var removedRoutePoints = change.elementsToRemove
          .OfType<ScenarioRerouteHandle>()
          .GroupBy(each => each.RouteKey)
          .ToDictionary(group => group.Key, group => group.Select(each => each.RouteIndex).OrderByDescending(each => each).ToList());

        foreach (var pair in removedRoutePoints)
        {
          if (!edgeRoutes.TryGetValue(pair.Key, out var points)) continue;
          foreach (var index in pair.Value)
            if (index >= 0 && index < points.Count) points.RemoveAt(index);
          if (points.Count == 0) edgeRoutes.Remove(pair.Key);
        }

        var rebuildAfterRemoval = removedRoutePoints.Count > 0;
        foreach (var element in change.elementsToRemove)
        {
          if (element is Edge edge)
          {
            if (edge is ScenarioRoutedEdge routed && removedRoutePoints.ContainsKey(routed.RouteKey))
              continue;

            var logicalOutput = edge is ScenarioRoutedEdge routedEdge
              ? routedEdge.LogicalOutputPort
              : edge.output;
            if (logicalOutput?.node is ScenarioNodeView outputNode)
            {
              outputNode.HandlePortDisconnection(logicalOutput);
              edgeRoutes.Remove(GetRouteKey(logicalOutput));
              rebuildAfterRemoval |= edge is ScenarioRoutedEdge;
            }
          }
          else if (element is ScenarioNodeView nodeView)
          {
            window.RemoveNode(nodeView);
          }
        }

        if (rebuildAfterRemoval)
          schedule.Execute(RebuildAllEdges);
      }

      // 엣지 변경(연결/해제)이 있으면 디버그 패널을 갱신한다.
      bool hasEdgeChange = (change.edgesToCreate != null && change.edgesToCreate.Count > 0)
                         || (change.elementsToRemove != null && change.elementsToRemove.OfType<Edge>().Any());
      if (hasEdgeChange)
      {
        window.NotifyGraphStructureChanged();
      }

      return change;
    }

    public void CreateEdge(Port output, Port input)
    {
      if (output == null || input == null) return;

      var routeKey = GetRouteKey(output);
      if (!edgeRoutes.TryGetValue(routeKey, out var routePoints) || routePoints.Count == 0)
      {
        AddRoutedEdge(output, input, routeKey, output, 0);
        return;
      }

      Port previousOutput = output;
      for (var index = 0; index < routePoints.Count; index++)
      {
        var reroute = new ScenarioRerouteHandle(routeKey, index, routePoints[index].ToVector2());
        AddElement(reroute);
        AddRoutedEdge(previousOutput, reroute.InputPort, routeKey, output, index);
        previousOutput = reroute.OutputPort;
      }
      AddRoutedEdge(previousOutput, input, routeKey, output, routePoints.Count);
    }

    private void AddRoutedEdge(Port output, Port input, string routeKey, Port logicalOutput, int segmentIndex)
    {
      var edge = new ScenarioRoutedEdge
      {
        output = output,
        input = input,
        RouteKey = routeKey,
        LogicalOutputPort = logicalOutput,
        SegmentIndex = segmentIndex
      };
      edge.tooltip = "연결선을 더블클릭하면 중계점을 추가합니다.";
      edge.RegisterCallback<MouseDownEvent>(evt =>
      {
        if (evt.button != 0 || evt.clickCount != 2) return;
        var graphPosition = contentViewContainer.WorldToLocal(edge.LocalToWorld(evt.localMousePosition));
        AddReroutePoint(routeKey, edge.SegmentIndex, graphPosition);
        evt.StopPropagation();
      });
      edge.output.Connect(edge);
      edge.input.Connect(edge);
      AddElement(edge);
    }

    private void AddReroutePoint(string routeKey, int insertIndex, Vector2 position)
    {
      SyncEdgeRoutePositions();
      if (!edgeRoutes.TryGetValue(routeKey, out var points))
      {
        points = new List<SerializableVector2>();
        edgeRoutes[routeKey] = points;
      }
      points.Insert(Mathf.Clamp(insertIndex, 0, points.Count), new SerializableVector2(position));
      RebuildAllEdges();
      window.NotifyGraphStructureChanged();
    }

    private void SyncEdgeRoutePositions()
    {
      foreach (var reroute in graphElements.OfType<ScenarioRerouteHandle>())
      {
        if (!edgeRoutes.TryGetValue(reroute.RouteKey, out var points)) continue;
        if (reroute.RouteIndex < 0 || reroute.RouteIndex >= points.Count) continue;
        points[reroute.RouteIndex] = new SerializableVector2(reroute.GetPosition().center);
      }
    }

    private static string GetRouteKey(Port output)
    {
      return output?.node is ScenarioNodeView node
        ? $"{node.Data.Identifier}:{node.GetOutputRouteSlot(output)}"
        : string.Empty;
    }

    public Vector2 ScreenToGraphPosition(Vector2 screenPosition)
    {
      var windowPosition = window.position.position;
      var local = screenPosition - windowPosition;
      var world = contentViewContainer.WorldToLocal(local);
      return world;
    }
  }
}
