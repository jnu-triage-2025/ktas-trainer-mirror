using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioPlayerTagNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("operation")]
    public string Operation { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; }

    /// <summary>Add / Remove 용.</summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

    /// <summary>Change 용 원본 태그.</summary>
    [JsonPropertyName("fromTag")]
    public string FromTag { get; set; }

    /// <summary>Change 용 대상 태그.</summary>
    [JsonPropertyName("toTag")]
    public string ToTag { get; set; }
  }
}
