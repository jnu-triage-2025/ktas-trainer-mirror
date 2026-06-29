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
    public bool InteractionRequired { get; set; }

    /// <summary>
    /// 자동 진행까지 대기할 시간(초). null/0 이하이면 기존처럼 사용자 입력을 기다린다(하위호환).
    /// 0 보다 크면 표시 후 해당 시간 경과 시 자동으로 다음 노드로 진행하며,
    /// 그 전에 사용자가 진행 입력을 주면 즉시 진행하고 타이머는 취소된다.
    /// </summary>
    public float? AutoAdvanceSeconds { get; set; }
  }
}
