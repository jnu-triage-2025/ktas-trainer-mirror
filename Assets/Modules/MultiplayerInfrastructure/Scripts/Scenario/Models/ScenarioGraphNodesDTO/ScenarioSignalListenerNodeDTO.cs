using System.Collections.Generic;
using System.Text.Json.Serialization;
namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioSignalListenerNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("listenerIdentifier")] public string ListenerIdentifier { get; set; }
    [JsonPropertyName("operation")] public string Operation { get; set; }
    [JsonPropertyName("sourceSignalIdentifier")] public string SourceSignalIdentifier { get; set; }
    [JsonPropertyName("outputSignalIdentifier")] public string OutputSignalIdentifier { get; set; }
    [JsonPropertyName("requiredSignalIdentifiers")] public List<string> RequiredSignalIdentifiers { get; set; }
    [JsonPropertyName("consumeOnce")] public bool? ConsumeOnce { get; set; }
  }
}
