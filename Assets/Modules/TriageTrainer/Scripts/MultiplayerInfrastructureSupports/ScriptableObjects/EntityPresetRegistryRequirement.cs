using System;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [Serializable]
  public struct EntityPresetRegistryRequirement
  {
    public string identifier;

    [Tooltip("폴백 등록용 EntityType. 프리팹이 스스로 레지스트리에 등록하는 컴포넌트" +
             "(ISpawnedEntityIdentifierReceiver, 예: PatientController/MovingPatientBedController) 를 가지면 " +
             "이 값은 무시되고 컴포넌트가 자기 EntityType 으로 등록한다. " +
             "컨테이너(분리용) 프리팹처럼 루트가 소비되는 경우에도 사용되지 않는다. " +
             "자가 등록 컴포넌트가 없는 단순 프리팹에만 적용된다.")]
    public EntityType entityType;
    public GameObject prefab;
    public string displayName;
    public bool isNetworked;

    [Tooltip("스폰 시 루트로 분리(ungroup)할 자식 NetworkObject 목록. " +
             "예) 환자+침대 결합 프리팹에서 침대(Bed)를 bed_a 로 분리. NetworkObject 만 분리 가능.")]
    public EntityPresetChildDetachment[] childDetachments;
  }
}
