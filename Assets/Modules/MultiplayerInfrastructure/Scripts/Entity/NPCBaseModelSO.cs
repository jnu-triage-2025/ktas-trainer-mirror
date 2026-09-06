using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [Serializable]
  [CreateAssetMenu(fileName = "New NPC Base Model", menuName = "Multiplayer Infrastructure/NPC Base Model")]
  public class NPCBaseModelSO : ScriptableObject
  {
    [SerializeField] public string identifier;
    [SerializeField] public string displayName;
    [TextArea][SerializeField] public string description;

    // 인터렉션 정의는 시나리오 데이터(interactions)와 상시 카탈로그(Resources/Interactions)가 담당한다.
  }
}
