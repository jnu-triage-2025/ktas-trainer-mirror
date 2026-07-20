using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Scenario.Requirements.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

using MI = MultiplayerInfrastructure;

namespace MultiplayerInfrastructure.Editor
{
  public class ScenarioGraphAuthoringWindow : EditorWindow
  {
    private ScenarioGraphView graphView;
    private ScenarioInspectorView inspectorView;
    private ScenarioRuntimeHistoryView runtimeHistoryView;
    private ScenarioNodeSearchWindow searchWindow;
    private VisualElement mainContainer;
    private VisualElement graphHost;
    private ScenarioDebugPanelView debugPanelView;
    private ScenarioSearchPanelView searchPanelView;
    private VisualElement editContainer;
    private ToolbarButton graphTabButton;
    private ToolbarButton editTabButton;

    private ScenarioGraph graphData = new ScenarioGraph();
    private readonly Dictionary<string, ScenarioNodeView> nodeViews = new Dictionary<string, ScenarioNodeView>();

    private ScenarioController runtimeScenarioController;
    private ScenarioNodeView runtimeHighlightedNodeView;
    private string runtimeHighlightedNodeIdentifier;

    private Vector2 cachedMousePosition;
    private string currentFilePath;
    private TextField graphIdentifierField;
    private TextField graphTagsField;

    // Scenario documents are stored as "<identifier>.scenario.json"; node-layout sidecars
    // are stored as "<identifier>.scenario.editor.json".
    private const string ScenarioExtension = ".scenario.json";
    private const string ScenarioEditorExtension = ".scenario.editor.json";

    // 자동 배치(AutoLayoutNodes) 튜닝 파라미터.
    // - AutoLayoutColumnSpacing: 열(depth) 간 가로 간격.
    // - AutoLayoutRowSpacing: 같은 열에 세로로 쌓이는 노드 간 세로 간격.
    // - AutoLayoutColumnsPerBand: 한 밴드(가로 줄)에 들어갈 최대 열 개수.
    //   이 값을 넘어가면 아래 밴드로 접고 진행 방향을 좌우 반전한다.
    private const float AutoLayoutColumnSpacing = 380f;
    private const float AutoLayoutRowSpacing = 240f;
    private const int AutoLayoutColumnsPerBand = 6;

    /// <summary>
    /// Ensures a scenario file path uses the ".scenario.json" extension. Paths already
    /// ending in ".scenario.json" are returned unchanged; a plain ".json" path is upgraded.
    /// </summary>
    private static string NormalizeScenarioPath(string path)
    {
      if (string.IsNullOrEmpty(path))
        return path;

      if (path.EndsWith(ScenarioExtension, StringComparison.OrdinalIgnoreCase))
        return path;

      if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        return path.Substring(0, path.Length - ".json".Length) + ScenarioExtension;

      return path + ScenarioExtension;
    }

