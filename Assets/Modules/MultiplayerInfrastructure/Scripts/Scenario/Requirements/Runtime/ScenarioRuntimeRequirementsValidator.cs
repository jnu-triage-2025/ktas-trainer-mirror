using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public enum ScenarioRuntimeValidationMode { Off, ReportOnly, AbortScenarioStart, AbortSessionBootstrap }
  public enum ScenarioRuntimeReadiness { Ready, NotReady, Indeterminate }

  public sealed class ScenarioRuntimeValidationContext
  {
    public ScenarioRequirementAuthority Authority { get; }
    public bool IsServer { get; }
    public bool IsClient { get; }
    public bool IsHost { get; }
    public ScenarioRuntimeValidationContext(ScenarioRequirementAuthority authority, bool isServer = true, bool isClient = true, bool isHost = false) { Authority = authority; IsServer = isServer; IsClient = isClient; IsHost = isHost; }
  }

  public sealed class ScenarioRuntimeValidationResult
  {
    public string ScenarioIdentifier { get; }
    public ScenarioRuntimeReadiness Readiness { get; }
    public ScenarioRequirementManifest Manifest { get; }
    public ScenarioRequirementValidationReport Report { get; }
    public IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics { get; }
    public bool ShouldAbort { get; }
    public ScenarioRuntimeValidationResult(string scenarioIdentifier, ScenarioRuntimeReadiness readiness, ScenarioRequirementManifest manifest, ScenarioRequirementValidationReport report, IEnumerable<ScenarioRequirementDiagnostic> diagnostics, bool shouldAbort)
    { ScenarioIdentifier = scenarioIdentifier ?? string.Empty; Readiness = readiness; Manifest = manifest; Report = report; Diagnostics = (diagnostics ?? Array.Empty<ScenarioRequirementDiagnostic>()).OrderBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.Message, StringComparer.Ordinal).ToArray(); ShouldAbort = shouldAbort; }
  }

  public static class ScenarioRuntimeRequirementsValidator
  {
    public static bool AppliesToContext(ScenarioRequirementAuthority authority, ScenarioRuntimeValidationContext context)
    {
      if (authority == ScenarioRequirementAuthority.Any) return true;
      if (authority == ScenarioRequirementAuthority.Server) return context.IsServer;
      if (authority == ScenarioRequirementAuthority.Client) return context.IsClient;
      return authority == ScenarioRequirementAuthority.HostOnly && context.IsHost;
    }

    public static ScenarioRuntimeValidationResult Validate(
      ScenarioGraph graph,
      ScenarioRuntimeValidationMode mode,
      ScenarioRuntimeValidationContext context,
      ScenarioRequirementSceneComposition composition = null)
    {
      if (graph == null) throw new ArgumentNullException(nameof(graph));
      if (mode == ScenarioRuntimeValidationMode.Off)
        return new ScenarioRuntimeValidationResult(graph.Identifier, ScenarioRuntimeReadiness.Ready, null, null, Array.Empty<ScenarioRequirementDiagnostic>(), false);
      ScenarioRuntimeManifestRegistry.EnsureLoaded();
      ScenarioRequirementManifest manifest;
      var diagnostics = new List<ScenarioRequirementDiagnostic>(ScenarioRuntimeManifestRegistry.Diagnostics);
      var manifestDiagnostics = new List<ScenarioRequirementDiagnostic>();
      var graphFingerprint = ScenarioGraphFingerprint.Compute(graph);
      if (ScenarioRuntimeManifestRegistry.TryGet(graph.Identifier, graphFingerprint, out var entry) && entry.Manifest != null)
      {
        manifest = entry.Manifest;
        diagnostics.AddRange(entry.Diagnostics);
        manifestDiagnostics.AddRange(entry.Diagnostics);
      }
      else
      {
        manifest = ScenarioRequirementCompiler.CompileInferred(graph);
        var code = ScenarioRuntimeManifestRegistry.HasIdentifier(graph.Identifier) ? "SIR612" : "SIR605";
        var name = code == "SIR612" ? "RuntimeManifestFingerprintMismatch" : "InferredOnlyRuntimeManifest";
        var message = code == "SIR612"
          ? "A runtime sidecar exists for this scenario identifier, but its graph fingerprint does not match the graph being started; inferred requirements were used."
          : "No runtime sidecar manifest was found; inferred requirements were used.";
        diagnostics.Add(new ScenarioRequirementDiagnostic(code, name, ScenarioRequirementDiagnosticSeverity.Warning, message, null, string.Empty, string.Empty));
        diagnostics.AddRange(manifest.Diagnostics);
        manifestDiagnostics.AddRange(manifest.Diagnostics);
      }

      var snapshot = ScenarioRuntimeProviderSnapshotBuilder.Build(composition);
      diagnostics.AddRange(snapshot.Diagnostics.Select(value => new ScenarioRequirementDiagnostic(value.Code, value.Name, value.Severity, value.Message, null, value.RequirementKey.HasValue ? value.RequirementKey.Value.Identifier : string.Empty, value.FixHint, value.RequirementKey)));
      if (composition != null && composition.Scenes.Any(scene =>
      {
        var runtimeScene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scene.ScenePath);
        return !runtimeScene.IsValid() || !runtimeScene.isLoaded || !ScenarioRuntimeSceneReadiness.IsReady(runtimeScene);
      }))
        diagnostics.Add(new ScenarioRequirementDiagnostic("SIR608", "SceneReadinessPending", ScenarioRequirementDiagnosticSeverity.Warning, "One or more composition scenes have loaded but have not completed bootstrap readiness.", null, string.Empty, string.Empty));
      var report = ScenarioRequirementValidationEngine.ValidateRuntime(manifest, snapshot, composition ?? new ScenarioRequirementSceneComposition("runtime", Array.Empty<ScenarioRequirementCompositionScene>()));
      var applicableResults = report.Results.Where(value => AppliesToContext(value.Requirement.Authority, context)).ToArray();
      var readiness = applicableResults.Any(value => value.Status == ScenarioRequirementValidationStatus.NotReady) ? ScenarioRuntimeReadiness.NotReady : applicableResults.Any(value => value.Status == ScenarioRequirementValidationStatus.Indeterminate) ? ScenarioRuntimeReadiness.Indeterminate : ScenarioRuntimeReadiness.Ready;
      var blocking = applicableResults.Any(value => value.Status == ScenarioRequirementValidationStatus.Missing || value.Status == ScenarioRequirementValidationStatus.Duplicate || value.Status == ScenarioRequirementValidationStatus.MissingCapability || value.Status == ScenarioRequirementValidationStatus.WrongType || value.Status == ScenarioRequirementValidationStatus.WrongScene || value.Status == ScenarioRequirementValidationStatus.NotRegistered);
      // Diagnostics belonging to this selected manifest (schema/load/merge
      // failures included) invalidate its contract in strict mode.  Do not use
      // global discovery diagnostics here: an unrelated Resources scenario
      // must not block this one.
      blocking |= manifestDiagnostics.Any(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error);
      if (diagnostics.Any(value => value.Code == "SIR608")) readiness = ScenarioRuntimeReadiness.NotReady;
      if (applicableResults.Any(value => value.Requirement.MustProve) && readiness == ScenarioRuntimeReadiness.Indeterminate) blocking = true;
      var strictMode = mode == ScenarioRuntimeValidationMode.AbortScenarioStart || mode == ScenarioRuntimeValidationMode.AbortSessionBootstrap;
      var shouldAbort = strictMode && (blocking || readiness == ScenarioRuntimeReadiness.NotReady);
      return new ScenarioRuntimeValidationResult(graph.Identifier, readiness, manifest, report, diagnostics, shouldAbort);
    }

    public static IEnumerator WaitUntilReady(
      ScenarioGraph graph,
      ScenarioRuntimeValidationMode mode,
      ScenarioRuntimeValidationContext context,
      float timeoutSeconds,
      float pollIntervalSeconds,
      Action<ScenarioRuntimeValidationResult> completed,
      ScenarioRequirementSceneComposition composition = null)
    {
      var deadline = Time.realtimeSinceStartup + Mathf.Max(0f, timeoutSeconds);
      ScenarioRuntimeValidationResult result;
      do
      {
        result = Validate(graph, mode, context, composition);
        if (result.Readiness != ScenarioRuntimeReadiness.NotReady)
        {
          completed?.Invoke(result);
          yield break;
        }
        if (Time.realtimeSinceStartup >= deadline) break;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, pollIntervalSeconds));
      } while (true);

      var timeoutDiagnostics = result.Diagnostics.Concat(new[]
      {
        new ScenarioRequirementDiagnostic("SIR606", "RuntimeReadinessTimeout", ScenarioRequirementDiagnosticSeverity.Error, $"Runtime requirements did not become ready within {timeoutSeconds:0.###} seconds.", null, string.Empty, string.Empty)
      });
      completed?.Invoke(new ScenarioRuntimeValidationResult(result.ScenarioIdentifier, ScenarioRuntimeReadiness.NotReady, result.Manifest, result.Report, timeoutDiagnostics, mode == ScenarioRuntimeValidationMode.AbortScenarioStart || mode == ScenarioRuntimeValidationMode.AbortSessionBootstrap));
    }
  }
}
