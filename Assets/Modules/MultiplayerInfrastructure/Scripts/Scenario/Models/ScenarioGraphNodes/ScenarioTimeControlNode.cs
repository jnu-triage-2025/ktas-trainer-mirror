namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시간 표시(스톱워치/카운트다운)에 가할 연산.
  /// 생성/흐름/표시가 각각 분리되어 있어, 하나의 노드 타입으로 다중 타이머를 유연하게 제어한다.
  /// </summary>
  public enum ScenarioTimeOperationType
  {
    /// <summary>
    /// 타이머를 생성(또는 재설정)한다. "정지" 상태로 만들며 화면에 표시하지 않는다.
    /// 사용 파라미터: <see cref="ScenarioTimeControlNode.TimerId"/>,
    /// <see cref="ScenarioTimeControlNode.Direction"/>,
    /// <see cref="ScenarioTimeControlNode.DurationSeconds"/>(카운트다운 목표),
    /// <see cref="ScenarioTimeControlNode.StartSeconds"/>(시작 표시값).
    /// </summary>
    Create,

    /// <summary>타이머 흐름을 시작(또는 재시작)한다. 사용 파라미터: TimerId.</summary>
    Start,

    /// <summary>타이머 흐름을 일시정지한다. 사용 파라미터: TimerId.</summary>
    Pause,

    /// <summary>일시정지된 타이머 흐름을 재개한다. 사용 파라미터: TimerId.</summary>
    Resume,

    /// <summary>타이머 흐름을 정지하고 값을 시작값으로 되돌린다. 사용 파라미터: TimerId.</summary>
    Stop,

    /// <summary>
    /// 타이머의 현재 표시값을 절대값으로 설정한다(흐름 상태 유지). 카운트다운 목표(총)도 조정 가능.
    /// 사용 파라미터: TimerId, <see cref="ScenarioTimeControlNode.StartSeconds"/>(설정할 표시값),
    /// <see cref="ScenarioTimeControlNode.DurationSeconds"/>(카운트다운 목표 재설정, 0 이면 유지).
    /// </summary>
    Set,

    /// <summary>지정한 타이머를 화면에 표시한다(표시는 항상 최대 1개, 기존 표시 교체). 사용 파라미터: TimerId.</summary>
    Show,

    /// <summary>화면 표시를 끈다(타이머 상태/흐름은 유지). 파라미터 없음.</summary>
    Hide,

    /// <summary>타이머를 삭제한다(표시 중이면 표시도 꺼짐). 사용 파라미터: TimerId.</summary>
    Remove
  }

  /// <summary>
  /// 시간 표시(HUD)를 제어하는 시나리오 노드.
  ///
  /// <see cref="Operation"/> 에 따라 생성/흐름/표시/삭제를 각각 수행하며, 실행 즉시
  /// <see cref="ScenarioTimeRelay"/> 를 통해 서버 권한으로 모든 클라이언트에 전파하고
  /// 곧바로 다음 노드로 진행한다(대기하지 않는다).
  ///
  /// 식별자(<see cref="TimerId"/>)로 여러 타이머를 동시에 보유할 수 있으나, 화면에 표시되는
  /// 타이머는 항상 최대 1개다(<see cref="ScenarioTimeOperationType.Show"/> 로 전환).
  /// 카운트다운이 0 에 도달해도 자동으로 숨겨지지 않는다(표시 전환은 Show/Hide/Remove 로만).
  /// </summary>
  public sealed class ScenarioTimeControlNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.TimeControl;
    public string NextIdentifier { get; set; }

    /// <summary>가할 연산. 기본값은 <see cref="ScenarioTimeOperationType.Create"/>.</summary>
    public ScenarioTimeOperationType Operation { get; set; } = ScenarioTimeOperationType.Create;

    /// <summary>대상 타이머 식별자. <see cref="ScenarioTimeOperationType.Hide"/> 외 모든 연산에서 사용.</summary>
    public string TimerId { get; set; }

    /// <summary>
    /// 흐름 방향(<see cref="ScenarioTimeOperationType.Create"/> 에서만 사용).
    /// 정방향=스톱워치, 역방향=카운트다운.
    /// </summary>
    public ScenarioTimeDirection Direction { get; set; } = ScenarioTimeDirection.Stopwatch;

    /// <summary>
    /// Create 에서는 카운트다운 목표(총) 시간(초). Set 에서는 카운트다운 목표 재설정(0 이면 유지).
    /// 스톱워치에서는 무시된다.
    /// </summary>
    public float DurationSeconds { get; set; }

    /// <summary>
    /// Create 에서는 시작 시 표시값(스톱워치=경과, 카운트다운=남은값. 카운트다운 0 이면 목표로 대체).
    /// Set 에서는 설정할 현재 표시값(절대값). 다른 연산에서는 무시된다.
    /// </summary>
    public float StartSeconds { get; set; }
  }
}
