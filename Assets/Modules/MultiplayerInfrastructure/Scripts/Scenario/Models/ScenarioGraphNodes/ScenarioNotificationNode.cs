namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioNotificationNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Notification;
    public string NextIdentifier { get; set; }

    public string Message { get; set; }
    public ScenarioNotificationDisplayMode DisplayMode { get; set; } = ScenarioNotificationDisplayMode.Overlay;
    public float? Duration { get; set; }
  }
}
