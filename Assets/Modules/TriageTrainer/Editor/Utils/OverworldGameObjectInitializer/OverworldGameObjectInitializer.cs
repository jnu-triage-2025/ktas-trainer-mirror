using System.Reflection;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

using TriageTrainer.Utils;
using TriageTrainer.Entity;

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

    public static void SetStaticEntityLayouts(StaticEntityLayoutDefinition definition)
    {
      if (definition == null)
        throw new System.ArgumentNullException(nameof(definition));
      if (!definition.Validate(out var validationError))
        throw new System.ArgumentException(validationError, nameof(definition));

      var root = GetOrCreateGeneratedRoot().transform;
      var childName = "static_entities:" + definition.identifier.Trim();
      var existing = root.Find(childName);
      if (existing != null) DestroyObject(existing.gameObject);
      var layoutRoot = new GameObject(childName).transform;
      layoutRoot.SetParent(root, false);
#if UNITY_EDITOR
      if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(layoutRoot.gameObject, "Create Static Entity Layout");
#endif

      foreach (var group in definition.groups ?? new System.Collections.Generic.List<StaticEntityLayoutGroup>())
      {
        if (group.entities == null) continue;
        foreach (var entity in group.entities)
        {
          if (string.IsNullOrWhiteSpace(entity.identifier)) continue;
          GameObject instance;
          instance = entity.type == StaticEntityLayoutType.WallSuction
            ? InstantiatePrefab(definition.wallSuctionPrefab)
            : entity.type == StaticEntityLayoutType.Oxyflowmeter
              ? InstantiatePrefab(definition.oxyflowmeterPrefab)
              : new GameObject(entity.identifier);
          if (instance == null) continue;
          instance.name = entity.identifier;
          instance.transform.SetParent(layoutRoot, false);
          if (!entity.useDefaultPosition)
            instance.transform.localPosition = entity.position;
          if (!entity.useDefaultRotation)
            instance.transform.localRotation = Quaternion.Euler(entity.rotationEuler);
          if (entity.type == StaticEntityLayoutType.WaypointAnchor)
          {
            var waypoint = instance.AddComponent<WaypointAnchor>();
            waypoint.ConfigureIdentifier(entity.identifier);
          }
          else if (entity.type == StaticEntityLayoutType.MovingPatientBedPositioningPoint)
          {
            var point = instance.AddComponent<MovingPatientBedPositioningPoint>();
            point.SetIdentifierForEditor(entity.identifier);
            if (!entity.useDefaultOccupiedSize || !entity.useDefaultDisplayHeight)
              point.ConfigureOccupiedArea(
                entity.useDefaultOccupiedSize ? new Vector2(2.2f, 1f) : entity.occupiedSize,
                entity.useDefaultDisplayHeight ? 0.03f : entity.displayHeight);
          }
          else if (entity.type == StaticEntityLayoutType.PatientCareDescriptionZone)
          {
            var collider = instance.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = entity.zoneSize == Vector3.zero ? Vector3.one : entity.zoneSize;
            var zone = instance.AddComponent<PatientCareDescriptionZone>();
            zone.SetIdentifierForEditor(entity.identifier);
            zone.ConfigureArea(
              entity.useDefaultZoneCenter ? new Vector3(0f, 1.5f, 0f) : entity.zoneCenter,
              entity.useDefaultZoneSize ? new Vector3(3f, 3.5f, 3f) : entity.zoneSize);
          }
          else if (entity.type == StaticEntityLayoutType.WallSuction || entity.type == StaticEntityLayoutType.Oxyflowmeter)
            SetStaticObjectIdentifier(instance, entity.identifier);
#if UNITY_EDITOR
          if (!Application.isPlaying) Undo.RegisterCreatedObjectUndo(instance, "Create Static Entity");
#endif
        }
      }
    }

    private static void SetStaticObjectIdentifier(GameObject instance, string identifier)
    {
      var component = instance.GetComponent<MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment>();
      var field = component == null ? null : typeof(MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment)
        .GetField("_entityIdentifier", BindingFlags.Instance | BindingFlags.NonPublic);
      field?.SetValue(component, identifier.Trim());
    }

    private static GameObject InstantiatePrefab(GameObject prefab)
    {
      if (prefab == null) return null;
#if UNITY_EDITOR
      if (!Application.isPlaying) return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
#endif
      return Object.Instantiate(prefab);
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
