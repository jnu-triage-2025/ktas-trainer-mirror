using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioEntityTagNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("targetEntityIdentifier")]
    public string TargetEntityIdentifier { get; set; }

    [JsonPropertyName("targetEntityStateKey")]
    public string TargetEntityStateKey { get; set; }

    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    [JsonPropertyName("fromTag")]
    public string FromTag { get; set; }

    [JsonPropertyName("toTag")]
    public string ToTag { get; set; }
  }
}
