using System;
using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>조건 판정에 쓰는 관찰자와 실행 맥락.</summary>
  public readonly struct ScenarioConditionContext
  {
    /// <summary>관찰자 플레이어(로컬에 있을 때). 없으면 <see cref="PlayerIdentifier"/> 로만 판정한다.</summary>
    public PlayerController Player { get; }

    /// <summary>관찰자의 UserDescriptor.Identifier. 플레이어 컴포넌트가 없어도 태그·플래그 판정이 가능하다.</summary>
    public string PlayerIdentifier { get; }

    public ScenarioConditionContext(PlayerController player, string playerIdentifier)
    {
      Player = player;
      PlayerIdentifier = !string.IsNullOrWhiteSpace(playerIdentifier)
        ? playerIdentifier.Trim()
        : (player != null ? player.UserIdentifier : null);
    }

    public static ScenarioConditionContext ForPlayer(PlayerController player)
      => new ScenarioConditionContext(player, player != null ? player.UserIdentifier : null);

    public static ScenarioConditionContext ForPlayerIdentifier(string playerIdentifier)
    {
      PlayerController player = null;
      if (!string.IsNullOrWhiteSpace(playerIdentifier)
          && Registry.Registry.TryGetEntityByOwnerUserIdentifier(playerIdentifier, out var descriptor)
          && descriptor?.GameObject != null)
      {
        descriptor.GameObject.TryGetComponent(out player);
      }
      return new ScenarioConditionContext(player, playerIdentifier);
    }

    public static ScenarioConditionContext Global => new ScenarioConditionContext(null, null);

    public bool HasPlayer => Player != null || !string.IsNullOrWhiteSpace(PlayerIdentifier);
  }

  /// <summary>
  /// <see cref="ScenarioCondition"/> 목록을 판정하는 단일 엔진. 인터렉션 가시성과 Validator 게이트가 함께 쓴다.
  /// 판정 입력은 모두 서버 권위로 복제되는 값이어야 하며, 이 클래스는 로컬 레지스트리와 서비스만 읽는다.
  /// </summary>
  public static class ScenarioConditionEvaluator
  {
    private static readonly List<GameObject> EntityScratch = new List<GameObject>();

    public static bool Evaluate(
      IReadOnlyList<ScenarioCondition> conditions,
      ScenarioConditionMatchMode matchMode,
      in ScenarioConditionContext context)
      => Evaluate(conditions, matchMode, context, out _);

    /// <summary>조건 목록을 판정한다. 목록이 비어 있으면 true 다.</summary>
    public static bool Evaluate(
      IReadOnlyList<ScenarioCondition> conditions,
      ScenarioConditionMatchMode matchMode,
      in ScenarioConditionContext context,
      out string failureReason)
    {
      failureReason = null;
      if (conditions == null || conditions.Count == 0)
        return true;

      bool anyMode = matchMode == ScenarioConditionMatchMode.Any;
      List<string> anyFailures = anyMode ? new List<string>() : null;
      int evaluated = 0;
      for (int i = 0; i < conditions.Count; i++)
      {
        var condition = conditions[i];
        if (condition == null)
          continue;
        evaluated++;

        bool passed = EvaluateOne(condition, context, out string reason);
        if (anyMode)
        {
          if (passed)
            return true;
          anyFailures.Add(reason ?? $"condition[{i}] failed.");
        }
        else if (!passed)
        {
          failureReason = reason ?? $"condition[{i}] failed.";
          return false;
        }
      }

      if (anyMode && evaluated > 0)
      {
        failureReason = "no condition matched (Any): " + string.Join(" | ", anyFailures);
        return false;
      }

      return true;
    }

    public static bool EvaluateOne(ScenarioCondition condition, in ScenarioConditionContext context, out string failureReason)
    {
      bool result = EvaluateCore(condition, context, out failureReason);
      if (condition.Negate)
      {
        result = !result;
        failureReason = result ? null : $"{condition.Type} matched but negate=true.";
      }
      return result;
    }

    private static bool EvaluateCore(ScenarioCondition condition, in ScenarioConditionContext context, out string reason)
    {
      reason = null;
      switch (condition.Type)
      {
        case ScenarioConditionType.PlayerHasTag:
        {
          if (!TryRequirePlayerIdentifier(condition, context, out string playerId, out reason))
            return false;
          string tag = condition.Tag?.Trim();
          if (string.IsNullOrWhiteSpace(tag))
          {
            reason = "PlayerHasTag: tag is empty.";
            return false;
          }
          if (PlayerTagService.HasTagOnIdentifier(playerId, tag))
            return true;
          reason = $"PlayerHasTag: player '{playerId}' has no tag '{tag}'.";
          return false;
        }
        case ScenarioConditionType.PlayerHasQuestFlag:
        {
          if (!TryRequirePlayerIdentifier(condition, context, out string playerId, out reason))
            return false;
          string flag = condition.Flag?.Trim();
          if (string.IsNullOrWhiteSpace(flag))
          {
            reason = "PlayerHasQuestFlag: flag is empty.";
            return false;
          }
          if (PlayerQuestStateFlagService.Has(playerId, flag))
            return true;
          reason = $"PlayerHasQuestFlag: player '{playerId}' has no flag '{flag}'.";
          return false;
        }
        case ScenarioConditionType.PlayerHasQuest:
          return EvaluateQuest(condition, context, out reason);
        case ScenarioConditionType.PlayerHasItem:
        {
          if (!TryRequirePlayer(condition, context, out var player, out reason))
            return false;
          string item = condition.ItemIdentifier?.Trim();
          if (string.IsNullOrWhiteSpace(item))
          {
            reason = "PlayerHasItem: itemIdentifier is empty.";
            return false;
          }
          int required = Math.Max(1, condition.Count);
          int count = player.CountItemInInventory(item);
          if (count >= required)
            return true;
          reason = $"PlayerHasItem: player holds {count} of '{item}', requires {required}.";
          return false;
        }
        case ScenarioConditionType.PlayerState:
        {
          if (!TryRequirePlayer(condition, context, out var player, out reason))
            return false;
          return EvaluateProviderState(player.gameObject, condition, "PlayerState", out reason);
        }
        case ScenarioConditionType.PlayerWithinDistance:
        {
          if (!TryRequirePlayer(condition, context, out var player, out reason))
            return false;
          ResolveEntities(condition.Entity, EntityScratch);
          if (EntityScratch.Count == 0)
          {
            reason = $"PlayerWithinDistance: entity {condition.Entity} not found.";
            return false;
          }
          float limit = Mathf.Max(0f, condition.Meters);
          float nearest = float.PositiveInfinity;
          for (int i = 0; i < EntityScratch.Count; i++)
          {
            float distance = Vector3.Distance(player.transform.position, EntityScratch[i].transform.position);
            if (distance < nearest)
              nearest = distance;
          }
          if (nearest <= limit)
            return true;
          reason = $"PlayerWithinDistance: nearest {nearest:F2}m exceeds {limit:F2}m.";
          return false;
        }
        case ScenarioConditionType.SignalRaised:
        {
          string signal = condition.Signal?.Trim();
          if (string.IsNullOrWhiteSpace(signal))
          {
            reason = "SignalRaised: signal is empty.";
            return false;
          }
          if (ScenarioInteractionSignals.IsRaised(signal))
            return true;
          reason = $"SignalRaised: '{ScenarioInteractionSignals.Normalize(signal)}' is not raised.";
          return false;
        }
        case ScenarioConditionType.RegistryContains:
        {
          string identifier = condition.Identifier?.Trim();
          if (string.IsNullOrWhiteSpace(identifier))
          {
            reason = "RegistryContains: identifier is empty.";
            return false;
          }
          if (Registry.Registry.Contains(condition.RegistryType, identifier))
            return true;
          reason = $"RegistryContains: '{identifier}' is not registered in {condition.RegistryType}.";
          return false;
        }
        case ScenarioConditionType.EntityHasTag:
        {
          string tag = condition.Tag?.Trim();
          if (string.IsNullOrWhiteSpace(tag))
          {
            reason = "EntityHasTag: tag is empty.";
            return false;
          }
          if (condition.Entity == null || condition.Entity.IsEmpty)
          {
            reason = "EntityHasTag: entity is empty.";
            return false;
          }
          if (!string.IsNullOrWhiteSpace(condition.Entity.Identifier))
          {
            if (PlayerTagService.HasTagOnIdentifier(condition.Entity.Identifier.Trim(), tag))
              return true;
            reason = $"EntityHasTag: entity '{condition.Entity.Identifier}' has no tag '{tag}'.";
            return false;
          }
          // 태그 참조: 참조 태그를 가진 엔티티 가운데 하나라도 대상 태그를 가지면 참이다.
          foreach (var pair in Registry.Registry.GetAllEntities())
          {
            if (condition.Entity.Matches(pair.Key) && PlayerTagService.HasTagOnIdentifier(pair.Key, tag))
              return true;
          }
          reason = $"EntityHasTag: no entity {condition.Entity} has tag '{tag}'.";
          return false;
        }
        case ScenarioConditionType.EntityState:
        {
          ResolveEntities(condition.Entity, EntityScratch);
          if (EntityScratch.Count == 0)
          {
            reason = $"EntityState: entity {condition.Entity} not found.";
            return false;
          }
          string lastReason = null;
          for (int i = 0; i < EntityScratch.Count; i++)
          {
            if (EvaluateProviderState(EntityScratch[i], condition, "EntityState", out lastReason))
              return true;
          }
          reason = lastReason;
          return false;
        }
        case ScenarioConditionType.PlayerCount:
        {
          int count = CountPlayers(condition.Tag);
          var value = ConditionValue.From(count);
          if (value.Satisfies(condition.Compare, condition.Count.ToString()))
            return true;
          reason = $"PlayerCount: {count} does not satisfy {condition.Compare} {condition.Count}"
                   + (string.IsNullOrWhiteSpace(condition.Tag) ? "." : $" (tag '{condition.Tag}').");
          return false;
        }
        case ScenarioConditionType.ScenarioActive:
        {
          string active = ScenarioController.Instance != null ? ScenarioController.Instance.CurrentGraph?.Identifier : null;
          string expected = condition.ScenarioIdentifier?.Trim();
          if (string.IsNullOrWhiteSpace(expected))
          {
            if (!string.IsNullOrWhiteSpace(active))
              return true;
            reason = "ScenarioActive: no scenario is running.";
            return false;
          }
          if (string.Equals(active, expected, StringComparison.Ordinal))
            return true;
          reason = $"ScenarioActive: running '{active ?? "(none)"}', expected '{expected}'.";
          return false;
        }
        case ScenarioConditionType.Group:
          return Evaluate(condition.Conditions, condition.MatchMode, context, out reason);
        default:
          reason = $"unsupported condition type '{condition.Type}'.";
          return false;
      }
    }

    private static bool EvaluateQuest(ScenarioCondition condition, in ScenarioConditionContext context, out string reason)
    {
      reason = null;
      string questIdentifier = condition.QuestIdentifier?.Trim();
      if (string.IsNullOrWhiteSpace(questIdentifier))
      {
        reason = "PlayerHasQuest: questIdentifier is empty.";
        return false;
      }

      // 퀘스트 목록은 피어별 QuestManager 가 가진다. 플레이어 범위 퀘스트는 소유 클라이언트에만 존재하므로
      // 원격 플레이어를 관찰자로 판정할 때는 그 플레이어의 퀘스트를 알 수 없다. 이 판정은 로컬 관찰자 기준이다.
      var questManager = ResolveQuestManager();
      QuestData quest = null;
      if (questManager != null)
      {
        var quests = questManager.Quests;
        for (int i = 0; i < quests.Count; i++)
        {
          var each = quests[i];
          if (each == null || each.IsGroupWaitPlaceholder)
            continue;
          if (string.Equals(each.Id, questIdentifier, StringComparison.Ordinal)
              || string.Equals(each.DefinitionIdentifier, questIdentifier, StringComparison.Ordinal))
          {
            quest = each;
            break;
          }
        }
      }

      switch (condition.QuestState)
      {
        case ScenarioQuestConditionState.Absent:
          if (quest == null)
            return true;
          reason = $"PlayerHasQuest: quest '{questIdentifier}' is present.";
          return false;
        case ScenarioQuestConditionState.Completed:
          if (quest != null && quest.Completed)
            return true;
          reason = quest == null
            ? $"PlayerHasQuest: quest '{questIdentifier}' is absent."
            : $"PlayerHasQuest: quest '{questIdentifier}' is not completed.";
          return false;
        default:
        {
          if (quest == null)
          {
            reason = $"PlayerHasQuest: quest '{questIdentifier}' is absent.";
            return false;
          }
          if (quest.Completed)
          {
            reason = $"PlayerHasQuest: quest '{questIdentifier}' is already completed.";
            return false;
          }
          string criteria = condition.CompletionCriteriaIdentifier?.Trim();
          if (string.IsNullOrWhiteSpace(criteria))
            return true;
          if (IsCriteriaCurrent(quest, criteria))
            return true;
          reason = $"PlayerHasQuest: criteria '{criteria}' of quest '{questIdentifier}' is not the current target.";
          return false;
        }
      }
    }

    /// <summary>퀘스트 표시 서비스와 같은 규칙으로 완료 목표가 현재 진행 대상인지 판정한다.</summary>
    public static bool IsCriteriaCurrent(QuestData quest, string criteriaIdentifier)
    {
      if (quest == null || string.IsNullOrWhiteSpace(criteriaIdentifier))
        return false;

      var tasks = quest.Tasks != null && quest.Tasks.Count > 0 ? quest.Tasks : quest.CompletionCriteria;
      if (tasks == null)
        return false;

      if (quest.IsOrdinal)
      {
        QuestCompletionCriteria current = null;
        for (int i = 0; i < tasks.Count; i++)
        {
          if (tasks[i] != null && !tasks[i].Completed)
          {
            current = tasks[i];
            break;
          }
        }
        if (current == null)
          return false;
        if (string.Equals(current.Identifier, criteriaIdentifier, StringComparison.Ordinal))
          return true;
        var nested = FindCriteria(current.Conditions, criteriaIdentifier);
        return nested != null && !nested.Completed;
      }

      var criterion = FindCriteria(tasks, criteriaIdentifier);
      return criterion != null && !criterion.Completed;
    }

    private static QuestCompletionCriteria FindCriteria(IReadOnlyList<QuestCompletionCriteria> criteria, string identifier)
    {
      if (criteria == null)
        return null;
      for (int i = 0; i < criteria.Count; i++)
      {
        var each = criteria[i];
        if (each == null)
          continue;
        if (string.Equals(each.Identifier, identifier, StringComparison.Ordinal))
          return each;
        var nested = FindCriteria(each.Conditions, identifier);
        if (nested != null)
          return nested;
      }
      return null;
    }

    private static bool EvaluateProviderState(GameObject target, ScenarioCondition condition, string label, out string reason)
    {
      reason = null;
      string key = condition.Key?.Trim();
      if (string.IsNullOrWhiteSpace(key))
      {
        reason = $"{label}: key is empty.";
        return false;
      }
      if (target == null)
      {
        reason = $"{label}: target is missing.";
        return false;
      }

      var providers = target.GetComponentsInChildren<IConditionStateProvider>(true);
      for (int i = 0; i < providers.Length; i++)
      {
        if (providers[i] == null)
          continue;
        if (!providers[i].TryGetConditionValue(key, condition.Qualifier?.Trim(), out var value))
          continue;
        if (value.Satisfies(condition.Compare, condition.Value))
          return true;
        reason = $"{label}: '{key}'{FormatQualifier(condition.Qualifier)} is '{value}', expected {condition.Compare} '{condition.Value ?? "true"}'.";
        return false;
      }

      reason = $"{label}: no provider on '{target.name}' knows key '{key}'.";
      return false;
    }

    private static string FormatQualifier(string qualifier)
      => string.IsNullOrWhiteSpace(qualifier) ? string.Empty : $"[{qualifier.Trim()}]";

    private static bool TryRequirePlayerIdentifier(ScenarioCondition condition, in ScenarioConditionContext context,
      out string playerIdentifier, out string reason)
    {
      playerIdentifier = context.PlayerIdentifier;
      reason = null;
      if (!string.IsNullOrWhiteSpace(playerIdentifier))
        return true;
      reason = $"{condition.Type}: no observer player in context.";
      return false;
    }

    private static bool TryRequirePlayer(ScenarioCondition condition, in ScenarioConditionContext context,
      out PlayerController player, out string reason)
    {
      player = context.Player;
      reason = null;
      if (player != null)
        return true;
      reason = $"{condition.Type}: observer player component is unavailable" +
               (string.IsNullOrWhiteSpace(context.PlayerIdentifier) ? "." : $" for '{context.PlayerIdentifier}'.");
      return false;
    }

    /// <summary>엔티티 참조를 현재 등록된 GameObject 목록으로 푼다.</summary>
    public static void ResolveEntities(ScenarioEntityReference reference, List<GameObject> results)
    {
      results.Clear();
      if (reference == null || reference.IsEmpty)
        return;

      if (!string.IsNullOrWhiteSpace(reference.Identifier))
      {
        if (Registry.Registry.TryGetEntity(reference.Identifier.Trim(), out var descriptor) && descriptor?.GameObject != null)
          results.Add(descriptor.GameObject);
        return;
      }

      string tag = reference.Tag.Trim();
      foreach (var pair in Registry.Registry.GetAllEntities())
      {
        if (pair.Value?.GameObject == null)
          continue;
        if (PlayerTagService.HasTagOnIdentifier(pair.Key, tag))
          results.Add(pair.Value.GameObject);
      }
    }

    private static int CountPlayers(string tag)
    {
      var users = UserDescriptorService.GetAll();
      string normalizedTag = tag?.Trim();
      if (!string.IsNullOrWhiteSpace(normalizedTag))
      {
        int tagged = 0;
        if (users != null)
        {
          foreach (var pair in users)
          {
            var descriptor = pair.Value;
            if (descriptor != null && !string.IsNullOrWhiteSpace(descriptor.Identifier)
                && PlayerTagService.HasTagOnIdentifier(descriptor.Identifier, normalizedTag))
              tagged++;
          }
        }
        return tagged;
      }

      // Validator 노드와 같은 기준: 서버 구동 중에는 서버 접속자 수, 아니면 클라이언트가 아는 접속자 수.
      try
      {
        if (InstanceFinder.IsServerStarted)
          return InstanceFinder.ServerManager?.Clients?.Count ?? users?.Count ?? 0;
        if (InstanceFinder.IsClientStarted)
          return InstanceFinder.ClientManager?.Clients?.Count ?? users?.Count ?? 0;
      }
      catch (Exception)
      {
        // 네트워크 매니저가 없는 테스트 환경에서는 등록된 사용자 수로 대신한다.
      }
      return users?.Count ?? 0;
    }

    private static QuestManager ResolveQuestManager()
    {
      var manager = Registry.Registry.Get<QuestManager>(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());
      if (manager != null)
        return manager;
      return UnityEngine.Object.FindFirstObjectByType<QuestManager>();
    }
  }
}
