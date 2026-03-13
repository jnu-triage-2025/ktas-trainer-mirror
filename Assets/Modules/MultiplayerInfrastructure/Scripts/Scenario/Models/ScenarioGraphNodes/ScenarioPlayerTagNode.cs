namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 플레이어 태그를 추가·제거·변경하는 노드.
  /// Scope가 Current일 때는 시나리오 실행 중인 플레이어(_scenarioOwnerClientId)를 대상으로 합니다.
  /// Scope가 All일 때는 현재 접속한 모든 플레이어에게 적용됩니다.
  /// </summary>
  public sealed class ScenarioPlayerTagNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.PlayerTag;
    public string NextIdentifier { get; set; }

    /// <summary>태그 조작 타입.</summary>
    public ScenarioPlayerTagOperationType Operation { get; set; } = ScenarioPlayerTagOperationType.Add;

    /// <summary>대상 플레이어 범위.</summary>
    public ScenarioPlayerTagScope Scope { get; set; } = ScenarioPlayerTagScope.Current;

    /// <summary>Add / Remove 시 사용할 태그 값.</summary>
    public string Tag { get; set; }

    /// <summary>Change 시 교체 대상 원래 태그.</summary>
    public string FromTag { get; set; }

    /// <summary>Change 시 새로 교체될 태그.</summary>
    public string ToTag { get; set; }
  }
}
