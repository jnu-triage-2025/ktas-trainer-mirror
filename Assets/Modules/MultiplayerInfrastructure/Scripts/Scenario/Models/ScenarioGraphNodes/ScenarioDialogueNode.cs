namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioDialogueNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Dialogue;
    public string NextIdentifier { get; set; }

    public string SpeakerName { get; set; }
    public string DialogueContent { get; set; }
    public string PortraitSpriteIdentifier { get; set; }
  }
}
