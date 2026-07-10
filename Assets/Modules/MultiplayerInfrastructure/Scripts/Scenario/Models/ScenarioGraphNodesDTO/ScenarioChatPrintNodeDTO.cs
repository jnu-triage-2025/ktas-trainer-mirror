using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioChatPrintNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("targets")]
    public string Targets { get; set; }

    [JsonPropertyName("broadcast")]
    public bool? Broadcast { get; set; }
  }
}
