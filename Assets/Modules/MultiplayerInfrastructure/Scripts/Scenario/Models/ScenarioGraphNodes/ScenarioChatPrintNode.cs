using System;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <see cref="ScenarioChatPrintNode"/> 가 텍스트를 어디에 출력할지 지정하는 대상 플래그.
  /// (Validator 의 <see cref="ScenarioValidatorFailureReportTarget"/> 와 동일한 패턴)
  /// </summary>
  [Flags]
  public enum ScenarioChatPrintTarget
  {
    None = 0,

    /// <summary>Unity 콘솔(Debug.Log)에 출력한다.</summary>
    UnityConsole = 1 << 0,

    /// <summary>인게임 채팅창에 출력한다.</summary>
    InGameChat = 1 << 1
  }

  /// <summary>
  /// 시나리오 진행 중 임의의 텍스트를 인게임 채팅창 및/또는 Unity 콘솔에 출력하는 노드.
  /// 시그널/이벤트 발생을 눈으로 확인하는 디버깅·데모 용도로 사용한다.
  /// </summary>
  public sealed class ScenarioChatPrintNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.ChatPrint;
    public string NextIdentifier { get; set; }

    /// <summary>출력할 메시지 본문.</summary>
    public string Message { get; set; }

    /// <summary>출력 대상(콘솔/인게임 채팅, 플래그 조합 가능). 기본값은 인게임 채팅.</summary>
    public ScenarioChatPrintTarget Targets { get; set; } = ScenarioChatPrintTarget.InGameChat;

    /// <summary>
    /// true 이면 서버가 전체 클라이언트에게 브로드캐스트한다(InGameChat 대상에 한함).
    /// false(기본)이면 각 피어가 자기 로컬 채팅창/콘솔에만 출력한다.
    /// </summary>
    public bool Broadcast { get; set; } = false;
  }
}
