namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioServerInternalSignalNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ServerInternalSignal;
    public string NextIdentifier { get; set; }

    public string TargetIdentifier { get; set; } = ScenarioServerInternalSignalRegistry.ServerTarget;
    public string SignalIdentifier { get; set; }
    public ScenarioServerInternalSignalOperationType Operation { get; set; } = ScenarioServerInternalSignalOperationType.Register;
    public bool WaitForResolution { get; set; } = true;
  }
}
