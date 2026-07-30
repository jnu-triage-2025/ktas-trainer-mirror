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
    [SerializeField] private List<StaticEntityLayoutDefinition> staticEntityLayouts = new();
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
      if (staticEntityLayouts.Count == 0)
      {
        staticEntityLayouts.Add(AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(
          "Assets/Modules/TriageTrainer/ScriptableObjects/StaticEntityLayouts/OverworldPatientSupports.asset"));
      }
    }

    private void OnGUI()
    {
      serializedWindow.Update();
      EditorGUILayout.HelpBox(
        "시나리오 진행 중에 오버월드에서 사용할 주요한 게임 오브젝트들을 생성합니다.",
        MessageType.Info
      );
      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Waypoints", EditorStyles.boldLabel);

      buildingIdentifier = EditorGUILayout.TextField("Building Identifier", buildingIdentifier);
      buildingEnterance = EditorGUILayout.Vector3Field("BuildingEnterance", buildingEnterance);
      treatmentIdentifier = EditorGUILayout.TextField("Treatment Identifier", treatmentIdentifier);
      treatmentRoomEnterance = EditorGUILayout.Vector3Field("TreatmentRoomEnterance", treatmentRoomEnterance);

      EditorGUILayout.Space(4f);
      EditorGUILayout.LabelField("Spawnpoints", EditorStyles.boldLabel);
      commonSpawnPointIdentifier = EditorGUILayout.TextField("Common Identifier", commonSpawnPointIdentifier);
      commonSpawnPoint = EditorGUILayout.Vector3Field("Common SpawnPoint", commonSpawnPoint);
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

      if (GUILayout.Button("Reset Values", GUILayout.Height(22f)))
      {
        ResetValues();
        serializedWindow.Update();
      }

      EditorGUILayout.Space(4f);

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
            commonSpawnPoint
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

      serializedWindow.ApplyModifiedProperties();
    }

    private void ResetValues()
    {
      buildingIdentifier = OverworldGameObjectInitializer.BuildingEnteranceIdentifier;
      buildingEnterance = OverworldGameObjectInitializer.DefaultBuildingEnterance;
      treatmentIdentifier = OverworldGameObjectInitializer.TreatmentRoomEnteranceIdentifier;
      treatmentRoomEnterance = OverworldGameObjectInitializer.DefaultTreatmentRoomEnterance;
      commonSpawnPointIdentifier = OverworldGameObjectInitializer.CommonSpawnPointIdentifier;
      commonSpawnPoint = OverworldGameObjectInitializer.DefaultCommonSpawnPoint;
      staticEntityLayouts.Clear();
      staticEntityLayouts.Add(AssetDatabase.LoadAssetAtPath<StaticEntityLayoutDefinition>(
        "Assets/Modules/TriageTrainer/ScriptableObjects/StaticEntityLayouts/OverworldPatientSupports.asset"));
    }
  }
}
