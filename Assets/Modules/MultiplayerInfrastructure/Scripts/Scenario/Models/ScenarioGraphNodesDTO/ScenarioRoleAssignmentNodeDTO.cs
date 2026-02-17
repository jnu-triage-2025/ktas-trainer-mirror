using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioRoleAssignmentNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("roleOptions")]
    public List<string> RoleOptions { get; set; }

    [JsonPropertyName("assignmentMode")]
    public string AssignmentMode { get; set; }
  }
}
