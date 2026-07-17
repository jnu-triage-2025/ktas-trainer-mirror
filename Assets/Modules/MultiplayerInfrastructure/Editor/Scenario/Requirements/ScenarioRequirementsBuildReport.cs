#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using MultiplayerInfrastructure.Scenario.Requirements;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace MultiplayerInfrastructure.Scenario.Requirements.Editor
{
  public sealed class ScenarioRequirementsBuildDiagnostic
  {
    public string ScenarioIdentifier { get; }
    public string AssetPath { get; }
    public string ProfileIdentifier { get; }
    public string ScenePath { get; }
    public string RequirementKey { get; }
    public string Code { get; }
    public string Name { get; }
    public ScenarioRequirementDiagnosticSeverity Severity { get; }
    public string Message { get; }
    public ScenarioRequirementsBuildDiagnostic(string scenarioIdentifier, string assetPath, string profileIdentifier, string scenePath, string requirementKey, string code, string name, ScenarioRequirementDiagnosticSeverity severity, string message)
    { ScenarioIdentifier = scenarioIdentifier ?? string.Empty; AssetPath = assetPath ?? string.Empty; ProfileIdentifier = profileIdentifier ?? string.Empty; ScenePath = scenePath ?? string.Empty; RequirementKey = requirementKey ?? string.Empty; Code = code ?? string.Empty; Name = name ?? string.Empty; Severity = severity; Message = message ?? string.Empty; }
  }

  public sealed class ScenarioRequirementsBuildReport
  {
    private readonly List<ScenarioRequirementsBuildDiagnostic> _diagnostics = new List<ScenarioRequirementsBuildDiagnostic>();
    public IReadOnlyList<ScenarioRequirementsBuildDiagnostic> Diagnostics => new ReadOnlyCollection<ScenarioRequirementsBuildDiagnostic>(_diagnostics.OrderBy(value => value.ScenarioIdentifier, StringComparer.Ordinal).ThenBy(value => value.RequirementKey, StringComparer.Ordinal).ThenBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.AssetPath, StringComparer.Ordinal).ThenBy(value => value.ScenePath, StringComparer.Ordinal).ThenBy(value => value.ProfileIdentifier, StringComparer.Ordinal).ToArray());
    public int ErrorCount => Diagnostics.Count(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error);
    public int WarningCount => Diagnostics.Count(value => value.Severity == ScenarioRequirementDiagnosticSeverity.Warning);
    public int InfoCount => Diagnostics.Count(value => value.Severity == ScenarioRequirementDiagnosticSeverity.Info);

    // Whether any recorded error should actually fail the build.  This is
    // distinct from ErrorCount: the proposal's staged strictness (§14) means a
    // Development/Authoring profile reports errors without necessarily blocking
    // the build, and a missing profile must never block by default.  Only
    // Production-mode profile errors and hard discovery failures (recorded via
    // MarkBlocking) gate the build.
    private bool _hasBlockingErrors;
    public bool HasBlockingErrors { get => _hasBlockingErrors; set => _hasBlockingErrors = value; }
    public void MarkBlocking(bool blocking) => _hasBlockingErrors |= blocking;
    public void Add(ScenarioRequirementsBuildDiagnostic diagnostic) => _diagnostics.Add(diagnostic);
    public void Add(string scenario, string asset, string profile, string scene, string key, string code, string name, ScenarioRequirementDiagnosticSeverity severity, string message) => Add(new ScenarioRequirementsBuildDiagnostic(scenario, asset, profile, scene, key, code, name, severity, message));
    public string ToStableSummary() => $"Scenario requirements build validation failed. errors={ErrorCount} warnings={WarningCount} info={InfoCount}.";
    public byte[] ToUtf8Json(string buildTarget, string result)
    {
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
      {
        writer.WriteStartObject(); writer.WriteString("format", "scenario-requirements-build-report"); writer.WriteNumber("schemaVersion", 1); writer.WriteString("result", result); writer.WriteString("buildTarget", buildTarget ?? string.Empty); writer.WritePropertyName("diagnostics"); writer.WriteStartArray();
        foreach (var diagnostic in Diagnostics) { writer.WriteStartObject(); writer.WriteString("scenarioIdentifier", diagnostic.ScenarioIdentifier); writer.WriteString("assetPath", diagnostic.AssetPath); writer.WriteString("profileIdentifier", diagnostic.ProfileIdentifier); writer.WriteString("scenePath", diagnostic.ScenePath); writer.WriteString("requirementKey", diagnostic.RequirementKey); writer.WriteString("code", diagnostic.Code); writer.WriteString("name", diagnostic.Name); writer.WriteString("severity", diagnostic.Severity.ToString()); writer.WriteString("message", diagnostic.Message); writer.WriteEndObject(); }
        writer.WriteEndArray(); writer.WritePropertyName("summary"); writer.WriteStartObject(); writer.WriteNumber("info", InfoCount); writer.WriteNumber("warning", WarningCount); writer.WriteNumber("error", ErrorCount); writer.WriteEndObject(); writer.WriteEndObject();
      }
      return stream.ToArray().Concat(new[] { (byte)'\n' }).ToArray();
    }

    public void WriteUtf8Json(string path, string buildTarget, string result)
    {
      if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Report path is null or empty.", nameof(path));
      var directory = Path.GetDirectoryName(path);
      if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
      File.WriteAllBytes(path, ToUtf8Json(buildTarget, result));
    }
  }
}
#endif
