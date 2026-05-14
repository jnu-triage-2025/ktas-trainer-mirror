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

  public sealed class ScenarioValidatorNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Validator;
    public string NextIdentifier { get; set; }

    public IReadOnlyList<ScenarioValidatorRootCondition> RootConditions { get; set; } = new List<ScenarioValidatorRootCondition>();
    public ScenarioValidatorOnFailure OnFailure { get; set; } = ScenarioValidatorOnFailure.Panic;
    public ScenarioValidatorFailureReportTarget FailureReportTargets { get; set; } = ScenarioValidatorFailureReportTarget.UnityConsole;
    public string FailureNextIdentifier { get; set; }
  }
}
