using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "NPCRegistry", menuName = "Triage Trainer/NPC Registry")]
  public class NPCRegistryDataSO : ScriptableObject
  {
    public NPCRegistryRequirements[] Entries;
  }
}
