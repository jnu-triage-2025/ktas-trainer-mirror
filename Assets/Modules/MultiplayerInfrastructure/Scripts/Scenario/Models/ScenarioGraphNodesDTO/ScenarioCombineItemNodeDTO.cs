using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioCombineItemNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("inputItemIdentifiers")]
    public List<string> InputItemIdentifiers { get; set; }

    [JsonPropertyName("outputItemIdentifier")]
    public string OutputItemIdentifier { get; set; }

    [JsonPropertyName("autoCombine")]
    public bool? AutoCombine { get; set; }
  }
}
