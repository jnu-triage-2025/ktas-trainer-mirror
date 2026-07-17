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
  public sealed class ScenarioRequirementsApplyReport
  {
    public bool Succeeded { get; }
    public int AppliedOperationCount { get; }
    public IReadOnlyList<string> ChangedScenes { get; }
    public string Error { get; }
    internal ScenarioRequirementsApplyReport(bool succeeded, int appliedOperationCount, IEnumerable<string> changedScenes, string error)
    { Succeeded = succeeded; AppliedOperationCount = appliedOperationCount; ChangedScenes = (changedScenes ?? Array.Empty<string>()).ToArray(); Error = error ?? string.Empty; }
  }

  public static class ScenarioRequirementsApplyService
  {
    public static ScenarioRequirementsApplyReport Apply(ScenarioWorldObjectCreationPlanSet planSet, ScenarioRequirementSceneComposition composition, IReadOnlyDictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> configurations)
    {
      if (planSet == null) throw new ArgumentNullException(nameof(planSet));
      if (composition == null) throw new ArgumentNullException(nameof(composition));
      if (!planSet.CanApply) return new ScenarioRequirementsApplyReport(false, 0, null, "Apply is blocked by the creation plan.");
      if (PrefabStageUtility.GetCurrentPrefabStage() != null) return new ScenarioRequirementsApplyReport(false, 0, null, "Apply is blocked in Prefab Stage.");

      var changedScenes = new HashSet<string>(StringComparer.Ordinal);
      Undo.IncrementCurrentGroup();
      var undoGroup = Undo.GetCurrentGroup();
      Undo.SetCurrentGroupName("Apply Scenario Requirement Objects");
      var applied = 0;
      try
      {
        foreach (var plan in planSet.Plans)
        {
          if (plan.Operation == ScenarioWorldObjectOperationKind.NoChange) continue;
          if (plan.Operation == ScenarioWorldObjectOperationKind.MarkOrphan)
          {
            if (plan.ExistingMarker == null) throw new InvalidOperationException("Orphan marker no longer exists: " + plan.RequirementKey);
            Undo.RecordObject(plan.ExistingMarker, "Mark Orphaned Scenario Object");
            plan.ExistingMarker.MarkOrphan();
            EditorSceneManager.MarkSceneDirty(plan.ExistingMarker.gameObject.scene);
            changedScenes.Add(plan.ExistingMarker.gameObject.scene.path);
            applied++;
            continue;
          }
          if (plan.Operation == ScenarioWorldObjectOperationKind.DeleteGenerated)
          {
            if (plan.ExistingMarker == null || plan.ExistingMarker.IsOrphan == false)
              throw new InvalidOperationException("Only an orphaned generated object may be deleted: " + plan.RequirementKey);
            // Defense in depth: never destroy an object that carries manual
            // child objects, even if a plan asked to (proposal §12).
            if (HasManualChildren(plan.ExistingMarker))
              throw new InvalidOperationException("Generated object has manual child objects and cannot be auto-deleted: " + plan.RequirementKey);
            var deletedScene = plan.ExistingMarker.gameObject.scene;
            Undo.DestroyObjectImmediate(plan.ExistingMarker.gameObject);
            EditorSceneManager.MarkSceneDirty(deletedScene);
            changedScenes.Add(deletedScene.path);
            applied++;
            continue;
          }
          if (!ScenarioWorldObjectFactoryRegistry.TryGet(plan.FactoryIdentifier, out var factory)) throw new InvalidOperationException("Factory disappeared before Apply: " + plan.FactoryIdentifier);
          var scene = SceneManager.GetSceneByPath(plan.TargetScenePath);
          if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Target scene is not loaded: " + plan.TargetScenePath);
          if (!configurations.TryGetValue(plan.RequirementKey, out var configuration)) throw new InvalidOperationException("Generation configuration disappeared: " + plan.RequirementKey);
          var context = new ScenarioWorldObjectFactoryContext(scene, composition, configuration);
          if (!context.IsMainStage) throw new InvalidOperationException("Apply requires Main Stage.");
          var existing = plan.ExistingMarker != null ? plan.ExistingMarker.gameObject : null;
          factory.Apply(plan, context, existing);
          EditorSceneManager.MarkSceneDirty(scene);
          changedScenes.Add(scene.path);
          applied++;
        }
        Undo.CollapseUndoOperations(undoGroup);
        return new ScenarioRequirementsApplyReport(true, applied, changedScenes, string.Empty);
      }
      catch (Exception ex)
      {
        // Apply is transactional from the caller's perspective.  Revert every
        // operation in this dedicated group before reporting failure.
        Undo.RevertAllDownToGroup(undoGroup);
        return new ScenarioRequirementsApplyReport(false, applied, changedScenes, ex.Message);
      }
    }

    private static bool HasManualChildren(ScenarioGeneratedWorldObject marker)
    {
      if (marker == null) return false;
      var root = marker.transform;
      for (var index = 0; index < root.childCount; index++)
        if (root.GetChild(index).GetComponent<ScenarioGeneratedWorldObject>() == null) return true;
      return false;
    }
  }
}
#endif
