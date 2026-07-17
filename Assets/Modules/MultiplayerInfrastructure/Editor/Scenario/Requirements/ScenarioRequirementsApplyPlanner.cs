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
        var effectiveConfiguration = new ScenarioRequirementGenerationConfiguration(
          configuration.Position,
          configuration.RotationEuler,
          configuration.Scale,
          configuration.TargetScenePath,
          targetSceneGuid);
        var identity = BuildPlanIdentity(scenarioIdentifier, composition, requirement, effectiveConfiguration, factory);
        var operation = marker == null ? ScenarioWorldObjectOperationKind.Create : ScenarioWorldObjectOperationKind.Configure;
        plans.Add(new ScenarioWorldObjectCreationPlan(identity, scenarioIdentifier, requirement.Key, operation, effectiveConfiguration.TargetScenePath, effectiveConfiguration.TargetSceneGuid, FindRole(composition, effectiveConfiguration.TargetScenePath), factory.Identifier, factory.Version, marker?.Identity, marker == null ? "No owned generated object exists." : "Owned generated object will be configured.", ScenarioWorldObjectPlanRisk.None, Array.Empty<ScenarioWorldObjectFieldChange>(), marker, manifestFingerprint));
      }
      foreach (var sceneEntry in composition.Scenes)
      {
        var scene = SceneManager.GetSceneByPath(sceneEntry.ScenePath);
        if (!scene.IsValid() || !scene.isLoaded) continue;
        foreach (var marker in UnityEngine.Object.FindObjectsByType<ScenarioGeneratedWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
          if (marker.gameObject.scene != scene || marker.CompositionIdentifier != composition.Identifier || !marker.TryGetKey(out var markerKey)) continue;
          if ((requirements ?? Array.Empty<ScenarioRequirementDescriptor>()).Any(value => value.Key.Equals(markerKey))) continue;
          var deleteApproved = approvedDeletionMarkerIdentities != null && approvedDeletionMarkerIdentities.Contains(marker.Identity);
          var operation = deleteApproved
            ? ScenarioWorldObjectOperationKind.DeleteGenerated
            : marker.IsOrphan ? ScenarioWorldObjectOperationKind.NoChange : ScenarioWorldObjectOperationKind.MarkOrphan;
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
            deleteApproved ? "Explicitly approved deletion of generated orphan." : marker.IsOrphan ? "Generated object is orphaned; explicit deletion approval is required." : "Generated object is no longer required by the current manifest.",
            deleteApproved ? ScenarioWorldObjectPlanRisk.Destructive : marker.IsOrphan ? ScenarioWorldObjectPlanRisk.None : ScenarioWorldObjectPlanRisk.Warning,
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

    private static ScenarioRequirementScope FindRole(ScenarioRequirementSceneComposition composition, string path)
      => composition.Scenes.FirstOrDefault(value => value.ScenePath == path)?.Role ?? ScenarioRequirementScope.AnyLoadedScene;
    private static string BuildPlanIdentity(string scenarioIdentifier, ScenarioRequirementSceneComposition composition, ScenarioRequirementDescriptor requirement, ScenarioRequirementGenerationConfiguration configuration, IScenarioWorldObjectFactory factory)
      => string.Join("|", scenarioIdentifier, composition.Identifier, configuration.TargetSceneGuid, requirement.Key, factory.Identifier, factory.Version, configuration.Position?.ToString() ?? string.Empty, configuration.RotationEuler?.ToString() ?? string.Empty, configuration.Scale?.ToString() ?? string.Empty);
    private static ScenarioWorldObjectCreationPlan Blocked(ScenarioRequirementKey key, string reason)
      => new ScenarioWorldObjectCreationPlan("blocked|" + key, string.Empty, key, ScenarioWorldObjectOperationKind.Blocked, string.Empty, string.Empty, ScenarioRequirementScope.AnyLoadedScene, string.Empty, 0, string.Empty, reason, ScenarioWorldObjectPlanRisk.Blocked, Array.Empty<ScenarioWorldObjectFieldChange>(), null);
  }
}
#endif
