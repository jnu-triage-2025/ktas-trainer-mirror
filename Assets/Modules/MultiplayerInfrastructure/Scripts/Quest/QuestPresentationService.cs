using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Commons;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using UnityEngine;

namespace MultiplayerInfrastructure.Quest
{
  [DisallowMultipleComponent]
  public sealed class QuestPresentationService : MonoBehaviour
  {
    private readonly struct InteractionKey : IEquatable<InteractionKey>
    {
      public readonly string EntityIdentifier;
      public readonly string InteractionIdentifier;

      public InteractionKey(string entityIdentifier, string interactionIdentifier)
      {
        EntityIdentifier = entityIdentifier ?? string.Empty;
        InteractionIdentifier = interactionIdentifier ?? string.Empty;
      }

      /// <summary>대상 종류까지 포함해 NPC 마크와 상호작용 마크를 같은 사전에서 구분한다.</summary>
      public static InteractionKey ForTarget(
        QuestPresentationTargetType targetType,
        string entityIdentifier,
        string interactionIdentifier)
        => new InteractionKey(
          $"{targetType}|{entityIdentifier?.Trim() ?? string.Empty}",
          targetType == QuestPresentationTargetType.Interaction
            ? interactionIdentifier?.Trim() ?? string.Empty
            : string.Empty);

      public bool Equals(InteractionKey other) =>
        string.Equals(EntityIdentifier, other.EntityIdentifier, StringComparison.Ordinal)
        && string.Equals(InteractionIdentifier, other.InteractionIdentifier, StringComparison.Ordinal);

      public override bool Equals(object obj) => obj is InteractionKey other && Equals(other);

      public override int GetHashCode()
      {
        unchecked
        {
          return ((EntityIdentifier != null ? EntityIdentifier.GetHashCode() : 0) * 397)
                 ^ (InteractionIdentifier != null ? InteractionIdentifier.GetHashCode() : 0);
        }
      }
    }

    private sealed class ActiveBinding
    {
      public string QuestIdentifier;
      public int QuestOrder;
      public int BindingIndex;
      public QuestPresentationBinding Binding;
      public Sprite Icon;
    }

    /// <summary>시나리오 그래프의 QuestMark 노드가 선언한 마크 하나.</summary>
    private sealed class ScenarioMark
    {
      public QuestPresentationTargetType TargetType;
      public string EntityIdentifier;
      public string InteractionIdentifier;
      public string IconIdentifier;
      public int Priority;
    }

    /// <summary>시나리오 그래프가 명시적으로 켠 마크. 퀘스트 수명주기와 독립적이므로 서비스 인스턴스 밖에 보관한다.</summary>
    private static readonly Dictionary<InteractionKey, ScenarioMark> _scenarioMarks = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetScenarioMarks() => _scenarioMarks.Clear();

    public static QuestPresentationService ActiveInstance { get; private set; }

    /// <summary>이름표(0)보다 위에 마크를 쌓기 위한 기준 채널 순서.</summary>
    private const int OverheadQuestMarkChannelOrder = 100;

    public event Action OnPresentationChanged;
    public static event Action PresentationChanged;

    private readonly Dictionary<InteractionKey, ActiveBinding> _interactionBindings = new();
    /// <summary>월드 앵커 위에 아이콘을 띄우는 대상(NPC 머리 위, waypoint)의 마크. 키에 대상 종류를 포함한다.</summary>
    private readonly Dictionary<InteractionKey, ActiveBinding> _anchoredBindings = new();
    private readonly List<(EntityOverheadLabelUIController Controller, Transform Anchor, string Channel)> _overheadMarkers = new();
    private readonly HashSet<string> _previousIncompleteQuestIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _previousTrackedQuestIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completionGraceQuestIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> _reportedUnaddressableInteractions = new(StringComparer.Ordinal);
    private QuestManager _questManager;
    private ScenarioController _scenarioController;

    private void Awake()
    {
      ActiveInstance = this;
      _questManager = GetComponent<QuestManager>();
    }

