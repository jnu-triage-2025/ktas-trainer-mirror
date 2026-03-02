using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Item SO", menuName = "MultiplayerInfrastructure/Registry Preload Item SO")]
  public class RegistryPreloadItemSO : ScriptableObject
  {
    public ItemRegistryRequirement[] itemRegistryRequirements;
  }
}
