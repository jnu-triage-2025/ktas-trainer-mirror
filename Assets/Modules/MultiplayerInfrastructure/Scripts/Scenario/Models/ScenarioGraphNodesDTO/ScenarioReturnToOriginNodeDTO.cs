using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioReturnToOriginNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("description")]
    public string Description { get; set; }
  }
}
