using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioBedSnapNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("bedEntityIdentifier")]
    public string BedEntityIdentifier { get; set; }

    [JsonPropertyName("bedEntityStateKey")]
    public string BedEntityStateKey { get; set; }

    [JsonPropertyName("snapPointIdentifier")]
    public string SnapPointIdentifier { get; set; }

    [JsonPropertyName("teleport")]
    public bool? Teleport { get; set; }

    [JsonPropertyName("ignoreFailure")]
    public bool? IgnoreFailure { get; set; }
  }
}
