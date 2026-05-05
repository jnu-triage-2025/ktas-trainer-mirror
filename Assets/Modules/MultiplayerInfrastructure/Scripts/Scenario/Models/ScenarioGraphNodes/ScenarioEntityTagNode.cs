namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioEntityTagNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.EntityTag;
    public string NextIdentifier { get; set; }

    public ScenarioPlayerTagOperationType Operation { get; set; } = ScenarioPlayerTagOperationType.Add;
    public string TargetEntityIdentifier { get; set; }
    public string TargetEntityStateKey { get; set; }
    public string Tag { get; set; }
    public string FromTag { get; set; }
    public string ToTag { get; set; }
  }
}
