using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public enum ScenarioRequirementCandidateDisposition { ProposesOverride, ProposesDeclare, Invalid }

  public sealed class ScenarioRequirementCandidatePreview
  {
    public ScenarioRequirementKey Key { get; }
    public ScenarioRequirementCandidateDisposition Disposition { get; }
    public ScenarioRequirementDeclarationOperation ProposedOperation { get; }
    public double Confidence { get; }
    public IReadOnlyList<ScenarioRequirementDiagnostic> Diagnostics { get; }
    internal ScenarioRequirementCandidatePreview(ScenarioRequirementKey key, ScenarioRequirementCandidateDisposition disposition, ScenarioRequirementDeclarationOperation operation, double confidence, IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
    { Key = key; Disposition = disposition; ProposedOperation = operation; Confidence = confidence; Diagnostics = new ReadOnlyCollection<ScenarioRequirementDiagnostic>(ScenarioRequirementsLoader.Sort(diagnostics)); }
  }

  public sealed class ScenarioRequirementCandidatePreviewResult
  {
    public IReadOnlyList<ScenarioRequirementCandidatePreview> Candidates { get; }
    public bool IsValid => Candidates.All(value => value.Disposition != ScenarioRequirementCandidateDisposition.Invalid);
    internal ScenarioRequirementCandidatePreviewResult(IEnumerable<ScenarioRequirementCandidatePreview> candidates)
      => Candidates = new ReadOnlyCollection<ScenarioRequirementCandidatePreview>(candidates.OrderBy(value => value.Key).ToArray());
  }

  public static class ScenarioRequirementsCandidatePreviewService
  {
    public static ScenarioRequirementsDocument CreateApprovedDocument(
      ScenarioRequirementsDocument current,
      ScenarioRequirementCandidatesDocument candidates,
      ScenarioRequirementManifest inferredManifest,
      IEnumerable<ScenarioRequirementKey> approvedKeys)
    {
      if (current == null) throw new ArgumentNullException(nameof(current));
      if (candidates == null) throw new ArgumentNullException(nameof(candidates));
      if (inferredManifest == null) throw new ArgumentNullException(nameof(inferredManifest));
      if (!string.Equals(current.ScenarioIdentifier, candidates.ScenarioIdentifier, StringComparison.Ordinal))
        throw new InvalidOperationException("Candidate scenarioIdentifier does not match the sidecar.");
      var approved = new HashSet<ScenarioRequirementKey>(approvedKeys ?? Array.Empty<ScenarioRequirementKey>());
      var declarations = (current.DTO.Declarations ?? new List<ScenarioRequirementDeclarationDTO>()).ToList();
      foreach (var candidate in candidates.DTO.Candidates ?? new List<ScenarioRequirementCandidateDTO>())
      {
        var key = new ScenarioRequirementKey(candidate.Kind, candidate.Identifier.Trim());
        if (!approved.Contains(key)) continue;
        var existingIndex = declarations.FindIndex(value => value.Selector.Kind == key.Kind
          && string.Equals(value.Selector.Identifier.Trim(), key.Identifier, StringComparison.Ordinal)
          && value.OccurrenceSelector == null);
        var existing = existingIndex >= 0 ? declarations[existingIndex] : null;
        if (candidate.SuggestedBinding != null
            && candidate.SuggestedBinding.Mode == ScenarioRequirementBindingMode.RegistryProvided)
          throw new InvalidOperationException($"Candidate '{key}' cannot be approved with RegistryProvided binding because candidates do not declare providerIdentifier.");
        if (candidate.SuggestedBinding != null && existing?.Binding != null
            && (existing.Binding.Mode != candidate.SuggestedBinding.Mode
                || !string.Equals(existing.Binding.FactoryIdentifier, candidate.SuggestedBinding.FactoryIdentifier, StringComparison.Ordinal)))
          throw new InvalidOperationException($"Candidate '{key}' conflicts with the existing binding and must be resolved manually.");
        var approvedDeclaration = new ScenarioRequirementDeclarationDTO
        {
          Selector = new ScenarioRequirementSelectorDTO { Kind = key.Kind, Identifier = key.Identifier },
          Operation = existing?.Operation ?? (inferredManifest.Requirements.Any(value => value.Key.Equals(key))
            ? ScenarioRequirementDeclarationOperation.Override : ScenarioRequirementDeclarationOperation.Declare),
          Capabilities = (existing?.Capabilities ?? new List<ScenarioRequirementCapability>())
            .Concat(candidate.Capabilities ?? new List<ScenarioRequirementCapability>()).Distinct().OrderBy(value => value).ToList(),
          Scope = candidate.SuggestedBinding?.Scope ?? existing?.Scope,
          Authority = candidate.SuggestedBinding?.Authority ?? existing?.Authority,
          Availability = existing?.Availability,
          Cardinality = existing?.Cardinality,
          MustProve = existing?.MustProve,
          Binding = candidate.SuggestedBinding == null ? existing?.Binding : new ScenarioRequirementBindingDTO
          {
            Mode = candidate.SuggestedBinding.Mode,
            FactoryIdentifier = candidate.SuggestedBinding.FactoryIdentifier
          },
          Configuration = existing?.Configuration,
          Notes = candidate.Notes ?? existing?.Notes
        };
        if (existingIndex >= 0) declarations[existingIndex] = approvedDeclaration;
        else declarations.Add(approvedDeclaration);
      }
      return new ScenarioRequirementsDocument(new ScenarioRequirementsDocumentDTO
      {
        Format = current.DTO.Format,
        SchemaVersion = current.DTO.SchemaVersion,
        ScenarioIdentifier = current.DTO.ScenarioIdentifier,
        Source = current.DTO.Source,
        Declarations = declarations,
        Suppressions = (current.DTO.Suppressions ?? new List<ScenarioRequirementSuppressionDTO>()).ToList()
      });
    }

    public static ScenarioRequirementCandidatePreviewResult CreatePreview(
      ScenarioGraph graph,
      ScenarioRequirementManifest inferredManifest,
      ScenarioRequirementCandidatesDocument document,
      ISet<string> allowedFactoryIdentifiers)
    {
      if (graph == null) throw new ArgumentNullException(nameof(graph));
      if (inferredManifest == null) throw new ArgumentNullException(nameof(inferredManifest));
      if (document == null) throw new ArgumentNullException(nameof(document));
      var result = new List<ScenarioRequirementCandidatePreview>();
      var duplicateKeys = new HashSet<ScenarioRequirementKey>();
      var seenKeys = new HashSet<ScenarioRequirementKey>();
      foreach (var candidate in document.DTO.Candidates ?? new List<ScenarioRequirementCandidateDTO>())
      {
        var candidateKey = new ScenarioRequirementKey(candidate.Kind, candidate.Identifier.Trim());
        if (!seenKeys.Add(candidateKey)) duplicateKeys.Add(candidateKey);
      }
      foreach (var dto in document.DTO.Candidates ?? new List<ScenarioRequirementCandidateDTO>())
      {
        var key = new ScenarioRequirementKey(dto.Kind, dto.Identifier.Trim());
        var diagnostics = new List<ScenarioRequirementDiagnostic>();
        if (!string.Equals(document.ScenarioIdentifier, graph.Identifier, StringComparison.Ordinal)
            || !string.Equals(document.ScenarioIdentifier, inferredManifest.ScenarioIdentifier, StringComparison.Ordinal))
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR106", "ScenarioIdentifierMismatch", "Candidate scenarioIdentifier does not match the graph.", key));
        if (duplicateKeys.Contains(key))
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR703", "DuplicateCandidate", $"Duplicate candidate '{key}'.", key));
        var existing = inferredManifest.Requirements.FirstOrDefault(value => value.Key.Equals(key));
        foreach (var evidence in dto.Evidence ?? new List<ScenarioRequirementEvidenceDTO>())
        {
          if (!graph.TryGetNode(evidence.NodeIdentifier, out var node)
              || !string.Equals(node.NodeType.ToString(), evidence.NodeType, StringComparison.Ordinal)
              || existing == null
              || !existing.Occurrences.Any(value => value.NodeIdentifier == evidence.NodeIdentifier && value.FieldPath == evidence.FieldPath))
          {
            diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR701", "CandidateEvidenceMismatch",
              $"Candidate '{key}' evidence '{evidence.NodeIdentifier}:{evidence.FieldPath}' does not match inferred occurrences.", key));
          }
        }
        var factory = dto.SuggestedBinding?.FactoryIdentifier;
        if (!string.IsNullOrWhiteSpace(factory) && (allowedFactoryIdentifiers == null || !allowedFactoryIdentifiers.Contains(factory)))
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR702", "UnknownCandidateFactory", $"Candidate '{key}' uses unknown factory '{factory}'.", key));
        if (dto.SuggestedBinding?.Mode == ScenarioRequirementBindingMode.RegistryProvided)
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR702", "CandidateRegistryProviderUnsupported", $"Candidate '{key}' cannot propose RegistryProvided without a providerIdentifier.", key));

        result.Add(new ScenarioRequirementCandidatePreview(
          key,
          diagnostics.Count > 0 ? ScenarioRequirementCandidateDisposition.Invalid : existing == null ? ScenarioRequirementCandidateDisposition.ProposesDeclare : ScenarioRequirementCandidateDisposition.ProposesOverride,
          existing == null ? ScenarioRequirementDeclarationOperation.Declare : ScenarioRequirementDeclarationOperation.Override,
          dto.Confidence,
          diagnostics));
      }
      return new ScenarioRequirementCandidatePreviewResult(result);
    }
  }
}
