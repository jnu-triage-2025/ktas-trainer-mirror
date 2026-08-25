using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload NPC SO", menuName = "Multiplayer Infrastructure/Registry Preload NPC SO")]
  public class RegistryPreloadNpcSO : ScriptableObject
  {
    public NPCRegistryRequirements[] npcRegistryRequirements;
  }
}
