using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioGraphView : GraphView
  {
    private readonly ScenarioGraphAuthoringWindow window;
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

      var miniMap = new MiniMap { anchored = true };
      miniMap.SetPosition(new Rect(10, 10, 180, 140));
      Add(miniMap);

      graphViewChanged = OnGraphViewChanged;
    }

    public void ClearGraph()
    {
      graphElements.ForEach(RemoveElement);
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
        if (!string.IsNullOrEmpty(node.Data.NextIdentifier) &&
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
      }
    }

    public void RebuildAllEdges()
    {
      var edges = graphElements.OfType<Edge>().ToList();
      foreach (var edge in edges)
      {
        RemoveElement(edge);
      }
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
      }

      if (change.elementsToRemove != null)
      {
        foreach (var element in change.elementsToRemove)
        {
          if (element is Edge edge)
          {
            if (edge.output?.node is ScenarioNodeView outputNode)
            {
              outputNode.HandlePortDisconnection(edge.output);
            }
          }
          else if (element is ScenarioNodeView nodeView)
          {
            window.RemoveNode(nodeView);
          }
        }
      }

      return change;
    }

    public void CreateEdge(Port output, Port input)
    {
      if (output == null || input == null) return;

      var edge = new Edge
      {
        output = output,
        input = input
      };
      edge.output.Connect(edge);
      edge.input.Connect(edge);
      AddElement(edge);
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
