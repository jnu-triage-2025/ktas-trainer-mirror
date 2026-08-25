using System;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 엔티티 프리셋이 스폰 시 함께 생성할 <b>하위 엔티티 프리셋</b> 참조.
  ///
  /// 핵심 설계: 하위 오브젝트는 (컨테이너 프리팹의 자식 Transform 경로가 아니라) <b>다른 EntityPreset 의 식별자</b>로
  /// 참조한다. 즉 하위가 프리팹이더라도 "원본 프리팹"이 아니라, 사전 설정이 끝난 <b>하위 EntityPreset</b>(이미 레지스트리에
  /// 등록된 프리셋)을 선택한다. 이렇게 하면 핵심 로직이 항상 EntityPreset 단위로 동작하며, 중첩 NetworkObject 가 한
  /// 프리팹에 nested 되는 구조(FishNet 직렬화 문제의 원인)를 피할 수 있다 — 각 프리셋은 런타임에 독립적으로 인스턴스화된다.
  ///
  /// 이 타입은 프리셋 정의(EntityPresetDefinition) 및 등록 SO(EntityPresetRegistryRequirement)에 직렬화되어,
  /// "환자 + 환자침대를 결합 배치하고 스폰" 같은 구성을 <b>비런타임(에디터)에서 사전 설정·검증</b>할 수 있게 한다.
  /// </summary>
  [Serializable]
  public struct EntityPresetChildReference
  {
    /// <summary>함께 스폰할 하위 엔티티 프리셋의 식별자. 이 식별자는 레지스트리에 등록된 다른 EntityPreset 이어야 한다.</summary>
    public string childPresetIdentifier;

    /// <summary>
    /// 스폰된 하위 인스턴스에 부여할 엔티티 식별자(예: "bed_a"). 비어 있으면 GUID 기반 식별자가 부여된다.
    /// 하위 프리셋 자체의 등록 식별자와는 별개로, 이 스폰 인스턴스가 런타임에 가질 식별자다.
    /// </summary>
    public string spawnedEntityIdentifier;

    /// <summary>
    /// 활성화되면 하위 인스턴스를 부모(루트 프리셋) 아래 계층이 아니라, <b>루트 프리셋과 동일한 계층(형제 루트)</b>에 둔다.
    /// 즉 하위 프리셋이 또 다른 루트가 되며, 위치/회전은 루트 기준 오프셋을 반영해 월드에 배치된다.
    /// 환자(루트)와 환자침대(하위)처럼 위계 없이 독립적으로 배치·복제되어야 하는 결합 스폰에 사용한다.
    /// 비활성화되면 하위 인스턴스는 루트의 자식으로 부착된다(로컬 오프셋 유지).
    /// </summary>
    public bool unwrapOnSpawn;

    /// <summary>
    /// 활성화되면 하위 인스턴스가 <see cref="IEntityPresetParentLinkReceiver"/> 를 구현한 경우,
    /// 스폰 직후 <b>부모(루트) 프리셋의 런타임 식별자</b>를 하위에 전달한다.
    /// 결합의 구체적 의미(예: 환자침대가 부모 환자를 자기 위에 누임)는 하위 구현체가 결정한다.
    /// "사전 결합된 채로 스폰"(환자 위에 침대 결합)을 별도 시나리오 이벤트 없이 성립시키는 데 사용한다.
    /// </summary>
    public bool linkChildToParent;
  }
}
