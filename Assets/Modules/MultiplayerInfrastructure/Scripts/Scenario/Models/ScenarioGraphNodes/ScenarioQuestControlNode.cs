using MultiplayerInfrastructure.Quest;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioQuestControlNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.QuestControl;
    public string NextIdentifier { get; set; }

    public ScenarioQuestOperationType Operation { get; set; }
    public ScenarioQuestFailureStrategy FailureStrategy { get; set; } = ScenarioQuestFailureStrategy.Overwrite;
    public QuestData Quest { get; set; }
  }
}
