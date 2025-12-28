using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Registry;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioGraphAuthoringWindow : EditorWindow
  {
    private ScenarioGraphView graphView;
    private ScenarioInspectorView inspectorView;
    private ScenarioNodeSearchWindow searchWindow;
    private VisualElement mainContainer;


    private ScenarioGraph graphData = new ScenarioGraph();
    private readonly Dictionary<string, ScenarioNodeView> nodeViews = new Dictionary<string, ScenarioNodeView>();

    private Vector2 cachedMousePosition;
    private string currentFilePath;

    [MenuItem("TriageTrainer/Multiplayer Infrastructure/Multiplayer Scenario/Scenario Graph Authoring")]
    public static void Open()
    {
      var window = GetWindow<ScenarioGraphAuthoringWindow>();
      window.titleContent = new GUIContent("Scenario Graph Authoring");
      window.minSize = new Vector2(900f, 500f);
      window.Show();
    }

    private void OnEnable()
    {
      ConstructUI();
      CreateGraphView();
      CreateInspector();
      CreateSearchWindow();
      BindGraphEvents();
      LoadBlankGraph();
    }

    private void OnDisable()
    {
      if (mainContainer != null && mainContainer.parent != null)
      {
        rootVisualElement.Remove(mainContainer);
      }
    }

    private void ConstructUI()
    {
      rootVisualElement.Clear();
      rootVisualElement.style.flexDirection = FlexDirection.Column;
      rootVisualElement.style.flexGrow = 1f;
      rootVisualElement.style.overflow = Overflow.Hidden;
      rootVisualElement.style.width = Length.Percent(100);

      var toolbar = new Toolbar();

      var newButton = new ToolbarButton(LoadBlankGraph) { text = "New Graph" };
      toolbar.Add(newButton);

      var addNodeButton = new ToolbarButton(OpenCreateNodeMenu) { text = "Add Node" };
      toolbar.Add(addNodeButton);

      var loadButton = new ToolbarButton(OpenGraphFromJson) { text = "Open File" };
      toolbar.Add(loadButton);

      var saveButton = new ToolbarButton(SaveGraphToJson) { text = "Save File" };
      toolbar.Add(saveButton);

      var validateButton = new ToolbarButton(ValidateGraphUsingRuntimeValidator) { text = "Validate" };
      toolbar.Add(validateButton);

      rootVisualElement.Add(toolbar);

      EnsureGraphData();
    }

    private void CreateGraphView()
    {
      mainContainer = new VisualElement { name = "ScenarioMainContainer" };
      mainContainer.style.flexGrow = 1f;
      mainContainer.style.flexDirection = FlexDirection.Row;

      var graphHost = new VisualElement { name = "ScenarioGraphHost" };
      graphHost.style.flexGrow = 1f;
      graphHost.style.flexShrink = 1f;
      graphHost.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f, 1f));

      graphView = new ScenarioGraphView(this) { name = "ScenarioGraphView" };
      graphView.style.flexGrow = 1f;
      graphView.style.flexShrink = 1f;
      graphHost.Add(graphView);

      inspectorView = new ScenarioInspectorView(this) { name = "ScenarioInspectorView" };
      inspectorView.style.flexGrow = 1f;

      var inspectorHeader = new Label("Scenario Inspector")
      {
        style =
        {
          unityFontStyleAndWeight = FontStyle.Bold,
          marginBottom = 6,
          marginTop = 4,
          fontSize = 13,
          color = Color.white
        }
      };

      var inspectorScroll = new ScrollView { name = "ScenarioInspectorScroll" };
      inspectorScroll.style.flexGrow = 1f;
      inspectorScroll.Add(inspectorView);

      var inspectorPanel = new VisualElement { name = "ScenarioInspectorPanel" };
      inspectorPanel.style.width = 340;
      inspectorPanel.style.flexShrink = 0f;
      inspectorPanel.style.backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f, 1f));
      inspectorPanel.style.paddingLeft = 8;
      inspectorPanel.style.paddingRight = 8;
      inspectorPanel.style.paddingTop = 8;
      inspectorPanel.style.paddingBottom = 8;
      inspectorPanel.style.flexDirection = FlexDirection.Column;

      inspectorPanel.Add(inspectorHeader);
      inspectorPanel.Add(inspectorScroll);

      mainContainer.Add(graphHost);
      mainContainer.Add(inspectorPanel);

      rootVisualElement.Add(mainContainer);
    }

    private void CreateInspector()
    {
      inspectorView.SetTarget(null);
    }

    private void CreateSearchWindow()
    {
      searchWindow = ScriptableObject.CreateInstance<ScenarioNodeSearchWindow>();
      searchWindow.Initialize(this, graphView);
      graphView.nodeCreationRequest = ctx =>
      {
        cachedMousePosition = ctx.screenMousePosition;
        SearchWindow.Open(
              new SearchWindowContext(ctx.screenMousePosition),
              searchWindow);
      };
    }

    private void OpenCreateNodeMenu()
    {
      // Open the search window at the center of the editor window for quick node creation.
      var center = position.position + new Vector2(position.width * 0.5f, position.height * 0.5f);
      cachedMousePosition = center;
      SearchWindow.Open(new SearchWindowContext(center), searchWindow);
    }

    public ScenarioNodeView ChangeNodeType(ScenarioNodeView nodeView, ScenarioNodeType newType)
    {
      if (nodeView == null || nodeView.Data == null) return null;
      if (nodeView.Data.NodeType == newType) return nodeView;

      var oldData = nodeView.Data;
      var oldPos = nodeView.GetPosition();

      // Create new data with same identifier.
      var newData = ScenarioNodeFactory.Create(newType);
      newData.Identifier = oldData.Identifier;

      // Preserve simple next link if applicable.
      newData.NextIdentifier = oldData.NextIdentifier;

      // Replace data in graph.
      graphData.Nodes[newData.Identifier] = newData;

      // Remove old node view and its edges.
      graphView.RemoveElement(nodeView);
      nodeViews.Remove(newData.Identifier);

      // Add new node view and position it.
      var newView = graphView.AddNodeView(newData);
      newView.SetPosition(oldPos);
      nodeViews[newData.Identifier] = newView;

      // Rebuild connections based on updated data.
      graphView.RebuildAllEdges();

      return newView;
    }

    private void BindGraphEvents()
    {
      graphView.onNodeSelected = node => inspectorView.SetTarget(node);
    }

    public ScenarioNodeView CreateNode(ScenarioNodeType type, Vector2 screenMousePosition)
    {
      EnsureGraphData();

      var graphPosition = graphView.ScreenToGraphPosition(screenMousePosition);
      var data = ScenarioNodeFactory.Create(type);
      data.Identifier = GetUniqueIdentifier(data.NodeType.ToString().ToLower());

      graphData.Nodes.Add(data.Identifier, data);

      var nodeView = graphView.AddNodeView(data);
      nodeViews[data.Identifier] = nodeView;

      nodeView.SetPosition(new Rect(graphPosition, nodeView.DefaultSize));
      nodeView.RefreshPorts();

      inspectorView.SetTarget(nodeView);
      return nodeView;
    }

    private void LoadBlankGraph()
    {
      graphData = new ScenarioGraph();
      nodeViews.Clear();
      graphView.ClearGraph();
      inspectorView.SetTarget(null);
      currentFilePath = null;
    }

    private void EnsureGraphData()
    {
      if (graphData == null)
      {
        graphData = new ScenarioGraph();
      }
    }

    private string GetUniqueIdentifier(string prefix)
    {
      prefix = string.IsNullOrEmpty(prefix) ? "node" : prefix;
      var index = 1;
      var candidate = $"{prefix}_{index}";
      while (graphData.Nodes.ContainsKey(candidate))
      {
        index++;
        candidate = $"{prefix}_{index}";
      }

      return candidate;
    }

    public void NotifyNodeSelected(ScenarioNodeView nodeView)
    {
      inspectorView.SetTarget(nodeView);
    }

    public void RemoveNode(ScenarioNodeView nodeView)
    {
      if (nodeView == null || nodeView.Data == null) return;

      var id = nodeView.Data.Identifier;
      if (graphData.Nodes.ContainsKey(id))
      {
        graphData.Nodes.Remove(id);
      }

      nodeViews.Remove(id);

      foreach (var other in graphData.Nodes.Values)
      {
        if (other.NextIdentifier == id)
        {
          other.NextIdentifier = null;
        }

        if (other is ScenarioChoiceNode choice)
        {
          foreach (var option in choice.Options)
          {
            if (option.NextNodeIdentifier == id)
            {
              option.NextNodeIdentifier = null;
            }
          }
        }

        if (other is ScenarioParallelNode parallel)
        {
          foreach (var branch in parallel.Branches)
          {
            if (branch.Identifier == id)
            {
              branch.Identifier = null;
            }
          }
        }
      }

      inspectorView.SetTarget(null);
    }

    public bool TryRenameNode(ScenarioNodeView nodeView, string newId)
    {
      if (nodeView == null || nodeView.Data == null) return false;
      if (string.IsNullOrWhiteSpace(newId))
      {
        EditorUtility.DisplayDialog("Rename Failed", "식별자는 비어 있을 수 없습니다.", "확인");
        return false;
      }

      var trimmed = newId.Trim();
      if (trimmed == nodeView.Data.Identifier)
      {
        return true;
      }

      if (graphData.Nodes.ContainsKey(trimmed))
      {
        EditorUtility.DisplayDialog("Rename Failed", $"이미 존재하는 식별자입니다: {trimmed}", "확인");
        return false;
      }

      var oldId = nodeView.Data.Identifier;
      nodeView.Data.Identifier = trimmed;

      graphData.Nodes.Remove(oldId);
      graphData.Nodes[trimmed] = nodeView.Data;

      foreach (var node in graphData.Nodes.Values)
      {
        if (node.NextIdentifier == oldId)
        {
          node.NextIdentifier = trimmed;
        }

        if (node is ScenarioChoiceNode choice)
        {
          foreach (var option in choice.Options)
          {
            if (option.NextNodeIdentifier == oldId)
            {
              option.NextNodeIdentifier = trimmed;
            }
          }
        }

        if (node is ScenarioParallelNode parallel)
        {
          foreach (var branch in parallel.Branches)
          {
            if (branch.Identifier == oldId)
            {
              branch.Identifier = trimmed;
            }
          }
        }
      }

      nodeViews.Remove(oldId);
      nodeViews[trimmed] = nodeView;

      graphView.RebuildAllEdges();
      nodeView.RefreshTitle();

      return true;
    }

    private void OpenGraphFromJson()
    {
      var path = EditorUtility.OpenFilePanel("Open Scenario JSON", Application.dataPath, "json");
      if (string.IsNullOrEmpty(path)) return;

      var json = File.ReadAllText(path);
      try
      {
        var loaded = ScenarioGraphLoader.LoadFromJson(json, true);
        if (loaded == null || loaded.Nodes.Count == 0)
        {
          throw new InvalidOperationException("유효한 nodes 데이터를 찾지 못했습니다.");
        }

        graphData = loaded;
        nodeViews.Clear();
        graphView.ClearGraph();

        // Load editor data
        var editorPath = path.Replace(".json", ".editor.json");
        ScenarioGraphEditorData editorData = null;
        if (File.Exists(editorPath))
        {
          var editorJson = File.ReadAllText(editorPath);
          editorData = JsonSerializer.Deserialize<ScenarioGraphEditorData>(editorJson);
        }

        var orderedNodes = graphData.Nodes.Values.OrderBy(n => n.Identifier).ToList();
        const float spacingX = 320f;
        const float spacingY = 220f;
        int index = 0;

        foreach (var node in orderedNodes)
        {
          var nodeView = graphView.AddNodeView(node);
          Vector2 position;
          if (editorData != null && editorData.NodePositions.TryGetValue(node.Identifier, out var pos))
          {
            position = pos.ToVector2();
          }
          else
          {
            var col = index % 4;
            var row = index / 4;
            position = new Vector2(col * spacingX, row * spacingY);
          }
          nodeView.SetPosition(new Rect(position, nodeView.DefaultSize));
          nodeViews[node.Identifier] = nodeView;
          index++;
        }

        if (editorData == null)
        {
          AutoLayoutNodes();
        }

        graphView.RestoreEdges(nodeViews);
        inspectorView.SetTarget(null);
        currentFilePath = path;

        ValidateResources(graphData);
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
        EditorUtility.DisplayDialog("JSON Load Failed", ex.Message, "확인");
      }
    }

    private void SaveGraphToJson()
    {
      if (graphData == null || graphData.Nodes.Count == 0)
      {
        EditorUtility.DisplayDialog("Save Failed", "저장할 노드가 없습니다.", "확인");
        return;
      }

      var path = EditorUtility.SaveFilePanel("Save Scenario JSON", Application.dataPath, "scenario_graph.json", "json");
      if (string.IsNullOrEmpty(path)) return;

      try
      {
        var json = ScenarioGraphLoader.SaveToJson(graphData, true);
        File.WriteAllText(path, json);

        // Save editor data
        var editorData = new ScenarioGraphEditorData();
        foreach (var pair in nodeViews)
        {
          var nodeView = pair.Value;
          var rect = nodeView.GetPosition();
          editorData.NodePositions[pair.Key] = new SerializableVector2(rect.position);
        }
        var editorJson = JsonSerializer.Serialize(editorData, new JsonSerializerOptions { WriteIndented = true });
        var editorPath = path.Replace(".json", ".editor.json");
        File.WriteAllText(editorPath, editorJson);

        AssetDatabase.Refresh();
        currentFilePath = path;
        EditorUtility.DisplayDialog("Save Completed", $"JSON 저장 완료:\n{path}", "확인");
      }
      catch (Exception ex)
      {
        EditorUtility.DisplayDialog("Save Failed", ex.Message, "확인");
        Debug.LogException(ex);
      }
    }

    private void ValidateGraphUsingRuntimeValidator()
    {
      if (graphData == null || graphData.Nodes.Count == 0)
      {
        EditorUtility.DisplayDialog("Validate Failed", "검증할 노드가 없습니다.", "확인");
        return;
      }

      try
      {
        var json = ScenarioGraphLoader.SaveToJson(graphData, true);
        var graph = ScenarioGraphLoader.LoadFromJson(json, true);
        EditorUtility.DisplayDialog("Validation", $"Scenario loaded successfully.\nNode count: {graph.Nodes.Count}", "확인");
      }
      catch (Exception ex)
      {
        EditorUtility.DisplayDialog("Validation Failed", ex.Message, "확인");
        Debug.LogException(ex);
      }
    }

    public ScenarioGraph GraphData => graphData;

    public ScenarioNodeView GetNodeView(string Identifier)
    {
      nodeViews.TryGetValue(Identifier, out var nodeView);
      return nodeView;
    }

    public void RegisterNodeView(ScenarioNodeView nodeView)
    {
      if (nodeView?.Data == null) return;
      nodeViews[nodeView.Data.Identifier] = nodeView;
    }

    private void AutoLayoutNodes()
    {
      if (nodeViews.Count == 0) return;

      // Compute incoming edge counts
      var incoming = nodeViews.Keys.ToDictionary(id => id, id => 0);
      foreach (var node in graphData.Nodes.Values)
      {
        foreach (var tgt in GetOutgoingTargets(node))
        {
          if (incoming.ContainsKey(tgt)) incoming[tgt]++;
        }
      }

      var entries = incoming.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).ToList();
      if (entries.Count == 0)
      {
        entries = nodeViews.Keys.OrderBy(id => id).Take(1).ToList();
      }

      var depth = new Dictionary<string, int>();
      var queue = new Queue<string>();
      foreach (var id in entries)
      {
        depth[id] = 0;
        queue.Enqueue(id);
      }

      while (queue.Count > 0)
      {
        var current = queue.Dequeue();
        var currentDepth = depth[current];
        var node = graphData.Nodes[current];
        foreach (var tgt in GetOutgoingTargets(node))
        {
          if (!depth.ContainsKey(tgt))
          {
            depth[tgt] = currentDepth + 1;
            queue.Enqueue(tgt);
          }
          else
          {
            depth[tgt] = Math.Min(depth[tgt], currentDepth + 1);
          }
        }
      }

      // Any nodes not reached: place after deepest layer
      var maxDepth = depth.Count > 0 ? depth.Values.Max() : 0;
      foreach (var id in nodeViews.Keys)
      {
        if (!depth.ContainsKey(id)) depth[id] = maxDepth + 1;
      }

      const float spacingX = 360f;
      const float spacingY = 200f;
      foreach (var group in depth.GroupBy(kvp => kvp.Value).OrderBy(g => g.Key))
      {
        var nodesInDepth = group.Select(kvp => kvp.Key).OrderBy(id => id).ToList();
        for (int i = 0; i < nodesInDepth.Count; i++)
        {
          var id = nodesInDepth[i];
          if (!nodeViews.TryGetValue(id, out var view)) continue;
          var pos = new Vector2(group.Key * spacingX, i * spacingY);
          view.SetPosition(new Rect(pos, view.DefaultSize));
        }
      }
    }

    private static IEnumerable<string> GetOutgoingTargets(IScenarioNode node)
    {
      if (node == null) yield break;

      if (!string.IsNullOrEmpty(node.NextIdentifier))
      {
        yield return node.NextIdentifier;
      }

      if (node is ScenarioChoiceNode choice)
      {
        foreach (var opt in choice.Options)
        {
          if (!string.IsNullOrEmpty(opt.NextNodeIdentifier)) yield return opt.NextNodeIdentifier;
        }
      }

      if (node is ScenarioParallelNode parallel)
      {
        foreach (var branch in parallel.Branches)
        {
          if (!string.IsNullOrEmpty(branch.Identifier)) yield return branch.Identifier;
        }
      }
    }

    private void ValidateResources(ScenarioGraph graph)
    {
      var npcNodes = graph.Nodes.Values.OfType<ScenarioNPCMoveNode>();
      var missingNPCs = new List<string>();
      foreach (var node in npcNodes)
      {
        if (!string.IsNullOrEmpty(node.NPCIdentifier) && (NPCRegistry.Instance == null || NPCRegistry.Instance.GetNPC(node.NPCIdentifier) == null))
        {
          missingNPCs.Add(node.NPCIdentifier);
        }
      }
      var playerMoveNodes = graph.Nodes.Values.OfType<ScenarioPlayerMoveNode>().Where(n => n.DestinationType == ScenarioMoveDestinationType.Waypoint);
      var missingWaypoints = new List<string>();
      foreach (var node in playerMoveNodes)
      {
        if (!string.IsNullOrEmpty(node.DestinationIdentifier) && (WaypointRegistry.Instance == null || !WaypointRegistry.Instance.GetWaypointPosition(node.DestinationIdentifier).HasValue))
        {
          missingWaypoints.Add(node.DestinationIdentifier);
        }
      }
      var npcMoveNodes = graph.Nodes.Values.OfType<ScenarioNPCMoveNode>().Where(n => n.DestinationType == ScenarioMoveDestinationType.Waypoint);
      foreach (var node in npcMoveNodes)
      {
        if (!string.IsNullOrEmpty(node.DestinationIdentifier) && (WaypointRegistry.Instance == null || !WaypointRegistry.Instance.GetWaypointPosition(node.DestinationIdentifier).HasValue))
        {
          if (!missingWaypoints.Contains(node.DestinationIdentifier))
          {
            missingWaypoints.Add(node.DestinationIdentifier);
          }
        }
      }
      if (missingNPCs.Any() || missingWaypoints.Any())
      {
        var message = "";
        if (missingNPCs.Any())
        {
          message += "다음 NPC들이 등록되지 않았습니다:\n" + string.Join("\n", missingNPCs) + "\n";
        }
        if (missingWaypoints.Any())
        {
          message += "다음 Waypoint들이 등록되지 않았습니다:\n" + string.Join("\n", missingWaypoints);
        }
        EditorUtility.DisplayDialog("검증 실패", message, "확인");
      }
      else
      {
        EditorUtility.DisplayDialog("검증 성공", "모든 NPC와 Waypoint가 등록되었습니다.", "확인");
      }
    }
  }
}