    /// <summary>
    /// Returns the editor node-layout sidecar path for a scenario file path.
    /// </summary>
    private static string GetEditorSidecarPath(string scenarioPath)
    {
      if (string.IsNullOrEmpty(scenarioPath))
        return scenarioPath;

      if (scenarioPath.EndsWith(ScenarioExtension, StringComparison.OrdinalIgnoreCase))
        return scenarioPath.Substring(0, scenarioPath.Length - ScenarioExtension.Length) + ScenarioEditorExtension;

      // Fallback for legacy plain ".json" paths.
      if (scenarioPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        return scenarioPath.Substring(0, scenarioPath.Length - ".json".Length) + ScenarioEditorExtension;

      return scenarioPath + ScenarioEditorExtension;
    }

    /// <summary>
    /// Derives the scenario identifier from a file path, stripping both the ".scenario.json"
    /// (or legacy ".json") extension and any residual ".scenario" suffix.
    /// </summary>
    private static string GetScenarioIdentifierFromPath(string path)
    {
      var name = Path.GetFileNameWithoutExtension(path);
      if (!string.IsNullOrEmpty(name) && name.EndsWith(".scenario", StringComparison.OrdinalIgnoreCase))
        name = name.Substring(0, name.Length - ".scenario".Length);

      return name;
    }

    [MenuItem("Tools/Multiplayer Infrastructure/Scenario Graph Editor")]
    public static void Open()
    {
      var window = GetWindow<ScenarioGraphAuthoringWindow>();
      window.titleContent = new GUIContent("Scenario Graph Editor");
      window.minSize = new Vector2(900f, 500f);
      window.Show();
    }

    /// <summary>
    /// Opens scenario documents directly from the Project window. Returning false for
    /// every other TextAsset preserves Unity's normal asset-opening behaviour.
    /// </summary>
    [OnOpenAsset]
    public static bool OpenScenarioTextAsset(int instanceID, int line)
    {
      var asset = EditorUtility.InstanceIDToObject(instanceID) as TextAsset;
      var path = asset == null ? null : AssetDatabase.GetAssetPath(asset);
      if (string.IsNullOrEmpty(path) || !path.EndsWith(ScenarioExtension, StringComparison.OrdinalIgnoreCase))
        return false;

      var window = GetWindow<ScenarioGraphAuthoringWindow>();
      window.titleContent = new GUIContent("Scenario Graph Editor");
      window.minSize = new Vector2(900f, 500f);
      window.Show();
      window.OpenGraphFromPath(Path.GetFullPath(path));
      return true;
    }

    private void OnEnable()
    {
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
      EditorApplication.update += SyncRuntimeScenarioController;
      ConstructUI();
      CreateGraphView();
      CreateDebugPanel();
      CreateEditTab();
      CreateSearchPanel();
      CreateInspector();
      CreateSearchWindow();
      BindGraphEvents();
      LoadBlankGraph();
      SetActiveTab(true);
      SyncRuntimeHighlight();
    }

    private void OnDisable()
    {
      EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
      EditorApplication.update -= SyncRuntimeScenarioController;
      UnbindRuntimeScenarioController();
      ClearRuntimeHighlight();

      rootVisualElement.UnregisterCallback<KeyDownEvent>(OnGlobalKeyDown, TrickleDown.TrickleDown);

      if (mainContainer != null && mainContainer.parent != null)
      {
        rootVisualElement.Remove(mainContainer);
      }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
      if (state == PlayModeStateChange.EnteredPlayMode)
      {
        SyncRuntimeHighlight();
        return;
      }

      if (state == PlayModeStateChange.ExitingPlayMode)
      {
        UnbindRuntimeScenarioController();
        ClearRuntimeVisitState();
        ClearRuntimeHighlight();
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

      var fileMenu = new ToolbarMenu { text = "File" };
      fileMenu.menu.AppendAction("New", _ => LoadBlankGraph());
      fileMenu.menu.AppendAction("Open", _ => OpenGraphFromJson());
      fileMenu.menu.AppendAction("Open Selected Scenario TextAsset", _ => OpenSelectedScenarioTextAsset(),
        _ => GetSelectedScenarioTextAsset() != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
      fileMenu.menu.AppendAction("Save", _ => SaveGraphToJson());
      fileMenu.menu.AppendAction("Save As...", _ => SaveGraphToJsonAs());
      fileMenu.menu.AppendAction("Validate", _ => ValidateGraphUsingRuntimeValidator());
      toolbar.Add(fileMenu);

      var addNodeButton = new ToolbarButton(OpenCreateNodeMenu) { text = "+" };
      toolbar.Add(addNodeButton);

      var searchButton = new ToolbarButton(OpenSearchPanel) { text = "Find" };
      toolbar.Add(searchButton);

      graphIdentifierField = new TextField
      {
        label = "Graph ID"
      };
      graphIdentifierField.style.width = 340f;
      graphIdentifierField.RegisterValueChangedCallback(evt =>
      {
        EnsureGraphData();
        graphData.Identifier = evt.newValue?.Trim() ?? string.Empty;
      });
      toolbar.Add(graphIdentifierField);

      graphTagsField = new TextField
      {
        label = "Tags(csv)"
      };
      graphTagsField.style.width = 420f;
      graphTagsField.RegisterValueChangedCallback(evt =>
      {
        EnsureGraphData();
        graphData.Tags = ParseCsvTags(evt.newValue);
      });
      toolbar.Add(graphTagsField);

      rootVisualElement.Add(toolbar);

      var tabs = new Toolbar();
      graphTabButton = new ToolbarButton(() => SetActiveTab(true)) { text = "Graph" };
      editTabButton = new ToolbarButton(() => SetActiveTab(false)) { text = "Edit" };
      tabs.Add(graphTabButton);
      tabs.Add(editTabButton);
      rootVisualElement.Add(tabs);

      EnsureGraphData();
      RefreshGraphIdentifierField();
      RefreshGraphTagsField();
    }

    private void CreateGraphView()
    {
      mainContainer = new VisualElement { name = "ScenarioMainContainer" };
      mainContainer.style.flexGrow    = 1f;
      mainContainer.style.flexShrink  = 1f;  // 디버그 패널이 공간을 차지하면 축소될 수 있어야 함
      mainContainer.style.flexDirection = FlexDirection.Row;

      graphHost = new VisualElement { name = "ScenarioGraphHost" };
      graphHost.style.flexGrow = 1f;
      graphHost.style.flexShrink = 1f;
      graphHost.style.overflow = Overflow.Hidden; // 검색 패널 오버레이가 graphHost 경계 안에만 보이도록
      graphHost.style.backgroundColor = new StyleColor(new Color(0.12f, 0.12f, 0.12f, 1f));

      graphView = new ScenarioGraphView(this) { name = "ScenarioGraphView" };
      graphView.style.flexGrow = 1f;
      graphView.style.flexShrink = 1f;
      graphHost.Add(graphView);

      inspectorView = new ScenarioInspectorView(this) { name = "ScenarioInspectorView" };
      inspectorView.style.flexGrow = 1f;

      runtimeHistoryView = new ScenarioRuntimeHistoryView { name = "ScenarioRuntimeHistoryView" };
      runtimeHistoryView.OnNodeSelected = FocusNodeByIdentifier;

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
      inspectorPanel.Add(runtimeHistoryView);
      inspectorPanel.Add(inspectorScroll);

      mainContainer.Add(graphHost);
      mainContainer.Add(inspectorPanel);

      rootVisualElement.Add(mainContainer);
      // 디버그 패널은 mainContainer 아래에 배치한다. CreateDebugPanel() 에서 추가된다.
    }

    private void CreateDebugPanel()
    {
      debugPanelView = new ScenarioDebugPanelView();
      debugPanelView.OnNodeFocusRequested = FocusNodeByIdentifier;
      rootVisualElement.Add(debugPanelView);
    }

    private void CreateEditTab()
    {
      editContainer = new VisualElement { name = "ScenarioEditContainer" };
      editContainer.style.flexGrow = 1f;
      editContainer.style.paddingLeft = 12;
      editContainer.style.paddingRight = 12;
      editContainer.style.paddingTop = 12;

      editContainer.Add(new Label("Scenario Ingame Requirements")
      {
        style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, marginBottom = 6 }
      });
      editContainer.Add(new HelpBox(
        "Requirements 작업은 기존과 같이 별도 패널에서 열립니다. 현재 Graph Editor에서 연 시나리오 파일이 자동으로 공유됩니다.",
        HelpBoxMessageType.Info));
      editContainer.Add(new Button(OpenRequirementsWindow) { text = "Open Scenario Ingame Requirements" });
      editContainer.Add(new Button(OpenSelectedScenarioTextAsset) { text = "Open Selected Scenario TextAsset" });
      rootVisualElement.Add(editContainer);
    }

    private void SetActiveTab(bool showGraph)
    {
      if (mainContainer != null) mainContainer.style.display = showGraph ? DisplayStyle.Flex : DisplayStyle.None;
      if (debugPanelView != null) debugPanelView.style.display = showGraph ? DisplayStyle.Flex : DisplayStyle.None;
      if (editContainer != null) editContainer.style.display = showGraph ? DisplayStyle.None : DisplayStyle.Flex;
      graphTabButton?.SetEnabled(!showGraph);
      editTabButton?.SetEnabled(showGraph);
    }

    private TextAsset GetCurrentScenarioTextAsset()
    {
      if (string.IsNullOrEmpty(currentFilePath)) return null;
      var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
      if (string.IsNullOrEmpty(projectRoot)) return null;
      var relativePath = currentFilePath.Replace(projectRoot + Path.DirectorySeparatorChar, string.Empty)
        .Replace(Path.DirectorySeparatorChar, '/');
      return AssetDatabase.LoadAssetAtPath<TextAsset>(relativePath);
    }

    private static TextAsset GetSelectedScenarioTextAsset()
    {
      var asset = Selection.activeObject as TextAsset;
      var path = asset == null ? null : AssetDatabase.GetAssetPath(asset);
      return !string.IsNullOrEmpty(path) && path.EndsWith(ScenarioExtension, StringComparison.OrdinalIgnoreCase) ? asset : null;
    }

    private void OpenRequirementsWindow()
    {
      var scenarioAsset = GetCurrentScenarioTextAsset();
      if (scenarioAsset == null)
      {
        EditorUtility.DisplayDialog("Scenario Ingame Requirements", "프로젝트의 .scenario.json TextAsset을 먼저 열어 주세요.", "확인");
        return;
      }

      ScenarioRequirementsWindow.Open(scenarioAsset);
    }

    private void OpenSelectedScenarioTextAsset()
    {
      var asset = GetSelectedScenarioTextAsset();
      if (asset == null)
      {
        EditorUtility.DisplayDialog("Open Scenario", "Project 창에서 .scenario.json TextAsset을 선택해 주세요.", "확인");
        return;
      }

      var path = AssetDatabase.GetAssetPath(asset);
      OpenGraphFromPath(Path.GetFullPath(path));
      SetActiveTab(true);
    }

    private void CreateSearchPanel()
    {
      searchPanelView = new ScenarioSearchPanelView();
      searchPanelView.OnResultSelected = FocusNodeByIdentifier;
      searchPanelView.OnQueryChanged   = HandleSearchQuery;
      searchPanelView.OnClosed         = () => { /* 포커스를 graphView 로 돌려줌 */ graphView?.Focus(); };

      // graphHost 위에 절대 위치 오버레이로 추가
      // graphHost 가 아직 null 이면 rootVisualElement 에 임시 추가 후 CreateGraphView 이후 재배치
      if (graphHost != null)
        graphHost.Add(searchPanelView);
      else
        rootVisualElement.Add(searchPanelView);

      // Cmd/Ctrl+F: rootVisualElement 에서 키 이벤트를 잡는다
      // TrickleDown 으로 등록해서 GraphView 보다 먼저 처리
      rootVisualElement.RegisterCallback<KeyDownEvent>(OnGlobalKeyDown, TrickleDown.TrickleDown);
    }

    private void HandleSearchQuery(string query)
    {
      if (searchPanelView == null || graphData == null) return;
      var results = ScenarioNodeSearcher.Search(graphData, query);
      searchPanelView.SetResults(results, query);
    }

    /// <summary>에디터 윈도우 전역 키 핸들러. Cmd/Ctrl+F 로 검색 패널을 토글한다.</summary>
    private void OnGlobalKeyDown(KeyDownEvent evt)
    {
      bool isMac    = Application.platform == RuntimePlatform.OSXEditor;
      bool modifier = isMac ? evt.commandKey : evt.ctrlKey;

      if (modifier && evt.keyCode == KeyCode.F)
      {
        if (searchPanelView == null) return;

        if (searchPanelView.IsOpen)
          searchPanelView.Close();
        else
          searchPanelView.Open();

        evt.StopPropagation();
        evt.PreventDefault();
      }
    }

    /// <summary>Cmd/Ctrl+F 로 검색 패널을 여는 public API (툴바 버튼 등에서 호출 가능).</summary>
    public void OpenSearchPanel()
    {
      searchPanelView?.Open();
    }

    private void RefreshDebugPanel()
    {
      if (debugPanelView == null) return;
      var items = ScenarioGraphDiagnostics.Run(graphData);
      debugPanelView.Refresh(items);
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
      if (newType != ScenarioNodeType.Parallel)
      {
        newData.NextIdentifier = oldData.NextIdentifier;
      }

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
      SyncRuntimeHighlight();
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();

      return newView;
    }

    private void BindGraphEvents()
    {
      graphView.onNodeSelected = node => inspectorView.SetTarget(node);
    }

    private void SyncRuntimeHighlight()
    {
      if (!Application.isPlaying)
      {
        UnbindRuntimeScenarioController();
        ClearRuntimeVisitState();
        ClearRuntimeHighlight();
        return;
      }

      var controller = ScenarioController.Instance;
      if (controller == null)
      {
        UnbindRuntimeScenarioController();
        ClearRuntimeVisitState();
        ClearRuntimeHighlight();
        return;
      }

      BindRuntimeScenarioController(controller);

      if (graphData == null)
      {
        ClearRuntimeHighlight();
        return;
      }

      SyncRuntimeVisitState();

      if (controller.CurrentGraph != null
          && controller.CurrentGraph.Identifier == graphData.Identifier)
      {
        UpdateRuntimeHighlight(controller.CurrentNode?.Identifier);
      }
      else
      {
        ClearRuntimeHighlight();
      }
    }

    private void SyncRuntimeScenarioController()
    {
      if (!Application.isPlaying)
      {
        return;
      }

      var controller = ScenarioController.Instance;
      if (runtimeScenarioController != controller)
      {
        SyncRuntimeHighlight();
      }
    }

    private void BindRuntimeScenarioController(ScenarioController controller)
    {
      if (runtimeScenarioController == controller)
      {
        return;
      }

      UnbindRuntimeScenarioController();

      runtimeScenarioController = controller;
      runtimeScenarioController.OnScenarioStarted += HandleRuntimeScenarioStarted;
      runtimeScenarioController.OnScenarioEnded += HandleRuntimeScenarioEnded;
      runtimeScenarioController.OnNodeChanged += HandleRuntimeNodeChanged;
    }

    private void UnbindRuntimeScenarioController()
    {
      if (runtimeScenarioController == null)
      {
        return;
      }

      runtimeScenarioController.OnScenarioStarted -= HandleRuntimeScenarioStarted;
      runtimeScenarioController.OnScenarioEnded -= HandleRuntimeScenarioEnded;
      runtimeScenarioController.OnNodeChanged -= HandleRuntimeNodeChanged;
      runtimeScenarioController = null;
    }

    private void HandleRuntimeScenarioStarted()
    {
      SyncRuntimeHighlight();
    }

    private void HandleRuntimeScenarioEnded()
    {
      ClearRuntimeHighlight();
      RefreshRuntimeHistoryView();
    }

    private void HandleRuntimeNodeChanged(IScenarioNode node)
    {
      if (!Application.isPlaying)
      {
        return;
      }

      if (runtimeScenarioController == null)
      {
        SyncRuntimeHighlight();
        return;
      }

      if (graphData == null)
      {
        ClearRuntimeHighlight();
        RefreshRuntimeHistoryView();
        return;
      }

      UpdateRuntimeVisitState(node?.Identifier);
      UpdateRuntimeHighlight(node?.Identifier);
    }

    private void UpdateRuntimeHighlight(string nodeIdentifier)
    {
      if (runtimeHighlightedNodeIdentifier == nodeIdentifier)
      {
        return;
      }

      ClearRuntimeHighlight();

      if (string.IsNullOrWhiteSpace(nodeIdentifier))
      {
        return;
      }

      if (nodeViews.TryGetValue(nodeIdentifier, out var nodeView))
      {
        runtimeHighlightedNodeView = nodeView;
        runtimeHighlightedNodeIdentifier = nodeIdentifier;
        runtimeHighlightedNodeView.SetExecutionHighlighted(true);
      }
    }

    private void ClearRuntimeHighlight()
    {
      if (runtimeHighlightedNodeView != null)
      {
        runtimeHighlightedNodeView.SetExecutionHighlighted(false);
      }

      runtimeHighlightedNodeView = null;
      runtimeHighlightedNodeIdentifier = null;
    }

    private void SyncRuntimeVisitState()
    {
      if (runtimeScenarioController == null || graphData == null)
      {
        ClearRuntimeVisitState();
        RefreshRuntimeHistoryView();
        return;
      }

      foreach (var pair in nodeViews)
      {
        pair.Value?.SetRuntimeVisitOrders(runtimeScenarioController.GetNodeVisitOrders(graphData.Identifier, pair.Key));
      }

      RefreshRuntimeHistoryView();
    }

    private void UpdateRuntimeVisitState(string nodeIdentifier)
    {
      if (runtimeScenarioController == null || string.IsNullOrWhiteSpace(nodeIdentifier))
      {
        return;
      }

      if (nodeViews.TryGetValue(nodeIdentifier, out var nodeView))
      {
        nodeView.SetRuntimeVisitOrders(runtimeScenarioController.GetNodeVisitOrders(graphData.Identifier, nodeIdentifier));
      }

      RefreshRuntimeHistoryView();
    }

    private void ClearRuntimeVisitState()
    {
      foreach (var nodeView in nodeViews.Values)
      {
        nodeView?.SetRuntimeVisitOrders(Array.Empty<int>());
      }

      RefreshRuntimeHistoryView();
    }

    private void RefreshRuntimeHistoryView()
    {
      if (runtimeHistoryView == null)
      {
        return;
      }

      runtimeHistoryView.SetEntries(BuildRuntimeHistoryEntries());
    }

    private IReadOnlyList<ScenarioRuntimeHistoryEntry> BuildRuntimeHistoryEntries()
    {
      if (runtimeScenarioController == null || graphData == null || string.IsNullOrWhiteSpace(graphData.Identifier))
      {
        return Array.Empty<ScenarioRuntimeHistoryEntry>();
      }

      var entries = new List<ScenarioRuntimeHistoryEntry>();

      foreach (var pair in nodeViews)
      {
        var nodeView = pair.Value;
        if (nodeView == null)
        {
          continue;
        }

        var visitOrders = runtimeScenarioController.GetNodeVisitOrders(graphData.Identifier, pair.Key);
        if (visitOrders == null || visitOrders.Count == 0)
        {
          continue;
        }

        var displayLabel = nodeView.Data != null
          ? $"{nodeView.Data.Identifier} [{nodeView.Data.NodeType}]"
          : pair.Key;

        foreach (var visitOrder in visitOrders)
        {
          entries.Add(new ScenarioRuntimeHistoryEntry(visitOrder, pair.Key, displayLabel, nodeView.Data));
        }
      }

      entries.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
      return entries;
    }

    private void FocusNodeByIdentifier(string nodeIdentifier)
    {
      if (string.IsNullOrWhiteSpace(nodeIdentifier))
      {
        return;
      }

      if (!nodeViews.TryGetValue(nodeIdentifier, out var nodeView) || nodeView == null)
      {
        return;
      }

      inspectorView.SetTarget(nodeView);

      graphView.ClearSelection();
      graphView.AddToSelection(nodeView);
      graphView.FrameSelection();
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
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();
      return nodeView;
    }

    private void LoadBlankGraph()
    {
      graphData = new ScenarioGraph { Identifier = "new_scenario_graph" };
      nodeViews.Clear();
      graphView.ClearGraph();
      inspectorView.SetTarget(null);
      currentFilePath = null;
      RefreshGraphIdentifierField();
      RefreshGraphTagsField();
      ClearRuntimeHighlight();
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();
    }

    private void EnsureGraphData()
    {
      if (graphData == null)
      {
        graphData = new ScenarioGraph { Identifier = "new_scenario_graph" };
      }

      if (string.IsNullOrWhiteSpace(graphData.Identifier))
      {
        graphData.Identifier = "new_scenario_graph";
      }

      if (graphData.Tags == null)
      {
        graphData.Tags = Array.Empty<string>();
      }
    }

    private void RefreshGraphIdentifierField()
    {
      if (graphIdentifierField == null)
        return;

      graphIdentifierField.SetValueWithoutNotify(graphData?.Identifier ?? string.Empty);
    }

    private void RefreshGraphTagsField()
    {
      if (graphTagsField == null)
        return;

      var tags = graphData?.Tags ?? Array.Empty<string>();
      graphTagsField.SetValueWithoutNotify(string.Join(",", tags));
    }

    private static IReadOnlyList<string> ParseCsvTags(string csv)
    {
      if (string.IsNullOrWhiteSpace(csv))
      {
        return Array.Empty<string>();
      }

      return csv
        .Split(',')
        .Select(each => each.Trim())
        .Where(each => !string.IsNullOrWhiteSpace(each))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
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
      if (graphData == null || graphData.Nodes == null) return;

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

        if (other is ScenarioQuizNode quiz)
        {
          if (quiz.OnCorrectNextIdentifier == id)
          {
            quiz.OnCorrectNextIdentifier = null;
          }

          if (quiz.OnIncorrectNextIdentifier == id)
          {
            quiz.OnIncorrectNextIdentifier = null;
          }
        }
      }

      inspectorView.SetTarget(null);
      SyncRuntimeHighlight();
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();
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

        if (node is ScenarioQuizNode quiz)
        {
          if (quiz.OnCorrectNextIdentifier == oldId)
          {
            quiz.OnCorrectNextIdentifier = trimmed;
          }

          if (quiz.OnIncorrectNextIdentifier == oldId)
          {
            quiz.OnIncorrectNextIdentifier = trimmed;
          }
        }
      }

      nodeViews.Remove(oldId);
      nodeViews[trimmed] = nodeView;

      graphView.RebuildAllEdges();
      nodeView.RefreshTitle();
      SyncRuntimeHighlight();
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();

      return true;
    }

