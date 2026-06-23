namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 엔티티 프리셋 스폰 시, 컨테이너 프리팹의 특정 자식 <b>NetworkObject</b>를 루트로 분리(ungroup)하여
  /// 독립 엔티티로 스폰·등록하기 위한 지정.
  ///
  /// 분리 대상은 반드시 NetworkObject 여야 한다(MultiplayerInfrastructure/FishNet 으로 추적·제어되는
  /// 객체만 독립 루트로 안전하게 분리·복제 가능하기 때문). NetworkObject 가 아닌 자식은 분리하지 않는다.
  /// 지정되지 않은 자식 NetworkObject 는 루트의 위계에 그대로 남는다(자동 분리하지 않음).
  /// </summary>
  public sealed class ScenarioEntityChildDetachment
  {
    /// <summary>분리할 자식의 식별 경로/이름. 컨테이너 루트 기준 Transform 경로(예: "Bed") 또는 자식 오브젝트 이름.</summary>
    public string ChildPath { get; set; }

    /// <summary>분리된 인스턴스에 부여할 엔티티 식별자(예: "bed_a"). 비어 있으면 GUID 기반 식별자 부여.</summary>
    public string SpawnedEntityIdentifier { get; set; }
  }
}
