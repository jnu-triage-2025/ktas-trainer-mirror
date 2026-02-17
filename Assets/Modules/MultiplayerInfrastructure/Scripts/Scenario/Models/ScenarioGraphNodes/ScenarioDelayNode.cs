namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioDelayNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Delay;
    public string NextIdentifier { get; set; }

    public float DurationSeconds { get; set; }
    public ScenarioDelayWaitUntil WaitUntil { get; set; } = ScenarioDelayWaitUntil.WaitUntilDone;
  }
}
