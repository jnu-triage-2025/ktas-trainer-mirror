using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.ItemSystem
{
  internal sealed class SceneItemPlacementSceneViewOptions : EditorWindow
  {
    private const string MenuRoot = "Tools/Multiplayer Infrastructure/Scene Item Visualization/";
    private const string ToggleMenuPath = MenuRoot + "Show In Scene View";
    private const string SettingsMenuPath = MenuRoot + "Settings...";

    [MenuItem(ToggleMenuPath)]
    private static void ToggleSceneViewVisibility()
    {
      bool nextValue = !EditorPrefs.GetBool(SceneItemPlacement.SceneViewVisibilityPrefKey, true);
      EditorPrefs.SetBool(SceneItemPlacement.SceneViewVisibilityPrefKey, nextValue);
      SceneView.RepaintAll();
    }

    [MenuItem(ToggleMenuPath, true)]
    private static bool ToggleSceneViewVisibilityValidate()
    {
      Menu.SetChecked(ToggleMenuPath, EditorPrefs.GetBool(SceneItemPlacement.SceneViewVisibilityPrefKey, true));
      return true;
    }

    [MenuItem(SettingsMenuPath)]
    private static void Open()
    {
      var window = GetWindow<SceneItemPlacementSceneViewOptions>(true, "Scene Item Visualization", true);
      window.minSize = new Vector2(360f, 140f);
      window.maxSize = new Vector2(520f, 180f);
      window.Show();
    }

    private void OnGUI()
    {
      EditorGUILayout.LabelField("Scene Item Visualization", EditorStyles.boldLabel);
      EditorGUILayout.Space(4f);

      bool isVisible = EditorPrefs.GetBool(SceneItemPlacement.SceneViewVisibilityPrefKey, true);
      EditorGUI.BeginChangeCheck();
      isVisible = EditorGUILayout.ToggleLeft("Show item markers and info in Scene view", isVisible);
      if (EditorGUI.EndChangeCheck())
      {
        EditorPrefs.SetBool(SceneItemPlacement.SceneViewVisibilityPrefKey, isVisible);
        SceneView.RepaintAll();
      }

      using (new EditorGUI.DisabledScope(!isVisible))
      {
        float displayRange = Mathf.Max(0f, EditorPrefs.GetFloat(SceneItemPlacement.SceneViewDisplayRangePrefKey, SceneItemPlacement.DefaultSceneViewDisplayRange));

        EditorGUI.BeginChangeCheck();
        displayRange = EditorGUILayout.Slider(
          new GUIContent("Display Range", "Distance from the current Scene camera. Set to 0 to always show."),
          displayRange,
          0f,
          200f);
        if (EditorGUI.EndChangeCheck())
        {
          EditorPrefs.SetFloat(SceneItemPlacement.SceneViewDisplayRangePrefKey, displayRange);
          SceneView.RepaintAll();
        }

        EditorGUILayout.HelpBox("Only SceneItemPlacement markers inside this distance from the Scene camera are drawn.", MessageType.Info);
      }

      EditorGUILayout.Space(6f);
      if (GUILayout.Button("Reset Defaults"))
      {
        EditorPrefs.SetBool(SceneItemPlacement.SceneViewVisibilityPrefKey, true);
        EditorPrefs.SetFloat(SceneItemPlacement.SceneViewDisplayRangePrefKey, SceneItemPlacement.DefaultSceneViewDisplayRange);
        SceneView.RepaintAll();
      }
    }
  }
}
