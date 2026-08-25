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

    /// <summary>
    /// 사용할 목소리 프로파일 식별자.
    /// TTSService의 Voice Profiles 목록에 등록된 식별자를 지정한다.
    /// null 또는 빈 문자열이면 TTSService의 기본 목소리를 사용한다.
    /// </summary>
    public string TtsVoiceIdentifier { get; set; }

    /// <summary>사전 스타일 또는 시나리오 JSON 프로필을 직접 지정하는 노드별 TTS 프로필.</summary>
    public ScenarioTTSVoiceProfile TtsVoiceProfile { get; set; }
  }
}
