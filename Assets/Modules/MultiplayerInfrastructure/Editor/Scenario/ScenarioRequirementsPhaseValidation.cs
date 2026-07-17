#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Requirements.Editor;

namespace MultiplayerInfrastructure.Scenario.Editor
{
  /// <summary>Phase 0-4 acceptance smoke validation for local and CI batch execution.</summary>
  internal static class ScenarioRequirementsPhaseValidation
  {
    internal static void Run()
    {
      ValidateAllScenarioAssets();
      ValidateCanonicalAcceptanceCases();
      ValidateSidecarAndCandidateCases();
      ValidateSceneScannerCases();
      ScenarioRequirementsPhase4Validation.Run();
      Debug.Log("[ScenarioRequirementsPhaseValidation] Phase 0-4 validation passed.");
    }

    private static void ValidateAllScenarioAssets()
    {
      var paths = AssetDatabase.GetAllAssetPaths()
        .Where(path => path.EndsWith(".scenario.json", StringComparison.Ordinal))
        .OrderBy(path => path, StringComparer.Ordinal)
        .ToArray();

      foreach (var path in paths)
      {
        var graph = ScenarioGraphLoader.LoadFromJson(File.ReadAllText(path));
        var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
        if (manifest.Diagnostics.Any(diagnostic => diagnostic.Severity >= ScenarioRequirementDiagnosticSeverity.Error))
        {
          throw new InvalidOperationException(
            $"Scenario '{path}' failed requirement compilation: "
            + string.Join(" | ", manifest.Diagnostics
              .Where(diagnostic => diagnostic.Severity >= ScenarioRequirementDiagnosticSeverity.Error)
              .Select(diagnostic => diagnostic.Code + " " + diagnostic.Message)));
        }
      }
    }

    private static void ValidateCanonicalAcceptanceCases()
    {
      var graph = new ScenarioGraph { Identifier = "phase-acceptance" };
      graph.Add(new ScenarioNPCMoveNode
      {
        Identifier = "npc-move",
        NPCIdentifier = "npc-1",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "waypoint-a",
        NextIdentifier = "highlight"
      });
      graph.Add(new ScenarioQuestWaypointHighlightNode
      {
        Identifier = "highlight",
        WaypointIdentifier = "waypoint-a"
      });

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      var npc = manifest.Requirements.Single(requirement =>
        requirement.Kind == ScenarioRequirementKind.Npc && requirement.Identifier == "npc-1");
      var waypoint = manifest.Requirements.Single(requirement =>
        requirement.Kind == ScenarioRequirementKind.SpatialAnchor && requirement.Identifier == "waypoint-a");

      Require(npc.Capabilities.Contains(ScenarioRequirementCapability.ResolvableNpcMoveTarget),
        "NPCMove target capability was not extracted.");
      Require(waypoint.Capabilities.Contains(ScenarioRequirementCapability.ProvidesPosition),
        "Waypoint position capability was not extracted.");
      Require(waypoint.Capabilities.Contains(ScenarioRequirementCapability.HighlightableWaypoint),
        "Waypoint highlight capability was not merged.");
      Require(waypoint.Occurrences.Count == 2,
        "All waypoint source occurrences were not preserved.");

      var malformed = new ScenarioGraph { Identifier = "malformed" };
      malformed.Add(new ScenarioSoundNode { Identifier = "sound", SoundResourceIdentifier = " " });
      var malformedManifest = ScenarioRequirementCompiler.CompileInferred(malformed);
      Require(malformedManifest.Diagnostics.Any(diagnostic => diagnostic.Code == "SIR100"),
        "Whitespace identifier did not produce SIR100.");
    }

    private static void Require(bool condition, string message)
    {
      if (!condition) throw new InvalidOperationException(message);
    }

