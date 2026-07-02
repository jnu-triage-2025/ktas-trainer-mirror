using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioDialogueNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("speakerName")]
    public string SpeakerName { get; set; }

    [JsonPropertyName("dialogueContent")]
    public string DialogueContent { get; set; }

    [JsonPropertyName("portraitSpriteIdentifier")]
    public string PortraitSpriteIdentifier { get; set; }

    [JsonPropertyName("autoAdvanceSeconds")]
    public float? AutoAdvanceSeconds { get; set; }

    [JsonPropertyName("interactionRequired")]
    public bool? InteractionRequired { get; set; }

    [JsonPropertyName("playTTS")]
    public bool? PlayTTS { get; set; }

    [JsonPropertyName("ttsVoiceIdentifier")]
    public string TtsVoiceIdentifier { get; set; }
  }
}
