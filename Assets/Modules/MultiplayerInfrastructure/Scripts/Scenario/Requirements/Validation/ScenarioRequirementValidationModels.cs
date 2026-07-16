using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public enum ScenarioRequirementValidationStatus { Satisfied, Missing, Duplicate, WrongType, MissingCapability, WrongScene, Inactive, NotRegistered, NotReady, Indeterminate, Suppressed, Malformed, Stale, NotConsumed }
  public enum ScenarioRequirementEvidenceCompleteness { Complete, Partial, Unsupported }
  public enum ScenarioRequirementProviderOrigin { SceneComponent, SceneBinding, RegistryPreloaderDeclaration, ProjectContributor }

  public sealed class ScenarioRequirementCompositionScene
  {
    public string ScenePath { get; }
    public string SceneGuid { get; }
    public ScenarioRequirementScope Role { get; }
    public ScenarioRequirementCompositionScene(string scenePath, string sceneGuid, ScenarioRequirementScope role) { ScenePath = scenePath ?? string.Empty; SceneGuid = sceneGuid ?? string.Empty; Role = role; }
  }

  public sealed class ScenarioRequirementSceneComposition
  {
    public string Identifier { get; }
    public IReadOnlyList<ScenarioRequirementCompositionScene> Scenes { get; }
    public ScenarioRequirementSceneComposition(string identifier, IEnumerable<ScenarioRequirementCompositionScene> scenes) { Identifier = identifier ?? string.Empty; Scenes = new ReadOnlyCollection<ScenarioRequirementCompositionScene>((scenes ?? Array.Empty<ScenarioRequirementCompositionScene>()).OrderBy(value => value.ScenePath, StringComparer.Ordinal).ToArray()); }
  }

  public sealed class ScenarioRequirementProvider
  {
    public string PhysicalProviderIdentifier { get; }
    public ScenarioRequirementKey Key { get; }
    public string ScenePath { get; }
    public ScenarioRequirementScope SceneRole { get; }
    public string HierarchyPath { get; }
    public IReadOnlyList<ScenarioRequirementCapability> Capabilities { get; }
    public bool IsActive { get; }
    public bool IsComponentEnabled { get; }
    public bool IsExpectedToRegister { get; }
    public ScenarioRequirementProviderOrigin Origin { get; }

    public ScenarioRequirementProvider(string physicalProviderIdentifier, ScenarioRequirementKey key, string scenePath, ScenarioRequirementScope sceneRole, string hierarchyPath, IEnumerable<ScenarioRequirementCapability> capabilities, bool isActive, bool isComponentEnabled, bool isExpectedToRegister, ScenarioRequirementProviderOrigin origin)
    { PhysicalProviderIdentifier = physicalProviderIdentifier ?? string.Empty; Key = key; ScenePath = scenePath ?? string.Empty; SceneRole = sceneRole; HierarchyPath = hierarchyPath ?? string.Empty; Capabilities = Array.AsReadOnly((capabilities ?? Array.Empty<ScenarioRequirementCapability>()).Distinct().OrderBy(value => value).ToArray()); IsActive = isActive; IsComponentEnabled = isComponentEnabled; IsExpectedToRegister = isExpectedToRegister; Origin = origin; }
  }

  public sealed class ScenarioRequirementValidationDiagnostic
  {
    public string Code { get; }
    public string Name { get; }
    public ScenarioRequirementDiagnosticSeverity Severity { get; }
    public string Message { get; }
    public ScenarioRequirementKey? RequirementKey { get; }
    public string ScenePath { get; }
    public string PhysicalProviderIdentifier { get; }
    public string FixHint { get; }
    public ScenarioRequirementValidationDiagnostic(string code, string name, ScenarioRequirementDiagnosticSeverity severity, string message, ScenarioRequirementKey? requirementKey = null, string scenePath = null, string physicalProviderIdentifier = null, string fixHint = null)
    { Code = code; Name = name; Severity = severity; Message = message; RequirementKey = requirementKey; ScenePath = scenePath ?? string.Empty; PhysicalProviderIdentifier = physicalProviderIdentifier ?? string.Empty; FixHint = fixHint ?? string.Empty; }
  }

  public sealed class ScenarioRequirementProviderSnapshot
  {
    public IReadOnlyList<ScenarioRequirementProvider> Providers { get; }
    public IReadOnlyList<ScenarioRequirementValidationDiagnostic> Diagnostics { get; }
    public IReadOnlyDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> Completeness { get; }
    public ScenarioRequirementProviderSnapshot(IEnumerable<ScenarioRequirementProvider> providers, IEnumerable<ScenarioRequirementValidationDiagnostic> diagnostics, IDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness> completeness)
    { Providers = new ReadOnlyCollection<ScenarioRequirementProvider>((providers ?? Array.Empty<ScenarioRequirementProvider>()).OrderBy(value => value.Key).ThenBy(value => value.PhysicalProviderIdentifier, StringComparer.Ordinal).ToArray()); Diagnostics = new ReadOnlyCollection<ScenarioRequirementValidationDiagnostic>((diagnostics ?? Array.Empty<ScenarioRequirementValidationDiagnostic>()).OrderBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.ScenePath, StringComparer.Ordinal).ThenBy(value => value.PhysicalProviderIdentifier, StringComparer.Ordinal).ToArray()); Completeness = new ReadOnlyDictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>(new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>(completeness ?? new Dictionary<ScenarioRequirementKind, ScenarioRequirementEvidenceCompleteness>())); }
  }

  public sealed class ScenarioRequirementValidationResult
  {
    public ScenarioRequirementDescriptor Requirement { get; }
    public ScenarioRequirementValidationStatus Status { get; }
    public IReadOnlyList<ScenarioRequirementProvider> Providers { get; }
    public IReadOnlyList<ScenarioRequirementValidationDiagnostic> Diagnostics { get; }
    public ScenarioRequirementValidationResult(ScenarioRequirementDescriptor requirement, ScenarioRequirementValidationStatus status, IEnumerable<ScenarioRequirementProvider> providers, IEnumerable<ScenarioRequirementValidationDiagnostic> diagnostics)
    { Requirement = requirement; Status = status; Providers = Array.AsReadOnly((providers ?? Array.Empty<ScenarioRequirementProvider>()).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<ScenarioRequirementValidationDiagnostic>()).ToArray()); }
  }

  public sealed class ScenarioRequirementValidationReport
  {
    public string ScenarioIdentifier { get; }
    public IReadOnlyList<ScenarioRequirementValidationResult> Results { get; }
    public IReadOnlyList<ScenarioRequirementValidationDiagnostic> Diagnostics { get; }
    public bool HasErrors => Diagnostics.Any(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Error);
    public ScenarioRequirementValidationReport(string scenarioIdentifier, IEnumerable<ScenarioRequirementValidationResult> results, IEnumerable<ScenarioRequirementValidationDiagnostic> diagnostics)
    { ScenarioIdentifier = scenarioIdentifier ?? string.Empty; Results = Array.AsReadOnly((results ?? Array.Empty<ScenarioRequirementValidationResult>()).OrderBy(value => value.Requirement.Key).ToArray()); Diagnostics = Array.AsReadOnly((diagnostics ?? Array.Empty<ScenarioRequirementValidationDiagnostic>()).OrderBy(value => value.RequirementKey?.ToString() ?? string.Empty, StringComparer.Ordinal).ThenBy(value => value.Code, StringComparer.Ordinal).ThenBy(value => value.ScenePath, StringComparer.Ordinal).ThenBy(value => value.PhysicalProviderIdentifier, StringComparer.Ordinal).ToArray()); }
  }
}
