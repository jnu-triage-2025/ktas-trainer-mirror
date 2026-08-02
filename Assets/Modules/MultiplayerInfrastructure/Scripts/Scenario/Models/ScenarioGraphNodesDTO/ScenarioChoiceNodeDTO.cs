using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioChoiceNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("speakerName")]
    public string SpeakerName { get; set; }

    [JsonPropertyName("dialogueContent")]
    public string DialogueContent { get; set; }

    [JsonPropertyName("portraitSpriteIdentifier")]
    public string PortraitSpriteIdentifier { get; set; }

    [JsonPropertyName("options")]
    public List<ScenarioChoiceOptionDTO> Options { get; set; }

    [JsonPropertyName("assessmentIdentifier")]
    public string AssessmentIdentifier { get; set; }

    [JsonPropertyName("correctOptionIndex")]
    public int? CorrectOptionIndex { get; set; }

    [JsonPropertyName("playTTS")]
    public bool? PlayTTS { get; set; }

    [JsonPropertyName("ttsVoiceIdentifier")]
    public string TtsVoiceIdentifier { get; set; }

    // Choice 노드는 nextIdentifier가 항상 null이어야 하므로 DTO에도 명시적으로 포함
    [JsonPropertyName("nextIdentifier")]
    public new string NextIdentifier
    {
      get => base.NextIdentifier;
      set => base.NextIdentifier = value;
    }
  }
}
