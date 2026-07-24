using System;
using System.Collections.Generic;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioRequirementValidationEngine
  {
    public static ScenarioRequirementValidationReport ValidateRuntime(ScenarioRequirementManifest manifest, ScenarioRequirementProviderSnapshot snapshot, ScenarioRequirementSceneComposition composition)
    {
      // Scene assets cannot prove a fixed network authority, but runtime is
      // already executing in a concrete authority context.  The caller filters
      // non-applicable requirements; applicable ones must be checked against
      // the live provider snapshot rather than downgraded to Indeterminate.
      // At runtime, scene-role scope can only be proven when a composition
      // profile with scene roles is available.  When it is not (the common
      // StartScenario path passes no composition), every provider is tagged
      // AnyLoadedScene, so a fixed-scope requirement must NOT be reported as
      // WrongScene; its scope is simply unprovable and becomes Indeterminate.
      var proveSceneScope = composition != null && composition.Scenes.Count > 0;
      var report = ValidateInternal(manifest, snapshot, composition, proveFixedAuthority: true, proveSceneScope: proveSceneScope);
      var results = report.Results.Select(result =>
        result.Status == ScenarioRequirementValidationStatus.Inactive
          ? new ScenarioRequirementValidationResult(result.Requirement, ScenarioRequirementValidationStatus.NotReady, result.Providers, result.Diagnostics.Concat(new[] { new ScenarioRequirementValidationDiagnostic("SIR413", "ProviderNotReady", ScenarioRequirementDiagnosticSeverity.Warning, "Runtime provider is not active or enabled.", result.Requirement.Key) }))
          : result).ToArray();
      return new ScenarioRequirementValidationReport(manifest.ScenarioIdentifier, results, report.Diagnostics);
    }
    public static ScenarioRequirementValidationReport Validate(ScenarioRequirementManifest manifest, ScenarioRequirementProviderSnapshot snapshot, ScenarioRequirementSceneComposition composition)
      => ValidateInternal(manifest, snapshot, composition, proveFixedAuthority: false, proveSceneScope: true);

    private static ScenarioRequirementValidationReport ValidateInternal(ScenarioRequirementManifest manifest, ScenarioRequirementProviderSnapshot snapshot, ScenarioRequirementSceneComposition composition, bool proveFixedAuthority, bool proveSceneScope)
    {
      if (manifest == null) throw new ArgumentNullException(nameof(manifest));
      if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
      if (composition == null) throw new ArgumentNullException(nameof(composition));
      var results = new List<ScenarioRequirementValidationResult>();
      var diagnostics = new List<ScenarioRequirementValidationDiagnostic>(snapshot.Diagnostics);
      foreach (var requirement in manifest.Requirements)
      {
        var result = ValidateRequirement(requirement, snapshot, composition, proveFixedAuthority, proveSceneScope);
        results.Add(result);
        diagnostics.AddRange(result.Diagnostics);
      }
      return new ScenarioRequirementValidationReport(manifest.ScenarioIdentifier, results, diagnostics);
    }

    private static ScenarioRequirementValidationResult ValidateRequirement(ScenarioRequirementDescriptor requirement, ScenarioRequirementProviderSnapshot snapshot, ScenarioRequirementSceneComposition composition, bool proveFixedAuthority, bool proveSceneScope)
    {
      if (requirement.IsSuppressed && requirement.Suppression.ExpiresOnUtc >= DateTime.UtcNow.Date)
        return Result(requirement, ScenarioRequirementValidationStatus.Suppressed);
      if (requirement.EffectiveAvailability == ScenarioRequirementAvailability.NotConsumed) return Result(requirement, ScenarioRequirementValidationStatus.NotConsumed);
      if (requirement.Occurrences.Any(value =>
            value.Direction == ScenarioRequirementDirection.Produces
            && value.ExpectedSupply == ScenarioRequirementExpectedSupply.Scenario))
      {
        return Result(requirement, ScenarioRequirementValidationStatus.Satisfied);
      }
      if (requirement.EffectiveAvailability == ScenarioRequirementAvailability.OptionalFallback && requirement.Cardinality.Minimum == 0)
      {
        var optionalMatches = snapshot.Providers.Where(value => value.Key.Equals(requirement.Key)).ToArray();
        if (optionalMatches.Length == 0) return Result(requirement, ScenarioRequirementValidationStatus.Indeterminate);
      }
      if (requirement.Occurrences.Count > 0 && requirement.Occurrences.All(value => value.Direction == ScenarioRequirementDirection.Produces)) return Indeterminate(requirement, "SIR410", "Producer-only requirement is not scene-cardinality validated.");
      if (requirement.Scope == ScenarioRequirementScope.DontDestroyOnLoad) return Indeterminate(requirement, "SIR311", "DontDestroyOnLoad scope cannot be proven from scene assets.");
      if (!proveFixedAuthority && requirement.Authority != ScenarioRequirementAuthority.Any)
        return Indeterminate(requirement, "SIR411", "Fixed network authority cannot be proven from scene evidence.");
      // A fixed scene-role scope cannot be judged without a composition profile
      // that maps scenes to roles.  Reporting WrongScene here would falsely fail
      // every scope-fixed requirement whenever the runtime path has no
      // composition (proposal §15); report it as unprovable instead.
      if (!proveSceneScope && requirement.Scope != ScenarioRequirementScope.AnyLoadedScene)
        return Indeterminate(requirement, "SIR315", "Scene-role scope cannot be proven without a composition profile.");

      var allKeyMatches = snapshot.Providers.Where(value => value.Key.Equals(requirement.Key)).ToArray();
      var inScope = allKeyMatches.Where(value => IsInScope(requirement.Scope, value.SceneRole)).ToArray();
      if (allKeyMatches.Length > 0 && inScope.Length == 0)
        return Result(requirement, ScenarioRequirementValidationStatus.WrongScene, allKeyMatches,
          Diagnostic("SIR310", "WrongSceneRole", ScenarioRequirementDiagnosticSeverity.Warning, "Matching provider exists only in a different scene role.", requirement.Key));

      var capable = inScope.Where(value => requirement.Capabilities.All(value.Capabilities.Contains)).ToArray();
      if (inScope.Length > 0 && capable.Length == 0)
        return Result(requirement, ScenarioRequirementValidationStatus.MissingCapability, inScope,
          Diagnostic("SIR403", "MissingProviderCapability", ScenarioRequirementDiagnosticSeverity.Error, "No single physical provider supplies every required capability.", requirement.Key));

      var ready = capable.Where(value => value.IsActive && value.IsComponentEnabled && value.IsExpectedToRegister).GroupBy(value => value.PhysicalProviderIdentifier, StringComparer.Ordinal).Select(value => value.First()).ToArray();
      if (capable.Length > 0 && ready.Length == 0)
        return Result(requirement, ScenarioRequirementValidationStatus.Inactive, capable,
          Diagnostic("SIR404", "InactiveProvider", ScenarioRequirementDiagnosticSeverity.Warning, "Matching provider is inactive or disabled.", requirement.Key));

      if (!snapshot.Completeness.TryGetValue(requirement.Kind, out var completeness) || completeness != ScenarioRequirementEvidenceCompleteness.Complete)
      {
        // An incomplete source cannot prove that distinct provider identities are
        // distinct physical objects.  In particular, runtime registry and scene
        // evidence may describe the same object with unrelated identifiers.
        if (ready.Length == 0 || (requirement.Cardinality.Maximum.HasValue && ready.Length > requirement.Cardinality.Maximum.Value))
          return Indeterminate(requirement, "SIR412", "Provider evidence is incomplete for this requirement kind.", ready);
      }
      if (requirement.Cardinality.Maximum.HasValue && ready.Length > requirement.Cardinality.Maximum.Value)
        return Result(requirement, ScenarioRequirementValidationStatus.Duplicate, ready,
          Diagnostic("SIR401", "DuplicatePhysicalProvider", ScenarioRequirementDiagnosticSeverity.Error, $"Expected at most {requirement.Cardinality.Maximum.Value} provider(s), found {ready.Length}.", requirement.Key));
      if (!requirement.Cardinality.IsValidated) return Indeterminate(requirement, "SIR410", "Cardinality is not statically validated.", ready);
      if (ready.Length < requirement.Cardinality.Minimum)
        return Result(requirement, ScenarioRequirementValidationStatus.Missing, ready,
          Diagnostic("SIR400", "MissingProvider", ScenarioRequirementDiagnosticSeverity.Error, $"Expected at least {requirement.Cardinality.Minimum} provider(s), found {ready.Length}.", requirement.Key));
      return Result(requirement, ScenarioRequirementValidationStatus.Satisfied, ready);
    }

    private static bool IsInScope(ScenarioRequirementScope requirement, ScenarioRequirementScope provider)
      => requirement == ScenarioRequirementScope.AnyLoadedScene || requirement == provider;
    private static ScenarioRequirementValidationResult Indeterminate(ScenarioRequirementDescriptor requirement, string code, string message, IEnumerable<ScenarioRequirementProvider> providers = null)
      => Result(requirement, ScenarioRequirementValidationStatus.Indeterminate, providers, Diagnostic(code, "Indeterminate", ScenarioRequirementDiagnosticSeverity.Info, message, requirement.Key));
    private static ScenarioRequirementValidationResult Result(ScenarioRequirementDescriptor requirement, ScenarioRequirementValidationStatus status, IEnumerable<ScenarioRequirementProvider> providers = null, params ScenarioRequirementValidationDiagnostic[] diagnostics)
      => new ScenarioRequirementValidationResult(requirement, status, providers, diagnostics);
    private static ScenarioRequirementValidationDiagnostic Diagnostic(string code, string name, ScenarioRequirementDiagnosticSeverity severity, string message, ScenarioRequirementKey key)
      => new ScenarioRequirementValidationDiagnostic(code, name, severity, message, key);
  }
}
