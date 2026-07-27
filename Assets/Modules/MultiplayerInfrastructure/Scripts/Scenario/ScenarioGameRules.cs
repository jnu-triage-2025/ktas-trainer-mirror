namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>시나리오 실행에 적용되는 서버 게임 규칙.</summary>
  public static class ScenarioGameRules
  {
    /// <summary>
    /// true이면 Parallel/PlayerAssignedTag 게이트에서 요구 태그가 충족되지 않아도 흐름을 계속 진행한다.
    /// 기본값은 데모/단독 진행을 위해 true이며, false이면 그래프에 정의된 원래 태그 게이트를 엄격히 적용한다.
    /// </summary>
    public static bool IgnoreTagAssignFullSatisfactionOnScenarioPlay { get; set; } = true;
  }
}
