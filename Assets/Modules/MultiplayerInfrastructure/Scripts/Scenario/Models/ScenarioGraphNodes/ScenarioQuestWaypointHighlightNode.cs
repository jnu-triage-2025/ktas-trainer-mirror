namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioQuestWaypointHighlightNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.QuestWaypointHighlight;
    public string NextIdentifier { get; set; }
    public string WaypointIdentifier { get; set; }
  }
}
