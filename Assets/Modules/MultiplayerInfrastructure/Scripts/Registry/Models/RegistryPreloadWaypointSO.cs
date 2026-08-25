using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "New Registry Preload Waypoint SO", menuName = "Multiplayer Infrastructure/Registry Preload Waypoint SO")]
  public class RegistryPreloadWaypointSO : ScriptableObject
  {
    public WaypointRequirements[] waypointRequirements;
  }
}
