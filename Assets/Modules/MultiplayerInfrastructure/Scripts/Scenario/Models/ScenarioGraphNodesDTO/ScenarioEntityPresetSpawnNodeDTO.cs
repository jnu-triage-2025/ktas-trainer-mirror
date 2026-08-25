using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioEntityPresetSpawnNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("presetIdentifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string PresetIdentifier { get; set; }

    [JsonPropertyName("actingNpcIdentifier")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
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

    [JsonPropertyName("rotationX")]
    public float? RotationX { get; set; }

    [JsonPropertyName("rotationY")]
    public float? RotationY { get; set; }

    [JsonPropertyName("rotationZ")]
    public float? RotationZ { get; set; }

    [JsonPropertyName("resultStateKey")]
    public string ResultStateKey { get; set; }
  }
}
