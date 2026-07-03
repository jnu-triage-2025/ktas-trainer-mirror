using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioTriageAssessControlNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("targetEntityIdentifier")]
    public string TargetEntityIdentifier { get; set; }

    [JsonPropertyName("assessable")]
    public bool Assessable { get; set; }
  }
}