    private void OpenGraphFromJson()
    {
      var path = EditorUtility.OpenFilePanel("Open Scenario JSON", Application.dataPath, "json");
      if (string.IsNullOrEmpty(path)) return;

      OpenGraphFromPath(path);
    }

    private void OpenGraphFromPath(string path)
    {
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
        if (string.IsNullOrWhiteSpace(graphData.Identifier))
          graphData.Identifier = GetScenarioIdentifierFromPath(path);

        nodeViews.Clear();
        graphView.ClearGraph();

        // Load editor data
        var editorPath = GetEditorSidecarPath(path);
        ScenarioGraphEditorData editorData = null;
        if (File.Exists(editorPath))
        {
          var editorJson = File.ReadAllText(editorPath);
          editorData = JsonSerializer.Deserialize<ScenarioGraphEditorData>(
              editorJson,
              new JsonSerializerOptions { IncludeFields = true });
        }

        var orderedNodes = graphData.Nodes.Values.OrderBy(n => n.Identifier).ToList();
        const float spacingX = 360f;
        const float spacingY = 240f;
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
        RefreshGraphIdentifierField();
        RefreshGraphTagsField();

        ValidateResources(graphData);
        SyncRuntimeHighlight();
        RefreshRuntimeHistoryView();
        RefreshDebugPanel();
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

      // If we have a current path, save directly; otherwise fall back to Save As.
      if (string.IsNullOrEmpty(currentFilePath))
      {
        SaveGraphToJsonAs();
        return;
      }

      SaveGraphToPath(currentFilePath);
    }

