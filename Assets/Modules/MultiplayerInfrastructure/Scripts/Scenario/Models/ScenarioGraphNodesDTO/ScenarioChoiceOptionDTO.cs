using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioChoiceOptionDTO
  {
    [JsonPropertyName("displayText")]
    public string DisplayText { get; set; }

    [JsonPropertyName("displayIconIdentifier")]
    public string DisplayIconIdentifier { get; set; }

    [JsonPropertyName("displayColor")]
    public ScenarioColorDTO DisplayColor { get; set; }

    [JsonPropertyName("nextNodeIdentifier")]
    public string NextNodeIdentifier { get; set; }
  }
}
