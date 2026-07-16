using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public sealed class ScenarioRuntimeManifestEntry
  {
    public string Identifier { get; }
    public string GraphFingerprint { get; }
    public TextAsset ScenarioAsset { get; }
    public TextAsset RequirementsAsset { get; }
    public ScenarioGraph Graph { get; }
    public ScenarioRequirementManifest Manifest { get; }
    public IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics { get; }
    public bool IsValid { get; }
    public bool IsInferredOnly { get; }
    internal ScenarioRuntimeManifestEntry(string identifier, string graphFingerprint, TextAsset scenarioAsset, TextAsset requirementsAsset, ScenarioGraph graph, ScenarioRequirementManifest manifest, IEnumerable<ScenarioRequirementDiagnostic> diagnostics, bool inferredOnly)
    { Identifier = identifier; GraphFingerprint = graphFingerprint ?? string.Empty; ScenarioAsset = scenarioAsset; RequirementsAsset = requirementsAsset; Graph = graph; Manifest = manifest; Diagnostics = (diagnostics ?? Array.Empty<ScenarioRequirementDiagnostic>()).OrderBy(value => value.Code, StringComparer.Ordinal).ToArray(); IsValid = manifest != null && !Diagnostics.Any(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error); IsInferredOnly = inferredOnly; }
  }

  public static class ScenarioRuntimeManifestRegistry
  {
    private static readonly Dictionary<string, ScenarioRuntimeManifestEntry> Entries = new Dictionary<string, ScenarioRuntimeManifestEntry>(StringComparer.Ordinal);
    private static readonly List<ScenarioRequirementDiagnostic> DiscoveryDiagnostics = new List<ScenarioRequirementDiagnostic>();
    private static bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnSubsystemRegistration() => Reset();

    public static IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics => DiscoveryDiagnostics.OrderBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.Message, StringComparer.Ordinal).ToArray();

    public static void Reset()
    {
      Entries.Clear();
      DiscoveryDiagnostics.Clear();
      _initialized = false;
    }

    public static void EnsureLoaded()
    {
      if (_initialized) return;
      _initialized = true;
      foreach (var asset in Resources.LoadAll<TextAsset>("Scenario").OrderBy(value => value.name, StringComparer.Ordinal))
      {
        if (asset == null || !asset.name.EndsWith(".scenario", StringComparison.Ordinal)) continue;
        var identifier = asset.name.Substring(0, asset.name.Length - ".scenario".Length);
        var sidecar = Resources.Load<TextAsset>("Scenario/" + asset.name + ".requirements");
        var diagnostics = new List<ScenarioRequirementDiagnostic>();
        try
        {
          var graph = ScenarioGraphLoader.LoadFromJson(asset.text);
          var graphFingerprint = ScenarioGraphFingerprint.Compute(graph);
          var runtimeKey = identifier + "|" + graphFingerprint;
          if (Entries.ContainsKey(runtimeKey))
          {
            DiscoveryDiagnostics.Add(Diagnostic("SIR509", "DuplicateRuntimeScenarioIdentifier", $"Multiple Resources scenarios use identifier and fingerprint '{runtimeKey}'."));
            continue;
          }
          if (!string.Equals(graph.Identifier, identifier, StringComparison.Ordinal))
            diagnostics.Add(Diagnostic("SIR610", "RuntimeScenarioIdentifierMismatch", $"Resource name '{identifier}' does not match graph identifier '{graph.Identifier}'."));
          ScenarioRequirementsDocument sidecarDocument = null;
          if (sidecar != null)
          {
            var loaded = ScenarioRequirementsLoader.LoadSidecar(sidecar.text);
            diagnostics.AddRange(loaded.Diagnostics);
            sidecarDocument = loaded.Document;
          }
          var sourceBytes = Encoding.UTF8.GetBytes(asset.text);
          var manifest = ScenarioRequirementCompiler.Compile(graph, sourceBytes, sidecarDocument, new ScenarioRequirementCompilationContext(DateTime.UtcNow));
          Entries[runtimeKey] = new ScenarioRuntimeManifestEntry(identifier, graphFingerprint, asset, sidecar, graph, manifest, diagnostics.Concat(manifest.Diagnostics), sidecar == null);
        }
        catch (Exception ex)
        {
          diagnostics.Add(Diagnostic("SIR604", "RuntimeManifestLoadFailure", ex.Message));
          Entries[identifier + "|invalid"] = new ScenarioRuntimeManifestEntry(identifier, string.Empty, asset, sidecar, null, null, diagnostics, sidecar == null);
        }
      }
    }

    public static bool TryGet(string identifier, out ScenarioRuntimeManifestEntry entry)
    {
      EnsureLoaded();
      var matches = Entries.Where(value => value.Value.Identifier == (identifier ?? string.Empty)).Select(value => value.Value).ToArray();
      entry = matches.Length == 1 ? matches[0] : null;
      if (matches.Length > 1) DiscoveryDiagnostics.Add(Diagnostic("SIR611", "AmbiguousRuntimeManifestFingerprint", $"Multiple runtime manifests match scenario identifier '{identifier}'."));
      return entry != null;
    }

    /// <summary>
    /// Resolves a manifest only when both graph identity and content fingerprint match.
    /// A sidecar declaration is source-specific and must never be reused for a graph that
    /// merely happens to have the same scenario identifier.
    /// </summary>
    public static bool TryGet(string identifier, string graphFingerprint, out ScenarioRuntimeManifestEntry entry)
    {
      EnsureLoaded();
      var runtimeKey = (identifier ?? string.Empty) + "|" + (graphFingerprint ?? string.Empty);
      return Entries.TryGetValue(runtimeKey, out entry);
    }

    public static bool HasIdentifier(string identifier)
    {
      EnsureLoaded();
      return Entries.Values.Any(value => value.Identifier == (identifier ?? string.Empty));
    }

    private static ScenarioRequirementDiagnostic Diagnostic(string code, string name, string message)
      => new ScenarioRequirementDiagnostic(code, name, ScenarioRequirementDiagnosticSeverity.Error, message, null, string.Empty, string.Empty);
  }
}
