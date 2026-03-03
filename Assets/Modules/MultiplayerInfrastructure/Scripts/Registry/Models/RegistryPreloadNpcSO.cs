using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload NPC SO", menuName = "MultiplayerInfrastructure/Registry Preload NPC SO")]
  public class RegistryPreloadNpcSO : ScriptableObject
  {
    public NPCRegistryRequirements[] npcRegistryRequirements;
  }
}
