using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioCameraTargetNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("targetObjectIdentifier")]
    public string TargetObjectIdentifier { get; set; }

    [JsonPropertyName("offsetX")]
    public float? OffsetX { get; set; }

    [JsonPropertyName("offsetY")]
    public float? OffsetY { get; set; }

    [JsonPropertyName("offsetZ")]
    public float? OffsetZ { get; set; }

    [JsonPropertyName("blendTime")]
    public float? BlendTime { get; set; }
  }
}
