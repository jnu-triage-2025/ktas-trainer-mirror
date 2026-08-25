using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioParallelBranch
  {
    public string Identifier { get; set; }
    public string CompletionConditionIdentifier { get; set; }
    public IReadOnlyList<string> RequiredPlayerTags { get; set; }
    public IReadOnlyList<string> ForbiddenPlayerTags { get; set; }
    public ScenarioPlayerTagMatchMode RequiredPlayerTagsMatchMode { get; set; } = ScenarioPlayerTagMatchMode.All;
  }
}
