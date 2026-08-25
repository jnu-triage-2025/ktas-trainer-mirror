namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// 시나리오 식별자를 노출하는 엔티티가 구현하는 범용 인터페이스.
  ///
  /// <para>
  /// 트리거 존(<c>ScenarioTriggerZone</c>) 등 "진입한 엔티티가 누구인지" 를 알아야 하는 컴포넌트가,
  /// 도메인 타입(예: PatientController)에 직접 의존하지 않고 엔티티의 시나리오 식별자를 얻기 위해 사용한다.
  /// 진입 콜라이더에서 <c>GetComponentInParent&lt;IScenarioIdentifiedEntity&gt;()</c> 로 조회한다.
  /// </para>
  ///
  /// <para>재사용성: 이 인터페이스는 어떤 도메인에도 의존하지 않는다. 식별자의 의미와 생성 방식은 구현체가 정한다.</para>
  /// </summary>
  public interface IScenarioIdentifiedEntity
  {
    /// <summary>이 엔티티의 시나리오 식별자(레지스트리 등록 식별자와 동일). 비어 있을 수 있다.</summary>
    public string ScenarioEntityIdentifier { get; }
  }
}
