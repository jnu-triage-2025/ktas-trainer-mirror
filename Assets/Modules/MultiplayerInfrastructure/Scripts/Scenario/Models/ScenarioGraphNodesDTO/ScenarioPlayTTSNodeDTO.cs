using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioPlayTTSNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("transcriptIdentifier")]
    public string TranscriptIdentifier { get; set; }

    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; }

    [JsonPropertyName("waitUntilFinished")]
    public bool? WaitUntilFinished { get; set; }

    [JsonPropertyName("ttsVoiceIdentifier")]
    public string TtsVoiceIdentifier { get; set; }
  }
}
