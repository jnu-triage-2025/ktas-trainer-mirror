using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioInvokeEventNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("eventIdentifier")]
    public string EventIdentifier { get; set; }

    [JsonPropertyName("invokeOnRoleClient")]
    public bool? InvokeOnRoleClient { get; set; }

    [JsonPropertyName("moveNextBehavior")]
    public string MoveNextBehavior { get; set; }
  }
}
