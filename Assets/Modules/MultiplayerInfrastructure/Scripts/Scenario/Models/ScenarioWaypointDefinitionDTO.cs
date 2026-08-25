using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioWaypointDefinitionDTO
  {
    [JsonPropertyName("identifier")] public string Identifier { get; set; }
    [JsonPropertyName("positionX")] public float? PositionX { get; set; }
    [JsonPropertyName("positionY")] public float? PositionY { get; set; }
    [JsonPropertyName("positionZ")] public float? PositionZ { get; set; }
    [JsonPropertyName("rotationX")] public float? RotationX { get; set; }
    [JsonPropertyName("rotationY")] public float? RotationY { get; set; }
    [JsonPropertyName("rotationZ")] public float? RotationZ { get; set; }
    [JsonPropertyName("despawnOnScenarioEnd")] public bool? DespawnOnScenarioEnd { get; set; }
  }
}
