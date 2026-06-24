using System;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.Serialization;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [Serializable]
  public struct EntityPresetRegistryRequirement
  {
    public string identifier;

    [FormerlySerializedAs("entityType")]
    [Tooltip("폴백 등록용 EntityType(기본값 Undefined). 프리팹이 스스로 레지스트리에 등록하는 컴포넌트" +
             "(ISpawnedEntityIdentifierReceiver, 예: PatientController/MovingPatientBedController) 를 가지면 " +
             "이 값은 무시되고 컴포넌트가 자기 EntityType 으로 등록한다. " +
             "자가 등록 컴포넌트가 없는 단순 프리팹에만 적용된다.")]
    public EntityType fallbackEntityType;
    public GameObject prefab;
    public string displayName;
    public bool isNetworked;

    [Tooltip("이 프리셋과 함께 스폰할 하위 엔티티 프리셋 참조 목록. " +
             "하위는 원본 프리팹이 아니라 '이미 등록된 다른 EntityPreset 의 식별자' 로 가리킨다. " +
             "예) 환자 그룹 프리셋이 침대 프리셋(bed_a)을 unwrap 으로 함께 스폰.")]
    public EntityPresetChildReference[] childReferences;
  }
}
