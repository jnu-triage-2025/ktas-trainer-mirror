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
        "Choice" => Deserialize<ScenarioChoiceNodeDTO>(root, options),
        "Sound" => Deserialize<ScenarioSoundNodeDTO>(root, options),
        "PlayerMove" => Deserialize<ScenarioPlayerMoveNodeDTO>(root, options),
        "CameraTarget" => Deserialize<ScenarioCameraTargetNodeDTO>(root, options),
        "InvokeEvent" => Deserialize<ScenarioInvokeEventNodeDTO>(root, options),
        "Validator" => Deserialize<ScenarioValidatorNodeDTO>(root, options),
        "Parallel" => Deserialize<ScenarioParallelNodeDTO>(root, options),
        _ => throw new JsonException($"Unknown nodeType '{nodeType}'.")
      };
    }

    public override void Write(Utf8JsonWriter writer, ScenarioNodeDTO value, JsonSerializerOptions options)
    {
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
