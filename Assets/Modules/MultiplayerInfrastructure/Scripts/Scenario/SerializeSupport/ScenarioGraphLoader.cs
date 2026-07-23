using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using UnityEngine;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.Entity.Patient;

namespace MultiplayerInfrastructure.Scenario
{
  public static class ScenarioGraphLoader
  {
    private static readonly JsonSerializerOptions SerializerOptions;

    static ScenarioGraphLoader()
    {
      SerializerOptions = new JsonSerializerOptions
      {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true
      };

      SerializerOptions.Converters.Add(new ScenarioNodeDTOConverter());
    }

#if UNITY_EDITOR
    /// <summary>
    /// 에디터에서 스키마 TextAsset이 변경되거나 브랜치 전환으로 교체된 뒤에도
    /// 이전 정적 캐시를 사용하지 않도록 다음 검증 전에 스키마를 다시 읽게 한다.
    /// </summary>
    public static void ReloadSchemaForEditor()
    {
      ScenarioJsonSchemaProvider.ForceReload();
    }
#endif

    public static ScenarioGraph LoadFromJson(string json, bool validateWithSchema = true)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        throw new ArgumentException("Scenario json text is null or empty.", nameof(json));
      }

      if (validateWithSchema)
      {
        ScenarioJsonSchemaValidator.Validate(json);
      }

      ScenarioGraphDTO dto;
      try
      {
        dto = JsonSerializer.Deserialize<ScenarioGraphDTO>(json, SerializerOptions);
      }
      catch (JsonException ex)
      {
        throw new JsonException("Scenario json payload is invalid.", ex);
      }

