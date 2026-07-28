using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
[CreateAssetMenu(fileName = "New Registry Preload Player Character SO", menuName = "Multiplayer Infrastructure/Registry Preload Player Character SO")]
  public class RegistryPreloadPlayerCharacterSO : ScriptableObject
  {
    public PlayerCharacterRegistryRequirement[] playerCharacterRegistryRequirements;
  }
}
