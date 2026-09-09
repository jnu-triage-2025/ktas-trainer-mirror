using System;
using System.Linq;
using MultiplayerInfrastructure.ItemSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Editor.ItemSystem
{
  /// <summary>Adds authoritative static pickup metadata to the two overworld nasal-cannula props.</summary>
  internal static class NasalCannulaStaticPlacementBootstrap
  {
    private const string ScenePath = "Assets/Scenes/OverworldScene.unity";

    public static void InstallOverworldPickups()
    {
      var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
      var targets = Resources.FindObjectsOfTypeAll<GameObject>()
        .Where(go => go.scene == scene && (go.name == "NasalCannula" || go.name == "NasalCannula (1)"))
        .OrderBy(go => go.name, StringComparer.Ordinal)
        .ToArray();
      if (targets.Length != 2)
        throw new InvalidOperationException($"Expected two nasal-cannula props, found {targets.Length}.");

      for (var index = 0; index < targets.Length; index++)
      {
        var target = targets[index];
        if (target.GetComponent<Collider>() == null)
        {
          var collider = target.AddComponent<BoxCollider>();
          collider.size = Vector3.one * 0.6f;
        }

        var item = target.GetComponent<StaticPlacedItem>() ?? target.AddComponent<StaticPlacedItem>();
        var serialized = new SerializedObject(item);
        serialized.FindProperty("_entityIdentifier").stringValue = $"scene-item:overworld:nasal-cannula:{index + 1}";
        var rewards = serialized.FindProperty("_pickupRewards");
        rewards.arraySize = 1;
        var reward = rewards.GetArrayElementAtIndex(0);
        reward.FindPropertyRelative("_itemIdentifier").stringValue = "nasal_cannula";
        reward.FindPropertyRelative("_amount").intValue = 1;
        reward.FindPropertyRelative("_decreaseRemains").intValue = 1;
        serialized.FindProperty("_initialState").FindPropertyRelative("_remains").intValue = 1;
        serialized.FindProperty("_autoLoadModel").boolValue = false;
        serialized.ApplyModifiedPropertiesWithoutUndo();
      }

      EditorSceneManager.MarkSceneDirty(scene);
      if (!EditorSceneManager.SaveScene(scene))
        throw new InvalidOperationException("Could not save overworld nasal-cannula pickup metadata.");
      Debug.Log("[NasalCannulaStaticPlacementBootstrap] Installed two nasal-cannula static pickups.");
    }
  }
}
