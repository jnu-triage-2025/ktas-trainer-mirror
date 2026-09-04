namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioServerInternalSignalNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ServerInternalSignal;
    public string NextIdentifier { get; set; }

    public string TargetIdentifier { get; set; } = ScenarioServerInternalSignalRegistry.ServerTarget;
    public string SignalIdentifier { get; set; }
    public ScenarioServerInternalSignalOperationType Operation { get; set; } = ScenarioServerInternalSignalOperationType.Register;
    public bool WaitForResolution { get; set; } = true;

    /// <summary>
    /// <see cref="WaitForResolution"/> 대기의 선택적 타임아웃(초). null 또는 0 이하이면 Resolve 가 올 때까지
    /// 무한 대기한다. 양수이면 그 시간 안에 Resolve 가 오지 않을 경우 대기를 끝내고 다음 노드로 진행한다.
    /// Resolve 를 올릴 담당자가 이탈한 경우처럼, 조건을 만들 사람이 사라진 대기가 세션을 멈추지 않게 한다.
    /// </summary>
    public float? WaitTimeoutSeconds { get; set; } = null;
  }
}
