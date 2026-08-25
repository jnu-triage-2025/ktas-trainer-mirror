using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioParallelNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("waitMode")]
    public string WaitMode { get; set; }

    [JsonPropertyName("branches")]
    public List<ScenarioParallelBranchDTO> Branches { get; set; }

    [JsonPropertyName("allocationType")]
    public string AllocationType { get; set; }

    [JsonPropertyName("whenBranchingPlayerNotMatched")]
    public string WhenBranchingPlayerNotMatched { get; set; }
  }
}
