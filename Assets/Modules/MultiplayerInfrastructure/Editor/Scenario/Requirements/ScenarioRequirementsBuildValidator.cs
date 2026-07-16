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

    [MenuItem("Tools/Multiplayer Infrastructure/Validate Scenario Requirements Build")]
    public static void ValidateFromEditor()
    {
      var result = Validate(null);
      foreach (var diagnostic in result.Diagnostics)
      {
        if (diagnostic.Severity >= ScenarioRequirementDiagnosticSeverity.Error) Debug.LogError(diagnostic.Message);
        else if (diagnostic.Severity == ScenarioRequirementDiagnosticSeverity.Warning) Debug.LogWarning(diagnostic.Message);
        else Debug.Log(diagnostic.Message);
      }
      Debug.Log($"[ScenarioRequirementsBuild] scenarios={ScenarioRequirementsResourceScanner.Discover().Count} warnings={result.WarningCount} errors={result.ErrorCount} info={result.InfoCount}");
    }

    public static ScenarioRequirementsBuildReport Validate(BuildReport buildReport)
    {
      var result = new ScenarioRequirementsBuildReport();
      var profiles = AssetDatabase.FindAssets("t:ScenarioSceneCompositionProfile")
        .Select(AssetDatabase.GUIDToAssetPath)
        .Select(AssetDatabase.LoadAssetAtPath<ScenarioSceneCompositionProfile>)
        .Where(value => value != null && !string.IsNullOrWhiteSpace(value.Identifier))
        .OrderBy(value => value.Identifier, StringComparer.Ordinal).ToArray();
      var buildScenes = EditorBuildSettings.scenes;
      var discovered = ScenarioRequirementsResourceScanner.Discover();
      var production = buildReport != null && (buildReport.summary.options & BuildOptions.Development) == 0;

      foreach (var pair in discovered)
      {
        try
        {
          var graph = ScenarioGraphLoader.LoadFromJson(System.Text.Encoding.UTF8.GetString(pair.SourceBytes));
          var selected = SelectProfiles(profiles, graph.Identifier, result, pair.ScenarioPath);
          if (selected.Length == 0)
          {
            result.Add(graph.Identifier, pair.ScenarioPath, string.Empty, string.Empty, string.Empty, "SIR506", "CompositionProfileMissing", ScenarioRequirementDiagnosticSeverity.Error, "No composition profile selected for this scenario.");
            var inferred = ScenarioRequirementCompiler.CompileInferred(graph);
            AddCompilerDiagnostics(result, inferred, graph.Identifier, pair.ScenarioPath, string.Empty);
            continue;
          }
          foreach (var profile in selected) ValidateScenarioProfile(pair, graph, profile, buildScenes, production, result);
        }
        catch (Exception ex)
        {
          result.Add(string.Empty, pair.ScenarioPath, string.Empty, string.Empty, string.Empty, "SIR505", "ScenarioAssetDiscoveryFailure", ScenarioRequirementDiagnosticSeverity.Error, ex.Message);
        }
      }
      result.HasBlockingErrors = result.ErrorCount > 0 && production;
      return result;
    }

    private static ScenarioSceneCompositionProfile[] SelectProfiles(ScenarioSceneCompositionProfile[] profiles, string identifier, ScenarioRequirementsBuildReport report, string assetPath)
    {
      var exact = profiles.Where(value => value.ScenarioSelectors.Any(selector => selector != null && selector.IsExact && selector.Pattern == identifier)).ToArray();
      if (exact.Length > 1) report.Add(identifier, assetPath, string.Empty, string.Empty, string.Empty, "SIR503", "DuplicateOrAmbiguousProfile", ScenarioRequirementDiagnosticSeverity.Error, "Multiple exact composition profiles match.");
      if (exact.Length > 0) return exact;
      var wildcard = profiles.Where(value => value.Matches(identifier)).ToArray();
      if (wildcard.Length > 1) report.Add(identifier, assetPath, string.Empty, string.Empty, string.Empty, "SIR503", "DuplicateOrAmbiguousProfile", ScenarioRequirementDiagnosticSeverity.Error, "Multiple wildcard composition profiles match.");
      return wildcard.Length == 1 ? wildcard : Array.Empty<ScenarioSceneCompositionProfile>();
    }

    private static void ValidateScenarioProfile(ScenarioRequirementAssetPair pair, ScenarioGraph graph, ScenarioSceneCompositionProfile profile, EditorBuildSettingsScene[] buildScenes, bool production, ScenarioRequirementsBuildReport report)
    {
      var sidecarAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(pair.SidecarPath);
      ScenarioRequirementsDocument sidecar = null;
      if (sidecarAsset != null)
      {
        var loaded = ScenarioRequirementsLoader.LoadSidecar(sidecarAsset.text);
        if (!loaded.IsValid)
        {
          foreach (var diagnostic in loaded.Diagnostics) report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, diagnostic.HasRequirementKey ? diagnostic.RequirementKey.ToString() : string.Empty, diagnostic.Code, diagnostic.Name, diagnostic.Severity, diagnostic.Message);
          return;
        }
        sidecar = loaded.Document;
      }
      else if (profile.ValidationProfile.RequireProfileForBuild)
      {
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, string.Empty, "SIR506", "SidecarMissingUnderPolicy", ScenarioRequirementDiagnosticSeverity.Error, "Requirements sidecar is required by the selected profile.");
      }
      var manifest = ScenarioRequirementCompiler.Compile(graph, pair.SourceBytes, sidecar, new ScenarioRequirementCompilationContext(DateTime.UtcNow));
      AddCompilerDiagnostics(report, manifest, graph.Identifier, pair.ScenarioPath, profile.Identifier);
      var compositionScenes = profile.Scenes
        .Select(scene => new ScenarioRequirementCompositionScene(scene.ScenePath, scene.SceneGuid, scene.Role))
        .ToArray();
      var composition = new ScenarioRequirementSceneComposition(profile.Identifier, compositionScenes);
      var snapshot = ScenarioRequirementsSceneScanner.Scan(manifest, composition);
      var sceneReport = ScenarioRequirementValidationEngine.Validate(manifest, snapshot, composition);
      foreach (var diagnostic in sceneReport.Diagnostics)
      {
        var severity = diagnostic.Severity;
        if (profile.ValidationProfile.Mode == ScenarioRequirementsValidationProfileMode.Authoring
            && severity >= ScenarioRequirementDiagnosticSeverity.Error)
          severity = ScenarioRequirementDiagnosticSeverity.Warning;
        else if (profile.ValidationProfile.Mode == ScenarioRequirementsValidationProfileMode.Production
                 && (diagnostic.Code == "SIR310" || diagnostic.Code == "SIR404" || diagnostic.Code == "SIR413"))
          severity = ScenarioRequirementDiagnosticSeverity.Error;
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, diagnostic.ScenePath,
          diagnostic.RequirementKey.HasValue ? diagnostic.RequirementKey.Value.ToString() : string.Empty,
          diagnostic.Code, diagnostic.Name, severity, diagnostic.Message);
      }
      foreach (var scene in profile.Scenes)
      {
        var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.ScenePath);
        if (asset == null || AssetDatabase.AssetPathToGUID(scene.ScenePath) != scene.SceneGuid)
          report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, scene.ScenePath, string.Empty, "SIR502", "ScenePathGuidMismatch", ScenarioRequirementDiagnosticSeverity.Error, "Profile scene path/GUID does not resolve to the same asset.");
        var buildScene = buildScenes.FirstOrDefault(value => value.path == scene.ScenePath || value.guid.ToString() == scene.SceneGuid);
        if (!scene.Optional && (buildScene == null || !buildScene.enabled))
          report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, scene.ScenePath, string.Empty, "SIR501", "RequiredSceneAbsentFromBuildSettings", ScenarioRequirementDiagnosticSeverity.Error, "Required profile scene is not enabled in Build Settings.");
        var buildSceneCount = buildScenes.Count(value => value.path == scene.ScenePath && value.enabled);
        if (buildSceneCount < scene.MinimumLoadCount || buildSceneCount > scene.MaximumLoadCount)
          report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, scene.ScenePath, string.Empty, "SIR504", "SceneLoadCountMismatch", ScenarioRequirementDiagnosticSeverity.Error, $"Expected scene load count {scene.MinimumLoadCount}-{scene.MaximumLoadCount}, found {buildSceneCount}.");
      }
      if (profile.ValidationProfile.FailOnIndeterminate && sceneReport.Results.Any(value => value.Status == ScenarioRequirementValidationStatus.Indeterminate))
        report.Add(graph.Identifier, pair.ScenarioPath, profile.Identifier, string.Empty, string.Empty, "SIR507", "IndeterminateRequirement", ScenarioRequirementDiagnosticSeverity.Error, "Profile requires proof for indeterminate requirements.");
    }

    private static void AddCompilerDiagnostics(ScenarioRequirementsBuildReport report, ScenarioRequirementManifest manifest, string scenarioIdentifier, string assetPath, string profileIdentifier)
    {
      foreach (var diagnostic in manifest.Diagnostics)
        report.Add(scenarioIdentifier, assetPath, profileIdentifier, string.Empty, diagnostic.HasRequirementKey ? diagnostic.RequirementKey.ToString() : string.Empty, diagnostic.Code, diagnostic.Name, diagnostic.Severity, diagnostic.Message);
    }
  }
}
#endif
