using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioInteractionVisibilityOperation
  {
    Show,
    Hide,
    Reset
  }

  public enum ScenarioInteractionVisibilityPlayerScope
  {
    All,
    Current,
    ByTag
  }

  /// <summary>가시성 노드가 가리키는 인터렉션 주소. 엔티티는 식별자 또는 태그 참조다.</summary>
  public sealed class ScenarioInteractionTarget
  {
    public ScenarioEntityReference Entity { get; set; } = new ScenarioEntityReference();
    public string InteractionIdentifier { get; set; }

    public ScenarioInteractionTarget Clone()
      => new ScenarioInteractionTarget { Entity = Entity?.Clone() ?? new ScenarioEntityReference(), InteractionIdentifier = InteractionIdentifier };
  }

  /// <summary>
  /// 트리거 방식 가시성 제어 노드. 인터렉션 주소의 가시성 오버라이드를 서버 권위로 기록한다.
  /// <see cref="PlayerScope"/> 가 All 이면 전역 층에, Current/ByTag 면 해당 플레이어의 층에 기록한다.
  /// Reset 은 오버라이드를 지워 조건 판정으로 되돌린다.
  /// </summary>
  public sealed class ScenarioInteractionVisibilityNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.InteractionVisibility;
    public string NextIdentifier { get; set; }

    public ScenarioInteractionVisibilityOperation Operation { get; set; } = ScenarioInteractionVisibilityOperation.Show;
    public List<ScenarioInteractionTarget> Targets { get; set; } = new List<ScenarioInteractionTarget>();
    public ScenarioInteractionVisibilityPlayerScope PlayerScope { get; set; } = ScenarioInteractionVisibilityPlayerScope.All;
    public List<string> PlayerTags { get; set; } = new List<string>();
    public ScenarioConditionMatchMode TagMatchMode { get; set; } = ScenarioConditionMatchMode.Any;
  }
}
