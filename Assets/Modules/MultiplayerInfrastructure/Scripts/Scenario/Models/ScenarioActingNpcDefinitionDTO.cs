using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioActingNpcDefinitionDTO
  {
    [JsonPropertyName("identifier")] public string Identifier { get; set; }
    [JsonPropertyName("actingNpcType")] public string ActingNpcType { get; set; }
    [JsonPropertyName("presetIdentifier")] public string PresetIdentifier { get; set; }
    [JsonPropertyName("displayName")] public string DisplayName { get; set; }
    [JsonPropertyName("showOverheadName")] public bool? ShowOverheadName { get; set; }
    [JsonPropertyName("positionX")] public float? PositionX { get; set; }
    [JsonPropertyName("positionY")] public float? PositionY { get; set; }
    [JsonPropertyName("positionZ")] public float? PositionZ { get; set; }
    [JsonPropertyName("rotationX")] public float? RotationX { get; set; }
    [JsonPropertyName("rotationY")] public float? RotationY { get; set; }
    [JsonPropertyName("rotationZ")] public float? RotationZ { get; set; }
    [JsonPropertyName("spawnOnStart")] public bool? SpawnOnStart { get; set; }
    [JsonPropertyName("despawnOnScenarioEnd")] public bool? DespawnOnScenarioEnd { get; set; }
    [JsonPropertyName("interactions")] public List<ScenarioActingNpcInteractionDefinitionDTO> Interactions { get; set; }
  }

  internal sealed class ScenarioActingNpcInteractionDefinitionDTO
  {
    [JsonPropertyName("identifier")] public string Identifier { get; set; }
    [JsonPropertyName("interactionType")] public string InteractionType { get; set; }
    [JsonPropertyName("displayText")] public string DisplayText { get; set; }
    [JsonPropertyName("iconIdentifier")] public string IconIdentifier { get; set; }
    [JsonPropertyName("scenarioIdentifier")] public string ScenarioIdentifier { get; set; }
    [JsonPropertyName("scenarioStartNodeIdentifier")] public string ScenarioStartNodeIdentifier { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; }
    [JsonPropertyName("submitButtonText")] public string SubmitButtonText { get; set; }
    [JsonPropertyName("requiredItems")] public List<ScenarioActingNpcItemRequirementDTO> RequiredItems { get; set; }
    [JsonPropertyName("completionSignalIdentifier")] public string CompletionSignalIdentifier { get; set; }
    [JsonPropertyName("consumeOnce")] public bool? ConsumeOnce { get; set; }
    [JsonPropertyName("enabled")] public bool? Enabled { get; set; }
  }

  internal sealed class ScenarioActingNpcItemRequirementDTO
  {
    [JsonPropertyName("itemIdentifier")] public string ItemIdentifier { get; set; }
    [JsonPropertyName("count")] public int? Count { get; set; }
  }
}
