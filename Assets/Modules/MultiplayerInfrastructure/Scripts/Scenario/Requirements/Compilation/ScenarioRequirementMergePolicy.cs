using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public sealed class ScenarioRequirementCompilationContext
  {
    public DateTime TodayUtc { get; }
    public ScenarioRequirementCompilationContext(DateTime todayUtc) => TodayUtc = todayUtc.Date;
  }

  internal static class ScenarioRequirementMergePolicy
  {
    private sealed class MutableDescriptor
    {
      public ScenarioRequirementKey Key;
      public ScenarioRequirementScope Scope;
      public ScenarioRequirementAuthority Authority;
      public int Minimum;
      public int? Maximum;
      public bool CardinalityValidated;
      public readonly HashSet<ScenarioRequirementCapability> Capabilities = new HashSet<ScenarioRequirementCapability>();
      public List<ScenarioRequirementOccurrence> Occurrences = new List<ScenarioRequirementOccurrence>();
      public ScenarioRequirementBindingHint Binding;
      public ScenarioRequirementAvailability? DeclaredAvailability;
      public bool MustProve;
      public ScenarioRequirementSuppression Suppression;
      public readonly List<ScenarioRequirementDeclarationSource> Sources = new List<ScenarioRequirementDeclarationSource>();
    }

    public static ScenarioRequirementManifest Merge(
      ScenarioRequirementManifest inferred,
      string sourceHash,
      ScenarioRequirementsDocument document,
      ScenarioRequirementCompilationContext context)
    {
      var diagnostics = inferred.Diagnostics.ToList();
      var descriptors = inferred.Requirements.ToDictionary(value => value.Key, FromDescriptor);
      if (document == null) return Build(inferred.ScenarioIdentifier, sourceHash, descriptors.Values, diagnostics);

      if (!string.Equals(document.ScenarioIdentifier, inferred.ScenarioIdentifier, StringComparison.Ordinal))
        diagnostics.Add(Error("SIR106", "ScenarioIdentifierMismatch", $"Sidecar scenarioIdentifier '{document.ScenarioIdentifier}' does not match graph '{inferred.ScenarioIdentifier}'."));
      if (!string.Equals(document.ScenarioSha256, sourceHash, StringComparison.Ordinal))
        diagnostics.Add(Warning("SIR201", "StaleDeclarationSource", "Sidecar source hash does not match the scenario source bytes."));

      var declarations = document.DTO.Declarations ?? new List<ScenarioRequirementDeclarationDTO>();
      var duplicateGroups = declarations.Select((value, index) => new { value, index })
        .GroupBy(value => DeclarationIdentity(value.value), StringComparer.Ordinal)
        .Where(group => group.Count() > 1).ToArray();
      var duplicateIndexes = new HashSet<int>(duplicateGroups.SelectMany(group => group.Select(value => value.index)));
      foreach (var group in duplicateGroups)
        diagnostics.Add(Error("SIR203", "DuplicateDeclarationSelector", $"Duplicate declaration selector '{group.Key}'."));

      foreach (var indexed in declarations.Select((value, index) => new { value, index })
                 .OrderBy(item => item.value.Operation == ScenarioRequirementDeclarationOperation.Declare ? 0 : 1)
                 .ThenBy(item => item.index))
      {
        var dto = indexed.value;
        if (dto?.Selector == null || duplicateIndexes.Contains(indexed.index)) continue;
        var key = new ScenarioRequirementKey(dto.Selector.Kind, dto.Selector.Identifier.Trim());
        if (dto.Operation == ScenarioRequirementDeclarationOperation.Declare)
        {
          if (descriptors.ContainsKey(key))
          {
            diagnostics.Add(Error("SIR211", "DeclareConflictsWithInferred", $"Declare targets existing inferred requirement '{key}'.", key));
            continue;
          }
          descriptors.Add(key, CreateDeclared(key));
        }
        else if (!descriptors.TryGetValue(key, out _))
        {
          diagnostics.Add(Error("SIR202", "OrphanDeclaration", $"Override target '{key}' does not exist.", key));
          continue;
        }

        var target = descriptors[key];
        if (dto.OccurrenceSelector != null && !target.Occurrences.Any(value =>
              string.Equals(value.NodeIdentifier, dto.OccurrenceSelector.NodeIdentifier, StringComparison.Ordinal)
              && string.Equals(value.FieldPath, dto.OccurrenceSelector.FieldPath, StringComparison.Ordinal)))
        {
          diagnostics.Add(Error("SIR202", "OrphanDeclaration", $"Occurrence selector for '{key}' does not match inferred sources.", key));
          continue;
        }
        Apply(target, dto, indexed.index, diagnostics);
      }

      ApplySuppressions(document.DTO.Suppressions, descriptors, context ?? new ScenarioRequirementCompilationContext(DateTime.UtcNow), diagnostics);
      return Build(inferred.ScenarioIdentifier, sourceHash, descriptors.Values, diagnostics);
    }

    private static void Apply(MutableDescriptor target, ScenarioRequirementDeclarationDTO dto, int index, List<ScenarioRequirementDiagnostic> diagnostics)
    {
      if (dto.Scope.HasValue)
      {
        if (target.Scope != ScenarioRequirementScope.AnyLoadedScene && dto.Scope != ScenarioRequirementScope.AnyLoadedScene && target.Scope != dto.Scope)
          diagnostics.Add(Error("SIR207", "ScopeConflict", $"Requirement '{target.Key}' has conflicting scopes.", target.Key));
        else if (dto.Scope != ScenarioRequirementScope.AnyLoadedScene) target.Scope = dto.Scope.Value;
      }
      if (dto.Authority.HasValue)
      {
        if (target.Authority != ScenarioRequirementAuthority.Any && dto.Authority != ScenarioRequirementAuthority.Any && target.Authority != dto.Authority)
          diagnostics.Add(Error("SIR206", "AuthorityConflict", $"Requirement '{target.Key}' has conflicting authorities.", target.Key));
        else if (dto.Authority != ScenarioRequirementAuthority.Any) target.Authority = dto.Authority.Value;
      }
      if (dto.Cardinality != null)
      {
        var minimum = Math.Max(target.Minimum, dto.Cardinality.Minimum ?? 0);
        var maximum = Min(target.Maximum, dto.Cardinality.Maximum);
        if (maximum.HasValue && minimum > maximum.Value)
          diagnostics.Add(Error("SIR204", "ImpossibleCardinality", $"Requirement '{target.Key}' cardinality intersection is impossible.", target.Key));
        else { target.Minimum = minimum; target.Maximum = maximum; target.CardinalityValidated = true; }
      }
      if (dto.Availability.HasValue)
      {
        var selected = target.Occurrences.Where(value => value.Direction == ScenarioRequirementDirection.Consumes
          && (dto.OccurrenceSelector == null || value.NodeIdentifier == dto.OccurrenceSelector.NodeIdentifier && value.FieldPath == dto.OccurrenceSelector.FieldPath)).ToArray();
        if (selected.Length == 0 && dto.Operation == ScenarioRequirementDeclarationOperation.Declare)
          target.DeclaredAvailability = dto.Availability.Value;
        else if (selected.Any(value => dto.Availability.Value > value.Availability))
          diagnostics.Add(Error("SIR209", "AvailabilityWeakening", $"Override weakens inferred availability for '{target.Key}'.", target.Key));
        else target.Occurrences = target.Occurrences.Select(value => selected.Contains(value) ? value.WithAvailability(dto.Availability.Value) : value).ToList();
      }
      foreach (var capability in dto.Capabilities ?? new List<ScenarioRequirementCapability>()) target.Capabilities.Add(capability);
      target.MustProve |= dto.MustProve ?? false;
      if (dto.Binding != null)
      {
        var binding = new ScenarioRequirementBindingHint(dto.Binding.Mode, dto.Binding.FactoryIdentifier, dto.Binding.ProviderIdentifier);
        if (target.Binding != null && (target.Binding.Mode != binding.Mode || target.Binding.FactoryIdentifier != binding.FactoryIdentifier || target.Binding.ProviderIdentifier != binding.ProviderIdentifier))
          diagnostics.Add(Error("SIR208", "BindingConflict", $"Requirement '{target.Key}' has conflicting bindings.", target.Key));
        else target.Binding = binding;
      }
      target.Sources.Add(new ScenarioRequirementDeclarationSource(
        dto.Operation == ScenarioRequirementDeclarationOperation.Declare ? ScenarioRequirementSourceOrigin.Declared : ScenarioRequirementSourceOrigin.Override,
        dto.Operation, dto.OccurrenceSelector?.NodeIdentifier, dto.OccurrenceSelector?.FieldPath, index));
    }

    private static void ApplySuppressions(List<ScenarioRequirementSuppressionDTO> suppressions, Dictionary<ScenarioRequirementKey, MutableDescriptor> descriptors, ScenarioRequirementCompilationContext context, List<ScenarioRequirementDiagnostic> diagnostics)
    {
      foreach (var dto in suppressions ?? new List<ScenarioRequirementSuppressionDTO>())
      {
        if (dto?.Selector == null) continue;
        var key = new ScenarioRequirementKey(dto.Selector.Kind, dto.Selector.Identifier.Trim());
        if (!descriptors.TryGetValue(key, out var target)) { diagnostics.Add(Error("SIR212", "OrphanSuppression", $"Suppression target '{key}' does not exist.", key)); continue; }
        if (string.IsNullOrWhiteSpace(dto.Owner))
        { diagnostics.Add(Error("SIR210", "MalformedSuppression", $"Suppression for '{key}' must declare an owner.", key)); continue; }
        if (diagnostics.Any(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error
                                     && ((value.HasRequirementKey && value.RequirementKey.Equals(key))
                                         || value.Code == "SIR203" || value.Code == "SIR105" || value.Code == "SIR104")))
        {
          diagnostics.Add(Error("SIR213", "SuppressionCannotMaskInvalidDeclaration", $"Suppression for '{key}' cannot mask malformed or duplicate declarations.", key));
          continue;
        }
        if (!DateTime.TryParseExact(dto.ExpiresOn, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expires))
        { diagnostics.Add(Error("SIR210", "MalformedSuppression", $"Suppression for '{key}' has invalid expiry.", key)); continue; }
        if (expires.Date < context.TodayUtc) { diagnostics.Add(Error("SIR205", "ExpiredSuppression", $"Suppression for '{key}' expired on {dto.ExpiresOn}.", key)); continue; }
        target.Suppression = new ScenarioRequirementSuppression(dto.Reason.Trim(), dto.Owner.Trim(), expires);
      }
    }

    private static ScenarioRequirementManifest Build(string scenarioIdentifier, string hash, IEnumerable<MutableDescriptor> values, IEnumerable<ScenarioRequirementDiagnostic> diagnostics)
    {
      var descriptors = values.OrderBy(value => value.Key).Select(value => new ScenarioRequirementDescriptor(
        value.Key, value.Scope, value.Authority,
        new ScenarioRequirementCardinality(value.Minimum, value.Maximum, value.CardinalityValidated),
        value.DeclaredAvailability ?? EffectiveAvailability(value.Occurrences), value.Capabilities, value.Occurrences.OrderBy(item => item.StableIdentity, StringComparer.Ordinal),
        value.Binding, value.MustProve, value.Suppression, value.Sources)).ToArray();
      return new ScenarioRequirementManifest(scenarioIdentifier, hash, descriptors, ScenarioRequirementsLoader.Sort(diagnostics));
    }

    private static MutableDescriptor FromDescriptor(ScenarioRequirementDescriptor value) => new MutableDescriptor { Key = value.Key, Scope = value.Scope, Authority = value.Authority, Minimum = value.Cardinality.Minimum, Maximum = value.Cardinality.Maximum, CardinalityValidated = value.Cardinality.IsValidated, Binding = value.BindingHint, MustProve = value.MustProve, Suppression = value.Suppression, Occurrences = value.Occurrences.ToList() }.Also(result => { foreach (var capability in value.Capabilities) result.Capabilities.Add(capability); foreach (var source in value.DeclarationSources) result.Sources.Add(source); });
    private static MutableDescriptor CreateDeclared(ScenarioRequirementKey key) => new MutableDescriptor { Key = key, Scope = ScenarioRequirementScope.AnyLoadedScene, Authority = ScenarioRequirementAuthority.Any, Minimum = DefaultCardinality(key.Kind).Minimum, Maximum = DefaultCardinality(key.Kind).Maximum, CardinalityValidated = DefaultCardinality(key.Kind).IsValidated };
    private static ScenarioRequirementCardinality DefaultCardinality(ScenarioRequirementKind kind) => kind == ScenarioRequirementKind.EventHandler ? ScenarioRequirementCardinality.AtLeastOne : kind == ScenarioRequirementKind.RuntimeSignal || kind == ScenarioRequirementKind.PlayerTagState || kind == ScenarioRequirementKind.RuntimeEntityReference ? ScenarioRequirementCardinality.NotValidated : ScenarioRequirementCardinality.ExactlyOne;
    private static int? Min(int? left, int? right) => !left.HasValue ? right : !right.HasValue ? left : Math.Min(left.Value, right.Value);
    private static ScenarioRequirementAvailability EffectiveAvailability(IEnumerable<ScenarioRequirementOccurrence> values) { var consumers = values.Where(value => value.Direction == ScenarioRequirementDirection.Consumes).ToArray(); return consumers.Length == 0 ? ScenarioRequirementAvailability.NotConsumed : consumers.Min(value => value.Availability); }
    private static string DeclarationIdentity(ScenarioRequirementDeclarationDTO dto) => dto == null ? string.Empty : $"{dto.Selector?.Kind}:{dto.Selector?.Identifier?.Trim()}:{dto.Operation}:{dto.OccurrenceSelector?.NodeIdentifier}:{dto.OccurrenceSelector?.FieldPath}";
    private static ScenarioRequirementDiagnostic Error(string code, string name, string message, ScenarioRequirementKey? key = null) => ScenarioRequirementsLoader.Diagnostic(code, name, message, key);
    private static ScenarioRequirementDiagnostic Warning(string code, string name, string message) => new ScenarioRequirementDiagnostic(code, name, ScenarioRequirementDiagnosticSeverity.Warning, message, null, string.Empty, string.Empty);

    private static T Also<T>(this T value, Action<T> action) { action(value); return value; }
  }
}