    private void SaveGraphToJsonAs()
    {
      if (graphData == null || graphData.Nodes.Count == 0)
      {
        EditorUtility.DisplayDialog("Save Failed", "저장할 노드가 없습니다.", "확인");
        return;
      }

      var path = EditorUtility.SaveFilePanel("Save Scenario JSON", Application.dataPath, "scenario_graph.scenario.json", "json");
      if (string.IsNullOrEmpty(path)) return;

      SaveGraphToPath(NormalizeScenarioPath(path));
    }

    private void SaveGraphToPath(string path)
    {
      if (string.IsNullOrEmpty(path)) return;

      EnsureGraphData();
      if (string.IsNullOrWhiteSpace(graphData.Identifier))
      {
        graphData.Identifier = GetScenarioIdentifierFromPath(path);
        RefreshGraphIdentifierField();
      }

      string json;

      try
      {
        json = ScenarioGraphLoader.SaveToJson(graphData, true);
      }
      catch (Exception ex)
      {
        var dialogMessage =
          "이 시나리오는 무결성 검증에 실패했습니다. 각 노드의 필수 속성을 입력했는지 확인하세요.\n" +
          "검증에 실패하더라도 파일을 저장할 수 있습니다. 계속하시겠습니까?\n\n" +
          ex.Message;
        var choice = EditorUtility.DisplayDialogComplex("검증 실패", dialogMessage, "예", "아니오", null);
        if (choice != 0)
        {
          return;
        }

        try
        {
          json = ScenarioGraphLoader.SaveToJson(graphData, false);
        }
        catch (Exception saveEx)
        {
          EditorUtility.DisplayDialog("Save Failed", saveEx.Message, "확인");
          Debug.LogException(saveEx);
          return;
        }
      }

      try
      {
        File.WriteAllText(path, json);

        // Save editor data
        var editorData = new ScenarioGraphEditorData();
        foreach (var pair in nodeViews)
        {
          var nodeView = pair.Value;
          var rect = nodeView.GetPosition();
          editorData.NodePositions[pair.Key] = new SerializableVector2(rect.position);
        }
        var editorJson = JsonSerializer.Serialize(
            editorData,
            new JsonSerializerOptions
            {
              WriteIndented = true,
              IncludeFields = true
            });
        var editorPath = GetEditorSidecarPath(path);
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

    /// <summary>
    /// 엣지 연결/해제 등 외부에서 그래프 구조가 변경되었을 때 호출한다.
    /// 디버그 패널을 다시 실행한다.
    /// </summary>
    public void NotifyGraphStructureChanged()
    {
      RefreshDebugPanel();
    }

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
        // 노드로 등록되지 않은 식별자(completionConditionIdentifier 등)는 건너뛴다.
        if (!graphData.Nodes.TryGetValue(current, out var node))
          continue;
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

      // 뱀 형태(serpentine / boustrophedon) 배치.
      // - 연결된 노드는 depth(열)가 1씩 증가하므로 가까이 배치된다.
      // - 같은 depth에 동시에 연결된 노드들은 세로로 나란히 쌓는다.
      // - depth가 columnsPerBand 이상 오른쪽으로 이어지면 아래 밴드로 내려가고
      //   진행 방향을 좌우 반전시켜, 오른쪽 끝에서 왼쪽으로 되돌아온다.
      // 간격/접힘 기준은 클래스 상단의 AutoLayout* 상수로 조정한다.
      const float spacingX = AutoLayoutColumnSpacing;
      const float spacingY = AutoLayoutRowSpacing;
      const int columnsPerBand = AutoLayoutColumnsPerBand;

      // 각 depth(열)에 몇 개의 노드가 세로로 쌓이는지 미리 계산해,
      // 밴드가 아래로 내려갈 때 겹치지 않도록 밴드 높이를 잡는다.
      var groupsByDepth = depth
        .GroupBy(kvp => kvp.Value)
        .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).OrderBy(id => id).ToList());

