using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioGraphDTO
  {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("nodes")]
    public Dictionary<string, ScenarioNodeDTO> Nodes { get; set; }
  }
}
