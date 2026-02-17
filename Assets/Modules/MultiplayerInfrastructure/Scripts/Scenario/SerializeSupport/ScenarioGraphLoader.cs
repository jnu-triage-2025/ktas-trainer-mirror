using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Encodings.Web;
using UnityEngine;

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

      return graph;
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
          ScenarioNotificationNodeDTO notification => ConvertNotification(notification),
          ScenarioDelayNodeDTO delay => ConvertDelay(delay),
          ScenarioInteractionNodeDTO interaction => ConvertInteraction(interaction),
          ScenarioCombineItemNodeDTO combineItem => ConvertCombineItem(combineItem),
          ScenarioQuizNodeDTO quiz => ConvertQuiz(quiz),
          ScenarioStateUpdateNodeDTO stateUpdate => ConvertStateUpdate(stateUpdate),
          ScenarioRoleAssignmentNodeDTO roleAssignment => ConvertRoleAssignment(roleAssignment),
          _ => throw new JsonException($"Unsupported scenario node dto type '{dto.GetType().Name}'.")
        };

    private static ScenarioDialogueNode ConvertDialogue(ScenarioDialogueNodeDTO dto) =>
        new ScenarioDialogueNode
        {
          Identifier = dto.Identifier,
          SpeakerName = dto.SpeakerName,
          DialogueContent = dto.DialogueContent,
          PortraitSpriteIdentifier = dto.PortraitSpriteIdentifier,
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

    private static ScenarioValidatorNode ConvertValidator(ScenarioValidatorNodeDTO dto) =>
        new ScenarioValidatorNode
        {
          Identifier = dto.Identifier,
          Condition = ParseValidatorCondition(dto.Condition),
          TargetCount = dto.TargetCount ?? 0,
          OnFailure = ParseValidatorOnFailure(dto.OnFailure),
          FailureNextIdentifier = dto.FailureNextIdentifier,
          NextIdentifier = dto.NextIdentifier
        };

    private static ScenarioNotificationNode ConvertNotification(ScenarioNotificationNodeDTO dto) =>
        new ScenarioNotificationNode
        {
          Identifier = dto.Identifier,
          Message = dto.Message,
          DisplayMode = ParseNotificationDisplayMode(dto.DisplayMode),
          Duration = dto.Duration,
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

    private static ScenarioRoleAssignmentNode ConvertRoleAssignment(ScenarioRoleAssignmentNodeDTO dto) =>
        new ScenarioRoleAssignmentNode
        {
          Identifier = dto.Identifier,
          RoleOptions = dto.RoleOptions ?? new List<string>(),
          AssignmentMode = ParseRoleAssignmentMode(dto.AssignmentMode),
          NextIdentifier = dto.NextIdentifier
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
            RequiredRoleIdentifiers = branchDTO.RequiredRoleIdentifiers ?? new List<string>()
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

    private static ScenarioNotificationDisplayMode ParseNotificationDisplayMode(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioNotificationDisplayMode.Overlay;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioNotificationDisplayMode parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioNotificationDisplayMode '{value}'.");
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

    private static ScenarioRoleAssignmentMode ParseRoleAssignmentMode(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
      {
        return ScenarioRoleAssignmentMode.Select;
      }

      if (Enum.TryParse(value, ignoreCase: true, out ScenarioRoleAssignmentMode parsed))
      {
        return parsed;
      }

      throw new JsonException($"Unknown ScenarioRoleAssignmentMode '{value}'.");
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
          ScenarioNotificationNode notification => ConvertToDTO(notification),
          ScenarioDelayNode delay => ConvertToDTO(delay),
          ScenarioInteractionNode interaction => ConvertToDTO(interaction),
          ScenarioCombineItemNode combineItem => ConvertToDTO(combineItem),
          ScenarioQuizNode quiz => ConvertToDTO(quiz),
          ScenarioStateUpdateNode stateUpdate => ConvertToDTO(stateUpdate),
          ScenarioRoleAssignmentNode roleAssignment => ConvertToDTO(roleAssignment),
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
          Condition = node.Condition.ToString(),
          TargetCount = node.TargetCount,
          OnFailure = node.OnFailure.ToString(),
          FailureNextIdentifier = node.FailureNextIdentifier,
          NextIdentifier = node.NextIdentifier
        };

    private static ScenarioNotificationNodeDTO ConvertToDTO(ScenarioNotificationNode node) =>
        new ScenarioNotificationNodeDTO
        {
          NodeType = "Notification",
          Identifier = node.Identifier,
          Message = node.Message,
          DisplayMode = node.DisplayMode.ToString(),
          Duration = node.Duration,
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

    private static ScenarioRoleAssignmentNodeDTO ConvertToDTO(ScenarioRoleAssignmentNode node) =>
        new ScenarioRoleAssignmentNodeDTO
        {
          NodeType = "RoleAssignment",
          Identifier = node.Identifier,
          RoleOptions = node.RoleOptions?.ToList() ?? new List<string>(),
          AssignmentMode = node.AssignmentMode.ToString(),
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
          RequiredRoleIdentifiers = branch.RequiredRoleIdentifiers?.ToList() ?? new List<string>()
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
  }
}
