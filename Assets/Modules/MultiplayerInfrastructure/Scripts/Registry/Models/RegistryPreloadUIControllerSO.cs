using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload UI Controller SO", menuName = "MultiplayerInfrastructure/Registry Preload UI Controller SO")]
  public class RegistryPreloadUIControllerSO : ScriptableObject
  {
    public UIControllerRegistryRequirement[] uiControllerRegistryRequirements;
  }
}
