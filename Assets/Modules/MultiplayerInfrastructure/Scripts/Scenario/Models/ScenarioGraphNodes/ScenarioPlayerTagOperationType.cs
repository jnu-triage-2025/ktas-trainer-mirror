namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// PlayerTag 노드의 태그 조작 타입.
  /// </summary>
  public enum ScenarioPlayerTagOperationType
  {
    /// <summary>지정한 태그를 대상 플레이어에게 추가합니다.</summary>
    Add,
    /// <summary>지정한 태그를 대상 플레이어에게서 제거합니다.</summary>
    Remove,
    /// <summary>대상 플레이어의 FromTag를 ToTag로 교체합니다.</summary>
    Change,
  }
}
