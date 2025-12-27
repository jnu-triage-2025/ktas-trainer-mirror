namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioSoundNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Sound;
    public string NextIdentifier { get; set; }

    public string SoundResourceIdentifier { get; set; }
    public bool WaitUntilFinished { get; set; }
  }
}
