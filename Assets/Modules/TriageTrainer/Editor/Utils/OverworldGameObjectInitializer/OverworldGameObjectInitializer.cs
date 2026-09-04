using System.Collections.Generic;
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
    [System.Serializable]
    public sealed class WaypointSetDefinition
    {
      public string identifier = "waypoint-set";
      public string displayName = "Waypoint Set";
      public List<WaypointDefinition> waypoints = new();
    }

    [System.Serializable]
    public sealed class WaypointDefinition
    {
      public string identifier = "waypoint";
      public Vector3 position;
    }

    // 오버월드 오브젝트 초기화 처리를 추가할 때는 아래 형식처럼 식별자와 처리에 필요한
    // 값을 리터럴로 선언한다. 지침은 클래스 요약 주석 참고.
    public const string BuildingEnteranceIdentifier = "building-enterance";
    public static readonly Vector3 DefaultBuildingEnterance = new(-72.525f, 1f, 2.3f);

    public const string TreatmentRoomEnteranceIdentifier = "treatment-room-enterance";
    public static readonly Vector3 DefaultTreatmentRoomEnterance = new(-66.5f, 1f, -10.2f);

    public const string CommonSpawnPointIdentifier = "spawnpoint-commons";
    public static readonly Vector3 DefaultCommonSpawnPoint = new(-73f, 1f, -7.5f);

    // Scenario A 앵커는 TriageScenarioEventBootstrap.PatientAWorldAnchors.EnsurePatientAWorldAnchors
    // 의 런타임 폴백과 같은 값을 쓴다. 양쪽을 항상 동기화한다. 씬에 작성된 앵커가 런타임
    // 폴백보다 우선하므로, 여기서 구운 플레이스홀더가 patient_a 스폰 위치를 해결되지 않은
    // 상태로 두는 대시 조용히 옮겨버릴 수 있다.
    public const string PatientASpawnWaypointIdentifier = "scen_a:patient_spawnpoint_a";
    public static readonly Vector3 DefaultPatientASpawnWaypoint = new(-71.73906f, 0.01f, 0.07443f);

    public const string PatientAArrivalWaypointIdentifier = "scen_a:quest_arrival_patient_a";
    public static readonly Vector3 DefaultPatientAArrivalWaypoint = new(-72.525f, 0f, 0.7f);

    public const string PatientADoctorTreatroomEnteranceWaypointIdentifier = "scen_a:doctor_treatment_room_waypoint_enterance";
    public static readonly Vector3 DefaultPatientADoctorWaypoint = new(-68f, 1f, -10f);

    public const string PatientADocterTreatroomEnteredWaypointIdentifier = "scen_a:doctor_treatment_room_waypoint_entered";
    public static readonly Vector3 DefaultPatientADoctorEnteredWaypoint = new(-61.5f, 1f, -10f);

    public static readonly Vector3 DefaultPatientAArrivalZoneSize = new(8f, 3f, 8f);
    public static readonly string[] PatientAArrivalEnterSignals = System.Array.Empty<string>();
    public const string PatientAArrivalPerEntitySignalTemplate = "quest_arrival_patient_a_{id}";

    // patient_b_c_ct 공간 앵커. 의도적으로 평범한 WaypointAnchor 오브젝트로 둔다.
    // 런타임의 환자/프리셋 스폰이 ID 로 목적지를 해석하기 때문이다.
    public const string PatientBSpawnWaypointIdentifier = "scen_b:patient_spawnpoint_b";
    public static readonly Vector3 DefaultPatientBSpawnWaypoint = new(-74f, 0f, 2.3f);

    public const string PatientCSpawnWaypointIdentifier = "scen_b:patient_spawnpoint_c";
    public static readonly Vector3 DefaultPatientCSpawnWaypoint = new(-72f, 0f, 2.3f);

    public const string PatientDummyDBSpawnWaypointIdentifier = "scen_b:patient_spawnpoint_dummy_d_a";
    public static readonly Vector3 DefaultPatientDummyDBSpawnWaypoint = new(-70f, 0f, 2.3f);

    public const string TriageArrivalWaypointIdentifier = "scen_b:quest_arrival_triage_area";
    public static readonly Vector3 DefaultTriageArrivalWaypoint = new(-72.525f, 0f, 0.7f);

    public const string DoctorSpawnWaypointIdentifier = "scen_b:doctor_spawnpoint";
    public static readonly Vector3 DefaultDoctorSpawnWaypoint = new(-83f, 1f, -32f);

    public const string DoctorRouteWaypointSetIdentifier = "overworld:doctor-route";
    public static readonly WaypointSetDefinition DefaultDoctorRouteWaypointSet = new()
    {
      identifier = DoctorRouteWaypointSetIdentifier,
      displayName = "Doctor Route",
      waypoints = new List<WaypointDefinition>
      {
        new() { identifier = "overworld:doctor-route:01", position = new Vector3(-83f, 1f, -32f) },
        new() { identifier = "overworld:doctor-route:02", position = new Vector3(-77f, 1f, -32f) },
        new() { identifier = "overworld:doctor-route:03", position = new Vector3(-68f, 1f, -32f) },
        new() { identifier = "overworld:doctor-route:04", position = new Vector3(-68f, 1f, -17.5f) },
      }
    };

    public const string CareAreaWaypointIdentifier = "scen_b:care_area_waypoint";
    public static readonly Vector3 DefaultCareAreaWaypoint = new(-67f, 1f, -15.5f);

    public const string DoctorCareAreaWaypointIdentifier = "scen_b:doctor_care_area_waypoint";
    public static readonly Vector3 DefaultDoctorCareAreaWaypoint = new(-67f, 1f, -15.5f);

    public const string CtPatientBTargetPositionWaypointIdentifier = "ct:patient_target_pos_b";
    public static readonly Vector3 DefaultCtPatientBTargetPositionWaypoint = new(-80f, 0.5f, -18.3f);

    public const string CtPatientCTargetPositionWaypointIdentifier = "ct:patient_target_pos_c";
    public static readonly Vector3 DefaultCtPatientCTargetPositionWaypoint = new(-80f, 0.5f, -18.3f);

    public const string CtRoomWaypointIdentifier = "ct:ctroom";
    public static readonly Vector3 DefaultCtRoomWaypoint = new(-79f, 2f, -22.5f);

    // 시나리오 신호 존. 각 존은 감싸는 웨이포인트의 식별자와 위치를 재사용하므로,
    // 박스 크기와 신호 문자열만 개별화한다. 도착은 콜라이더 겹침으로 판정하므로,
    // 실효 허용 오차는 박스 절반 크기에 진입 물체의 절반 크기를 더한 값이다.
    public static readonly Vector3 DefaultTriageArrivalZoneSize = new(8f, 3f, 8f);

    // Scenario B의 역할별 도착 집계와 Patient A t14의 공통 분류구역 도착 게이트는 같은 물리
    // 구역을 사용한다. 둘 중 하나만 올리면 다른 시나리오가 영구 대기하므로 함께 발행한다.
    public static readonly string[] TriageArrivalEnterSignals =
      { "quest_arrival_triage_area", "arrive_triagearea" };
    public const string TriageArrivalPerEntitySignalTemplate = "quest_arrival_triage_area_{id}";

    // B/C 목표 앵커는 의도적으로 같은 위치를 공유한다. 하나의 도착 존이 식별된
    // 환자를 각자 따로 기록하므로, 배치가 조금 어긋나도 도착으로 인정된다. 박스는
    // 그 공유 앵커에 나란히 놓인 B 와 C 침대를 모두 담을 만큼 넓어야 한다.
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
        PatientADoctorTreatroomEnteranceWaypointIdentifier, DefaultPatientADoctorWaypoint,
        PatientADocterTreatroomEnteredWaypointIdentifier, DefaultPatientADoctorEnteredWaypoint,
        PatientBSpawnWaypointIdentifier, DefaultPatientBSpawnWaypoint,
        PatientCSpawnWaypointIdentifier, DefaultPatientCSpawnWaypoint,
        PatientDummyDBSpawnWaypointIdentifier, DefaultPatientDummyDBSpawnWaypoint,
        TriageArrivalWaypointIdentifier, DefaultTriageArrivalWaypoint,
        DoctorSpawnWaypointIdentifier, DefaultDoctorSpawnWaypoint,
        DoctorCareAreaWaypointIdentifier, DefaultDoctorCareAreaWaypoint,
        CtPatientBTargetPositionWaypointIdentifier, DefaultCtPatientBTargetPositionWaypoint,
        CtPatientCTargetPositionWaypointIdentifier, DefaultCtPatientCTargetPositionWaypoint,
        CtRoomWaypointIdentifier, DefaultCtRoomWaypoint,
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
        string patientADoctorEnteredWaypointIdentifier, Vector3 patientADoctorEnteredWaypoint,
        string patientBSpawnWaypointIdentifier, Vector3 patientBSpawnWaypoint,
        string patientCSpawnWaypointIdentifier, Vector3 patientCSpawnWaypoint,
        string patientDummyDBSpawnWaypointIdentifier, Vector3 patientDummyDBSpawnWaypoint,
        string triageArrivalWaypointIdentifier, Vector3 triageArrivalWaypoint,
        string doctorSpawnWaypointIdentifier, Vector3 doctorSpawnWaypoint,
        string doctorCareAreaWaypointIdentifier, Vector3 doctorCareAreaWaypoint,
        string ctPatientBWaypointIdentifier, Vector3 ctPatientBWaypoint,
        string ctPatientCWaypointIdentifier, Vector3 ctPatientCWaypoint,
        string ctRoomWaypointIdentifier, Vector3 ctRoomWaypoint,
        Vector3 patientAArrivalZoneSize,
        string[] patientAArrivalEnterSignals,
        string patientAArrivalPerEntitySignalTemplate,
        Vector3 triageArrivalZoneSize,
        string[] triageArrivalEnterSignals,
        string triageArrivalPerEntitySignalTemplate,
        Vector3 ctPatientTargetZoneSize,
        string[] ctPatientArrivalEnterSignals,
        string ctPatientArrivalPerEntitySignalTemplate,
        WaypointSetDefinition doctorRouteWaypointSet = null)
    {
      var generatedRoot = GetOrCreateGeneratedRoot();
      DeleteGeneratedScenarioObjects(generatedRoot.transform);
      CreateWaypoint(generatedRoot.transform, buildingIdentifier, buildingEnterance);
      CreateWaypoint(generatedRoot.transform, treatmentIdentifier, treatmentRoomEnterance);
      CreateWaypoint(generatedRoot.transform, patientASpawnWaypointIdentifier, patientASpawnWaypoint);
      CreateWaypoint(generatedRoot.transform, patientAArrivalWaypointIdentifier, patientAArrivalWaypoint);
      CreateWaypoint(generatedRoot.transform, patientADoctorWaypointIdentifier, patientADoctorWaypoint);
      CreateWaypoint(generatedRoot.transform, patientADoctorEnteredWaypointIdentifier, patientADoctorEnteredWaypoint);
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
      CreateWaypoint(generatedRoot.transform, ctRoomWaypointIdentifier, ctRoomWaypoint);
      CreateScenarioSignalZone(
        generatedRoot.transform,
        ctPatientBWaypointIdentifier,
        ctPatientBWaypoint,
        ctPatientTargetZoneSize,
        ctPatientArrivalEnterSignals,
        ctPatientArrivalPerEntitySignalTemplate,
        perEntityPlayersOnly: false);
      CreateSpawnPoint(generatedRoot.transform, commonSpawnPointIdentifier, commonSpawnPoint);
      CreateWaypointSets(generatedRoot.transform, new[]
      {
        doctorRouteWaypointSet ?? DefaultDoctorRouteWaypointSet
      });
    }

    public static void Delete()
    {
      var generatedRoots = UnityEngine.Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
      if (existing != null)
        DestroyObject(existing.gameObject);
      var layoutRoot = new GameObject(childName).transform;
      layoutRoot.SetParent(root, false);
#if UNITY_EDITOR
      if (!Application.isPlaying)
        Undo.RegisterCreatedObjectUndo(layoutRoot.gameObject, "Create Static Entity Layout");
#endif

      foreach (var group in definition.groups ?? new System.Collections.Generic.List<StaticEntityLayoutGroup>())
      {
        if (group.entities == null)
          continue;
        foreach (var entity in group.entities)
        {
          if (string.IsNullOrWhiteSpace(entity.identifier))
            continue;
          GameObject instance;
          instance = entity.type == StaticEntityLayoutType.WallSuction
            ? InstantiatePrefab(definition.wallSuctionPrefab)
            : entity.type == StaticEntityLayoutType.Oxyflowmeter
              ? InstantiatePrefab(definition.oxyflowmeterPrefab)
              : new GameObject(entity.identifier);
          if (instance == null)
            continue;
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
            ConfigureWallAttachment(instance, entity);
#if UNITY_EDITOR
          if (!Application.isPlaying)
            Undo.RegisterCreatedObjectUndo(instance, "Create Static Entity");
#endif
        }
      }
    }

    private static void ConfigureWallAttachment(GameObject instance, StaticEntityTransformDefinition entity)
    {
      var component = instance.GetComponent<MultiplayerInfrastructure.ItemSystem.StaticObjectDisplayment>();
      component?.SetEntityIdentifier(entity.identifier);

      // 설치 완료 신호를 씬의 프리팹 오버라이드로만 두면, 레이아웃을 다시 생성할 때 인스턴스가
      // 통째로 교체되면서 값이 사라진다. 그러면 설치해도 신호가 올라가지 않아 퀘스트 목표와
      // 시나리오 검증 노드가 영구히 대기한다. 배치 데이터를 진실 원천으로 삼아 매번 다시 주입한다.
      var signalTarget = instance.GetComponent<IAttachCompletionSignalConfigurable>();
      signalTarget?.SetAttachCompletionSignalForEditor(entity.attachCompletionSignal);
    }

    private static GameObject InstantiatePrefab(GameObject prefab)
    {
      if (prefab == null)
        return null;
#if UNITY_EDITOR
      if (!Application.isPlaying)
        return (GameObject)PrefabUtility.InstantiatePrefab(prefab);
#endif
      return UnityEngine.Object.Instantiate(prefab);
    }

    private static GeneratedByOverworldGameObjectInitializerEditor GetOrCreateGeneratedRoot()
    {
      var generatedRoots = UnityEngine.Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);

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

    private static WaypointAnchor CreateWaypoint(Transform parent, string identifier, Vector3 position)
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
      return waypointAnchor;
    }

    private static void CreateWaypointSets(
      Transform parent, IReadOnlyList<WaypointSetDefinition> waypointSets)
    {
      if (waypointSets == null)
        return;

      foreach (var definition in waypointSets)
      {
        if (definition == null || string.IsNullOrWhiteSpace(definition.identifier))
          continue;

        string identifier = definition.identifier.Trim();
        var setObject = new GameObject(string.IsNullOrWhiteSpace(definition.displayName)
          ? $"Waypoint Set_{identifier}"
          : definition.displayName.Trim());
        setObject.transform.SetParent(parent, false);
        setObject.transform.localPosition = Vector3.zero;
        var waypointSet = setObject.AddComponent<WaypointSet>();
        waypointSet.ConfigureIdentifier(identifier);

#if UNITY_EDITOR
        if (!Application.isPlaying)
          Undo.RegisterCreatedObjectUndo(setObject, "Create Waypoint Set");
#endif

        if (definition.waypoints == null)
          continue;

        var orderedWaypoints = new List<WaypointAnchor>();
        foreach (var waypoint in definition.waypoints)
        {
          if (waypoint == null || string.IsNullOrWhiteSpace(waypoint.identifier))
            continue;
          orderedWaypoints.Add(CreateWaypoint(
            setObject.transform, waypoint.identifier.Trim(), waypoint.position));
        }
        waypointSet.ConfigureWaypoints(orderedWaypoints);
      }
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

    private static void DestroyObject(UnityEngine.Object target)
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

      UnityEngine.Object.Destroy(target);
    }
  }
}
