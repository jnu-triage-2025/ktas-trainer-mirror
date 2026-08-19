using System;
using System.Collections.Generic;
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

    public static QuestPresentationService ActiveInstance { get; private set; }

    public event Action OnPresentationChanged;
    public static event Action PresentationChanged;

    private readonly Dictionary<InteractionKey, ActiveBinding> _interactionBindings = new();
    private readonly Dictionary<string, ActiveBinding> _npcBindings = new(StringComparer.Ordinal);
    private readonly List<(EntityOverheadLabelUIController Controller, Transform Anchor, string Channel)> _npcMarkers = new();
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
      _npcBindings.Clear();
      ClearNpcMarkers();
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
      _interactionBindings.Clear();
      _npcBindings.Clear();
      ClearNpcMarkers();
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
      if (type == RegistryType.IconSprite || type == RegistryType.Npc || type == RegistryType.Entity)
        Reconcile(_questManager?.Quests);
    }

    private void HandleRegistryEntryRemoved(RegistryType type, string identifier)
    {
      if (type == RegistryType.IconSprite || type == RegistryType.Npc || type == RegistryType.Entity)
        Reconcile(_questManager?.Quests);
    }

    private void Reconcile(IReadOnlyList<QuestData> quests)
    {
      _interactionBindings.Clear();
      _npcBindings.Clear();
      ClearNpcMarkers();
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
            else if (binding.TargetType == QuestPresentationTargetType.Npc)
            {
              string npcKey = binding.EntityIdentifier.Trim();
              if (!_npcBindings.TryGetValue(npcKey, out var current) || IsPreferred(candidate, current))
                _npcBindings[npcKey] = candidate;
            }
          }
        }
      }
      foreach (var active in _npcBindings.Values)
        RegisterNpcMarker(active);

      _previousIncompleteQuestIds.Clear();
      foreach (string questIdentifier in nextIncompleteQuestIds)
        _previousIncompleteQuestIds.Add(questIdentifier);
      _previousTrackedQuestIds.Clear();
      foreach (string questIdentifier in nextTrackedQuestIds)
        _previousTrackedQuestIds.Add(questIdentifier);

      OnPresentationChanged?.Invoke();
      PresentationChanged?.Invoke();
    }

    private void RegisterNpcMarker(ActiveBinding active)
    {
      var binding = active.Binding;
      var npcObject = Registry.Registry.Get<GameObject>(RegistryType.Npc, binding.EntityIdentifier);
      var anchorProvider = FindAnchorProvider(npcObject);
      var anchor = anchorProvider?.OverheadPresentationAnchor;
      var controller = EntityOverheadLabelUIController.ActiveInstance;
      if (anchor == null || controller == null)
        return;

      string channel = $"quest:{active.QuestIdentifier}:{active.BindingIndex}";
      controller.SetLabel(anchor, channel, 100 + binding.Priority, new EntityOverheadLabelUIController.LabelContent(active.Icon));
      _npcMarkers.Add((controller, anchor, channel));
    }

    private void ClearNpcMarkers()
    {
      for (int i = 0; i < _npcMarkers.Count; i++)
      {
        var marker = _npcMarkers[i];
        if (marker.Controller != null && marker.Anchor != null)
          marker.Controller.RemoveLabel(marker.Anchor, marker.Channel);
      }

      _npcMarkers.Clear();
    }

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
