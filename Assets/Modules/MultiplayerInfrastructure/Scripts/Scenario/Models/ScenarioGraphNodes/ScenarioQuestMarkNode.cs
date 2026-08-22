using MultiplayerInfrastructure.Quest;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 퀘스트 마크(상호작용 아이콘 대체 / NPC 머리 위 아이콘)를 시나리오 그래프에서 명시적으로 켜고 끈다.
  /// 퀘스트 정의의 presentationBindings 와 동일한 표시 경로(<see cref="QuestPresentationService"/>)를 사용하므로
  /// 대상 종류·아이콘·우선순위 규칙이 퀘스트가 만든 마크와 같다. 표시 수명이 퀘스트 수명과 다를 때 사용한다.
  /// </summary>
  public sealed class ScenarioQuestMarkNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.QuestMark;
    public string NextIdentifier { get; set; }

    public ScenarioQuestMarkOperationType Operation { get; set; } = ScenarioQuestMarkOperationType.Show;

    /// <summary>마크를 붙일 대상 종류. NPC는 머리 위 아이콘, Interaction은 상호작용 힌트의 기본 아이콘을 대체한다.</summary>
    public QuestPresentationTargetType TargetType { get; set; } = QuestPresentationTargetType.Npc;

    /// <summary>대상 엔티티 식별자. NPC 대상은 Npc 레지스트리 식별자를 사용한다.</summary>
    public string EntityIdentifier { get; set; }

    /// <summary><see cref="TargetType"/>이 Interaction일 때 엔티티 내부의 상호작용 행을 구분하는 식별자.</summary>
    public string InteractionIdentifier { get; set; }

    /// <summary>비우면 대상 종류별 기본 퀘스트 마크 아이콘을 사용한다.</summary>
    public string IconIdentifier { get; set; }

    /// <summary>같은 대상에 여러 마크가 겹칠 때의 우선순위. 값이 클수록 우선한다.</summary>
    public int Priority { get; set; }
  }
}
