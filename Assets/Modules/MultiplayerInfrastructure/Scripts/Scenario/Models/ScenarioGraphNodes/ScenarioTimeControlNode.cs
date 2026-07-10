namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시간 표시(스톱워치/카운트다운)에 가할 동작.
  /// </summary>
  public enum ScenarioTimeAction
  {
    /// <summary>타이머를 시작(또는 재시작)한다. 방향/시작값/목표값을 사용한다.</summary>
    Start,

    /// <summary>흐름을 일시정지한다(표시 유지).</summary>
    Pause,

    /// <summary>일시정지된 흐름을 재개한다.</summary>
    Resume,

    /// <summary>흐름을 정지하고 값을 리셋한다(표시 유지).</summary>
    Stop,

    /// <summary>시간 표시를 숨긴다.</summary>
    Hide
  }

  /// <summary>
  /// 시간 표시(HUD)를 제어하는 시나리오 노드.
  ///
  /// 실행 즉시 <see cref="ScenarioTimeRelay"/> 를 통해 서버 권한으로 모든 클라이언트에
  /// 명령을 전파하고 곧바로 다음 노드로 진행한다(대기하지 않는다).
  /// 정방향(<see cref="ScenarioTimeDirection.Stopwatch"/>)은 스톱워치,
  /// 역방향(<see cref="ScenarioTimeDirection.Countdown"/>)은 타이머/카운트다운으로 흐른다.
  ///
  /// 후속 확장(시간 조건 분기, 진행 수준/로깅 연동)을 위해 상태는
  /// <see cref="ScenarioTimeState"/> 정적 저장소에 노출된다.
  /// </summary>
  public sealed class ScenarioTimeControlNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.TimeControl;
    public string NextIdentifier { get; set; }

    /// <summary>가할 동작. 기본값은 <see cref="ScenarioTimeAction.Start"/>.</summary>
    public ScenarioTimeAction Action { get; set; } = ScenarioTimeAction.Start;

    /// <summary>
    /// 흐름 방향(<see cref="ScenarioTimeAction.Start"/> 에서만 의미가 있다).
    /// 정방향=스톱워치, 역방향=카운트다운.
    /// </summary>
    public ScenarioTimeDirection Direction { get; set; } = ScenarioTimeDirection.Stopwatch;

    /// <summary>
    /// 카운트다운의 목표(총) 시간(초). Start + Countdown 에서만 사용된다.
    /// 스톱워치에서는 무시된다.
    /// </summary>
    public float DurationSeconds { get; set; }

    /// <summary>
    /// 시작 시점의 표시 값(초). Start 에서만 사용된다.
    /// 스톱워치는 보통 0, 카운트다운은 보통 <see cref="DurationSeconds"/> 와 같게 준다.
    /// 카운트다운에서 0(또는 미지정)이면 <see cref="DurationSeconds"/> 로 대체한다.
    /// </summary>
    public float StartSeconds { get; set; }
  }
}
