using System;

namespace MultiplayerInfrastructure.Registry
{
  /// <summary>
  /// 엔티티 프리셋(컨테이너 프리팹)에서 스폰 시 루트로 분리(ungroup)할 자식 <b>NetworkObject</b> 지정.
  ///
  /// 분리는 NetworkObject 인 자식에 대해서만 허용된다(MultiplayerInfrastructure/FishNet 으로 추적·제어되는
  /// 객체만 독립 루트로 안전하게 분리·복제 가능). 지정되지 않은 자식 NetworkObject 는 자동 분리하지 않는다.
  ///
  /// 이 타입은 프리셋 정의(EntityPresetDefinition) 및 등록 SO(EntityPresetRegistryRequirement)에 직렬화되어,
  /// "환자+침대를 한 프리팹으로 결합 배치하고 스폰 시 분리" 같은 구성을 <b>비런타임(에디터)에서 사전 설정·검증</b>할 수 있게 한다.
  /// </summary>
  [Serializable]
  public struct EntityPresetChildDetachment
  {
    /// <summary>컨테이너 루트 기준 자식 Transform 경로(예: "Bed") 또는 자식 오브젝트 이름.</summary>
    public string childPath;

    /// <summary>분리된 인스턴스에 부여할 엔티티 식별자(예: "bed_a"). 비어 있으면 GUID 기반 식별자 부여.</summary>
    public string spawnedEntityIdentifier;
  }
}
