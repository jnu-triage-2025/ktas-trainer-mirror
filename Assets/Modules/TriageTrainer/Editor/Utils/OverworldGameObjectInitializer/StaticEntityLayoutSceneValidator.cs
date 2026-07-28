using System.Linq;
using TriageTrainer.Utils;
using UnityEditor;
using UnityEngine;

namespace TriageTrainer.Editor.Utils
{
  public static class StaticEntityLayoutSceneValidator
  {
    [MenuItem("Tools/Triage Trainer/Validate Static Entity Layouts")]
    private static void Validate()
    {
      var roots = Object.FindObjectsByType<GeneratedByOverworldGameObjectInitializerEditor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      var layouts = roots.SelectMany(root => Enumerable.Range(0, root.transform.childCount)
        .Select(index => root.transform.GetChild(index).name)
        .Where(name => name.StartsWith("static_entities:", System.StringComparison.Ordinal))).ToList();
      if (layouts.Count == 0)
        Debug.LogWarning("[StaticEntityLayout] 생성된 static_entities:<identifier> 오브젝트가 없습니다.");
      else
        Debug.Log("[StaticEntityLayout] 생성된 레이아웃: " + string.Join(", ", layouts));
    }
  }
}
