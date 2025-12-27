namespace MultiplayerInfrastructure.Scenario
{
  public enum ScenarioWaitMode
  {
    All,    // 모든 브랜치가 끝나야 NextId 진행
    Any,    // 하나라도 끝나면 NextId 진행
    None    // 시작 즉시 NextId 진행 (fire-and-forget)
  }
}
