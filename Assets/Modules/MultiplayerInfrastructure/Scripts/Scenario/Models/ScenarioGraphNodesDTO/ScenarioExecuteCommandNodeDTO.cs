using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioExecuteCommandNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("commandLine")]
    public string CommandLine { get; set; }
  }
}
