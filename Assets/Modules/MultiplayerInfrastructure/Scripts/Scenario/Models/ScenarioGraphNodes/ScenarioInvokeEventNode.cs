namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioInvokeEventNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.InvokeEvent;
    public string NextIdentifier { get; set; }

    public string EventIdentifier { get; set; }

    /// <summary>
    /// 역할 브랜치에서 true이면 서버 실행과 별도로 배정된 클라이언트에서도 표시용 핸들러를 실행한다.
    /// 로컬 UI처럼 클라이언트별 표현이 필요한 이벤트에만 사용한다.
    /// </summary>
    public bool InvokeOnRoleClient { get; set; }

    /// <summary>
    /// 이벤트 호출 후 NextIdentifier 로 언제 이동할지 제어한다.
    /// False: 자동으로 이동하지 않는다, Immediately: 발신 직후 이동한다, WaitUntilDone: 핸들러 완료를 기다린다.
    /// </summary>
    public ScenarioInvokeEventMoveNextBehavior MoveNextBehavior { get; set; } = ScenarioInvokeEventMoveNextBehavior.WaitUntilDone;
  }
}
