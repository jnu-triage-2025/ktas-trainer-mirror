using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioChoiceNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Choice;
    public string NextIdentifier { get; set; }

    public string SpeakerName { get; set; }
    public string DialogueContent { get; set; }
    public string PortraitSpriteIdentifier { get; set; }

    public List<ScenarioChoiceOption> Options { get; set; }

    /// <summary>교육 평가 로그에 사용할 안정적인 항목 식별자. 비어 있으면 일반 선택으로 취급한다.</summary>
    public string AssessmentIdentifier { get; set; }

    /// <summary>의도된 정답 옵션 인덱스. null이면 정답 여부를 기록하지 않는다.</summary>
    public int? CorrectOptionIndex { get; set; }

    /// <summary>
    /// true이면 <see cref="DialogueContent"/> 를 표시할 때 TTS로 함께 재생한다.
    /// 변수를 포함하지 않는 콘텐츠는 에디터에서 사전 합성(bake)될 수 있으며,
    /// bake되지 않은 경우 런타임에 즉석으로 합성해 재생한다.
    /// </summary>
    public bool PlayTTS { get; set; }

    /// <summary>
    /// 사용할 목소리 프로파일 식별자.
    /// TTSService의 Voice Profiles 목록에 등록된 식별자를 지정한다.
    /// null 또는 빈 문자열이면 TTSService의 기본 목소리를 사용한다.
    /// <see cref="PlayTTS"/>가 true일 때만 효과가 있다.
    /// </summary>
    public string TtsVoiceIdentifier { get; set; }
    public ScenarioTTSVoiceProfile TtsVoiceProfile { get; set; }
  }
}
