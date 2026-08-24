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
  /// <summary>
  /// 오버월드 씬에 배치할 게임 오브젝트(웨이포인트, 스폰 포인트, 시나리오 신호 존 등)를
  /// 일괄 생성하는 이니셜라이저.
  /// </summary>
  /// <remarks>
  /// 오버월드 오브젝트 초기화 처리를 이 이니셜라이저에 등록하려면 상단에 식별자 값과
  /// 처리에 필요한 값을 함께 선언해야 한다. 현재 이니셜라이저에는 특화 구현이 없으므로,
  /// 특화된 구현이 필요하다면 먼저 일반화한 로직을 구현한 뒤, 상단의 리터럴 선언 부분에서
  /// 특화 값을 사전 설정한다. 이렇게 특화한 리터럴 값들은 이니셜라이저 에디터 창
  /// (OverworldGameObjectInitializerEditor)에서 수정 가능해야 한다.
  /// </remarks>
  public static class OverworldGameObjectInitializer
  {
    // 오버월드 오브젝트 초기화 처리를 추가할 때는 아래 형식처럼 식별자와 처리에 필요한
    // 값을 리터럴로 선언한다. 지침은 클래스 요약 주석 참고.
    public const string BuildingEnteranceIdentifier = "building-enterance";
    public static readonly Vector3 DefaultBuildingEnterance = new(-72.525f, 1f, 2.3f);

    public const string TreatmentRoomEnteranceIdentifier = "treatment-room-enterance";
    public static readonly Vector3 DefaultTreatmentRoomEnterance = new(-66.5f, 1f, -10.2f);
    
    public const string CommonSpawnPointIdentifier = "spawnpoint-commons";
    public static readonly Vector3 DefaultCommonSpawnPoint = new(-73f, 1f, -7.5f);

    // Scenario A anchors deliberately start unresolved. A scene author must set their real
    // positions in the initializer window before applying the generated wiring.
    public const string PatientASpawnWaypointIdentifier = "scen_a:patient_spawnpoint_a";
    public static readonly Vector3 DefaultPatientASpawnWaypoint = new(-1f, -1f, -1f);

    public const string PatientAArrivalWaypointIdentifier = "scen_a:quest_arrival_patient_a";
    public static readonly Vector3 DefaultPatientAArrivalWaypoint = new(-1f, -1f, -1f);

    public const string PatientADoctorWaypointIdentifier = "scen_a:doctor_treatment_room_waypoint";
    public static readonly Vector3 DefaultPatientADoctorWaypoint = new(-1f, -1f, -1f);
    public static readonly Vector3 DefaultPatientAArrivalZoneSize = new(8f, 3f, 8f);
    public static readonly string[] PatientAArrivalEnterSignals = System.Array.Empty<string>();
    public const string PatientAArrivalPerEntitySignalTemplate = "quest_arrival_patient_a_{id}";

    // patient_b_c_ct spatial anchors. These are intentionally plain WaypointAnchor
    // objects: runtime patient/preset spawns resolve their destinations by ID.
    public const string PatientBSpawnWaypointIdentifier = "scen_b:patient_spawnpoint_b";
    public static readonly Vector3 DefaultPatientBSpawnWaypoint = new(-74f, 0f, 2.3f);

    public const string PatientCSpawnWaypointIdentifier = "scen_b:patient_spawnpoint_c";
    public static readonly Vector3 DefaultPatientCSpawnWaypoint = new(-72f, 0f, 2.3f);

    public const string PatientDummyDBSpawnWaypointIdentifier = "scen_b:patient_spawnpoint_dummy_d_a";
    public static readonly Vector3 DefaultPatientDummyDBSpawnWaypoint = new(-70f, 0f, 2.3f);

    public const string TriageArrivalWaypointIdentifier = "scen_b:quest_arrival_triage_area";
    public static readonly Vector3 DefaultTriageArrivalWaypoint = new(-72.525f, 0f, 0.7f);

    public const string DoctorSpawnWaypointIdentifier = "scen_b:doctor_spawnpoint";
    public static readonly Vector3 DefaultDoctorSpawnWaypoint = new(-82f, 1f, -35f);

    public const string CareAreaWaypointIdentifier = "scen_b:care_area_waypoint";
    public static readonly Vector3 DefaultCareAreaWaypoint = new(-67f, 1f, -15.5f);

    public const string DoctorCareAreaWaypointIdentifier = "scen_b:doctor_care_area_waypoint";
    public static readonly Vector3 DefaultDoctorCareAreaWaypoint = new(-67f, 1f, -15.5f);

    public const string CtPatientBTargetPositionWaypointIdentifier = "ct:patient_target_pos_b";
    public static readonly Vector3 DefaultCtPatientBTargetPositionWaypoint = new(-80f, 0.5f, -18.3f);

    public const string CtPatientCTargetPositionWaypointIdentifier = "ct:patient_target_pos_c";
    public static readonly Vector3 DefaultCtPatientCTargetPositionWaypoint = new(-80f, 0.5f, -18.3f);

    // Scenario signal zones. Each zone reuses the identifier and position of the waypoint it
    // wraps, so only the box size and the signal strings specialize it. Arrival is judged by
    // collider overlap, which means the effective tolerance is the box half-extent plus the
    // half-extent of whatever enters it.
    public static readonly Vector3 DefaultTriageArrivalZoneSize = new(8f, 3f, 8f);

    // Scenario B의 역할별 도착 집계와 Patient A t14의 공통 분류구역 도착 게이트는 같은 물리
    // 구역을 사용한다. 둘 중 하나만 올리면 다른 시나리오가 영구 대기하므로 함께 발행한다.
    public static readonly string[] TriageArrivalEnterSignals =
      { "quest_arrival_triage_area", "arrive_triagearea" };
    public const string TriageArrivalPerEntitySignalTemplate = "quest_arrival_triage_area_{id}";

    // B/C target anchors intentionally share a position. One arrival zone records each
    // identified patient independently, so minor placement differences still count. The box
    // must stay wide enough to hold the B and C beds parked side by side at that shared anchor.
    public static readonly Vector3 DefaultCtPatientTargetZoneSize = new(4f, 3f, 4f);
    public static readonly string[] CtPatientArrivalEnterSignals = System.Array.Empty<string>();
    public const string CtPatientArrivalPerEntitySignalTemplate = "ct_patient_arrived_{id}";

    public static void Set()
    {
      Set(
        BuildingEnteranceIdentifier,
        DefaultBuildingEnterance,
        TreatmentRoomEnteranceIdentifier,
        DefaultTreatmentRoomEnterance,
        CommonSpawnPointIdentifier,
        DefaultCommonSpawnPoint,
        PatientASpawnWaypointIdentifier, DefaultPatientASpawnWaypoint,
        PatientAArrivalWaypointIdentifier, DefaultPatientAArrivalWaypoint,
        PatientADoctorWaypointIdentifier, DefaultPatientADoctorWaypoint,
        PatientBSpawnWaypointIdentifier, DefaultPatientBSpawnWaypoint,
        PatientCSpawnWaypointIdentifier, DefaultPatientCSpawnWaypoint,
        PatientDummyDBSpawnWaypointIdentifier, DefaultPatientDummyDBSpawnWaypoint,
        TriageArrivalWaypointIdentifier, DefaultTriageArrivalWaypoint,
        DoctorSpawnWaypointIdentifier, DefaultDoctorSpawnWaypoint,
        DoctorCareAreaWaypointIdentifier, DefaultDoctorCareAreaWaypoint,
        CtPatientBTargetPositionWaypointIdentifier, DefaultCtPatientBTargetPositionWaypoint,
        CtPatientCTargetPositionWaypointIdentifier, DefaultCtPatientCTargetPositionWaypoint,
        DefaultPatientAArrivalZoneSize,
        PatientAArrivalEnterSignals,
        PatientAArrivalPerEntitySignalTemplate,
        DefaultTriageArrivalZoneSize,
        TriageArrivalEnterSignals,
        TriageArrivalPerEntitySignalTemplate,
        DefaultCtPatientTargetZoneSize,
        CtPatientArrivalEnterSignals,
        CtPatientArrivalPerEntitySignalTemplate
      );
    }

    public static void Set(
      string buildingIdentifier,
      Vector3 buildingEnterance,
        string treatmentIdentifier,
        Vector3 treatmentRoomEnterance,
        string commonSpawnPointIdentifier,
        Vector3 commonSpawnPoint,
        string patientASpawnWaypointIdentifier, Vector3 patientASpawnWaypoint,
        string patientAArrivalWaypointIdentifier, Vector3 patientAArrivalWaypoint,
        string patientADoctorWaypointIdentifier, Vector3 patientADoctorWaypoint,
        string patientBSpawnWaypointIdentifier, Vector3 patientBSpawnWaypoint,
        string patientCSpawnWaypointIdentifier, Vector3 patientCSpawnWaypoint,
        string patientDummyDBSpawnWaypointIdentifier, Vector3 patientDummyDBSpawnWaypoint,
        string triageArrivalWaypointIdentifier, Vector3 triageArrivalWaypoint,
        string doctorSpawnWaypointIdentifier, Vector3 doctorSpawnWaypoint,
        string doctorCareAreaWaypointIdentifier, Vector3 doctorCareAreaWaypoint,
        string ctPatientBWaypointIdentifier, Vector3 ctPatientBWaypoint,
        string ctPatientCWaypointIdentifier, Vector3 ctPatientCWaypoint,
        Vector3 patientAArrivalZoneSize,
        string[] patientAArrivalEnterSignals,
        string patientAArrivalPerEntitySignalTemplate,
        Vector3 triageArrivalZoneSize,
        string[] triageArrivalEnterSignals,
        string triageArrivalPerEntitySignalTemplate,
        Vector3 ctPatientTargetZoneSize,
        string[] ctPatientArrivalEnterSignals,
        string ctPatientArrivalPerEntitySignalTemplate)
    {
      var generatedRoot = GetOrCreateGeneratedRoot();
      DeleteGeneratedScenarioObjects(generatedRoot.transform);
      CreateWaypoint(generatedRoot.transform, buildingIdentifier, buildingEnterance);
      CreateWaypoint(generatedRoot.transform, treatmentIdentifier, treatmentRoomEnterance);
      CreateWaypoint(generatedRoot.transform, patientASpawnWaypointIdentifier, patientASpawnWaypoint);
      CreateWaypoint(generatedRoot.transform, patientAArrivalWaypointIdentifier, patientAArrivalWaypoint);
      CreateWaypoint(generatedRoot.transform, patientADoctorWaypointIdentifier, patientADoctorWaypoint);
      CreateScenarioSignalZone(
        generatedRoot.transform,
        patientAArrivalWaypointIdentifier,
        patientAArrivalWaypoint,
        patientAArrivalZoneSize,
        patientAArrivalEnterSignals,
        patientAArrivalPerEntitySignalTemplate,
        perEntityPlayersOnly: true);
      CreateWaypoint(generatedRoot.transform, patientBSpawnWaypointIdentifier, patientBSpawnWaypoint);
      CreateWaypoint(generatedRoot.transform, patientCSpawnWaypointIdentifier, patientCSpawnWaypoint);
      CreateWaypoint(generatedRoot.transform, patientDummyDBSpawnWaypointIdentifier, patientDummyDBSpawnWaypoint);
      CreateWaypoint(generatedRoot.transform, triageArrivalWaypointIdentifier, triageArrivalWaypoint);
      CreateWaypoint(generatedRoot.transform, doctorSpawnWaypointIdentifier, doctorSpawnWaypoint);
      CreateWaypoint(generatedRoot.transform, doctorCareAreaWaypointIdentifier, doctorCareAreaWaypoint);
      CreateScenarioSignalZone(
        generatedRoot.transform,
        triageArrivalWaypointIdentifier,
        triageArrivalWaypoint,
        triageArrivalZoneSize,
        triageArrivalEnterSignals,
        triageArrivalPerEntitySignalTemplate,
        perEntityPlayersOnly: true);
      CreateWaypoint(generatedRoot.transform, ctPatientBWaypointIdentifier, ctPatientBWaypoint);
      CreateWaypoint(generatedRoot.transform, ctPatientCWaypointIdentifier, ctPatientCWaypoint);
      CreateScenarioSignalZone(
        generatedRoot.transform,
        ctPatientBWaypointIdentifier,
        ctPatientBWaypoint,
        ctPatientTargetZoneSize,
        ctPatientArrivalEnterSignals,
        ctPatientArrivalPerEntitySignalTemplate,
        perEntityPlayersOnly: false);
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
          if (entity.type == StaticEntityLayoutType.MovingPatientBedPositioningPoint)
          {
            var point = instance.AddComponent<MovingPatientBedPositioningPoint>();
            point.SetIdentifierForEditor(entity.identifier);
            if (!entity.useDefaultOccupiedSize || !entity.useDefaultDisplayHeight)
              point.ConfigureOccupiedArea(
                entity.useDefaultOccupiedSize ? new Vector2(2.2f, 1f) : entity.occupiedSize,
                entity.useDefaultDisplayHeight ? 0.03f : entity.displayHeight);
          }
          else if (entity.type == StaticEntityLayoutType.DefibrillatorCartSnapPoint)
          {
            var point = instance.AddComponent<DefibrillatorCartSnapPoint>();
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
      component?.SetEntityIdentifier(identifier);
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

    private static void DeleteGeneratedScenarioObjects(Transform parent)
    {
      for (int i = parent.childCount - 1; i >= 0; i--)
      {
        var child = parent.GetChild(i);
        if (child.name.StartsWith("static_entities:", System.StringComparison.Ordinal))
          continue;
        DestroyObject(child.gameObject);
      }
    }

    private static void CreateWaypoint(Transform parent, string identifier, Vector3 position)
    {
      var waypointObject = new GameObject($"Waypoint_{identifier}");
      waypointObject.transform.SetParent(parent, false);
      waypointObject.transform.position = position;

      var waypointAnchor = waypointObject.AddComponent<WaypointAnchor>();
      waypointAnchor.ConfigureIdentifier(identifier);

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

    private static void CreateScenarioSignalZone(
      Transform parent,
      string identifier,
      Vector3 position,
      Vector3 size,
      string[] enterSignals,
      string perEntitySignalTemplate,
      bool perEntityPlayersOnly)
    {
      var zoneObject = new GameObject($"ScenarioZone_{identifier}");
      zoneObject.transform.SetParent(parent, false);
      zoneObject.transform.position = position;
      var collider = zoneObject.AddComponent<BoxCollider>();
      collider.isTrigger = true;
      collider.size = size;
      var zone = zoneObject.AddComponent<MultiplayerInfrastructure.Scenario.ScenarioTriggerZone>();
      SetPrivateField(zone, "_identifier", identifier);
      SetPrivateField(zone, "_triggerOnce", false);
      // 호출자가 공유 기본값 배열을 넘길 수 있다. 생성된 컴포넌트가 그 인스턴스를 그대로
      // 물지 않도록 복제해서 넣는다.
      SetPrivateField(zone, "_raiseSignalsOnEnter",
        (string[])(enterSignals ?? System.Array.Empty<string>()).Clone());
      SetPrivateField(zone, "_perEntitySignalTemplate", perEntitySignalTemplate);
      SetPrivateField(zone, "_perEntityPlayersOnly", perEntityPlayersOnly);
      // RuntimeState의 도착 신호는 시나리오 시작마다 초기화된다. 존 자체가 엔티티를
      // 영구 기억하면 두 번째 실행에서 신호가 재발행되지 않으므로 진입마다 발행한다.
      SetPrivateField(zone, "_perEntityRaiseOncePerEntity", false);

#if UNITY_EDITOR
      if (!Application.isPlaying)
        Undo.RegisterCreatedObjectUndo(zoneObject, "Create Scenario Signal Zone");
#endif
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
      target?.GetType()
        .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
        ?.SetValue(target, value);
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
