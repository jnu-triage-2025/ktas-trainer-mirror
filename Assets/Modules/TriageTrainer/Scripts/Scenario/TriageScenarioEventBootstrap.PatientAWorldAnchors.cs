using System;
using System.Reflection;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity.IntravenousLine;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private const string PatientASpawnAnchorId = "scen_a:patient_spawnpoint_a";
    private const string PatientAArrivalAnchorId = "scen_a:quest_arrival_patient_a";
    private const string PatientADoctorAnchorId = "scen_a:doctor_treatment_room_waypoint";

    /// <summary>
    /// 구버전 OverworldScene에서도 patient_a 시작 흐름이 원점 스폰 또는 영구 대기에 빠지지 않게 한다.
    /// 씬 담당자가 같은 식별자의 앵커를 배치한 경우에는 그 값을 우선하며 아무것도 생성하지 않는다.
    /// </summary>
    private static void EnsurePatientAWorldAnchors()
    {
      EnsureWaypoint(PatientASpawnAnchorId, new Vector3(-71.73906f, 0.01f, 0.07443f));
      EnsureWaypoint(PatientADoctorAnchorId, new Vector3(-67f, 1f, -15.5f));

      EnsureWaypoint(PatientAArrivalAnchorId, new Vector3(-72.525f, 0f, 0.7f));
      if (FindArrivalZone() == null)
        CreateArrivalZone(FindWaypoint(PatientAArrivalAnchorId)?.transform.position
          ?? new Vector3(-72.525f, 0f, 0.7f));
      DisableLegacyPatientAYankauerIvPort();
    }

    private static bool HasWaypoint(string identifier)
      => FindWaypoint(identifier) != null;

    private static WaypointAnchor FindWaypoint(string identifier)
    {
      foreach (var waypoint in FindObjectsByType<WaypointAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (waypoint != null && string.Equals(waypoint.Identifier, identifier, StringComparison.Ordinal))
          return waypoint;
      }
      return null;
    }

    private static void EnsureWaypoint(string identifier, Vector3 position)
    {
      if (HasWaypoint(identifier))
        return;
      var gameObject = new GameObject($"RuntimeWaypoint_{identifier}");
      gameObject.transform.position = position;
      gameObject.AddComponent<WaypointAnchor>().ConfigureIdentifier(identifier);
    }

    private static ScenarioTriggerZone FindArrivalZone()
    {
      foreach (var zone in FindObjectsByType<ScenarioTriggerZone>(FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (zone != null && string.Equals(zone.Identifier, PatientAArrivalAnchorId, StringComparison.Ordinal))
          return zone;
      }
      return null;
    }

    private static void CreateArrivalZone(Vector3 position)
    {
      var gameObject = new GameObject($"RuntimeZone_{PatientAArrivalAnchorId}");
      gameObject.SetActive(false);
      gameObject.transform.position = position;
      var collider = gameObject.AddComponent<BoxCollider>();
      collider.isTrigger = true;
      collider.size = new Vector3(8f, 3f, 8f);
      var zone = gameObject.AddComponent<ScenarioTriggerZone>();
      SetZoneField(zone, "_identifier", PatientAArrivalAnchorId);
      SetZoneField(zone, "_triggerOnce", false);
      SetZoneField(zone, "_perEntitySignalTemplate", "quest_arrival_patient_a_{id}");
      SetZoneField(zone, "_perEntityPlayersOnly", true);
      SetZoneField(zone, "_perEntityRaiseOncePerEntity", false);
      gameObject.SetActive(true);
    }

    private static void SetZoneField(ScenarioTriggerZone zone, string name, object value)
    {
      typeof(ScenarioTriggerZone).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?.SetValue(zone, value);
    }

    private static void DisableLegacyPatientAYankauerIvPort()
    {
      foreach (var point in FindObjectsByType<IntravenousLineConnectionPoint>(
                 FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (point == null
            || !string.Equals(point.Identifier, "connect_wall_component_and_yankauer",
              StringComparison.Ordinal))
          continue;

        var configs = typeof(IntravenousLineConnectionPoint).GetField(
            "_interactConfigs", BindingFlags.Instance | BindingFlags.NonPublic)
          ?.GetValue(point) as System.Collections.Generic.IEnumerable<IntravenousLineConnectionPoint.InteractConfig>;
        if (configs == null)
          continue;
        foreach (var config in configs)
          if (config != null)
            config.Enabled = false;
      }
    }
  }
}
