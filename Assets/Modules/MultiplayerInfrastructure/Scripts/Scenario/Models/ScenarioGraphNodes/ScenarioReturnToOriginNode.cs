namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 곁가지 체인의 종료 표식. 여기 닿으면 체인을 닫고 원래 흐름으로 돌아간다.
  ///
  /// <list type="bullet">
  ///   <item>ManualEntrypoint 준비 체인이면 진입 지점으로 돌아가 그 노드의 NextIdentifier 로 이어진다.</item>
  ///   <item>병렬 브랜치면 그 브랜치만 완료 처리된다.</item>
  ///   <item>메인 흐름에서 만나면 아무 일도 하지 않고 NextIdentifier 로 넘어간다.</item>
  /// </list>
  ///
  /// <para><b>왜 노드인가:</b> 예전에는 곁가지의 끝을 "NextIdentifier 가 비어 있음"으로 표시했다.
  /// 종료 의도가 데이터에 드러나지 않아, 나중에 누군가 그 노드에 다음 노드를 연결하는 순간
  /// 곁가지가 원래 흐름으로 그대로 흘러가 버렸다. 표식을 노드로 두면 그래프에서 눈에 보이고,
  /// 출력 포트가 없어 실수로 이어 붙이는 것 자체가 불가능하다.</para>
  ///
  /// <para>이 노드는 <see cref="NextIdentifier"/> 를 쓰지 않는다. 그래프 에디터도 출력 포트를
  /// 만들지 않으며, 값이 남아 있으면 진단이 경고한다.</para>
  /// </summary>
  public sealed class ScenarioReturnToOriginNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ReturnToOrigin;

    /// <summary>사용하지 않는다. 곁가지는 이 노드에서 끝난다.</summary>
    public string NextIdentifier { get; set; }

    /// <summary>작성자용 메모. 실행에는 쓰이지 않는다.</summary>
    public string Description { get; set; }
  }
}
