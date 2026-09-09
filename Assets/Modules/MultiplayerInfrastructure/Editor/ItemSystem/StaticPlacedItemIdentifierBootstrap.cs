using System;
using System.Linq;
using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Editor.ItemSystem
{
  /// <summary>Repairs duplicate serialized identifiers on static overworld pickups.</summary>
  internal static class StaticPlacedItemIdentifierBootstrap
  {
    private const string ScenePath = "Assets/Scenes/OverworldScene.unity";

    public static void NormalizeOverworldIdentifiers()
    {
      var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
      var items = Resources.FindObjectsOfTypeAll<StaticPlacedItem>()
        .Where(item => item != null && item.gameObject.scene == scene)
        .GroupBy(item => item.EntityIdentifier, StringComparer.Ordinal)
        .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
        .ToArray();

      var repaired = 0;
      foreach (var duplicates in items)
      {
        foreach (var item in duplicates)
        {
          // GlobalObjectId is stored by Unity for each scene object and stays
          // stable across clients and builds of this scene.  Hash it to keep
          // presentation entity identifiers compact and protocol-safe.
          var objectId = GlobalObjectId.GetGlobalObjectIdSlow(item).ToString();
          var unique = $"scene-item:overworld:{item.name}:{Hash128.Compute(objectId)}";
          var serialized = new SerializedObject(item);
          serialized.FindProperty("_entityIdentifier").stringValue = unique;
          serialized.ApplyModifiedPropertiesWithoutUndo();
          repaired++;
        }
      }

      if (repaired == 0)
      {
        Debug.Log("[StaticPlacedItemIdentifierBootstrap] No duplicate static pickup identifiers found.");
        return;
      }

      EditorSceneManager.MarkSceneDirty(scene);
      if (!EditorSceneManager.SaveScene(scene))
        throw new InvalidOperationException("Could not save normalized static pickup identifiers.");
      Debug.Log($"[StaticPlacedItemIdentifierBootstrap] Normalized {repaired} duplicate static pickup identifiers.");
    }
  }
}