    private void OnEnable()
    {
      if (_questManager == null)
        _questManager = GetComponent<QuestManager>();

      if (_questManager != null)
      {
        _questManager.OnQuestListChanged += HandleQuestListChanged;
        _questManager.OnQuestPresentationExpired += ExpireQuestPresentation;
      }

      ScenarioController.InstanceAvailable += HandleScenarioControllerAvailable;
      _scenarioController = ScenarioController.Instance ?? FindAnyObjectByType<ScenarioController>();
      SubscribeToScenarioController(_scenarioController);

      Registry.Registry.OnEntryRegistered += HandleRegistryEntryChanged;
      Registry.Registry.OnEntryUnregistered += HandleRegistryEntryRemoved;
      Reconcile(_questManager?.Quests);
    }

    private void OnDisable()
    {
      if (_questManager != null)
      {
        _questManager.OnQuestListChanged -= HandleQuestListChanged;
        _questManager.OnQuestPresentationExpired -= ExpireQuestPresentation;
      }

      ScenarioController.InstanceAvailable -= HandleScenarioControllerAvailable;
      UnsubscribeFromScenarioController();
      _scenarioController = null;

      Registry.Registry.OnEntryRegistered -= HandleRegistryEntryChanged;
      Registry.Registry.OnEntryUnregistered -= HandleRegistryEntryRemoved;
      _interactionBindings.Clear();
      _anchoredBindings.Clear();
      ClearOverheadMarkers();
      _previousIncompleteQuestIds.Clear();
      _previousTrackedQuestIds.Clear();
      _completionGraceQuestIds.Clear();
      _reportedUnaddressableInteractions.Clear();
      OnPresentationChanged?.Invoke();
      PresentationChanged?.Invoke();
    }

    private void OnDestroy()
    {
      if (ReferenceEquals(ActiveInstance, this))
        ActiveInstance = null;
    }

    public bool TryGetPrimaryIconOverride(IInteract interact, out Sprite icon)
    {
      icon = null;
      if (interact is not IQuestPresentationTarget target)
        return false;

      if (string.IsNullOrWhiteSpace(target.PresentationEntityIdentifier)
          || string.IsNullOrWhiteSpace(target.InteractionIdentifier))
      {
        WarnUnaddressableInteraction(interact);
        return false;
      }

      return TryGetPrimaryIconOverride(
        target.PresentationEntityIdentifier,
        target.InteractionIdentifier,
        out icon);
    }

    public bool TryGetPrimaryIconOverride(
      string entityIdentifier,
      string interactionIdentifier,
      out Sprite icon)
    {
      icon = null;
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(interactionIdentifier))
        return false;

      var key = new InteractionKey(
        entityIdentifier.Trim(),
        interactionIdentifier.Trim());
      if (!_interactionBindings.TryGetValue(key, out var active) || active?.Icon == null)
        return false;

      icon = active.Icon;
      return true;
    }

    /// <summary>
    /// 시나리오 그래프의 QuestMark 노드가 켠 마크를 등록한다. 같은 대상에 이미 마크가 있으면 덮어쓴다.
    /// 아이콘 식별자를 비우면 대상 종류별 기본 퀘스트 마크 아이콘을 사용한다.
    /// </summary>
    public static void SetScenarioMark(
      QuestPresentationTargetType targetType,
      string entityIdentifier,
      string interactionIdentifier,
      string iconIdentifier,
      int priority)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      if (targetType == QuestPresentationTargetType.Interaction
          && string.IsNullOrWhiteSpace(interactionIdentifier))
        return;

