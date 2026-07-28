using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
[CreateAssetMenu(fileName = "New Registry Preload Interactable Entity SO", menuName = "Multiplayer Infrastructure/Registry Preload Interactable Entity SO")]
  public class RegistryPreloadInteractableEntitySO : ScriptableObject
  {
    public WaypointRequirements[] interactableEntityRequirements;
  }
}
