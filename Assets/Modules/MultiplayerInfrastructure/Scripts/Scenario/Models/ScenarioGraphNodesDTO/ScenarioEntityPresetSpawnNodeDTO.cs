using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioEntityChildDetachmentDTO
  {
    [JsonPropertyName("childPath")]
    public string ChildPath { get; set; }

    [JsonPropertyName("spawnedEntityIdentifier")]
    public string SpawnedEntityIdentifier { get; set; }
  }

  internal sealed class ScenarioEntityPresetSpawnNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("presetIdentifier")]
    public string PresetIdentifier { get; set; }

    [JsonPropertyName("spawnedEntityIdentifier")]
    public string SpawnedEntityIdentifier { get; set; }

    [JsonPropertyName("childDetachments")]
    public List<ScenarioEntityChildDetachmentDTO> ChildDetachments { get; set; }

    [JsonPropertyName("positionSourceEntityIdentifier")]
    public string PositionSourceEntityIdentifier { get; set; }

    [JsonPropertyName("positionX")]
    public float? PositionX { get; set; }

    [JsonPropertyName("positionY")]
    public float? PositionY { get; set; }

    [JsonPropertyName("positionZ")]
    public float? PositionZ { get; set; }

    [JsonPropertyName("resultStateKey")]
    public string ResultStateKey { get; set; }
  }
}
