using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioDelayNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("durationSeconds")]
    public float? DurationSeconds { get; set; }

    [JsonPropertyName("waitUntil")]
    public string WaitUntil { get; set; }
  }
}
