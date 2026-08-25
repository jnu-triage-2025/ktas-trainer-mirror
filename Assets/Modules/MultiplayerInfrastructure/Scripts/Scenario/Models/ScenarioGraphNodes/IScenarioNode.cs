namespace MultiplayerInfrastructure.Scenario
{
  public interface IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType { get; }
    public string NextIdentifier { get; set; }
  }
}
