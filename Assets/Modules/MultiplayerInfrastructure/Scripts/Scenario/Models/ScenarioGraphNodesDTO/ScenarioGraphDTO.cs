using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioGraphDTO
  {
    [JsonPropertyName("nodes")]
    public Dictionary<string, ScenarioNodeDTO> Nodes { get; set; }
  }
}
