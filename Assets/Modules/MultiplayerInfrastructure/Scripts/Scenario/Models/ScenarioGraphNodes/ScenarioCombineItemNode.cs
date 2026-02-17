using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioCombineItemNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.CombineItem;
    public string NextIdentifier { get; set; }

    public IReadOnlyList<string> InputItemIdentifiers { get; set; }
    public string OutputItemIdentifier { get; set; }
    public bool AutoCombine { get; set; }
  }
}
