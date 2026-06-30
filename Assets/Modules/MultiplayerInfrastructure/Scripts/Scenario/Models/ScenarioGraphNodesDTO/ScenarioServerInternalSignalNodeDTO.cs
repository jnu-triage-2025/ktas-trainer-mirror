using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioServerInternalSignalNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("targetIdentifier")]
    public string TargetIdentifier { get; set; }

    [JsonPropertyName("signalIdentifier")]
    public string SignalIdentifier { get; set; }

    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("waitForResolution")]
    public bool? WaitForResolution { get; set; }
  }
}