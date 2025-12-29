using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioValidatorNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("condition")]
    public string Condition { get; set; }

    [JsonPropertyName("targetCount")]
    public int? TargetCount { get; set; }

    [JsonPropertyName("onFailure")]
    public string OnFailure { get; set; }

    [JsonPropertyName("failureNextIdentifier")]
    public string FailureNextIdentifier { get; set; }
  }
}
