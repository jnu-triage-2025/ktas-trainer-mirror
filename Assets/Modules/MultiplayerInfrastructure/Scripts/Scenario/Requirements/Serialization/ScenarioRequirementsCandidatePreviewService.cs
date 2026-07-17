using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MultiplayerInfrastructure.Scenario.Preflight;

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
      => CreateApprovedDocument(current, candidates, inferredManifest, approvedKeys, null);

    public static ScenarioRequirementsDocument CreateApprovedDocument(
      ScenarioRequirementsDocument current,
      ScenarioRequirementCandidatesDocument candidates,
      ScenarioRequirementManifest inferredManifest,
      IEnumerable<ScenarioRequirementKey> approvedKeys,
      ISet<string> allowedFactoryIdentifiers)
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
        if (!TryCreateKey(candidate, out var key))
          throw new InvalidOperationException("Candidate identifier is null, empty, or whitespace. LoadCandidates must succeed before approval.");
        if (!approved.Contains(key)) continue;
        var existingIndex = declarations.FindIndex(value => value.Selector.Kind == key.Kind
          && string.Equals(value.Selector.Identifier.Trim(), key.Identifier, StringComparison.Ordinal)
          && value.OccurrenceSelector == null);
        var existing = existingIndex >= 0 ? declarations[existingIndex] : null;
        if (candidate.SuggestedBinding != null
            && candidate.SuggestedBinding.Mode == ScenarioRequirementBindingMode.RegistryProvided)
          throw new InvalidOperationException($"Candidate '{key}' cannot be approved with RegistryProvided binding because candidates do not declare providerIdentifier.");
        // An approved Generated/Prefab candidate must resolve to a known factory
        // (data contract §3, §10.5).  Do not trust the caller's approvedKeys to
        // have consulted the preview; re-validate the factory here so an unknown
        // or null factory cannot be written into the canonical sidecar.
        if (candidate.SuggestedBinding != null
            && (candidate.SuggestedBinding.Mode == ScenarioRequirementBindingMode.GeneratedSceneObject
                || candidate.SuggestedBinding.Mode == ScenarioRequirementBindingMode.PrefabInstance))
        {
          var factory = candidate.SuggestedBinding.FactoryIdentifier;
          if (string.IsNullOrWhiteSpace(factory))
            throw new InvalidOperationException($"Candidate '{key}' cannot be approved: {candidate.SuggestedBinding.Mode} binding requires a factoryIdentifier.");
          if (allowedFactoryIdentifiers == null || !allowedFactoryIdentifiers.Contains(factory))
            throw new InvalidOperationException($"Candidate '{key}' cannot be approved: unknown factory '{factory}'.");
        }
        var applyCandidateFields = existing == null;
        var approvedDeclaration = new ScenarioRequirementDeclarationDTO
        {
          Selector = new ScenarioRequirementSelectorDTO { Kind = key.Kind, Identifier = key.Identifier },
          Operation = existing?.Operation ?? (inferredManifest.Requirements.Any(value => value.Key.Equals(key))
            ? ScenarioRequirementDeclarationOperation.Override : ScenarioRequirementDeclarationOperation.Declare),
          // Approval may introduce a new declaration, but it must never
          // silently rewrite an already canonical declaration.
          Capabilities = applyCandidateFields ? (candidate.Capabilities ?? new List<ScenarioRequirementCapability>()).Distinct().OrderBy(value => value).ToList() : (existing.Capabilities ?? new List<ScenarioRequirementCapability>()).ToList(),
          Scope = applyCandidateFields ? candidate.SuggestedBinding?.Scope : existing.Scope,
          Authority = applyCandidateFields ? candidate.SuggestedBinding?.Authority : existing.Authority,
          Availability = existing?.Availability,
          Cardinality = existing?.Cardinality,
          MustProve = existing?.MustProve,
          Binding = !applyCandidateFields ? existing.Binding : candidate.SuggestedBinding == null ? null : new ScenarioRequirementBindingDTO
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
      var candidateIndex = 0;
      foreach (var candidate in document.DTO.Candidates ?? new List<ScenarioRequirementCandidateDTO>())
      {
        if (!TryCreateKey(candidate, out var candidateKey))
        {
          candidateIndex++;
          continue;
        }
        if (!seenKeys.Add(candidateKey)) duplicateKeys.Add(candidateKey);
        candidateIndex++;
      }
      candidateIndex = 0;
      foreach (var dto in document.DTO.Candidates ?? new List<ScenarioRequirementCandidateDTO>())
      {
        var hasValidKey = TryCreateKey(dto, out var key);
        if (!hasValidKey) key = new ScenarioRequirementKey(ScenarioRequirementKind.RuntimeSignal, "invalid-candidate-" + candidateIndex);
        var diagnostics = new List<ScenarioRequirementDiagnostic>();
        if (!hasValidKey)
        {
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR700", "InvalidCandidateIdentifier", "Candidate identifier is null, empty, or whitespace.", key));
          result.Add(new ScenarioRequirementCandidatePreview(key, ScenarioRequirementCandidateDisposition.Invalid, ScenarioRequirementDeclarationOperation.Declare, 0d, diagnostics));
          candidateIndex++;
          continue;
        }
        if (!string.Equals(document.ScenarioIdentifier, graph.Identifier, StringComparison.Ordinal)
            || !string.Equals(document.ScenarioIdentifier, inferredManifest.ScenarioIdentifier, StringComparison.Ordinal))
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR106", "ScenarioIdentifierMismatch", "Candidate scenarioIdentifier does not match the graph.", key));
        if (duplicateKeys.Contains(key))
          diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR703", "DuplicateCandidate", $"Duplicate candidate '{key}'.", key));
        var existing = inferredManifest.Requirements.FirstOrDefault(value => value.Key.Equals(key));
        foreach (var evidence in dto.Evidence ?? new List<ScenarioRequirementEvidenceDTO>())
        {
          // Data contract §3 requires only that evidence matches the scenario
          // graph: the node exists, its nodeType matches, and the field is a
          // canonical external-lookup field.  Do NOT additionally require the
          // evidence to appear in the inferred occurrence set, which would
          // reject legitimate override candidates whose real graph field simply
          // was not captured as an occurrence.
          if (!graph.TryGetNode(evidence.NodeIdentifier, out var node)
              || !string.Equals(node.NodeType.ToString(), evidence.NodeType, StringComparison.Ordinal)
              || !HasCanonicalLookupField(node, evidence.FieldPath))
          {
            diagnostics.Add(ScenarioRequirementsLoader.Diagnostic("SIR701", "CandidateEvidenceMismatch",
              $"Candidate '{key}' evidence '{evidence.NodeIdentifier}:{evidence.FieldPath}' does not match the scenario graph.", key));
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
        candidateIndex++;
      }
      return new ScenarioRequirementCandidatePreviewResult(result);
    }

    private static bool HasCanonicalLookupField(IScenarioNode node, string fieldPath)
    {
      if (node == null || string.IsNullOrWhiteSpace(fieldPath)
          || !ScenarioNodeRuntimeLookupRegistry.TryGet(node.NodeType, out var registration))
        return false;
      return registration.Lookups.Any(lookup => lookup.Classification == ScenarioRuntimeLookupClassification.External
                                                && string.Equals(lookup.FieldPath, fieldPath, StringComparison.Ordinal));
    }

    private static bool TryCreateKey(ScenarioRequirementCandidateDTO candidate, out ScenarioRequirementKey key)
    {
      key = default;
      if (candidate == null || string.IsNullOrWhiteSpace(candidate.Identifier)) return false;
      key = new ScenarioRequirementKey(candidate.Kind, candidate.Identifier.Trim());
      return true;
    }
  }
}
