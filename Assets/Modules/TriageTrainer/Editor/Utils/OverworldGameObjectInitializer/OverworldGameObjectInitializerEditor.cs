using System.Collections.Generic;
using System.Linq;
using TriageTrainer.Utils;
using UnityEditor;
using UnityEngine;
using TriageTrainer.Entity;

namespace TriageTrainer.Editor.Utils
{
  public sealed class OverworldGameObjectInitializerEditor : EditorWindow
  {
    private string buildingIdentifier = OverworldGameObjectInitializer.BuildingEnteranceIdentifier;
    private Vector3 buildingEnterance = OverworldGameObjectInitializer.DefaultBuildingEnterance;
    private string treatmentIdentifier = OverworldGameObjectInitializer.TreatmentRoomEnteranceIdentifier;
    private Vector3 treatmentRoomEnterance = OverworldGameObjectInitializer.DefaultTreatmentRoomEnterance;
    private string commonSpawnPointIdentifier = OverworldGameObjectInitializer.CommonSpawnPointIdentifier;
    private Vector3 commonSpawnPoint = OverworldGameObjectInitializer.DefaultCommonSpawnPoint;
    private Vector3 patientBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientBSpawnWaypoint;
    private Vector3 patientCSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientCSpawnWaypoint;
    private Vector3 patientDummyDBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientDummyDBSpawnWaypoint;
    private Vector3 triageArrivalWaypoint = OverworldGameObjectInitializer.DefaultTriageArrivalWaypoint;
    private Vector3 doctorSpawnWaypoint = OverworldGameObjectInitializer.DefaultDoctorSpawnWaypoint;
    private Vector3 doctorCareAreaWaypoint = OverworldGameObjectInitializer.DefaultDoctorCareAreaWaypoint;
    private Vector3 ctPatientBWaypoint = OverworldGameObjectInitializer.DefaultCtPatientBTargetPositionWaypoint;
    private Vector3 ctPatientCWaypoint = OverworldGameObjectInitializer.DefaultCtPatientCTargetPositionWaypoint;
    private string patientBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientBSpawnWaypointIdentifier;
    private string patientCSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientCSpawnWaypointIdentifier;
    private string patientDummyDBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientDummyDBSpawnWaypointIdentifier;
    private string triageArrivalWaypointIdentifier = OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier;
    private string doctorSpawnWaypointIdentifier = OverworldGameObjectInitializer.DoctorSpawnWaypointIdentifier;
    private string doctorCareAreaWaypointIdentifier = OverworldGameObjectInitializer.DoctorCareAreaWaypointIdentifier;
    private string ctPatientBWaypointIdentifier = OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier;
    private string ctPatientCWaypointIdentifier = OverworldGameObjectInitializer.CtPatientCTargetPositionWaypointIdentifier;
    private Vector2 scrollPosition;
    [SerializeField] private List<StaticEntityLayoutDefinition> staticEntityLayouts = new();
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
          "Assets/Modules/TriageTrainer/ScriptableObjects/StaticEntityLayouts/OverworldPatientSupports.asset"));
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
      DrawWaypointFields("Patient B Spawn", ref patientBSpawnWaypointIdentifier, ref patientBSpawnWaypoint);
      DrawWaypointFields("Patient C Spawn", ref patientCSpawnWaypointIdentifier, ref patientCSpawnWaypoint);
      DrawWaypointFields("Patient Dummy D B Spawn", ref patientDummyDBSpawnWaypointIdentifier, ref patientDummyDBSpawnWaypoint);
      DrawWaypointFields("Scenario B Triage Arrival", ref triageArrivalWaypointIdentifier, ref triageArrivalWaypoint);
      DrawWaypointFields("Scenario B Doctor Spawn", ref doctorSpawnWaypointIdentifier, ref doctorSpawnWaypoint);
      DrawWaypointFields("Scenario B Doctor Care Area", ref doctorCareAreaWaypointIdentifier, ref doctorCareAreaWaypoint);
      DrawWaypointFields("CT Patient B", ref ctPatientBWaypointIdentifier, ref ctPatientBWaypoint);
      DrawWaypointFields("CT Patient C", ref ctPatientCWaypointIdentifier, ref ctPatientCWaypoint);

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
      EditorGUILayout.HelpBox("Layout asset의 정의에 따라 MovingPatientBedPositioningPoint, Wall 장비, PatientCareDescriptionZone을 생성합니다.", MessageType.None);

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
            patientBSpawnWaypointIdentifier, patientBSpawnWaypoint,
            patientCSpawnWaypointIdentifier, patientCSpawnWaypoint,
            patientDummyDBSpawnWaypointIdentifier, patientDummyDBSpawnWaypoint,
            triageArrivalWaypointIdentifier, triageArrivalWaypoint,
            doctorSpawnWaypointIdentifier, doctorSpawnWaypoint,
            doctorCareAreaWaypointIdentifier, doctorCareAreaWaypoint,
            ctPatientBWaypointIdentifier, ctPatientBWaypoint,
            ctPatientCWaypointIdentifier, ctPatientCWaypoint
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
      patientBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientBSpawnWaypoint;
      patientCSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientCSpawnWaypoint;
      patientDummyDBSpawnWaypoint = OverworldGameObjectInitializer.DefaultPatientDummyDBSpawnWaypoint;
      triageArrivalWaypoint = OverworldGameObjectInitializer.DefaultTriageArrivalWaypoint;
      doctorSpawnWaypoint = OverworldGameObjectInitializer.DefaultDoctorSpawnWaypoint;
      doctorCareAreaWaypoint = OverworldGameObjectInitializer.DefaultDoctorCareAreaWaypoint;
      ctPatientBWaypoint = OverworldGameObjectInitializer.DefaultCtPatientBTargetPositionWaypoint;
      ctPatientCWaypoint = OverworldGameObjectInitializer.DefaultCtPatientCTargetPositionWaypoint;
      patientBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientBSpawnWaypointIdentifier;
      patientCSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientCSpawnWaypointIdentifier;
      patientDummyDBSpawnWaypointIdentifier = OverworldGameObjectInitializer.PatientDummyDBSpawnWaypointIdentifier;
      triageArrivalWaypointIdentifier = OverworldGameObjectInitializer.TriageArrivalWaypointIdentifier;
      doctorSpawnWaypointIdentifier = OverworldGameObjectInitializer.DoctorSpawnWaypointIdentifier;
      doctorCareAreaWaypointIdentifier = OverworldGameObjectInitializer.DoctorCareAreaWaypointIdentifier;
      ctPatientBWaypointIdentifier = OverworldGameObjectInitializer.CtPatientBTargetPositionWaypointIdentifier;
      ctPatientCWaypointIdentifier = OverworldGameObjectInitializer.CtPatientCTargetPositionWaypointIdentifier;
      staticEntityLayouts.Clear();
      staticEntityLayouts.Add(AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(
        "Assets/Modules/TriageTrainer/ScriptableObjects/StaticEntityLayouts/OverworldPatientSupports.asset"));
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

    private static void DrawWaypointFields(string title, string identifier, ref Vector3 value)
    {
      using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
      {
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Identifier", identifier);
        value = EditorGUILayout.Vector3Field("Value", value);
      }
    }
  }
}
