namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioInvokeEventNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.InvokeEvent;
    public string NextIdentifier { get; set; }

    public string EventIdentifier { get; set; }

    /// <summary>
    /// Controls when to move to NextIdentifier after invoking the event.
    /// False: never moves automatically, Immediately: move right after firing, WaitUntilDone: wait for handler completion.
    /// </summary>
    public ScenarioInvokeEventMoveNextBehavior MoveNextBehavior { get; set; } = ScenarioInvokeEventMoveNextBehavior.WaitUntilDone;
  }
}
