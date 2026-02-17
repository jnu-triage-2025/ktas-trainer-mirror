using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioInteractionNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("actorScope")]
    public string ActorScope { get; set; }

    [JsonPropertyName("targetIdentifier")]
    public string TargetIdentifier { get; set; }

    [JsonPropertyName("requiredItemIdentifier")]
    public string RequiredItemIdentifier { get; set; }

    [JsonPropertyName("interactionType")]
    public string InteractionType { get; set; }

    [JsonPropertyName("completionConditionIdentifier")]
    public string CompletionConditionIdentifier { get; set; }
  }
}
