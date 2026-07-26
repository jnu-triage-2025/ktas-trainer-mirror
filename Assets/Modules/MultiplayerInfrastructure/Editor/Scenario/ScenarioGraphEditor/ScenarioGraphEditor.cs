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
    private VisualElement definitionsContainer;
    private ToolbarButton graphTabButton;
    private ToolbarButton definitionsTabButton;
    private ScenarioActingNpcEditorView actingNpcEditorView;
    private ScenarioWaypointEditorView waypointEditorView;

    private ScenarioGraph graphData = new ScenarioGraph();
    private readonly Dictionary<string, ScenarioNodeView> nodeViews = new Dictionary<string, ScenarioNodeView>();

    private ScenarioController runtimeScenarioController;
    private ScenarioNodeView runtimeHighlightedNodeView;
    private string runtimeHighlightedNodeIdentifier;

    private Vector2 cachedMousePosition;
    private string currentFilePath;
    private TextField graphIdentifierField;
    private TextField graphTagsField;
    private TextField defaultEntrypointField;

    private const int UndoHistoryLimit = 100;
    private const double UndoCoalesceDelaySeconds = 0.3d;
    private readonly Stack<GraphSnapshot> undoHistory = new Stack<GraphSnapshot>();
    private readonly Stack<GraphSnapshot> redoHistory = new Stack<GraphSnapshot>();
    private GraphSnapshot lastCommittedSnapshot;
    private GraphSnapshot pendingSnapshot;
    private double pendingSnapshotChangedAt;
    private double nextUndoPollAt;
    private bool isRestoringSnapshot;

    [Serializable]
    private sealed class GraphSnapshot
    {
      public string GraphJson;
      public ScenarioGraphEditorData EditorData;
      public bool IsEmpty;
      public string Identifier;
      public string[] Tags;
      public string[] QuestDefinitionIncludes;
      public string DefaultEntrypoint;

      public string Fingerprint => GraphJson + "\n" + JsonSerializer.Serialize(
        EditorData,
        new JsonSerializerOptions { IncludeFields = true });
    }

    // Scenario documents are stored as "<identifier>.scenario.json"; node-layout sidecars
    // are stored as "<identifier>.scenario.editor.json".
    private const string ScenarioExtension = ".scenario.json";
    private const string ScenarioEditorExtension = ".scenario.editor.json";

    // Sugiyama 계층형 자동 배치 튜닝 파라미터.
    // 레이어는 좌→우 진행이며, 같은 부모에서 뻗는 후속 노드는 다음
    // 레이어의 동일한 x 좌표에 세로로 쌓인다.
    private const float AutoLayoutColumnSpacing = 420f;
    private const float AutoLayoutRowSpacing = 240f;
    private const int AutoLayoutCrossingReductionPasses = 6;
    private const int AutoLayoutCoordinateAssignmentPasses = 4;

    private readonly struct LayoutScore
    {
      public readonly int CrossingCount;
      public readonly float VerticalEdgeCost;

      public LayoutScore(int crossingCount, float verticalEdgeCost)
      {
        CrossingCount = crossingCount;
        VerticalEdgeCost = verticalEdgeCost;
      }
    }

    private sealed class LayeredLayoutGraph
    {
      public Dictionary<string, int> Layer { get; } = new Dictionary<string, int>();
      public Dictionary<string, List<string>> Forward { get; } = new Dictionary<string, List<string>>();
      public Dictionary<string, List<string>> Incoming { get; } = new Dictionary<string, List<string>>();
    }

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
      EditorApplication.update += TrackUndoState;
      ConstructUI();
      CreateGraphView();
      CreateDebugPanel();
      CreateDefinitionsTab();
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
      EditorApplication.update -= TrackUndoState;
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

      // File 버튼은 클릭 시 GenericMenu 를 연다.
      // ToolbarMenu 의 DropdownMenu 는 이 Unity 버전에 하위 메뉴(서브메뉴) API 가 없어,
      // "Open Recent" 하위 메뉴를 구성할 수 있는 GenericMenu 를 사용한다.
      var fileButton = new ToolbarButton(ShowFileMenu) { text = "File ▼" };
      toolbar.Add(fileButton);

      var editButton = new ToolbarButton(ShowEditMenu) { text = "Edit ▼" };
      toolbar.Add(editButton);

      var viewButton = new ToolbarButton(ShowViewMenu) { text = "View ▼" };
      toolbar.Add(viewButton);

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

      // startNodeIdentifier 없이 시나리오를 시작할 때 사용되는 기본 진입 노드(DefaultInit) 식별자.
      // 이 값을 가진 노드는 그래프에서 금색 "★ Default Init" 배지로 강조된다.
      defaultEntrypointField = new TextField
      {
        label = "Default Init",
        tooltip = "startNodeIdentifier 없이 시나리오를 시작할 때 사용할 기본 진입 노드 식별자(defaultEntrypoint)"
      };
      defaultEntrypointField.style.width = 320f;
      defaultEntrypointField.RegisterValueChangedCallback(evt =>
      {
        EnsureGraphData();
        graphData.DefaultEntrypoint = string.IsNullOrWhiteSpace(evt.newValue) ? null : evt.newValue.Trim();
        RefreshDefaultEntrypointMarkers();
        RefreshDebugPanel();
      });
      toolbar.Add(defaultEntrypointField);

      rootVisualElement.Add(toolbar);

      var tabs = new Toolbar();
      graphTabButton = new ToolbarButton(() => SetActiveTab(true)) { text = "Graph" };
      definitionsTabButton = new ToolbarButton(() => SetActiveTab(false)) { text = "Definitions" };
      tabs.Add(graphTabButton);
      tabs.Add(definitionsTabButton);
      rootVisualElement.Add(tabs);

      EnsureGraphData();
      RefreshGraphIdentifierField();
      RefreshGraphTagsField();
      RefreshDefaultEntrypointField();
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

    private void CreateDefinitionsTab()
    {
      definitionsContainer = new VisualElement { name = "ScenarioDefinitionsContainer" };
      definitionsContainer.style.flexGrow = 1f;
      definitionsContainer.style.paddingLeft = 12;
      definitionsContainer.style.paddingRight = 12;
      definitionsContainer.style.paddingTop = 12;

      var scroll = new ScrollView();
      scroll.style.flexGrow = 1f;
      actingNpcEditorView = new ScenarioActingNpcEditorView(
        () => graphData,
        () =>
        {
          RefreshDebugPanel();
        });
      scroll.Add(actingNpcEditorView);
      waypointEditorView = new ScenarioWaypointEditorView(
        () => graphData,
        () => RefreshDebugPanel());
      scroll.Add(waypointEditorView);
      scroll.Add(new Label("Scenario Ingame Requirements")
      {
        style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14, marginBottom = 6 }
      });
      scroll.Add(new HelpBox(
        "Requirements 작업은 기존과 같이 별도 패널에서 열립니다. 현재 Graph Editor에서 연 시나리오 파일이 자동으로 공유됩니다.",
        HelpBoxMessageType.Info));
      scroll.Add(new Button(OpenRequirementsWindow) { text = "Open Scenario Ingame Requirements" });
      scroll.Add(new Button(OpenSelectedScenarioTextAsset) { text = "Open Selected Scenario TextAsset" });
      definitionsContainer.Add(scroll);
      rootVisualElement.Add(definitionsContainer);
    }

    private void SetActiveTab(bool showGraph)
    {
      if (mainContainer != null) mainContainer.style.display = showGraph ? DisplayStyle.Flex : DisplayStyle.None;
      if (debugPanelView != null) debugPanelView.style.display = showGraph ? DisplayStyle.Flex : DisplayStyle.None;
      if (definitionsContainer != null) definitionsContainer.style.display = showGraph ? DisplayStyle.None : DisplayStyle.Flex;
      graphTabButton?.SetEnabled(!showGraph);
      definitionsTabButton?.SetEnabled(showGraph);
      if (!showGraph)
      {
        actingNpcEditorView?.Refresh();
        waypointEditorView?.Refresh();
      }
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

    /// <summary>에디터 윈도우 전역 키 핸들러.</summary>
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
      }

      if (modifier && evt.keyCode == KeyCode.Z)
      {
        if (evt.shiftKey)
          RedoGraphChange();
        else
          UndoGraphChange();

        evt.StopPropagation();
      }

      if (modifier && evt.keyCode == KeyCode.S)
      {
        if (evt.shiftKey)
          SaveGraphToJsonAs();
        else
          SaveGraphToJson();

        evt.StopPropagation();
      }
    }

    private void ResetUndoHistory()
    {
      undoHistory.Clear();
      redoHistory.Clear();
      pendingSnapshot = null;
      lastCommittedSnapshot = CaptureSnapshot();
    }

    private void TrackUndoState()
    {
      UpdateUndoState(false);
    }

    private void UpdateUndoState(bool force)
    {
      if (isRestoringSnapshot || graphView == null || graphData == null)
        return;

      var now = EditorApplication.timeSinceStartup;
      if (!force && now < nextUndoPollAt)
        return;
      nextUndoPollAt = now + 0.1d;

      var current = CaptureSnapshot();
      if (current == null)
        return;

      if (lastCommittedSnapshot == null)
      {
        lastCommittedSnapshot = current;
        return;
      }

      var comparison = pendingSnapshot ?? lastCommittedSnapshot;
      if (current.Fingerprint != comparison.Fingerprint)
      {
        pendingSnapshot = current;
        pendingSnapshotChangedAt = now;
      }

      if (pendingSnapshot != null &&
          now - pendingSnapshotChangedAt >= UndoCoalesceDelaySeconds)
      {
        CommitPendingSnapshot();
      }
    }

    private void CommitPendingSnapshot()
    {
      if (pendingSnapshot == null)
        return;

      PushSnapshot(undoHistory, lastCommittedSnapshot);
      lastCommittedSnapshot = pendingSnapshot;
      pendingSnapshot = null;
      redoHistory.Clear();
    }

    private void UndoGraphChange()
    {
      UpdateUndoState(true);
      CommitPendingSnapshot();
      if (undoHistory.Count == 0)
        return;

      PushSnapshot(redoHistory, lastCommittedSnapshot);
      RestoreSnapshot(undoHistory.Pop());
    }

    private void RedoGraphChange()
    {
      UpdateUndoState(true);
      CommitPendingSnapshot();
      if (redoHistory.Count == 0)
        return;

      PushSnapshot(undoHistory, lastCommittedSnapshot);
      RestoreSnapshot(redoHistory.Pop());
    }

    private static void PushSnapshot(Stack<GraphSnapshot> history, GraphSnapshot snapshot)
    {
      if (snapshot == null)
        return;

      if (history.Count >= UndoHistoryLimit)
      {
        var retained = history.Take(UndoHistoryLimit - 1).Reverse().ToArray();
        history.Clear();
        foreach (var item in retained)
          history.Push(item);
      }

      history.Push(snapshot);
    }

    private GraphSnapshot CaptureSnapshot()
    {
      if (graphData == null || graphView == null)
        return null;

      var editorData = new ScenarioGraphEditorData();
      foreach (var pair in nodeViews.OrderBy(each => each.Key))
      {
        if (pair.Value != null)
          editorData.NodePositions[pair.Key] = new SerializableVector2(pair.Value.GetPosition().position);
      }
      editorData.EdgeRoutes = graphView.CaptureEdgeRoutes();

      return new GraphSnapshot
      {
        GraphJson = ScenarioGraphLoader.SaveToJson(graphData, false),
        EditorData = editorData,
        IsEmpty = graphData.Nodes.Count == 0,
        Identifier = graphData.Identifier,
        Tags = graphData.Tags?.ToArray() ?? Array.Empty<string>(),
        QuestDefinitionIncludes = graphData.QuestDefinitionIncludes?.ToArray() ?? Array.Empty<string>(),
        DefaultEntrypoint = graphData.DefaultEntrypoint
      };
    }

    private void RestoreSnapshot(GraphSnapshot snapshot)
    {
      if (snapshot == null)
        return;

      isRestoringSnapshot = true;
      try
      {
        graphData = snapshot.IsEmpty
          ? new ScenarioGraph
          {
            Identifier = snapshot.Identifier,
            Tags = snapshot.Tags ?? Array.Empty<string>(),
            QuestDefinitionIncludes = snapshot.QuestDefinitionIncludes ?? Array.Empty<string>(),
            DefaultEntrypoint = snapshot.DefaultEntrypoint
          }
          : ScenarioGraphLoader.LoadFromJson(snapshot.GraphJson, false);
        nodeViews.Clear();
        graphView.ClearGraph();
        graphView.SetEdgeRoutes(snapshot.EditorData?.EdgeRoutes);

        foreach (var node in graphData.Nodes.Values.OrderBy(each => each.Identifier))
        {
          var nodeView = graphView.AddNodeView(node);
          var position = snapshot.EditorData != null &&
                         snapshot.EditorData.NodePositions.TryGetValue(node.Identifier, out var savedPosition)
            ? savedPosition.ToVector2()
            : Vector2.zero;
          nodeView.SetPosition(new Rect(position, nodeView.DefaultSize));
          nodeViews[node.Identifier] = nodeView;
        }

        graphView.RestoreEdges(nodeViews);
        inspectorView.SetTarget(null);
        RefreshGraphIdentifierField();
        RefreshGraphTagsField();
        RefreshDefaultEntrypointField();
        RefreshDefaultEntrypointMarkers();
        SyncRuntimeHighlight();
        RefreshRuntimeHistoryView();
        RefreshDebugPanel();
        lastCommittedSnapshot = snapshot;
        pendingSnapshot = null;
        Repaint();
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }
      finally
      {
        isRestoringSnapshot = false;
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
      RefreshDefaultEntrypointMarkers();
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
      RefreshDefaultEntrypointMarkers();
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
      RefreshDefaultEntrypointField();
      ClearRuntimeHighlight();
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();
      ResetUndoHistory();
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

    private void RefreshDefaultEntrypointField()
    {
      if (defaultEntrypointField == null)
        return;

      defaultEntrypointField.SetValueWithoutNotify(graphData?.DefaultEntrypoint ?? string.Empty);
    }

    /// <summary>
    /// graphData.DefaultEntrypoint 와 일치하는 노드 뷰에만 "★ Default Init" 배지를 표시한다.
    /// 그래프 로드/노드 생성·삭제·이름 변경·타입 변경 후 호출해 표시를 동기화한다.
    /// </summary>
    public void RefreshDefaultEntrypointMarkers()
    {
      var target = graphData?.DefaultEntrypoint;
      foreach (var pair in nodeViews)
      {
        pair.Value?.SetDefaultInitMarked(!string.IsNullOrEmpty(target) && pair.Key == target);
      }
    }

    /// <summary>
    /// 기본 진입 노드(defaultEntrypoint)를 설정한다. null/빈 값은 해제.
    /// 노드 우클릭 메뉴("Set as Default Init")에서 호출된다.
    /// </summary>
    public void SetDefaultEntrypoint(string identifier)
    {
      EnsureGraphData();
      graphData.DefaultEntrypoint = string.IsNullOrWhiteSpace(identifier) ? null : identifier.Trim();
      RefreshDefaultEntrypointField();
      RefreshDefaultEntrypointMarkers();
      RefreshDebugPanel();
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
      graphView.RemoveEdgeRoutesForNode(id);

      // 삭제된 노드가 기본 진입 노드(DefaultInit)였다면 해제한다.
      if (graphData.DefaultEntrypoint == id)
      {
        graphData.DefaultEntrypoint = null;
        RefreshDefaultEntrypointField();
      }

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
          for (int i = 0; i < parallel.Branches.Count; i++)
          {
            var branch = parallel.Branches[i];
            if (branch.Identifier == id)
            {
              // 브랜치 identifier 는 스키마 필수(null 불허) 필드라 null 로 비우면
              // 저장 검증이 실패한다. 자동 추가 브랜치와 같은 branch_N placeholder 로
              // 교체해 저장 가능하고 재연결 가능한 상태를 유지한다.
              branch.Identifier = $"branch_{i + 1}";
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
      RefreshDefaultEntrypointMarkers();
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
      graphView.RenameEdgeRoutes(oldId, trimmed);

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

      // 기본 진입 노드(DefaultInit) 참조도 새 식별자로 따라간다.
      if (graphData.DefaultEntrypoint == oldId)
      {
        graphData.DefaultEntrypoint = trimmed;
      }

      nodeViews.Remove(oldId);
      nodeViews[trimmed] = nodeView;

      graphView.RebuildAllEdges();
      nodeView.RefreshTitle();
      SyncRuntimeHighlight();
      RefreshDefaultEntrypointField();
      RefreshDefaultEntrypointMarkers();
      RefreshRuntimeHistoryView();
      RefreshDebugPanel();

      return true;
    }

    /// <summary>
    /// File 메뉴를 열어 보여준다. GenericMenu 의 경로("/" 구분) 문법으로
    /// "Open Recent" 하위 메뉴를 Open 아래에 구성한다. 클릭 시마다 최신 상태로 만든다.
    /// </summary>
    private void ShowFileMenu()
    {
      var menu = new GenericMenu();

      menu.AddItem(new GUIContent("New"), false, () => LoadBlankGraph());
      menu.AddItem(new GUIContent("Open"), false, () => OpenGraphFromJson());
      if (GetSelectedScenarioTextAsset() != null)
        menu.AddItem(new GUIContent("Open Selected Scenario TextAsset"), false, () => OpenSelectedScenarioTextAsset());
      else
        menu.AddDisabledItem(new GUIContent("Open Selected Scenario TextAsset"));

      // Open Recent 하위 메뉴 — 최근 연 파일 목록(Library/ScenarioGraphEditor 캐시), 최신 순.
      var recents = ScenarioGraphEditorRecentStore.LoadExisting();
      if (recents.Count == 0)
      {
        menu.AddDisabledItem(new GUIContent("Open Recent/(최근 항목 없음)"));
      }
      else
      {
        foreach (var path in recents)
        {
          menu.AddItem(new GUIContent($"Open Recent/{GetRecentDisplayLabel(path)}"), false, () => OpenGraphFromPath(path));
        }

        menu.AddSeparator("Open Recent/");
        menu.AddItem(new GUIContent("Open Recent/Clear Recent"), false, () => ScenarioGraphEditorRecentStore.Clear());
      }

      menu.AddSeparator(string.Empty);
      menu.AddItem(new GUIContent("Save"), false, () => SaveGraphToJson());
      menu.AddItem(new GUIContent("Save As..."), false, () => SaveGraphToJsonAs());
      if (!string.IsNullOrEmpty(currentFilePath) && File.Exists(currentFilePath))
        menu.AddItem(new GUIContent(GetRevealFileMenuLabel()), false, RevealCurrentFile);
      else
        menu.AddDisabledItem(new GUIContent(GetRevealFileMenuLabel()));
      menu.AddSeparator(string.Empty);
      menu.AddItem(new GUIContent("Validate"), false, () => ValidateGraphUsingRuntimeValidator());

      menu.ShowAsContext();
    }

    private void ShowEditMenu()
    {
      var menu = new GenericMenu();
      if (nodeViews.Count > 0)
        menu.AddItem(new GUIContent("Relocate Nodes"), false, RelocateNodes);
      else
        menu.AddDisabledItem(new GUIContent("Relocate Nodes"));
      menu.ShowAsContext();
    }

    private void ShowViewMenu()
    {
      var menu = new GenericMenu();
      if (graphView != null)
        menu.AddItem(new GUIContent("Reset Minimap"), false, graphView.ResetMiniMap);
      else
        menu.AddDisabledItem(new GUIContent("Reset Minimap"));
      menu.ShowAsContext();
    }

    private void RelocateNodes()
    {
      if (nodeViews.Count == 0)
        return;

      // 현재 위치와 수동 라우팅을 먼저 Undo 기준점으로 확정한다.
      UpdateUndoState(true);
      CommitPendingSnapshot();

      AutoLayoutNodes(true);

      // 사이드카가 없는 최초 로드와 같은 상태로 맞춘다. 기존 reroute 지점은
      // 새 노드 배치와 맞지 않으므로 제거한 뒤 논리 연결에서 다시 만든다.
      graphView.SetEdgeRoutes(null);
      graphView.RebuildAllEdges();
      graphView.FrameAll();
      RefreshDebugPanel();

      // Relocate 전체를 한 번의 Undo 대상으로 기록한다.
      UpdateUndoState(true);
      CommitPendingSnapshot();
    }

    private static string GetRevealFileMenuLabel()
    {
      return Application.platform switch
      {
        RuntimePlatform.OSXEditor => "Reveal in Finder",
        RuntimePlatform.WindowsEditor => "Show in File Explorer",
        RuntimePlatform.LinuxEditor => "Show in File Manager",
        _ => "Show in File Manager"
      };
    }

    private void RevealCurrentFile()
    {
      if (string.IsNullOrEmpty(currentFilePath) || !File.Exists(currentFilePath))
        return;

      EditorUtility.RevealInFinder(Path.GetFullPath(currentFilePath));
    }

    /// <summary>
    /// Recent 메뉴 표시용 라벨. GenericMenu 의 "/" 는 하위 메뉴 구분자이므로 경로 대신
    /// "파일이름 (상위폴더)" 형태로 표시한다.
    /// </summary>
    private static string GetRecentDisplayLabel(string path)
    {
      var fileName = Path.GetFileName(path);
      var parentName = Path.GetFileName(Path.GetDirectoryName(path));
      return string.IsNullOrEmpty(parentName) ? fileName : $"{fileName} ({parentName})";
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
        ScenarioGraphLoader.ReloadSchemaForEditor();
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
          AutoLayoutNodes(true);
        }

        graphView.SetEdgeRoutes(editorData?.EdgeRoutes);
        graphView.RestoreEdges(nodeViews);
        inspectorView.SetTarget(null);
        currentFilePath = path;
        RefreshGraphIdentifierField();
        RefreshGraphTagsField();
        RefreshDefaultEntrypointField();
        RefreshDefaultEntrypointMarkers();

        ScenarioGraphEditorRecentStore.Record(path);

        SyncRuntimeHighlight();
        RefreshRuntimeHistoryView();
        RefreshDebugPanel();
        ResetUndoHistory();
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
        ScenarioGraphLoader.ReloadSchemaForEditor();
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
        editorData.EdgeRoutes = graphView.CaptureEdgeRoutes();
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
        ScenarioGraphEditorRecentStore.Record(path);
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

    private void AutoLayoutNodes(bool showProgress = false)
    {
      if (nodeViews.Count == 0) return;

      try
      {
        ReportAutoLayoutProgress(showProgress, 0.02f, "자동 배치를 준비하는 중...");
        AutoLayoutNodesInternal(showProgress);
      }
      finally
      {
        if (showProgress)
          EditorUtility.ClearProgressBar();
      }
    }

    private void AutoLayoutNodesInternal(bool showProgress)
    {
      var identifiers = nodeViews.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
      var outgoing = identifiers.ToDictionary(id => id, _ => new List<string>());
      foreach (var node in graphData.Nodes.Values)
      {
        if (node == null || !outgoing.TryGetValue(node.Identifier, out var targets)) continue;
        targets.AddRange(GetOutgoingTargets(node)
          .Where(outgoing.ContainsKey)
          .Distinct());
      }
      ReportAutoLayoutProgress(showProgress, 0.12f, "그래프 연결을 분석하는 중...");

      // DFS로 역방향(순환) 간선을 제외한다. 이 간선들은 화면에서는 그대로
      // 보이지만 레이어 순서를 강제하지 않아 진행 경로가 뒤로 밀리지 않는다.
      var forward = BuildAcyclicEdges(identifiers, outgoing);
      var incoming = identifiers.ToDictionary(id => id, _ => new List<string>());
      foreach (var pair in forward)
        foreach (var target in pair.Value)
          incoming[target].Add(pair.Key);
      ReportAutoLayoutProgress(showProgress, 0.22f, "순환 연결을 정규화하는 중...");

      // Longest-path layering: 모든 순방향 간선의 대상이 출발 노드보다
      // 오른쪽 레이어에 놓이도록 한다.
      var realNodeLayer = AssignLayers(identifiers, forward, incoming);
      ReportAutoLayoutProgress(showProgress, 0.34f, "노드 레이어를 계산하는 중...");

      // Sugiyama 정규화: 긴 간선을 중간 레이어의 dummy vertex 체인으로
      // 분리하여, 교차 최소화가 모든 간선을 인접 레이어 단위로 처리하게 한다.
      var layoutGraph = NormalizeLongEdges(realNodeLayer, forward);
      var layers = layoutGraph.Layer.GroupBy(pair => pair.Value)
        .OrderBy(group => group.Key)
        .ToDictionary(group => group.Key, group => group
          .Select(pair => pair.Key)
          .OrderBy(id => id, StringComparer.Ordinal)
          .ToList());
      ReportAutoLayoutProgress(showProgress, 0.44f, "긴 연결을 레이어별로 분할하는 중...");

      ReduceCrossings(
        layers,
        layoutGraph.Layer,
        layoutGraph.Forward,
        layoutGraph.Incoming,
        (completed, total) =>
        {
          var progress = 0.44f + 0.38f * completed / Math.Max(1f, total);
          ReportAutoLayoutProgress(
            showProgress,
            progress,
            $"연결선 교차를 줄이는 중... ({completed}/{total})");
        });
      ReportAutoLayoutProgress(showProgress, 0.86f, "노드 세로 좌표를 정렬하는 중...");
      var y = AssignVerticalCoordinates(
        layers, layoutGraph.Layer, layoutGraph.Forward, layoutGraph.Incoming);
      ReportAutoLayoutProgress(showProgress, 0.94f, "노드 위치를 적용하는 중...");

      // 모든 실노드의 가장 이른 레이어를 GraphView의 왼쪽 끝(x = 0)으로 둔다.
      var leftmostLayer = realNodeLayer.Count > 0 ? realNodeLayer.Values.Min() : 0;
      foreach (var pair in realNodeLayer)
      {
        if (!nodeViews.TryGetValue(pair.Key, out var view)) continue;
        view.SetPosition(new Rect(
          new Vector2((pair.Value - leftmostLayer) * AutoLayoutColumnSpacing, y[pair.Key]),
          view.DefaultSize));
      }
      ReportAutoLayoutProgress(showProgress, 1f, "그래프 노드 배치를 완료했습니다.");
    }

    private static void ReportAutoLayoutProgress(bool showProgress, float progress, string message)
    {
      if (!showProgress) return;
      EditorUtility.DisplayProgressBar(
        "Scenario Graph Auto Layout",
        message,
        Mathf.Clamp01(progress));
    }

    private static LayeredLayoutGraph NormalizeLongEdges(
      IReadOnlyDictionary<string, int> realNodeLayer,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      var graph = new LayeredLayoutGraph();
      foreach (var pair in realNodeLayer)
      {
        graph.Layer[pair.Key] = pair.Value;
        graph.Forward[pair.Key] = new List<string>();
        graph.Incoming[pair.Key] = new List<string>();
      }

      var dummyIndex = 0;
      foreach (var pair in forward)
      {
        foreach (var target in pair.Value)
        {
          var previous = pair.Key;
          var sourceLayer = realNodeLayer[pair.Key];
          var targetLayer = realNodeLayer[target];
          for (var intermediateLayer = sourceLayer + 1;
               intermediateLayer < targetLayer;
               intermediateLayer++)
          {
            var dummy = $"__scenario_layout_dummy_{dummyIndex++}";
            graph.Layer[dummy] = intermediateLayer;
            graph.Forward[dummy] = new List<string>();
            graph.Incoming[dummy] = new List<string>();
            AddLayoutEdge(graph, previous, dummy);
            previous = dummy;
          }
          AddLayoutEdge(graph, previous, target);
        }
      }
      return graph;
    }

    private static void AddLayoutEdge(LayeredLayoutGraph graph, string source, string target)
    {
      graph.Forward[source].Add(target);
      graph.Incoming[target].Add(source);
    }

    private static Dictionary<string, List<string>> BuildAcyclicEdges(
      IReadOnlyList<string> identifiers, IReadOnlyDictionary<string, List<string>> outgoing)
    {
      var forward = identifiers.ToDictionary(id => id, _ => new List<string>());
      var state = identifiers.ToDictionary(id => id, _ => 0);

      void Visit(string source)
      {
        state[source] = 1;
        foreach (var target in outgoing[source])
        {
          // 현재 DFS 경로로 되돌아가는 간선은 feedback edge로 취급한다.
          if (state[target] == 1) continue;
          forward[source].Add(target);
          if (state[target] == 0) Visit(target);
        }
        state[source] = 2;
      }

      foreach (var id in identifiers)
        if (state[id] == 0) Visit(id);
      return forward;
    }

    private static Dictionary<string, int> AssignLayers(
      IReadOnlyList<string> identifiers,
      IReadOnlyDictionary<string, List<string>> forward,
      IReadOnlyDictionary<string, List<string>> incoming)
    {
      var indegree = identifiers.ToDictionary(id => id, id => incoming[id].Count);
      var ready = new SortedSet<string>(identifiers.Where(id => indegree[id] == 0), StringComparer.Ordinal);
      var layer = identifiers.ToDictionary(id => id, _ => 0);

      while (ready.Count > 0)
      {
        var source = ready.Min;
        ready.Remove(source);
        foreach (var target in forward[source])
        {
          layer[target] = Math.Max(layer[target], layer[source] + 1);
          if (--indegree[target] == 0) ready.Add(target);
        }
      }
      return layer;
    }

    private static void ReduceCrossings(
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<string, List<string>> forward,
      IReadOnlyDictionary<string, List<string>> incoming,
      Action<int, int> onPassCompleted = null)
    {
      var orderedLayers = layers.Keys.OrderBy(value => value).ToList();
      var bestScore = EvaluateLayout(layers, layer, forward);
      var bestOrder = CloneLayerOrder(layers);

      for (var pass = 0; pass < AutoLayoutCrossingReductionPasses; pass++)
      {
        Sweep(orderedLayers, 1, incoming, layer, layers, forward);
        Sweep(orderedLayers, -1, forward, layer, layers, forward);
        TransposeLayers(orderedLayers, layers, layer, forward);

        var score = EvaluateLayout(layers, layer, forward);
        if (IsBetter(score, bestScore))
        {
          bestScore = score;
          bestOrder = CloneLayerOrder(layers);
        }

        onPassCompleted?.Invoke(pass + 1, AutoLayoutCrossingReductionPasses);
      }

      RestoreLayerOrder(layers, bestOrder);
    }

    private static void Sweep(
      IReadOnlyList<int> orderedLayers, int direction,
      IReadOnlyDictionary<string, List<string>> neighbors,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      var start = direction > 0 ? 1 : orderedLayers.Count - 2;
      var end = direction > 0 ? orderedLayers.Count : -1;
      for (var index = start; index != end; index += direction)
      {
        var positions = layers.ToDictionary(pair => pair.Key,
          pair => pair.Value.Select((id, order) => new { id, order })
            .ToDictionary(pair => pair.id, pair => pair.order));
        layers[orderedLayers[index]].Sort((left, right) =>
        {
          var leftMedian = MedianNeighborOrder(left, neighbors, layer, positions);
          var rightMedian = MedianNeighborOrder(right, neighbors, layer, positions);
          var comparison = leftMedian.CompareTo(rightMedian);
          if (comparison != 0) return comparison;

          // 같은 부모에서 갈라지는 대상은 부모 노드 위치가 같아 median도 같다.
          // 이때 실제 출력 포트의 위→아래 순서로 정렬해야 연결선이 교차하지 않는다.
          if (direction > 0)
          {
            var leftPortOrder = MedianIncomingPortOrder(left, neighbors, forward);
            var rightPortOrder = MedianIncomingPortOrder(right, neighbors, forward);
            comparison = leftPortOrder.CompareTo(rightPortOrder);
            if (comparison != 0) return comparison;
          }

          return StringComparer.Ordinal.Compare(left, right);
        });
      }
    }

    private static float MedianNeighborOrder(string id,
      IReadOnlyDictionary<string, List<string>> neighbors,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<int, Dictionary<string, int>> positions)
    {
      var orders = neighbors[id]
        .Where(other => layer[other] != layer[id] && positions[layer[other]].ContainsKey(other))
        .Select(other => (float)positions[layer[other]][other]).OrderBy(value => value).ToList();
      return orders.Count == 0 ? float.MaxValue : orders[orders.Count / 2];
    }

    private static float MedianIncomingPortOrder(
      string target,
      IReadOnlyDictionary<string, List<string>> incoming,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      var orders = incoming[target]
        .Select(source => forward[source].IndexOf(target))
        .Where(index => index >= 0)
        .OrderBy(index => index)
        .ToList();
      if (orders.Count == 0) return float.MaxValue;

      var middle = orders.Count / 2;
      return orders.Count % 2 == 1
        ? orders[middle]
        : (orders[middle - 1] + orders[middle]) * 0.5f;
    }

    private static Dictionary<int, List<string>> CloneLayerOrder(
      IReadOnlyDictionary<int, List<string>> layers)
    {
      return layers.ToDictionary(pair => pair.Key, pair => pair.Value.ToList());
    }

    private static void RestoreLayerOrder(
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<int, List<string>> savedOrder)
    {
      foreach (var pair in savedOrder)
      {
        layers[pair.Key].Clear();
        layers[pair.Key].AddRange(pair.Value);
      }
    }

    private static void TransposeLayers(
      IReadOnlyList<int> orderedLayers,
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      var improved = true;
      while (improved)
      {
        improved = false;
        foreach (var layerIndex in orderedLayers)
        {
          var nodes = layers[layerIndex];
          for (var index = 0; index + 1 < nodes.Count; index++)
          {
            var before = EvaluateLayout(layers, layer, forward);
            (nodes[index], nodes[index + 1]) = (nodes[index + 1], nodes[index]);
            var after = EvaluateLayout(layers, layer, forward);
            if (IsBetter(after, before))
            {
              improved = true;
            }
            else
            {
              (nodes[index], nodes[index + 1]) = (nodes[index + 1], nodes[index]);
            }
          }
        }
      }
    }

    private static LayoutScore EvaluateLayout(
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      return new LayoutScore(
        CountCrossings(layers, layer, forward),
        CalculateVerticalEdgeCost(layers, layer, forward));
    }

    private static bool IsBetter(LayoutScore candidate, LayoutScore current)
    {
      if (candidate.CrossingCount != current.CrossingCount)
        return candidate.CrossingCount < current.CrossingCount;

      return candidate.VerticalEdgeCost < current.VerticalEdgeCost - 0.001f;
    }

    private static float CalculateVerticalEdgeCost(
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      var centeredPositions = layers.ToDictionary(
        pair => pair.Key,
        pair =>
        {
          var center = (pair.Value.Count - 1) * 0.5f;
          return pair.Value.Select((id, index) => new { id, position = index - center })
            .ToDictionary(item => item.id, item => item.position);
        });

      var cost = 0f;
      foreach (var pair in forward)
      {
        var sourceLayer = layer[pair.Key];
        var sourcePosition = centeredPositions[sourceLayer][pair.Key];
        var portCenter = (pair.Value.Count - 1) * 0.5f;
        for (var portIndex = 0; portIndex < pair.Value.Count; portIndex++)
        {
          var target = pair.Value[portIndex];
          var targetPosition = centeredPositions[layer[target]][target];
          // 포트 묶음의 위·아래 위치도 작은 가중치로 반영한다.
          var sourceEndpoint = sourcePosition + (portIndex - portCenter) * 0.1f;
          cost += Mathf.Abs(targetPosition - sourceEndpoint);
        }
      }
      return cost;
    }

    private static int CountCrossings(
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<string, List<string>> forward)
    {
      var positions = layers.ToDictionary(
        pair => pair.Key,
        pair => pair.Value.Select((id, index) => new { id, index })
          .ToDictionary(item => item.id, item => item.index));
      var edgesByLayerPair = new Dictionary<(int source, int target), List<(float source, int target)>>();

      foreach (var pair in forward)
      {
        var sourceLayer = layer[pair.Key];
        var sourceNodeOrder = positions[sourceLayer][pair.Key];
        var portCount = Math.Max(1, pair.Value.Count);
        for (var portIndex = 0; portIndex < pair.Value.Count; portIndex++)
        {
          var target = pair.Value[portIndex];
          var targetLayer = layer[target];
          var key = (sourceLayer, targetLayer);
          if (!edgesByLayerPair.TryGetValue(key, out var endpoints))
          {
            endpoints = new List<(float source, int target)>();
            edgesByLayerPair[key] = endpoints;
          }

          // 같은 노드의 여러 출력도 실제 포트 순서가 비교되도록 작은 분수를 더한다.
          var sourceEndpoint = sourceNodeOrder + (portIndex + 1f) / (portCount + 1f);
          endpoints.Add((sourceEndpoint, positions[targetLayer][target]));
        }
      }

      var crossings = 0;
      foreach (var endpoints in edgesByLayerPair.Values)
      {
        for (var left = 0; left < endpoints.Count; left++)
        {
          for (var right = left + 1; right < endpoints.Count; right++)
          {
            var sourceDelta = endpoints[left].source - endpoints[right].source;
            var targetDelta = endpoints[left].target - endpoints[right].target;
            if (sourceDelta * targetDelta < 0f)
              crossings++;
          }
        }
      }
      return crossings;
    }

    private static Dictionary<string, float> AssignVerticalCoordinates(
      IReadOnlyDictionary<int, List<string>> layers,
      IReadOnlyDictionary<string, int> layer,
      IReadOnlyDictionary<string, List<string>> forward,
      IReadOnlyDictionary<string, List<string>> incoming)
    {
      // 각 레이어를 자체 중심 기준으로 초기화한다. 이후 하향/상향 sweep에서
      // 부모는 자식 묶음의 중심으로, 자식은 부모의 중심으로 당겨진다.
      var y = new Dictionary<string, float>();
      foreach (var pair in layers)
      {
        var center = (pair.Value.Count - 1) * AutoLayoutRowSpacing * 0.5f;
        for (var index = 0; index < pair.Value.Count; index++)
          y[pair.Value[index]] = index * AutoLayoutRowSpacing - center;
      }

      var orderedLayers = layers.Keys.OrderBy(value => value).ToList();
      for (var pass = 0; pass < AutoLayoutCoordinateAssignmentPasses; pass++)
      {
        foreach (var layerIndex in orderedLayers)
          AlignLayerAroundNeighbors(layers[layerIndex], incoming, y);
        for (var index = orderedLayers.Count - 1; index >= 0; index--)
          AlignLayerAroundNeighbors(layers[orderedLayers[index]], forward, y);
      }

      // GraphView의 일반적인 양수 좌표 영역에서 시작하도록 전체 결과만 이동한다.
      var minimum = y.Count > 0 ? y.Values.Min() : 0f;
      foreach (var id in layer.Keys)
        y[id] -= minimum;
      return y;
    }

    private static void AlignLayerAroundNeighbors(
      IReadOnlyList<string> nodes,
      IReadOnlyDictionary<string, List<string>> neighbors,
      IDictionary<string, float> y)
    {
      if (nodes.Count == 0) return;

      var desired = nodes
        .Select(id => MedianCoordinate(neighbors[id], y, y[id]))
        .ToList();
      var placed = new float[nodes.Count];
      placed[0] = desired[0];
      for (var index = 1; index < nodes.Count; index++)
      {
        placed[index] = Mathf.Max(
          desired[index],
          placed[index - 1] + AutoLayoutRowSpacing);
      }

      // 충돌 해소가 한쪽 방향으로만 밀어내지 않도록 레이어 전체를 원래
      // 목표 중심으로 되돌린다. 분기 부모가 자식 묶음의 중앙에 놓이게 된다.
      var desiredCenter = desired.Average();
      var placedCenter = placed.Average();
      var offset = desiredCenter - placedCenter;
      for (var index = 0; index < nodes.Count; index++)
        y[nodes[index]] = placed[index] + offset;
    }

    private static float MedianCoordinate(
      IEnumerable<string> identifiers,
      IDictionary<string, float> y,
      float fallback)
    {
      var coordinates = identifiers
        .Where(y.ContainsKey)
        .Select(id => y[id])
        .OrderBy(value => value)
        .ToList();
      if (coordinates.Count == 0) return fallback;

      var middle = coordinates.Count / 2;
      return coordinates.Count % 2 == 1
        ? coordinates[middle]
        : (coordinates[middle - 1] + coordinates[middle]) * 0.5f;
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

      if (node is ScenarioQuizNode quiz)
      {
        if (!string.IsNullOrEmpty(quiz.OnCorrectNextIdentifier)) yield return quiz.OnCorrectNextIdentifier;
        if (!string.IsNullOrEmpty(quiz.OnIncorrectNextIdentifier)) yield return quiz.OnIncorrectNextIdentifier;
      }
    }

  }
}
