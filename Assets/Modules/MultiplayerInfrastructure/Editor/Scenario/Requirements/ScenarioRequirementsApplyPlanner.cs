#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  public static class ScenarioRequirementsApplyPlanner
  {
    public static ScenarioWorldObjectCreationPlanSet CreatePlan(
      ScenarioRequirementManifest manifest,
      ScenarioRequirementSceneComposition composition,
      IReadOnlyDictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> configurations)
      => CreatePlan(manifest, composition, configurations, null);

    public static ScenarioWorldObjectCreationPlanSet CreatePlan(
      ScenarioRequirementManifest manifest,
      ScenarioRequirementSceneComposition composition,
      IReadOnlyDictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> configurations,
      ISet<string> approvedDeletionMarkerIdentities)
      => CreatePlanInternal(manifest.ScenarioIdentifier, manifest.GraphFingerprint, manifest.Requirements, composition, configurations, approvedDeletionMarkerIdentities);

    public static ScenarioWorldObjectCreationPlanSet CreatePlan(
      string scenarioIdentifier,
      IEnumerable<ScenarioRequirementDescriptor> requirements,
      ScenarioRequirementSceneComposition composition,
      IReadOnlyDictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> configurations)
      => CreatePlanInternal(scenarioIdentifier, string.Empty, requirements, composition, configurations, null);

    private static ScenarioWorldObjectCreationPlanSet CreatePlanInternal(
      string scenarioIdentifier,
      string manifestFingerprint,
      IEnumerable<ScenarioRequirementDescriptor> requirements,
      ScenarioRequirementSceneComposition composition,
      IReadOnlyDictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> configurations,
      ISet<string> approvedDeletionMarkerIdentities)
    {
      var plans = new List<ScenarioWorldObjectCreationPlan>();
      foreach (var requirement in (requirements ?? Array.Empty<ScenarioRequirementDescriptor>()).OrderBy(value => value.Key))
      {
        if (requirement.BindingHint == null
            || (requirement.BindingHint.Mode != ScenarioRequirementBindingMode.GeneratedSceneObject
                && requirement.BindingHint.Mode != ScenarioRequirementBindingMode.PrefabInstance)) continue;
        if (!configurations.TryGetValue(requirement.Key, out var configuration))
        {
          plans.Add(Blocked(requirement.Key, "Generation configuration is missing."));
          continue;
        }
        if (!ScenarioWorldObjectFactoryRegistry.TryGet(requirement.BindingHint.FactoryIdentifier, out var factory))
        {
          plans.Add(Blocked(requirement.Key, "Factory is not registered: " + requirement.BindingHint.FactoryIdentifier));
          continue;
        }
        if (!factory.Supports(requirement, configuration, out var reason))
        {
          plans.Add(Blocked(requirement.Key, reason));
          continue;
        }
        var scene = SceneManager.GetSceneByPath(configuration.TargetScenePath);
        if (!scene.IsValid() || !scene.isLoaded)
        {
          plans.Add(Blocked(requirement.Key, "Target scene is not loaded: " + configuration.TargetScenePath));
          continue;
        }
        if (!composition.Scenes.Any(value => string.Equals(value.ScenePath, configuration.TargetScenePath, StringComparison.Ordinal)))
        {
          plans.Add(Blocked(requirement.Key, "Target scene is not part of the selected composition."));
          continue;
        }
        if (PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
          plans.Add(Blocked(requirement.Key, "Apply is blocked in Prefab Stage."));
          continue;
        }
        var targetSceneGuid = string.IsNullOrWhiteSpace(configuration.TargetSceneGuid)
          ? AssetDatabase.AssetPathToGUID(configuration.TargetScenePath)
          : configuration.TargetSceneGuid;
        var matchingMarkers = FindMarkers(scene, composition.Identifier, requirement.Key, targetSceneGuid);
        if (matchingMarkers.Count > 1)
        {
          plans.Add(new ScenarioWorldObjectCreationPlan(
            "blocked-duplicate|" + requirement.Key,
            scenarioIdentifier,
            requirement.Key,
            ScenarioWorldObjectOperationKind.Blocked,
            configuration.TargetScenePath,
            configuration.TargetSceneGuid,
            FindRole(composition, configuration.TargetScenePath),
            factory.Identifier,
            factory.Version,
            string.Empty,
            "Multiple generated objects own the same requirement key.",
            ScenarioWorldObjectPlanRisk.Blocked,
            Array.Empty<ScenarioWorldObjectFieldChange>(),
            null));
          continue;
        }
        var marker = matchingMarkers.SingleOrDefault();
        // When the requirement returns after having been orphaned, re-adopt the
        // existing orphaned object (Configure clears the orphan flag) instead of
        // creating a second live object.  This keeps re-apply idempotent and
        // avoids duplicate generated objects (proposal §12, acceptance
        // criterion 8).
        if (marker == null)
          marker = FindAdoptableOrphan(scene, scenarioIdentifier, composition.Identifier, requirement.Key, targetSceneGuid);
        var effectiveConfiguration = new ScenarioRequirementGenerationConfiguration(
          configuration.Position,
          configuration.RotationEuler,
          configuration.Scale,
          configuration.TargetScenePath,
          targetSceneGuid);
        var identity = BuildPlanIdentity(scenarioIdentifier, composition, requirement, effectiveConfiguration, factory);
        var operation = marker == null ? ScenarioWorldObjectOperationKind.Create : ScenarioWorldObjectOperationKind.Configure;
        var planReason = marker == null
          ? "No owned generated object exists."
          : marker.IsOrphan ? "Orphaned generated object will be re-adopted and configured." : "Owned generated object will be configured.";
        plans.Add(new ScenarioWorldObjectCreationPlan(identity, scenarioIdentifier, requirement.Key, operation, effectiveConfiguration.TargetScenePath, effectiveConfiguration.TargetSceneGuid, FindRole(composition, effectiveConfiguration.TargetScenePath), factory.Identifier, factory.Version, marker?.Identity, planReason, ScenarioWorldObjectPlanRisk.None, Array.Empty<ScenarioWorldObjectFieldChange>(), marker, manifestFingerprint));
      }
      foreach (var sceneEntry in composition.Scenes)
      {
        var scene = SceneManager.GetSceneByPath(sceneEntry.ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) continue;
        foreach (var marker in UnityEngine.Object.FindObjectsByType<ScenarioGeneratedWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
          if (marker.gameObject.scene != scene || marker.CompositionIdentifier != composition.Identifier || !marker.TryGetKey(out var markerKey)) continue;
          // A generated object is owned by the scenario that created it.  Only
          // orphan markers produced by the scenario currently being applied;
          // another scenario sharing the same composition may still require this
          // key, and orphaning/deleting its object would violate scenario-level
          // ownership (proposal §12, blueprint §8).  Markers with no recorded
          // scenario identifier (legacy) fall back to composition-only handling.
          if (!string.IsNullOrEmpty(marker.ScenarioIdentifier)
              && !string.Equals(marker.ScenarioIdentifier, scenarioIdentifier, StringComparison.Ordinal)) continue;
          if ((requirements ?? Array.Empty<ScenarioRequirementDescriptor>()).Any(value => value.Key.Equals(markerKey))) continue;
          var deleteApproved = approvedDeletionMarkerIdentities != null && approvedDeletionMarkerIdentities.Contains(marker.Identity);
          var hasManualChildren = HasManualChildren(marker);
          // Deletion is a two-step contract that matches the Apply service guard
          // (a generated object must be orphaned before it can be deleted) and
          // never destroys an object that has manual/user-added children
          // (proposal §12).  An approved deletion of a not-yet-orphaned object
          // first marks it orphan; the next Apply performs the deletion.
          ScenarioWorldObjectOperationKind operation;
          string reason;
          ScenarioWorldObjectPlanRisk risk;
          if (deleteApproved && marker.IsOrphan && !hasManualChildren)
          {
            operation = ScenarioWorldObjectOperationKind.DeleteGenerated;
            reason = "Explicitly approved deletion of generated orphan.";
            risk = ScenarioWorldObjectPlanRisk.Destructive;
          }
          else if (deleteApproved && marker.IsOrphan && hasManualChildren)
          {
            operation = ScenarioWorldObjectOperationKind.NoChange;
            reason = "Deletion skipped: orphan has manual child objects and is not auto-deleted.";
            risk = ScenarioWorldObjectPlanRisk.Warning;
          }
          else if (marker.IsOrphan)
          {
            operation = ScenarioWorldObjectOperationKind.NoChange;
            reason = "Generated object is orphaned; explicit deletion approval is required.";
            risk = ScenarioWorldObjectPlanRisk.None;
          }
          else
          {
            operation = ScenarioWorldObjectOperationKind.MarkOrphan;
            reason = "Generated object is no longer required by the current manifest.";
            risk = ScenarioWorldObjectPlanRisk.Warning;
          }
          plans.Add(new ScenarioWorldObjectCreationPlan(
            "orphan|" + marker.Identity,
            marker.ScenarioIdentifier,
            markerKey,
            operation,
            sceneEntry.ScenePath,
            AssetDatabase.AssetPathToGUID(sceneEntry.ScenePath),
            sceneEntry.Role,
            marker.FactoryIdentifier,
            marker.FactoryVersion,
            marker.Identity,
            reason,
            risk,
            Array.Empty<ScenarioWorldObjectFieldChange>(),
            marker));
        }
      }
      return new ScenarioWorldObjectCreationPlanSet(plans);
    }

    private static List<ScenarioGeneratedWorldObject> FindMarkers(Scene scene, string compositionIdentifier, ScenarioRequirementKey key, string sceneGuid)
    {
      var result = new List<ScenarioGeneratedWorldObject>();
      foreach (var marker in UnityEngine.Object.FindObjectsByType<ScenarioGeneratedWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        // Orphaned markers are handled exclusively by the orphan pass and must
        // not participate in live duplicate-block detection.  Otherwise a stale
        // orphan sharing a key could spuriously block a legitimate re-apply, or
        // a single orphan could be silently resurrected via Configure.
        if (!marker.IsOrphan
            && marker.gameObject.scene == scene
            && marker.CompositionIdentifier == compositionIdentifier
            && (string.IsNullOrWhiteSpace(marker.TargetSceneGuid) || marker.TargetSceneGuid == sceneGuid)
            && marker.TryGetKey(out var markerKey) && markerKey.Equals(key)) result.Add(marker);
      return result;
    }

    // A generated object is considered to have manual children when its
    // hierarchy contains any child GameObject that the generator does not own
    // (i.e. a child without its own generated marker).  Such objects must not
    // be auto-deleted to avoid authoring loss (proposal §12).
    private static bool HasManualChildren(ScenarioGeneratedWorldObject marker)
    {
      if (marker == null) return false;
      var root = marker.transform;
      for (var index = 0; index < root.childCount; index++)
      {
        var child = root.GetChild(index);
        if (child.GetComponent<ScenarioGeneratedWorldObject>() == null) return true;
      }
      return false;
    }

    // Finds a single orphaned generated object that can be re-adopted for a
    // returning requirement key.  Only a unique orphan is adoptable; if several
    // orphans share the key the caller keeps them out of live handling so the
    // ambiguity is resolved explicitly by the user.  Re-adoption is scoped to
    // the applying scenario (or legacy ownerless markers) so applying scenario
    // A never steals scenario B's orphaned object, consistent with the
    // scenario-scoped orphan pass (proposal §12, blueprint §8).
    private static ScenarioGeneratedWorldObject FindAdoptableOrphan(Scene scene, string scenarioIdentifier, string compositionIdentifier, ScenarioRequirementKey key, string sceneGuid)
    {
      ScenarioGeneratedWorldObject found = null;
      foreach (var marker in UnityEngine.Object.FindObjectsByType<ScenarioGeneratedWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
      {
        if (!marker.IsOrphan
            || marker.gameObject.scene != scene
            || marker.CompositionIdentifier != compositionIdentifier
            || (!string.IsNullOrEmpty(marker.ScenarioIdentifier) && !string.Equals(marker.ScenarioIdentifier, scenarioIdentifier, StringComparison.Ordinal))
            || !(string.IsNullOrWhiteSpace(marker.TargetSceneGuid) || marker.TargetSceneGuid == sceneGuid)
            || !marker.TryGetKey(out var markerKey) || !markerKey.Equals(key)) continue;
        if (found != null) return null;
        found = marker;
      }
      return found;
    }

    private static ScenarioRequirementScope FindRole(ScenarioRequirementSceneComposition composition, string path)
      => composition.Scenes.FirstOrDefault(value => value.ScenePath == path)?.Role ?? ScenarioRequirementScope.AnyLoadedScene;
    private static string BuildPlanIdentity(string scenarioIdentifier, ScenarioRequirementSceneComposition composition, ScenarioRequirementDescriptor requirement, ScenarioRequirementGenerationConfiguration configuration, IScenarioWorldObjectFactory factory)
      => string.Join("|", scenarioIdentifier, composition.Identifier, configuration.TargetSceneGuid, requirement.Key, factory.Identifier, factory.Version, configuration.Position?.ToString() ?? string.Empty, configuration.RotationEuler?.ToString() ?? string.Empty, configuration.Scale?.ToString() ?? string.Empty);
    private static ScenarioWorldObjectCreationPlan Blocked(ScenarioRequirementKey key, string reason)
      => new ScenarioWorldObjectCreationPlan("blocked|" + key, string.Empty, key, ScenarioWorldObjectOperationKind.Blocked, string.Empty, string.Empty, ScenarioRequirementScope.AnyLoadedScene, string.Empty, 0, string.Empty, reason, ScenarioWorldObjectPlanRisk.Blocked, Array.Empty<ScenarioWorldObjectFieldChange>(), null);
  }
}
#endif
