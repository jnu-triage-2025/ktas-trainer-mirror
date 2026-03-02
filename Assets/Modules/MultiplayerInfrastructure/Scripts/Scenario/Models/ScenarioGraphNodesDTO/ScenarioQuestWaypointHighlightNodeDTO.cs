using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioQuestWaypointHighlightNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("waypointIdentifier")]
    public string WaypointIdentifier { get; set; }
  }
}
