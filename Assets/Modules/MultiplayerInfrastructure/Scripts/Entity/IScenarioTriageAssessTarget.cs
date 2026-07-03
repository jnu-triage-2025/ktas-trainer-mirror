namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// 시나리오 그래프의 TriageAssessControl 노드가 트리아지 평가 인터랙션을 활성화/비활성화할 수 있는
  /// 엔티티가 구현하는 인터페이스.
  ///
  /// <para>
  /// 시나리오 컨트롤러는 식별자로 엔티티를 레지스트리에서 찾은 뒤 이 인터페이스를 통해 트리아지 평가
  /// 가능 여부를 제어한다. 실제 인터랙션 노출/게이팅 규칙은 구현체(예: TriageTrainer 의 PatientController)
  /// 책임이다.
  /// </para>
  ///
  /// 재사용성: 이 인터페이스 자체는 트리아지 도메인 구현에 의존하지 않는다("트리아지 평가 가능 플래그 대상").
  /// </summary>
  public interface IScenarioTriageAssessTarget
  {
    /// <summary>
    /// 트리아지 평가 인터랙션의 활성화 여부를 설정한다.
    /// </summary>
    /// <param name="assessable">활성화(true)/비활성화(false).</param>
    void SetTriageAssessable(bool assessable);
  }
}
