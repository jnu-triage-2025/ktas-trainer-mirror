using System;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.ItemSystem
{
  [CustomEditor(typeof(SceneItemPlacement))]
  public class SceneItemPlacementEditor : UnityEditor.Editor
  {
    // ─── 캐시 ──────────────────────────────────────────────────────────────
    private static List<(string identifier, string displayName, Type type)> _itemCache;
    private static string[] _dropdownLabels; // "identifier (DisplayName)"
    private static string[] _identifiers;

    private SerializedProperty _identifierProp;
    private SerializedProperty _stackCountProp;

    // ─── 라이프사이클 ──────────────────────────────────────────────────────
    private void OnEnable()
    {
      _identifierProp = serializedObject.FindProperty("_itemIdentifier");
      _stackCountProp = serializedObject.FindProperty("_stackCount");
      EnsureCache();
    }

    public override void OnInspectorGUI()
    {
      serializedObject.Update();
      EnsureCache();

      // ── Identifier 드롭다운 ─────────────────────────────────────────────
      string current = _identifierProp.stringValue;
      int currentIndex = Array.IndexOf(_identifiers, current);
      // 0번은 "(선택 없음)" 슬롯
      int displayIndex = currentIndex < 0 ? 0 : currentIndex + 1;

      // 레이블 배열: 앞에 "(선택 없음)" 추가
      string[] labelsWithNone = new string[_dropdownLabels.Length + 1];
      labelsWithNone[0] = "(선택 없음)";
      Array.Copy(_dropdownLabels, 0, labelsWithNone, 1, _dropdownLabels.Length);

      EditorGUILayout.LabelField("Item Identifier", EditorStyles.boldLabel);

      int chosen = EditorGUILayout.Popup("Item", displayIndex, labelsWithNone);
      if (chosen == 0)
      {
        _identifierProp.stringValue = string.Empty;
      }
      else
      {
        _identifierProp.stringValue = _identifiers[chosen - 1];
      }

      // ── 선택된 아이템 정보 표시 ────────────────────────────────────────
      if (chosen > 0)
      {
        var entry = _itemCache[chosen - 1];
        EditorGUILayout.HelpBox(
          $"Type: {entry.type.FullName}\nIdentifier: {entry.identifier}\nDisplayName: {entry.displayName}",
          MessageType.None);
      }
      else if (!string.IsNullOrEmpty(current))
      {
        EditorGUILayout.HelpBox(
          $"'{current}' 은(는) 현재 프로젝트에서 찾을 수 없는 identifier입니다.",
          MessageType.Warning);
      }

      EditorGUILayout.Space(4);

      // ── Stack Count ────────────────────────────────────────────────────
      EditorGUILayout.PropertyField(_stackCountProp, new GUIContent("Stack Count"));

      EditorGUILayout.Space(6);

      // ── 새로고침 버튼 ──────────────────────────────────────────────────
      if (GUILayout.Button("아이템 목록 새로고침", GUILayout.Height(22)))
      {
        _itemCache = null;
        EnsureCache();
        Repaint();
      }

      serializedObject.ApplyModifiedProperties();
    }

    // ─── 캐시 빌드 ────────────────────────────────────────────────────────

    private static void EnsureCache()
    {
      if (_itemCache != null)
        return;

      _itemCache = new List<(string, string, Type)>();

      var baseType = typeof(Item);
      const BindingFlags flags =
        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
      {
        Type[] types;
        try
        { types = asm.GetTypes(); }
        catch { continue; }

        foreach (var t in types)
        {
          if (t.IsAbstract || t.IsInterface)
            continue;
          if (!baseType.IsAssignableFrom(t))
            continue;

          var idField = t.GetField("Identifier", flags);
          if (idField == null || idField.FieldType != typeof(string))
            continue;

          string id = (string)idField.GetValue(null);
          if (string.IsNullOrEmpty(id))
            continue;

          string dn = id;
          var dnField = t.GetField("DisplayName", flags);
          if (dnField != null && dnField.FieldType == typeof(string))
          {
            string v = (string)dnField.GetValue(null);
            if (!string.IsNullOrEmpty(v))
              dn = v;
          }

          _itemCache.Add((id, dn, t));
        }
      }

      // identifier 기준 정렬
      _itemCache.Sort((a, b) => string.Compare(a.identifier, b.identifier, StringComparison.OrdinalIgnoreCase));

      _identifiers = new string[_itemCache.Count];
      _dropdownLabels = new string[_itemCache.Count];

      for (int i = 0; i < _itemCache.Count; i++)
      {
        _identifiers[i] = _itemCache[i].identifier;
        _dropdownLabels[i] = $"{_itemCache[i].identifier}  ({_itemCache[i].displayName})";
      }
    }
  }
}
