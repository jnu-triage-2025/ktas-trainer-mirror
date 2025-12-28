using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CreateAssetMenu(fileName = "WaypointRegistry", menuName = "Triage Trainer/Waypoint Registry")]
  public class WaypointRegistryDataSO : ScriptableObject
  {
    public WaypointRequirements[] Entries;
  }
}
