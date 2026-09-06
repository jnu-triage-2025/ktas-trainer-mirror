using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioInteractionTargetDTO
  {
    [JsonPropertyName("entity")] public ScenarioEntityReferenceDTO Entity { get; set; }
    [JsonPropertyName("interaction")] public string Interaction { get; set; }
  }

  internal sealed class ScenarioInteractionVisibilityNodeDTO : ScenarioNodeDTO
  {
    [JsonPropertyName("operation")] public string Operation { get; set; }
    [JsonPropertyName("targets")] public List<ScenarioInteractionTargetDTO> Targets { get; set; }
    [JsonPropertyName("playerScope")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string PlayerScope { get; set; }
    [JsonPropertyName("playerTags")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string> PlayerTags { get; set; }
    [JsonPropertyName("tagMatchMode")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string TagMatchMode { get; set; }
  }
}
