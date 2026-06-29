using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// Manages quest lifecycle and tracked selections shared by UI controllers.
  /// </summary>
  public class QuestManager : MonoBehaviour
  {
    private const string QuestCompletedRuntimeStatePrefix = "quest.completed.";
    [Flags]
    public enum FeatureFlags
    {
      None = 0,
      HighlightAssignedWaypoint = 1 << 0
    }

    [SerializeField] private int _maxTracked = DefaultsQuestControl.MaxTrackedQuests;
    [SerializeField] private FeatureFlags _featureFlags = FeatureFlags.HighlightAssignedWaypoint;
    [SerializeField] private bool _autoEvaluateCompletion = true;

    private readonly Dictionary<string, QuestData> _quests = new();
    private readonly List<string> _trackedQuestOrder = new();
    private QuestDefinitionRegistry _definitionRegistry;
    private PlayerController _ownerPlayer;

    public event Action<IReadOnlyList<QuestData>> OnQuestListChanged;
    public event Action<IReadOnlyList<QuestData>> OnTrackedQuestsChanged;

    public IReadOnlyList<QuestData> Quests => Snapshot(_quests.Values);
    public IReadOnlyList<QuestData> TrackedQuests
    {
      get
      {
        var result = new List<QuestData>(_trackedQuestOrder.Count);
        for (int i = 0; i < _trackedQuestOrder.Count; i++)
        {
          if (_quests.TryGetValue(_trackedQuestOrder[i], out var q) && q != null)
            result.Add(q.Clone());
        }
        return result;
      }
    }

    public bool HasQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId))
        return false;

      return _quests.ContainsKey(questId);
    }

    public bool TryGetQuest(string questId, out QuestData quest)
    {
      quest = null;
      if (string.IsNullOrWhiteSpace(questId))
        return false;

      if (_quests.TryGetValue(questId, out var existing))
      {
        quest = existing.Clone();
        return true;
      }

      return false;
    }

    private void Awake()
    {
      Registry.Registry.Register(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>(), this);
      _definitionRegistry = Registry.Registry.Get<QuestDefinitionRegistry>(RegistryType.Service, Registry.Registry.TypeKey<QuestDefinitionRegistry>());
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());
    }

    public void SetQuests(IEnumerable<QuestData> quests, bool clearExisting = true)
    {
      if (quests == null) return;

      if (clearExisting)
      {
        _quests.Clear();
        _trackedQuestOrder.Clear();
      }

      foreach (var quest in quests)
      {
        AddOrUpdateQuest(quest, notify: false);
      }

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void AddOrUpdateQuest(QuestData quest, bool notify = true)
    {
      if (quest == null || string.IsNullOrWhiteSpace(quest.Id))
        return;

      var cloned = ResolveQuestData(quest);
      var isNewQuest = !_quests.ContainsKey(cloned.Id);
      _quests[cloned.Id] = cloned;
      UpdateQuestCompletionRuntimeState(cloned.Id, false);

      if (cloned.IsTracked)
      {
        EnsureTracked(cloned.Id, suppressNotify: true);
      }

      if (isNewQuest)
      {
        TryHighlightWaypointForQuest(cloned);
      }

      if (notify)
      {
        EvaluateQuestProgress(cloned.Id);
        ClampTrackedToLimit();
        NotifyListChanged();
        NotifyTrackedChanged();
      }
    }

    public void EvaluateAllQuestProgress()
    {
      var questIds = new List<string>(_quests.Keys);
      for (int i = 0; i < questIds.Count; i++)
      {
        EvaluateQuestProgress(questIds[i]);
      }

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public bool EvaluateQuestProgress(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId) || !_quests.TryGetValue(questId, out var quest) || quest == null)
        return false;

      var result = QuestCriteriaEvaluator.Evaluate(quest.CompletionCriteria, GetOwnerPlayerController());
      if (quest.Scope == QuestScopeType.Global)
      {
        result = QuestCriteriaEvaluator.EvaluateGlobal(quest.CompletionCriteria);
      }
      quest.Progress = new QuestProgressValue(result.Current, result.Target);
      quest.Completed = result.IsSatisfied;
      UpdateQuestCompletionRuntimeState(questId, quest.Completed);

      if (quest.Completed && quest.IsAutoComplete)
      {
        quest.IsTracked = false;
        _trackedQuestOrder.Remove(questId);
      }

      return quest.Completed;
    }

    private void TryHighlightWaypointForQuest(QuestData quest)
    {
      if (!_featureFlags.HasFlag(FeatureFlags.HighlightAssignedWaypoint) || quest == null)
        return;

      if (string.IsNullOrWhiteSpace(quest.WaypointIdentifier))
        return;

      if (WaypointAnchor.TryGet(quest.WaypointIdentifier, out var anchor))
      {
        anchor.Highlight();
      }
    }

    public void RemoveQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId)) return;

      _quests.Remove(questId);
      _trackedQuestOrder.Remove(questId);
      UpdateQuestCompletionRuntimeState(questId, false);

      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void ClearAll()
    {
      var existingIds = new List<string>(_quests.Keys);
      _quests.Clear();
      _trackedQuestOrder.Clear();
      for (int i = 0; i < existingIds.Count; i++)
      {
        UpdateQuestCompletionRuntimeState(existingIds[i], false);
      }
      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void SetTracked(string questId, bool tracked)
    {
      if (string.IsNullOrWhiteSpace(questId) || !_quests.TryGetValue(questId, out var quest))
        return;

      if (!quest.IsTrackable)
        tracked = false;

      if (tracked)
      {
        EnsureTracked(questId, suppressNotify: false);
      }
      else
      {
        quest.IsTracked = false;
        _trackedQuestOrder.Remove(questId);
      }

      ClampTrackedToLimit();
      NotifyTrackedChanged();
      NotifyListChanged();
    }

    private void EnsureTracked(string questId, bool suppressNotify)
    {
      if (_trackedQuestOrder.Contains(questId))
        return;

      if (_trackedQuestOrder.Count >= _maxTracked)
      {
        var removedId = _trackedQuestOrder[0];
        _trackedQuestOrder.RemoveAt(0);
        if (_quests.TryGetValue(removedId, out var removedQuest))
          removedQuest.IsTracked = false;
      }

      _trackedQuestOrder.Add(questId);
      if (_quests.TryGetValue(questId, out var quest))
        quest.IsTracked = true;

      if (!suppressNotify)
        ClampTrackedToLimit();
    }

    private void ClampTrackedToLimit()
    {
      while (_trackedQuestOrder.Count > _maxTracked)
      {
        var removedId = _trackedQuestOrder[0];
        _trackedQuestOrder.RemoveAt(0);
        if (_quests.TryGetValue(removedId, out var removedQuest))
          removedQuest.IsTracked = false;
      }
    }

    private void NotifyListChanged() => OnQuestListChanged?.Invoke(Quests);
    private void NotifyTrackedChanged() => OnTrackedQuestsChanged?.Invoke(TrackedQuests);

    private static string BuildQuestCompletedRuntimeStateKey(string questId)
    {
      return string.IsNullOrWhiteSpace(questId)
        ? null
        : $"{QuestCompletedRuntimeStatePrefix}{questId.Trim()}";
    }

    private static void UpdateQuestCompletionRuntimeState(string questId, bool completed)
    {
      var key = BuildQuestCompletedRuntimeStateKey(questId);
      if (string.IsNullOrWhiteSpace(key))
        return;

      if (completed)
      {
        Registry.Registry.Register(RegistryType.RuntimeState, key, true);
      }
      else
      {
        Registry.Registry.Unregister(RegistryType.RuntimeState, key);
      }
    }

    private QuestData ResolveQuestData(QuestData quest)
    {
      var resolved = quest.Clone();

      if (!string.IsNullOrWhiteSpace(resolved.DefinitionIdentifier) && TryResolveFromDefinition(resolved.DefinitionIdentifier, out var fromDefinition))
      {
        fromDefinition.Id = string.IsNullOrWhiteSpace(resolved.Id) ? fromDefinition.Id : resolved.Id;
        if (resolved.IsTracked)
          fromDefinition.IsTracked = true;

        if (!string.IsNullOrWhiteSpace(resolved.WaypointIdentifier))
          fromDefinition.WaypointIdentifier = resolved.WaypointIdentifier;

        return fromDefinition;
      }

      if (string.IsNullOrWhiteSpace(resolved.DefinitionIdentifier)
          && !string.IsNullOrWhiteSpace(resolved.Id)
          && TryResolveFromDefinition(resolved.Id, out var fallbackById))
      {
        fallbackById.Id = resolved.Id;
        if (resolved.IsTracked)
          fallbackById.IsTracked = true;

        if (!string.IsNullOrWhiteSpace(resolved.WaypointIdentifier))
          fallbackById.WaypointIdentifier = resolved.WaypointIdentifier;

        return fallbackById;
      }

      if (resolved.Progress == null)
        resolved.Progress = new QuestProgressValue(0, 1);

      if (resolved.CompletionCriteria == null)
        resolved.CompletionCriteria = new List<QuestCompletionCriteria>();

      return resolved;
    }

    private bool TryResolveFromDefinition(string definitionIdentifier, out QuestData quest)
    {
      quest = null;
      if (string.IsNullOrWhiteSpace(definitionIdentifier))
        return false;

      if (_definitionRegistry == null)
        _definitionRegistry = Registry.Registry.Get<QuestDefinitionRegistry>(RegistryType.Service, Registry.Registry.TypeKey<QuestDefinitionRegistry>());

      QuestDefinition definition = null;
      if (_definitionRegistry != null)
        _definitionRegistry.TryGet(definitionIdentifier, out definition);

      if (definition == null)
      {
        if (!QuestDefinitionRegistry.TryGetGlobal(definitionIdentifier, out definition) || definition == null)
          return false;
      }

      quest = new QuestData
      {
        Id = definition.Identifier,
        DefinitionIdentifier = definition.Identifier,
        Title = definition.Title ?? string.Empty,
        Description = definition.Description ?? string.Empty,
        QuestContent = definition.QuestContent ?? string.Empty,
        WaypointIdentifier = definition.WaypointIdentifier ?? string.Empty,
        IsTrackable = definition.IsTrackable,
        IsAutoComplete = definition.IsAutoComplete,
        IsTracked = definition.IsTrackable && definition.IsTrackedByDefault,
        Scope = definition.Scope,
        CompletionCriteria = CloneCriteria(definition.CompletionCriteria),
        Progress = new QuestProgressValue(0, 1),
        Completed = false
      };

      return true;
    }

    private PlayerController GetOwnerPlayerController()
    {
      if (_ownerPlayer != null)
        return _ownerPlayer;

      _ownerPlayer = Registry.Registry.Get<PlayerController>(RegistryType.Service, Registry.Registry.TypeKey<PlayerController>());
      if (_ownerPlayer != null)
        return _ownerPlayer;

      var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        if (players[i] != null && players[i].IsOwner)
        {
          _ownerPlayer = players[i];
          break;
        }
      }

      return _ownerPlayer;
    }

    private static List<QuestCompletionCriteria> CloneCriteria(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      var list = new List<QuestCompletionCriteria>();
      if (criteria == null)
        return list;

      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        if (each != null)
          list.Add(each.Clone());
      }

      return list;
    }

    private static List<QuestData> Snapshot(IEnumerable<QuestData> source)
    {
      var list = new List<QuestData>();
      foreach (var quest in source)
      {
        if (quest != null)
          list.Add(quest.Clone());
      }
      return list;
    }

    private void Update()
    {
      if (!_autoEvaluateCompletion || _quests.Count == 0)
        return;

      bool changed = false;
      foreach (var quest in _quests)
      {
        var before = quest.Value?.Progress?.Current ?? 0;
        var beforeTarget = quest.Value?.Progress?.Target ?? 1;
        var beforeCompleted = quest.Value?.Completed ?? false;

        EvaluateQuestProgress(quest.Key);

        var currentQuest = quest.Value;
        if (currentQuest == null)
          continue;

        var after = currentQuest.Progress?.Current ?? 0;
        var afterTarget = currentQuest.Progress?.Target ?? 1;
        var afterCompleted = currentQuest.Completed;
        if (before != after || beforeTarget != afterTarget || beforeCompleted != afterCompleted)
        {
          changed = true;
        }
      }

      if (changed)
      {
        ClampTrackedToLimit();
        NotifyListChanged();
        NotifyTrackedChanged();
      }
    }
  }

  public static class QuestCriteriaEvaluator
  {
    public static QuestCriteriaEvaluationResult Evaluate(IReadOnlyList<QuestCompletionCriteria> criteria, PlayerController playerController)
    {
      if (criteria == null || criteria.Count == 0)
      {
        return new QuestCriteriaEvaluationResult(false, 0, 1);
      }

      int satisfied = 0;
      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        if (each == null)
          continue;

        var result = Evaluate(each, playerController);
        if (result.IsSatisfied)
          satisfied++;
      }

      int target = Math.Max(1, criteria.Count);
      return new QuestCriteriaEvaluationResult(satisfied >= target, Math.Min(satisfied, target), target);
    }

    public static QuestCriteriaEvaluationResult EvaluateGlobal(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      if (criteria == null || criteria.Count == 0)
      {
        return new QuestCriteriaEvaluationResult(false, 0, 1);
      }

      int satisfied = 0;
      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        if (each == null)
          continue;

        if (EvaluateGlobal(each).IsSatisfied)
          satisfied++;
      }

      int target = Math.Max(1, criteria.Count);
      return new QuestCriteriaEvaluationResult(satisfied >= target, Math.Min(satisfied, target), target);
    }

    private static QuestCriteriaEvaluationResult Evaluate(QuestCompletionCriteria criteria, PlayerController playerController)
    {
      if (criteria == null)
        return new QuestCriteriaEvaluationResult(true, 1, 1);

      int count = Math.Max(1, criteria.Count);

      switch (criteria.Type)
      {
        case QuestCompletionCriteriaType.InventoryContains:
        {
          int inventoryCount = playerController != null
              ? playerController.CountItemInInventory(criteria.ItemId)
              : 0;
          bool satisfied = inventoryCount >= count;
          return new QuestCriteriaEvaluationResult(satisfied, Math.Min(inventoryCount, count), count);
        }

        case QuestCompletionCriteriaType.InteractionSignalReceived:
        {
          var normalized = ScenarioInteractionSignals.Normalize(criteria.SignalId);
          bool raised = !string.IsNullOrWhiteSpace(normalized)
              && Registry.Registry.Contains(RegistryType.RuntimeState, normalized);
          int current = raised ? count : 0;
          return new QuestCriteriaEvaluationResult(raised, current, count);
        }

        case QuestCompletionCriteriaType.AllOf:
          return EvaluateComposite(criteria.Conditions, true, playerController);

        case QuestCompletionCriteriaType.AnyOf:
          return EvaluateComposite(criteria.Conditions, false, playerController);

        default:
          return new QuestCriteriaEvaluationResult(false, 0, 1);
      }
    }

    private static QuestCriteriaEvaluationResult EvaluateGlobal(QuestCompletionCriteria criteria)
    {
      if (criteria == null)
        return new QuestCriteriaEvaluationResult(true, 1, 1);

      int count = Math.Max(1, criteria.Count);

      switch (criteria.Type)
      {
        case QuestCompletionCriteriaType.InventoryContains:
        {
          int totalCount = CountItemAcrossAllPlayers(criteria.ItemId);
          bool satisfied = totalCount >= count;
          return new QuestCriteriaEvaluationResult(satisfied, Math.Min(totalCount, count), count);
        }
        case QuestCompletionCriteriaType.InteractionSignalReceived:
        {
          var normalized = ScenarioInteractionSignals.Normalize(criteria.SignalId);
          bool raised = !string.IsNullOrWhiteSpace(normalized)
              && Registry.Registry.Contains(RegistryType.RuntimeState, normalized);
          int current = raised ? count : 0;
          return new QuestCriteriaEvaluationResult(raised, current, count);
        }
        case QuestCompletionCriteriaType.AllOf:
          return EvaluateCompositeGlobal(criteria.Conditions, true);
        case QuestCompletionCriteriaType.AnyOf:
          return EvaluateCompositeGlobal(criteria.Conditions, false);
        default:
          return new QuestCriteriaEvaluationResult(false, 0, 1);
      }
    }

    private static QuestCriteriaEvaluationResult EvaluateComposite(IReadOnlyList<QuestCompletionCriteria> conditions, bool requireAll, PlayerController playerController)
    {
      if (conditions == null || conditions.Count == 0)
        return new QuestCriteriaEvaluationResult(true, 1, 1);

      int satisfied = 0;
      for (int i = 0; i < conditions.Count; i++)
      {
        var each = conditions[i];
        if (each == null)
          continue;

        if (Evaluate(each, playerController).IsSatisfied)
          satisfied++;
      }

      int total = Math.Max(1, conditions.Count);
      if (requireAll)
      {
        return new QuestCriteriaEvaluationResult(satisfied >= total, Math.Min(satisfied, total), total);
      }

      int current = satisfied > 0 ? 1 : 0;
      return new QuestCriteriaEvaluationResult(satisfied > 0, current, 1);
    }

    private static QuestCriteriaEvaluationResult EvaluateCompositeGlobal(IReadOnlyList<QuestCompletionCriteria> conditions, bool requireAll)
    {
      if (conditions == null || conditions.Count == 0)
        return new QuestCriteriaEvaluationResult(true, 1, 1);

      int satisfied = 0;
      for (int i = 0; i < conditions.Count; i++)
      {
        var each = conditions[i];
        if (each == null)
          continue;

        if (EvaluateGlobal(each).IsSatisfied)
          satisfied++;
      }

      int total = Math.Max(1, conditions.Count);
      if (requireAll)
      {
        return new QuestCriteriaEvaluationResult(satisfied >= total, Math.Min(satisfied, total), total);
      }

      int current = satisfied > 0 ? 1 : 0;
      return new QuestCriteriaEvaluationResult(satisfied > 0, current, 1);
    }

    private static int CountItemAcrossAllPlayers(string itemId)
    {
      if (string.IsNullOrWhiteSpace(itemId))
        return 0;

      int total = 0;
      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        var each = players[i];
        if (each == null)
          continue;

        total += Math.Max(0, each.CountItemInInventory(itemId));
      }

      return total;
    }
  }

  public sealed class QuestDefinitionRegistry : MonoBehaviour
  {
    private const string QuestResourceRoot = "Quest";
    private const string QuestAssetSuffix = ".quest";

    [SerializeField] private TextAsset _definitionsJson;

    private readonly Dictionary<string, QuestDefinition> _definitions = new(StringComparer.Ordinal);
    private static Dictionary<string, QuestDefinition> _resourceDefinitions;

    private void Awake()
    {
      Registry.Registry.Register(RegistryType.Service, Registry.Registry.TypeKey<QuestDefinitionRegistry>(), this);
      LoadDefinitions();
    }

    private void OnDestroy()
    {
      Registry.Registry.Unregister(RegistryType.Service, Registry.Registry.TypeKey<QuestDefinitionRegistry>());
    }

    public bool TryGet(string identifier, out QuestDefinition definition)
    {
      definition = null;
      var normalizedIdentifier = NormalizeQuestLookupIdentifier(identifier);
      if (string.IsNullOrWhiteSpace(normalizedIdentifier))
        return false;

      if (_definitions.TryGetValue(normalizedIdentifier, out var existing) && existing != null)
      {
        definition = existing.Clone();
        return true;
      }

      return false;
    }

    private void LoadDefinitions()
    {
      _definitions.Clear();

      if (_definitionsJson == null || string.IsNullOrWhiteSpace(_definitionsJson.text))
      {
        return;
      }

      QuestDefinitionRegistryPayload payload;
      try
      {
        payload = JsonSerializer.Deserialize<QuestDefinitionRegistryPayload>(_definitionsJson.text, CreateQuestDefinitionsJsonOptions());
      }
      catch (Exception ex)
      {
        Debug.LogError($"[QuestDefinitionRegistry] Failed to parse quest definitions: {ex.Message}");
        return;
      }

      if (payload?.Definitions == null)
        return;

      for (int i = 0; i < payload.Definitions.Count; i++)
      {
        var definition = payload.Definitions[i];
        if (definition == null || string.IsNullOrWhiteSpace(definition.Identifier))
          continue;

        var key = ComposeNamespacedIdentifier(payload.Namespace, definition.Identifier);
        if (string.IsNullOrWhiteSpace(key))
          continue;

        var cloned = definition.Clone();
        cloned.Identifier = key;
        _definitions[key] = cloned;
      }
    }

    public static bool TryGetGlobal(string identifier, out QuestDefinition definition)
    {
      definition = null;
      var normalizedIdentifier = NormalizeQuestLookupIdentifier(identifier);
      if (string.IsNullOrWhiteSpace(normalizedIdentifier))
        return false;

      EnsureResourceLoaded();
      if (_resourceDefinitions == null)
        return false;

      if (_resourceDefinitions.TryGetValue(normalizedIdentifier, out var found) && found != null)
      {
        definition = found.Clone();
        return true;
      }

      return false;
    }

    public static void EnsureIncludesLoaded(IReadOnlyList<string> includes)
    {
      EnsureResourceLoaded();
      if (includes == null || includes.Count == 0)
        return;

      for (int i = 0; i < includes.Count; i++)
      {
        var include = includes[i]?.Trim();
        if (string.IsNullOrWhiteSpace(include))
          continue;

        LoadIncludeIntoResourceDefinitions(include);
      }
    }

    private static void EnsureResourceLoaded()
    {
      if (_resourceDefinitions != null)
        return;

      _resourceDefinitions = new Dictionary<string, QuestDefinition>(StringComparer.Ordinal);
      var definitionAssets = Resources.LoadAll<TextAsset>(QuestResourceRoot);
      if (definitionAssets == null || definitionAssets.Length == 0)
        return;

      for (int i = 0; i < definitionAssets.Length; i++)
      {
        var asset = definitionAssets[i];
        TryMergeDefinitionsFromAsset(asset, out _);
      }
    }

    private static void LoadIncludeIntoResourceDefinitions(string include)
    {
      var normalizedInclude = NormalizeIncludePath(include);
      if (string.IsNullOrWhiteSpace(normalizedInclude))
        return;

      var asset = Resources.Load<TextAsset>(normalizedInclude);
      if (asset == null)
      {
        Debug.LogWarning($"[QuestDefinitionRegistry] Quest include '{include}' not found at Resources/{normalizedInclude}.json");
        return;
      }

      TryMergeDefinitionsFromAsset(asset, out _);
    }

    private static bool TryMergeDefinitionsFromAsset(TextAsset definitionsAsset, out int loadedCount)
    {
      loadedCount = 0;
      if (definitionsAsset == null || string.IsNullOrWhiteSpace(definitionsAsset.text))
        return false;

      if (!definitionsAsset.name.EndsWith(QuestAssetSuffix, StringComparison.OrdinalIgnoreCase))
        return false;

      QuestDefinitionRegistryPayload payload;
      try
      {
        payload = JsonSerializer.Deserialize<QuestDefinitionRegistryPayload>(definitionsAsset.text, CreateQuestDefinitionsJsonOptions());
      }
      catch (Exception ex)
      {
        Debug.LogError($"[QuestDefinitionRegistry] Failed to parse fallback quest definitions from Resources/{definitionsAsset.name}: {ex.Message}");
        return false;
      }

      if (payload?.Definitions == null)
        return false;

      for (int i = 0; i < payload.Definitions.Count; i++)
      {
        var each = payload.Definitions[i];
        if (each == null || string.IsNullOrWhiteSpace(each.Identifier))
          continue;

        var key = ComposeNamespacedIdentifier(payload.Namespace, each.Identifier);
        if (string.IsNullOrWhiteSpace(key))
          continue;

        var cloned = each.Clone();
        cloned.Identifier = key;
        _resourceDefinitions[key] = cloned;
        loadedCount++;
      }

      return loadedCount > 0;
    }

    private static string NormalizeIncludePath(string include)
    {
      var normalized = include.Replace("\\", "/").Trim();
      if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
        normalized = normalized.Substring("Resources/".Length);

      if (!normalized.StartsWith("Quest/", StringComparison.OrdinalIgnoreCase))
        normalized = $"Quest/{normalized}";

      if (normalized.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        normalized = normalized.Substring(0, normalized.Length - ".json".Length);

      return normalized;
    }

    private static JsonSerializerOptions CreateQuestDefinitionsJsonOptions()
    {
      var options = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true
      };
      options.Converters.Add(new JsonStringEnumConverter());
      return options;
    }

    private static string NormalizeQuestLookupIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return null;

      var trimmed = identifier.Trim();
      var separatorIndex = trimmed.IndexOf("::", StringComparison.Ordinal);
      if (separatorIndex <= 0)
        return trimmed;

      var scope = trimmed.Substring(0, separatorIndex).Trim();
      var local = trimmed.Substring(separatorIndex + 2).Trim();
      if (string.IsNullOrWhiteSpace(scope) || string.IsNullOrWhiteSpace(local))
        return null;

      return $"{scope}::{local}";
    }

    private static string ComposeNamespacedIdentifier(string payloadNamespace, string identifier)
    {
      var local = identifier?.Trim();
      if (string.IsNullOrWhiteSpace(local))
        return null;

      // 이미 namespace::identifier 형태면 그대로 사용.
      if (local.Contains("::", StringComparison.Ordinal))
        return NormalizeQuestLookupIdentifier(local);

      var scope = payloadNamespace?.Trim();
      if (string.IsNullOrWhiteSpace(scope))
        return local;

      return $"{scope}::{local}";
    }
  }
}
