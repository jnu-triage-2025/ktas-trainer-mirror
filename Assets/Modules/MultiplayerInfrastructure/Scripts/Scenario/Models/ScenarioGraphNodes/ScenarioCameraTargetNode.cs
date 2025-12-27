namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioCameraTargetNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.CameraTarget;
    public string NextIdentifier { get; set; }

    public string TargetObjectIdentifier { get; set; }
    public float OffsetX { get; set; } = 0f;
    public float OffsetY { get; set; } = 0f;
    public float OffsetZ { get; set; } = 0f;

    public float BlendTime { get; set; } = 1f;
  }
}
