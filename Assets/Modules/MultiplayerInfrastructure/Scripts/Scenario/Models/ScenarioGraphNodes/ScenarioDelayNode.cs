namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioDelayNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Delay;
    public string NextIdentifier { get; set; }

    /// <summary>대기 시간. 원본 단위(Tick/Milliseconds/Seconds)를 보존한다.</summary>
    public ScenarioTimeValue Duration { get; set; } = ScenarioTimeValue.Seconds(0d);
    public ScenarioDelayWaitUntil WaitUntil { get; set; } = ScenarioDelayWaitUntil.WaitUntilDone;
  }
}
