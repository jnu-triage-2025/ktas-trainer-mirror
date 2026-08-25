using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioDisinteractableDialogueNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("speakerName")]
    public string SpeakerName { get; set; }

    [JsonPropertyName("dialogueContent")]
    public string DialogueContent { get; set; }

    [JsonPropertyName("portraitSpriteIdentifier")]
    public string PortraitSpriteIdentifier { get; set; }

    [JsonPropertyName("fadeInDuration")]
    public ScenarioTimeValue? FadeInDuration { get; set; }

    [JsonPropertyName("displayDuration")]
    public ScenarioTimeValue? DisplayDuration { get; set; }

    [JsonPropertyName("fadeOutDuration")]
    public ScenarioTimeValue? FadeOutDuration { get; set; }

    [JsonPropertyName("playTTS")]
    public bool? PlayTTS { get; set; }

    [JsonPropertyName("ttsVoiceIdentifier")]
    public string TtsVoiceIdentifier { get; set; }
    [JsonPropertyName("ttsVoiceProfile")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ScenarioTTSVoiceProfileDTO TtsVoiceProfile { get; set; }
  }
}
