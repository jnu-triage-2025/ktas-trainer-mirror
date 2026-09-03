using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FishNet.Managing;
using FishNet.Transporting;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// UI 컨트롤러들이 공유하는 퀘스트 수명 주기와 추적 선택 상태를 관리한다.
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
    [SerializeField] private BasicMovementControlTutorialQuestResolver _basicMovementControlTutorialResolver;

    private readonly Dictionary<string, QuestData> _quests = new();
    private readonly List<string> _trackedQuestOrder = new();
    // 일반 criteria 평가로 다시 false가 되어서는 안 되는 전용 gameplay resolver 완료 상태.
    private readonly HashSet<string> _resolverCompletedQuestIds = new();
    private QuestDefinitionRegistry _definitionRegistry;
    private PlayerController _ownerPlayer;
    private NetworkManager _networkManager;
    private bool _subscribedToSessionLifecycle;
    private bool _evaluatingSignalProgress;

    public event Action<IReadOnlyList<QuestData>> OnQuestListChanged;
    public event Action<IReadOnlyList<QuestData>> OnTrackedQuestsChanged;
    public event Action<QuestData> OnQuestCompleted;
    public event Action<bool> OnQuestPreviewImmediateTransitionChanged;
    public event Action<string> OnQuestPresentationExpired;

    public void SetQuestPreviewImmediateTransition(bool enabled) =>
      OnQuestPreviewImmediateTransitionChanged?.Invoke(enabled);

    public void ExpireQuestPresentation(string questId)
    {
      if (!string.IsNullOrWhiteSpace(questId))
        OnQuestPresentationExpired?.Invoke(questId);
    }

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
      ScenarioInteractionSignals.OnSignalRegistered += HandleScenarioSignalRegistered;
      if (GetComponent<QuestPresentationService>() == null)
        gameObject.AddComponent<QuestPresentationService>();
      _definitionRegistry = Registry.Registry.Get<QuestDefinitionRegistry>(RegistryType.Service, Registry.Registry.TypeKey<QuestDefinitionRegistry>());
      EnsureBasicMovementControlTutorialResolver();
      SubscribeToSessionLifecycle();
    }

    private void OnDestroy()
    {
      ScenarioInteractionSignals.OnSignalRegistered -= HandleScenarioSignalRegistered;
      UnsubscribeFromSessionLifecycle();
      Registry.Registry.Unregister(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());
    }

    private void HandleScenarioSignalRegistered(string signalIdentifier)
    {
      // 신호가 퀘스트 목표를 완료한 직후 진행 상태와 프리뷰를 갱신한다.
      // 다음 시나리오 노드가 실행되기 전에 이전 목표가 한 프레임 이상 남지 않도록 한다.
      if (_evaluatingSignalProgress)
        return;

      _evaluatingSignalProgress = true;
      try
      {
        EvaluateAllQuestProgress();
      }
      finally
      {
        _evaluatingSignalProgress = false;
      }
    }

    private void Start()
    {
      // QuestManager가 NetworkManager보다 먼저 초기화되는 씬 구성을 지원한다.
      SubscribeToSessionLifecycle();
    }

    private void SubscribeToSessionLifecycle()
    {
      if (_subscribedToSessionLifecycle)
        return;

      _networkManager = FindAnyObjectByType<NetworkManager>();
      if (_networkManager == null)
        return;

      _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
      _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
      _subscribedToSessionLifecycle = true;
    }

    private void UnsubscribeFromSessionLifecycle()
    {
      if (!_subscribedToSessionLifecycle || _networkManager == null)
        return;

      _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
      _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
      _subscribedToSessionLifecycle = false;
    }

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Stopped)
        ResetProgressForSessionEnd();
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Stopped)
        ResetProgressForSessionEnd();
    }

    public void SetQuests(IEnumerable<QuestData> quests, bool clearExisting = true)
    {
      if (quests == null)
        return;

      if (clearExisting)
      {
        var existingIds = new List<string>(_quests.Keys);
        _quests.Clear();
        _trackedQuestOrder.Clear();
        _resolverCompletedQuestIds.Clear();
        for (int i = 0; i < existingIds.Count; i++)
          UpdateQuestCompletionRuntimeState(existingIds[i], false);
      }

      foreach (var quest in quests)
      {
        AddOrUpdateQuest(quest, notify: false);
      }

      var questIds = new List<string>(_quests.Keys);
      var completedSnapshots = new List<QuestData>();
      for (int i = 0; i < questIds.Count; i++)
      {
        if (EvaluateQuestProgressInternal(questIds[i], null, out var completedSnapshot) && completedSnapshot != null)
          completedSnapshots.Add(completedSnapshot);
      }

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();
      PublishCompleted(completedSnapshots);
    }

    public void AddOrUpdateQuest(QuestData quest, bool notify = true)
    {
      if (quest == null || string.IsNullOrWhiteSpace(quest.Id))
        return;

      var cloned = ResolveQuestData(quest);
      var isNewQuest = !_quests.TryGetValue(cloned.Id, out var existingQuest);
      if (isNewQuest)
        _resolverCompletedQuestIds.Remove(cloned.Id);
      bool wasCompleted = existingQuest?.Completed ?? false;
      bool wasTracked = !isNewQuest && _trackedQuestOrder.Contains(cloned.Id);
      if (!isNewQuest)
      {
        MergeQuestRuntimeState(existingQuest, cloned);
        cloned.IsTracked = wasTracked;
      }

      _quests[cloned.Id] = cloned;
      UpdateQuestCompletionRuntimeState(cloned.Id, cloned.Completed);

      if (isNewQuest)
        ClearCompletionSignals(GetQuestTasks(cloned));

      if (cloned.IsTracked)
      {
        EnsureTracked(cloned.Id, suppressNotify: true);
      }
      else
      {
        _trackedQuestOrder.Remove(cloned.Id);
      }

      if (isNewQuest)
      {
        TryHighlightWaypointForQuest(cloned);
      }

      if (notify)
      {
        EvaluateQuestProgressInternal(cloned.Id, wasCompleted, out var completedSnapshot);
        ClampTrackedToLimit();
        NotifyListChanged();
        NotifyTrackedChanged();
        PublishCompleted(completedSnapshot);
      }
    }

    public void EvaluateAllQuestProgress()
    {
      var questIds = new List<string>(_quests.Keys);
      var completedSnapshots = new List<QuestData>();
      for (int i = 0; i < questIds.Count; i++)
      {
        if (EvaluateQuestProgressInternal(questIds[i], null, out var completedSnapshot) && completedSnapshot != null)
          completedSnapshots.Add(completedSnapshot);
      }

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();
      PublishCompleted(completedSnapshots);
    }

    public bool EvaluateQuestProgress(string questId)
    {
      if (!EvaluateQuestProgressInternal(questId, null, out var completedSnapshot))
        return false;

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();
      PublishCompleted(completedSnapshot);
      return _quests.TryGetValue(questId, out var quest) && quest != null && quest.Completed;
    }

    /// <summary>
    /// 특수 게임플레이 resolver가 명시적으로 완료한 퀘스트를 반영한다.
    /// 일반 퀘스트는 criteria 평가를 계속 사용하며, 입력 교육처럼 criteria로 표현할 수 없는
    /// 누적 행동은 해당 resolver만 이 경로를 호출한다.
    /// </summary>
    public bool CompleteQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId)
          || !_quests.TryGetValue(questId, out var quest)
          || quest == null)
        return false;

      bool wasCompleted = quest.Completed;
      bool wasTracked = _trackedQuestOrder.Contains(questId);
      _resolverCompletedQuestIds.Add(questId);
      quest.Completed = true;
      quest.Progress = new QuestProgressValue(1, 1);
      MarkCriteriaCompleted(GetQuestTasks(quest));
      UpdateQuestCompletionRuntimeState(questId, true);

      if (quest.IsAutoComplete)
      {
        quest.IsTracked = false;
        _trackedQuestOrder.Remove(questId);
      }

      ClampTrackedToLimit();
      NotifyListChanged();
      NotifyTrackedChanged();

      if (!wasCompleted)
      {
        var completedSnapshot = quest.Clone();
        completedSnapshot.IsTracked = wasTracked;
        PublishCompleted(completedSnapshot);
      }

      return true;
    }

    private bool EvaluateQuestProgressInternal(string questId, bool? previousCompleted, out QuestData completedSnapshot)
    {
      completedSnapshot = null;
      if (string.IsNullOrWhiteSpace(questId) || !_quests.TryGetValue(questId, out var quest) || quest == null)
        return false;

      if (_resolverCompletedQuestIds.Contains(questId))
      {
        completedSnapshot = null;
        return true;
      }

      bool wasCompleted = previousCompleted ?? quest.Completed;
      bool wasTracked = _trackedQuestOrder.Contains(questId);
      var tasks = GetQuestTasks(quest);
      var evaluation = quest.Scope == QuestScopeType.Global
        ? QuestCriteriaEvaluator.EvaluateTreeGlobal(tasks, quest.IsOrdinal)
        : QuestCriteriaEvaluator.EvaluateTree(tasks, GetOwnerPlayerController(), quest.IsOrdinal);

      ApplyEvaluation(tasks, evaluation.Children);
      quest.Progress = new QuestProgressValue(evaluation.Result.Current, evaluation.Result.Target);
      quest.Completed = evaluation.Result.IsSatisfied;
      UpdateQuestCompletionRuntimeState(questId, quest.Completed);

      if (!wasCompleted && quest.Completed)
      {
        completedSnapshot = quest.Clone();
        completedSnapshot.IsTracked = wasTracked;
      }

      if (quest.Completed && quest.IsAutoComplete)
      {
        quest.IsTracked = false;
        _trackedQuestOrder.Remove(questId);
      }

      return true;
    }

    private void TryHighlightWaypointForQuest(QuestData quest)
    {
      if (!_featureFlags.HasFlag(FeatureFlags.HighlightAssignedWaypoint) || quest == null)
        return;

      string waypointIdentifier = GetActiveWaypointIdentifier(quest);
      if (string.IsNullOrWhiteSpace(waypointIdentifier))
        return;

      if (WaypointAnchor.TryGet(waypointIdentifier, out var anchor))
      {
        anchor.Highlight();
      }
    }

    public void RemoveQuest(string questId)
    {
      if (string.IsNullOrWhiteSpace(questId))
        return;

      _quests.Remove(questId);
      _trackedQuestOrder.Remove(questId);
      _resolverCompletedQuestIds.Remove(questId);
      UpdateQuestCompletionRuntimeState(questId, false);

      NotifyListChanged();
      NotifyTrackedChanged();
    }

    public void ClearAll()
    {
      var existingIds = new List<string>(_quests.Keys);
      _quests.Clear();
      _trackedQuestOrder.Clear();
      _resolverCompletedQuestIds.Clear();
      for (int i = 0; i < existingIds.Count; i++)
      {
        UpdateQuestCompletionRuntimeState(existingIds[i], false);
      }
      NotifyListChanged();
      NotifyTrackedChanged();
    }

    /// <summary>
    /// 세션 종료 시 기본 정책(진행 상태 초기화)을 적용한다.
    /// <see cref="QuestData.PersistProgressOnSessionEnd"/>가 true인 퀘스트만 유지한다.
    /// </summary>
    public void ResetProgressForSessionEnd()
    {
      var removedIds = new List<string>();
      foreach (var pair in _quests)
      {
        if (pair.Value?.PersistProgressOnSessionEnd == true)
          continue;

        removedIds.Add(pair.Key);
      }

      if (removedIds.Count == 0)
        return;

      for (int i = 0; i < removedIds.Count; i++)
      {
        string questId = removedIds[i];
        if (_quests.TryGetValue(questId, out var quest))
          ClearCompletionSignals(GetQuestTasks(quest));

        _quests.Remove(questId);
        _trackedQuestOrder.Remove(questId);
        _resolverCompletedQuestIds.Remove(questId);
        UpdateQuestCompletionRuntimeState(questId, false);
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

    private void PublishCompleted(QuestData completedSnapshot)
    {
      if (completedSnapshot == null
          || string.IsNullOrWhiteSpace(completedSnapshot.Id)
          || !_quests.TryGetValue(completedSnapshot.Id, out var currentQuest)
          || currentQuest == null
          || !currentQuest.Completed)
        return;

      OnQuestCompleted?.Invoke(completedSnapshot);
    }

    private void PublishCompleted(IReadOnlyList<QuestData> completedSnapshots)
    {
      if (completedSnapshots == null)
        return;

      for (int i = 0; i < completedSnapshots.Count; i++)
        PublishCompleted(completedSnapshots[i]);
    }

    private static bool ApplyEvaluation(IReadOnlyList<QuestCompletionCriteria> criteria, IReadOnlyList<QuestCriteriaEvaluationNode> evaluations)
    {
      if (criteria == null || evaluations == null)
        return false;

      bool changed = false;
      int count = Math.Min(criteria.Count, evaluations.Count);
      for (int i = 0; i < count; i++)
      {
        var criterion = criteria[i];
        var evaluation = evaluations[i];
        if (criterion == null || evaluation == null)
          continue;

        int previousCurrent = criterion.Progress?.Current ?? 0;
        int previousTarget = criterion.Progress?.Target ?? 1;
        bool previousCompleted = criterion.Completed;
        criterion.Progress = new QuestProgressValue(evaluation.Result.Current, evaluation.Result.Target);
        criterion.Completed = evaluation.Result.IsSatisfied;
        changed |= previousCurrent != criterion.Progress.Current
            || previousTarget != criterion.Progress.Target
            || previousCompleted != criterion.Completed;
        changed |= ApplyEvaluation(criterion.Conditions, evaluation.Children);
        if (!previousCompleted && criterion.Completed && !string.IsNullOrWhiteSpace(criterion.OnCompleteSignalIdentifier))
          ScenarioInteractionSignals.Raise(criterion.OnCompleteSignalIdentifier);
      }

      return changed;
    }

    private static void MarkCriteriaCompleted(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      if (criteria == null)
        return;

      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion == null)
          continue;

        bool wasCompleted = criterion.Completed;
        int target = Math.Max(1, criterion.Count);
        criterion.Progress = new QuestProgressValue(target, target);
        criterion.Completed = true;
        if (!wasCompleted && !string.IsNullOrWhiteSpace(criterion.OnCompleteSignalIdentifier))
          ScenarioInteractionSignals.Raise(criterion.OnCompleteSignalIdentifier);
        MarkCriteriaCompleted(criterion.Conditions);
      }
    }

    private void EnsureBasicMovementControlTutorialResolver()
    {
      if (_basicMovementControlTutorialResolver == null)
        _basicMovementControlTutorialResolver = GetComponent<BasicMovementControlTutorialQuestResolver>();

      if (_basicMovementControlTutorialResolver == null)
        _basicMovementControlTutorialResolver = gameObject.AddComponent<BasicMovementControlTutorialQuestResolver>();
    }

    private static void ClearCompletionSignals(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      if (criteria == null)
        return;

      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion == null)
          continue;

        if (!string.IsNullOrWhiteSpace(criterion.OnCompleteSignalIdentifier))
          ScenarioInteractionSignals.Clear(criterion.OnCompleteSignalIdentifier);
        ClearCompletionSignals(criterion.Conditions);
      }
    }

    public static IReadOnlyList<QuestCompletionCriteria> GetQuestTasks(QuestData quest)
    {
      if (quest?.Tasks != null && quest.Tasks.Count > 0)
        return quest.Tasks;

      if (quest?.CompletionCriteria != null)
        return quest.CompletionCriteria;

      return Array.Empty<QuestCompletionCriteria>();
    }

    public static IReadOnlyList<string> GetActiveWaypointIdentifiers(QuestData quest)
    {
      var result = new List<string>();
      var tasks = GetQuestTasks(quest);
      if (tasks == null || tasks.Count == 0)
        return result;

      int start = quest != null && quest.IsOrdinal ? GetOrdinalActiveTaskIndex(tasks) : 0;
      int end = quest != null && quest.IsOrdinal ? Math.Min(tasks.Count, start + 1) : tasks.Count;
      for (int i = start; i < end; i++)
      {
        CollectActiveWaypoints(tasks[i], result);
      }

      return result;
    }

    public static string GetActiveWaypointIdentifier(QuestData quest)
    {
      var identifiers = GetActiveWaypointIdentifiers(quest);
      return identifiers.Count > 0 ? identifiers[0] : null;
    }

    private static int GetOrdinalActiveTaskIndex(IReadOnlyList<QuestCompletionCriteria> tasks)
    {
      for (int i = 0; i < tasks.Count; i++)
      {
        if (tasks[i] != null && !tasks[i].Completed)
          return i;
      }

      return tasks.Count;
    }

    private static void CollectActiveWaypoints(QuestCompletionCriteria criterion, List<string> result)
    {
      if (criterion == null || criterion.Completed)
        return;

      if (criterion.Type == QuestCompletionCriteriaType.WaypointReached
          && !string.IsNullOrWhiteSpace(criterion.WaypointIdentifier)
          && !result.Contains(criterion.WaypointIdentifier))
        result.Add(criterion.WaypointIdentifier);

      if (criterion.Conditions == null)
        return;

      for (int i = 0; i < criterion.Conditions.Count; i++)
        CollectActiveWaypoints(criterion.Conditions[i], result);
    }

    private static void MergeQuestRuntimeState(QuestData source, QuestData destination)
    {
      if (source == null || destination == null)
        return;

      destination.Progress = source.Progress?.Clone() ?? destination.Progress;
      destination.Completed = source.Completed;
      MergeCriteriaRuntimeState(GetQuestTasks(source), GetQuestTasks(destination));
    }

    private static void MergeCriteriaRuntimeState(IReadOnlyList<QuestCompletionCriteria> source, IReadOnlyList<QuestCompletionCriteria> destination)
    {
      if (source == null || destination == null)
        return;

      int count = Math.Min(source.Count, destination.Count);
      for (int i = 0; i < count; i++)
      {
        var sourceCriterion = source[i];
        var destinationCriterion = destination[i];
        if (sourceCriterion == null || destinationCriterion == null || sourceCriterion.Type != destinationCriterion.Type)
          continue;

        destinationCriterion.Progress = sourceCriterion.Progress?.Clone() ?? QuestProgressValue.SingleStep;
        destinationCriterion.Completed = sourceCriterion.Completed;
        MergeCriteriaRuntimeState(sourceCriterion.Conditions, destinationCriterion.Conditions);
      }
    }

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

        RestoreDefinitionRuntimeState(resolved, fromDefinition);

        return fromDefinition;
      }

      if (!string.IsNullOrWhiteSpace(resolved.DefinitionIdentifier))
      {
        // 정의 식별자를 지정했는데 해석하지 못하면 제목과 과제가 비어 있는 퀘스트가 그대로 등록되고,
        // 완료 조건이 없으므로 영원히 끝나지 않는다. 조용히 넘어가면 원인을 추적할 수 없다.
        Debug.LogWarning(
          $"[QuestManager] 퀘스트 정의 '{resolved.DefinitionIdentifier}' 를 찾지 못했습니다. "
          + $"퀘스트 '{resolved.Id}' 를 인라인 데이터만으로 등록합니다. "
          + "정의 식별자의 오타나 include 누락 여부를 확인하세요.");
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

        RestoreDefinitionRuntimeState(resolved, fallbackById);

        return fallbackById;
      }

      if (resolved.Progress == null)
        resolved.Progress = new QuestProgressValue(0, 1);

      if (resolved.CompletionCriteria == null)
        resolved.CompletionCriteria = new List<QuestCompletionCriteria>();

      return resolved;
    }

    private static void RestoreDefinitionRuntimeState(QuestData source, QuestData destination)
    {
      destination.SourceScenarioIdentifier = source.SourceScenarioIdentifier;
      if (source.PresentationBindings != null && source.PresentationBindings.Count > 0)
        destination.PresentationBindings = QuestData.ClonePresentationBindings(source.PresentationBindings);

      var sourceTasks = GetQuestTasks(source);
      if (sourceTasks == null || sourceTasks.Count == 0)
        return;

      destination.IsOrdinal = source.IsOrdinal;
      destination.Progress = source.Progress?.Clone() ?? destination.Progress;
      destination.Completed = source.Completed;
      MergeCriteriaRuntimeState(sourceTasks, GetQuestTasks(destination));
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
        PersistProgressOnSessionEnd = definition.PersistProgressOnSessionEnd,
        IsTracked = definition.IsTrackable && definition.IsTrackedByDefault,
        Scope = definition.Scope,
        IsOrdinal = definition.IsOrdinal,
        Tasks = CloneCriteria(definition.Tasks),
        CompletionCriteria = CloneCriteria(definition.CompletionCriteria),
        PresentationBindings = QuestData.ClonePresentationBindings(definition.PresentationBindings),
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
      var questIds = new List<string>(_quests.Keys);
      var completedSnapshots = new List<QuestData>();
      for (int i = 0; i < questIds.Count; i++)
      {
        string questId = questIds[i];
        if (!_quests.TryGetValue(questId, out var quest) || quest == null)
          continue;

        var before = quest.Clone();
        if (!EvaluateQuestProgressInternal(questId, null, out var completedSnapshot))
          continue;

        if (completedSnapshot != null)
          completedSnapshots.Add(completedSnapshot);

        if (_quests.TryGetValue(questId, out var currentQuest) && HasQuestStateChanged(before, currentQuest))
          changed = true;
      }

      if (changed)
      {
        ClampTrackedToLimit();
        NotifyListChanged();
        NotifyTrackedChanged();
      }

      PublishCompleted(completedSnapshots);
    }

    private static bool HasQuestStateChanged(QuestData before, QuestData after)
    {
      if (before == null || after == null)
        return before != after;

      if (before.Completed != after.Completed || before.IsTracked != after.IsTracked)
        return true;

      if ((before.Progress?.Current ?? 0) != (after.Progress?.Current ?? 0)
          || (before.Progress?.Target ?? 1) != (after.Progress?.Target ?? 1))
        return true;

      return before.IsOrdinal != after.IsOrdinal
          || HasCriteriaStateChanged(GetQuestTasks(before), GetQuestTasks(after));
    }

    private static bool HasCriteriaStateChanged(IReadOnlyList<QuestCompletionCriteria> before, IReadOnlyList<QuestCompletionCriteria> after)
    {
      int beforeCount = before?.Count ?? 0;
      int afterCount = after?.Count ?? 0;
      if (beforeCount != afterCount)
        return true;

      for (int i = 0; i < beforeCount; i++)
      {
        var beforeCriterion = before[i];
        var afterCriterion = after[i];
        if (beforeCriterion == null || afterCriterion == null)
        {
          if (beforeCriterion != afterCriterion)
            return true;
          continue;
        }

        if (beforeCriterion.Completed != afterCriterion.Completed
            || (beforeCriterion.Progress?.Current ?? 0) != (afterCriterion.Progress?.Current ?? 0)
            || (beforeCriterion.Progress?.Target ?? 1) != (afterCriterion.Progress?.Target ?? 1)
            || HasCriteriaStateChanged(beforeCriterion.Conditions, afterCriterion.Conditions))
          return true;
      }

      return false;
    }
  }

  public static class QuestCriteriaEvaluator
  {
    public static QuestCriteriaEvaluationResult Evaluate(IReadOnlyList<QuestCompletionCriteria> criteria, PlayerController playerController)
    {
      return EvaluateTree(criteria, playerController, false).Result;
    }

    public static QuestCriteriaEvaluationResult EvaluateGlobal(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      return EvaluateTreeGlobal(criteria, false).Result;
    }

    internal static QuestCriteriaEvaluationNode EvaluateTree(IReadOnlyList<QuestCompletionCriteria> criteria, PlayerController playerController, bool isOrdinal)
    {
      if (criteria == null || criteria.Count == 0)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      var children = new List<QuestCriteriaEvaluationNode>(criteria.Count);
      int satisfied = 0;
      bool activeTaskEvaluated = false;
      bool ordinalPrefixIntact = true;
      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        bool preserveCompletedTask = isOrdinal && ordinalPrefixIntact && each != null && each.Completed;
        bool canEvaluate = !isOrdinal || !activeTaskEvaluated;
        var child = preserveCompletedTask || !canEvaluate
          ? CreatePreservedNode(each)
          : EvaluateNode(each, playerController);
        if (isOrdinal && !ordinalPrefixIntact)
          child = CreateIncompleteNode(each);
        children.Add(child);
        if (child.Result.IsSatisfied)
          satisfied++;
        if (isOrdinal && !preserveCompletedTask)
        {
          activeTaskEvaluated = true;
          ordinalPrefixIntact = false;
        }
      }

      int target = Math.Max(1, criteria.Count);
      var result = new QuestCriteriaEvaluationResult(satisfied >= target, Math.Min(satisfied, target), target);
      return new QuestCriteriaEvaluationNode(result, children);
    }

    internal static QuestCriteriaEvaluationNode EvaluateTreeGlobal(IReadOnlyList<QuestCompletionCriteria> criteria, bool isOrdinal)
    {
      if (criteria == null || criteria.Count == 0)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      var children = new List<QuestCriteriaEvaluationNode>(criteria.Count);
      int satisfied = 0;
      bool activeTaskEvaluated = false;
      bool ordinalPrefixIntact = true;
      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        bool preserveCompletedTask = isOrdinal && ordinalPrefixIntact && each != null && each.Completed;
        bool canEvaluate = !isOrdinal || !activeTaskEvaluated;
        var child = preserveCompletedTask || !canEvaluate
          ? CreatePreservedNode(each)
          : EvaluateNodeGlobal(each);
        if (isOrdinal && !ordinalPrefixIntact)
          child = CreateIncompleteNode(each);
        children.Add(child);
        if (child.Result.IsSatisfied)
          satisfied++;
        if (isOrdinal && !preserveCompletedTask)
        {
          activeTaskEvaluated = true;
          ordinalPrefixIntact = false;
        }
      }

      int target = Math.Max(1, criteria.Count);
      var result = new QuestCriteriaEvaluationResult(satisfied >= target, Math.Min(satisfied, target), target);
      return new QuestCriteriaEvaluationNode(result, children);
    }

    private static QuestCriteriaEvaluationNode EvaluateNode(QuestCompletionCriteria criteria, PlayerController playerController)
    {
      if (criteria == null)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      int count = Math.Max(1, criteria.Count);

      switch (criteria.Type)
      {
        case QuestCompletionCriteriaType.InventoryContains:
          {
            int inventoryCount = playerController != null
                ? playerController.CountItemInInventory(criteria.ItemId)
                : 0;
            bool satisfied = inventoryCount >= count;
            return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(satisfied, Math.Min(inventoryCount, count), count));
          }

        case QuestCompletionCriteriaType.InteractionSignalReceived:
          {
            // SignalScope.Owner 조건은 이 퀘스트를 보유한 참여자가 직접 올린 신호만 인정한다.
            // 귀속을 확인할 수 없으면 ScenarioSignalAttribution 이 전역 판정으로 물러서므로
            // 진행이 막히지 않는다.
            bool raised = ScenarioSignalAttribution.IsSatisfied(
                criteria.SignalId, criteria.SignalScope, playerController?.UserIdentifier);
            int current = raised ? count : 0;
            return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(raised, current, count));
          }

        case QuestCompletionCriteriaType.WaypointReached:
          {
            bool reached = IsWaypointReached(criteria, playerController);
            return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(reached, reached ? 1 : 0, 1));
          }

        case QuestCompletionCriteriaType.AllOf:
          return EvaluateComposite(criteria.Conditions, true, playerController);

        case QuestCompletionCriteriaType.AnyOf:
          return EvaluateComposite(criteria.Conditions, false, playerController);

        default:
          return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));
      }
    }

    private static QuestCriteriaEvaluationNode EvaluateNodeGlobal(QuestCompletionCriteria criteria)
    {
      if (criteria == null)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      int count = Math.Max(1, criteria.Count);

      switch (criteria.Type)
      {
        case QuestCompletionCriteriaType.InventoryContains:
          {
            int totalCount = CountItemAcrossAllPlayers(criteria.ItemId);
            bool satisfied = totalCount >= count;
            return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(satisfied, Math.Min(totalCount, count), count));
          }
        case QuestCompletionCriteriaType.InteractionSignalReceived:
          {
            // Global 스코프 퀘스트는 참여자 전원이 함께 달성하는 목표이므로 발신자를 구분하지 않는다.
            bool raised = ScenarioSignalAttribution.IsRaised(criteria.SignalId);
            int current = raised ? count : 0;
            return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(raised, current, count));
          }
        case QuestCompletionCriteriaType.WaypointReached:
          {
            bool reached = IsWaypointReachedGlobal(criteria);
            return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(reached, reached ? 1 : 0, 1));
          }
        case QuestCompletionCriteriaType.AllOf:
          return EvaluateCompositeGlobal(criteria.Conditions, true);
        case QuestCompletionCriteriaType.AnyOf:
          return EvaluateCompositeGlobal(criteria.Conditions, false);
        default:
          return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));
      }
    }

    private static QuestCriteriaEvaluationNode EvaluateComposite(IReadOnlyList<QuestCompletionCriteria> conditions, bool requireAll, PlayerController playerController)
    {
      if (conditions == null || conditions.Count == 0)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      var children = new List<QuestCriteriaEvaluationNode>(conditions.Count);
      int satisfied = 0;
      for (int i = 0; i < conditions.Count; i++)
      {
        var child = EvaluateNode(conditions[i], playerController);
        children.Add(child);
        if (child.Result.IsSatisfied)
          satisfied++;
      }

      int total = Math.Max(1, conditions.Count);
      if (requireAll)
      {
        var result = new QuestCriteriaEvaluationResult(satisfied >= total, Math.Min(satisfied, total), total);
        return new QuestCriteriaEvaluationNode(result, children);
      }

      int current = satisfied > 0 ? 1 : 0;
      return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(satisfied > 0, current, 1), children);
    }

    private static QuestCriteriaEvaluationNode EvaluateCompositeGlobal(IReadOnlyList<QuestCompletionCriteria> conditions, bool requireAll)
    {
      if (conditions == null || conditions.Count == 0)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      var children = new List<QuestCriteriaEvaluationNode>(conditions.Count);
      int satisfied = 0;
      for (int i = 0; i < conditions.Count; i++)
      {
        var child = EvaluateNodeGlobal(conditions[i]);
        children.Add(child);
        if (child.Result.IsSatisfied)
          satisfied++;
      }

      int total = Math.Max(1, conditions.Count);
      if (requireAll)
      {
        var result = new QuestCriteriaEvaluationResult(satisfied >= total, Math.Min(satisfied, total), total);
        return new QuestCriteriaEvaluationNode(result, children);
      }

      int current = satisfied > 0 ? 1 : 0;
      return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(satisfied > 0, current, 1), children);
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

    private static bool IsWaypointReached(QuestCompletionCriteria criteria, PlayerController playerController)
    {
      if (playerController == null
          || string.IsNullOrWhiteSpace(criteria.WaypointIdentifier)
          || !WaypointAnchor.TryGet(criteria.WaypointIdentifier, out var anchor)
          || anchor == null)
        return false;

      float reachDistance = Mathf.Max(0.01f, criteria.ReachDistance);
      return (playerController.transform.position - anchor.transform.position).sqrMagnitude <= reachDistance * reachDistance;
    }

    private static bool IsWaypointReachedGlobal(QuestCompletionCriteria criteria)
    {
      if (string.IsNullOrWhiteSpace(criteria.WaypointIdentifier)
          || !WaypointAnchor.TryGet(criteria.WaypointIdentifier, out var anchor)
          || anchor == null)
        return false;

      float reachDistance = Mathf.Max(0.01f, criteria.ReachDistance);
      float reachDistanceSquared = reachDistance * reachDistance;
      var players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
      for (int i = 0; i < players.Length; i++)
      {
        var player = players[i];
        if (player != null && (player.transform.position - anchor.transform.position).sqrMagnitude <= reachDistanceSquared)
          return true;
      }

      return false;
    }

    private static QuestCriteriaEvaluationNode CreatePreservedNode(QuestCompletionCriteria criterion)
    {
      if (criterion == null)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      var children = new List<QuestCriteriaEvaluationNode>();
      if (criterion.Conditions != null)
      {
        for (int i = 0; i < criterion.Conditions.Count; i++)
          children.Add(CreatePreservedNode(criterion.Conditions[i]));
      }

      var progress = criterion.Progress ?? QuestProgressValue.SingleStep;
      var result = new QuestCriteriaEvaluationResult(criterion.Completed, progress.Current, progress.Target);
      return new QuestCriteriaEvaluationNode(result, children);
    }

    private static QuestCriteriaEvaluationNode CreateIncompleteNode(QuestCompletionCriteria criterion)
    {
      if (criterion == null)
        return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, 1));

      var children = new List<QuestCriteriaEvaluationNode>();
      if (criterion.Conditions != null)
      {
        for (int i = 0; i < criterion.Conditions.Count; i++)
          children.Add(CreateIncompleteNode(criterion.Conditions[i]));
      }

      int target = Math.Max(1, criterion.Progress?.Target ?? criterion.Count);
      return new QuestCriteriaEvaluationNode(new QuestCriteriaEvaluationResult(false, 0, target), children);
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
        {
          Debug.LogWarning(
            $"[QuestDefinitionRegistry] {i}번째 퀘스트 정의에 식별자가 없어 등록하지 않습니다.");
          continue;
        }

        var key = ComposeNamespacedIdentifier(payload.Namespace, definition.Identifier);
        if (string.IsNullOrWhiteSpace(key))
        {
          Debug.LogWarning(
            $"[QuestDefinitionRegistry] 퀘스트 정의 '{definition.Identifier}' 의 네임스페이스 결합 결과가 "
            + "비어 있어 등록하지 않습니다.");
          continue;
        }

        if (_definitions.ContainsKey(key))
        {
          Debug.LogWarning(
            $"[QuestDefinitionRegistry] 퀘스트 정의 식별자 '{key}' 가 중복되어 이전 정의를 덮어씁니다.");
        }

        var cloned = definition.Clone();
        cloned.Identifier = key;
        WarnInvalidPresentationBindings(cloned, key);
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

    /// <summary>에디터 작성 도구가 Resources/Quest 에셋을 변경한 뒤 호출한다.</summary>
    public static void InvalidateResourceCache()
    {
      _resourceDefinitions = null;
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
        WarnInvalidPresentationBindings(cloned, key);
        _resourceDefinitions[key] = cloned;
        loadedCount++;
      }

      return loadedCount > 0;
    }

    private static void WarnInvalidPresentationBindings(QuestDefinition definition, string definitionIdentifier)
    {
      if (definition?.PresentationBindings == null || definition.PresentationBindings.Count == 0)
        return;

      var criterionIdentifiers = new HashSet<string>(StringComparer.Ordinal);
      CollectCriterionIdentifiers(definition.Tasks, criterionIdentifiers, definitionIdentifier);
      CollectCriterionIdentifiers(definition.CompletionCriteria, criterionIdentifiers, definitionIdentifier);
      var bindingKeys = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < definition.PresentationBindings.Count; i++)
      {
        var binding = definition.PresentationBindings[i];
        if (binding == null
            || string.IsNullOrWhiteSpace(binding.EntityIdentifier)
            || string.IsNullOrWhiteSpace(binding.IconIdentifier)
            || (binding.TargetType == QuestPresentationTargetType.Interaction
                && string.IsNullOrWhiteSpace(binding.InteractionIdentifier))
            || (binding.Activation == QuestPresentationActivation.CompletionCriteria
                && !criterionIdentifiers.Contains(binding.CompletionCriteriaIdentifier ?? string.Empty)))
        {
          Debug.LogWarning($"[QuestDefinitionRegistry] Definition '{definitionIdentifier}' has an invalid presentation binding at index {i}.");
          continue;
        }

        string key = $"{binding.TargetType}|{binding.EntityIdentifier}|{binding.InteractionIdentifier}|{binding.Priority}";
        if (!bindingKeys.Add(key))
          Debug.LogWarning($"[QuestDefinitionRegistry] Definition '{definitionIdentifier}' has duplicate presentation binding target at index {i}.");
      }
    }

    private static void CollectCriterionIdentifiers(
      IReadOnlyList<QuestCompletionCriteria> criteria,
      HashSet<string> identifiers,
      string definitionIdentifier)
    {
      if (criteria == null)
        return;

      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion == null)
          continue;

        if (!string.IsNullOrWhiteSpace(criterion.Identifier) && !identifiers.Add(criterion.Identifier))
          Debug.LogWarning($"[QuestDefinitionRegistry] Definition '{definitionIdentifier}' has duplicate completion criterion identifier '{criterion.Identifier}'.");
        CollectCriterionIdentifiers(criterion.Conditions, identifiers, definitionIdentifier);
      }
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
