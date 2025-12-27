namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioPlayerMoveNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.PlayerMove;
    public string NextIdentifier { get; set; }

    public ScenarioMoveDestinationType DestinationType { get; set; }
    public string DestinationIdentifier { get; set; }
    public float DestinationX { get; set; }
    public float DestinationY { get; set; }
    public float DestinationZ { get; set; }

    public bool IgnoreGroundCheck { get; set; }
    public ScenarioMoveMode MoveMode { get; set; } = ScenarioMoveMode.BySpeed;
    public float MoveSpeed { get; set; }
    public float MoveDuration { get; set; }
  }
}
