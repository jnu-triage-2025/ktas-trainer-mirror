using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioTTSVoiceProfileDTO
  {
    [JsonPropertyName("preset")] public TTSVoiceStyle? Preset { get; set; }
    [JsonPropertyName("voiceIdentifier")] public string VoiceIdentifier { get; set; }
    [JsonPropertyName("voiceStyleName")] public string VoiceStyleName { get; set; }
    [JsonPropertyName("language")] public string Language { get; set; }
    [JsonPropertyName("speed")] public float? Speed { get; set; }
    [JsonPropertyName("totalStep")] public int? TotalStep { get; set; }
  }
}