      // 밴드(bandIndex = depth / columnsPerBand) 별로 가장 많이 쌓인 열의 노드 수를 구한다.
      var bandRowCount = new Dictionary<int, int>();
      foreach (var kvp in groupsByDepth)
      {
        int band = kvp.Key / columnsPerBand;
        int rows = kvp.Value.Count;
        if (!bandRowCount.TryGetValue(band, out var existing) || rows > existing)
          bandRowCount[band] = rows;
      }

      // 각 밴드의 시작 y 오프셋(윗쪽 누적 높이)을 계산한다.
      var bandYOffset = new Dictionary<int, float>();
      float accumulatedY = 0f;
      int maxBand = bandRowCount.Count > 0 ? bandRowCount.Keys.Max() : 0;
      for (int band = 0; band <= maxBand; band++)
      {
        bandYOffset[band] = accumulatedY;
        int rows = bandRowCount.TryGetValue(band, out var r) ? r : 1;
        // 밴드 사이에 한 칸 여유를 두어 세로로 쌓인 노드와 다음 밴드가 겹치지 않게 한다.
        accumulatedY += (rows + 1) * spacingY;
      }

      foreach (var kvp in groupsByDepth.OrderBy(g => g.Key))
      {
        int d = kvp.Key;
        var nodesInDepth = kvp.Value;

        int band = d / columnsPerBand;
        int columnInBand = d % columnsPerBand;

        // 짝수 밴드는 왼→오, 홀수 밴드는 오→왼 방향으로 진행한다.
        bool leftToRight = (band % 2) == 0;
        int effectiveColumn = leftToRight ? columnInBand : (columnsPerBand - 1 - columnInBand);

        float x = effectiveColumn * spacingX;
        float yBase = bandYOffset.TryGetValue(band, out var yo) ? yo : band * spacingY;

        for (int i = 0; i < nodesInDepth.Count; i++)
        {
          var id = nodesInDepth[i];
          if (!nodeViews.TryGetValue(id, out var view)) continue;
          var pos = new Vector2(x, yBase + i * spacingY);
          view.SetPosition(new Rect(pos, view.DefaultSize));
        }
      }
    }

