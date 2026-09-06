using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioEntityReferenceDTO
  {
    [JsonPropertyName("id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Identifier { get; set; }

    [JsonPropertyName("tag")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Tag { get; set; }
  }

  internal sealed class ScenarioConditionDTO
  {
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("negate")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? Negate { get; set; }
    [JsonPropertyName("tag")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Tag { get; set; }
    [JsonPropertyName("flag")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Flag { get; set; }
    [JsonPropertyName("questIdentifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string QuestIdentifier { get; set; }
    [JsonPropertyName("questState")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string QuestState { get; set; }
    [JsonPropertyName("completionCriteriaIdentifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string CompletionCriteriaIdentifier { get; set; }
    [JsonPropertyName("itemIdentifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string ItemIdentifier { get; set; }
    [JsonPropertyName("count")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Count { get; set; }
    [JsonPropertyName("key")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Key { get; set; }
    [JsonPropertyName("qualifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Qualifier { get; set; }
    [JsonPropertyName("compare")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Compare { get; set; }
    [JsonPropertyName("value")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Value { get; set; }
    [JsonPropertyName("entity")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ScenarioEntityReferenceDTO Entity { get; set; }
    [JsonPropertyName("meters")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public float? Meters { get; set; }
    [JsonPropertyName("signal")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Signal { get; set; }
    [JsonPropertyName("registryType")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string RegistryType { get; set; }
    [JsonPropertyName("identifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Identifier { get; set; }
    [JsonPropertyName("scenarioIdentifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string ScenarioIdentifier { get; set; }
    [JsonPropertyName("matchMode")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string MatchMode { get; set; }
    [JsonPropertyName("conditions")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<ScenarioConditionDTO> Conditions { get; set; }
  }
}
