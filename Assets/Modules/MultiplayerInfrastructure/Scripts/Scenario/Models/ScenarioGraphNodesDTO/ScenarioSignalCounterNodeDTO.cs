using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary><see cref="ScenarioSignalCounterNode"/> 의 JSON 직렬화 DTO.</summary>
  internal sealed class ScenarioSignalCounterNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("counterIdentifier")] public string CounterIdentifier { get; set; }
    [JsonPropertyName("operation")] public string Operation { get; set; }
    [JsonPropertyName("sourceSignalPrefix")] public string SourceSignalPrefix { get; set; }
    [JsonPropertyName("threshold")] public int? Threshold { get; set; }
    [JsonPropertyName("useActiveRoleRosterThreshold")] public bool? UseActiveRoleRosterThreshold { get; set; }
    [JsonPropertyName("outputSignalIdentifier")] public string OutputSignalIdentifier { get; set; }
  }
}