    private static void ValidateSidecarAndCandidateCases()
    {
      var graph = new ScenarioGraph { Identifier = "phase2" };
      graph.Add(new ScenarioNPCMoveNode
      {
        Identifier = "move",
        NPCIdentifier = "npc-1",
        DestinationType = ScenarioMoveDestinationType.Waypoint,
        DestinationIdentifier = "room"
      });
      var sourceBytes = Encoding.UTF8.GetBytes("phase2-source");
      var hash = ScenarioRequirementSourceHasher.ComputeSha256(sourceBytes);
      var sidecarJson = "{" +
        "\"format\":\"scenario-ingame-requirements\"," +
        "\"schemaVersion\":1," +
        "\"scenarioIdentifier\":\"phase2\"," +
        "\"source\":{\"scenarioSha256\":\"" + hash + "\"}," +
        "\"declarations\":[{" +
          "\"selector\":{\"kind\":\"SpatialAnchor\",\"identifier\":\"room\"}," +
          "\"operation\":\"Override\"," +
          "\"scope\":\"Overworld\"," +
          "\"mustProve\":true," +
          "\"capabilities\":[\"HighlightableWaypoint\"]," +
          "\"occurrenceSelector\":{\"nodeIdentifier\":\"move\",\"fieldPath\":\"destinationIdentifier\"}," +
          "\"binding\":{\"mode\":\"GeneratedSceneObject\",\"factoryIdentifier\":\"mi.waypoint-anchor\"}" +
        "}]," +
        "\"suppressions\":[{" +
          "\"selector\":{\"kind\":\"Npc\",\"identifier\":\"npc-1\"}," +
          "\"reason\":\"Supplied by test runtime provider\",\"owner\":\"test\",\"expiresOn\":\"2099-12-31\"}]}";

      var sidecar = ScenarioRequirementsLoader.LoadSidecar(sidecarJson);
      Require(sidecar.IsValid, "Valid requirements sidecar was rejected.");
      var manifest = ScenarioRequirementCompiler.Compile(graph, sourceBytes, sidecar.Document,
        new ScenarioRequirementCompilationContext(new DateTime(2026, 7, 16)));
      Require(manifest.IsValid, "Valid sidecar merge produced errors.");
      Require(manifest.ScenarioSha256 == hash, "Exact source SHA-256 was not preserved.");
      var room = manifest.Requirements.Single(value => value.Kind == ScenarioRequirementKind.SpatialAnchor);
      Require(room.Scope == ScenarioRequirementScope.Overworld && room.MustProve,
        "Sidecar scope or mustProve override was not applied.");
      Require(room.Capabilities.Contains(ScenarioRequirementCapability.HighlightableWaypoint),
        "Declared capability was not unioned.");
      Require(room.BindingHint?.FactoryIdentifier == "mi.waypoint-anchor",
        "Binding hint was not compiled.");
      Require(manifest.Requirements.Single(value => value.Kind == ScenarioRequirementKind.Npc).IsSuppressed,
        "Valid suppression was not retained on the descriptor.");

      var stale = ScenarioRequirementCompiler.Compile(graph, Encoding.UTF8.GetBytes("changed"), sidecar.Document,
        new ScenarioRequirementCompilationContext(new DateTime(2026, 7, 16)));
      Require(stale.Diagnostics.Any(value => value.Code == "SIR201"), "Stale source did not produce SIR201.");

      var duplicate = ScenarioRequirementsLoader.LoadSidecar(sidecarJson.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1"));
      Require(!duplicate.IsValid && duplicate.Diagnostics.Any(value => value.Code == "SIR107"),
        "Duplicate JSON property was not rejected.");

      var candidateJson = "{" +
        "\"format\":\"scenario-ingame-requirements-candidates\",\"schemaVersion\":1,\"scenarioIdentifier\":\"phase2\"," +
        "\"candidates\":[{\"kind\":\"SpatialAnchor\",\"identifier\":\"room\"," +
        "\"capabilities\":[\"HighlightableWaypoint\"]," +
        "\"evidence\":[{\"nodeIdentifier\":\"move\",\"nodeType\":\"NPCMove\",\"fieldPath\":\"destinationIdentifier\"}]," +
        "\"suggestedBinding\":{\"mode\":\"GeneratedSceneObject\",\"factoryIdentifier\":\"mi.waypoint-anchor\"}," +
        "\"confidence\":0.9,\"reviewRequired\":true}]}";
      var candidates = ScenarioRequirementsLoader.LoadCandidates(candidateJson);
      Require(candidates.IsValid, "Valid AI candidate document was rejected.");
      var preview = ScenarioRequirementsCandidatePreviewService.CreatePreview(
        graph, ScenarioRequirementCompiler.CompileInferred(graph), candidates.Document,
        new HashSet<string>(StringComparer.Ordinal) { "mi.waypoint-anchor" });
      Require(preview.IsValid && preview.Candidates.Single().Disposition == ScenarioRequirementCandidateDisposition.ProposesOverride,
        "AI candidate evidence preview did not propose a safe override.");
      var approved = ScenarioRequirementsCandidatePreviewService.CreateApprovedDocument(
        sidecar.Document, candidates.Document, ScenarioRequirementCompiler.CompileInferred(graph),
        new[] { new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "room") });
      var firstWrite = ScenarioRequirementsWriter.WriteUtf8(approved);
      var secondWrite = ScenarioRequirementsWriter.WriteUtf8(approved);
      Require(firstWrite.SequenceEqual(secondWrite), "Approved sidecar serialization is not byte deterministic.");
      Require(ScenarioRequirementsLoader.LoadSidecar(Encoding.UTF8.GetString(firstWrite)).IsValid,
        "Deterministically written sidecar does not satisfy its schema.");
    }

