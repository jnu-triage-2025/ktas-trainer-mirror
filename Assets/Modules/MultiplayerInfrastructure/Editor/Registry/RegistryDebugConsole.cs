#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public class RegistryDebugConsole : EditorWindow
  {
    private RegistryType _registerType = RegistryType.Item;
    private string _registerIdentifier = string.Empty;
    private UnityEngine.Object _registerValue;

    private RegistryType _viewType = RegistryType.Item;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Multiplayer Infrastructure/Registry Debug Console")]
    private static void Open()
    {
      var window = GetWindow<RegistryDebugConsole>("Registry Debug Console");
      window.minSize = new Vector2(520f, 360f);
    }

    private void OnGUI()
    {
      DrawRegisterSection();
      EditorGUILayout.Space(8f);
      DrawViewSection();
    }

    private void DrawRegisterSection()
    {
      EditorGUILayout.LabelField("Register", EditorStyles.boldLabel);

      _registerType = (RegistryType)EditorGUILayout.EnumPopup("Registry Type", _registerType);
      _registerIdentifier = EditorGUILayout.TextField("Identifier", _registerIdentifier);
      _registerValue = EditorGUILayout.ObjectField("Value", _registerValue, typeof(UnityEngine.Object), true);

      using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_registerIdentifier) || _registerValue == null))
      {
        if (!GUILayout.Button("Register Value"))
        {
          return;
        }

        Registry.Register(_registerType, _registerIdentifier, _registerValue);
        Repaint();
      }
    }

    private void DrawViewSection()
    {
      EditorGUILayout.LabelField("Entries", EditorStyles.boldLabel);
      _viewType = (RegistryType)EditorGUILayout.EnumPopup("Registry Type", _viewType);

      IReadOnlyDictionary<string, object> entries = Registry.GetAll<object>(_viewType);
      EditorGUILayout.LabelField($"Count: {entries.Count}");

      _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
      foreach (KeyValuePair<string, object> entry in entries)
      {
        DrawEntry(_viewType, entry.Key, entry.Value);
      }
      EditorGUILayout.EndScrollView();
    }

    private static void DrawEntry(RegistryType registryType, string identifier, object value)
    {
      using (new EditorGUILayout.HorizontalScope())
      {
        EditorGUILayout.LabelField(identifier, GUILayout.MinWidth(220f));

        if (registryType == RegistryType.IconSprite)
        {
          Sprite sprite = value as Sprite;
          string resourcePath = value as string;

          if (sprite == null && !string.IsNullOrWhiteSpace(resourcePath))
            sprite = Resources.Load<Sprite>(resourcePath);

          EditorGUILayout.ObjectField(sprite, typeof(Sprite), false, GUILayout.Width(180f));

          if (!string.IsNullOrWhiteSpace(resourcePath))
            EditorGUILayout.SelectableLabel(resourcePath, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
          else
            EditorGUILayout.SelectableLabel(sprite != null ? sprite.name : "null", EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

          return;
        }

        if (value is UnityEngine.Object unityObject)
        {
          EditorGUILayout.ObjectField(unityObject, typeof(UnityEngine.Object), true);
          return;
        }

        string valueText = value?.ToString() ?? "null";
        EditorGUILayout.SelectableLabel(valueText, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
      }
    }
  }
}
#endif
