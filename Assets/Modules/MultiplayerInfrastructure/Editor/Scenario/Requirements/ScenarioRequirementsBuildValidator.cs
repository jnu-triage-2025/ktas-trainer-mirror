#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  public sealed class ScenarioRequirementsBuildValidator : IPreprocessBuildWithReport
  {
    public int callbackOrder => 1000;

    public void OnPreprocessBuild(BuildReport report)
    {
      var buildReport = Validate(report);
      WriteJsonReport(report, buildReport);
      foreach (var diagnostic in buildReport.Diagnostics)
      {
        var line = $"[ScenarioRequirementsBuild] {diagnostic.Code} {diagnostic.Severity} {diagnostic.ScenarioIdentifier} {diagnostic.RequirementKey} {diagnostic.Message}";
        if (diagnostic.Severity >= ScenarioRequirementDiagnosticSeverity.Error) Debug.LogError(line);
        else if (diagnostic.Severity == ScenarioRequirementDiagnosticSeverity.Warning) Debug.LogWarning(line);
        else Debug.Log(line);
      }
      Debug.Log($"[ScenarioRequirementsBuild] scenarios={ScenarioRequirementsResourceScanner.Discover().Count} warnings={buildReport.WarningCount} errors={buildReport.ErrorCount} info={buildReport.InfoCount}");
      if (buildReport.HasBlockingErrors) throw new BuildFailedException(buildReport.ToStableSummary());
    }

    private static void WriteJsonReport(BuildReport build, ScenarioRequirementsBuildReport report)
    {
      try
      {
        var outputPath = build?.summary.outputPath;
        if (string.IsNullOrWhiteSpace(outputPath)) return;
        var directory = System.IO.Path.GetDirectoryName(outputPath);
        if (string.IsNullOrWhiteSpace(directory)) return;
        var reportPath = System.IO.Path.Combine(directory, "scenario-requirements-build-report.json");
        report.WriteUtf8Json(reportPath, build.summary.platform.ToString(), report.HasBlockingErrors ? "failed" : "passed");
        Debug.Log("[ScenarioRequirementsBuild] JSON report: " + reportPath);
      }
      catch (Exception ex)
      {
        Debug.LogWarning("[ScenarioRequirementsBuild] Failed to write JSON report: " + ex.Message);
      }
    }

    internal static ScenarioRequirementsBuildReport ValidateFromEditor()
    {
      var result = Validate(null);
      foreach (var diagnostic in result.Diagnostics)
      {
        if (diagnostic.Severity >= ScenarioRequirementDiagnosticSeverity.Error) Debug.LogError(diagnostic.Message);
        else if (diagnostic.Severity == ScenarioRequirementDiagnosticSeverity.Warning) Debug.LogWarning(diagnostic.Message);
        else Debug.Log(diagnostic.Message);
      }
      Debug.Log($"[ScenarioRequirementsBuild] scenarios={ScenarioRequirementsResourceScanner.Discover().Count} warnings={result.WarningCount} errors={result.ErrorCount} info={result.InfoCount}");
      return result;
    }

    internal static ScenarioRequirementsBuildReport Validate(BuildReport buildReport)
    {
      var result = new ScenarioRequirementsBuildReport();
      var profiles = AssetDatabase.FindAssets("t:ScenarioSceneCompositionProfile")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<ScenarioSceneCompositionProfile>)
        .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Identifier))
        .OrderBy(value => value.Identifier, StringComparer.Ordinal).ToArray();
      var buildScenes = EditorBuildSettings.scenes;
      var discovered = ScenarioRequirementsResourceScanner.Discover();
      var isBuild = buildReport != null;

      foreach (var pair in discovered)
      {
        try
        {
          var graph = ScenarioGraphLoader.LoadFromJson(System.Text.Encoding.UTF8.GetString(pair.SourceBytes));
          var selected = SelectProfiles(profiles, graph.Identifier, result, pair.ScenarioPath, out var ambiguous);
          if (ambiguous)
          {
            // Two or more profiles match: this is a single, reported composition
            // conflict (SIR503).  It must NOT degrade to the legacy 0-match
            // AnyLoadedScene inference path, which would both double-report and
            // silently relax validation (proposal blueprint §9).
            continue;
          }
          if (selected.Length == 0)
          {
            result.Add(graph.Identifier, pair.ScenarioPath, string.Empty, string.Empty, string.Empty, "SIR506", "CompositionProfileMissing", isBuild ? ScenarioRequirementDiagnosticSeverity.Error : ScenarioRequirementDiagnosticSeverity.Warning, isBuild ? "No composition profile selected for this scenario." : "No composition profile selected; authoring validation used legacy AnyLoadedScene inference.");
            var inferred = ScenarioRequirementCompiler.CompileInferred(graph);
            AddCompilerDiagnostics(result, inferred, graph.Identifier, pair.ScenarioPath, string.Empty);
            continue;
          }
          foreach (var profile in selected) ValidateScenarioProfile(pair, graph, profile, buildScenes, result);
        }
        catch (Exception ex)
        {
          result.Add(string.Empty, pair.ScenarioPath, string.Empty, string.Empty, string.Empty, "SIR505", "ScenarioAssetDiscoveryFailure", ScenarioRequirementDiagnosticSeverity.Error, ex.Message);
        }
      }
      result.HasBlockingErrors = result.ErrorCount > 0;
      return result;
    }

    private static ScenarioSceneCompositionProfile[] SelectProfiles(ScenarioSceneCompositionProfile[] profiles, string identifier, ScenarioRequirementsBuildReport report, string assetPath, out bool ambiguous)
    {
      ambiguous = false;
      var exact = profiles.Where(value => value.ScenarioSelectors.Any(selector => selector != null && selector.IsExact && selector.Pattern == identifier)).ToArray();
      if (exact.Length > 1)
      {
        report.Add(identifier, assetPath, string.Empty, string.Empty, string.Empty, "SIR503", "DuplicateOrAmbiguousProfile", ScenarioRequirementDiagnosticSeverity.Error, "Multiple exact composition profiles match.");
        ambiguous = true;
        return Array.Empty<ScenarioSceneCompositionProfile>();
      }
      if (exact.Length == 1) return exact;
      var wildcard = profiles.Where(value => value.Matches(identifier)).ToArray();
      if (wildcard.Length > 1)
      {
        report.Add(identifier, assetPath, string.Empty, string.Empty, string.Empty, "SIR503", "DuplicateOrAmbiguousProfile", ScenarioRequirementDiagnosticSeverity.Error, "Multiple wildcard composition profiles match.");
        ambiguous = true;
        return Array.Empty<ScenarioSceneCompositionProfile>();
      }
      return wildcard.Length == 1 ? wildcard : Array.Empty<ScenarioSceneCompositionProfile>();
    }

    private static void ValidateScenarioProfile(ScenarioRequirementAssetPair pair, ScenarioGraph graph, ScenarioSceneCompositionProfile profile, EditorBuildSettingsScene[] buildScenes, ScenarioRequirementsBuildReport report)
    {
      // Authoring is Warning-centric and Apply-capable (proposal §14); its
      // structural composition diagnostics must not hard-block the build.
      var isAuthoring = profile.ValidationProfile.Mode == ScenarioRequirementsValidationProfileMode.Authoring;
      var structuralSeverity = isAuthoring ? ScenarioRequirementDiagnosticSeverity.Warning : ScenarioRequirementDiagnosticSeverity.Error;
      var sidecarAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(pair.SidecarPath);
      ScenarioRequirementsDocument sidecar = null;
      if (sidecarAsset != null)
      {
        var loaded = ScenarioRequirementsLoader.LoadSidecar(sidecarAsset.text);
        foreach (var diagnostic in loaded.Diagnostics) report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, diagnostic.HasRequirementKey ? diagnostic.RequirementKey.ToString() : string.Empty, diagnostic.Code, diagnostic.Name, diagnostic.Severity, diagnostic.Message);
        // A malformed sidecar is a reported Error, but validation must still
        // collect every other diagnostic for this scenario (proposal §14,
        // acceptance criterion 9).  Fall back to inferred-only requirements so
        // the remaining scene/composition/cardinality checks continue to run.
        sidecar = loaded.IsValid ? loaded.Document : null;
      }
      else if (profile.ValidationProfile.RequireProfileForBuild)
      {
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, string.Empty, "SIR506", "SidecarMissingUnderPolicy", structuralSeverity, "Requirements sidecar is required by the selected profile.");
      }
      var manifest = ScenarioRequirementCompiler.Compile(graph, pair.SourceBytes, sidecar, new ScenarioRequirementCompilationContext(DateTime.UtcNow));
      AddCompilerDiagnostics(report, manifest, graph.Identifier, pair.ScenarioPath, profile.Identifier);
      var compositionScenes = profile.Scenes
        .Select(scene => new ScenarioRequirementCompositionScene(scene.ScenePath, scene.SceneGuid, scene.Role))
        .ToArray();
      var composition = new ScenarioRequirementSceneComposition(profile.Identifier, compositionScenes);
      var snapshot = ScenarioRequirementsSceneScanner.Scan(manifest, composition);
      var sceneReport = ScenarioRequirementValidationEngine.Validate(manifest, snapshot, composition);
      // WrongScene/Inactive/NotRegistered are Warning by default in the engine
      // but the diagnostic severity table (proposal §13) escalates them to Error
      // in BOTH Development and Production (Production additionally hard-blocks).
      // Only Authoring keeps them Warning-centric.
      var elevateSceneErrors = !isAuthoring;
      var elevateIndeterminate = !isAuthoring;
      foreach (var diagnostic in sceneReport.Diagnostics)
      {
        var severity = diagnostic.Severity;
        if (isAuthoring
            && severity >= ScenarioRequirementDiagnosticSeverity.Error)
          severity = ScenarioRequirementDiagnosticSeverity.Warning;
        else if (elevateSceneErrors
                 && (diagnostic.Code == "SIR310" || diagnostic.Code == "SIR404" || diagnostic.Code == "SIR413"))
          severity = ScenarioRequirementDiagnosticSeverity.Error;
        // Indeterminate results are reported as Info by the engine.  In
        // Development/Production profiles they must surface as Warning (the
        // mustProve escalation to Error is handled separately below), matching
        // the diagnostic severity table (proposal §13).
        else if (elevateIndeterminate
                 && severity == ScenarioRequirementDiagnosticSeverity.Info
                 && string.Equals(diagnostic.Name, "Indeterminate", StringComparison.Ordinal))
          severity = ScenarioRequirementDiagnosticSeverity.Warning;
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, diagnostic.ScenePath,
          diagnostic.RequirementKey.HasValue ? diagnostic.RequirementKey.Value.ToString() : string.Empty,
          diagnostic.Code, diagnostic.Name, severity, diagnostic.Message);
      }
      foreach (var scene in profile.Scenes)
      {
        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.ScenePath);
        if (asset == null || AssetDatabase.AssetPathToGUID(scene.ScenePath) != scene.SceneGuid)
          report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, scene.ScenePath, string.Empty, "SIR502", "ScenePathGuidMismatch", structuralSeverity, "Profile scene path/GUID does not resolve to the same asset.");
        var matchingBuildScenes = buildScenes.Where(value => value.path == scene.ScenePath || value.guid.ToString() == scene.SceneGuid).ToArray();
        var buildScene = matchingBuildScenes.FirstOrDefault();
        if (!scene.Optional && (buildScene == null || !buildScene.enabled))
          report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, scene.ScenePath, string.Empty, "SIR501", "RequiredSceneAbsentFromBuildSettings", structuralSeverity, "Required profile scene is not enabled in Build Settings.");
        var buildSceneCount = matchingBuildScenes.Count(value => value.enabled);
        if (buildSceneCount < scene.MinimumLoadCount || buildSceneCount > scene.MaximumLoadCount)
          report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, scene.ScenePath, string.Empty, "SIR504", "SceneLoadCountMismatch", structuralSeverity, $"Expected scene load count {scene.MinimumLoadCount}-{scene.MaximumLoadCount}, found {buildSceneCount}.");
      }
      if (profile.ValidationProfile.FailOnIndeterminate && sceneReport.Results.Any(value => value.Status == ScenarioRequirementValidationStatus.Indeterminate))
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, string.Empty, "SIR507", "IndeterminateRequirement", ScenarioRequirementDiagnosticSeverity.Error, "Profile requires proof for indeterminate requirements.");
      foreach (var requirement in sceneReport.Results.Where(value => value.Status == ScenarioRequirementValidationStatus.Indeterminate && value.Requirement.MustProve))
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, requirement.Requirement.Key.ToString(), "SIR507", "MustProveIndeterminateRequirement", ScenarioRequirementDiagnosticSeverity.Error, "Requirement is marked mustProve but static evidence is indeterminate.");
    }

    private static void AddCompilerDiagnostics(ScenarioRequirementsBuildReport report, ScenarioRequirementManifest manifest, string scenarioIdentifier, string assetPath, string profileIdentifier)
    {
      foreach (var diagnostic in manifest.Diagnostics)
        report.Add(scenarioIdentifier, assetPath, profileIdentifier, string.Empty, diagnostic.HasRequirementKey ? diagnostic.RequirementKey.ToString() : string.Empty, diagnostic.Code, diagnostic.Name, diagnostic.Severity, diagnostic.Message);
    }
  }
}
#endif
