using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// JSON의 nodeType 필드에 따라 구체 DTO 타입을 선택해 역직렬화합니다.
  /// </summary>
  internal sealed class ScenarioNodeDTOConverter : JsonConverter<ScenarioNodeDTO>
  {
    public override ScenarioNodeDTO Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      using var doc = JsonDocument.ParseValue(ref reader);
      var root = doc.RootElement;

      if (!root.TryGetProperty("nodeType", out var nodeTypeProp) ||
          nodeTypeProp.ValueKind != JsonValueKind.String)
      {
        throw new JsonException("Scenario node JSON is missing a string 'nodeType' property.");
      }

      var nodeType = nodeTypeProp.GetString();
      return nodeType switch
      {
        "Dialogue" => Deserialize<ScenarioDialogueNodeDTO>(root, options),
        "DisinteractableDialogue" => Deserialize<ScenarioDisinteractableDialogueNodeDTO>(root, options),
        "Choice" => Deserialize<ScenarioChoiceNodeDTO>(root, options),
        "Sound" => Deserialize<ScenarioSoundNodeDTO>(root, options),
        "PlayerMove" => Deserialize<ScenarioPlayerMoveNodeDTO>(root, options),
        "NPCMove" => Deserialize<ScenarioNPCMoveNodeDTO>(root, options),
        "CameraTarget" => Deserialize<ScenarioCameraTargetNodeDTO>(root, options),
        "InvokeEvent" => Deserialize<ScenarioInvokeEventNodeDTO>(root, options),
        "ServerInternalSignal" => Deserialize<ScenarioServerInternalSignalNodeDTO>(root, options),
        "SignalListener" => Deserialize<ScenarioSignalListenerNodeDTO>(root, options),
        "Validator" => Deserialize<ScenarioValidatorNodeDTO>(root, options),
        "Parallel" => Deserialize<ScenarioParallelNodeDTO>(root, options),
        "QuestControl" => Deserialize<ScenarioQuestControlNodeDTO>(root, options),
        "QuestWaypointHighlight" => Deserialize<ScenarioQuestWaypointHighlightNodeDTO>(root, options),
        "Delay" => Deserialize<ScenarioDelayNodeDTO>(root, options),
        "Interaction" => Deserialize<ScenarioInteractionNodeDTO>(root, options),
        "CombineItem" => Deserialize<ScenarioCombineItemNodeDTO>(root, options),
        "Quiz" => Deserialize<ScenarioQuizNodeDTO>(root, options),
        "StateUpdate" => Deserialize<ScenarioStateUpdateNodeDTO>(root, options),
        "PlayTTS" => Deserialize<ScenarioPlayTTSNodeDTO>(root, options),
        "PlayerTag" => Deserialize<ScenarioPlayerTagNodeDTO>(root, options),
        "TagModification" => Deserialize<ScenarioPlayerTagNodeDTO>(root, options),
        "EntityPresetSpawn" => Deserialize<ScenarioEntityPresetSpawnNodeDTO>(root, options),
        "EntityTag" => Deserialize<ScenarioEntityTagNodeDTO>(root, options),
        "EntityInit" => Deserialize<ScenarioEntityInitNodeDTO>(root, options),
        "TriageAssessControl" => Deserialize<ScenarioTriageAssessControlNodeDTO>(root, options),
        "PatientMedicalStatePreset" => Deserialize<ScenarioPatientMedicalStatePresetNodeDTO>(root, options),
        "ItemSubmissionConfig" => Deserialize<ScenarioItemSubmissionConfigNodeDTO>(root, options),
        "NpcInteractControl" => Deserialize<ScenarioNpcInteractControlNodeDTO>(root, options),
        "ChatPrint" => Deserialize<ScenarioChatPrintNodeDTO>(root, options),
        "ExecuteCommand" => Deserialize<ScenarioExecuteCommandNodeDTO>(root, options),
        "TimeControl" => Deserialize<ScenarioTimeControlNodeDTO>(root, options),
        _ => throw new JsonException($"Unknown nodeType '{nodeType}'.")
      };
    }

    public override void Write(Utf8JsonWriter writer, ScenarioNodeDTO value, JsonSerializerOptions options)
    {
      if (value is ScenarioParallelNodeDTO parallel)
      {
        writer.WriteStartObject();
        writer.WriteString("identifier", parallel.Identifier);
        writer.WriteString("nodeType", parallel.NodeType);

        if (!string.IsNullOrWhiteSpace(parallel.WaitMode))
        {
          writer.WriteString("waitMode", parallel.WaitMode);
        }

        if (!string.IsNullOrWhiteSpace(parallel.AllocationType))
        {
          writer.WriteString("allocationType", parallel.AllocationType);
        }

        if (!string.IsNullOrWhiteSpace(parallel.WhenBranchingPlayerNotMatched))
        {
          writer.WriteString("whenBranchingPlayerNotMatched", parallel.WhenBranchingPlayerNotMatched);
        }

        writer.WritePropertyName("branches");
        JsonSerializer.Serialize(writer, parallel.Branches, options);
        writer.WriteEndObject();
        return;
      }

      JsonSerializer.Serialize(writer, (object)value, value.GetType(), options);
    }

    private static T Deserialize<T>(JsonElement element, JsonSerializerOptions options)
    {
      var json = element.GetRawText();
      return JsonSerializer.Deserialize<T>(json, options)
             ?? throw new JsonException($"Failed to deserialize {typeof(T).Name}.");
    }
  }
}
