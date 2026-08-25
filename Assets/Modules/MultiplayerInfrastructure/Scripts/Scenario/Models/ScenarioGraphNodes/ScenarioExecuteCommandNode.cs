namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 진행 중 인게임 채팅 명령어를 서버 권한으로 실행하는 노드.
  /// 명령 문자열 자체에 대상 선택자(<c>@s</c>/<c>@a</c>/<c>@n</c>/<c>fish:&lt;id&gt;</c>)와
  /// 파이프라인(<c>|</c>)을 포함할 수 있으므로, "특정 플레이어/서버 기준 실행"을
  /// 명령 문자열로 표현한다. (예: <c>give @a item:gauze</c>)
  /// </summary>
  public sealed class ScenarioExecuteCommandNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ExecuteCommand;
    public string NextIdentifier { get; set; }

    /// <summary>
    /// 실행할 명령 문자열. 선행 슬래시(<c>/</c>)는 없어도 되며, 대상 선택자와 파이프라인을
    /// 포함할 수 있다. 서버(또는 오프라인) 컨텍스트에서 시스템 권한으로 실행된다.
    /// </summary>
    public string CommandLine { get; set; }
  }
}
