using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [Serializable]
  [CreateAssetMenu(fileName = "New NPC Base Model", menuName = "Multiplayer Infrastructure/NPC Base Model")]
  public class NPCBaseModelSO : ScriptableObject
  {
    [SerializeField] public string identifier;
    [SerializeField] public string displayName;
    [TextArea] [SerializeField] public string description;

    [Header("Scenario Interacts")]
    [SerializeField] public List<NPCScenarioInteractDefinition> scenarioInteracts = new();

    [Header("Item Submission Interacts")]
    [Tooltip("이 NPC 에게 아이템을 제출하는 상호작용 목록. Npc 가 런타임에 ItemSubmissionInteractable 을 자동 생성한다.")]
    [SerializeField] public List<NPCSubmissionInteractDefinition> submissionInteracts = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (scenarioInteracts == null)
        scenarioInteracts = new List<NPCScenarioInteractDefinition>();
      if (submissionInteracts == null)
        submissionInteracts = new List<NPCSubmissionInteractDefinition>();
    }
#endif
  }
}
