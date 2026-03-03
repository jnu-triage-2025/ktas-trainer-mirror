using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Entity SO", menuName = "MultiplayerInfrastructure/Registry Preload Entity SO")]
  public class RegistryPreloadEntitySO : ScriptableObject
  {
    public EntityRegistryRequirement[] entityRegistryRequirements;
  }
}
