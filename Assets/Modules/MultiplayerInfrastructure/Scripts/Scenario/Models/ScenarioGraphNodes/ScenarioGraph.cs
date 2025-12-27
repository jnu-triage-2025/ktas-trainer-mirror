using System;
using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioGraph
  {
    public Dictionary<string, IScenarioNode> Nodes { get; } = new Dictionary<string, IScenarioNode>();

    public void Add(IScenarioNode node)
    {
      if (node == null) throw new ArgumentNullException(nameof(node));
      Nodes[node.Identifier] = node;
    }

    public bool TryGetNode(string identifier, out IScenarioNode node)
      => Nodes.TryGetValue(identifier, out node);
  }
}
