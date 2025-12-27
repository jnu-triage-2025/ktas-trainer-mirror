

using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  [JsonConverter(typeof(ScenarioNodeDTOConverter))]
  internal abstract class ScenarioNodeDTO
  {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("nodeType")]
    public string NodeType { get; set; }

    [JsonPropertyName("nextIdentifier")]
    public string NextIdentifier { get; set; }
  }
}
