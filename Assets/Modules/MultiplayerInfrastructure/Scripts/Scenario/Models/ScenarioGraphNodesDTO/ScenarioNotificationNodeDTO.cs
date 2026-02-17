using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioNotificationNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("displayMode")]
    public string DisplayMode { get; set; }

    [JsonPropertyName("duration")]
    public float? Duration { get; set; }
  }
}
