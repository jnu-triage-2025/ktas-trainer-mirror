using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioColorDTO
  {
    [JsonPropertyName("r")]
    public float R { get; set; }

    [JsonPropertyName("g")]
    public float G { get; set; }

    [JsonPropertyName("b")]
    public float B { get; set; }

    [JsonPropertyName("a")]
    public float A { get; set; }
  }
}
