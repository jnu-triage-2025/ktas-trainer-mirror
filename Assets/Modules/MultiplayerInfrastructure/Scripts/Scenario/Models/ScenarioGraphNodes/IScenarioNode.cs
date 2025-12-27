namespace MultiplayerInfrastructure.Scenario
{
  public interface IScenarioNode
  {
    string Identifier { get; }
    ScenarioNodeType NodeType { get; }
    string NextIdentifier { get; }
  }
}
