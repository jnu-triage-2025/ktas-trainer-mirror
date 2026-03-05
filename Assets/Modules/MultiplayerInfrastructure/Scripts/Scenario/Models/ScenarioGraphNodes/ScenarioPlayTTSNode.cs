using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// TTSService를 통해 지정된 identifier의 음성을 재생합니다.
  /// Variables에 지정된 값으로 동적 세그먼트를 오버라이드합니다.
  /// </summary>
  public sealed class ScenarioPlayTTSNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.PlayTTS;
    public string NextIdentifier { get; set; }

    /// <summary>TTSService에 등록된 스크립트 식별자</summary>
    public string TranscriptIdentifier { get; set; }

    /// <summary>동적 세그먼트 변수 오버라이드 (key: 변수명, value: 실제 값)</summary>
    public Dictionary<string, string> Variables { get; set; } = new Dictionary<string, string>();

    /// <summary>true이면 음성 재생이 끝날 때까지 다음 노드로 진행하지 않습니다.</summary>
    public bool WaitUntilFinished { get; set; } = true;
  }
}
