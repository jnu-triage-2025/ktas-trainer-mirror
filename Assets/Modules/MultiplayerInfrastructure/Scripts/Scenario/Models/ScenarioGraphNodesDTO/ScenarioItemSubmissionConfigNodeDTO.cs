using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioItemRequirementDTO
  {
    [JsonPropertyName("itemIdentifier")]
    public string ItemIdentifier { get; set; }

    [JsonPropertyName("count")]
    public int? Count { get; set; }
  }

  internal sealed class ScenarioItemSubmissionConfigNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("presetIdentifier")]
    public string PresetIdentifier { get; set; }

    [JsonPropertyName("spawnedEntityIdentifier")]
    public string SpawnedEntityIdentifier { get; set; }

    [JsonPropertyName("positionSourceEntityIdentifier")]
    public string PositionSourceEntityIdentifier { get; set; }

    [JsonPropertyName("positionX")]
    public float? PositionX { get; set; }

    [JsonPropertyName("positionY")]
    public float? PositionY { get; set; }

    [JsonPropertyName("positionZ")]
    public float? PositionZ { get; set; }

    [JsonPropertyName("targetIdentifier")]
    public string TargetIdentifier { get; set; }

    [JsonPropertyName("targetStateKey")]
    public string TargetStateKey { get; set; }

    [JsonPropertyName("requiredItems")]
    public List<ScenarioItemRequirementDTO> RequiredItems { get; set; }

    [JsonPropertyName("completionSignalIdentifier")]
    public string CompletionSignalIdentifier { get; set; }

    [JsonPropertyName("enabled")]
    public bool? Enabled { get; set; }

    [JsonPropertyName("resultStateKey")]
    public string ResultStateKey { get; set; }
  }
}
