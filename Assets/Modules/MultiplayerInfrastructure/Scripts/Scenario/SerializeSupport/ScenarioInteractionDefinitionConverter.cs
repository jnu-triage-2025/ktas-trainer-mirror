using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 조건 절, 인터렉션 정의, 가시성 노드의 DTO 와 도메인 모델 사이 변환. <see cref="ScenarioGraphLoader"/> 가 호출한다.
  /// </summary>
  internal static class ScenarioInteractionDefinitionConverter
  {
    // ── 엔티티 참조 ─────────────────────────────────────────────────────

    public static ScenarioEntityReference ConvertEntity(ScenarioEntityReferenceDTO dto, string context)
    {
      if (dto == null)
        return new ScenarioEntityReference();
      var reference = new ScenarioEntityReference { Identifier = dto.Identifier?.Trim(), Tag = dto.Tag?.Trim() };
      if (!string.IsNullOrWhiteSpace(reference.Identifier) && !string.IsNullOrWhiteSpace(reference.Tag))
        throw new JsonException($"{context}: entity reference must specify either 'id' or 'tag', not both.");
      return reference;
    }

    public static ScenarioEntityReferenceDTO ConvertEntityToDTO(ScenarioEntityReference reference)
    {
      if (reference == null || reference.IsEmpty)
        return null;
      return new ScenarioEntityReferenceDTO
      {
        Identifier = NullIfWhiteSpace(reference.Identifier),
        Tag = NullIfWhiteSpace(reference.Tag)
      };
    }

    // ── 조건 절 ─────────────────────────────────────────────────────────

    public static List<ScenarioCondition> ConvertConditions(IEnumerable<ScenarioConditionDTO> dtos, string context)
    {
      var result = new List<ScenarioCondition>();
      if (dtos == null)
        return result;
      int index = 0;
      foreach (var dto in dtos)
      {
        if (dto == null)
        {
          index++;
          continue;
        }
        result.Add(ConvertCondition(dto, $"{context}.conditions[{index}]"));
        index++;
      }
      return result;
    }

    public static ScenarioCondition ConvertCondition(ScenarioConditionDTO dto, string context)
    {
      if (dto == null)
        throw new JsonException($"{context}: condition is null.");
      if (!Enum.TryParse(dto.Type, true, out ScenarioConditionType type))
        throw new JsonException($"{context}: unknown condition type '{dto.Type}'.");

      var condition = new ScenarioCondition
      {
        Type = type,
        Negate = dto.Negate ?? false,
        Tag = dto.Tag?.Trim(),
        Flag = dto.Flag?.Trim(),
        QuestIdentifier = dto.QuestIdentifier?.Trim(),
        QuestState = ParseEnum(dto.QuestState, ScenarioQuestConditionState.Active, context, "questState"),
        CompletionCriteriaIdentifier = dto.CompletionCriteriaIdentifier?.Trim(),
        ItemIdentifier = dto.ItemIdentifier?.Trim(),
        Count = dto.Count ?? 1,
        Key = dto.Key?.Trim(),
        Qualifier = dto.Qualifier?.Trim(),
        Compare = ParseEnum(dto.Compare, ScenarioConditionCompare.Equal, context, "compare"),
        Value = dto.Value,
        Entity = dto.Entity != null ? ConvertEntity(dto.Entity, context) : null,
        Meters = dto.Meters ?? 3f,
        Signal = dto.Signal?.Trim(),
        RegistryType = ParseEnum(dto.RegistryType, RegistryType.RuntimeState, context, "registryType"),
        Identifier = dto.Identifier?.Trim(),
        ScenarioIdentifier = dto.ScenarioIdentifier?.Trim(),
        MatchMode = ParseEnum(dto.MatchMode, ScenarioConditionMatchMode.All, context, "matchMode"),
        Conditions = ConvertConditions(dto.Conditions, context)
      };

      ValidateCondition(condition, context);
      return condition;
    }

    private static void ValidateCondition(ScenarioCondition condition, string context)
    {
      switch (condition.Type)
      {
        case ScenarioConditionType.PlayerHasTag:
        case ScenarioConditionType.EntityHasTag:
          Require(condition.Tag, context, "tag");
          if (condition.Type == ScenarioConditionType.EntityHasTag)
            RequireEntity(condition.Entity, context);
          break;
        case ScenarioConditionType.PlayerHasQuestFlag:
          Require(condition.Flag, context, "flag");
          break;
        case ScenarioConditionType.PlayerHasQuest:
          Require(condition.QuestIdentifier, context, "questIdentifier");
          break;
        case ScenarioConditionType.PlayerHasItem:
          Require(condition.ItemIdentifier, context, "itemIdentifier");
          break;
        case ScenarioConditionType.PlayerState:
          Require(condition.Key, context, "key");
          break;
        case ScenarioConditionType.EntityState:
          Require(condition.Key, context, "key");
          RequireEntity(condition.Entity, context);
          break;
        case ScenarioConditionType.PlayerWithinDistance:
          RequireEntity(condition.Entity, context);
          break;
        case ScenarioConditionType.SignalRaised:
          Require(condition.Signal, context, "signal");
          break;
        case ScenarioConditionType.RegistryContains:
          Require(condition.Identifier, context, "identifier");
          break;
        case ScenarioConditionType.Group:
          if (condition.Conditions == null || condition.Conditions.Count == 0)
            throw new JsonException($"{context}: Group condition requires at least one child condition.");
          break;
      }
    }

    public static List<ScenarioConditionDTO> ConvertConditionsToDTO(IReadOnlyList<ScenarioCondition> conditions)
    {
      if (conditions == null || conditions.Count == 0)
        return null;
      return conditions.Where(value => value != null).Select(ConvertConditionToDTO).ToList();
    }

    public static ScenarioConditionDTO ConvertConditionToDTO(ScenarioCondition condition)
    {
      var dto = new ScenarioConditionDTO
      {
        Type = condition.Type.ToString(),
        Negate = condition.Negate ? true : (bool?)null,
        Entity = ConvertEntityToDTO(condition.Entity)
      };

      switch (condition.Type)
      {
        case ScenarioConditionType.PlayerHasTag:
        case ScenarioConditionType.EntityHasTag:
          dto.Tag = NullIfWhiteSpace(condition.Tag);
          break;
        case ScenarioConditionType.PlayerHasQuestFlag:
          dto.Flag = NullIfWhiteSpace(condition.Flag);
          break;
        case ScenarioConditionType.PlayerHasQuest:
          dto.QuestIdentifier = NullIfWhiteSpace(condition.QuestIdentifier);
          dto.QuestState = condition.QuestState != ScenarioQuestConditionState.Active ? condition.QuestState.ToString() : null;
          dto.CompletionCriteriaIdentifier = NullIfWhiteSpace(condition.CompletionCriteriaIdentifier);
          break;
        case ScenarioConditionType.PlayerHasItem:
          dto.ItemIdentifier = NullIfWhiteSpace(condition.ItemIdentifier);
          dto.Count = condition.Count != 1 ? condition.Count : (int?)null;
          break;
        case ScenarioConditionType.PlayerState:
        case ScenarioConditionType.EntityState:
          dto.Key = NullIfWhiteSpace(condition.Key);
          dto.Qualifier = NullIfWhiteSpace(condition.Qualifier);
          dto.Compare = condition.Compare != ScenarioConditionCompare.Equal ? condition.Compare.ToString() : null;
          dto.Value = condition.Value;
          break;
        case ScenarioConditionType.PlayerWithinDistance:
          dto.Meters = condition.Meters;
          break;
        case ScenarioConditionType.SignalRaised:
          dto.Signal = NullIfWhiteSpace(condition.Signal);
          break;
        case ScenarioConditionType.RegistryContains:
          dto.RegistryType = condition.RegistryType.ToString();
          dto.Identifier = NullIfWhiteSpace(condition.Identifier);
          break;
        case ScenarioConditionType.PlayerCount:
          dto.Tag = NullIfWhiteSpace(condition.Tag);
          dto.Compare = condition.Compare.ToString();
          dto.Count = condition.Count;
          break;
        case ScenarioConditionType.ScenarioActive:
          dto.ScenarioIdentifier = NullIfWhiteSpace(condition.ScenarioIdentifier);
          break;
        case ScenarioConditionType.Group:
          dto.MatchMode = condition.MatchMode.ToString();
          dto.Conditions = ConvertConditionsToDTO(condition.Conditions);
          break;
      }

      return dto;
    }

    // ── 인터렉션 정의 ───────────────────────────────────────────────────

    /// <summary>
    /// 최상위 interactions 구역과, 폐기 예정인 actingNpcs[].interactions 를 하나의 정의 목록으로 합친다.
    /// 옛 형식은 같은 NPC 식별자를 가진 정의로 변환하고 에디터에서 경고한다. 변환된 actingNpc 의 목록은 비운다.
    /// </summary>
    public static IReadOnlyList<InteractionDefinition> ConvertInteractions(
      string graphIdentifier,
      IEnumerable<ScenarioInteractionDefinitionDTO> dtos,
      IReadOnlyList<ScenarioActingNpcDefinition> actingNpcs)
    {
      var result = new List<InteractionDefinition>();
      var addresses = new HashSet<string>(StringComparer.Ordinal);

      if (dtos != null)
      {
        int index = 0;
        foreach (var dto in dtos)
        {
          if (dto == null)
          {
            index++;
            continue;
          }
          var definition = ConvertInteraction(dto, $"interactions[{index}]");
          AddUnique(result, addresses, definition, $"interactions[{index}]");
          index++;
        }
      }

      if (actingNpcs != null)
      {
        var converted = new List<string>();
        foreach (var actingNpc in actingNpcs)
        {
          if (actingNpc?.Interactions == null || actingNpc.Interactions.Count == 0)
            continue;
          foreach (var legacy in actingNpc.Interactions)
          {
            if (legacy == null)
              continue;
            var definition = ConvertLegacyActingNpcInteraction(actingNpc.Identifier, legacy);
            AddUnique(result, addresses, definition, $"actingNpcs['{actingNpc.Identifier}'].interactions['{legacy.Identifier}']");
            converted.Add($"{actingNpc.Identifier}/{legacy.Identifier}");
          }
          actingNpc.Interactions = Array.Empty<ScenarioActingNpcInteractionDefinition>();
        }
        if (converted.Count > 0 && Application.isEditor)
        {
          Debug.LogWarning(
            $"[ScenarioGraphLoader] Scenario '{graphIdentifier}' declares deprecated actingNpcs[].interactions; " +
            $"converted to top-level 'interactions': [{string.Join(", ", converted)}]. Save the graph to migrate.");
        }
      }

      return result;
    }

    private static void AddUnique(List<InteractionDefinition> result, HashSet<string> addresses,
      InteractionDefinition definition, string context)
    {
      string key = (definition.Entity.IsTagReference ? "tag:" + definition.Entity.Tag : "id:" + definition.Entity.Identifier)
                   + "/" + definition.InteractionIdentifier;
      if (!addresses.Add(key))
        throw new JsonException($"{context}: interaction address '{key}' is duplicated.");
      result.Add(definition);
    }

    public static InteractionDefinition ConvertInteraction(ScenarioInteractionDefinitionDTO dto, string context)
    {
      var entity = ConvertEntity(dto.Entity, context);
      if (entity.IsEmpty)
        throw new JsonException($"{context}: 'entity' must specify 'id' or 'tag'.");
      string interaction = dto.Interaction?.Trim();
      if (string.IsNullOrWhiteSpace(interaction))
        throw new JsonException($"{context}: 'interaction' is required.");

      var definition = new InteractionDefinition
      {
        Entity = entity,
        InteractionIdentifier = interaction,
        HandlerKey = dto.HandlerKey?.Trim(),
        CompletionSignal = dto.CompletionSignal?.Trim(),
        ScenarioIdentifier = dto.ScenarioIdentifier?.Trim(),
        StartNodeIdentifier = dto.StartNodeIdentifier?.Trim(),
        ActivateObjects = dto.ActivateObjects?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? new List<string>(),
        DeactivateObjects = dto.DeactivateObjects?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? new List<string>()
      };

      if (!string.IsNullOrWhiteSpace(dto.Kind))
      {
        definition.Kind = ParseEnum(dto.Kind, InteractionKind.Custom, context, "kind");
        definition.KindSpecified = true;
      }

      if (dto.Display != null)
      {
        definition.Display = new InteractionDisplay
        {
          Text = dto.Display.Text,
          IconIdentifiers = dto.Display.IconIdentifiers?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? new List<string>(),
          Color = dto.Display.Color != null ? new Color(dto.Display.Color.R, dto.Display.Color.G, dto.Display.Color.B, dto.Display.Color.A) : (Color?)null,
          AllowIconFallback = dto.Display.AllowIconFallback ?? true,
          Priority = dto.Display.Priority ?? 0,
          PrioritySpecified = dto.Display.Priority.HasValue
        };
      }

      if (!string.IsNullOrWhiteSpace(dto.AfterInteract))
      {
        definition.AfterInteract = ParseEnum(dto.AfterInteract, InteractionAfterInteract.None, context, "afterInteract");
        definition.AfterInteractSpecified = true;
      }

      definition.RequiredItems = ConvertRequirements(dto.RequiredItems, context, "requiredItems");
      definition.ConsumeItems = ConvertRequirements(dto.ConsumeItems, context, "consumeItems");
      if (dto.Extras != null)
      {
        foreach (var pair in dto.Extras)
        {
          if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null)
            definition.Extras[pair.Key.Trim()] = pair.Value;
        }
      }

      if (dto.ItemSubmission != null)
      {
        definition.Submission = new InteractionSubmissionSettings
        {
          Title = dto.ItemSubmission.Title,
          SubmitButtonText = dto.ItemSubmission.SubmitButtonText,
          RequiredItems = ConvertRequirements(dto.ItemSubmission.RequiredItems, context, "itemSubmission.requiredItems")
        };
      }

      if (dto.Visibility != null)
      {
        if (dto.Visibility.Initial.HasValue)
        {
          definition.InitialVisible = dto.Visibility.Initial.Value;
          definition.InitialVisibleSpecified = true;
        }
        definition.MatchMode = ParseEnum(dto.Visibility.MatchMode, ScenarioConditionMatchMode.All, context, "visibility.matchMode");
        definition.VisibilityConditions = ConvertConditions(dto.Visibility.Conditions, context + ".visibility");
      }

      ValidateDefinition(definition, context);
      return definition;
    }

    private static void ValidateDefinition(InteractionDefinition definition, string context)
    {
      switch (definition.Kind)
      {
        case InteractionKind.StartScenario:
          if (string.IsNullOrWhiteSpace(definition.ScenarioIdentifier))
            throw new JsonException($"{context}: StartScenario kind requires 'scenarioIdentifier'.");
          break;
        case InteractionKind.ItemSubmission:
          if (definition.Submission == null || definition.Submission.RequiredItems == null
              || definition.Submission.RequiredItems.Count == 0)
            throw new JsonException($"{context}: ItemSubmission kind requires 'itemSubmission.requiredItems'.");
          break;
        case InteractionKind.Signal:
          if (string.IsNullOrWhiteSpace(definition.CompletionSignal))
            throw new JsonException($"{context}: Signal kind requires 'completionSignal'.");
          break;
      }
    }

    private static List<InteractionItemRequirement> ConvertRequirements(
      IEnumerable<ScenarioInteractionItemRequirementDTO> dtos, string context, string field)
    {
      var result = new List<InteractionItemRequirement>();
      if (dtos == null)
        return result;
      foreach (var dto in dtos)
      {
        if (dto == null)
          continue;
        if (string.IsNullOrWhiteSpace(dto.ItemIdentifier))
          throw new JsonException($"{context}.{field}: itemIdentifier is required.");
        result.Add(new InteractionItemRequirement(dto.ItemIdentifier.Trim(), dto.Count ?? 1));
      }
      return result;
    }

    private static InteractionDefinition ConvertLegacyActingNpcInteraction(string npcIdentifier, ScenarioActingNpcInteractionDefinition legacy)
    {
      var definition = new InteractionDefinition
      {
        Entity = ScenarioEntityReference.ForIdentifier(npcIdentifier),
        InteractionIdentifier = legacy.Identifier,
        KindSpecified = true,
        Display = new InteractionDisplay { Text = legacy.DisplayText },
        CompletionSignal = legacy.CompletionSignalIdentifier,
        ScenarioIdentifier = legacy.ScenarioIdentifier,
        StartNodeIdentifier = legacy.ScenarioStartNodeIdentifier,
        InitialVisible = legacy.Enabled,
        InitialVisibleSpecified = true
      };
      if (!string.IsNullOrWhiteSpace(legacy.IconIdentifier))
        definition.Display.IconIdentifiers.Add(legacy.IconIdentifier);

      switch (legacy.InteractionType)
      {
        case ScenarioActingNpcInteractionType.StartScenario:
          definition.Kind = InteractionKind.StartScenario;
          break;
        case ScenarioActingNpcInteractionType.ItemSubmission:
          definition.Kind = InteractionKind.ItemSubmission;
          definition.Submission = new InteractionSubmissionSettings
          {
            Title = legacy.Title,
            SubmitButtonText = legacy.SubmitButtonText,
            RequiredItems = legacy.RequiredItems?.Where(value => value != null && !string.IsNullOrWhiteSpace(value.ItemIdentifier))
              .Select(value => new InteractionItemRequirement(value.ItemIdentifier, value.Count)).ToList() ?? new List<InteractionItemRequirement>()
          };
          if (legacy.ConsumeOnce)
          {
            definition.AfterInteract = InteractionAfterInteract.HideForAll;
            definition.AfterInteractSpecified = true;
          }
          break;
        default:
          definition.Kind = InteractionKind.Signal;
          break;
      }
      return definition;
    }

    public static List<ScenarioInteractionDefinitionDTO> ConvertInteractionsToDTO(IReadOnlyList<InteractionDefinition> definitions)
    {
      if (definitions == null || definitions.Count == 0)
        return null;
      return definitions.Where(value => value != null).Select(ConvertInteractionToDTO).ToList();
    }

    public static ScenarioInteractionDefinitionDTO ConvertInteractionToDTO(InteractionDefinition definition)
    {
      var dto = new ScenarioInteractionDefinitionDTO
      {
        Entity = ConvertEntityToDTO(definition.Entity),
        Interaction = definition.InteractionIdentifier,
        Kind = definition.KindSpecified ? definition.Kind.ToString() : null,
        HandlerKey = NullIfWhiteSpace(definition.HandlerKey),
        CompletionSignal = NullIfWhiteSpace(definition.CompletionSignal),
        AfterInteract = definition.AfterInteractSpecified ? definition.AfterInteract.ToString() : null,
        ScenarioIdentifier = NullIfWhiteSpace(definition.ScenarioIdentifier),
        StartNodeIdentifier = NullIfWhiteSpace(definition.StartNodeIdentifier),
        ActivateObjects = definition.ActivateObjects != null && definition.ActivateObjects.Count > 0 ? new List<string>(definition.ActivateObjects) : null,
        DeactivateObjects = definition.DeactivateObjects != null && definition.DeactivateObjects.Count > 0 ? new List<string>(definition.DeactivateObjects) : null
      };

      var display = definition.Display;
      if (display != null && (!string.IsNullOrWhiteSpace(display.Text) || (display.IconIdentifiers != null && display.IconIdentifiers.Count > 0)
                              || display.Color.HasValue || display.PrioritySpecified || !display.AllowIconFallback))
      {
        dto.Display = new ScenarioInteractionDisplayDTO
        {
          Text = NullIfWhiteSpace(display.Text),
          IconIdentifiers = display.IconIdentifiers != null && display.IconIdentifiers.Count > 0 ? new List<string>(display.IconIdentifiers) : null,
          Color = display.Color.HasValue
            ? new ScenarioColorDTO { R = display.Color.Value.r, G = display.Color.Value.g, B = display.Color.Value.b, A = display.Color.Value.a }
            : null,
          AllowIconFallback = display.AllowIconFallback ? (bool?)null : false,
          Priority = display.PrioritySpecified ? display.Priority : (int?)null
        };
      }

      if (definition.RequiredItems != null && definition.RequiredItems.Count > 0)
        dto.RequiredItems = definition.RequiredItems.Where(value => value != null).Select(ConvertRequirementToDTO).ToList();
      if (definition.ConsumeItems != null && definition.ConsumeItems.Count > 0)
        dto.ConsumeItems = definition.ConsumeItems.Where(value => value != null).Select(ConvertRequirementToDTO).ToList();
      if (definition.Extras != null && definition.Extras.Count > 0)
        dto.Extras = new Dictionary<string, string>(definition.Extras, StringComparer.Ordinal);

      if (definition.Submission != null)
      {
        dto.ItemSubmission = new ScenarioInteractionSubmissionDTO
        {
          Title = NullIfWhiteSpace(definition.Submission.Title),
          SubmitButtonText = NullIfWhiteSpace(definition.Submission.SubmitButtonText),
          RequiredItems = definition.Submission.RequiredItems?.Where(value => value != null).Select(ConvertRequirementToDTO).ToList()
        };
      }

      if (definition.InitialVisibleSpecified || definition.HasVisibilityConditions)
      {
        dto.Visibility = new ScenarioInteractionVisibilityDTO
        {
          Initial = definition.InitialVisibleSpecified ? definition.InitialVisible : (bool?)null,
          MatchMode = definition.HasVisibilityConditions && definition.MatchMode != ScenarioConditionMatchMode.All ? definition.MatchMode.ToString() : null,
          Conditions = ConvertConditionsToDTO(definition.VisibilityConditions)
        };
      }

      return dto;
    }

    private static ScenarioInteractionItemRequirementDTO ConvertRequirementToDTO(InteractionItemRequirement requirement)
      => new ScenarioInteractionItemRequirementDTO
      {
        ItemIdentifier = requirement.ItemIdentifier,
        Count = requirement.Count != 1 ? requirement.Count : (int?)null
      };

    // ── 가시성 노드 ─────────────────────────────────────────────────────

    public static ScenarioInteractionVisibilityNode ConvertVisibilityNode(ScenarioInteractionVisibilityNodeDTO dto)
    {
      string context = $"InteractionVisibility '{dto.Identifier}'";
      var node = new ScenarioInteractionVisibilityNode
      {
        Identifier = dto.Identifier,
        NextIdentifier = dto.NextIdentifier,
        Operation = ParseEnum(dto.Operation, ScenarioInteractionVisibilityOperation.Show, context, "operation"),
        PlayerScope = ParseEnum(dto.PlayerScope, ScenarioInteractionVisibilityPlayerScope.All, context, "playerScope"),
        PlayerTags = dto.PlayerTags?.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()).ToList() ?? new List<string>(),
        TagMatchMode = ParseEnum(dto.TagMatchMode, ScenarioConditionMatchMode.Any, context, "tagMatchMode")
      };

      if (dto.Targets != null)
      {
        int index = 0;
        foreach (var target in dto.Targets)
        {
          if (target == null)
          {
            index++;
            continue;
          }
          var entity = ConvertEntity(target.Entity, $"{context}.targets[{index}]");
          if (entity.IsEmpty)
            throw new JsonException($"{context}.targets[{index}]: 'entity' must specify 'id' or 'tag'.");
          if (string.IsNullOrWhiteSpace(target.Interaction))
            throw new JsonException($"{context}.targets[{index}]: 'interaction' is required.");
          node.Targets.Add(new ScenarioInteractionTarget { Entity = entity, InteractionIdentifier = target.Interaction.Trim() });
          index++;
        }
      }

      if (node.Targets.Count == 0)
        throw new JsonException($"{context}: 'targets' requires at least one entry.");
      if (node.PlayerScope == ScenarioInteractionVisibilityPlayerScope.ByTag && node.PlayerTags.Count == 0)
        throw new JsonException($"{context}: playerScope 'ByTag' requires 'playerTags'.");

      return node;
    }

    public static ScenarioInteractionVisibilityNodeDTO ConvertVisibilityNodeToDTO(ScenarioInteractionVisibilityNode node)
      => new ScenarioInteractionVisibilityNodeDTO
      {
        NodeType = "InteractionVisibility",
        Identifier = node.Identifier,
        NextIdentifier = node.NextIdentifier,
        Operation = node.Operation.ToString(),
        Targets = node.Targets?.Where(value => value != null).Select(value => new ScenarioInteractionTargetDTO
        {
          Entity = ConvertEntityToDTO(value.Entity),
          Interaction = value.InteractionIdentifier
        }).ToList() ?? new List<ScenarioInteractionTargetDTO>(),
        PlayerScope = node.PlayerScope != ScenarioInteractionVisibilityPlayerScope.All ? node.PlayerScope.ToString() : null,
        PlayerTags = node.PlayerTags != null && node.PlayerTags.Count > 0 ? new List<string>(node.PlayerTags) : null,
        TagMatchMode = node.TagMatchMode != ScenarioConditionMatchMode.Any ? node.TagMatchMode.ToString() : null
      };

    // ── Validator 조건 절 ───────────────────────────────────────────────

    public static void ApplyValidatorConditions(ScenarioValidatorNode node, ScenarioValidatorNodeDTO dto)
    {
      if (node?.RootConditions == null || dto?.RootConditions == null)
        return;
      int count = Math.Min(node.RootConditions.Count, dto.RootConditions.Count);
      for (int i = 0; i < count; i++)
      {
        var rootDto = dto.RootConditions[i];
        var root = node.RootConditions[i];
        if (rootDto == null || root == null)
          continue;
        root.Conditions = ConvertConditions(rootDto.Conditions, $"Validator '{node.Identifier}'.rootConditions[{i}]");
        if (root.Condition == ScenarioValidatorCondition.Conditions && root.Conditions.Count == 0)
          throw new JsonException($"Validator '{node.Identifier}'.rootConditions[{i}]: condition 'Conditions' requires a non-empty 'conditions' list.");
      }
    }

    public static void ApplyValidatorConditionsToDTO(ScenarioValidatorNode node, ScenarioValidatorNodeDTO dto)
    {
      if (node?.RootConditions == null || dto?.RootConditions == null)
        return;
      int count = Math.Min(node.RootConditions.Count, dto.RootConditions.Count);
      for (int i = 0; i < count; i++)
      {
        if (node.RootConditions[i] == null || dto.RootConditions[i] == null)
          continue;
        dto.RootConditions[i].Conditions = ConvertConditionsToDTO(node.RootConditions[i].Conditions);
      }
    }

    // ── 도우미 ─────────────────────────────────────────────────────────

    private static TEnum ParseEnum<TEnum>(string value, TEnum fallback, string context, string field) where TEnum : struct
    {
      if (string.IsNullOrWhiteSpace(value))
        return fallback;
      if (Enum.TryParse(value.Trim(), true, out TEnum parsed))
        return parsed;
      throw new JsonException($"{context}: unknown {field} '{value}'.");
    }

    private static void Require(string value, string context, string field)
    {
      if (string.IsNullOrWhiteSpace(value))
        throw new JsonException($"{context}: '{field}' is required for this condition type.");
    }

    private static void RequireEntity(ScenarioEntityReference entity, string context)
    {
      if (entity == null || entity.IsEmpty)
        throw new JsonException($"{context}: 'entity' must specify 'id' or 'tag'.");
    }

    private static string NullIfWhiteSpace(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
  }
}
