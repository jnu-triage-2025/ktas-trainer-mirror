using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MultiplayerInfrastructure.Automation.Editor
{
  public static class AutomationBuild
  {
    internal static bool InProgress { get; private set; }
    [MenuItem("Tools/E2E/Export door frame clearance")]
    public static void ExportDoorFrameClearance()
    {
      Physics.SyncTransforms();
      var rows = new Newtonsoft.Json.Linq.JArray();
      foreach (var door in UnityEngine.Object.FindObjectsByType<autoDoorSlide>(FindObjectsSortMode.None))
      {
        var probes = new Newtonsoft.Json.Linq.JArray();
        foreach (float lateral in new[] { -1f, -0.75f, -0.5f, -0.25f, 0f, 0.25f, 0.5f, 0.75f, 1f })
        foreach (float height in new[] { 0f, 0.05f, 0.1f, 0.2f, 0.5f, 1f })
        {
          Vector3 center = new Vector3(door.transform.position.x, height, door.transform.position.z);
          center += door.transform.right * lateral;
          Vector3 direction = door.transform.forward;
          var hits = Physics.RaycastAll(center - direction * 2, direction, 4, ~0, QueryTriggerInteraction.Ignore)
            .Where(hit => hit.collider.transform.IsChildOf(door.transform))
            .OrderBy(hit => hit.distance);
          probes.Add(new Newtonsoft.Json.Linq.JObject {
            ["worldHeight"] = height, ["lateralOffset"] = lateral,
            ["hits"] = new Newtonsoft.Json.Linq.JArray(hits.Select(hit => new Newtonsoft.Json.Linq.JObject {
              ["name"] = hit.collider.name, ["type"] = hit.collider.GetType().Name,
              ["distance"] = hit.distance,
              ["convex"] = hit.collider is MeshCollider meshCollider ? (bool?)meshCollider.convex : null,
              ["mesh"] = hit.collider is MeshCollider mesh && mesh.sharedMesh != null ? mesh.sharedMesh.name : null,
              ["triangleIndex"] = hit.triangleIndex,
              ["point"] = new Newtonsoft.Json.Linq.JArray(hit.point.x, hit.point.y, hit.point.z)
            }))
          });
        }
        rows.Add(new Newtonsoft.Json.Linq.JObject {
          ["name"] = door.name,
          ["position"] = new Newtonsoft.Json.Linq.JArray(door.transform.position.x, door.transform.position.y, door.transform.position.z),
          ["probes"] = probes
        });
      }
      System.IO.Directory.CreateDirectory("artifacts/unity-e2e");
      System.IO.File.WriteAllText("artifacts/unity-e2e/door-frame-clearance.json", new Newtonsoft.Json.Linq.JObject {
        ["scene"] = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
        ["capturedAt"] = DateTime.UtcNow, ["doors"] = rows,
        ["nearbyColliders"] = new Newtonsoft.Json.Linq.JArray(
          UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None)
          .Where(c => c.enabled && !c.isTrigger && c.bounds.Intersects(new Bounds(new Vector3(-64, 1, -13), new Vector3(18, 4, 18))))
          .Select(c => new Newtonsoft.Json.Linq.JObject {
            ["path"] = AnimationUtility.CalculateTransformPath(c.transform, null),
            ["type"] = c.GetType().Name,
            ["center"] = new Newtonsoft.Json.Linq.JArray(c.bounds.center.x, c.bounds.center.y, c.bounds.center.z),
            ["size"] = new Newtonsoft.Json.Linq.JArray(c.bounds.size.x, c.bounds.size.y, c.bounds.size.z)
          }))
      }.ToString());
      Debug.Log("E2E door clearance exported: artifacts/unity-e2e/door-frame-clearance.json");
    }

    [MenuItem("Tools/E2E/Build macOS automation player")]
    public static void BuildMac()
    {
      Build(BuildTarget.StandaloneOSX, Environment.GetEnvironmentVariable("UNITY_E2E_BUILD_PATH") ?? "Build/E2E.app");
    }
    [MenuItem("Tools/E2E/Build Windows automation player")]
    public static void BuildWindows()
    {
      Build(BuildTarget.StandaloneWindows64, Environment.GetEnvironmentVariable("UNITY_E2E_BUILD_PATH") ?? "Build/E2E/ktas-trainer.exe");
    }
    [MenuItem("Tools/E2E/Build macOS ordinary player for removal verification")]
    public static void BuildOrdinaryMac() => Build(BuildTarget.StandaloneOSX, "Build/E2E-removal-check.app", false);
    [MenuItem("Tools/E2E/Build Windows ordinary player for removal verification")]
    public static void BuildOrdinaryWindows() => Build(BuildTarget.StandaloneWindows64, "Build/E2E-removal-check/KTASTrainer.exe", false);
    private static void Build(BuildTarget target, string path, bool instrumented = true)
    {
      var options = new BuildPlayerOptions {
        scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
        target = target, locationPathName = path,
        options = instrumented ? BuildOptions.Development : BuildOptions.None,
        extraScriptingDefines = instrumented ? new[] { "UNITY_E2E" } : Array.Empty<string>()
      };
      BuildReport report;
      InProgress = instrumented;
      try { report = BuildPipeline.BuildPlayer(options); }
      finally { InProgress = false; }
      if (report.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("E2E build failed: " + report.summary.result);
      Debug.Log("E2E build completed: " + path);
    }
  }
}
