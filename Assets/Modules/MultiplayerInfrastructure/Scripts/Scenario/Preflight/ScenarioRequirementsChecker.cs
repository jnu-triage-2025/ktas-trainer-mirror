using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario.Requirements;
using Canonical = MultiplayerInfrastructure.Scenario.Requirements;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario.Preflight
{
  /// <summary>
  /// 수집된 요구 항목을 레지스트리/씬 기준으로 검사한 결과.
  /// </summary>
  public sealed class ScenarioPreflightReport
  {
    public IReadOnlyList<ScenarioRequirement> Requirements { get; }
    public int MissingCount { get; }
    public int IndeterminateCount { get; }
    public int BlockingCount { get; }
    public ScenarioRequirementValidationReport CanonicalReport { get; }

    public bool HasMissing => MissingCount > 0;

    public ScenarioPreflightReport(IReadOnlyList<ScenarioRequirement> requirements, int missingCount, int indeterminateCount, int blockingCount = 0, ScenarioRequirementValidationReport canonicalReport = null)
    {
      Requirements = requirements;
      MissingCount = missingCount;
      IndeterminateCount = indeterminateCount;
      BlockingCount = blockingCount;
      CanonicalReport = canonicalReport;
    }
  }

  /// <summary>
  /// 요구 항목 목록을 검사해 충족/누락/불확실로 판정하고, 요약 리포트를 만든다.
  /// 신호(sig.*)는 정적으로 판정할 수 없으므로 항상 Indeterminate 로 둔다(누락 집계 제외).
  /// </summary>
  public static class ScenarioRequirementsChecker
  {
    public static ScenarioPreflightReport CheckCanonical(ScenarioRequirementManifest manifest, ScenarioRequirementProviderSnapshot snapshot, ScenarioRequirementSceneComposition composition)
    {
      var canonical = ScenarioRequirementValidationEngine.Validate(manifest, snapshot, composition);
      var projected = new List<ScenarioRequirement>();
      var missing = 0;
      var indeterminate = 0;
      var blocking = 0;
      foreach (var result in canonical.Results)
      {
        var status = MapStatus(result.Status);
        var requirement = new ScenarioRequirement(ProjectKind(result.Requirement), result.Requirement.Identifier, result.Requirement.Occurrences.FirstOrDefault()?.NodeIdentifier, result.Requirement)
        { Status = status };
        projected.Add(requirement);
        if (status == ScenarioRequirementStatus.Missing || status == ScenarioRequirementStatus.Duplicate || status == ScenarioRequirementStatus.MissingCapability || status == ScenarioRequirementStatus.WrongScene || status == ScenarioRequirementStatus.Inactive) { missing++; blocking++; }
        else if (status == ScenarioRequirementStatus.Indeterminate || status == ScenarioRequirementStatus.NotReady) indeterminate++;
      }
      return new ScenarioPreflightReport(projected, missing, indeterminate, blocking, canonical);
    }

    public static ScenarioPreflightReport Check(IReadOnlyList<ScenarioRequirement> requirements)
    {
      int missing = 0;
      int indeterminate = 0;

      if (requirements != null)
      {
        foreach (var requirement in requirements)
        {
          if (requirement == null)
          {
            indeterminate++;
            continue;
          }
          requirement.Status = Evaluate(requirement);
          switch (requirement.Status)
          {
            case ScenarioRequirementStatus.Missing:
              missing++;
              break;
            case ScenarioRequirementStatus.Indeterminate:
              indeterminate++;
              break;
          }
        }
      }

      return new ScenarioPreflightReport(
        requirements ?? new List<ScenarioRequirement>(),
        missing,
        indeterminate);
    }

    private static ScenarioRequirementStatus MapStatus(ScenarioRequirementValidationStatus status)
    {
      switch (status)
      {
        case ScenarioRequirementValidationStatus.Satisfied: return ScenarioRequirementStatus.Satisfied;
        case ScenarioRequirementValidationStatus.Missing: return ScenarioRequirementStatus.Missing;
        case ScenarioRequirementValidationStatus.Duplicate: return ScenarioRequirementStatus.Duplicate;
        case ScenarioRequirementValidationStatus.WrongScene: return ScenarioRequirementStatus.WrongScene;
        case ScenarioRequirementValidationStatus.Inactive: return ScenarioRequirementStatus.Inactive;
        case ScenarioRequirementValidationStatus.MissingCapability: return ScenarioRequirementStatus.MissingCapability;
        case ScenarioRequirementValidationStatus.Suppressed: return ScenarioRequirementStatus.Suppressed;
        case ScenarioRequirementValidationStatus.NotConsumed: return ScenarioRequirementStatus.NotConsumed;
        case ScenarioRequirementValidationStatus.NotReady: return ScenarioRequirementStatus.NotReady;
        case ScenarioRequirementValidationStatus.Malformed: return ScenarioRequirementStatus.Malformed;
        default: return ScenarioRequirementStatus.Indeterminate;
      }
    }

    private static ScenarioRequirementKind ProjectKind(ScenarioRequirementDescriptor descriptor)
    {
      switch (descriptor.Kind)
      {
        case Canonical.ScenarioRequirementKind.EventHandler: return ScenarioRequirementKind.EventHandler;
        case Canonical.ScenarioRequirementKind.SpatialAnchor:
        case Canonical.ScenarioRequirementKind.SpawnPoint: return ScenarioRequirementKind.Waypoint;
        case Canonical.ScenarioRequirementKind.EntityPreset: return ScenarioRequirementKind.EntityPreset;
        case Canonical.ScenarioRequirementKind.ItemDefinition: return ScenarioRequirementKind.Item;
        case Canonical.ScenarioRequirementKind.RuntimeSignal:
        case Canonical.ScenarioRequirementKind.PlayerTagState:
        case Canonical.ScenarioRequirementKind.RuntimeEntityReference: return ScenarioRequirementKind.Signal;
        default: return ScenarioRequirementKind.InteractionTarget;
      }
    }

    private static ScenarioRequirementStatus Evaluate(ScenarioRequirement requirement)
    {
      if (requirement.CanonicalDescriptor != null)
      {
        return EvaluateCanonical(requirement.CanonicalDescriptor);
      }

      switch (requirement.Kind)
      {
        case ScenarioRequirementKind.InteractionTarget:
          return Registry.Registry.TryGetEntity(requirement.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied
            : ScenarioRequirementStatus.Missing;

        case ScenarioRequirementKind.Waypoint:
          return Registry.Registry.Contains(RegistryType.Waypoint, requirement.Identifier)
                 || Registry.Registry.TryGetEntity(requirement.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied
            : ScenarioRequirementStatus.Missing;

        case ScenarioRequirementKind.EntityPreset:
          return Registry.Registry.TryGetEntityPreset(requirement.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied
            : ScenarioRequirementStatus.Missing;

        case ScenarioRequirementKind.EventHandler:
          // 핸들러는 선택적일 수 있으므로(연출용 등) 누락이어도 정보성으로만 다룬다.
          return ScenarioEventIdentifierRegistry.TryGetHandler(requirement.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied
            : ScenarioRequirementStatus.Indeterminate;

        case ScenarioRequirementKind.Item:
          // 아이템은 인벤토리/스폰 시점에 동적으로 만들어질 수 있어 정적 판정이 어렵다.
          // 엔티티로 등록돼 있으면 충족, 아니면 정보성으로 둔다(누락으로 단정하지 않음).
          return Registry.Registry.TryGetEntity(requirement.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied
            : ScenarioRequirementStatus.Indeterminate;

        case ScenarioRequirementKind.Signal:
        default:
          // 런타임 신호는 정적으로 판정 불가.
          return ScenarioRequirementStatus.Indeterminate;
      }
    }

    private static ScenarioRequirementStatus EvaluateCanonical(ScenarioRequirementDescriptor descriptor)
    {
      if (descriptor.EffectiveAvailability == ScenarioRequirementAvailability.NotConsumed
          || descriptor.EffectiveAvailability == ScenarioRequirementAvailability.OptionalFallback)
      {
        return ScenarioRequirementStatus.Indeterminate;
      }

      if (descriptor.Occurrences.All(occurrence => occurrence.Direction == ScenarioRequirementDirection.Produces)
          || descriptor.Kind == Canonical.ScenarioRequirementKind.RuntimeSignal
          || descriptor.Kind == Canonical.ScenarioRequirementKind.RuntimeEntityReference
          || descriptor.Kind == Canonical.ScenarioRequirementKind.PlayerTagState)
      {
        return ScenarioRequirementStatus.Indeterminate;
      }

      switch (descriptor.Kind)
      {
        case Canonical.ScenarioRequirementKind.Entity:
          return Registry.Registry.TryGetEntity(descriptor.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.Npc:
          var requiresNpcComponent = descriptor.Capabilities.Contains(ScenarioRequirementCapability.RegisteredNpcComponent);
          return Registry.Registry.Contains(RegistryType.Npc, descriptor.Identifier)
                 || !requiresNpcComponent && Registry.Registry.TryGetEntity(descriptor.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.Interactable:
          var requiresSubmissionTarget = descriptor.Capabilities.Contains(ScenarioRequirementCapability.ItemSubmissionTarget);
          return Registry.Registry.Contains(RegistryType.InteractableEntity, descriptor.Identifier)
                 || !requiresSubmissionTarget && Registry.Registry.TryGetEntity(descriptor.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.SpatialAnchor:
          if (descriptor.Capabilities.Contains(ScenarioRequirementCapability.HighlightableWaypoint))
          {
            return WaypointAnchor.TryGet(descriptor.Identifier, out _)
              ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;
          }
          return Registry.Registry.Contains(RegistryType.Waypoint, descriptor.Identifier)
                 || Registry.Registry.Contains(RegistryType.InteractableEntity, descriptor.Identifier)
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.EntityPreset:
          return Registry.Registry.TryGetEntityPreset(descriptor.Identifier, out var preset) && preset?.Prefab != null
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.EventHandler:
          return ScenarioEventIdentifierRegistry.TryGetHandler(descriptor.Identifier, out _)
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.RegistryEntry:
          var registryTypes = descriptor.Occurrences
            .Select(value => value.ResolverDiscriminator)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();
          return registryTypes.Length > 0
                 && registryTypes.All(value => System.Enum.TryParse(value, out RegistryType registryType)
                                                && Registry.Registry.Contains(registryType, descriptor.Identifier))
            ? ScenarioRequirementStatus.Satisfied : ScenarioRequirementStatus.Missing;

        case Canonical.ScenarioRequirementKind.Service:
        case Canonical.ScenarioRequirementKind.AudioResource:
        case Canonical.ScenarioRequirementKind.SpriteResource:
        case Canonical.ScenarioRequirementKind.TtsTranscript:
        case Canonical.ScenarioRequirementKind.TtsVoice:
        case Canonical.ScenarioRequirementKind.QuestDefinition:
        case Canonical.ScenarioRequirementKind.ItemDefinition:
        case Canonical.ScenarioRequirementKind.SpawnPoint:
          return ScenarioRequirementStatus.Indeterminate;

        default:
          return ScenarioRequirementStatus.Indeterminate;
      }
    }

    /// <summary>
    /// 리포트를 사람이 읽을 수 있는 요약 + 항목별 상세 문자열로 만든다.
    /// 누락(Missing)과 정보성(Indeterminate) 항목만 나열한다(충족 항목은 생략).
    /// </summary>
    public static string BuildSummary(string scenarioIdentifier, ScenarioPreflightReport report)
    {
      var builder = new StringBuilder();
      builder.Append($"[ScenarioPreflight] Scenario '{scenarioIdentifier}': ");
      builder.Append($"{report.MissingCount} missing");
      if (report.IndeterminateCount > 0)
      {
        builder.Append($", {report.IndeterminateCount} unverified");
      }
      builder.Append('.');

      foreach (var requirement in report.Requirements)
      {
        if (requirement.Status == ScenarioRequirementStatus.Missing)
        {
          builder.Append($"\n  - MISSING {requirement.Kind} '{requirement.Identifier}' (node '{requirement.SourceNodeIdentifier}')");
        }
      }

      foreach (var requirement in report.Requirements)
      {
        if (requirement.Status == ScenarioRequirementStatus.Indeterminate)
        {
          builder.Append($"\n  - unverified {requirement.Kind} '{requirement.Identifier}' (node '{requirement.SourceNodeIdentifier}')");
        }
        if (requirement.Status != ScenarioRequirementStatus.Satisfied
            && requirement.Status != ScenarioRequirementStatus.Indeterminate
            && requirement.Status != ScenarioRequirementStatus.Unknown)
          builder.Append($"\n  - {requirement.Status.ToString().ToUpperInvariant()} '{requirement.Kind}' '{requirement.Identifier}' (node '{requirement.SourceNodeIdentifier}')");
      }

      return builder.ToString();
    }
  }
}
