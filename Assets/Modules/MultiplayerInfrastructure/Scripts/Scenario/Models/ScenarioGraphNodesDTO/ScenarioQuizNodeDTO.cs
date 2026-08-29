using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioQuizNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("question")]
    public string Question { get; set; }

    [JsonPropertyName("questionTTSPassing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string QuestionTTSPassing { get; set; }

    [JsonPropertyName("options")]
    public List<string> Options { get; set; }

    [JsonPropertyName("correctIndex")]
    public int? CorrectIndex { get; set; }

    [JsonPropertyName("onCorrectNextIdentifier")]
    public string OnCorrectNextIdentifier { get; set; }

    [JsonPropertyName("onIncorrectNextIdentifier")]
    public string OnIncorrectNextIdentifier { get; set; }

    [JsonPropertyName("feedbackCorrect")]
    public string FeedbackCorrect { get; set; }

    [JsonPropertyName("feedbackCorrectTTSPassing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string FeedbackCorrectTTSPassing { get; set; }

    [JsonPropertyName("feedbackIncorrect")]
    public string FeedbackIncorrect { get; set; }

    [JsonPropertyName("feedbackIncorrectTTSPassing")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string FeedbackIncorrectTTSPassing { get; set; }

    [JsonPropertyName("playTTS")]
    public bool? PlayTTS { get; set; }

    [JsonPropertyName("ttsVoiceIdentifier")]
    public string TtsVoiceIdentifier { get; set; }
    [JsonPropertyName("ttsVoiceProfile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioTTSVoiceProfileDTO TtsVoiceProfile { get; set; }
  }
}
