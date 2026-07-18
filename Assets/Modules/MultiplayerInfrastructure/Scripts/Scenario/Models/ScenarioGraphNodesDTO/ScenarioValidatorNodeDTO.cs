using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioValidatorNodeDTO : ScenarioNodeDTO
  {
    internal sealed class ScenarioValidatorRootConditionDTO
    {
      [JsonPropertyName("condition")]
      public string Condition { get; set; }

      [JsonPropertyName("targetCount")]
      public int? TargetCount { get; set; }

      [JsonPropertyName("playerTag")]
      public string PlayerTag { get; set; }

      [JsonPropertyName("playerScope")]
      public string PlayerScope { get; set; }

      [JsonPropertyName("validationRules")]
      public System.Collections.Generic.List<ScenarioValidatorRuleDTO> ValidationRules { get; set; }

      [JsonPropertyName("matchMode")]
      public string MatchMode { get; set; }
    }

    internal sealed class ScenarioValidatorRuleDTO
    {
      [JsonPropertyName("type")]
      public string Type { get; set; }

      [JsonPropertyName("condition")]
      public string Condition { get; set; }

      [JsonPropertyName("registryType")]
      public string RegistryType { get; set; }

      [JsonPropertyName("registryIdentifier")]
      public string RegistryIdentifier { get; set; }
    }

    [JsonPropertyName("rootConditions")]
    public System.Collections.Generic.List<ScenarioValidatorRootConditionDTO> RootConditions { get; set; }

    [JsonPropertyName("onFailure")]
    public string OnFailure { get; set; }

    [JsonPropertyName("failureReportTargets")]
    public string FailureReportTargets { get; set; }

    [JsonPropertyName("failureNextIdentifier")]
    public string FailureNextIdentifier { get; set; }

    [JsonPropertyName("waitForCondition")]
    public bool? WaitForCondition { get; set; }

    [JsonPropertyName("waitTimeoutSeconds")]
    public float? WaitTimeoutSeconds { get; set; }

    [JsonPropertyName("onWaitTimeout")]
    public string OnWaitTimeout { get; set; }
  }
}
