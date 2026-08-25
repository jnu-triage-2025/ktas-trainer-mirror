namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 특정 환자(엔티티)에 대해 트리아지(Triage) 평가 인터랙션을 활성화/비활성화하는 시나리오 노드.
  ///
  /// <para>
  /// 시나리오 진행 중 플레이어가 특정 환자를 트리아지 분류할 수 있는 시점을 제어하는 데 사용한다.
  /// 대상 엔티티는 레지스트리에서 <see cref="TargetEntityIdentifier"/> 로 조회되며, 해당 엔티티가
  /// 트리아지 평가 제어를 지원하는 대상(<see cref="Entity.IScenarioTriageAssessTarget"/>)이어야 한다.
  /// </para>
  /// </summary>
  public sealed class ScenarioTriageAssessControlNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.TriageAssessControl;
    public string NextIdentifier { get; set; }

    /// <summary>트리아지 평가를 제어할 대상 환자(엔티티) 식별자.</summary>
    public string TargetEntityIdentifier { get; set; }

    /// <summary>true 면 트리아지 평가 인터랙션을 활성화, false 면 비활성화한다.</summary>
    public bool Assessable { get; set; }
  }
}
