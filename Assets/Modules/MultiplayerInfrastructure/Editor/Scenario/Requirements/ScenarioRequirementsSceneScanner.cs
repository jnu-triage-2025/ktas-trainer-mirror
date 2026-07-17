#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Entity;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  public static class ScenarioRequirementsSceneScanner
  {
    public static ScenarioRequirementProviderSnapshot Scan(ScenarioRequirementManifest manifest, ScenarioRequirementSceneComposition composition)
    {
      var providers = new List<ScenarioRequirementProvider>();
      var diagnostics = new List<ScenarioRequirementValidationDiagnostic>();
      // Prefab Stage is validated separately from the Main Stage.  An open
      // Prefab Stage must not suppress Main Stage composition validation, so we
      // do not abort the whole scan here; instead each composition scene is
      // scanned and the Prefab Stage's own preview scene is explicitly excluded
      // below (proposal §11, blueprint §7).
      var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
      if (prefabStage != null)
        diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR313", "PrefabStageExcludedFromScan", ScenarioRequirementDiagnosticSeverity.Info, "A Prefab Stage is open; it is excluded from scenario requirement scene validation and does not affect Main Stage results."));
      var prefabStageScene = prefabStage != null ? prefabStage.scene : default;
      var previewScenes = new List<Scene>();
      try
      {
        foreach (var entry in composition.Scenes)
        {
          var scene = SceneManager.GetSceneByPath(entry.ScenePath);
          if (scene.IsValid() && prefabStageScene.IsValid() && scene == prefabStageScene)
          {
            // Never treat the Prefab Stage's scene as a composition scene.
            diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR300", "MalformedSceneComposition", ScenarioRequirementDiagnosticSeverity.Error, $"Scene '{entry.ScenePath}' resolves to the open Prefab Stage and cannot be scanned as a composition scene.", scenePath: entry.ScenePath));
            continue;
          }
          if (!scene.IsValid() || !scene.isLoaded)
          {
            if (string.IsNullOrWhiteSpace(entry.ScenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(entry.ScenePath) == null)
            { diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR300", "MalformedSceneComposition", ScenarioRequirementDiagnosticSeverity.Error, $"Scene asset '{entry.ScenePath}' does not exist.", scenePath: entry.ScenePath)); continue; }
            // Build validation must not load authoring scenes into the main
            // stage: doing so can fire registrations and dirty user scenes.
            // Preview scenes provide the same serialized hierarchy for a
            // read-only scan without changing the active scene set.
            scene = EditorSceneManager.OpenPreviewScene(entry.ScenePath);
            previewScenes.Add(scene);
          }
          ScanScene(scene, entry, manifest, composition, providers, diagnostics);
        }
      }
      catch (Exception ex)
      {
        diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR312", "SceneScanFailed", ScenarioRequirementDiagnosticSeverity.Error, ex.Message));
      }
      finally
      {
        for (var index = previewScenes.Count - 1; index >= 0; index--) EditorSceneManager.ClosePreviewScene(previewScenes[index]);
      }
      var completeness = new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>
      {
        [ScenarioRequirementKind.Npc] = ScenarioRequirementEvidenceCompleteness.Complete,
        [ScenarioRequirementKind.SpatialAnchor] = ScenarioRequirementEvidenceCompleteness.Complete,
        [ScenarioRequirementKind.Interactable] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.SpawnPoint] = ScenarioRequirementEvidenceCompleteness.Partial,
        [ScenarioRequirementKind.Entity] = ScenarioRequirementEvidenceCompleteness.Partial
      };
      ScenarioRequirementsAssetProviderScanner.AddProviders(manifest, providers, diagnostics, completeness);
      return new ScenarioRequirementProviderSnapshot(providers, diagnostics, completeness);
    }

    private static void ScanScene(Scene scene, ScenarioRequirementCompositionScene entry, ScenarioRequirementManifest manifest, ScenarioRequirementSceneComposition composition, List<ScenarioRequirementProvider> providers, List<ScenarioRequirementValidationDiagnostic> diagnostics)
    {
      var bindings = new List<ScenarioRequirementsSceneBinding>();
      foreach (var root in scene.GetRootGameObjects())
      {
        bindings.AddRange(root.GetComponentsInChildren<ScenarioRequirementsSceneBinding>(true));
        foreach (var component in root.GetComponentsInChildren<Component>(true))
        {
          if (component == null) continue;
          if (component is WaypointAnchor waypoint) AddProvider(providers, diagnostics, waypoint, ScenarioRequirementKind.SpatialAnchor, waypoint.Identifier, entry, waypoint.SupportsHighlight ? new[] { ScenarioRequirementCapability.ProvidesPosition, ScenarioRequirementCapability.HighlightableWaypoint, ScenarioRequirementCapability.RegisteredEntity } : new[] { ScenarioRequirementCapability.ProvidesPosition, ScenarioRequirementCapability.RegisteredEntity });
          else if (component is Npc npc) AddProvider(providers, diagnostics, npc, ScenarioRequirementKind.Npc, npc.Identifier, entry, new[] { ScenarioRequirementCapability.ResolvableNpcMoveTarget, ScenarioRequirementCapability.RegisteredNpcComponent, ScenarioRequirementCapability.ProvidesPosition, ScenarioRequirementCapability.RegisteredEntity, ScenarioRequirementCapability.Interactable });
          else if (component is ItemSubmissionInteractable submission) AddProvider(providers, diagnostics, submission, ScenarioRequirementKind.Interactable, submission.Identifier, entry, new[] { ScenarioRequirementCapability.Interactable, ScenarioRequirementCapability.ToggleableInteractable, ScenarioRequirementCapability.ItemSubmissionTarget, ScenarioRequirementCapability.RegisteredEntity });
          else if (component is ScenarioInteractable interactable) AddProvider(providers, diagnostics, interactable, ScenarioRequirementKind.Interactable, interactable.Identifier, entry, new[] { ScenarioRequirementCapability.Interactable, ScenarioRequirementCapability.RegisteredEntity });
          else if (component is MonoBehaviour behaviour && behaviour is IPlayerSpawnPointProvider spawn) AddProvider(providers, diagnostics, behaviour, ScenarioRequirementKind.SpawnPoint, spawn.Identifier, entry, Array.Empty<ScenarioRequirementCapability>(), spawn.IsAvailable);
        }
      }
      if (bindings.Count > 1) diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR308", "MultipleSceneBindingComponents", ScenarioRequirementDiagnosticSeverity.Warning, $"Scene '{entry.ScenePath}' contains {bindings.Count} binding components.", scenePath: entry.ScenePath));
      foreach (var binding in bindings)
      {
        if (!string.IsNullOrWhiteSpace(binding.CompositionIdentifier)
            && !string.Equals(binding.CompositionIdentifier, composition.Identifier, StringComparison.Ordinal))
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR306", "BindingCompositionMismatch", ScenarioRequirementDiagnosticSeverity.Warning, $"Binding belongs to composition '{binding.CompositionIdentifier}', not '{composition.Identifier}'.", scenePath: entry.ScenePath));
          continue;
        }
        AddBindingProviders(binding, entry, manifest, providers, diagnostics);
      }
    }

    private static void AddBindingProviders(
      ScenarioRequirementsSceneBinding owner,
      ScenarioRequirementCompositionScene scene,
      ScenarioRequirementManifest manifest,
      List<ScenarioRequirementProvider> providers,
      List<ScenarioRequirementValidationDiagnostic> diagnostics)
    {
      foreach (var binding in owner.Bindings ?? Array.Empty<ScenarioRequirementObjectBinding>())
      {
        if (binding == null || !binding.TryGetKey(out var key))
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR301", "MalformedSceneBindingKey", ScenarioRequirementDiagnosticSeverity.Error, "Scene binding has an invalid requirement key.", scenePath: owner.gameObject.scene.path));
          continue;
        }
        if (binding.Target == null)
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR302", "MissingSceneBindingTarget", ScenarioRequirementDiagnosticSeverity.Error, $"Binding '{key}' has no target.", key, owner.gameObject.scene.path));
          continue;
        }
        if (EditorUtility.IsPersistent(binding.Target))
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR304", "PersistentAssetBindingTarget", ScenarioRequirementDiagnosticSeverity.Error, $"Binding '{key}' targets an asset instead of a scene object.", key, owner.gameObject.scene.path));
          continue;
        }
        var targetObject = binding.Target as GameObject ?? (binding.Target as Component)?.gameObject;
        if (targetObject == null)
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR302", "MissingSceneBindingTarget", ScenarioRequirementDiagnosticSeverity.Error, $"Binding '{key}' target is not a GameObject or Component.", key, owner.gameObject.scene.path));
          continue;
        }
        if (targetObject.scene != owner.gameObject.scene)
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR303", "CrossSceneBindingTarget", ScenarioRequirementDiagnosticSeverity.Error, $"Binding '{key}' points to another scene.", key, owner.gameObject.scene.path));
          continue;
        }
        if (!manifest.Requirements.Any(value => value.Key.Equals(key)))
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR305", "OrphanSceneBinding", ScenarioRequirementDiagnosticSeverity.Warning, $"Binding '{key}' is not present in the manifest.", key, owner.gameObject.scene.path));
          continue;
        }

        var targetComponent = binding.Target as Component;
        if (targetComponent == null)
        {
          targetComponent = targetObject.GetComponent<WaypointAnchor>()
            ?? (Component)targetObject.GetComponent<Npc>()
            ?? (Component)targetObject.GetComponent<ItemSubmissionInteractable>();
          if (targetComponent == null)
            targetComponent = targetObject.GetComponent<ScenarioInteractable>();
        }
        if (targetComponent == null)
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR307", "UnsupportedBindingTargetType", ScenarioRequirementDiagnosticSeverity.Error, $"Binding '{key}' target does not expose a supported scenario provider component.", key, owner.gameObject.scene.path));
          continue;
        }

        var capabilities = InferBindingCapabilities(targetComponent, key);
        if (!capabilities.Any() && manifest.Requirements.Any(value => value.Key.Equals(key) && value.Capabilities.Count > 0))
        {
          diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR403", "MissingProviderCapability", ScenarioRequirementDiagnosticSeverity.Error, $"Binding '{key}' target does not provide the required scenario capabilities.", key, owner.gameObject.scene.path));
        }
        // Physical provider identity must be computed identically to the
        // scene-component scan path so that the same physical object reached via
        // a scene component and via a binding coalesces to one provider during
        // cardinality/duplicate detection (proposal §2, blueprint §7).
        var identity = ComputePhysicalIdentity(targetComponent, scene.ScenePath);
        var behaviour = targetComponent as Behaviour;
        providers.Add(new ScenarioRequirementProvider(
          identity,
          key,
          scene.ScenePath,
          scene.Role,
          GetHierarchyPath(targetComponent.transform),
          capabilities,
          targetObject.activeInHierarchy,
          behaviour == null || behaviour.enabled,
          targetObject.activeInHierarchy && (behaviour == null || behaviour.enabled),
          ScenarioRequirementProviderOrigin.SceneBinding));
      }
    }

    private static IEnumerable<ScenarioRequirementCapability> InferBindingCapabilities(Component component, ScenarioRequirementKey key)
    {
      if (component is WaypointAnchor waypoint)
      {
        var capabilities = new List<ScenarioRequirementCapability>
        {
          ScenarioRequirementCapability.ProvidesPosition,
          ScenarioRequirementCapability.RegisteredEntity
        };
        if (waypoint.SupportsHighlight) capabilities.Add(ScenarioRequirementCapability.HighlightableWaypoint);
        return capabilities;
      }
      if (component is Npc)
        return new[] { ScenarioRequirementCapability.ResolvableNpcMoveTarget, ScenarioRequirementCapability.RegisteredNpcComponent, ScenarioRequirementCapability.ProvidesPosition, ScenarioRequirementCapability.RegisteredEntity };
      if (component is ItemSubmissionInteractable)
        return new[] { ScenarioRequirementCapability.Interactable, ScenarioRequirementCapability.ToggleableInteractable, ScenarioRequirementCapability.ItemSubmissionTarget, ScenarioRequirementCapability.RegisteredEntity };
      if (component is ScenarioInteractable)
        return new[] { ScenarioRequirementCapability.Interactable, ScenarioRequirementCapability.RegisteredEntity };
      return Array.Empty<ScenarioRequirementCapability>();
    }

    private static void AddProvider(List<ScenarioRequirementProvider> providers, List<ScenarioRequirementValidationDiagnostic> diagnostics, Component component, ScenarioRequirementKind kind, string identifier, ScenarioRequirementCompositionScene scene, IEnumerable<ScenarioRequirementCapability> capabilities, bool? expectedToRegister = null)
    {
      if (string.IsNullOrWhiteSpace(identifier)) { diagnostics.Add(new ScenarioRequirementValidationDiagnostic("SIR301", "MalformedProviderIdentifier", ScenarioRequirementDiagnosticSeverity.Error, $"Provider '{GetHierarchyPath(component.transform)}' has no identifier.", scenePath: scene.ScenePath)); return; }
      var key = new ScenarioRequirementKey(kind, identifier.Trim());
      var behaviour = component as Behaviour;
      var enabled = behaviour == null || behaviour.enabled;
      var identity = ComputePhysicalIdentity(component, scene.ScenePath);
      providers.Add(new ScenarioRequirementProvider(identity, key, scene.ScenePath, scene.Role, GetHierarchyPath(component.transform), capabilities, component.gameObject.activeInHierarchy, enabled, expectedToRegister ?? (component.gameObject.activeInHierarchy && enabled), ScenarioRequirementProviderOrigin.SceneComponent));
    }

    /// <summary>
    /// Computes a stable physical-provider identity for a scene component.  The
    /// same physical object reached through different scan paths (scene
    /// component vs scene binding) must yield the same identity so that
    /// duplicate detection does not double-count one object.  The fallback path
    /// keys on the owning GameObject's instance id so that multiple provider
    /// components on the same object still coalesce to a single physical
    /// provider.
    /// </summary>
    private static string ComputePhysicalIdentity(Component component, string scenePath)
    {
      var identity = GlobalObjectId.GetGlobalObjectIdSlow(component).ToString();
      if (!string.IsNullOrWhiteSpace(identity) && !identity.EndsWith("-0-0", StringComparison.Ordinal))
        return identity;
      return scenePath + ":" + component.gameObject.GetInstanceID();
    }

    private static string GetHierarchyPath(Transform transform)
    {
      var names = new Stack<string>();
      for (var cursor = transform; cursor != null; cursor = cursor.parent) names.Push(cursor.name);
      return string.Join("/", names);
    }
  }
}
#endif
