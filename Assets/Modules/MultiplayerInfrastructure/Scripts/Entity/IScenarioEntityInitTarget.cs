namespace MultiplayerInfrastructure.Entity
{
  /// <summary>
  /// 시나리오 그래프의 EntityInit 노드가 초기 표시/부착 상태를 설정할 수 있는 엔티티가 구현하는 인터페이스.
  ///
  /// <para>
  /// 시나리오 컨트롤러는 식별자로 엔티티를 레지스트리에서 찾은 뒤, 그 GameObject 에서 이 인터페이스를
  /// 찾아 명명된 표시 상태(예: 환자의 처치 부착물)를 표시/비표시로 설정한다. 상태 이름의 해석과
  /// 실제 적용(자식 GameObject 토글 등)은 구현체 책임이다.
  /// </para>
  ///
  /// 재사용성: 이 인터페이스 자체는 시나리오/트리아지 도메인에 의존하지 않는다(범용 "명명된 표시 상태 대상").
  /// 환자 부착물 표현 같은 도메인 매핑은 구현체(예: TriageTrainer 의 PatientController) 책임이다.
  /// </summary>
  public interface IScenarioEntityInitTarget
  {
    /// <summary>
    /// 명명된 표시/부착 상태를 설정한다.
    /// 네트워크 환경에서는 내부적으로 서버 권한 RPC 를 통해 모든 피어에 전파한다.
    /// </summary>
    /// <param name="displayStateName">표시/부착 상태 이름(구현체가 해석).</param>
    /// <param name="active">표시(true)/비표시(false).</param>
    /// <returns>구현체가 이름을 인식하여 적용했으면 true.</returns>
    bool ApplyScenarioDisplayState(string displayStateName, bool active);

    /// <summary>
    /// 현재 설정된 모든 표시/부착 상태를 네트워크 전체에 일괄 동기화한다.
    /// 초기 상태 항목을 여러 개 연속 적용한 뒤 반드시 서버에서 호출해야
    /// 늦은 입장 클라이언트에 전체 상태가 정확히 전달된다.
    /// 네트워크 미사용(오프라인) 환경에서는 no-op 이어도 된다.
    /// </summary>
    void SyncAllDisplayStatesNetworked();
  }
}
