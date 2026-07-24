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

    [JsonPropertyName("questDefinitionIncludes")]
    public List<string> QuestDefinitionIncludes { get; set; }

    [JsonPropertyName("defaultEntrypoint")]
    public string DefaultEntrypoint { get; set; }

    [JsonPropertyName("actingNpcs")]
    public List<ScenarioActingNpcDefinitionDTO> ActingNpcs { get; set; }

    [JsonPropertyName("waypoints")]
    public List<ScenarioWaypointDefinitionDTO> Waypoints { get; set; }

    [JsonPropertyName("nodes")]
    public Dictionary<string, ScenarioNodeDTO> Nodes { get; set; }
  }
}
