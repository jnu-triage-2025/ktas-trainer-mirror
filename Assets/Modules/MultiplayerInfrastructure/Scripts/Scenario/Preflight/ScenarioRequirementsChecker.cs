using System.Collections.Generic;
using System.Text;
using MultiplayerInfrastructure.Registry;
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

    public bool HasMissing => MissingCount > 0;

    public ScenarioPreflightReport(IReadOnlyList<ScenarioRequirement> requirements, int missingCount, int indeterminateCount)
    {
      Requirements = requirements;
      MissingCount = missingCount;
      IndeterminateCount = indeterminateCount;
    }
  }

  /// <summary>
  /// 요구 항목 목록을 검사해 충족/누락/불확실로 판정하고, 요약 리포트를 만든다.
  /// 신호(sig.*)는 정적으로 판정할 수 없으므로 항상 Indeterminate 로 둔다(누락 집계 제외).
  /// </summary>
  public static class ScenarioRequirementsChecker
  {
    public static ScenarioPreflightReport Check(IReadOnlyList<ScenarioRequirement> requirements)
    {
      int missing = 0;
      int indeterminate = 0;

      if (requirements != null)
      {
        foreach (var requirement in requirements)
        {
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

    private static ScenarioRequirementStatus Evaluate(ScenarioRequirement requirement)
    {
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
      }

      return builder.ToString();
    }
  }
}
