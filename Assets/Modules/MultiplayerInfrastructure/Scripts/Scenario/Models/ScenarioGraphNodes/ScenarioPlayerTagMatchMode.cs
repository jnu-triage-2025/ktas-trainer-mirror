namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// ParallelBranch의 RequiredPlayerTags 매칭 방식.
  /// </summary>
  public enum ScenarioPlayerTagMatchMode
  {
    /// <summary>모든 태그를 만족해야 매칭됩니다.</summary>
    All,

    /// <summary>하나 이상의 태그를 만족하면 매칭됩니다.</summary>
    Any
  }
}
