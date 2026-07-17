#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  internal static class ScenarioRequirementsPhase4Validation
  {
    internal static void Run()
    {
      const string path = "Assets/TempScenarioRequirementsPhase4.unity";
      try
      {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, path);
        var graph = new ScenarioGraph { Identifier = "phase4" };
        graph.Add(new ScenarioPlayerMoveNode { Identifier = "move", DestinationType = ScenarioMoveDestinationType.Waypoint, DestinationIdentifier = "room" });
        var key = new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "room");
        var descriptor = ScenarioRequirementCompiler.CompileInferred(graph).Requirements.Single(value => value.Key.Equals(key));
        descriptor = new ScenarioRequirementDescriptor(key, ScenarioRequirementScope.AnyLoadedScene, ScenarioRequirementAuthority.Any, descriptor.Cardinality, descriptor.EffectiveAvailability, descriptor.Capabilities, descriptor.Occurrences, new ScenarioRequirementBindingHint(ScenarioRequirementBindingMode.GeneratedSceneObject, "mi.waypoint-anchor", null), false, null, Array.Empty<ScenarioRequirementDeclarationSource>());
        var composition = new ScenarioRequirementSceneComposition("phase4", new[] { new ScenarioRequirementCompositionScene(path, AssetDatabase.AssetPathToGUID(path), ScenarioRequirementScope.AnyLoadedScene) });
        var configuration = new ScenarioRequirementGenerationConfiguration(new Vector3(1, 2, 3), null, null, path, AssetDatabase.AssetPathToGUID(path));
        var configurations = new Dictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration> { [key] = configuration };
        var plan = ScenarioRequirementsApplyPlanner.CreatePlan("phase4", new[] { descriptor }, composition, configurations);
        if (!plan.CanApply || plan.Plans.Single().Operation != ScenarioWorldObjectOperationKind.Create) throw new InvalidOperationException("Phase 4 planner did not create a valid plan.");
        var applied = ScenarioRequirementsApplyService.Apply(plan, composition, configurations);
        if (!applied.Succeeded) throw new InvalidOperationException(applied.Error);
        var generated = UnityEngine.Object.FindObjectsByType<ScenarioGeneratedWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).SingleOrDefault(value => value.gameObject.scene == scene && value.TryGetKey(out var markerKey) && markerKey.Equals(key));
        if (generated == null || generated.FactoryIdentifier != "mi.waypoint-anchor") throw new InvalidOperationException("Generated marker was not configured.");
        var replan = ScenarioRequirementsApplyPlanner.CreatePlan("phase4", new[] { descriptor }, composition, configurations);
        if (!replan.CanApply || replan.Plans.Single().Operation != ScenarioWorldObjectOperationKind.Configure) throw new InvalidOperationException("Replan was not idempotent.");
        Undo.PerformUndo();
        if (UnityEngine.Object.FindObjectsByType<ScenarioGeneratedWorldObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Any(value => value.gameObject.scene == scene && value.TryGetKey(out var afterUndoKey) && afterUndoKey.Equals(key))) throw new InvalidOperationException("One Undo did not revert the generated object.");
        Debug.Log("[ScenarioRequirementsPhase4Validation] Phase 4 validation passed.");
      }
      finally
      {
        var scene = SceneManager.GetSceneByPath(path);
        if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.DeleteAsset(path);
      }
    }
  }
}
#endif
