using System;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace TriageTrainer.MultiplayerInfrastructureSupports.ScriptableObjects
{
  [Serializable]
  public struct EntityPresetRegistryRequirement
  {
    public string identifier;
    public EntityType entityType;
    public GameObject prefab;
    public string displayName;
    public bool isNetworked;

    [Tooltip("스폰 시 루트로 분리(ungroup)할 자식 NetworkObject 목록. " +
             "예) 환자+침대 결합 프리팹에서 침대(Bed)를 bed_a 로 분리. NetworkObject 만 분리 가능.")]
    public EntityPresetChildDetachment[] childDetachments;
  }
}
