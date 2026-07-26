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
    private readonly ScenarioGraphAuthoringWindow window;
    private readonly ScenarioGraphMiniMap miniMap;
    private readonly Dictionary<string, List<SerializableVector2>> edgeRoutes = new Dictionary<string, List<SerializableVector2>>();
    public Action<ScenarioNodeView> onNodeSelected;

    public ScenarioGraphView(ScenarioGraphAuthoringWindow window)
    {
      this.window = window;
      style.flexGrow = 1f;

      this.AddManipulator(new ContentZoomer());
      this.AddManipulator(new ContentDragger());
      this.AddManipulator(new SelectionDragger());
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

    private void SaveMiniMapLayout(Rect rect)
    {
      window.SaveMiniMapLayout(rect);
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
