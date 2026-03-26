using MultiplayerInfrastructure.Player;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.Player
{
  [CustomEditor(typeof(PlayerController), true)]
  public sealed class PlayerControllerTagDebugEditor : UnityEditor.Editor
  {
    public override bool RequiresConstantRepaint()
    {
      return Application.isPlaying;
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();

      DrawDefaultInspector();

      EditorGUILayout.Space();
      EditorGUILayout.LabelField("Debug Tags", EditorStyles.boldLabel);

      using (new EditorGUI.DisabledScope(true))
      {
        var idProp = serializedObject.FindProperty("_debugTagUserIdentifier");
        var nameProp = serializedObject.FindProperty("_debugTagUserDisplayName");
        var hasIdProp = serializedObject.FindProperty("_debugHasUserIdentifier");
        var tagsProp = serializedObject.FindProperty("_debugTags");

        if (idProp == null || nameProp == null || hasIdProp == null || tagsProp == null)
        {
          EditorGUILayout.LabelField("(debug tag fields not found)");
          serializedObject.ApplyModifiedProperties();
          return;
        }

        EditorGUILayout.TextField("User Identifier", idProp.stringValue);
        EditorGUILayout.TextField("User Display Name", nameProp.stringValue);
        EditorGUILayout.Toggle("Has User Identifier", hasIdProp.boolValue);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Debug Tags", EditorStyles.boldLabel);

        if (tagsProp.arraySize == 0)
        {
          EditorGUILayout.LabelField("(none)");
        }
        else
        {
          for (int i = 0; i < tagsProp.arraySize; i++)
          {
            var item = tagsProp.GetArrayElementAtIndex(i);
            EditorGUILayout.TextField($"[{i}]", item.stringValue);
          }
        }
      }

      serializedObject.ApplyModifiedProperties();
    }
  }
}
