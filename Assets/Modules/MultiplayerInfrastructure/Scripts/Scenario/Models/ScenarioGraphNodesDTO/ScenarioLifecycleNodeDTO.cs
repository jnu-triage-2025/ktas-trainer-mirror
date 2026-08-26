using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioLifecycleNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("operation")] public string Operation { get; set; }
    [JsonPropertyName("revertTrackedChanges")] public bool? RevertTrackedChanges { get; set; }
    [JsonPropertyName("clearRuntimeState")] public bool? ClearRuntimeState { get; set; }
    [JsonPropertyName("restartEntrypointIdentifier")] public string RestartEntrypointIdentifier { get; set; }
  }
}
