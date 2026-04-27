using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Player Character SO", menuName = "MultiplayerInfrastructure/Registry Preload Player Character SO")]
  public class RegistryPreloadPlayerCharacterSO : ScriptableObject
  {
    public PlayerCharacterRegistryRequirement[] playerCharacterRegistryRequirements;
  }
}
