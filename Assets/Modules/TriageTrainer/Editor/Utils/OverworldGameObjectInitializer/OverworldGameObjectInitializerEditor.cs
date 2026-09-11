using System.Collections.Generic;
using System.Linq;
using TriageTrainer.Entity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TriageTrainer.Editor.Utils
{
  public sealed class OverworldGameObjectInitializerEditor : EditorWindow
  {
    private const string OverworldScenePath = "Assets/Scenes/OverworldScene.unity";
    private const string DefaultStaticLayoutPath =
      "Assets/Modules/TriageTrainer/ScriptableObjects/StaticEntityLayouts/OverworldPatientSupports.asset";

    private string buildingIdentifier = OverworldGameObjectInitializer.BuildingEnteranceIdentifier;
    private Vector3 buildingEnterance = OverworldGameObjectInitializer.DefaultBuildingEnterance;
    private string treatmentIdentifier = OverworldGameObjectInitializer.TreatmentRoomEnteranceIdentifier;
    private Vector3 treatmentRoomEnterance = OverworldGameObjectInitializer.DefaultTreatmentRoomEnterance;
    private string commonSpawnPointIdentifier = OverworldGameObjectInitializer.CommonSpawnPointIdentifier;
    private Vector3 commonSpawnPoint = OverworldGameObjectInitializer.DefaultCommonSpawnPoint;
    private string disasterIntroPatientASpawnWaypointIdentifier = OverworldGameObjectInitializer.DisasterIntroPatientASpawnWaypointIdentifier;
    private Vector3 disasterIntroPatientASpawnWaypoint = OverworldGameObjectInitializer.DefaultDisasterIntroPatientASpawnWaypoint;
    private string disasterIntroPatientDummyDASpawnWaypointIdentifier = OverworldGameObjectInitializer.DisasterIntroPatientDummyDASpawnWaypointIdentifier;
    private Vector3 disasterIntroPatientDummyDASpawnWaypoint = OverworldGameObjectInitializer.DefaultDisasterIntroPatientDummyDASpawnWaypoint;
    private Vector3 patientASpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientASpawnWaypoint;
    private Vector3 patientAArrivalWaypoint = OverworldGameObjectInitializer.DefaultPatientAArrivalWaypoint;
    private Vector3 patientADoctorWaypoint = OverworldGameObjectInitializer.DefaultPatientADoctorWaypoint;
    private Vector3 patientADoctorEnteredWaypoint = OverworldGameObjectInitializer.DefaultPatientADoctorEnteredWaypoint;
    private Vector3 patientBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientBSpawnWaypoint;
    private Vector3 patientCSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientCSpawnWaypoint;
    private Vector3 patientDummyDBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientDummyDBSpawnWaypoint;
    private Vector3 triageArrivalWaypoint = OverworldGameObjectInitializer.DefaultTriageArrivalWaypoint;
    private Vector3 doctorSpawnWaypoint = OverworldGameObjectInitializer.DefaultDoctorSpawnWaypoint;
    private Vector3 doctorCareAreaWaypoint = OverworldGameObjectInitializer.DefaultDoctorCareAreaWaypoint;
    private Vector3 ctPatientBWaypoint = OverworldGameObjectInitializer.DefaultCtPatientBTargetPositionWaypoint;
    private Vector3 ctPatientCWaypoint = OverworldGameObjectInitializer.DefaultCtPatientCTargetPositionWaypoint;
    private Vector3 ctRoomWaypoint = OverworldGameObjectInitializer.DefaultCtRoomWaypoint;
    private string patientBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientBSpawnWaypointIdentifier;
    private string patientASpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientASpawnWaypointIdentifier;
    private string patientAArrivalWaypointIdentifier = OverworldGameObjectInitializer.PatientAArrivalWaypointIdentifier;
    private string patientADoctorWaypointIdentifier =
      OverworldGameObjectInitializer.PatientADoctorTreatroomEnteranceWaypointIdentifier;
    private string patientADoctorEnteredWaypointIdentifier =
      OverworldGameObjectInitializer.PatientADocterTreatroomEnteredWaypointIdentifier;
    private string patientCSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientCSpawnWaypointIdentifier;
    private string patientDummyDBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientDummyDBSpawnWaypointIdentifier;
    private string triageArrivalWaypointIdentifier = OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier;
    private string doctorSpawnWaypointIdentifier = OverworldGameObjectInitializer.DoctorSpawnWaypointIdentifier;
    private string doctorCareAreaWaypointIdentifier = OverworldGameObjectInitializer.DoctorCareAreaWaypointIdentifier;
    private string doctorRouteWaypointSetIdentifier =
      OverworldGameObjectInitializer.DoctorRouteWaypointSetIdentifier;
    private string ctPatientBWaypointIdentifier = OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier;
    private string ctPatientCWaypointIdentifier = OverworldGameObjectInitializer.CtPatientCTargetPositionWaypointIdentifier;
    private string ctRoomWaypointIdentifier = OverworldGameObjectInitializer.CtRoomWaypointIdentifier;
    private Vector3 triageArrivalZoneSize = OverworldGameObjectInitializer.DefaultTriageArrivalZoneSize;
    private Vector3 patientAArrivalZoneSize = OverworldGameObjectInitializer.DefaultPatientAArrivalZoneSize;
    private Vector3 ctPatientTargetZoneSize = OverworldGameObjectInitializer.DefaultCtPatientTargetZoneSize;
    private string triageArrivalEnterSignals = string.Join(", ", OverworldGameObjectInitializer.TriageArrivalEnterSignals);
    private string patientAArrivalEnterSignals = string.Join(", ", OverworldGameObjectInitializer.PatientAArrivalEnterSignals);
    private string patientAArrivalPerEntitySignalTemplate = OverworldGameObjectInitializer.PatientAArrivalPerEntitySignalTemplate;
    private string triageArrivalPerEntitySignalTemplate = OverworldGameObjectInitializer.TriageArrivalPerEntitySignalTemplate;
    private string ctPatientArrivalEnterSignals = string.Join(", ", OverworldGameObjectInitializer.CtPatientArrivalEnterSignals);
    private string ctPatientArrivalPerEntitySignalTemplate = OverworldGameObjectInitializer.CtPatientArrivalPerEntitySignalTemplate;
    private Vector2 scrollPosition;
    [SerializeField] private List<StaticEntityLayoutDefinition> staticEntityLayouts = new();
    [SerializeField] private List<OverworldGameObjectInitializer.WaypointDefinition> doctorRouteWaypoints =
      CloneWaypoints(OverworldGameObjectInitializer.DefaultDoctorRouteWaypointSet.waypoints);
    [SerializeField] private MonoScript initializerScript;
    private SerializedObject serializedWindow;

    [MenuItem("Tools/Triage Trainer/Overworld GameObject Initializer")]
    private static void Open()
    {
      var window = GetWindow<OverworldGameObjectInitializerEditor>("Overworld Initializer");
      window.minSize = new Vector2(360f, 170f);
    }

    private void OnEnable()
    {
      serializedWindow = new SerializedObject(this);
      initializerScript = AssetDatabase.LoadAssetAtPath<MonoScript>(
        "Assets/Modules/TriageTrainer/Editor/Utils/OverworldGameObjectInitializer/OverworldGameObjectInitializer.cs");
      if (staticEntityLayouts.Count == 0)
      {
        staticEntityLayouts.Add(AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(
          DefaultStaticLayoutPath));
      }
    }

    [MenuItem("Tools/Triage Trainer/Apply Default Overworld Wiring")]
    public static void ApplyDefaultOverworldWiring()
    {
      if (EditorApplication.isPlaying)
        throw new System.InvalidOperationException("Overworld wiring cannot be generated in play mode.");

      var previousSetup = EditorSceneManager.GetSceneManagerSetup();
      try
      {
        var scene = EditorSceneManager.OpenScene(OverworldScenePath, OpenSceneMode.Single);
        var layout = AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(DefaultStaticLayoutPath);
        if (layout == null)
          throw new System.InvalidOperationException($"Missing default static layout: {DefaultStaticLayoutPath}");

        OverworldGameObjectInitializer.Set();
        OverworldGameObjectInitializer.SetStaticEntityLayouts(layout);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
          throw new System.InvalidOperationException($"Failed to save scene: {OverworldScenePath}");
        AssetDatabase.SaveAssets();
        Debug.Log($"[OverworldInitializer] Saved default world wiring to {OverworldScenePath}.");
      }
      finally
      {
        if (!Application.isBatchMode)
          EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
      }
    }

    private void OnGUI()
    {
      serializedWindow.Update();
      scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
      EditorGUILayout.HelpBox(
        "시나리오 진행 중에 오버월드에서 사용할 주요한 게임 오브젝트들을 생성합니다.\n이 값 필드들은 OverworldGameObjectInitializerEditor.cs의 코드를 수정하여야 필드를 추가할 수 있습니다. 또한 아래 필드들이 수정 가능한 상태인 것은 조정 소요가 있을 때, 임시로 값을 수정해서 확인하기 위함입니다. 만약 영구적으로 값을 수정해야 한다면 코드 리터럴을 수정해야 합니다.",
        MessageType.Info
      );
      using (new EditorGUI.DisabledScope(true))
      {
        EditorGUILayout.ObjectField("더블클릭으로 열기:", initializerScript, typeof(MonoScript), false);
      }
      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Waypoints", EditorStyles.boldLabel);

      DrawWaypointFields("Building Entrance", ref buildingIdentifier, ref buildingEnterance);
      DrawWaypointFields("Treatment Room Entrance", ref treatmentIdentifier, ref treatmentRoomEnterance);
      DrawWaypointFields("Disaster Intro Patient A Spawn", ref disasterIntroPatientASpawnWaypointIdentifier, ref disasterIntroPatientASpawnWaypoint);
      DrawWaypointFields("Disaster Intro Patient Dummy D A Spawn", ref disasterIntroPatientDummyDASpawnWaypointIdentifier, ref disasterIntroPatientDummyDASpawnWaypoint);
      DrawWaypointFields("Patient A Spawn", ref patientASpawnWaypointIdentifier, ref patientASpawnWaypoint);
      DrawWaypointFields("Patient A Arrival", ref patientAArrivalWaypointIdentifier, ref patientAArrivalWaypoint);
      DrawWaypointFields("Patient A Doctor Treatroom Enterance", ref patientADoctorWaypointIdentifier, ref patientADoctorWaypoint);
      DrawWaypointFields("Patient A Doctor Treatroom Entered", ref patientADoctorEnteredWaypointIdentifier, ref patientADoctorEnteredWaypoint);
      DrawWaypointFields("Patient B Spawn", ref patientBSpawnWaypointIdentifier, ref patientBSpawnWaypoint);
      DrawWaypointFields("Patient C Spawn", ref patientCSpawnWaypointIdentifier, ref patientCSpawnWaypoint);
      DrawWaypointFields("Patient Dummy D B Spawn", ref patientDummyDBSpawnWaypointIdentifier, ref patientDummyDBSpawnWaypoint);
      DrawWaypointFields("Scenario B Triage Arrival", ref triageArrivalWaypointIdentifier, ref triageArrivalWaypoint);
      DrawWaypointFields("Scenario B Doctor Spawn", ref doctorSpawnWaypointIdentifier, ref doctorSpawnWaypoint);
      DrawWaypointFields("Scenario B Doctor Care Area", ref doctorCareAreaWaypointIdentifier, ref doctorCareAreaWaypoint);
      DrawWaypointFields("CT Patient B", ref ctPatientBWaypointIdentifier, ref ctPatientBWaypoint);
      DrawWaypointFields("CT Patient C", ref ctPatientCWaypointIdentifier, ref ctPatientCWaypoint);
      DrawWaypointFields("CT Room", ref ctRoomWaypointIdentifier, ref ctRoomWaypoint);
      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Waypoint Sets", EditorStyles.boldLabel);
      DrawFixedWaypointSet(
        "Doctor Route",
        ref doctorRouteWaypointSetIdentifier,
        serializedWindow.FindProperty(nameof(doctorRouteWaypoints)));

      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Scenario Signal Zones", EditorStyles.boldLabel);
      DrawZoneFields(
        "Scenario A Patient Arrival Zone",
        patientAArrivalWaypointIdentifier,
        ref patientAArrivalZoneSize,
        ref patientAArrivalEnterSignals,
        ref patientAArrivalPerEntitySignalTemplate);
      DrawZoneFields(
        "Scenario B Triage Arrival Zone",
        triageArrivalWaypointIdentifier,
        ref triageArrivalZoneSize,
        ref triageArrivalEnterSignals,
        ref triageArrivalPerEntitySignalTemplate);
      DrawZoneFields(
        "CT Patient Arrival Zone (B/C 공용)",
        ctPatientBWaypointIdentifier,
        ref ctPatientTargetZoneSize,
        ref ctPatientArrivalEnterSignals,
        ref ctPatientArrivalPerEntitySignalTemplate);
      EditorGUILayout.HelpBox(
        "존은 감싸는 웨이포인트의 식별자와 위치를 그대로 쓰므로, 여기서는 상자 크기만 조정합니다.\n"
        + "도착 판정은 중심점 사이의 거리가 아니라 콜라이더 겹침으로 이루어집니다. 따라서 실효 허용 거리는 "
        + "상자 반경(크기의 절반)에 들어오는 대상의 반쪽 크기가 더해진 값입니다. 환자 침대는 2.5 x 1.25이므로 "
        + "약 1.25m가 더 붙습니다.\n"
        + "CT 존은 환자 B와 C의 웨이포인트가 같은 좌표를 공유하므로, 침대 두 대가 나란히 들어갈 폭을 유지해야 합니다.",
        MessageType.None
      );

      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Spawnpoints", EditorStyles.boldLabel);
      DrawWaypointFields("Spawnpoint Commons (player-only)", ref commonSpawnPointIdentifier, ref commonSpawnPoint);
      EditorGUILayout.HelpBox(
        "Set 실행 시 FishNet PlayerSpawner의 Spawns 배열이 CommonSpawnPoint 하나로 설정됩니다.",
        MessageType.None
      );

      EditorGUILayout.Space(8f);
      EditorGUILayout.LabelField("Static Entity Layout", EditorStyles.boldLabel);
      EditorGUILayout.PropertyField(
        serializedWindow.FindProperty(nameof(staticEntityLayouts)),
        new GUIContent("Layout Definitions"), true);
      serializedWindow.ApplyModifiedProperties();
      EditorGUILayout.HelpBox("Layout asset의 정의에 따라 환자 침대 포지셔닝 포인트, Defibrillator Cart Snap Point, Wall 장비, PatientCareDescriptionZone을 생성합니다.", MessageType.None);

      EditorGUILayout.Space(8f);
      EditorGUILayout.LabelField("Apply", EditorStyles.boldLabel);
      if (GUILayout.Button("Reset Values", GUILayout.Height(22f)))
      {
        ResetValues();
        serializedWindow.Update();
      }

      EditorGUILayout.Space(2f);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("Set", GUILayout.Height(22f)))
        {
          OverworldGameObjectInitializer.Set(
            buildingIdentifier,
            buildingEnterance,
            treatmentIdentifier,
            treatmentRoomEnterance,
            commonSpawnPointIdentifier,
            commonSpawnPoint,
            disasterIntroPatientASpawnWaypointIdentifier, disasterIntroPatientASpawnWaypoint,
            disasterIntroPatientDummyDASpawnWaypointIdentifier, disasterIntroPatientDummyDASpawnWaypoint,
            patientASpawnWaypointIdentifier, patientASpawnWaypoint,
            patientAArrivalWaypointIdentifier, patientAArrivalWaypoint,
            patientADoctorWaypointIdentifier, patientADoctorWaypoint,
            patientADoctorEnteredWaypointIdentifier, patientADoctorEnteredWaypoint,
            patientBSpawnWaypointIdentifier, patientBSpawnWaypoint,
            patientCSpawnWaypointIdentifier, patientCSpawnWaypoint,
            patientDummyDBSpawnWaypointIdentifier, patientDummyDBSpawnWaypoint,
            triageArrivalWaypointIdentifier, triageArrivalWaypoint,
            doctorSpawnWaypointIdentifier, doctorSpawnWaypoint,
            doctorCareAreaWaypointIdentifier, doctorCareAreaWaypoint,
            ctPatientBWaypointIdentifier, ctPatientBWaypoint,
            ctPatientCWaypointIdentifier, ctPatientCWaypoint,
            ctRoomWaypointIdentifier, ctRoomWaypoint,
            patientAArrivalZoneSize,
            ParseSignalList(patientAArrivalEnterSignals),
            patientAArrivalPerEntitySignalTemplate,
            triageArrivalZoneSize,
            ParseSignalList(triageArrivalEnterSignals),
            triageArrivalPerEntitySignalTemplate,
            ctPatientTargetZoneSize,
            ParseSignalList(ctPatientArrivalEnterSignals),
            ctPatientArrivalPerEntitySignalTemplate,
            new OverworldGameObjectInitializer.WaypointSetDefinition
            {
              identifier = doctorRouteWaypointSetIdentifier,
              displayName = OverworldGameObjectInitializer.DefaultDoctorRouteWaypointSet.displayName,
              waypoints = doctorRouteWaypoints
            }
          );
          var seenIdentifiers = new HashSet<string>();
          foreach (var layout in staticEntityLayouts.Where(value => value != null))
          {
            if (string.IsNullOrWhiteSpace(layout.identifier))
            {
              Debug.LogWarning("identifier가 비어 있는 StaticEntityLayout을 건너뜁니다.", layout);
              continue;
            }
            if (!seenIdentifiers.Add(layout.identifier.Trim()))
            {
              Debug.LogWarning($"중복된 StaticEntityLayout identifier를 건너뜁니다: {layout.identifier}", layout);
              continue;
            }
            OverworldGameObjectInitializer.SetStaticEntityLayouts(layout);
          }
        }

        if (GUILayout.Button("Delete", GUILayout.Height(22f)))
        {
          OverworldGameObjectInitializer.Delete();
        }
      }

      EditorGUILayout.Space(4f);
      serializedWindow.ApplyModifiedProperties();
      EditorGUILayout.EndScrollView();
    }

    private void ResetValues()
    {
      buildingIdentifier = OverworldGameObjectInitializer.BuildingEnteranceIdentifier;
      buildingEnterance = OverworldGameObjectInitializer.DefaultBuildingEnterance;
      treatmentIdentifier = OverworldGameObjectInitializer.TreatmentRoomEnteranceIdentifier;
      treatmentRoomEnterance = OverworldGameObjectInitializer.DefaultTreatmentRoomEnterance;
      commonSpawnPointIdentifier = OverworldGameObjectInitializer.CommonSpawnPointIdentifier;
      commonSpawnPoint = OverworldGameObjectInitializer.DefaultCommonSpawnPoint;
      disasterIntroPatientASpawnWaypointIdentifier = OverworldGameObjectInitializer.DisasterIntroPatientASpawnWaypointIdentifier;
      disasterIntroPatientASpawnWaypoint = OverworldGameObjectInitializer.DefaultDisasterIntroPatientASpawnWaypoint;
      disasterIntroPatientDummyDASpawnWaypointIdentifier = OverworldGameObjectInitializer.DisasterIntroPatientDummyDASpawnWaypointIdentifier;
      disasterIntroPatientDummyDASpawnWaypoint = OverworldGameObjectInitializer.DefaultDisasterIntroPatientDummyDASpawnWaypoint;
      patientASpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientASpawnWaypoint;
      patientAArrivalWaypoint = OverworldGameObjectInitializer.DefaultPatientAArrivalWaypoint;
      patientADoctorWaypoint = OverworldGameObjectInitializer.DefaultPatientADoctorWaypoint;
      patientADoctorEnteredWaypoint = OverworldGameObjectInitializer.DefaultPatientADoctorEnteredWaypoint;
      patientBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientBSpawnWaypoint;
      patientCSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientCSpawnWaypoint;
      patientDummyDBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientDummyDBSpawnWaypoint;
      triageArrivalWaypoint = OverworldGameObjectInitializer.DefaultTriageArrivalWaypoint;
      doctorSpawnWaypoint = OverworldGameObjectInitializer.DefaultDoctorSpawnWaypoint;
      doctorCareAreaWaypoint = OverworldGameObjectInitializer.DefaultDoctorCareAreaWaypoint;
      ctPatientBWaypoint = OverworldGameObjectInitializer.DefaultCtPatientBTargetPositionWaypoint;
      ctPatientCWaypoint = OverworldGameObjectInitializer.DefaultCtPatientCTargetPositionWaypoint;
      ctRoomWaypoint = OverworldGameObjectInitializer.DefaultCtRoomWaypoint;
      patientBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientBSpawnWaypointIdentifier;
      patientASpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientASpawnWaypointIdentifier;
      patientAArrivalWaypointIdentifier = OverworldGameObjectInitializer.PatientAArrivalWaypointIdentifier;
      patientADoctorWaypointIdentifier =
        OverworldGameObjectInitializer.PatientADoctorTreatroomEnteranceWaypointIdentifier;
      patientADoctorEnteredWaypointIdentifier =
        OverworldGameObjectInitializer.PatientADocterTreatroomEnteredWaypointIdentifier;
      patientCSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientCSpawnWaypointIdentifier;
      patientDummyDBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientDummyDBSpawnWaypointIdentifier;
      triageArrivalWaypointIdentifier = OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier;
      doctorSpawnWaypointIdentifier = OverworldGameObjectInitializer.DoctorSpawnWaypointIdentifier;
      doctorCareAreaWaypointIdentifier = OverworldGameObjectInitializer.DoctorCareAreaWaypointIdentifier;
      doctorRouteWaypointSetIdentifier =
        OverworldGameObjectInitializer.DoctorRouteWaypointSetIdentifier;
      ctPatientBWaypointIdentifier = OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier;
      ctPatientCWaypointIdentifier = OverworldGameObjectInitializer.CtPatientCTargetPositionWaypointIdentifier;
      ctRoomWaypointIdentifier = OverworldGameObjectInitializer.CtRoomWaypointIdentifier;
      triageArrivalZoneSize = OverworldGameObjectInitializer.DefaultTriageArrivalZoneSize;
      patientAArrivalZoneSize = OverworldGameObjectInitializer.DefaultPatientAArrivalZoneSize;
      ctPatientTargetZoneSize = OverworldGameObjectInitializer.DefaultCtPatientTargetZoneSize;
      triageArrivalEnterSignals = string.Join(", ", OverworldGameObjectInitializer.TriageArrivalEnterSignals);
      patientAArrivalEnterSignals = string.Join(", ", OverworldGameObjectInitializer.PatientAArrivalEnterSignals);
      patientAArrivalPerEntitySignalTemplate = OverworldGameObjectInitializer.PatientAArrivalPerEntitySignalTemplate;
      triageArrivalPerEntitySignalTemplate = OverworldGameObjectInitializer.TriageArrivalPerEntitySignalTemplate;
      ctPatientArrivalEnterSignals = string.Join(", ", OverworldGameObjectInitializer.CtPatientArrivalEnterSignals);
      ctPatientArrivalPerEntitySignalTemplate = OverworldGameObjectInitializer.CtPatientArrivalPerEntitySignalTemplate;
      doctorRouteWaypoints = CloneWaypoints(
        OverworldGameObjectInitializer.DefaultDoctorRouteWaypointSet.waypoints);
      staticEntityLayouts.Clear();
      staticEntityLayouts.Add(AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(
        DefaultStaticLayoutPath));
    }

    private static void DrawWaypointFields(string title, ref string identifier, ref Vector3 value)
    {
      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
      {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        identifier = EditorGUILayout.TextField("Identifier", identifier);
        value = EditorGUILayout.Vector3Field("Value", value);
      }
    }

    private static void DrawFixedWaypointSet(
      string title, ref string identifier, SerializedProperty waypoints)
    {
      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
      {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        identifier = EditorGUILayout.TextField("Identifier", identifier);
        EditorGUILayout.PropertyField(waypoints, new GUIContent("Waypoints"), true);
        EditorGUILayout.HelpBox(
          "이 waypoint set 항목은 코드에 고정되어 있습니다. Waypoints 목록의 항목 순서가 NPC 이동 순서가 됩니다.",
          MessageType.None);
      }
    }

    private static List<OverworldGameObjectInitializer.WaypointDefinition> CloneWaypoints(
      IEnumerable<OverworldGameObjectInitializer.WaypointDefinition> source)
    {
      return source == null
        ? new List<OverworldGameObjectInitializer.WaypointDefinition>()
        : source.Where(waypoint => waypoint != null)
          .Select(waypoint => new OverworldGameObjectInitializer.WaypointDefinition
          {
            identifier = waypoint.identifier,
            position = waypoint.position
          }).ToList();
    }

    /// <summary>
    /// 시나리오 신호 존의 생성 값을 그린다. 존은 감싸는 웨이포인트에서 식별자와 위치를 가져온다.
    /// </summary>
    private static void DrawZoneFields(
      string title, string identifier, ref Vector3 size,
      ref string enterSignals, ref string perEntitySignalTemplate)
    {
      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
      {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Identifier", identifier);
        size = EditorGUILayout.Vector3Field("Size (m)", size);
        enterSignals = EditorGUILayout.TextField("Enter Signals (CSV)", enterSignals);
        perEntitySignalTemplate = EditorGUILayout.TextField("Per-Entity Signal", perEntitySignalTemplate);
        EditorGUILayout.LabelField(
          "Half Extent",
          $"x {size.x * 0.5f:0.##}m / z {size.z * 0.5f:0.##}m");
      }
    }

    private static string[] ParseSignalList(string value)
    {
      if (string.IsNullOrWhiteSpace(value))
        return System.Array.Empty<string>();

      return value.Split(',')
        .Select(signal => signal.Trim())
        .Where(signal => !string.IsNullOrWhiteSpace(signal))
        .Distinct()
        .ToArray();
    }
  }
}