      _scenarioMarks[InteractionKey.ForTarget(targetType, entityIdentifier, interactionIdentifier)] = new ScenarioMark
      {
        TargetType = targetType,
        EntityIdentifier = entityIdentifier.Trim(),
        InteractionIdentifier = interactionIdentifier?.Trim(),
        IconIdentifier = string.IsNullOrWhiteSpace(iconIdentifier)
          ? DefaultMarkIconIdentifier(targetType)
          : iconIdentifier.Trim(),
        Priority = priority
      };
      ActiveInstance?.RefreshPresentation();
    }

    /// <summary>시나리오 그래프의 QuestMark 노드가 끈 마크를 해제한다.</summary>
    public static void ClearScenarioMark(
      QuestPresentationTargetType targetType,
      string entityIdentifier,
      string interactionIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      if (_scenarioMarks.Remove(InteractionKey.ForTarget(targetType, entityIdentifier, interactionIdentifier)))
        ActiveInstance?.RefreshPresentation();
    }

    /// <summary>시나리오가 끝날 때 그래프가 켠 마크를 모두 해제한다.</summary>
    public static void ClearScenarioMarks()
    {
      if (_scenarioMarks.Count == 0)
        return;

      _scenarioMarks.Clear();
      ActiveInstance?.RefreshPresentation();
    }

    private static string DefaultMarkIconIdentifier(QuestPresentationTargetType targetType)
      => targetType == QuestPresentationTargetType.Interaction
        ? IconSpriteIdentifiers.QuestInteractionMark
        : IconSpriteIdentifiers.QuestNpcMark;

    public void RefreshPresentation()
    {
      if (_questManager == null)
        _questManager = GetComponent<QuestManager>();
      Reconcile(_questManager?.Quests);
    }

    private void HandleQuestListChanged(IReadOnlyList<QuestData> quests) => Reconcile(quests);

    private void HandleScenarioControllerAvailable(ScenarioController controller)
    {
      if (controller == null || ReferenceEquals(_scenarioController, controller))
        return;

      UnsubscribeFromScenarioController();
      _scenarioController = controller;
      SubscribeToScenarioController(controller);
    }

    private void SubscribeToScenarioController(ScenarioController controller)
    {
      if (controller != null)
        controller.OnScenarioEnded += HandleScenarioEnded;
    }

    private void UnsubscribeFromScenarioController()
    {
      if (_scenarioController != null)
        _scenarioController.OnScenarioEnded -= HandleScenarioEnded;
    }

    private void HandleScenarioEnded()
    {
      _scenarioMarks.Clear();
      _interactionBindings.Clear();
      _anchoredBindings.Clear();
      ClearOverheadMarkers();
      OnPresentationChanged?.Invoke();
      PresentationChanged?.Invoke();
      Reconcile(_questManager?.Quests);
    }

    public void ExpireQuestPresentation(string questIdentifier)
    {
      if (_completionGraceQuestIds.Remove(questIdentifier ?? string.Empty))
        Reconcile(_questManager?.Quests);
    }

    private void HandleRegistryEntryChanged(RegistryType type, string identifier, object value)
    {
      if (IsPresentationAffectingRegistry(type))
        Reconcile(_questManager?.Quests);
    }

    private void HandleRegistryEntryRemoved(RegistryType type, string identifier)
    {
      if (IsPresentationAffectingRegistry(type))
        Reconcile(_questManager?.Quests);
    }

    /// <summary>표시 대상이나 아이콘이 바뀔 수 있는 레지스트리 변경인지 판정한다.</summary>
    private static bool IsPresentationAffectingRegistry(RegistryType type)
      => type == RegistryType.IconSprite
         || type == RegistryType.Npc
         || type == RegistryType.Entity
         || type == RegistryType.Waypoint;

    private void Reconcile(IReadOnlyList<QuestData> quests)
    {
      _interactionBindings.Clear();
      _anchoredBindings.Clear();
      ClearOverheadMarkers();
      var nextIncompleteQuestIds = new HashSet<string>(StringComparer.Ordinal);
      var nextTrackedQuestIds = new HashSet<string>(StringComparer.Ordinal);
      if (quests != null)
      {
        for (int questIndex = 0; questIndex < quests.Count; questIndex++)
        {
          var quest = quests[questIndex];
          if (quest == null || quest.PresentationBindings == null)
            continue;

          string questIdentifier = quest.Id ?? string.Empty;
          if (!quest.Completed)
          {
            nextIncompleteQuestIds.Add(questIdentifier);
            _completionGraceQuestIds.Remove(questIdentifier);
          }
          else if (QuestPreviewHudUIController.HasActiveInstance
                   && _previousIncompleteQuestIds.Contains(questIdentifier)
                   && _previousTrackedQuestIds.Contains(questIdentifier))
          {
            _completionGraceQuestIds.Add(questIdentifier);
          }

          if (quest.IsTracked)
            nextTrackedQuestIds.Add(questIdentifier);

          if (quest.Completed && !_completionGraceQuestIds.Contains(questIdentifier))
            continue;

          for (int bindingIndex = 0; bindingIndex < quest.PresentationBindings.Count; bindingIndex++)
          {
            var binding = quest.PresentationBindings[bindingIndex];
            bool isCompletionGrace = quest.Completed && _completionGraceQuestIds.Contains(questIdentifier);
            if ((!isCompletionGrace && !ShouldActivate(quest, binding))
                || string.IsNullOrWhiteSpace(binding.EntityIdentifier)
                || string.IsNullOrWhiteSpace(binding.IconIdentifier))
              continue;

            if (!Registry.Registry.TryGet<Sprite>(RegistryType.IconSprite, binding.IconIdentifier, out var sprite)
                || sprite == null)
            {
              WarnMissingIcon(binding.IconIdentifier, questIdentifier);
              continue;
            }

            var candidate = new ActiveBinding
            {
              QuestIdentifier = quest.Id ?? string.Empty,
              QuestOrder = questIndex,
              BindingIndex = bindingIndex,
              Binding = binding,
              Icon = sprite
            };

            if (binding.TargetType == QuestPresentationTargetType.Interaction)
            {
              if (string.IsNullOrWhiteSpace(binding.InteractionIdentifier))
                continue;

              var key = new InteractionKey(binding.EntityIdentifier.Trim(), binding.InteractionIdentifier.Trim());
              if (!_interactionBindings.TryGetValue(key, out var current) || IsPreferred(candidate, current))
                _interactionBindings[key] = candidate;
            }
            else if (IsAnchoredTarget(binding.TargetType))
            {
              var key = InteractionKey.ForTarget(binding.TargetType, binding.EntityIdentifier, null);
              if (!_anchoredBindings.TryGetValue(key, out var current) || IsPreferred(candidate, current))
                _anchoredBindings[key] = candidate;
            }
          }
        }
      }
      MergeScenarioMarks();

      foreach (var active in _anchoredBindings.Values)
        RegisterAnchoredMarker(active);

      _previousIncompleteQuestIds.Clear();
      foreach (string questIdentifier in nextIncompleteQuestIds)
        _previousIncompleteQuestIds.Add(questIdentifier);
      _previousTrackedQuestIds.Clear();
      foreach (string questIdentifier in nextTrackedQuestIds)
        _previousTrackedQuestIds.Add(questIdentifier);

      OnPresentationChanged?.Invoke();
      PresentationChanged?.Invoke();
    }

    /// <summary>
    /// 시나리오 그래프가 명시적으로 켠 마크를 퀘스트가 만든 마크와 같은 대상 사전에 합친다.
    /// 그래프 선언은 작성자가 시점을 직접 지정한 것이므로 우선순위가 같으면 퀘스트 마크보다 앞선다.
    /// </summary>
    private void MergeScenarioMarks()
    {
      foreach (var mark in _scenarioMarks.Values)
      {
        if (mark == null || string.IsNullOrWhiteSpace(mark.EntityIdentifier))
          continue;

        if (!Registry.Registry.TryGet<Sprite>(RegistryType.IconSprite, mark.IconIdentifier, out var sprite)
            || sprite == null)
        {
          WarnMissingIcon(mark.IconIdentifier, "(scenario)");
          continue;
        }

        var candidate = new ActiveBinding
        {
          QuestIdentifier = string.Empty,
          // 퀘스트 목록 인덱스보다 앞선 값이라 우선순위가 같을 때 그래프 선언이 이긴다.
          QuestOrder = -1,
          BindingIndex = 0,
          Binding = new QuestPresentationBinding
          {
            Activation = QuestPresentationActivation.WholeQuest,
            TargetType = mark.TargetType,
            EntityIdentifier = mark.EntityIdentifier,
            InteractionIdentifier = mark.InteractionIdentifier,
            IconIdentifier = mark.IconIdentifier,
            Priority = mark.Priority
          },
          Icon = sprite
        };

        if (mark.TargetType == QuestPresentationTargetType.Interaction)
        {
          if (string.IsNullOrWhiteSpace(mark.InteractionIdentifier))
            continue;

          var key = new InteractionKey(mark.EntityIdentifier, mark.InteractionIdentifier);
          if (!_interactionBindings.TryGetValue(key, out var current) || IsPreferred(candidate, current))
            _interactionBindings[key] = candidate;
        }
        else
        {
          var key = InteractionKey.ForTarget(mark.TargetType, mark.EntityIdentifier, null);
          if (!_anchoredBindings.TryGetValue(key, out var current) || IsPreferred(candidate, current))
            _anchoredBindings[key] = candidate;
        }
      }
    }

    /// <summary>
    /// 월드 앵커 위에 마크를 올리는 대상의 앵커를 찾아 표시를 요청한다. 대상 종류별로 다른 것은
    /// 앵커를 찾는 방법뿐이고, 실제 표시는 <see cref="RegisterOverheadMarker"/> 하나로 모여 있다.
    /// </summary>
    private void RegisterAnchoredMarker(ActiveBinding active)
    {
      var binding = active.Binding;
      if (!TryResolveOverheadAnchor(binding.TargetType, binding.EntityIdentifier, out var anchor))
        return;

      RegisterOverheadMarker(
        anchor,
        $"quest:{active.QuestIdentifier}:{active.BindingIndex}",
        OverheadQuestMarkChannelOrder + binding.Priority,
        active.Icon);
    }

    /// <summary>대상 종류별로 마크를 붙일 앵커 Transform 을 찾는다. 표시 자체는 담당하지 않는다.</summary>
    private static bool TryResolveOverheadAnchor(
      QuestPresentationTargetType targetType,
      string entityIdentifier,
      out Transform anchor)
    {
      anchor = null;
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return false;

      string trimmedIdentifier = entityIdentifier.Trim();
      switch (targetType)
      {
        case QuestPresentationTargetType.Npc:
          var npcObject = Registry.Registry.Get<GameObject>(RegistryType.Npc, trimmedIdentifier);
          anchor = FindAnchorProvider(npcObject)?.OverheadPresentationAnchor;
          break;

        case QuestPresentationTargetType.Waypoint:
          if (WaypointAnchor.TryGet(trimmedIdentifier, out var waypoint) && waypoint != null)
            anchor = waypoint.transform;
          break;
      }

      return anchor != null;
    }

    /// <summary>
    /// 앵커 Transform 하나에 마크 아이콘을 올리는 공통 표시 경로. 대상이 NPC 인지 waypoint 인지 알지 못하며,
    /// 표시를 켜는 일만 한다. 퀘스트 진행 판정은 이 경로와 무관하다.
    /// </summary>
    private void RegisterOverheadMarker(Transform anchor, string channel, int channelOrder, Sprite icon)
    {
      var controller = EntityOverheadLabelUIController.ActiveInstance;
      if (anchor == null || controller == null || icon == null)
        return;

      // 채널 스택은 앵커에서 위로 쌓이므로, 이름표(order 0)보다 큰 순서를 써야 이름 위에 마크가 놓인다.
      controller.SetLabel(
        anchor,
        channel,
        channelOrder,
        new EntityOverheadLabelUIController.LabelContent(icon));
      _overheadMarkers.Add((controller, anchor, channel));
    }

    private void ClearOverheadMarkers()
    {
      for (int i = 0; i < _overheadMarkers.Count; i++)
      {
        var marker = _overheadMarkers[i];
        if (marker.Controller != null && marker.Anchor != null)
          marker.Controller.RemoveLabel(marker.Anchor, marker.Channel);
      }

      _overheadMarkers.Clear();
    }

    /// <summary>월드 앵커 위에 아이콘을 띄우는 대상 종류인지 판정한다.</summary>
    private static bool IsAnchoredTarget(QuestPresentationTargetType targetType)
      => targetType == QuestPresentationTargetType.Npc
         || targetType == QuestPresentationTargetType.Waypoint;

    private static bool ShouldActivate(QuestData quest, QuestPresentationBinding binding)
    {
      if (quest == null || binding == null || (!binding.ShowWhenUntracked && !quest.IsTracked))
        return false;

      if (binding.Activation == QuestPresentationActivation.WholeQuest)
        return true;

      if (string.IsNullOrWhiteSpace(binding.CompletionCriteriaIdentifier))
        return false;

      var tasks = quest.Tasks != null && quest.Tasks.Count > 0 ? quest.Tasks : quest.CompletionCriteria;
      if (tasks == null)
        return false;

      if (quest.IsOrdinal)
      {
        var current = FindFirstIncomplete(tasks);
        if (current == null)
          return false;

        if (string.Equals(current.Identifier, binding.CompletionCriteriaIdentifier, StringComparison.Ordinal))
          return true;

        var nested = FindByIdentifier(current.Conditions, binding.CompletionCriteriaIdentifier);
        return nested != null && !nested.Completed;
      }

      var criterion = FindByIdentifier(tasks, binding.CompletionCriteriaIdentifier);
      return criterion != null && !criterion.Completed;
    }

    private static QuestCompletionCriteria FindFirstIncomplete(IReadOnlyList<QuestCompletionCriteria> criteria)
    {
      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion != null && !criterion.Completed)
          return criterion;
      }

      return null;
    }

    private static QuestCompletionCriteria FindByIdentifier(
      IReadOnlyList<QuestCompletionCriteria> criteria,
      string identifier)
    {
      if (criteria == null)
        return null;

      for (int i = 0; i < criteria.Count; i++)
      {
        var criterion = criteria[i];
        if (criterion == null)
          continue;

        if (string.Equals(criterion.Identifier, identifier, StringComparison.Ordinal))
          return criterion;

        var nested = FindByIdentifier(criterion.Conditions, identifier);
        if (nested != null)
          return nested;
      }

      return null;
    }

    private static bool IsPreferred(ActiveBinding candidate, ActiveBinding current)
    {
      if (current == null)
        return true;

      int priority = candidate.Binding.Priority.CompareTo(current.Binding.Priority);
      if (priority != 0)
        return priority > 0;

      if (candidate.QuestOrder != current.QuestOrder)
        return candidate.QuestOrder < current.QuestOrder;

      return string.Compare(candidate.QuestIdentifier, current.QuestIdentifier, StringComparison.Ordinal) < 0;
    }

    private static IOverheadPresentationAnchorProvider FindAnchorProvider(GameObject gameObject)
    {
      if (gameObject == null)
        return null;

      var components = gameObject.GetComponents<MonoBehaviour>();
      for (int i = 0; i < components.Length; i++)
      {
        if (components[i] is IOverheadPresentationAnchorProvider provider)
          return provider;
      }

      return null;
    }

    private void WarnUnaddressableInteraction(IInteract interact)
    {
      if (interact == null)
        return;

      string typeName = interact.GetType().FullName ?? interact.GetType().Name;
      if (_reportedUnaddressableInteractions.Add(typeName))
        WarnUnaddressableInteractionType(typeName);
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void WarnUnaddressableInteractionType(string typeName)
    {
      Debug.LogWarning(
        $"[QuestPresentation] Interaction type '{typeName}' has no quest presentation address and cannot receive a quest icon override.");
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private static void WarnMissingIcon(string iconIdentifier, string questIdentifier)
    {
      Debug.LogWarning(
        $"[QuestPresentation] Icon sprite '{iconIdentifier}' for quest '{questIdentifier}' is not registered.");
    }
  }
}
