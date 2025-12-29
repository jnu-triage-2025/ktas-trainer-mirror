namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioValidatorCondition
  {
    PlayerCountEqual,
    PlayerCountNotEqual,
    PlayerCountLessThan,
    PlayerCountLessThanOrEqual,
    PlayerCountGreaterThan,
    PlayerCountGreaterThanOrEqual
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

    public ScenarioValidatorCondition Condition { get; set; }
    public int TargetCount { get; set; }
    public ScenarioValidatorOnFailure OnFailure { get; set; } = ScenarioValidatorOnFailure.Panic;
    public string FailureNextIdentifier { get; set; }
  }
}
