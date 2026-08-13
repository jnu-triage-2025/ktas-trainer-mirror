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
    public string QuestDefinitionIdentifier { get; set; }
    public QuestData Quest { get; set; }

    /// <summary>
    /// true이면 목표 완료 연출을 기다리지 않고 이 변경을 HUD에 즉시 표시합니다.
    /// </summary>
    public bool SkipCompletionDisplayDelay { get; set; }

    /// <summary>
    /// null이면 QuestDefinition/인라인 QuestData의 설정을 사용합니다.
    /// 지정하면 해당 QuestControl 실행으로 생성·갱신되는 퀘스트의 세션 종료 후 진행 유지 여부를 덮어씁니다.
    /// </summary>
    public bool? PersistProgressOnSessionEnd { get; set; }
  }
}