      return ToDomain(dto);
    }

    private static ScenarioGraph ToDomain(ScenarioGraphDTO dto)
    {
      if (dto == null)
      {
        throw new JsonException("Scenario graph payload is missing.");
      }

      if (dto.Nodes == null || dto.Nodes.Count == 0)
      {
        throw new JsonException("'nodes' object is missing.");
      }

      var graph = new ScenarioGraph();
      graph.Identifier = ResolveGraphIdentifier(dto);
      graph.Tags = NormalizeTags(dto.Tags);
      graph.QuestDefinitionIncludes = NormalizeQuestDefinitionIncludes(dto.QuestDefinitionIncludes);
      graph.ActingNpcs = ConvertActingNpcs(dto.ActingNpcs);
      graph.DefaultEntrypoint = string.IsNullOrWhiteSpace(dto.DefaultEntrypoint) ? null : dto.DefaultEntrypoint.Trim();

      foreach (var pair in dto.Nodes)
      {
        var nodeDTO = pair.Value;
        if (nodeDTO == null)
        {
          throw new JsonException($"Node '{pair.Key}' is null in the json payload.");
        }

        if (!string.Equals(pair.Key, nodeDTO.Identifier, StringComparison.Ordinal))
        {
          throw new JsonException(
              $"Node key '{pair.Key}' does not match its 'identifier' field '{nodeDTO.Identifier ?? "<null>"}'.");
        }

        graph.Add(ConvertNode(nodeDTO));
      }

      ValidateActingNpcSpawnReferences(graph);
      WarnForUndeclaredTags(graph);

      return graph;
    }

    private static void ValidateActingNpcSpawnReferences(ScenarioGraph graph)
    {
      var actingNpcIdentifiers = new HashSet<string>(
        graph.ActingNpcs.Where(actingNpc => actingNpc != null).Select(actingNpc => actingNpc.Identifier),
        StringComparer.Ordinal);
      foreach (var node in graph.Nodes.Values.OfType<ScenarioEntityPresetSpawnNode>())
      {
        if (!string.IsNullOrWhiteSpace(node.ActingNpcIdentifier)
            && !actingNpcIdentifiers.Contains(node.ActingNpcIdentifier))
        {
          throw new JsonException($"EntityPresetSpawn node '{node.Identifier}' references unknown actingNpc '{node.ActingNpcIdentifier}'.");
        }
      }
    }

    private static IReadOnlyList<ScenarioActingNpcDefinition> ConvertActingNpcs(
      IEnumerable<ScenarioActingNpcDefinitionDTO> actingNpcs)
    {
      if (actingNpcs == null)
        return Array.Empty<ScenarioActingNpcDefinition>();

      var result = new List<ScenarioActingNpcDefinition>();
      var identifiers = new HashSet<string>(StringComparer.Ordinal);
      var interactionIdentifiers = new HashSet<string>(StringComparer.Ordinal);
      foreach (var dto in actingNpcs)
      {
        if (dto == null)
          continue;

        var identifier = dto.Identifier?.Trim();
        if (string.IsNullOrWhiteSpace(identifier))
          throw new JsonException("ActingNpc identifier is required.");
        if (!identifiers.Add(identifier))
          throw new JsonException($"ActingNpc identifier '{identifier}' is duplicated.");
        if (!Enum.TryParse(dto.ActingNpcType ?? "Npc", true, out ScenarioActingNpcType actingNpcType))
          throw new JsonException($"ActingNpc '{identifier}' has unknown actingNpcType '{dto.ActingNpcType}'.");

        result.Add(new ScenarioActingNpcDefinition
        {
          Identifier = identifier,
          ActingNpcType = actingNpcType,
          PresetIdentifier = dto.PresetIdentifier?.Trim(),
          DisplayName = dto.DisplayName,
          PositionX = dto.PositionX ?? 0f,
          PositionY = dto.PositionY ?? 0f,
          PositionZ = dto.PositionZ ?? 0f,
          RotationX = dto.RotationX ?? 0f,
          RotationY = dto.RotationY ?? 0f,
          RotationZ = dto.RotationZ ?? 0f,
          SpawnOnStart = dto.SpawnOnStart ?? true,
          DespawnOnScenarioEnd = dto.DespawnOnScenarioEnd ?? true,
          Interactions = ConvertActingNpcInteractions(identifier, dto.Interactions, interactionIdentifiers)
        });
      }

      return result;
    }

    private static IReadOnlyList<ScenarioActingNpcInteractionDefinition> ConvertActingNpcInteractions(
      string actingNpcIdentifier,
      IEnumerable<ScenarioActingNpcInteractionDefinitionDTO> interactions,
      HashSet<string> graphInteractionIdentifiers)
    {
      if (interactions == null)
        return Array.Empty<ScenarioActingNpcInteractionDefinition>();

      var result = new List<ScenarioActingNpcInteractionDefinition>();
      var identifiers = new HashSet<string>(StringComparer.Ordinal);
      foreach (var dto in interactions)
      {
        if (dto == null)
          continue;

        var identifier = dto.Identifier?.Trim();
        if (string.IsNullOrWhiteSpace(identifier))
          throw new JsonException($"ActingNpc '{actingNpcIdentifier}' interaction identifier is required.");
        if (!identifiers.Add(identifier))
          throw new JsonException($"ActingNpc '{actingNpcIdentifier}' interaction '{identifier}' is duplicated.");
        if (!graphInteractionIdentifiers.Add(identifier))
          throw new JsonException($"ActingNpc interaction identifier '{identifier}' must be unique in the scenario.");
        if (!Enum.TryParse(dto.InteractionType, true, out ScenarioActingNpcInteractionType interactionType))
          throw new JsonException(
            $"ActingNpc '{actingNpcIdentifier}' interaction '{identifier}' has unknown interactionType '{dto.InteractionType}'.");

        var requiredItems = ConvertActingNpcItemRequirements(dto.RequiredItems);
        if (interactionType == ScenarioActingNpcInteractionType.StartScenario
            && string.IsNullOrWhiteSpace(dto.ScenarioIdentifier))
        {
          throw new JsonException(
            $"ActingNpc '{actingNpcIdentifier}' interaction '{identifier}' requires scenarioIdentifier.");
        }
        if (interactionType == ScenarioActingNpcInteractionType.ItemSubmission
            && requiredItems.Count == 0)
        {
          throw new JsonException(
            $"ActingNpc '{actingNpcIdentifier}' interaction '{identifier}' requires at least one valid required item.");
        }

        result.Add(new ScenarioActingNpcInteractionDefinition
        {
          Identifier = identifier,
          InteractionType = interactionType,
          DisplayText = dto.DisplayText,
          IconIdentifier = dto.IconIdentifier?.Trim(),
          ScenarioIdentifier = dto.ScenarioIdentifier?.Trim(),
          ScenarioStartNodeIdentifier = dto.ScenarioStartNodeIdentifier?.Trim(),
          Title = dto.Title,
          SubmitButtonText = dto.SubmitButtonText,
          RequiredItems = requiredItems,
          CompletionSignalIdentifier = dto.CompletionSignalIdentifier?.Trim(),
          ConsumeOnce = dto.ConsumeOnce ?? true,
          Enabled = dto.Enabled ?? true
        });
      }

      return result;
    }

    private static IReadOnlyList<ScenarioActingNpcItemRequirement> ConvertActingNpcItemRequirements(
      IEnumerable<ScenarioActingNpcItemRequirementDTO> requirements)
    {
      if (requirements == null)
        return Array.Empty<ScenarioActingNpcItemRequirement>();

      return requirements
        .Where(value => value != null && !string.IsNullOrWhiteSpace(value.ItemIdentifier))
        .Select(value => new ScenarioActingNpcItemRequirement
        {
          ItemIdentifier = value.ItemIdentifier.Trim(),
          Count = Math.Max(1, value.Count ?? 1)
        })
        .ToList();
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags)
    {
      if (tags == null)
      {
        return Array.Empty<string>();
      }

      return tags
          .Where(each => !string.IsNullOrWhiteSpace(each))
          .Select(each => each.Trim())
          .Distinct(StringComparer.OrdinalIgnoreCase)
          .ToList();
    }

    private static IReadOnlyList<string> NormalizeQuestDefinitionIncludes(IEnumerable<string> includes)
    {
      if (includes == null)
      {
        return Array.Empty<string>();
      }

      return includes
          .Where(each => !string.IsNullOrWhiteSpace(each))
          .Select(each => each.Trim())
          .Distinct(StringComparer.Ordinal)
          .ToList();
    }

    private static void WarnForUndeclaredTags(ScenarioGraph graph)
    {
      if (graph == null)
      {
        return;
      }

      var declared = new HashSet<string>(NormalizeTags(graph.Tags), StringComparer.OrdinalIgnoreCase);
      var used = CollectUsedTags(graph);

      var undeclared = used
          .Where(each => !declared.Contains(each))
          .OrderBy(each => each, StringComparer.OrdinalIgnoreCase)
          .ToList();

      if (undeclared.Count == 0)
      {
        return;
      }

      Debug.LogWarning(
          $"[ScenarioGraphLoader] Scenario '{graph.Identifier}' uses undeclared tags: " +
          $"[{string.Join(", ", undeclared)}]. " +
          "Declare these in top-level 'tags' for deterministic authoring.");
    }

    private static HashSet<string> CollectUsedTags(ScenarioGraph graph)
    {
      var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

      void AddIfNotBlank(string value)
      {
        if (!string.IsNullOrWhiteSpace(value))
        {
          used.Add(value.Trim());
        }
      }

      foreach (var node in graph.Nodes.Values.OrderBy(value => value?.Identifier ?? string.Empty, StringComparer.Ordinal))
      {
        switch (node)
        {
          case ScenarioPlayerTagNode tagNode:
            AddIfNotBlank(tagNode.Tag);
            AddIfNotBlank(tagNode.FromTag);
            AddIfNotBlank(tagNode.ToTag);
            AddIfNotBlank(tagNode.SwapTagA);
            AddIfNotBlank(tagNode.SwapTagB);
            break;

          case ScenarioValidatorNode validatorNode:
            if (validatorNode.RootConditions != null)
            {
              foreach (var rootCondition in validatorNode.RootConditions)
              {
                if (rootCondition == null)
                {
                  continue;
                }

                if (rootCondition.Condition == ScenarioValidatorCondition.PlayerAssignedTag)
                {
                  AddIfNotBlank(rootCondition.PlayerTag);
                }
              }
            }
            break;

          case ScenarioParallelNode parallelNode:
            if (parallelNode.Branches != null)
            {
              foreach (var branch in parallelNode.Branches)
              {
                if (branch?.RequiredPlayerTags != null)
                {
                  foreach (var requiredTag in branch.RequiredPlayerTags)
                  {
                    AddIfNotBlank(requiredTag);
                  }
                }

                if (branch?.ForbiddenPlayerTags != null)
                {
                  foreach (var forbiddenTag in branch.ForbiddenPlayerTags)
                  {
                    AddIfNotBlank(forbiddenTag);
                  }
                }
              }
            }
            break;
        }
      }

      return used;
    }

    private static string ResolveGraphIdentifier(ScenarioGraphDTO dto)
    {
      if (!string.IsNullOrWhiteSpace(dto.Identifier))
        return dto.Identifier.Trim();

      if (dto.Nodes != null)
      {
        foreach (var pair in dto.Nodes)
        {
          if (!string.IsNullOrWhiteSpace(pair.Key))
            return $"scenario_{pair.Key.Trim()}";
        }
      }

      return "scenario_graph";
    }

    private static IScenarioNode ConvertNode(ScenarioNodeDTO dto) =>
        dto switch
        {
          ScenarioDialogueNodeDTO dialogue => ConvertDialogue(dialogue),
          ScenarioDisinteractableDialogueNodeDTO dialogue => ConvertDisinteractableDialogue(dialogue),
          ScenarioChoiceNodeDTO choice => ConvertChoice(choice),
          ScenarioSoundNodeDTO sound => ConvertSound(sound),
          ScenarioPlayerMoveNodeDTO move => ConvertPlayerMove(move),
          ScenarioNPCMoveNodeDTO npcMove => ConvertNPCMove(npcMove),
          ScenarioCameraTargetNodeDTO camera => ConvertCameraTarget(camera),
          ScenarioInvokeEventNodeDTO invoke => ConvertInvokeEvent(invoke),
          ScenarioServerInternalSignalNodeDTO internalSignal => ConvertServerInternalSignal(internalSignal),
          ScenarioSignalListenerNodeDTO signalListener => ConvertSignalListener(signalListener),
          ScenarioEntityStateSignalBindingNodeDTO stateBinding => ConvertEntityStateSignalBinding(stateBinding),
          ScenarioSignalCounterNodeDTO signalCounter => ConvertSignalCounter(signalCounter),
          ScenarioValidatorNodeDTO validator => ConvertValidator(validator),
          ScenarioParallelNodeDTO parallel => ConvertParallel(parallel),
          ScenarioQuestControlNodeDTO questControl => ConvertQuestControl(questControl),
          ScenarioQuestWaypointHighlightNodeDTO highlight => ConvertQuestWaypointHighlight(highlight),
          ScenarioDelayNodeDTO delay => ConvertDelay(delay),
          ScenarioInteractionNodeDTO interaction => ConvertInteraction(interaction),
          ScenarioCombineItemNodeDTO combineItem => ConvertCombineItem(combineItem),
          ScenarioQuizNodeDTO quiz => ConvertQuiz(quiz),
          ScenarioStateUpdateNodeDTO stateUpdate => ConvertStateUpdate(stateUpdate),
          ScenarioPlayTTSNodeDTO playTTS => ConvertPlayTTS(playTTS),
          ScenarioPlayerTagNodeDTO playerTag => ConvertPlayerTag(playerTag),
          ScenarioEntityPresetSpawnNodeDTO entityPresetSpawn => ConvertEntityPresetSpawn(entityPresetSpawn),
          ScenarioEntityTagNodeDTO entityTag => ConvertEntityTag(entityTag),
          ScenarioEntityInitNodeDTO entityInit => ConvertEntityInit(entityInit),
          ScenarioTriageAssessControlNodeDTO triageAssess => ConvertTriageAssessControl(triageAssess),
          ScenarioPatientMedicalStatePresetNodeDTO patientPreset => ConvertPatientMedicalStatePreset(patientPreset),
          ScenarioItemSubmissionConfigNodeDTO itemSubmission => ConvertItemSubmissionConfig(itemSubmission),
          ScenarioNpcInteractControlNodeDTO npcInteractControl => ConvertNpcInteractControl(npcInteractControl),
          ScenarioChatPrintNodeDTO chatPrint => ConvertChatPrint(chatPrint),
          ScenarioExecuteCommandNodeDTO executeCommand => ConvertExecuteCommand(executeCommand),
          ScenarioTimeControlNodeDTO timeControl => ConvertTimeControl(timeControl),
          _ => throw new JsonException($"Unsupported scenario node dto type '{dto.GetType().Name}'.")
        };

    private static ScenarioDisinteractableDialogueNode ConvertDisinteractableDialogue(ScenarioDisinteractableDialogueNodeDTO dto) =>
        new ScenarioDisinteractableDialogueNode
        {
          Identifier = dto.Identifier,
          SpeakerName = dto.SpeakerName,
          DialogueContent = dto.DialogueContent,
          PortraitSpriteIdentifier = dto.PortraitSpriteIdentifier,
          FadeInDuration = dto.FadeInDuration ?? ScenarioTimeValue.Seconds(0.5d),
          DisplayDuration = dto.DisplayDuration ?? ScenarioTimeValue.Seconds(1.5d),
          FadeOutDuration = dto.FadeOutDuration ?? ScenarioTimeValue.Seconds(0.5d),
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioDialogueNode ConvertDialogue(ScenarioDialogueNodeDTO dto) =>
        new ScenarioDialogueNode
        {
          Identifier = dto.Identifier,
          SpeakerName = dto.SpeakerName,
          DialogueContent = dto.DialogueContent,
          PortraitSpriteIdentifier = dto.PortraitSpriteIdentifier,
          AutoAdvanceSeconds = dto.AutoAdvanceSeconds,
          InteractionRequired = dto.InteractionRequired ?? false,
          PlayTTS = dto.PlayTTS ?? false,
          TtsVoiceIdentifier = string.IsNullOrEmpty(dto.TtsVoiceIdentifier) ? null : dto.TtsVoiceIdentifier,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioChoiceNode ConvertChoice(ScenarioChoiceNodeDTO dto)
    {
      var options = new List<ScenarioChoiceOption>(dto.Options?.Count ?? 0);

      if (dto.Options != null)
      {
        foreach (var optionDTO in dto.Options)
        {
          if (optionDTO == null)
          {
            continue;
          }

          Color? color = null;
          if (optionDTO.DisplayColor != null)
          {
            color = new Color(
                optionDTO.DisplayColor.R,
                optionDTO.DisplayColor.G,
                optionDTO.DisplayColor.B,
                optionDTO.DisplayColor.A);
          }

          options.Add(new ScenarioChoiceOption
          {
            DisplayText = optionDTO.DisplayText,
            DisplayIconIdentifier = optionDTO.DisplayIconIdentifier,
            DisplayColor = color ?? Color.white,
            NextNodeIdentifier = optionDTO.NextNodeIdentifier
          });
        }
      }

      return new ScenarioChoiceNode
      {
        Identifier = dto.Identifier,
        SpeakerName = dto.SpeakerName,
        DialogueContent = dto.DialogueContent,
        PortraitSpriteIdentifier = dto.PortraitSpriteIdentifier,
        PlayTTS = dto.PlayTTS ?? false,
        TtsVoiceIdentifier = string.IsNullOrEmpty(dto.TtsVoiceIdentifier) ? null : dto.TtsVoiceIdentifier,
        Options = options
      };
    }

    private static ScenarioSoundNode ConvertSound(ScenarioSoundNodeDTO dto) =>
        new ScenarioSoundNode
        {
          Identifier = dto.Identifier,
          SoundResourceIdentifier = dto.SoundResourceIdentifier,
          WaitUntilFinished = dto.WaitUntilFinished ?? true,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioPlayerMoveNode ConvertPlayerMove(ScenarioPlayerMoveNodeDTO dto) =>
        new ScenarioPlayerMoveNode
        {
          Identifier = dto.Identifier,
          DestinationType = ParseDestinationType(dto.DestinationType),
          DestinationIdentifier = dto.DestinationIdentifier,
          DestinationX = dto.DestinationX ?? 0f,
          DestinationY = dto.DestinationY ?? 0f,
          DestinationZ = dto.DestinationZ ?? 0f,
          IgnoreGroundCheck = dto.IgnoreGroundCheck ?? false,
          MoveMode = ParseMoveMode(dto.MoveMode),
          MoveSpeed = dto.MoveSpeed ?? 0f,
          MoveDuration = dto.MoveDuration ?? 0f,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioNPCMoveNode ConvertNPCMove(ScenarioNPCMoveNodeDTO dto) =>
        new ScenarioNPCMoveNode
        {
          Identifier = dto.Identifier,
          NPCIdentifier = dto.NPCIdentifier,
          DestinationType = ParseDestinationType(dto.DestinationType),
          DestinationIdentifier = dto.DestinationIdentifier,
          DestinationX = dto.DestinationX ?? 0f,
          DestinationY = dto.DestinationY ?? 0f,
          DestinationZ = dto.DestinationZ ?? 0f,
          IgnoreGroundCheck = dto.IgnoreGroundCheck ?? false,
          MoveMode = ParseMoveMode(dto.MoveMode),
          MoveSpeed = dto.MoveSpeed ?? 0f,
          MoveDuration = dto.MoveDuration ?? 0f,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioCameraTargetNode ConvertCameraTarget(ScenarioCameraTargetNodeDTO dto) =>
        new ScenarioCameraTargetNode
        {
          Identifier = dto.Identifier,
          TargetObjectIdentifier = dto.TargetObjectIdentifier,
          OffsetX = dto.OffsetX ?? 0f,
          OffsetY = dto.OffsetY ?? 0f,
          OffsetZ = dto.OffsetZ ?? 0f,
          BlendTime = dto.BlendTime ?? 0f,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioInvokeEventNode ConvertInvokeEvent(ScenarioInvokeEventNodeDTO dto) =>
        new ScenarioInvokeEventNode
        {
          Identifier = dto.Identifier,
          EventIdentifier = dto.EventIdentifier,
          MoveNextBehavior = ParseInvokeEventMoveNext(dto.MoveNextBehavior),
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioServerInternalSignalNode ConvertServerInternalSignal(ScenarioServerInternalSignalNodeDTO dto) =>
        new ScenarioServerInternalSignalNode
        {
          Identifier = dto.Identifier,
          TargetIdentifier = dto.TargetIdentifier,
          SignalIdentifier = dto.SignalIdentifier,
          Operation = ParseServerInternalSignalOperation(dto.Operation),
          WaitForResolution = dto.WaitForResolution ?? true,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioSignalListenerNode ConvertSignalListener(ScenarioSignalListenerNodeDTO dto) =>
        new ScenarioSignalListenerNode
        {
          Identifier = dto.Identifier, ListenerIdentifier = dto.ListenerIdentifier,
          Operation = Enum.TryParse(dto.Operation, true, out ScenarioSignalListenerOperation operation) ? operation : ScenarioSignalListenerOperation.Register,
          SourceSignalIdentifier = dto.SourceSignalIdentifier, OutputSignalIdentifier = dto.OutputSignalIdentifier,
          RequiredSignalIdentifiers = dto.RequiredSignalIdentifiers ?? new List<string>(), ConsumeOnce = dto.ConsumeOnce ?? true,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioEntityStateSignalBindingNode ConvertEntityStateSignalBinding(ScenarioEntityStateSignalBindingNodeDTO dto) =>
        new ScenarioEntityStateSignalBindingNode
        {
          Identifier = dto.Identifier,
          NextIdentifier = dto.NextIdentifier,
          BindingIdentifier = dto.BindingIdentifier,
          Operation = Enum.TryParse(dto.Operation, true, out ScenarioEntityStateSignalBindingOperation operation)
            ? operation
            : ScenarioEntityStateSignalBindingOperation.Register,
          TargetEntityIdentifier = dto.TargetEntityIdentifier,
          TargetEntityStateKey = dto.TargetEntityStateKey,
          EventName = dto.EventName,
          EventKey = dto.EventKey,
          OutputSignalIdentifier = dto.OutputSignalIdentifier,
          ConsumeOnce = dto.ConsumeOnce ?? false,
        };

    private static ScenarioSignalCounterNode ConvertSignalCounter(ScenarioSignalCounterNodeDTO dto) =>
        new ScenarioSignalCounterNode
        {
          Identifier = dto.Identifier,
          NextIdentifier = dto.NextIdentifier,
          CounterIdentifier = dto.CounterIdentifier,
          Operation = Enum.TryParse(dto.Operation, true, out ScenarioSignalCounterOperation operation)
            ? operation
            : ScenarioSignalCounterOperation.Register,
          SourceSignalPrefix = dto.SourceSignalPrefix,
          Threshold = dto.Threshold ?? 1,
          OutputSignalIdentifier = dto.OutputSignalIdentifier,
        };

    private static ScenarioChatPrintNode ConvertChatPrint(ScenarioChatPrintNodeDTO dto) =>
        new ScenarioChatPrintNode
        {
          Identifier = dto.Identifier,
          Message = dto.Message,
          Targets = ParseChatPrintTarget(dto.Targets),
          Broadcast = dto.Broadcast ?? false,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioExecuteCommandNode ConvertExecuteCommand(ScenarioExecuteCommandNodeDTO dto) =>
        new ScenarioExecuteCommandNode
        {
          Identifier = dto.Identifier,
          CommandLine = dto.CommandLine,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioQuestControlNode ConvertQuestControl(ScenarioQuestControlNodeDTO dto) =>
        new ScenarioQuestControlNode
        {
          Identifier = dto.Identifier,
          Operation = ParseQuestOperation(dto.Operation),
          FailureStrategy = ParseQuestFailureStrategy(dto.FailureStrategy),
          QuestDefinitionIdentifier = dto.QuestDefinitionIdentifier,
          Quest = dto.Quest,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioQuestWaypointHighlightNode ConvertQuestWaypointHighlight(ScenarioQuestWaypointHighlightNodeDTO dto) =>
        new ScenarioQuestWaypointHighlightNode
        {
          Identifier = dto.Identifier,
          WaypointIdentifier = dto.WaypointIdentifier,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioValidatorNode ConvertValidator(ScenarioValidatorNodeDTO dto) =>
        new ScenarioValidatorNode
        {
          Identifier = dto.Identifier,
          RootConditions = ParseValidatorRootConditions(dto.RootConditions),
          OnFailure = ParseValidatorOnFailure(dto.OnFailure),
          FailureReportTargets = ParseValidatorFailureReportTargets(dto.FailureReportTargets),
          FailureNextIdentifier = dto.FailureNextIdentifier,
          WaitForCondition = dto.WaitForCondition ?? false,
          WaitTimeoutSeconds = (dto.WaitTimeoutSeconds is > 0f) ? dto.WaitTimeoutSeconds : null,
          OnWaitTimeout = ParseValidatorWaitTimeoutBehavior(dto.OnWaitTimeout),
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioDelayNode ConvertDelay(ScenarioDelayNodeDTO dto) =>
        new ScenarioDelayNode
        {
          Identifier = dto.Identifier,
          Duration = dto.Duration ?? ScenarioTimeValue.Seconds(0d),
          WaitUntil = ParseDelayWaitUntil(dto.WaitUntil),
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioTimeControlNode ConvertTimeControl(ScenarioTimeControlNodeDTO dto) =>
        new ScenarioTimeControlNode
        {
          Identifier = dto.Identifier,
          Operation = ParseTimeOperation(dto.Operation),
          TimerId = dto.TimerId,
          Direction = ParseTimeDirection(dto.Direction),
          DurationSeconds = dto.DurationSeconds ?? 0f,
          StartSeconds = dto.StartSeconds ?? 0f,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioInteractionNode ConvertInteraction(ScenarioInteractionNodeDTO dto) =>
        new ScenarioInteractionNode
        {
          Identifier = dto.Identifier,
          ActorScope = ParseInteractionActorScope(dto.ActorScope),
          TargetIdentifier = dto.TargetIdentifier,
          RequiredItemIdentifier = dto.RequiredItemIdentifier,
          InteractionType = ParseInteractionType(dto.InteractionType),
          CompletionConditionIdentifier = dto.CompletionConditionIdentifier,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioCombineItemNode ConvertCombineItem(ScenarioCombineItemNodeDTO dto) =>
        new ScenarioCombineItemNode
        {
          Identifier = dto.Identifier,
          InputItemIdentifiers = dto.InputItemIdentifiers ?? new List<string>(),
          OutputItemIdentifier = dto.OutputItemIdentifier,
          AutoCombine = dto.AutoCombine ?? false,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioQuizNode ConvertQuiz(ScenarioQuizNodeDTO dto) =>
        new ScenarioQuizNode
        {
          Identifier = dto.Identifier,
          Question = dto.Question,
          Options = dto.Options ?? new List<string>(),
          CorrectIndex = dto.CorrectIndex ?? 0,
          OnCorrectNextIdentifier = dto.OnCorrectNextIdentifier,
          OnIncorrectNextIdentifier = dto.OnIncorrectNextIdentifier,
          FeedbackCorrect = dto.FeedbackCorrect,
          FeedbackIncorrect = dto.FeedbackIncorrect,
          PlayTTS = dto.PlayTTS ?? false,
          TtsVoiceIdentifier = string.IsNullOrEmpty(dto.TtsVoiceIdentifier) ? null : dto.TtsVoiceIdentifier,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioStateUpdateNode ConvertStateUpdate(ScenarioStateUpdateNodeDTO dto) =>
        new ScenarioStateUpdateNode
        {
          Identifier = dto.Identifier,
          TargetEntityIdentifier = dto.TargetEntityIdentifier,
          StateKey = dto.StateKey,
          StateValue = dto.StateValue,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioTriageAssessControlNode ConvertTriageAssessControl(ScenarioTriageAssessControlNodeDTO dto) =>
        new ScenarioTriageAssessControlNode
        {
          Identifier = dto.Identifier,
          TargetEntityIdentifier = dto.TargetEntityIdentifier,
          Assessable = dto.Assessable,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioPlayTTSNode ConvertPlayTTS(ScenarioPlayTTSNodeDTO dto) =>
        new ScenarioPlayTTSNode
        {
          Identifier = dto.Identifier,
          TranscriptIdentifier = dto.TranscriptIdentifier,
          Variables = dto.Variables ?? new Dictionary<string, string>(),
          WaitUntilFinished = dto.WaitUntilFinished ?? true,
          TtsVoiceIdentifier = string.IsNullOrEmpty(dto.TtsVoiceIdentifier) ? null : dto.TtsVoiceIdentifier,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioPlayTTSNodeDTO ConvertToDTO(ScenarioPlayTTSNode node) =>
        new ScenarioPlayTTSNodeDTO
        {
          NodeType = "PlayTTS",
          Identifier = node.Identifier,
          TranscriptIdentifier = node.TranscriptIdentifier,
          Variables = node.Variables != null && node.Variables.Count > 0
              ? node.Variables
              : null,
          WaitUntilFinished = node.WaitUntilFinished,
          TtsVoiceIdentifier = string.IsNullOrEmpty(node.TtsVoiceIdentifier) ? null : node.TtsVoiceIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioPlayerTagNode ConvertPlayerTag(ScenarioPlayerTagNodeDTO dto) =>
        new ScenarioPlayerTagNode
        {
          Identifier = dto.Identifier,
          Operation = ParsePlayerTagOperationType(dto.Operation),
          Scope = ParsePlayerTagScope(dto.Scope),
          Tag = dto.Tag,
          FromTag = dto.FromTag,
          ToTag = dto.ToTag,
          SwapTagA = dto.SwapTagA,
          SwapTagB = dto.SwapTagB,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioPlayerTagNodeDTO ConvertToDTO(ScenarioPlayerTagNode node) =>
        new ScenarioPlayerTagNodeDTO
        {
          NodeType = "TagModification",
          Identifier = node.Identifier,
          Operation = node.Operation.ToString(),
          Scope = node.Scope.ToString(),
          Tag = node.Tag,
          FromTag = node.FromTag,
          ToTag = node.ToTag,
          SwapTagA = node.SwapTagA,
          SwapTagB = node.SwapTagB,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioEntityPresetSpawnNode ConvertEntityPresetSpawn(ScenarioEntityPresetSpawnNodeDTO dto) =>
        new ScenarioEntityPresetSpawnNode
        {
          Identifier = dto.Identifier,
          PresetIdentifier = dto.PresetIdentifier,
          ActingNpcIdentifier = dto.ActingNpcIdentifier,
          SpawnedEntityIdentifier = dto.SpawnedEntityIdentifier,
          PositionSourceEntityIdentifier = dto.PositionSourceEntityIdentifier,
          PositionX = dto.PositionX ?? 0f,
          PositionY = dto.PositionY ?? 0f,
          PositionZ = dto.PositionZ ?? 0f,
          ResultStateKey = dto.ResultStateKey,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioEntityTagNode ConvertEntityTag(ScenarioEntityTagNodeDTO dto) =>
        new ScenarioEntityTagNode
        {
          Identifier = dto.Identifier,
          Operation = ParsePlayerTagOperationType(dto.Operation),
          TargetEntityIdentifier = dto.TargetEntityIdentifier,
          TargetEntityStateKey = dto.TargetEntityStateKey,
          Tag = dto.Tag,
          FromTag = dto.FromTag,
          ToTag = dto.ToTag,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioEntityInitNode ConvertEntityInit(ScenarioEntityInitNodeDTO dto)
    {
      var operations = new List<ScenarioEntityStateOperation>(dto.StateOperations?.Count ?? 0);
      if (dto.StateOperations != null)
      {
        foreach (var opDTO in dto.StateOperations)
        {
          if (opDTO == null)
            continue;

          operations.Add(new ScenarioEntityStateOperation
          {
            Kind = ParseEntityStateOperationKind(opDTO.Kind),
            Key = opDTO.Key,
            Value = opDTO.Value,
            DisplayActive = opDTO.DisplayActive ?? true
          });
        }
      }

      return new ScenarioEntityInitNode
      {
        Identifier = dto.Identifier,
        PresetIdentifier = dto.PresetIdentifier,
        PositionSourceEntityIdentifier = dto.PositionSourceEntityIdentifier,
        PositionX = dto.PositionX ?? 0f,
        PositionY = dto.PositionY ?? 0f,
        PositionZ = dto.PositionZ ?? 0f,
        TargetEntityIdentifier = dto.TargetEntityIdentifier,
        TargetEntityStateKey = dto.TargetEntityStateKey,
        EntityIdentifier = dto.EntityIdentifier,
        ResultStateKey = dto.ResultStateKey,
        StateOperations = operations,
        NextIdentifier = dto.NextIdentifier
      };
    }

    private static ScenarioEntityPresetSpawnNodeDTO ConvertToDTO(ScenarioEntityPresetSpawnNode node) =>
        new ScenarioEntityPresetSpawnNodeDTO
        {
          NodeType = "EntityPresetSpawn",
          Identifier = node.Identifier,
          PresetIdentifier = node.PresetIdentifier,
          ActingNpcIdentifier = node.ActingNpcIdentifier,
          SpawnedEntityIdentifier = node.SpawnedEntityIdentifier,
          PositionSourceEntityIdentifier = node.PositionSourceEntityIdentifier,
          PositionX = node.PositionX,
          PositionY = node.PositionY,
          PositionZ = node.PositionZ,
          ResultStateKey = node.ResultStateKey,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioItemSubmissionConfigNode ConvertItemSubmissionConfig(ScenarioItemSubmissionConfigNodeDTO dto)
    {
      var requirements = new List<ScenarioItemRequirement>();
      if (dto.RequiredItems != null)
      {
        foreach (var reqDto in dto.RequiredItems)
        {
          if (reqDto == null || string.IsNullOrWhiteSpace(reqDto.ItemIdentifier))
            continue;

          requirements.Add(new ScenarioItemRequirement
          {
            ItemIdentifier = reqDto.ItemIdentifier,
            Count = reqDto.Count.HasValue && reqDto.Count.Value > 0 ? reqDto.Count.Value : 1
          });
        }
      }

      return new ScenarioItemSubmissionConfigNode
      {
        Identifier = dto.Identifier,
        PresetIdentifier = dto.PresetIdentifier,
        SpawnedEntityIdentifier = dto.SpawnedEntityIdentifier,
        PositionSourceEntityIdentifier = dto.PositionSourceEntityIdentifier,
        PositionX = dto.PositionX ?? 0f,
        PositionY = dto.PositionY ?? 0f,
        PositionZ = dto.PositionZ ?? 0f,
        TargetIdentifier = dto.TargetIdentifier,
        TargetStateKey = dto.TargetStateKey,
        RequiredItems = requirements,
        CompletionSignalIdentifier = dto.CompletionSignalIdentifier,
        Enabled = dto.Enabled ?? true,
        ResultStateKey = dto.ResultStateKey,
        NextIdentifier = dto.NextIdentifier
      };
    }

    private static ScenarioItemSubmissionConfigNodeDTO ConvertToDTO(ScenarioItemSubmissionConfigNode node)
    {
      List<ScenarioItemRequirementDTO> requirements = null;
      if (node.RequiredItems != null && node.RequiredItems.Count > 0)
      {
        requirements = new List<ScenarioItemRequirementDTO>();
        foreach (var req in node.RequiredItems)
        {
          if (req == null || string.IsNullOrWhiteSpace(req.ItemIdentifier))
            continue;

          requirements.Add(new ScenarioItemRequirementDTO
          {
            ItemIdentifier = req.ItemIdentifier,
            Count = req.Count > 0 ? req.Count : 1
          });
        }
      }

      return new ScenarioItemSubmissionConfigNodeDTO
      {
        NodeType = "ItemSubmissionConfig",
        Identifier = node.Identifier,
        PresetIdentifier = node.PresetIdentifier,
        SpawnedEntityIdentifier = node.SpawnedEntityIdentifier,
        PositionSourceEntityIdentifier = node.PositionSourceEntityIdentifier,
        PositionX = node.PositionX,
        PositionY = node.PositionY,
        PositionZ = node.PositionZ,
        TargetIdentifier = node.TargetIdentifier,
        TargetStateKey = node.TargetStateKey,
        RequiredItems = requirements,
        CompletionSignalIdentifier = node.CompletionSignalIdentifier,
        Enabled = node.Enabled ? (bool?)null : false,
        ResultStateKey = node.ResultStateKey,
        NextIdentifier = node.NextIdentifier
      };
    }

    private static ScenarioNpcInteractControlNode ConvertNpcInteractControl(ScenarioNpcInteractControlNodeDTO dto) =>
        new ScenarioNpcInteractControlNode
        {
          Identifier = dto.Identifier,
          NpcIdentifier = dto.NpcIdentifier,
          InteractableIdentifier = dto.InteractableIdentifier,
          Operation = ParseNpcInteractControlOperation(dto.Operation),
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioNpcInteractControlNodeDTO ConvertToDTO(ScenarioNpcInteractControlNode node) =>
        new ScenarioNpcInteractControlNodeDTO
        {
          NodeType = "NpcInteractControl",
          Identifier = node.Identifier,
          NpcIdentifier = node.NpcIdentifier,
          InteractableIdentifier = node.InteractableIdentifier,
          Operation = node.Operation.ToString(),
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioNpcInteractControlOperation ParseNpcInteractControlOperation(string value)
    {
      if (!string.IsNullOrWhiteSpace(value)
          && System.Enum.TryParse<ScenarioNpcInteractControlOperation>(value, ignoreCase: true, out var parsed))
      {
        return parsed;
      }

      return ScenarioNpcInteractControlOperation.Add;
    }

    private static ScenarioEntityTagNodeDTO ConvertToDTO(ScenarioEntityTagNode node) =>
        new ScenarioEntityTagNodeDTO
        {
          NodeType = "EntityTag",
          Identifier = node.Identifier,
          Operation = node.Operation.ToString(),
          TargetEntityIdentifier = node.TargetEntityIdentifier,
          TargetEntityStateKey = node.TargetEntityStateKey,
          Tag = node.Tag,
          FromTag = node.FromTag,
          ToTag = node.ToTag,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioEntityInitNodeDTO ConvertToDTO(ScenarioEntityInitNode node)
    {
      List<ScenarioEntityStateOperationDTO> operations = null;
      if (node.StateOperations != null && node.StateOperations.Count > 0)
      {
        operations = new List<ScenarioEntityStateOperationDTO>(node.StateOperations.Count);
        foreach (var op in node.StateOperations)
        {
          if (op == null)
            continue;

          operations.Add(new ScenarioEntityStateOperationDTO
          {
            Kind = op.Kind.ToString(),
            Key = op.Key,
            Value = op.Value,
            DisplayActive = op.DisplayActive
          });
        }
      }

      return new ScenarioEntityInitNodeDTO
      {
        NodeType = "EntityInit",
        Identifier = node.Identifier,
        PresetIdentifier = node.PresetIdentifier,
        PositionSourceEntityIdentifier = node.PositionSourceEntityIdentifier,
        PositionX = node.PositionX,
        PositionY = node.PositionY,
        PositionZ = node.PositionZ,
        TargetEntityIdentifier = node.TargetEntityIdentifier,
        TargetEntityStateKey = node.TargetEntityStateKey,
        EntityIdentifier = node.EntityIdentifier,
        ResultStateKey = node.ResultStateKey,
        StateOperations = operations,
        NextIdentifier = node.NextIdentifier
      };
    }

    private static ScenarioParallelNode ConvertParallel(ScenarioParallelNodeDTO dto)
    {
      var branches = new List<ScenarioParallelBranch>(dto.Branches?.Count ?? 0);

      if (dto.Branches != null)
      {
        foreach (var branchDTO in dto.Branches)
        {
          if (branchDTO == null)
          {
            continue;
          }

          branches.Add(new ScenarioParallelBranch
          {
            Identifier = branchDTO.Identifier,
            CompletionConditionIdentifier = branchDTO.CompletionConditionIdentifier,
            RequiredPlayerTags = branchDTO.RequiredPlayerTags ?? new List<string>(),
            ForbiddenPlayerTags = branchDTO.ForbiddenPlayerTags ?? new List<string>(),
            RequiredPlayerTagsMatchMode = ParsePlayerTagMatchMode(branchDTO.RequiredPlayerTagsMatchMode)
          });
        }
      }

      return new ScenarioParallelNode
      {
        Identifier = dto.Identifier,
        WaitMode = ParseWaitMode(dto.WaitMode),
        AllocationType = ParseParallelAllocationType(dto.AllocationType),
        WhenBranchingPlayerNotMatched = ParseParallelMismatchHandling(dto.WhenBranchingPlayerNotMatched),
        Branches = branches,
        // NextIdentifier 누락 시 병렬 노드 완료 후 다음 노드로 진행할 수 없어
        // 시나리오가 조기 종료된다(다른 모든 컨버터와 동일하게 복사해야 함).
        NextIdentifier = dto.NextIdentifier
      };
    }

    private static ScenarioWaitMode ParseWaitMode(string waitModeText)
    {
      if (string.IsNullOrWhiteSpace(waitModeText))
      {
        return ScenarioWaitMode.All; // 프로젝트 기본값에 맞게 수정
      }

      if (Enum.TryParse(waitModeText, ignoreCase: true, out ScenarioWaitMode parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioWaitMode '{waitModeText}'.");
    }

    private static ScenarioQuestOperationType ParseQuestOperation(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return ScenarioQuestOperationType.Add;

      if (Enum.TryParse(text, ignoreCase: true, out ScenarioQuestOperationType parsed))
        return parsed;

      throw new JsonException($"Unknown ScenarioQuestOperationType '{text}'.");
    }

    private static ScenarioQuestFailureStrategy ParseQuestFailureStrategy(string text)
    {
      if (string.IsNullOrWhiteSpace(text))
        return ScenarioQuestFailureStrategy.Overwrite;

      if (Enum.TryParse(text, ignoreCase: true, out ScenarioQuestFailureStrategy parsed))
        return parsed;

      throw new JsonException($"Unknown ScenarioQuestFailureStrategy '{text}'.");
    }

    private static ScenarioMoveMode ParseMoveMode(string moveModeText)
    {
      if (string.IsNullOrWhiteSpace(moveModeText))
      {
        return ScenarioMoveMode.BySpeed;
      }

      if (Enum.TryParse(moveModeText, ignoreCase: true, out ScenarioMoveMode parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioMoveMode '{moveModeText}'.");
    }

    private static ScenarioMoveDestinationType ParseDestinationType(string destinationTypeText)
    {
      if (string.IsNullOrWhiteSpace(destinationTypeText))
      {
        return ScenarioMoveDestinationType.Position; // 프로젝트 기본값으로 대체
      }

      if (Enum.TryParse(destinationTypeText, ignoreCase: true, out ScenarioMoveDestinationType parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioMoveDestinationType '{destinationTypeText}'.");
    }

    private static ScenarioDelayWaitUntil ParseDelayWaitUntil(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioDelayWaitUntil.WaitUntilDone;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioDelayWaitUntil parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioDelayWaitUntil '{value}'.");
    }

    private static ScenarioTimeOperationType ParseTimeOperation(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioTimeOperationType.Create;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioTimeOperationType parsed)
          && Enum.IsDefined(typeof(ScenarioTimeOperationType), parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioTimeOperationType '{value}'.");
    }

    private static ScenarioTimeDirection ParseTimeDirection(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioTimeDirection.Stopwatch;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioTimeDirection parsed)
          && Enum.IsDefined(typeof(ScenarioTimeDirection), parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioTimeDirection '{value}'.");
    }

    private static ScenarioInteractionActorScope ParseInteractionActorScope(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioInteractionActorScope.Player;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioInteractionActorScope parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioInteractionActorScope '{value}'.");
    }

    private static ScenarioInteractionType ParseInteractionType(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioInteractionType.Use;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioInteractionType parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioInteractionType '{value}'.");
    }

    private static ScenarioPlayerTagOperationType ParsePlayerTagOperationType(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return ScenarioPlayerTagOperationType.Add;

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioPlayerTagOperationType parsed))
        return parsed;

      throw new JsonException($"Unknown ScenarioPlayerTagOperationType '{value}'.");
    }

    private static ScenarioPlayerTagScope ParsePlayerTagScope(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return ScenarioPlayerTagScope.Current;

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioPlayerTagScope parsed))
        return parsed;

      throw new JsonException($"Unknown ScenarioPlayerTagScope '{value}'.");
    }

    private static ScenarioEntityStateOperationKind ParseEntityStateOperationKind(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return ScenarioEntityStateOperationKind.DisplayState;

      // 숫자 문자열은 Enum.TryParse 로 파싱되더라도 정의되지 않은 값일 수 있으므로 먼저 거부한다.
      if (value.Length > 0 && char.IsDigit(value[0]))
        throw new JsonException($"Unknown ScenarioEntityStateOperationKind '{value}'.");

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioEntityStateOperationKind parsed)
          && Enum.IsDefined(typeof(ScenarioEntityStateOperationKind), parsed))
        return parsed;

      throw new JsonException($"Unknown ScenarioEntityStateOperationKind '{value}'.");
    }

    public static string SaveToJson(ScenarioGraph graph, bool validateWithSchema = true)
    {
      if (graph == null)
      {
        throw new ArgumentNullException(nameof(graph));
      }

      var dto = ToDTO(graph);
      var json = JsonSerializer.Serialize(dto, SerializerOptions);

      if (validateWithSchema)
      {
        ScenarioJsonSchemaValidator.Validate(json);
      }

      return json;
    }

    private static ScenarioGraphDTO ToDTO(ScenarioGraph graph)
    {
      if (graph == null)
      {
        throw new ArgumentNullException(nameof(graph));
      }

      var dto = new ScenarioGraphDTO
      {
        Identifier = string.IsNullOrWhiteSpace(graph.Identifier) ? "scenario_graph" : graph.Identifier.Trim(),
        Tags = NormalizeTags(graph.Tags).ToList(),
        QuestDefinitionIncludes = NormalizeQuestDefinitionIncludes(graph.QuestDefinitionIncludes).ToList(),
        ActingNpcs = ConvertActingNpcsToDTO(graph.ActingNpcs),
        DefaultEntrypoint = string.IsNullOrWhiteSpace(graph.DefaultEntrypoint) ? null : graph.DefaultEntrypoint.Trim(),
        Nodes = new Dictionary<string, ScenarioNodeDTO>()
      };

      foreach (var node in graph.Nodes.Values.OrderBy(value => value?.Identifier ?? string.Empty, StringComparer.Ordinal))
      {
        dto.Nodes[node.Identifier] = ConvertToDTO(node);
      }

      return dto;
    }

    private static List<ScenarioActingNpcDefinitionDTO> ConvertActingNpcsToDTO(
      IReadOnlyList<ScenarioActingNpcDefinition> actingNpcs)
    {
      if (actingNpcs == null || actingNpcs.Count == 0)
        return null;

      return actingNpcs.Where(value => value != null).Select(actingNpc => new ScenarioActingNpcDefinitionDTO
      {
        Identifier = actingNpc.Identifier,
        ActingNpcType = actingNpc.ActingNpcType.ToString(),
        PresetIdentifier = actingNpc.PresetIdentifier,
        DisplayName = actingNpc.DisplayName,
        PositionX = actingNpc.PositionX,
        PositionY = actingNpc.PositionY,
        PositionZ = actingNpc.PositionZ,
        RotationX = actingNpc.RotationX,
        RotationY = actingNpc.RotationY,
        RotationZ = actingNpc.RotationZ,
        SpawnOnStart = actingNpc.SpawnOnStart,
        DespawnOnScenarioEnd = actingNpc.DespawnOnScenarioEnd,
        Interactions = actingNpc.Interactions == null
          ? null
          : actingNpc.Interactions.Where(value => value != null).Select(interaction =>
            new ScenarioActingNpcInteractionDefinitionDTO
            {
              Identifier = interaction.Identifier,
              InteractionType = interaction.InteractionType.ToString(),
              DisplayText = interaction.DisplayText,
              IconIdentifier = interaction.IconIdentifier,
              ScenarioIdentifier = interaction.ScenarioIdentifier,
              ScenarioStartNodeIdentifier = interaction.ScenarioStartNodeIdentifier,
              Title = interaction.Title,
              SubmitButtonText = interaction.SubmitButtonText,
              RequiredItems = interaction.RequiredItems == null
                ? null
                : interaction.RequiredItems.Where(value => value != null).Select(value =>
                  new ScenarioActingNpcItemRequirementDTO
                  {
                    ItemIdentifier = value.ItemIdentifier,
                    Count = value.Count
                  }).ToList(),
              CompletionSignalIdentifier = interaction.CompletionSignalIdentifier,
              ConsumeOnce = interaction.ConsumeOnce,
              Enabled = interaction.Enabled
            }).ToList()
      }).ToList();
    }

    private static ScenarioNodeDTO ConvertToDTO(IScenarioNode node) =>
        node switch
        {
          ScenarioDialogueNode dialogue => ConvertToDTO(dialogue),
          ScenarioDisinteractableDialogueNode dialogue => ConvertToDTO(dialogue),
          ScenarioChoiceNode choice => ConvertToDTO(choice),
          ScenarioSoundNode sound => ConvertToDTO(sound),
          ScenarioPlayerMoveNode move => ConvertToDTO(move),
          ScenarioNPCMoveNode npcMove => ConvertToDTO(npcMove),
          ScenarioCameraTargetNode camera => ConvertToDTO(camera),
          ScenarioInvokeEventNode invoke => ConvertToDTO(invoke),
          ScenarioServerInternalSignalNode internalSignal => ConvertToDTO(internalSignal),
          ScenarioSignalListenerNode signalListener => ConvertToDTO(signalListener),
          ScenarioEntityStateSignalBindingNode stateBinding => ConvertToDTO(stateBinding),
          ScenarioSignalCounterNode signalCounter => ConvertToDTO(signalCounter),
          ScenarioValidatorNode validator => ConvertToDTO(validator),
          ScenarioParallelNode parallel => ConvertToDTO(parallel),
          ScenarioQuestControlNode questControl => ConvertToDTO(questControl),
          ScenarioQuestWaypointHighlightNode waypointHighlight => ConvertToDTO(waypointHighlight),
          ScenarioDelayNode delay => ConvertToDTO(delay),
          ScenarioInteractionNode interaction => ConvertToDTO(interaction),
          ScenarioCombineItemNode combineItem => ConvertToDTO(combineItem),
          ScenarioQuizNode quiz => ConvertToDTO(quiz),
          ScenarioStateUpdateNode stateUpdate => ConvertToDTO(stateUpdate),
          ScenarioPlayTTSNode playTTS => ConvertToDTO(playTTS),
          ScenarioPlayerTagNode playerTag => ConvertToDTO(playerTag),
          ScenarioEntityPresetSpawnNode entityPresetSpawn => ConvertToDTO(entityPresetSpawn),
          ScenarioEntityTagNode entityTag => ConvertToDTO(entityTag),
          ScenarioEntityInitNode entityInit => ConvertToDTO(entityInit),
          ScenarioTriageAssessControlNode triageAssess => ConvertToDTO(triageAssess),
          ScenarioPatientMedicalStatePresetNode patientPreset => ConvertToDTO(patientPreset),
          ScenarioItemSubmissionConfigNode itemSubmission => ConvertToDTO(itemSubmission),
          ScenarioNpcInteractControlNode npcInteractControl => ConvertToDTO(npcInteractControl),
          ScenarioChatPrintNode chatPrint => ConvertToDTO(chatPrint),
          ScenarioExecuteCommandNode executeCommand => ConvertToDTO(executeCommand),
          ScenarioTimeControlNode timeControl => ConvertToDTO(timeControl),
          _ => throw new JsonException($"Unsupported scenario node type '{node.GetType().Name}'.")
        };

    private static ScenarioDisinteractableDialogueNodeDTO ConvertToDTO(ScenarioDisinteractableDialogueNode node) =>
        new ScenarioDisinteractableDialogueNodeDTO
        {
          NodeType = "DisinteractableDialogue",
          Identifier = node.Identifier,
          SpeakerName = node.SpeakerName,
          DialogueContent = node.DialogueContent,
          PortraitSpriteIdentifier = node.PortraitSpriteIdentifier,
          FadeInDuration = node.FadeInDuration,
          DisplayDuration = node.DisplayDuration,
          FadeOutDuration = node.FadeOutDuration,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioDialogueNodeDTO ConvertToDTO(ScenarioDialogueNode node) =>
        new ScenarioDialogueNodeDTO
        {
          NodeType = "Dialogue",
          Identifier = node.Identifier,
          SpeakerName = node.SpeakerName,
          DialogueContent = node.DialogueContent,
          PortraitSpriteIdentifier = node.PortraitSpriteIdentifier,
          AutoAdvanceSeconds = node.AutoAdvanceSeconds,
          InteractionRequired = node.InteractionRequired ? true : (bool?)null,
          PlayTTS = node.PlayTTS ? true : (bool?)null,
          TtsVoiceIdentifier = string.IsNullOrEmpty(node.TtsVoiceIdentifier) ? null : node.TtsVoiceIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioChoiceNodeDTO ConvertToDTO(ScenarioChoiceNode node)
    {
      var dto = new ScenarioChoiceNodeDTO
      {
        NodeType = "Choice",
        Identifier = node.Identifier,
        SpeakerName = node.SpeakerName,
        DialogueContent = node.DialogueContent,
        PortraitSpriteIdentifier = node.PortraitSpriteIdentifier,
        PlayTTS = node.PlayTTS ? true : (bool?)null,
        TtsVoiceIdentifier = string.IsNullOrEmpty(node.TtsVoiceIdentifier) ? null : node.TtsVoiceIdentifier,
        NextIdentifier = null,
        Options = new List<ScenarioChoiceOptionDTO>()
      };

      foreach (var option in node.Options)
      {
        dto.Options.Add(new ScenarioChoiceOptionDTO
        {
          DisplayText = option.DisplayText,
          DisplayIconIdentifier = option.DisplayIconIdentifier,
          DisplayColor = new ScenarioColorDTO { R = option.DisplayColor.r, G = option.DisplayColor.g, B = option.DisplayColor.b, A = option.DisplayColor.a },
          NextNodeIdentifier = option.NextNodeIdentifier
        });
      }

      return dto;
    }

    private static ScenarioSoundNodeDTO ConvertToDTO(ScenarioSoundNode node) =>
        new ScenarioSoundNodeDTO
        {
          NodeType = "Sound",
          Identifier = node.Identifier,
          SoundResourceIdentifier = node.SoundResourceIdentifier,
          WaitUntilFinished = node.WaitUntilFinished,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioQuestControlNodeDTO ConvertToDTO(ScenarioQuestControlNode node) =>
        new ScenarioQuestControlNodeDTO
        {
          NodeType = "QuestControl",
          Identifier = node.Identifier,
          Operation = node.Operation.ToString(),
          FailureStrategy = node.FailureStrategy.ToString(),
          QuestDefinitionIdentifier = node.QuestDefinitionIdentifier,
          Quest = node.Quest,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioQuestWaypointHighlightNodeDTO ConvertToDTO(ScenarioQuestWaypointHighlightNode node) =>
        new ScenarioQuestWaypointHighlightNodeDTO
        {
          NodeType = "QuestWaypointHighlight",
          Identifier = node.Identifier,
          WaypointIdentifier = node.WaypointIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioPlayerMoveNodeDTO ConvertToDTO(ScenarioPlayerMoveNode node) =>
        new ScenarioPlayerMoveNodeDTO
        {
          NodeType = "PlayerMove",
          Identifier = node.Identifier,
          DestinationType = node.DestinationType.ToString(),
          DestinationIdentifier = node.DestinationIdentifier,
          DestinationX = node.DestinationX,
          DestinationY = node.DestinationY,
          DestinationZ = node.DestinationZ,
          IgnoreGroundCheck = node.IgnoreGroundCheck,
          MoveMode = node.MoveMode.ToString(),
          MoveSpeed = node.MoveSpeed,
          MoveDuration = node.MoveDuration,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioNPCMoveNodeDTO ConvertToDTO(ScenarioNPCMoveNode node) =>
        new ScenarioNPCMoveNodeDTO
        {
          NodeType = "NPCMove",
          Identifier = node.Identifier,
          NPCIdentifier = node.NPCIdentifier,
          DestinationType = node.DestinationType.ToString(),
          DestinationIdentifier = node.DestinationIdentifier,
          DestinationX = node.DestinationX,
          DestinationY = node.DestinationY,
          DestinationZ = node.DestinationZ,
          IgnoreGroundCheck = node.IgnoreGroundCheck,
          MoveMode = node.MoveMode.ToString(),
          MoveSpeed = node.MoveSpeed,
          MoveDuration = node.MoveDuration,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioCameraTargetNodeDTO ConvertToDTO(ScenarioCameraTargetNode node) =>
        new ScenarioCameraTargetNodeDTO
        {
          NodeType = "CameraTarget",
          Identifier = node.Identifier,
          TargetObjectIdentifier = node.TargetObjectIdentifier,
          OffsetX = node.OffsetX,
          OffsetY = node.OffsetY,
          OffsetZ = node.OffsetZ,
          BlendTime = node.BlendTime,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioInvokeEventNodeDTO ConvertToDTO(ScenarioInvokeEventNode node) =>
        new ScenarioInvokeEventNodeDTO
        {
          NodeType = "InvokeEvent",
          Identifier = node.Identifier,
          EventIdentifier = node.EventIdentifier,
          MoveNextBehavior = node.MoveNextBehavior.ToString(),
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioServerInternalSignalNodeDTO ConvertToDTO(ScenarioServerInternalSignalNode node) =>
        new ScenarioServerInternalSignalNodeDTO
        {
          NodeType = "ServerInternalSignal",
          Identifier = node.Identifier,
          TargetIdentifier = node.TargetIdentifier,
          SignalIdentifier = node.SignalIdentifier,
          Operation = node.Operation.ToString(),
          WaitForResolution = node.WaitForResolution,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioSignalListenerNodeDTO ConvertToDTO(ScenarioSignalListenerNode node) =>
        new ScenarioSignalListenerNodeDTO
        {
          NodeType = "SignalListener", Identifier = node.Identifier, ListenerIdentifier = node.ListenerIdentifier,
          Operation = node.Operation.ToString(), SourceSignalIdentifier = node.SourceSignalIdentifier,
          OutputSignalIdentifier = node.OutputSignalIdentifier,
          RequiredSignalIdentifiers = node.RequiredSignalIdentifiers?.ToList() ?? new List<string>(),
          // 기본값(true)일 때만 필드를 생략하고, false 는 명시적으로 기록한다.
          // (읽기 측 `dto.ConsumeOnce ?? true` 와 짝을 이뤄 false 가 라운드트립되도록 한다.)
          ConsumeOnce = node.ConsumeOnce ? (bool?)null : false, NextIdentifier = node.NextIdentifier
        };

    private static ScenarioEntityStateSignalBindingNodeDTO ConvertToDTO(ScenarioEntityStateSignalBindingNode node) =>
        new ScenarioEntityStateSignalBindingNodeDTO
        {
          NodeType = "EntityStateSignalBinding",
          Identifier = node.Identifier,
          NextIdentifier = node.NextIdentifier,
          BindingIdentifier = node.BindingIdentifier,
          Operation = node.Operation.ToString(),
          TargetEntityIdentifier = node.TargetEntityIdentifier,
          TargetEntityStateKey = node.TargetEntityStateKey,
          EventName = node.EventName,
          EventKey = node.EventKey,
          OutputSignalIdentifier = node.OutputSignalIdentifier,
          // 기본값(false)일 때 생략, true 는 명시 기록(읽기 측 `?? false` 와 짝).
          ConsumeOnce = node.ConsumeOnce ? true : (bool?)null,
        };

    private static ScenarioSignalCounterNodeDTO ConvertToDTO(ScenarioSignalCounterNode node) =>
        new ScenarioSignalCounterNodeDTO
        {
          NodeType = "SignalCounter",
          Identifier = node.Identifier,
          NextIdentifier = node.NextIdentifier,
          CounterIdentifier = node.CounterIdentifier,
          Operation = node.Operation.ToString(),
          SourceSignalPrefix = node.SourceSignalPrefix,
          Threshold = node.Threshold,
          OutputSignalIdentifier = node.OutputSignalIdentifier,
        };

    private static ScenarioChatPrintNodeDTO ConvertToDTO(ScenarioChatPrintNode node) =>
        new ScenarioChatPrintNodeDTO
        {
          NodeType = "ChatPrint",
          Identifier = node.Identifier,
          Message = node.Message,
          Targets = node.Targets != ScenarioChatPrintTarget.InGameChat
              ? node.Targets.ToString()
              : null,
          Broadcast = node.Broadcast ? true : (bool?)null,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioExecuteCommandNodeDTO ConvertToDTO(ScenarioExecuteCommandNode node) =>
        new ScenarioExecuteCommandNodeDTO
        {
          NodeType = "ExecuteCommand",
          Identifier = node.Identifier,
          CommandLine = node.CommandLine,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioValidatorNodeDTO ConvertToDTO(ScenarioValidatorNode node) =>
        new ScenarioValidatorNodeDTO
        {
          NodeType = "Validator",
          Identifier = node.Identifier,
          RootConditions = ConvertValidatorRootConditionsToDTO(node.RootConditions),
          OnFailure = node.OnFailure.ToString(),
          FailureReportTargets = node.FailureReportTargets.ToString(),
          FailureNextIdentifier = node.FailureNextIdentifier,
          WaitForCondition = node.WaitForCondition ? true : (bool?)null,
          WaitTimeoutSeconds = (node.WaitTimeoutSeconds is > 0f) ? node.WaitTimeoutSeconds : null,
          OnWaitTimeout = node.OnWaitTimeout != ScenarioValidatorWaitTimeoutBehavior.KeepWaiting
              ? node.OnWaitTimeout.ToString()
              : null,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioDelayNodeDTO ConvertToDTO(ScenarioDelayNode node) =>
        new ScenarioDelayNodeDTO
        {
          NodeType = "Delay",
          Identifier = node.Identifier,
          Duration = node.Duration,
          WaitUntil = node.WaitUntil.ToString(),
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioTimeControlNodeDTO ConvertToDTO(ScenarioTimeControlNode node) =>
        new ScenarioTimeControlNodeDTO
        {
          NodeType = "TimeControl",
          Identifier = node.Identifier,
          Operation = node.Operation.ToString(),
          TimerId = node.TimerId,
          Direction = node.Direction.ToString(),
          DurationSeconds = node.DurationSeconds,
          StartSeconds = node.StartSeconds,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioInteractionNodeDTO ConvertToDTO(ScenarioInteractionNode node) =>
        new ScenarioInteractionNodeDTO
        {
          NodeType = "Interaction",
          Identifier = node.Identifier,
          ActorScope = node.ActorScope.ToString(),
          TargetIdentifier = node.TargetIdentifier,
          RequiredItemIdentifier = node.RequiredItemIdentifier,
          InteractionType = node.InteractionType.ToString(),
          CompletionConditionIdentifier = node.CompletionConditionIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioCombineItemNodeDTO ConvertToDTO(ScenarioCombineItemNode node) =>
        new ScenarioCombineItemNodeDTO
        {
          NodeType = "CombineItem",
          Identifier = node.Identifier,
          InputItemIdentifiers = node.InputItemIdentifiers?.ToList() ?? new List<string>(),
          OutputItemIdentifier = node.OutputItemIdentifier,
          AutoCombine = node.AutoCombine,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioQuizNodeDTO ConvertToDTO(ScenarioQuizNode node) =>
        new ScenarioQuizNodeDTO
        {
          NodeType = "Quiz",
          Identifier = node.Identifier,
          Question = node.Question,
          Options = node.Options?.ToList() ?? new List<string>(),
          CorrectIndex = node.CorrectIndex,
          OnCorrectNextIdentifier = node.OnCorrectNextIdentifier,
          OnIncorrectNextIdentifier = node.OnIncorrectNextIdentifier,
          FeedbackCorrect = node.FeedbackCorrect,
          FeedbackIncorrect = node.FeedbackIncorrect,
          PlayTTS = node.PlayTTS ? true : (bool?)null,
          TtsVoiceIdentifier = string.IsNullOrEmpty(node.TtsVoiceIdentifier) ? null : node.TtsVoiceIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioStateUpdateNodeDTO ConvertToDTO(ScenarioStateUpdateNode node) =>
        new ScenarioStateUpdateNodeDTO
        {
          NodeType = "StateUpdate",
          Identifier = node.Identifier,
          TargetEntityIdentifier = node.TargetEntityIdentifier,
          StateKey = node.StateKey,
          StateValue = node.StateValue,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioTriageAssessControlNodeDTO ConvertToDTO(ScenarioTriageAssessControlNode node) =>
        new ScenarioTriageAssessControlNodeDTO
        {
          NodeType = "TriageAssessControl",
          Identifier = node.Identifier,
          TargetEntityIdentifier = node.TargetEntityIdentifier,
          Assessable = node.Assessable,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioInvokeEventMoveNextBehavior ParseInvokeEventMoveNext(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioInvokeEventMoveNextBehavior.WaitUntilDone;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioInvokeEventMoveNextBehavior parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioInvokeEventMoveNextBehavior '{value}'.");
    }

    private static ScenarioServerInternalSignalOperationType ParseServerInternalSignalOperation(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioServerInternalSignalOperationType.Register;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioServerInternalSignalOperationType parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioServerInternalSignalOperationType '{value}'.");
    }

    private static ScenarioValidatorCondition ParseValidatorCondition(string value)
    {
      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorCondition parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorCondition '{value}'.");
    }

    private static ScenarioValidatorOnFailure ParseValidatorOnFailure(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorOnFailure.Panic;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorOnFailure parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorOnFailure '{value}'.");
    }

    private static ScenarioValidatorWaitTimeoutBehavior ParseValidatorWaitTimeoutBehavior(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorWaitTimeoutBehavior.KeepWaiting;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorWaitTimeoutBehavior parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorWaitTimeoutBehavior '{value}'.");
    }

    private static RegistryType ParseValidatorRegistryType(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return RegistryType.Waypoint;
      }

      if (Enum.TryParse(value, ignoreCase: true, out RegistryType parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown validator registry type '{value}'.");
    }

    private static ScenarioValidatorPlayerScope ParseValidatorPlayerScope(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorPlayerScope.Any;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorPlayerScope parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorPlayerScope '{value}'.");
    }

    private static ScenarioValidatorFailureReportTarget ParseValidatorFailureReportTargets(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorFailureReportTarget.UnityConsole;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorFailureReportTarget parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorFailureReportTarget '{value}'.");
    }

    private static ScenarioChatPrintTarget ParseChatPrintTarget(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioChatPrintTarget.InGameChat;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioChatPrintTarget parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioChatPrintTarget '{value}'.");
    }

    private static IReadOnlyList<ScenarioValidatorRootCondition> ParseValidatorRootConditions(List<ScenarioValidatorNodeDTO.ScenarioValidatorRootConditionDTO> rootConditions)
    {
      if (rootConditions == null || rootConditions.Count == 0)
      {
        return Array.Empty<ScenarioValidatorRootCondition>();
      }

      var parsed = new List<ScenarioValidatorRootCondition>(rootConditions.Count);
      foreach (var each in rootConditions)
      {
        if (each == null)
        {
          continue;
        }

        parsed.Add(new ScenarioValidatorRootCondition
        {
          Condition = ParseValidatorCondition(each.Condition),
          TargetCount = each.TargetCount ?? 0,
          PlayerTag = each.PlayerTag,
          PlayerScope = ParseValidatorPlayerScope(each.PlayerScope),
          ValidationRules = ParseValidatorRules(each.ValidationRules),
          MatchMode = ParseValidatorMatchMode(each.MatchMode)
        });
      }

      return parsed;
    }

    private static List<ScenarioValidatorNodeDTO.ScenarioValidatorRootConditionDTO> ConvertValidatorRootConditionsToDTO(IReadOnlyList<ScenarioValidatorRootCondition> rootConditions)
    {
      if (rootConditions == null || rootConditions.Count == 0)
      {
        return null;
      }

      var dtoConditions = new List<ScenarioValidatorNodeDTO.ScenarioValidatorRootConditionDTO>(rootConditions.Count);
      foreach (var each in rootConditions)
      {
        if (each == null)
        {
          continue;
        }

        dtoConditions.Add(new ScenarioValidatorNodeDTO.ScenarioValidatorRootConditionDTO
        {
          Condition = each.Condition.ToString(),
          TargetCount = each.TargetCount,
          PlayerTag = each.PlayerTag,
          PlayerScope = each.PlayerScope.ToString(),
          ValidationRules = ConvertValidatorRulesToDTO(each.ValidationRules),
          MatchMode = each.MatchMode == ScenarioValidatorMatchMode.All ? null : each.MatchMode.ToString()
        });
      }

      return dtoConditions;
    }

    private static ScenarioValidatorMatchMode ParseValidatorMatchMode(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorMatchMode.All;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorMatchMode parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorMatchMode '{value}'.");
    }

    private static IReadOnlyList<ScenarioValidatorRule> ParseValidatorRules(List<ScenarioValidatorNodeDTO.ScenarioValidatorRuleDTO> rules)
    {
      if (rules == null || rules.Count == 0)
      {
        return Array.Empty<ScenarioValidatorRule>();
      }

      var parsed = new List<ScenarioValidatorRule>(rules.Count);
      foreach (var each in rules)
      {
        if (each == null)
        {
          continue;
        }

        parsed.Add(new ScenarioValidatorRule
        {
          Type = ParseValidatorRuleType(each.Type),
          Condition = ParseValidatorRuleCondition(each.Condition),
          RegistryType = ParseValidatorRegistryType(each.RegistryType),
          RegistryIdentifier = each.RegistryIdentifier
        });
      }

      return parsed;
    }

    private static List<ScenarioValidatorNodeDTO.ScenarioValidatorRuleDTO> ConvertValidatorRulesToDTO(IReadOnlyList<ScenarioValidatorRule> rules)
    {
      if (rules == null || rules.Count == 0)
      {
        return null;
      }

      var dtoRules = new List<ScenarioValidatorNodeDTO.ScenarioValidatorRuleDTO>(rules.Count);
      foreach (var each in rules)
      {
        if (each == null)
        {
          continue;
        }

        dtoRules.Add(new ScenarioValidatorNodeDTO.ScenarioValidatorRuleDTO
        {
          Type = each.Type.ToString(),
          Condition = each.Condition.ToString(),
          RegistryType = each.RegistryType.ToString(),
          RegistryIdentifier = each.RegistryIdentifier
        });
      }

      return dtoRules;
    }

    private static ScenarioValidatorRuleType ParseValidatorRuleType(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorRuleType.Registry;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorRuleType parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorRuleType '{value}'.");
    }

    private static ScenarioValidatorRuleCondition ParseValidatorRuleCondition(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioValidatorRuleCondition.Contains;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioValidatorRuleCondition parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioValidatorRuleCondition '{value}'.");
    }

    private static ScenarioParallelNodeDTO ConvertToDTO(ScenarioParallelNode node)
    {
      var dto = new ScenarioParallelNodeDTO
      {
        NodeType = "Parallel",
        Identifier = node.Identifier,
        WaitMode = node.WaitMode.ToString(),
        AllocationType = node.AllocationType.ToString(),
        WhenBranchingPlayerNotMatched = node.WhenBranchingPlayerNotMatched.ToString(),
        Branches = new List<ScenarioParallelBranchDTO>(),
        NextIdentifier = node.NextIdentifier
      };

      foreach (var branch in node.Branches)
      {
        dto.Branches.Add(new ScenarioParallelBranchDTO
        {
          Identifier = branch.Identifier,
          CompletionConditionIdentifier = branch.CompletionConditionIdentifier,
          RequiredPlayerTags = branch.RequiredPlayerTags?.ToList() ?? new List<string>(),
          ForbiddenPlayerTags = branch.ForbiddenPlayerTags?.ToList() ?? new List<string>(),
          RequiredPlayerTagsMatchMode = branch.RequiredPlayerTagsMatchMode.ToString()
        });
      }

      return dto;
    }

    private static ScenarioParallelAllocationType ParseParallelAllocationType(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioParallelAllocationType.SelfAll;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioParallelAllocationType parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioParallelAllocationType '{value}'.");
    }

    private static ScenarioParallelMismatchHandling ParseParallelMismatchHandling(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioParallelMismatchHandling.Panic;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioParallelMismatchHandling parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioParallelMismatchHandling '{value}'.");
    }

    private static ScenarioPlayerTagMatchMode ParsePlayerTagMatchMode(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioPlayerTagMatchMode.All;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioPlayerTagMatchMode parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioPlayerTagMatchMode '{value}'.");
    }

    // ── PatientMedicalStatePreset 변환 ──

    /// <summary>
    /// DTO → 도메인 노드 변환.
    /// 모든 의료 상태 필드는 optional이므로, JSON에서 생략된 항목은 null 그대로 전달되어
    /// <c>PatientController.ApplyMedicalStatePreset</c> 에서 현재 값을 유지한다.
    ///
    /// <para>새 PatientDescriptor / PatientMedicalState 필드 추가 시 이 메서드에도
    /// 매핑 줄을 추가한다.</para>
    /// </summary>
    private static ScenarioPatientMedicalStatePresetNode ConvertPatientMedicalStatePreset(
        ScenarioPatientMedicalStatePresetNodeDTO dto)
    {
      return new ScenarioPatientMedicalStatePresetNode
      {
        Identifier = dto.Identifier,
        NextIdentifier = dto.NextIdentifier,

        // 대상 엔티티
        TargetEntityIdentifier = dto.TargetEntityIdentifier,
        TargetEntityStateKey = dto.TargetEntityStateKey,

        // 전이(Transition)
        TransitionMode = ParseOptionalEnum<PatientMedicalStateTransitionMode>(dto.TransitionMode)
                         ?? PatientMedicalStateTransitionMode.Immediate,
        TransitionDurationSeconds = dto.TransitionDurationSeconds ?? 0f,

        // 환자 기술자
        Name = dto.Name,
        Sex = ParseOptionalEnum<Sex>(dto.Sex),
        Age = dto.Age,
        BloodType = ParseOptionalEnum<BloodType>(dto.BloodType),
        IntendedTriage = ParseOptionalEnum<TriageLevel>(dto.IntendedTriage),

        // 의식
        ConsciousnessGcs = dto.ConsciousnessGcs,
        ConsciousnessEyeOpening = ParseOptionalEnum<EyeOpeningResponse>(dto.ConsciousnessEyeOpening),
        ConsciousnessVerbal = ParseOptionalEnum<VerbalResponse>(dto.ConsciousnessVerbal),
        ConsciousnessMotor = ParseOptionalEnum<MotorResponse>(dto.ConsciousnessMotor),
        ConsciousnessLocLabel = ParseOptionalEnum<LOCLabel>(dto.ConsciousnessLocLabel),
        ConsciousnessPupillaryResponse = ParseOptionalEnum<PupillaryResponse>(dto.ConsciousnessPupillaryResponse),

        // 호흡
        RespirationAwRR = dto.RespirationAwRR,
        RespirationTypeValue = ParseOptionalEnum<RespirationType>(dto.RespirationTypeValue),

        // 맥박
        PulseRate = dto.PulseRate,
        PulseForceType = ParseOptionalEnum<BloodPulseForceType>(dto.PulseForceType),

        // 혈압
        BloodPressureSystolic = dto.BloodPressureSystolic,
        BloodPressureDiastolic = dto.BloodPressureDiastolic,

        // 피부
        SkinColorHue = ParseOptionalEnum<SkinColorHue>(dto.SkinColorHue),
        SkinTemperatureType = ParseOptionalEnum<SkinTemperatureType>(dto.SkinTemperatureType),

        // 체온 / 산소포화도
        BodyTemperatureCelsius = dto.BodyTemperatureCelsius,
        Spo2 = dto.Spo2,

        // 기타
        IsCardiacArrest = dto.IsCardiacArrest,
      };
    }

    /// <summary>
    /// 도메인 노드 → DTO 변환.
    /// null 인 항목은 DTO에도 null 로 직렬화되어 JSON에서 생략된다.
    /// </summary>
    private static ScenarioPatientMedicalStatePresetNodeDTO ConvertToDTO(ScenarioPatientMedicalStatePresetNode node)
    {
      return new ScenarioPatientMedicalStatePresetNodeDTO
      {
        NodeType = "PatientMedicalStatePreset",
        Identifier = node.Identifier,
        NextIdentifier = node.NextIdentifier,

        // 대상 엔티티
        TargetEntityIdentifier = node.TargetEntityIdentifier,
        TargetEntityStateKey = node.TargetEntityStateKey,

        // 전이(Transition)
        // Immediate(기본값)는 JSON에서 생략한다. Gradual일 때만 소요 시간과 함께 직렬화한다.
        TransitionMode = node.TransitionMode == PatientMedicalStateTransitionMode.Immediate
                         ? null
                         : node.TransitionMode.ToString(),
        TransitionDurationSeconds = node.TransitionMode == PatientMedicalStateTransitionMode.Gradual
                         ? node.TransitionDurationSeconds
                         : (float?)null,

        // 환자 기술자
        Name = node.Name,
        Sex = node.Sex?.ToString(),
        Age = node.Age,
        BloodType = node.BloodType?.ToString(),
        IntendedTriage = node.IntendedTriage?.ToString(),

        // 의식
        ConsciousnessGcs = node.ConsciousnessGcs,
        ConsciousnessEyeOpening = node.ConsciousnessEyeOpening?.ToString(),
        ConsciousnessVerbal = node.ConsciousnessVerbal?.ToString(),
        ConsciousnessMotor = node.ConsciousnessMotor?.ToString(),
        ConsciousnessLocLabel = node.ConsciousnessLocLabel?.ToString(),
        ConsciousnessPupillaryResponse = node.ConsciousnessPupillaryResponse?.ToString(),

        // 호흡
        RespirationAwRR = node.RespirationAwRR,
        RespirationTypeValue = node.RespirationTypeValue?.ToString(),

        // 맥박
        PulseRate = node.PulseRate,
        PulseForceType = node.PulseForceType?.ToString(),

        // 혈압
        BloodPressureSystolic = node.BloodPressureSystolic,
        BloodPressureDiastolic = node.BloodPressureDiastolic,

        // 피부
        SkinColorHue = node.SkinColorHue?.ToString(),
        SkinTemperatureType = node.SkinTemperatureType?.ToString(),

        // 체온 / 산소포화도
        BodyTemperatureCelsius = node.BodyTemperatureCelsius,
        Spo2 = node.Spo2,

        // 기타
        IsCardiacArrest = node.IsCardiacArrest,
      };
    }

    /// <summary>
    /// 문자열을 nullable 열거형으로 파싱한다.
    /// null 또는 공백이면 null 반환. 파싱 불가 문자열은 예외를 발생시킨다.
    /// </summary>
    private static TEnum? ParseOptionalEnum<TEnum>(string value) where TEnum : struct, Enum
    {
      if (string.IsNullOrWhiteSpace(value))
        return null;

      if (Enum.TryParse(value, ignoreCase: true, out TEnum parsed) && Enum.IsDefined(typeof(TEnum), parsed))
        return parsed;

      throw new JsonException($"Unknown {typeof(TEnum).Name} value '{value}'.");
    }
  }
}
