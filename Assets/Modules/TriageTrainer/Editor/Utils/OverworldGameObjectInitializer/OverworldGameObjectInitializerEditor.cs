using TriageTrainer.Utils;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Editor.Utils
{
  public sealed class OverworldGameObjectInitializerEditor : EditorWindow
  {
    [MenuItem("Tools/Triage Trainer/Overworld GameObject Initializer")]
    private static void Open()
    {
      var window = GetWindow<OverworldGameObjectInitializerEditor>("Overworld Initializer");
      window.minSize = new Vector2(360f, 170f);
    }

    private void OnGUI()
    {
      EditorGUILayout.HelpBox(
        "시나리오 진행 중에 오버월드에서 사용할 주요한 게임 오브젝트들을 생성합니다.",
        MessageType.Info
      );
      EditorGUILayout.LabelField("Waypoints", EditorStyles.boldLabel);
      EditorGUILayout.Space(4f);

      EditorGUILayout.TextField("Building Identifier", OverworldGameObjectInitializer.BuildingEnteranceIdentifier);
      EditorGUILayout.Vector3Field("BuildingEnterance", OverworldGameObjectInitializer.DefaultBuildingEnterance);
      EditorGUILayout.TextField("Treatment Identifier", OverworldGameObjectInitializer.TreatmentRoomEnteranceIdentifier);
      EditorGUILayout.Vector3Field("TreatmentRoomEnterance", OverworldGameObjectInitializer.DefaultTreatmentRoomEnterance);

      EditorGUILayout.Space(8f);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("Set", GUILayout.Height(22f)))
        {
          OverworldGameObjectInitializer.Set();
        }

        if (GUILayout.Button("Delete", GUILayout.Height(22f)))
        {
          OverworldGameObjectInitializer.Delete();
        }
      }
    }
  }
}
