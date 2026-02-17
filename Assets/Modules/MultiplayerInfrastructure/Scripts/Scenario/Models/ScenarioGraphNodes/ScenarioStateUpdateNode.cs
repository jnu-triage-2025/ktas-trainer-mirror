namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioStateUpdateNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.StateUpdate;
    public string NextIdentifier { get; set; }

    public string TargetEntityIdentifier { get; set; }
    public string StateKey { get; set; }
    public string StateValue { get; set; }
  }
}
