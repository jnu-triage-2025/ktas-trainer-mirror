namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 흐름의 특정 지점에 별칭을 붙이는 표식 노드.
  ///
  /// <para>평상시에는 아무 일도 하지 않고 곧바로 <see cref="NextIdentifier"/> 로 넘어간다.
  /// 운영자가 <c>/scenario enter &lt;identifier&gt;</c> 명령을 실행하면 재생 위치가 이 노드로
  /// 건너뛴다.</para>
  ///
  /// <para>명령으로 진입한 경우에 한해 <see cref="ManualEnterSetupIdentifier"/> 체인을 먼저
  /// 실행한다. 건너뛴 구간에서 만들어졌어야 할 인게임 상황(엔티티 스폰, 퀘스트 발행, 신호 등)을
  /// 여기서 맞춰 놓는 용도다. 체인이 끝나면 이 노드로 돌아와 <see cref="NextIdentifier"/> 로
  /// 진행한다.</para>
  /// </summary>
  public sealed class ScenarioManualEntrypointNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ManualEntrypoint;
    public string NextIdentifier { get; set; }

    /// <summary>
    /// 명령에서 이 지점을 가리킬 별칭. 비어 있으면 <see cref="Identifier"/> 를 그대로 쓴다.
    /// </summary>
    public string EntrypointIdentifier { get; set; }

    /// <summary>
    /// 명령으로 진입할 때만 실행할 준비 체인의 시작 노드 식별자.
    /// 체인은 다음 중 하나에 닿으면 끝나고 제어가 이 노드로 돌아온다.
    /// (1) NextIdentifier 가 비어 있는 노드, (2) 이 노드의 <see cref="Identifier"/>,
    /// (3) 이 노드의 <see cref="NextIdentifier"/>.
    ///
    /// <para><c>clear-state=true</c>(기본값)로 진입하면 시나리오 상태 저장소가 통째로 비워진다.
    /// 이 저장소는 StateUpdate 값뿐 아니라 EntityPresetSpawn·EntityInit·ItemSubmissionConfig 가
    /// 남긴 <c>resultStateKey → 엔티티 식별자</c> 해석 표도 겸한다. 월드에 엔티티가 살아 있어도
    /// 표가 비면 <c>targetEntityStateKey</c> 로 대상을 찾는 노드들이 전부 대상을 놓치므로,
    /// 준비 체인에서 필요한 키를 다시 채워 넣어야 한다.</para>
    /// </summary>
    public string ManualEnterSetupIdentifier { get; set; }

    /// <summary>작성자용 메모. 실행에는 쓰이지 않고 명령 목록에 함께 표시된다.</summary>
    public string Description { get; set; }

    /// <summary>명령에서 이 지점을 가리키는 데 쓰는 실제 별칭.</summary>
    public string ResolvedEntrypointIdentifier =>
      string.IsNullOrWhiteSpace(EntrypointIdentifier) ? Identifier : EntrypointIdentifier.Trim();
  }
}
