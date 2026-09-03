using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.Editor
{
  /// <summary>시나리오가 include하는 Resources/Quest 정의 파일을 편집하는 뷰.</summary>
  internal sealed class ScenarioQuestEditorView : VisualElement
  {
    private sealed class QuestSource
    {
      public string AssetPath;
      public QuestDefinitionRegistryPayload Payload;
    }

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private readonly Func<ScenarioGraph> getGraph;
    private readonly Action<string> onReferenceNodeOpenRequested;
    private readonly ScrollView scroll;
    private readonly List<QuestSource> sources = new List<QuestSource>();
    private readonly List<string> loadErrors = new List<string>();
    private readonly Dictionary<string, Foldout> definitionFoldouts = new Dictionary<string, Foldout>(StringComparer.Ordinal);
    private string loadedIncludesKey;
    private bool isViewBuilt;

    public ScenarioQuestEditorView(Func<ScenarioGraph> getGraph, Action<string> onReferenceNodeOpenRequested)
    {
      this.getGraph = getGraph;
      this.onReferenceNodeOpenRequested = onReferenceNodeOpenRequested;
      style.flexGrow = 1f;
      Add(new Label("Quests") { style = { unityFontStyleAndWeight = UnityEngine.FontStyle.Bold, fontSize = 14 } });
      Add(new HelpBox(
        "이 시나리오의 questDefinitionIncludes에 등록된 Resources/Quest 파일을 편집합니다. 변경 사항은 원본 .quest.json 파일에 바로 저장됩니다.",
        HelpBoxMessageType.Info));
      scroll = new ScrollView();
      scroll.style.flexGrow = 1f;
      Add(scroll);
      Refresh();
    }

    public void Refresh()
    {
      isViewBuilt = true;
      scroll.Clear();
      definitionFoldouts.Clear();
      var includes = getGraph?.Invoke()?.QuestDefinitionIncludes;
      if (includes == null || includes.Count == 0)
      {
        scroll.Add(new HelpBox("이 시나리오에는 questDefinitionIncludes가 등록되어 있지 않습니다.", HelpBoxMessageType.Warning));
        return;
      }

      Preload();
      for (var index = 0; index < loadErrors.Count; index++)
        scroll.Add(new HelpBox(loadErrors[index], HelpBoxMessageType.Warning));
      if (sources.Count == 0)
        return;
      for (var index = 0; index < sources.Count; index++)
        scroll.Add(BuildSource(sources[index]));
    }

    /// <summary>시나리오 로드 시 include 문서를 미리 읽어 Quests 탭 전환 비용을 줄인다.</summary>
    public void Preload(Action<int, int, string> reportProgress = null)
    {
      var includes = getGraph?.Invoke()?.QuestDefinitionIncludes;
      var includesKey = includes == null ? string.Empty : string.Join("\n", includes);
      if (string.Equals(loadedIncludesKey, includesKey, StringComparison.Ordinal))
        return;

      sources.Clear();
      loadErrors.Clear();
      loadedIncludesKey = includesKey;
      if (includes == null || includes.Count == 0)
        return;
      reportProgress?.Invoke(0, includes.Count + 1, "퀘스트 정의 파일을 찾는 중...");
      var paths = AssetDatabase.FindAssets("t:TextAsset")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Where(path => path.IndexOf("/Resources/Quest/", StringComparison.OrdinalIgnoreCase) >= 0)
        .ToArray();
      for (var index = 0; index < includes.Count; index++)
      {
        var include = includes[index];
        reportProgress?.Invoke(index + 1, includes.Count + 1, $"퀘스트 정의를 미리 읽는 중... ({index + 1}/{includes.Count})");
        var path = FindAssetPath(paths, include);
        if (path == null)
        {
          loadErrors.Add($"Quest include '{include}' 파일을 찾을 수 없습니다.");
          continue;
        }

        if (!TryLoad(path, out var payload, out var error))
        {
          loadErrors.Add($"'{include}'을 읽을 수 없습니다: {error}");
          continue;
        }

        var source = new QuestSource { AssetPath = path, Payload = payload };
        sources.Add(source);
      }
      reportProgress?.Invoke(includes.Count + 1, includes.Count + 1, "퀘스트 정의 파일 준비를 완료했습니다.");
    }

    public void SelectDefinition(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;
      if (!isViewBuilt || !definitionFoldouts.TryGetValue(identifier.Trim(), out var foldout))
      {
        Refresh();
        if (!definitionFoldouts.TryGetValue(identifier.Trim(), out foldout))
          return;
      }
      foldout.value = true;
      foldout.schedule.Execute(() => scroll.ScrollTo(foldout));
    }

    private VisualElement BuildSource(QuestSource source)
    {
      var sourceFoldout = new Foldout
      {
        text = $"{Path.GetFileName(source.AssetPath)} ({source.Payload.Definitions?.Count ?? 0})",
        value = true
      };
      sourceFoldout.style.marginTop = 8;
      sourceFoldout.Add(new Label(AssetPathToResourcePath(source.AssetPath)) { style = { opacity = 0.65f } });
      var definitions = source.Payload.Definitions ??= new List<QuestDefinition>();
      for (var index = 0; index < definitions.Count; index++)
      {
        if (definitions[index] != null)
          sourceFoldout.Add(BuildDefinition(source, definitions[index], index));
      }
      sourceFoldout.Add(new Button(() =>
      {
        definitions.Add(new QuestDefinition { Identifier = "quest" });
        Save(source);
        Refresh();
      })
      { text = "Add Quest" });
      return sourceFoldout;
    }

    private VisualElement BuildDefinition(QuestSource source, QuestDefinition definition, int index)
    {
      var foldout = new Foldout
      {
        text = DisplayName(definition, index),
        value = false
      };
      foldout.style.marginLeft = 10;
      var qualifiedIdentifier = ComposeIdentifier(source.Payload.Namespace, definition.Identifier);
      if (!string.IsNullOrWhiteSpace(qualifiedIdentifier))
        definitionFoldouts[qualifiedIdentifier] = foldout;

      var references = GetReferences(qualifiedIdentifier);
      foldout.Add(new Label(references.Count == 0 ? "Referenced: none" : $"Referenced by {references.Count}")
      {
        style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 4 }
      });
      if (references.Count > 0)
        foldout.Add(BuildReferenceList(references));

      foldout.Add(Text("Identifier", definition.Identifier, value =>
      {
        definition.Identifier = Normalize(value);
        Save(source);
        Refresh();
      }));
      foldout.Add(Text("Title", definition.Title, value => { definition.Title = value; Save(source); }));
      foldout.Add(MultilineText("Description", definition.Description, value => { definition.Description = value; Save(source); }));
      foldout.Add(MultilineText("Quest Content", definition.QuestContent, value => { definition.QuestContent = value; Save(source); }));
      foldout.Add(Text("Waypoint Identifier", definition.WaypointIdentifier, value => { definition.WaypointIdentifier = Normalize(value); Save(source); }));
      foldout.Add(Toggle("Trackable", definition.IsTrackable, value => { definition.IsTrackable = value; Save(source); }));
      foldout.Add(Toggle("Auto Complete", definition.IsAutoComplete, value => { definition.IsAutoComplete = value; Save(source); }));
      foldout.Add(Toggle("Tracked By Default", definition.IsTrackedByDefault, value => { definition.IsTrackedByDefault = value; Save(source); }));
      foldout.Add(Toggle("Persist Progress", definition.PersistProgressOnSessionEnd, value => { definition.PersistProgressOnSessionEnd = value; Save(source); }));
      foldout.Add(Enum("Scope", definition.Scope, value => { definition.Scope = value; Save(source); }));
      foldout.Add(Toggle("Ordinal", definition.IsOrdinal, value => { definition.IsOrdinal = value; Save(source); }));
      foldout.Add(BuildCriteriaList(source, definition.Tasks ??= new List<QuestCompletionCriteria>(), "Tasks"));
      foldout.Add(BuildCriteriaList(source, definition.CompletionCriteria ??= new List<QuestCompletionCriteria>(), "Completion Criteria"));
      foldout.Add(BuildPresentationBindings(source, definition.PresentationBindings ??= new List<QuestPresentationBinding>()));
      foldout.Add(new Button(() =>
      {
        source.Payload.Definitions.Remove(definition);
        Save(source);
        Refresh();
      })
      { text = "Remove Quest" });
      return foldout;
    }

    private VisualElement BuildCriteriaList(QuestSource source, List<QuestCompletionCriteria> criteria, string label)
    {
      var listFoldout = new Foldout { text = $"{label} ({criteria.Count})", value = false };
      for (var index = 0; index < criteria.Count; index++)
      {
        if (criteria[index] != null)
          listFoldout.Add(BuildCriterion(source, criteria, criteria[index], index));
      }
      listFoldout.Add(new Button(() =>
      {
        criteria.Add(new QuestCompletionCriteria());
        Save(source);
        Refresh();
      })
      { text = "Add Criterion" });
      return listFoldout;
    }

    private VisualElement BuildCriterion(QuestSource source, List<QuestCompletionCriteria> owner, QuestCompletionCriteria criterion, int index)
    {
      var foldout = new Foldout { text = $"{index + 1}. {criterion.Type}", value = false };
      foldout.style.marginLeft = 8;
      foldout.Add(Text("Identifier", criterion.Identifier, value => { criterion.Identifier = Normalize(value); Save(source); }));
      foldout.Add(Enum("Type", criterion.Type, value => { criterion.Type = value; Save(source); Refresh(); }));
      foldout.Add(Text("Item ID", criterion.ItemId, value => { criterion.ItemId = Normalize(value); Save(source); }));
      foldout.Add(Text("Signal ID", criterion.SignalId, value => { criterion.SignalId = Normalize(value); Save(source); }));
      // Owner 로 지정하면 이 퀘스트를 보유한 참여자가 직접 올린 신호만 인정한다.
      // 역할별로 한 사람이 수행하는 목표에 사용하고, 참여자 전원이 함께 달성하는 공동 목표에는 Any 를 유지한다.
      foldout.Add(Enum("Signal Scope", criterion.SignalScope, value => { criterion.SignalScope = value; Save(source); }));
      foldout.Add(Text("Waypoint Identifier", criterion.WaypointIdentifier, value => { criterion.WaypointIdentifier = Normalize(value); Save(source); }));
      var distance = new FloatField("Reach Distance") { value = criterion.ReachDistance };
      distance.RegisterValueChangedCallback(evt => { criterion.ReachDistance = evt.newValue; Save(source); });
      foldout.Add(distance);
      foldout.Add(MultilineText("Display Text", criterion.DisplayTextContent, value => { criterion.DisplayTextContent = value; Save(source); }));
      foldout.Add(Text("On Complete Signal", criterion.OnCompleteSignalIdentifier, value => { criterion.OnCompleteSignalIdentifier = Normalize(value); Save(source); }));
      var count = new IntegerField("Count") { value = criterion.Count };
      count.RegisterValueChangedCallback(evt => { criterion.Count = evt.newValue; Save(source); });
      foldout.Add(count);
      foldout.Add(BuildCriteriaList(source, criterion.Conditions ??= new List<QuestCompletionCriteria>(), "Conditions"));
      foldout.Add(new Button(() => { owner.Remove(criterion); Save(source); Refresh(); }) { text = "Remove Criterion" });
      return foldout;
    }

    private VisualElement BuildPresentationBindings(QuestSource source, List<QuestPresentationBinding> bindings)
    {
      var listFoldout = new Foldout { text = $"Presentation Bindings ({bindings.Count})", value = false };
      for (int index = 0; index < bindings.Count; index++)
      {
        var binding = bindings[index];
        if (binding == null)
          continue;

        var foldout = new Foldout { text = $"{index + 1}. {binding.TargetType}", value = false };
        foldout.style.marginLeft = 8;
        foldout.Add(Enum("Activation", binding.Activation, value => { binding.Activation = value; Save(source); Refresh(); }));
        foldout.Add(Text("Completion Criteria", binding.CompletionCriteriaIdentifier, value => { binding.CompletionCriteriaIdentifier = Normalize(value); Save(source); }));
        foldout.Add(Enum("Target Type", binding.TargetType, value => { binding.TargetType = value; Save(source); Refresh(); }));
        foldout.Add(Text("Entity Identifier", binding.EntityIdentifier, value => { binding.EntityIdentifier = Normalize(value); Save(source); }));
        foldout.Add(Text("Interaction Identifier", binding.InteractionIdentifier, value => { binding.InteractionIdentifier = Normalize(value); Save(source); }));
        foldout.Add(Text("Icon Identifier", binding.IconIdentifier, value => { binding.IconIdentifier = Normalize(value); Save(source); }));
        foldout.Add(Enum("Icon Mode", binding.IconMode, value => { binding.IconMode = value; Save(source); }));
        var priority = new IntegerField("Priority") { value = binding.Priority };
        priority.RegisterValueChangedCallback(evt => { binding.Priority = evt.newValue; Save(source); });
        foldout.Add(priority);
        foldout.Add(Toggle("Show When Untracked", binding.ShowWhenUntracked, value => { binding.ShowWhenUntracked = value; Save(source); }));
        foldout.Add(new Button(() => { bindings.Remove(binding); Save(source); Refresh(); }) { text = "Remove Binding" });
        listFoldout.Add(foldout);
      }

      listFoldout.Add(new Button(() =>
      {
        bindings.Add(new QuestPresentationBinding());
        Save(source);
        Refresh();
      })
      { text = "Add Binding" });
      return listFoldout;
    }

    private static TextField Text(string label, string value, Action<string> onChanged, bool multiline = false)
    {
      var field = new TextField(label) { value = value ?? string.Empty, multiline = multiline };
      if (multiline)
        field.style.minHeight = 45;
      field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
      return field;
    }

    private static TextField MultilineText(string label, string value, Action<string> onChanged)
    {
      var field = Text(label, value, onChanged, true);
      field.style.minHeight = 72;
      field.style.whiteSpace = WhiteSpace.Normal;
      return field;
    }

    private static Toggle Toggle(string label, bool value, Action<bool> onChanged)
    {
      var field = new Toggle(label) { value = value };
      field.RegisterValueChangedCallback(evt => onChanged(evt.newValue));
      return field;
    }

    private static EnumField Enum<T>(string label, T value, Action<T> onChanged) where T : Enum
    {
      var field = new EnumField(label, value);
      field.RegisterValueChangedCallback(evt => onChanged((T)evt.newValue));
      return field;
    }

    private static void Save(QuestSource source)
    {
      var json = JsonSerializer.Serialize(source.Payload, JsonOptions);
      File.WriteAllText(source.AssetPath, json + Environment.NewLine);
      AssetDatabase.ImportAsset(source.AssetPath);
      QuestDefinitionRegistry.InvalidateResourceCache();
    }

    private static bool TryLoad(string assetPath, out QuestDefinitionRegistryPayload payload, out string error)
    {
      payload = null;
      error = null;
      try
      {
        var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.TextAsset>(assetPath);
        payload = JsonSerializer.Deserialize<QuestDefinitionRegistryPayload>(asset?.text, JsonOptions);
        if (payload == null)
          throw new InvalidDataException("빈 퀘스트 정의 문서입니다.");
        payload.Definitions ??= new List<QuestDefinition>();
        return true;
      }
      catch (Exception exception)
      {
        error = exception.Message;
        return false;
      }
    }

    private static string FindAssetPath(IEnumerable<string> paths, string include)
    {
      if (string.IsNullOrWhiteSpace(include))
        return null;
      var normalized = include.Trim().Replace("\\", "/");
      if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
        normalized = normalized.Substring("Resources/".Length);
      if (!normalized.StartsWith("Quest/", StringComparison.OrdinalIgnoreCase))
        normalized = "Quest/" + normalized;
      if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        normalized = normalized.Substring(0, normalized.Length - 5);
      var expected = "/Resources/" + normalized + ".json";
      return paths.FirstOrDefault(path => path.EndsWith(expected, StringComparison.OrdinalIgnoreCase));
    }

    private static string AssetPathToResourcePath(string assetPath)
    {
      var marker = "/Resources/";
      var index = assetPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
      return index >= 0 ? assetPath.Substring(index + marker.Length) : assetPath;
    }

    private static string DisplayName(QuestDefinition definition, int index) =>
      string.IsNullOrWhiteSpace(definition.Identifier) ? $"Quest {index + 1}" : definition.Identifier;

    private static string ComposeIdentifier(string questNamespace, string identifier) =>
      string.IsNullOrWhiteSpace(questNamespace) ? identifier?.Trim() : $"{questNamespace.Trim()}::{identifier?.Trim()}";

    private static string Normalize(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private List<string> GetReferences(string definitionIdentifier)
    {
      var graph = getGraph?.Invoke();
      if (graph?.Nodes == null || string.IsNullOrWhiteSpace(definitionIdentifier))
        return new List<string>();

      return graph.Nodes.Values
        .OfType<ScenarioQuestControlNode>()
        .Where(node => string.Equals(node.QuestDefinitionIdentifier?.Trim(), definitionIdentifier, StringComparison.Ordinal))
        .Select(node => node.Identifier)
        .OrderBy(identifier => identifier, StringComparer.Ordinal)
        .ToList();
    }

    private VisualElement BuildReferenceList(IReadOnlyList<string> references)
    {
      var container = new VisualElement { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap, marginBottom = 4 } };
      for (var index = 0; index < references.Count; index++)
      {
        var nodeIdentifier = references[index];
        var reference = new Label(nodeIdentifier)
        {
          tooltip = "Double-click to open this node in the Graph tab",
          style =
          {
            marginRight = 6,
            marginBottom = 3,
            color = new UnityEngine.Color(0.42f, 0.72f, 1f, 1f),
            unityFontStyleAndWeight = UnityEngine.FontStyle.Bold
          }
        };
        reference.RegisterCallback<MouseDownEvent>(evt =>
        {
          if (evt.button != 0 || evt.clickCount != 2)
            return;
          onReferenceNodeOpenRequested?.Invoke(nodeIdentifier);
          evt.StopPropagation();
        });
        container.Add(reference);
      }
      return container;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
      var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };
      options.Converters.Add(new JsonStringEnumConverter());
      return options;
    }
  }
}