    private static void ValidateSceneScannerCases()
    {
      const string scenePath = "Assets/TempScenarioRequirementsPhase3.unity";
      var previousScene = SceneManager.GetActiveScene();
      try
      {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var firstObject = new GameObject("waypoint-one");
        var first = firstObject.AddComponent<WaypointAnchor>();
        SetSerialized(first, "identifier", "room");
        var secondObject = new GameObject("waypoint-two");
        var second = secondObject.AddComponent<WaypointAnchor>();
        SetSerialized(second, "identifier", "room");
        EditorSceneManager.MoveGameObjectToScene(firstObject, scene);
        EditorSceneManager.MoveGameObjectToScene(secondObject, scene);
        EditorSceneManager.SaveScene(scene, scenePath);

        var graph = new ScenarioGraph { Identifier = "phase3" };
        graph.Add(new ScenarioPlayerMoveNode { Identifier = "move", DestinationType = ScenarioMoveDestinationType.Waypoint, DestinationIdentifier = "room", NextIdentifier = "highlight" });
        graph.Add(new ScenarioQuestWaypointHighlightNode { Identifier = "highlight", WaypointIdentifier = "room" });
        var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
        var positionGraph = new ScenarioGraph { Identifier = "phase3-position" };
        positionGraph.Add(new ScenarioPlayerMoveNode { Identifier = "move", DestinationType = ScenarioMoveDestinationType.Waypoint, DestinationIdentifier = "room" });
        var positionManifest = ScenarioRequirementCompiler.CompileInferred(positionGraph);
        var composition = new ScenarioRequirementSceneComposition("phase3", new[]
        {
          new ScenarioRequirementCompositionScene(scenePath, AssetDatabase.AssetPathToGUID(scenePath), ScenarioRequirementScope.AnyLoadedScene)
        });
        var duplicateReport = ScenarioRequirementValidationEngine.Validate(manifest, ScenarioRequirementsSceneScanner.Scan(manifest, composition), composition);
        Require(duplicateReport.Results.Single(value => value.Requirement.Kind == ScenarioRequirementKind.SpatialAnchor).Status == ScenarioRequirementValidationStatus.MissingCapability,
          "Waypoint without highlight sprite should report MissingCapability before cardinality.");
        var positionDuplicateReport = ScenarioRequirementValidationEngine.Validate(positionManifest, ScenarioRequirementsSceneScanner.Scan(positionManifest, composition), composition);
        Require(positionDuplicateReport.Results.Single(value => value.Requirement.Kind == ScenarioRequirementKind.SpatialAnchor).Status == ScenarioRequirementValidationStatus.Duplicate,
          "Two physical waypoint providers should report Duplicate.");

        UnityEngine.Object.DestroyImmediate(secondObject);
        firstObject.SetActive(false);
        EditorSceneManager.SaveScene(scene, scenePath);
        var inactiveReport = ScenarioRequirementValidationEngine.Validate(manifest, ScenarioRequirementsSceneScanner.Scan(manifest, composition), composition);
        Require(inactiveReport.Results.Single(value => value.Requirement.Kind == ScenarioRequirementKind.SpatialAnchor).Status == ScenarioRequirementValidationStatus.MissingCapability,
          "Inactive waypoint without required capability should remain MissingCapability.");

        EditorSceneManager.CloseScene(scene, true);
        var closedSnapshot = ScenarioRequirementsSceneScanner.Scan(manifest, composition);
        Require(closedSnapshot.Providers.Count(value => value.Key.Kind == ScenarioRequirementKind.SpatialAnchor) == 1,
          "Closed scene additive scan did not preserve provider identity.");
      }
      finally
      {
        var scene = SceneManager.GetSceneByPath(scenePath);
        if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.DeleteAsset(scenePath);
        if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
      }
    }

