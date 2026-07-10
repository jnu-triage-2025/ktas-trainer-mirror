using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioTimeControlNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; }

    [JsonPropertyName("durationSeconds")]
    public float? DurationSeconds { get; set; }

    [JsonPropertyName("startSeconds")]
    public float? StartSeconds { get; set; }
  }
}
