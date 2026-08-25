using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioEntityInitNodeDTO : ScenarioNodeDTO
  {
    // 대상 결정: 프리셋 스폰
    [JsonPropertyName("presetIdentifier")]
    public string PresetIdentifier { get; set; }

    [JsonPropertyName("positionSourceEntityIdentifier")]
    public string PositionSourceEntityIdentifier { get; set; }

    [JsonPropertyName("positionX")]
    public float? PositionX { get; set; }

    [JsonPropertyName("positionY")]
    public float? PositionY { get; set; }

    [JsonPropertyName("positionZ")]
    public float? PositionZ { get; set; }

    // 대상 결정: 기존 엔티티 참조
    [JsonPropertyName("targetEntityIdentifier")]
    public string TargetEntityIdentifier { get; set; }

    [JsonPropertyName("targetEntityStateKey")]
    public string TargetEntityStateKey { get; set; }

    // 식별자 제어
    [JsonPropertyName("entityIdentifier")]
    public string EntityIdentifier { get; set; }

    [JsonPropertyName("resultStateKey")]
    public string ResultStateKey { get; set; }

    // 초기 상태
    [JsonPropertyName("stateOperations")]
    public List<ScenarioEntityStateOperationDTO> StateOperations { get; set; }
  }
}
