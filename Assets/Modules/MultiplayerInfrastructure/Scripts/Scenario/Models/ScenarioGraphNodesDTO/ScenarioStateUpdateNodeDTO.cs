using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioStateUpdateNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("targetEntityIdentifier")]
    public string TargetEntityIdentifier { get; set; }

    [JsonPropertyName("stateKey")]
    public string StateKey { get; set; }

    [JsonPropertyName("stateValue")]
    public string StateValue { get; set; }
  }
}
