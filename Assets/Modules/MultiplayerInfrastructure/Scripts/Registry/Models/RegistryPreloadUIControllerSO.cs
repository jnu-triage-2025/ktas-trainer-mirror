using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
[CreateAssetMenu(fileName = "New Registry Preload UI Controller SO", menuName = "Multiplayer Infrastructure/Registry Preload UI Controller SO")]
  public class RegistryPreloadUIControllerSO : ScriptableObject
  {
    public UIControllerRegistryRequirement[] uiControllerRegistryRequirements;
  }
}
