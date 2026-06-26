using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;

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

      // 동일 (종류, 식별자) 중복을 제거하기 위한 집합. 여러 노드가 같은 대상을 요구할 수 있다.
      var seen = new HashSet<string>(StringComparer.Ordinal);

      void AddRequirement(ScenarioRequirementKind kind, string identifier, string sourceNode)
      {
        if (string.IsNullOrWhiteSpace(identifier))
        {
          return;
        }

        var dedupeKey = kind + "\u0001" + identifier;
        if (!seen.Add(dedupeKey))
        {
          return;
        }

        requirements.Add(new ScenarioRequirement(kind, identifier.Trim(), sourceNode));
      }

      foreach (var pair in graph.Nodes)
      {
        var node = pair.Value;
        switch (node)
        {
          case ScenarioInteractionNode interaction:
            AddRequirement(ScenarioRequirementKind.InteractionTarget, interaction.TargetIdentifier, interaction.Identifier);
            AddRequirement(ScenarioRequirementKind.EventHandler, interaction.CompletionConditionIdentifier, interaction.Identifier);
            break;

          case ScenarioInvokeEventNode invoke:
            AddRequirement(ScenarioRequirementKind.EventHandler, invoke.EventIdentifier, invoke.Identifier);
            break;

          case ScenarioQuestWaypointHighlightNode waypoint:
            AddRequirement(ScenarioRequirementKind.Waypoint, waypoint.WaypointIdentifier, waypoint.Identifier);
            break;

          case ScenarioEntityPresetSpawnNode preset:
            AddRequirement(ScenarioRequirementKind.EntityPreset, preset.PresetIdentifier, preset.Identifier);
            break;

          case ScenarioCombineItemNode combine:
            if (combine.InputItemIdentifiers != null)
            {
              foreach (var input in combine.InputItemIdentifiers)
              {
                AddRequirement(ScenarioRequirementKind.Item, input, combine.Identifier);
              }
            }
            AddRequirement(ScenarioRequirementKind.Item, combine.OutputItemIdentifier, combine.Identifier);
            // 비자동 조합은 결과 식별자에 핸들러가 붙을 수 있으므로 이벤트 핸들러로도 보고한다.
            if (!combine.AutoCombine)
            {
              AddRequirement(ScenarioRequirementKind.EventHandler, combine.OutputItemIdentifier, combine.Identifier);
            }
            break;

          case ScenarioValidatorNode validator:
            CollectValidatorSignals(validator, AddRequirement);
            break;
        }
      }

      return requirements;
    }

    /// <summary>
    /// Validator 의 RegistryContains/RuntimeState 규칙에서 신호 식별자(sig.*)를 정보성 요구로 수집한다.
    /// 신호는 런타임에 게임플레이가 올리는 값이므로 정적 존재 판정은 하지 않는다.
    /// </summary>
    private static void CollectValidatorSignals(
      ScenarioValidatorNode validator,
      Action<ScenarioRequirementKind, string, string> addRequirement)
    {
      if (validator.RootConditions == null)
      {
        return;
      }

      foreach (var rootCondition in validator.RootConditions)
      {
        if (rootCondition == null
            || rootCondition.Condition != ScenarioValidatorCondition.RegistryContains
            || rootCondition.ValidationRules == null)
        {
          continue;
        }

        foreach (var rule in rootCondition.ValidationRules)
        {
          if (rule == null || rule.RegistryType != RegistryType.RuntimeState)
          {
            continue;
          }

          addRequirement(ScenarioRequirementKind.Signal, rule.RegistryIdentifier, validator.Identifier);
        }
      }
    }
  }
}