    /*
    private static void ValidateGenerationCases()
    {
      const string scenePath = "Assets/TempScenarioRequirementsPhase4.unity";
      var sourceBytes = Encoding.UTF8.GetBytes("phase4-source");
      var sourceHash = ScenarioRequirementSourceHasher.ComputeSha256(sourceBytes);
      var previousScene = SceneManager.GetActiveScene();
      try
      {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);
        var graph = new ScenarioGraph { Identifier = "phase4" };
        graph.Add(new ScenarioPlayerMoveNode { Identifier = "move", DestinationType = ScenarioMoveDestinationType.Waypoint, DestinationIdentifier = "generated-room" });
        var sidecarJson = "{\"format\":\"scenario-ingame-requirements\",\"schemaVersion\":1,\"scenarioIdentifier\":\"phase4\",\"source\":{\"scenarioSha256\":\"" + sourceHash + "\"},\"declarations\":[{\"selector\":{\"kind\":\"SpatialAnchor\",\"identifier\":\"generated-room\"},\"operation\":\"Override\",\"binding\":{\"mode\":\"GeneratedSceneObject\",\"factoryIdentifier\":\"mi.waypoint-anchor\"}}],\"suppressions":[]}";
        var loadedSidecar = ScenarioRequirementsLoader.LoadSidecar(sidecarJson);
        Require(loadedSidecar.IsValid, "Phase 4 generation sidecar failed to load.");
        var manifest = ScenarioRequirementCompiler.Compile(graph, sourceBytes, loadedSidecar.Document, new ScenarioRequirementCompilationContext(new DateTime(2026, 7, 16)));
        var composition = new ScenarioRequirementSceneComposition("phase4-composition", new[] { new ScenarioRequirementCompositionScene(scenePath, AssetDatabase.AssetPathToGUID(scenePath), ScenarioRequirementScope.AnyLoadedScene) });
        var configuration = new ScenarioRequirementGenerationConfiguration(new Vector3(1f, 2f, 3f), null, null, scenePath, AssetDatabase.AssetPathToGUID(scenePath));
        var configurations = new Dictionary<ScenarioRequirementKey, ScenarioRequirementGenerationConfiguration>
        {
          [new ScenarioRequirementKey(ScenarioRequirementKind.SpatialAnchor, "generated-room")] = configuration
        };
        var plan = ScenarioRequirementsApplyPlanner.CreatePlan(manifest, composition, configurations);
        Require(plan.CanApply && plan.Plans.Single().Operation == ScenarioWorldObjectOperationKind.Create, "Phase 4 planner did not produce Create.");
        var apply = ScenarioRequirementsApplyService.Apply(plan, composition, configurations);
        Require(apply.Succeeded && apply.AppliedOperationCount == 1, "Phase 4 Apply failed.");
        var snapshot = ScenarioRequirementsSceneScanner.Scan(manifest, composition);
        Require(snapshot.Providers.Any(value => value.Key.Identifier == "generated-room"), "Generated waypoint was not visible to scanner.");

        var secondPlan = ScenarioRequirementsApplyPlanner.CreatePlan(manifest, composition, configurations);
        Require(secondPlan.CanApply && secondPlan.Plans.Single().Operation == ScenarioWorldObjectOperationKind.Configure, "Reapplying did not produce Configure.");
        Undo.PerformUndo();
        var afterUndo = ScenarioRequirementsSceneScanner.Scan(manifest, composition);
        Require(!afterUndo.Providers.Any(value => value.Key.Identifier == "generated-room"), "One Undo did not remove generated object.");
        EditorSceneManager.CloseScene(scene, true);
      }
      finally
      {
        var scene = SceneManager.GetSceneByPath(scenePath);
        if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true);
        AssetDatabase.DeleteAsset(scenePath);
        if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
      }
    }
    */

    private static void SetSerialized(UnityEngine.Object target, string propertyName, string value)
    {
      var serialized = new SerializedObject(target);
      serialized.FindProperty(propertyName).stringValue = value;
      serialized.ApplyModifiedPropertiesWithoutUndo();
    }
  }
}
#endif
