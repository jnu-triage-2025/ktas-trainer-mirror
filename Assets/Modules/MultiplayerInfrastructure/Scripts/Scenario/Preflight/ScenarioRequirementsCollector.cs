using System.Collections.Generic;
using System.Linq;
using MultiplayerInfrastructure.Scenario.Requirements;
using Canonical = MultiplayerInfrastructure.Scenario.Requirements;

namespace MultiplayerInfrastructure.Scenario.Preflight
{
  /// <summary>
  /// <see cref="ScenarioGraph"/> 를 정적으로 훑어 시나리오가 요구하는 요소 목록을 만든다.
  /// 부작용이 없으며(레지스트리/씬을 조회하지 않음) 그래프 구조만 분석한다.
  /// 실제 존재 여부 판정은 <see cref="ScenarioRequirementsChecker"/> 가 수행한다.
  /// </summary>
  public static class ScenarioRequirementsCollector
  {
    public static List<ScenarioRequirement> Collect(ScenarioGraph graph)
    {
      var requirements = new List<ScenarioRequirement>();
      if (graph == null)
      {
        return requirements;
      }

      var manifest = ScenarioRequirementCompiler.CompileInferred(graph);
      foreach (var descriptor in manifest.Requirements)
      {
        requirements.Add(new ScenarioRequirement(
          ProjectKind(descriptor),
          descriptor.Identifier,
          descriptor.Occurrences.FirstOrDefault()?.NodeIdentifier,
          descriptor));
      }

      return requirements;
    }

    private static ScenarioRequirementKind ProjectKind(ScenarioRequirementDescriptor descriptor)
    {
      switch (descriptor.Kind)
      {
        case Canonical.ScenarioRequirementKind.EventHandler:
          return ScenarioRequirementKind.EventHandler;
        case Canonical.ScenarioRequirementKind.SpatialAnchor:
        case Canonical.ScenarioRequirementKind.SpawnPoint:
          return ScenarioRequirementKind.Waypoint;
        case Canonical.ScenarioRequirementKind.EntityPreset:
          return ScenarioRequirementKind.EntityPreset;
        case Canonical.ScenarioRequirementKind.ItemDefinition:
          return ScenarioRequirementKind.Item;
        case Canonical.ScenarioRequirementKind.RuntimeSignal:
        case Canonical.ScenarioRequirementKind.PlayerTagState:
        case Canonical.ScenarioRequirementKind.RuntimeEntityReference:
          return ScenarioRequirementKind.Signal;
        default:
          return ScenarioRequirementKind.InteractionTarget;
      }
    }
  }
}
