#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Requirements;
using TriageTrainer.Utils;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Scenario.Requirements.Editor
{
  public enum OverworldMigrationDisposition { ExistingLegacyGeneratedObject, ManualObject, Unresolved }

  public sealed class OverworldMigrationCandidate
  {
    public ScenarioRequirementKey Key { get; }
    public string Identifier { get; }
    public Vector3 Position { get; }
    public OverworldMigrationDisposition Disposition { get; }
    public GameObject ExistingObject { get; }
    public string Reason { get; }
    public OverworldMigrationCandidate(ScenarioRequirementKey key, string identifier, Vector3 position, OverworldMigrationDisposition disposition, GameObject existingObject, string reason) { Key = key; Identifier = identifier; Position = position; Disposition = disposition; ExistingObject = existingObject; Reason = reason; }
  }

  public static class OverworldInitializerMigration
  {
    [MenuItem("Tools/Triage Trainer/Scenario Requirements/Preview Overworld Initializer Migration")]
    public static void PreviewInEditor()
    {
      var summary = BuildSummary(Preview());
      Debug.Log(string.IsNullOrWhiteSpace(summary) ? "No legacy Overworld initializer objects found." : summary);
    }

    public static IReadOnlyList<OverworldMigrationCandidate> Preview()
    {
      var results = new List<OverworldMigrationCandidate>();
      var roots = UnityEngine.Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach (var root in roots.OrderBy(value => value.transform.GetInstanceID()))
      {
        foreach (var child in root.GetComponentsInChildren<Transform>(true).Where(value => value != root.transform))
        {
          var waypoint = child.GetComponent<WaypointAnchor>();
          var spawn = child.GetComponent<OverworldSpawnPoint>();
          if (waypoint != null && !string.IsNullOrWhiteSpace(waypoint.Identifier))
            results.Add(new OverworldMigrationCandidate(new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, waypoint.Identifier.Trim()), waypoint.Identifier.Trim(), child.position, OverworldMigrationDisposition.ExistingLegacyGeneratedObject, child.gameObject, "Legacy initializer generated waypoint; migration is preview-only."));
          else if (spawn != null && !string.IsNullOrWhiteSpace(spawn.Identifier))
            results.Add(new OverworldMigrationCandidate(new ScenarioRequirementKey(ScenarioRequirementKind.SpawnPoint, spawn.Identifier.Trim()), spawn.Identifier.Trim(), child.position, OverworldMigrationDisposition.ExistingLegacyGeneratedObject, child.gameObject, "Legacy initializer generated spawn point; migration is preview-only."));
        }
      }
      return results;
    }

    public static string BuildSummary(IEnumerable<OverworldMigrationCandidate> candidates)
      => string.Join("\n", (candidates ?? Array.Empty<OverworldMigrationCandidate>()).OrderBy(value => value.Key).Select(value => $"{value.Disposition}: {value.Key} at {value.Position} - {value.Reason}"));
  }
}
#endif
