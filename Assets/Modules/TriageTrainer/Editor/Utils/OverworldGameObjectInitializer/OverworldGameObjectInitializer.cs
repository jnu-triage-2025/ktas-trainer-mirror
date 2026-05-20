using System.Reflection;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

using TriageTrainer.Utils;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TriageTrainer.Editor.Utils
{
  public static class OverworldGameObjectInitializer
  {
    public static readonly Vector3 DefaultBuildingEnterance = new(-72.525f, 1f, 2.3f);
    public const string BuildingEnteranceIdentifier = "building-enterance";
    public static readonly Vector3 DefaultTreatmentRoomEnterance = new(-66.5f, 1f, -10.2f);
    public const string TreatmentRoomEnteranceIdentifier = "treatment-room-enterance";
    public static readonly Vector3 DefaultCommonSpawnPoint = new(-73f, 1f, -7.5f);
    public const string CommonSpawnPointIdentifier = "spawnpoint-commons";

    public static void Set()
    {
      Set(
        BuildingEnteranceIdentifier,
        DefaultBuildingEnterance,
        TreatmentRoomEnteranceIdentifier,
        DefaultTreatmentRoomEnterance,
        CommonSpawnPointIdentifier,
        DefaultCommonSpawnPoint
      );
    }

    public static void Set(
      string buildingIdentifier,
      Vector3 buildingEnterance,
      string treatmentIdentifier,
      Vector3 treatmentRoomEnterance,
      string commonSpawnPointIdentifier,
      Vector3 commonSpawnPoint)
    {
      var generatedRoot = GetOrCreateGeneratedRoot();
      DeleteChildren(generatedRoot.transform);
      CreateWaypoint(generatedRoot.transform, buildingIdentifier, buildingEnterance);
      CreateWaypoint(generatedRoot.transform, treatmentIdentifier, treatmentRoomEnterance);
      CreateSpawnPoint(generatedRoot.transform, commonSpawnPointIdentifier, commonSpawnPoint);
    }

    public static void Delete()
    {
      var generatedRoots = Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      foreach (var generated in generatedRoots)
      {
        DestroyObject(generated.gameObject);
      }
    }

    private static GeneratedByOverworldGameObjectInitializerEditor GetOrCreateGeneratedRoot()
    {
      var generatedRoots = Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

      if (generatedRoots.Length == 0)
      {
        return CreateGeneratedRoot();
      }

      var selected = generatedRoots[0];
      for (int i = 1; i < generatedRoots.Length; i++)
      {
        if (generatedRoots[i] == null)
          continue;

        DestroyObject(generatedRoots[i].gameObject);
      }

      return selected;
    }

    private static GeneratedByOverworldGameObjectInitializerEditor CreateGeneratedRoot()
    {
      var root = new GameObject(nameof(GeneratedByOverworldGameObjectInitializerEditor));
      var generated = root.AddComponent<GeneratedByOverworldGameObjectInitializerEditor>();

#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        Undo.RegisterCreatedObjectUndo(root, "Create Generated Root");
      }
#endif

      return generated;
    }

    private static void DeleteChildren(Transform parent)
    {
      for (int i = parent.childCount - 1; i >= 0; i--)
      {
        var child = parent.GetChild(i);
        DestroyObject(child.gameObject);
      }
    }

    private static void CreateWaypoint(Transform parent, string identifier, Vector3 position)
    {
      var waypointObject = new GameObject($"Waypoint_{identifier}");
      waypointObject.transform.SetParent(parent, false);
      waypointObject.transform.position = position;

      var waypointAnchor = waypointObject.AddComponent<WaypointAnchor>();
      SetWaypointIdentifier(waypointAnchor, identifier);

#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        Undo.RegisterCreatedObjectUndo(waypointObject, "Create Waypoint");
      }
#endif
    }

    private static Transform CreateSpawnPoint(Transform parent, string identifier, Vector3 position)
    {
      var spawnPointObject = new GameObject($"SpawnPoint_{identifier}");
      spawnPointObject.transform.SetParent(parent, false);
      spawnPointObject.transform.position = position;

      var marker = spawnPointObject.AddComponent<OverworldSpawnPoint>();
      marker.SetIdentifier(identifier);

#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        Undo.RegisterCreatedObjectUndo(spawnPointObject, "Create Common SpawnPoint");
      }
#endif

      return spawnPointObject.transform;
    }

    private static void SetWaypointIdentifier(WaypointAnchor waypointAnchor, string identifier)
    {
      var field = typeof(WaypointAnchor).GetField("identifier", BindingFlags.Instance | BindingFlags.NonPublic);
      if (field == null)
        return;

      field.SetValue(waypointAnchor, identifier);
    }

    private static void DestroyObject(Object target)
    {
      if (target == null)
        return;

#if UNITY_EDITOR
      if (!Application.isPlaying)
      {
        Undo.DestroyObjectImmediate(target);
        return;
      }
#endif

      Object.Destroy(target);
    }
  }
}
