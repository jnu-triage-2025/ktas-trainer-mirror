using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// <see cref="ScenarioEntityInitNodeDTO"/> 의 초기 상태 항목 DTO.
  /// </summary>
  internal sealed class ScenarioEntityStateOperationDTO
  {
    /// <summary>"StateStore" 또는 "DisplayState"(기본값 DisplayState).</summary>
    [JsonPropertyName("kind")]
    public string Kind { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }

    /// <summary>DisplayState 종류에서 표시(true)/비표시(false). 기본값 true.</summary>
    [JsonPropertyName("displayActive")]
    public bool? DisplayActive { get; set; }
  }
}
