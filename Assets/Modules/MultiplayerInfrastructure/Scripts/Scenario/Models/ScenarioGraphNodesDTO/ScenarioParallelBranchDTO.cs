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

    [JsonPropertyName("requiredPlayerTags")]
    public List<string> RequiredPlayerTags { get; set; }

    [JsonPropertyName("forbiddenPlayerTags")]
    public List<string> ForbiddenPlayerTags { get; set; }

    [JsonPropertyName("requiredPlayerTagsMatchMode")]
    public string RequiredPlayerTagsMatchMode { get; set; }
  }
}
