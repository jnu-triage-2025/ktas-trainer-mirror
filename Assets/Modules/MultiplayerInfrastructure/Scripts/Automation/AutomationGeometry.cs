#if UNITY_E2E || UNITY_EDITOR
using System.Linq;
using MultiplayerInfrastructure.Player;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MultiplayerInfrastructure.Automation
{
  internal static class AutomationGeometry
  {
    // Diagnostic surface sampling, not a pathfinding or walkability assertion.
    internal static JObject GroundProbe(Vector3 target, Transform ignoredRoot)
    {
      const float startHeight = 2f, distance = 10f;
      var result = new JObject { ["found"] = false, ["startHeight"] = startHeight, ["distance"] = distance };
      foreach (var hit in Physics.RaycastAll(target + Vector3.up * startHeight, Vector3.down,
                 distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore).OrderBy(hit => hit.distance))
      {
        if (hit.collider == null || (ignoredRoot != null && hit.collider.transform.IsChildOf(ignoredRoot))
            || hit.collider.GetComponentInParent<PlayerController>() != null
            || hit.collider.GetComponentInParent<Entity.Npc>() != null)
          continue;
        result["found"] = true;
        result["collider"] = hit.collider.name;
        result["point"] = new JArray(hit.point.x, hit.point.y, hit.point.z);
        result["normal"] = new JArray(hit.normal.x, hit.normal.y, hit.normal.z);
        result["heightFromSurface"] = target.y - hit.point.y;
        break;
      }
      return result;
    }
  }
}
#endif
