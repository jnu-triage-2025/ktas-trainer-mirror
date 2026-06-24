using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using UnityEngine;
using MultiplayerInfrastructure.Registry;

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

      WarnForUndeclaredTags(graph);

      return graph;
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

      foreach (var node in graph.Nodes.Values)
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
          ScenarioChoiceNodeDTO choice => ConvertChoice(choice),
          ScenarioSoundNodeDTO sound => ConvertSound(sound),
          ScenarioPlayerMoveNodeDTO move => ConvertPlayerMove(move),
          ScenarioNPCMoveNodeDTO npcMove => ConvertNPCMove(npcMove),
          ScenarioCameraTargetNodeDTO camera => ConvertCameraTarget(camera),
          ScenarioInvokeEventNodeDTO invoke => ConvertInvokeEvent(invoke),
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
          _ => throw new JsonException($"Unsupported scenario node dto type '{dto.GetType().Name}'.")
        };

    private static ScenarioDialogueNode ConvertDialogue(ScenarioDialogueNodeDTO dto) =>
        new ScenarioDialogueNode
        {
          Identifier = dto.Identifier,
          SpeakerName = dto.SpeakerName,
          DialogueContent = dto.DialogueContent,
          PortraitSpriteIdentifier = dto.PortraitSpriteIdentifier,
          AutoAdvanceSeconds = dto.AutoAdvanceSeconds,
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

    private static ScenarioQuestControlNode ConvertQuestControl(ScenarioQuestControlNodeDTO dto) =>
        new ScenarioQuestControlNode
        {
          Identifier = dto.Identifier,
          Operation = ParseQuestOperation(dto.Operation),
          FailureStrategy = ParseQuestFailureStrategy(dto.FailureStrategy),
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
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioDelayNode ConvertDelay(ScenarioDelayNodeDTO dto) =>
        new ScenarioDelayNode
        {
          Identifier = dto.Identifier,
          DurationSeconds = dto.DurationSeconds ?? 0f,
          WaitUntil = ParseDelayWaitUntil(dto.WaitUntil),
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

    private static ScenarioPlayTTSNode ConvertPlayTTS(ScenarioPlayTTSNodeDTO dto) =>
        new ScenarioPlayTTSNode
        {
          Identifier = dto.Identifier,
          TranscriptIdentifier = dto.TranscriptIdentifier,
          Variables = dto.Variables ?? new Dictionary<string, string>(),
          WaitUntilFinished = dto.WaitUntilFinished ?? true,
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

    private static ScenarioEntityPresetSpawnNodeDTO ConvertToDTO(ScenarioEntityPresetSpawnNode node) =>
        new ScenarioEntityPresetSpawnNodeDTO
        {
          NodeType = "EntityPresetSpawn",
          Identifier = node.Identifier,
          PresetIdentifier = node.PresetIdentifier,
          SpawnedEntityIdentifier = node.SpawnedEntityIdentifier,
          PositionSourceEntityIdentifier = node.PositionSourceEntityIdentifier,
          PositionX = node.PositionX,
          PositionY = node.PositionY,
          PositionZ = node.PositionZ,
          ResultStateKey = node.ResultStateKey,
          NextIdentifier = node.NextIdentifier
        };

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
        Nodes = new Dictionary<string, ScenarioNodeDTO>()
      };

      foreach (var node in graph.Nodes.Values)
      {
        dto.Nodes[node.Identifier] = ConvertToDTO(node);
      }

      return dto;
    }

    private static ScenarioNodeDTO ConvertToDTO(IScenarioNode node) =>
        node switch
        {
          ScenarioDialogueNode dialogue => ConvertToDTO(dialogue),
          ScenarioChoiceNode choice => ConvertToDTO(choice),
          ScenarioSoundNode sound => ConvertToDTO(sound),
          ScenarioPlayerMoveNode move => ConvertToDTO(move),
          ScenarioNPCMoveNode npcMove => ConvertToDTO(npcMove),
          ScenarioCameraTargetNode camera => ConvertToDTO(camera),
          ScenarioInvokeEventNode invoke => ConvertToDTO(invoke),
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
          _ => throw new JsonException($"Unsupported scenario node type '{node.GetType().Name}'.")
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

    private static ScenarioValidatorNodeDTO ConvertToDTO(ScenarioValidatorNode node) =>
        new ScenarioValidatorNodeDTO
        {
          NodeType = "Validator",
          Identifier = node.Identifier,
          RootConditions = ConvertValidatorRootConditionsToDTO(node.RootConditions),
          OnFailure = node.OnFailure.ToString(),
          FailureReportTargets = node.FailureReportTargets.ToString(),
          FailureNextIdentifier = node.FailureNextIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioDelayNodeDTO ConvertToDTO(ScenarioDelayNode node) =>
        new ScenarioDelayNodeDTO
        {
          NodeType = "Delay",
          Identifier = node.Identifier,
          DurationSeconds = node.DurationSeconds,
          WaitUntil = node.WaitUntil.ToString(),
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
          ValidationRules = ParseValidatorRules(each.ValidationRules)
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
          ValidationRules = ConvertValidatorRulesToDTO(each.ValidationRules)
        });
      }

      return dtoConditions;
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
        NextIdentifier = node.NextIdentifier,
        Branches = new List<ScenarioParallelBranchDTO>()
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
  }
}
