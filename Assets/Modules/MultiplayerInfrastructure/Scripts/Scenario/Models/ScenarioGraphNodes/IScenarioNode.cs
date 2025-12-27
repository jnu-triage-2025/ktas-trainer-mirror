namespace MultiplayerInfrastructure.Scenario
{
  public interface IScenarioNode
  {
    string Identifier { get; set; }
    ScenarioNodeType NodeType { get; }
    string NextIdentifier { get; set; }
  }
}
