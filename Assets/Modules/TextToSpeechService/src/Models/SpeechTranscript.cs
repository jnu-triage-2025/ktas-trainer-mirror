using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TextToSpeechService
{
  [Serializable]
  public struct SpeechTranscript
  {
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("basetext")]
    public string BaseText { get; set; }

    [JsonPropertyName("variables")]
    public Dictionary<string, string> Variables { get; set; }
  }
}
