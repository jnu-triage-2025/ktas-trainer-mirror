using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary><see cref="ScenarioEntityStateSignalBindingNode"/> 의 JSON 직렬화 DTO.</summary>
  internal sealed class ScenarioEntityStateSignalBindingNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("bindingIdentifier")] public string BindingIdentifier { get; set; }
    [JsonPropertyName("operation")] public string Operation { get; set; }
    [JsonPropertyName("targetEntityIdentifier")] public string TargetEntityIdentifier { get; set; }
    [JsonPropertyName("targetEntityStateKey")] public string TargetEntityStateKey { get; set; }
    [JsonPropertyName("eventName")] public string EventName { get; set; }
    [JsonPropertyName("eventKey")] public string EventKey { get; set; }
    [JsonPropertyName("outputSignalIdentifier")] public string OutputSignalIdentifier { get; set; }
    [JsonPropertyName("consumeOnce")] public bool? ConsumeOnce { get; set; }
  }
}
