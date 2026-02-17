using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioParallelBranchDTO
  {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("completionConditionIdentifier")]
    public string CompletionConditionIdentifier { get; set; }

    [JsonPropertyName("requiredRoleIdentifiers")]
    public List<string> RequiredRoleIdentifiers { get; set; }
  }
}
