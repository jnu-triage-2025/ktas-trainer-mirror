namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// PlayerTag 노드의 대상 플레이어 범위.
  /// </summary>
  public enum ScenarioPlayerTagScope
  {
    /// <summary>현재 연결된 모든 플레이어에게 적용합니다.</summary>
    All,
    /// <summary>이 시나리오를 실행 중인 플레이어(_scenarioOwnerClientId)에게만 적용합니다.</summary>
    Current,
    /// <summary>특정 태그(Tag)를 보유한 플레이어들에게만 적용합니다(Add/Remove 한정).</summary>
    ByTag,
  }
}