    private static IEnumerable<string> GetOutgoingTargets(IScenarioNode node)
    {
      if (node == null) yield break;

      if (node is not ScenarioParallelNode && !string.IsNullOrEmpty(node.NextIdentifier))
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
        if (!string.IsNullOrEmpty(node.NPCIdentifier)
            && Registry.Registry.Get<GameObject>(RegistryType.Npc, node.NPCIdentifier) == null
            && Registry.Registry.Get<GameObject>(RegistryType.Entity, node.NPCIdentifier) == null)
        {
          missingNPCs.Add(node.NPCIdentifier);
        }
      }
      var playerMoveNodes = graph.Nodes.Values.OfType<ScenarioPlayerMoveNode>().Where(n => n.DestinationType == ScenarioMoveDestinationType.Waypoint);
      var missingWaypoints = new List<string>();
      foreach (var node in playerMoveNodes)
      {
        if (!string.IsNullOrEmpty(node.DestinationIdentifier)
            && !Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, node.DestinationIdentifier, out _)
            && !Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, node.DestinationIdentifier, out _))
        {
          missingWaypoints.Add(node.DestinationIdentifier);
        }
      }
      var npcMoveNodes = graph.Nodes.Values.OfType<ScenarioNPCMoveNode>().Where(n => n.DestinationType == ScenarioMoveDestinationType.Waypoint);
      foreach (var node in npcMoveNodes)
      {
        if (!string.IsNullOrEmpty(node.DestinationIdentifier)
            && !Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, node.DestinationIdentifier, out _)
            && !Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, node.DestinationIdentifier, out _))
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
