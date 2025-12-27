using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioChoiceNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Choice;
    public string NextIdentifier { get; set; }

    public string SpeakerName { get; set; }
    public string DialogueContent { get; set; }
    public string PortraitSpriteIdentifier { get; set; }

    public List<ScenarioChoiceOption> Options { get; set; }
  }
}
