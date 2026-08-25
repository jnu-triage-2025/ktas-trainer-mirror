using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioDelayNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("duration")]
    public ScenarioTimeValue? Duration { get; set; }

    [JsonPropertyName("waitUntil")]
    public string WaitUntil { get; set; }
  }
}
