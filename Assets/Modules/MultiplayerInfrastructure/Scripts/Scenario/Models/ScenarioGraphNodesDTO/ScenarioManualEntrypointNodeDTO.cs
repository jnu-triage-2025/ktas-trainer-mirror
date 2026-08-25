using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioManualEntrypointNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("entrypointIdentifier")]
    public string EntrypointIdentifier { get; set; }

    [JsonPropertyName("manualEnterSetupIdentifier")]
    public string ManualEnterSetupIdentifier { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }
  }
}
