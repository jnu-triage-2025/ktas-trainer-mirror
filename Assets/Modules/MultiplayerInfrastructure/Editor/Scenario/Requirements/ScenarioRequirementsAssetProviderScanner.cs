#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  internal static class ScenarioRequirementsAssetProviderScanner
  {
    public static void AddProviders(
      ScenarioRequirementManifest manifest,
      List<ScenarioRequirementProvider> providers,
      List<ScenarioRequirementValidationDiagnostic> diagnostics,
      IDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> completeness)
    {
      foreach (var requirement in manifest.Requirements)
      {
        switch (requirement.Kind)
        {
          case ScenarioRequirementKind.AudioResource:
            AddAudio(requirement, providers, completeness);
            break;
          case ScenarioRequirementKind.SpriteResource:
            AddSprite(requirement, providers, completeness);
            break;
          case ScenarioRequirementKind.EventHandler:
            MarkRuntimeRegistered(requirement, completeness);
            break;
          case ScenarioRequirementKind.EntityPreset:
            MarkRuntimeRegistered(requirement, completeness);
            break;
          case ScenarioRequirementKind.ItemDefinition:
            MarkRuntimeRegistered(requirement, completeness);
            break;
          case ScenarioRequirementKind.QuestDefinition:
            AddQuestDefinition(requirement, providers, completeness);
            break;
        }
      }
    }

    private static void AddAudio(ScenarioRequirementDescriptor requirement, List<ScenarioRequirementProvider> providers, IDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> completeness)
    {
      var clip = FindResource<AudioClip>("Sound/" + requirement.Identifier) ?? FindResource<AudioClip>(requirement.Identifier);
      if (clip == null) return;
      providers.Add(AssetProvider(requirement.Key, "resource:audio:" + requirement.Identifier, ScenarioRequirementCapability.LoadableResource, clip));
      completeness[ScenarioRequirementKind.AudioResource] = ScenarioRequirementEvidenceCompleteness.Complete;
    }

    private static void AddSprite(ScenarioRequirementDescriptor requirement, List<ScenarioRequirementProvider> providers, IDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> completeness)
    {
      var sprite = FindResource<Sprite>(requirement.Identifier);
      if (sprite == null) sprite = FindResource<Sprite>("ItemTextures/" + requirement.Identifier);
      if (sprite == null) return;
      providers.Add(AssetProvider(requirement.Key, "resource:sprite:" + requirement.Identifier, ScenarioRequirementCapability.LoadableResource, sprite));
      completeness[ScenarioRequirementKind.SpriteResource] = ScenarioRequirementEvidenceCompleteness.Complete;
    }

    // Registry state belongs to a running player.  Editor and build validation
    // must remain deterministic and read-only, so these kinds are deliberately
    // left as partial evidence until an asset-backed provider is introduced.
    private static void MarkRuntimeRegistered(ScenarioRequirementDescriptor requirement, IDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> completeness)
    {
      if (!completeness.ContainsKey(requirement.Kind))
        completeness[requirement.Kind] = ScenarioRequirementEvidenceCompleteness.Partial;
    }

    private static void AddQuestDefinition(ScenarioRequirementDescriptor requirement, List<ScenarioRequirementProvider> providers, IDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> completeness)
    {
      var guids = AssetDatabase.FindAssets("t:TextAsset", new[] { "Assets/Resources" });
      foreach (var guid in guids)
      {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset == null || !string.Equals(asset.name, requirement.Identifier, StringComparison.Ordinal)) continue;
        providers.Add(AssetProvider(requirement.Key, "asset:quest:" + guid, ScenarioRequirementCapability.ResolvableQuestDefinition, asset));
        completeness[ScenarioRequirementKind.QuestDefinition] = ScenarioRequirementEvidenceCompleteness.Complete;
      }
    }

    private static ScenarioRequirementProvider AssetProvider(ScenarioRequirementKey key, string identity, ScenarioRequirementCapability capability, UnityEngine.Object asset)
      => new ScenarioRequirementProvider(identity, key, AssetDatabase.GetAssetPath(asset), ScenarioRequirementScope.AnyLoadedScene, string.Empty, new[] { capability }, true, true, true, ScenarioRequirementProviderOrigin.ProjectContributor);

    private static T FindResource<T>(string resourcePath) where T : UnityEngine.Object
    {
      var normalized = (resourcePath ?? string.Empty).Replace('\\', '/').Trim('/');
      if (string.IsNullOrWhiteSpace(normalized)) return null;
      foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
      {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var resourcesIndex = path.IndexOf("/Resources/", StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0) continue;
        var relative = path.Substring(resourcesIndex + "/Resources/".Length);
        var extension = System.IO.Path.GetExtension(relative);
        if (!string.IsNullOrEmpty(extension)) relative = relative.Substring(0, relative.Length - extension.Length);
        if (!string.Equals(relative, normalized, StringComparison.Ordinal)) continue;
        return AssetDatabase.LoadAssetAtPath<T>(path);
      }
      return null;
    }
  }
}
#endif
