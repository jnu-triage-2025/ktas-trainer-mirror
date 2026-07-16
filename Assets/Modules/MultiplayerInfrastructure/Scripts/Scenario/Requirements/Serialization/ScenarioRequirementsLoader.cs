using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Json.Schema;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public sealed class ScenarioRequirementsDocument
  {
    internal ScenarioRequirementsDocumentDTO DTO { get; }
    public int SchemaVersion => DTO.SchemaVersion;
    public string ScenarioIdentifier => DTO.ScenarioIdentifier;
    public string ScenarioSha256 => DTO.Source.ScenarioSha256;
    internal ScenarioRequirementsDocument(ScenarioRequirementsDocumentDTO dto) => DTO = dto;
  }

  public sealed class ScenarioRequirementCandidatesDocument
  {
    internal ScenarioRequirementCandidatesDTO DTO { get; }
    public string ScenarioIdentifier => DTO.ScenarioIdentifier;
    public int Count => DTO.Candidates?.Count ?? 0;
    internal ScenarioRequirementCandidatesDocument(ScenarioRequirementCandidatesDTO dto) => DTO = dto;
  }

  public sealed class ScenarioRequirementsLoadResult<T> where T : class
  {
    public T Document { get; }
    public IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics { get; }
    public bool IsValid => Document != null && !Diagnostics.Any(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error);
    internal ScenarioRequirementsLoadResult(T document, IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
    {
      Document = document;
      Diagnostics = new ReadOnlyCollection<ScenarioRequirementDiagnostic>((diagnostics ?? Array.Empty<ScenarioRequirementDiagnostic>()).ToArray());
    }
  }

  public static class ScenarioRequirementsLoader
  {
    private static readonly JsonSerializerOptions Options = CreateOptions();
    private static JsonSchema _sidecarSchema;
    private static JsonSchema _candidateSchema;

    public static ScenarioRequirementsLoadResult<ScenarioRequirementsDocument> LoadSidecar(string json)
    {
      var diagnostics = new List<ScenarioRequirementDiagnostic>();
      if (!TryValidate(json, false, diagnostics)) return new ScenarioRequirementsLoadResult<ScenarioRequirementsDocument>(null, diagnostics);
      try
      {
        var dto = JsonSerializer.Deserialize<ScenarioRequirementsDocumentDTO>(json, Options);
        ValidateBindingSemantics(dto, diagnostics);
        return new ScenarioRequirementsLoadResult<ScenarioRequirementsDocument>(
          diagnostics.Any(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error) ? null : new ScenarioRequirementsDocument(dto),
          Sort(diagnostics));
      }
      catch (JsonException ex)
      {
        diagnostics.Add(Diagnostic("SIR104", "MalformedRequirementsJson", ex.Message));
        return new ScenarioRequirementsLoadResult<ScenarioRequirementsDocument>(null, Sort(diagnostics));
      }
    }

    public static ScenarioRequirementsLoadResult<ScenarioRequirementCandidatesDocument> LoadCandidates(string json)
    {
      var diagnostics = new List<ScenarioRequirementDiagnostic>();
      if (!TryValidate(json, true, diagnostics)) return new ScenarioRequirementsLoadResult<ScenarioRequirementCandidatesDocument>(null, diagnostics);
      try
      {
        var dto = JsonSerializer.Deserialize<ScenarioRequirementCandidatesDTO>(json, Options);
        return new ScenarioRequirementsLoadResult<ScenarioRequirementCandidatesDocument>(new ScenarioRequirementCandidatesDocument(dto), Sort(diagnostics));
      }
      catch (JsonException ex)
      {
        diagnostics.Add(Diagnostic("SIR104", "MalformedRequirementsJson", ex.Message));
        return new ScenarioRequirementsLoadResult<ScenarioRequirementCandidatesDocument>(null, Sort(diagnostics));
      }
    }

    private static bool TryValidate(string json, bool candidates, List<ScenarioRequirementDiagnostic> diagnostics)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        diagnostics.Add(Diagnostic("SIR104", "MalformedRequirementsJson", "Requirements JSON is empty."));
        return false;
      }
      try
      {
        StrictJsonPropertyValidator.RejectDuplicateProperties(json);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("schemaVersion", out var version)
            || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var versionNumber) || versionNumber != 1)
        {
          diagnostics.Add(Diagnostic("SIR103", "UnsupportedSchemaVersion", "Only schemaVersion 1 is supported."));
          return false;
        }
        var result = GetSchema(candidates).Evaluate(document.RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List, RequireFormatValidation = true });
        if (!result.IsValid)
        {
          foreach (var detail in result.Details.Where(value => value.Errors != null && value.Errors.Any()).OrderBy(value => value.InstanceLocation.ToString(), StringComparer.Ordinal))
            diagnostics.Add(Diagnostic("SIR105", "RequirementsSchemaViolation", detail.InstanceLocation + ": " + string.Join(", ", detail.Errors.Select(value => value.Value))));
        }
      }
      catch (JsonException ex)
      {
        var code = ex.Message.StartsWith("Duplicate JSON property", StringComparison.Ordinal) ? "SIR107" : "SIR104";
        diagnostics.Add(Diagnostic(code, code == "SIR107" ? "DuplicateJsonProperty" : "MalformedRequirementsJson", ex.Message));
      }
      return diagnostics.Count == 0;
    }

    private static JsonSchema GetSchema(bool candidates)
    {
      if (candidates)
      {
        if (_candidateSchema == null) _candidateSchema = JsonSchema.FromText(LoadSchema("Schema/scenario.requirements.candidates.schema"));
        return _candidateSchema;
      }
      if (_sidecarSchema == null) _sidecarSchema = JsonSchema.FromText(LoadSchema("Schema/scenario.requirements.schema"));
      return _sidecarSchema;
    }

    private static string LoadSchema(string path)
    {
      var asset = Resources.Load<TextAsset>(path);
      if (asset == null) throw new InvalidOperationException($"Requirements schema not found at Resources/{path}.json");
      return asset.text;
    }

    private static void ValidateBindingSemantics(ScenarioRequirementsDocumentDTO dto, List<ScenarioRequirementDiagnostic> diagnostics)
    {
      if (dto?.Declarations == null) return;
      foreach (var declaration in dto.Declarations)
      {
        var binding = declaration?.Binding;
        if (binding == null) continue;
        if ((binding.Mode == ScenarioRequirementBindingMode.GeneratedSceneObject || binding.Mode == ScenarioRequirementBindingMode.PrefabInstance)
            && string.IsNullOrWhiteSpace(binding.FactoryIdentifier))
          diagnostics.Add(Diagnostic("SIR105", "RequirementsSchemaViolation", $"{binding.Mode} requires factoryIdentifier."));
        if (binding.Mode == ScenarioRequirementBindingMode.RegistryProvided && string.IsNullOrWhiteSpace(binding.ProviderIdentifier))
          diagnostics.Add(Diagnostic("SIR105", "RequirementsSchemaViolation", "RegistryProvided requires providerIdentifier."));
        var scale = declaration.Configuration?.Scale;
        if (scale != null && (scale.X <= 0 || scale.Y <= 0 || scale.Z <= 0))
          diagnostics.Add(Diagnostic("SIR105", "RequirementsSchemaViolation", "Configuration scale axes must be greater than zero."));
      }
      foreach (var suppression in dto.Suppressions ?? new List<ScenarioRequirementSuppressionDTO>())
        if ((suppression?.Reason?.Trim().Length ?? 0) < 10)
          diagnostics.Add(Diagnostic("SIR210", "MalformedSuppression", "Suppression reason must contain at least 10 non-padding characters."));
    }

    private static JsonSerializerOptions CreateOptions()
    {
      var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false, AllowTrailingCommas = false, ReadCommentHandling = JsonCommentHandling.Disallow };
      options.Converters.Add(new JsonStringEnumConverter(null, false));
      return options;
    }

    internal static ScenarioRequirementDiagnostic Diagnostic(string code, string name, string message, ScenarioRequirementKey? key = null)
      => new ScenarioRequirementDiagnostic(code, name, ScenarioRequirementDiagnosticSeverity.Error, message, null, key?.Identifier, string.Empty, key);

    internal static ScenarioRequirementDiagnostic[] Sort(IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
      => diagnostics.OrderBy(value => value.HasRequirementKey ? value.RequirementKey.ToString() : string.Empty, StringComparer.Ordinal)
        .ThenBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.Message, StringComparer.Ordinal).ToArray();
  }
}
