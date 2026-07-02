using System.Collections.Generic;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed class ScenarioQuizNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.Quiz;
    public string NextIdentifier { get; set; }

    public string Question { get; set; }
    public IReadOnlyList<string> Options { get; set; }
    public int CorrectIndex { get; set; }
    public string OnCorrectNextIdentifier { get; set; }
    public string OnIncorrectNextIdentifier { get; set; }
    public string FeedbackCorrect { get; set; }
    public string FeedbackIncorrect { get; set; }

    /// <summary>
    /// true이면 <see cref="Question"/> (및 피드백 텍스트)를 표시할 때 TTS로 함께 재생한다.
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
  }
}
