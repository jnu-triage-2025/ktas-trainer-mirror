namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <see cref="ScenarioEntityInitNode"/> 가 엔티티에 적용하는 초기 상태 항목의 종류.
  /// </summary>
  public enum ScenarioEntityStateOperationKind
  {
    /// <summary>
    /// 시나리오 인메모리 상태 저장소(<c>_stateStore</c>)에 <c>"{entityIdentifier}.{key}" = value</c> 를 기록한다.
    /// 시나리오 그래프의 다른 노드가 참조하는 순수 데이터 상태.
    /// </summary>
    StateStore,

    /// <summary>
    /// 엔티티 컴포넌트(<see cref="MultiplayerInfrastructure.Entity.IScenarioEntityInitTarget"/> 구현체)의
    /// 명명된 표시/부착 상태를 설정한다. 환자 엔티티의 처치 부착물(주사기/거즈/경부보호대 등)
    /// 초기 표시 여부 설정이 1차 목표다. <see cref="ScenarioEntityStateOperation.DisplayActive"/> 로 표시/비표시를 정한다.
    /// </summary>
    DisplayState,
  }
}
