namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioDisinteractableDialogueNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.DisinteractableDialogue;
    public string NextIdentifier { get; set; }

    public string SpeakerName { get; set; }
    public string DialogueContent { get; set; }
    public string PortraitSpriteIdentifier { get; set; }
    public ScenarioTimeValue FadeInDuration { get; set; } = ScenarioTimeValue.Seconds(0.5d);
    public ScenarioTimeValue DisplayDuration { get; set; } = ScenarioTimeValue.Seconds(1.5d);
    public ScenarioTimeValue FadeOutDuration { get; set; } = ScenarioTimeValue.Seconds(0.5d);
  }
}
