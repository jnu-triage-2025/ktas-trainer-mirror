using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioEntityPresetSpawnNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("presetIdentifier")]
    public string PresetIdentifier { get; set; }

    [JsonPropertyName("actingNpcIdentifier")]
    public string ActingNpcIdentifier { get; set; }

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

    [JsonPropertyName("resultStateKey")]
    public string ResultStateKey { get; set; }
  }
}
