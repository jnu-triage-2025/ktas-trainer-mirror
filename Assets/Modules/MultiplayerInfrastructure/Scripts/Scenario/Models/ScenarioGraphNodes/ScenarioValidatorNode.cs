using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Registry;

namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioValidatorCondition
  {
    PlayerCountEqual,
    PlayerCountNotEqual,
    PlayerCountLessThan,
    PlayerCountLessThanOrEqual,
    PlayerCountGreaterThan,
    PlayerCountGreaterThanOrEqual,
    RegistryContains,
    PlayerAssignedTag
  }

  public enum ScenarioValidatorPlayerScope
  {
    Any,
    All,
    Owner
  }

  [Flags]
  public enum ScenarioValidatorFailureReportTarget
  {
    None = 0,
    UnityConsole = 1 << 0,
    InGameChat = 1 << 1
  }

  public enum ScenarioValidatorRuleType
  {
    Registry
  }

  public enum ScenarioValidatorRuleCondition
  {
    Contains
  }

  [Serializable]
  public sealed class ScenarioValidatorRule
  {
    public ScenarioValidatorRuleType Type { get; set; } = ScenarioValidatorRuleType.Registry;
    public ScenarioValidatorRuleCondition Condition { get; set; } = ScenarioValidatorRuleCondition.Contains;
    public RegistryType RegistryType { get; set; } = RegistryType.Waypoint;
    public string RegistryIdentifier { get; set; }
  }

  [Serializable]
  public sealed class ScenarioValidatorRootCondition
  {
    public ScenarioValidatorCondition Condition { get; set; }
    public int TargetCount { get; set; }
    public string PlayerTag { get; set; }
    public ScenarioValidatorPlayerScope PlayerScope { get; set; } = ScenarioValidatorPlayerScope.Any;
    public IReadOnlyList<ScenarioValidatorRule> ValidationRules { get; set; } = new List<ScenarioValidatorRule>();
  }

  public enum ScenarioValidatorOnFailure
  {
    Panic,
    Branching,
    Ignore
  }

  /// <summary>
  /// <see cref="ScenarioValidatorNode.WaitForCondition"/> 게이트가 <see cref="ScenarioValidatorNode.WaitTimeoutSeconds"/>
  /// 동안 조건을 충족하지 못했을 때의 행동 정책. 기본값(<see cref="KeepWaiting"/>)은 기존 동작(무한 대기)과 동일하다.
  /// </summary>
  public enum ScenarioValidatorWaitTimeoutBehavior
  {
    /// <summary>기존 동작(무한 대기). 타임아웃을 무시하고 조건이 올라올 때까지 계속 대기한다. 기본값.</summary>
    KeepWaiting,

    /// <summary><see cref="ScenarioValidatorNode.FailureNextIdentifier"/> 로 분기한다(미지정/미존재 시 KeepWaiting 으로 폴백).</summary>
    FailBranch,

    /// <summary><see cref="ScenarioValidatorNode.NextIdentifier"/> 로 강제 진행한다(미수행 기록 이벤트와 함께).</summary>
    ForceAdvance,

    /// <summary>운영자에게 경고(콘솔/인게임챗)한 뒤 계속 대기한다.</summary>
    WarnAndKeepWaiting
  }

  public sealed class ScenarioValidatorNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Validator;
    public string NextIdentifier { get; set; }

    public IReadOnlyList<ScenarioValidatorRootCondition> RootConditions { get; set; } = new List<ScenarioValidatorRootCondition>();
    public ScenarioValidatorOnFailure OnFailure { get; set; } = ScenarioValidatorOnFailure.Panic;
    public ScenarioValidatorFailureReportTarget FailureReportTargets { get; set; } = ScenarioValidatorFailureReportTarget.UnityConsole;
    public string FailureNextIdentifier { get; set; }

    /// <summary>
    /// true 이면, 조건이 충족될 때까지 진행을 막고 폴링 대기하는 "게이트"로 동작한다.
    /// (인터랙션 완료 신호 <see cref="ScenarioInteractionSignals"/> 가 올라올 때까지 대기)
    /// false(기본)이면 기존처럼 1회만 평가하고 <see cref="OnFailure"/> 정책을 따른다(하위호환).
    /// </summary>
    public bool WaitForCondition { get; set; } = false;

    /// <summary>
    /// <see cref="WaitForCondition"/> 게이트의 선택적 타임아웃(초). null 또는 0 이하이면 타임아웃 없이
    /// 무한 대기한다(기존 동작, 하위호환). 양수이면 그 시간 안에 조건이 충족되지 않을 경우
    /// <see cref="OnWaitTimeout"/> 정책이 적용된다.
    /// </summary>
    public float? WaitTimeoutSeconds { get; set; } = null;

    /// <summary>
    /// <see cref="WaitTimeoutSeconds"/> 초과 시 행동 정책. 기본값은 기존 동작과 동일한
    /// <see cref="ScenarioValidatorWaitTimeoutBehavior.KeepWaiting"/>(계속 대기)이다.
    /// </summary>
    public ScenarioValidatorWaitTimeoutBehavior OnWaitTimeout { get; set; } = ScenarioValidatorWaitTimeoutBehavior.KeepWaiting;
  }
}
