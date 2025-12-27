using System;
using System.Collections.Generic;
using System.Text.Json;
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
        AllowTrailingCommas = true
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

    private static IScenarioNode ConvertNode(ScenarioNodeDTO dto) =>
        dto switch
        {
          ScenarioDialogueNodeDTO dialogue => ConvertDialogue(dialogue),
          ScenarioChoiceNodeDTO choice => ConvertChoice(choice),
          ScenarioSoundNodeDTO sound => ConvertSound(sound),
          ScenarioPlayerMoveNodeDTO move => ConvertPlayerMove(move),
          ScenarioCameraTargetNodeDTO camera => ConvertCameraTarget(camera),
          ScenarioParallelNodeDTO parallel => ConvertParallel(parallel),
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
            CompletionConditionIdentifier = branchDTO.CompletionConditionIdentifier
          });
        }
      }

      return new ScenarioParallelNode
      {
        Identifier = dto.Identifier,
        WaitMode = ParseWaitMode(dto.WaitMode),
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
  }
}
