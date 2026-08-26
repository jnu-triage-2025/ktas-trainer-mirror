namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 실행 수명주기를 그래프 안에서 명시적으로 제어한다.
  /// Cleanup은 다음 노드로 진행하고, End와 Restart는 현재 흐름을 종료한다.
  /// </summary>
  public sealed class ScenarioLifecycleNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Lifecycle;
    public string NextIdentifier { get; set; }
    public ScenarioLifecycleOperation Operation { get; set; } = ScenarioLifecycleOperation.Cleanup;
    public bool RevertTrackedChanges { get; set; } = true;
    public bool ClearRuntimeState { get; set; } = true;
    public string RestartEntrypointIdentifier { get; set; }
  }

  public enum ScenarioLifecycleOperation
  {
    Cleanup,
    End,
    Restart,
  }
}
