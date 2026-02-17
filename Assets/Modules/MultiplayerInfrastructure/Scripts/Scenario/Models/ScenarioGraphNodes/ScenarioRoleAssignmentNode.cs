using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioRoleAssignmentNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.RoleAssignment;
    public string NextIdentifier { get; set; }

    public IReadOnlyList<string> RoleOptions { get; set; }
    public ScenarioRoleAssignmentMode AssignmentMode { get; set; } = ScenarioRoleAssignmentMode.Select;
  }
}
