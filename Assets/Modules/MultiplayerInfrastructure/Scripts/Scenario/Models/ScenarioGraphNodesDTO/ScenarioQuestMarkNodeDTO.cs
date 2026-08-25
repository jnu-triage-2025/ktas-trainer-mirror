using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioQuestMarkNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("targetType")]
    public string TargetType { get; set; }

    [JsonPropertyName("entityIdentifier")]
    public string EntityIdentifier { get; set; }

    [JsonPropertyName("interactionIdentifier")]
    public string InteractionIdentifier { get; set; }

    [JsonPropertyName("iconIdentifier")]
    public string IconIdentifier { get; set; }

    [JsonPropertyName("priority")]
    public int? Priority { get; set; }
  }
}
