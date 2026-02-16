using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Entity
{
  [Serializable]
  [CreateAssetMenu(fileName = "New NPC Base Model", menuName = "Triage Trainer/NPC Base Model")]
  public class NPCBaseModelSO : ScriptableObject
  {
    [SerializeField] public string identifier;
    [SerializeField] public string displayName;
    [TextArea] [SerializeField] public string description;

    [Header("Scenario Interacts")]
    [SerializeField] public List<NPCScenarioInteractDefinition> scenarioInteracts = new();

#if UNITY_EDITOR
    private void OnValidate()
    {
      if (scenarioInteracts == null)
        scenarioInteracts = new List<NPCScenarioInteractDefinition>();
    }
#endif
  }
}
