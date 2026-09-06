using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario
{
  internal sealed class ScenarioInteractionDisplayDTO
  {
    [JsonPropertyName("text")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Text { get; set; }
    [JsonPropertyName("iconIdentifiers")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string> IconIdentifiers { get; set; }
    [JsonPropertyName("color")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ScenarioColorDTO Color { get; set; }
    [JsonPropertyName("allowIconFallback")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? AllowIconFallback { get; set; }
    [JsonPropertyName("priority")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Priority { get; set; }
  }

  internal sealed class ScenarioInteractionItemRequirementDTO
  {
    [JsonPropertyName("itemIdentifier")] public string ItemIdentifier { get; set; }
    [JsonPropertyName("count")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public int? Count { get; set; }
  }

  internal sealed class ScenarioInteractionSubmissionDTO
  {
    [JsonPropertyName("title")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Title { get; set; }
    [JsonPropertyName("submitButtonText")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string SubmitButtonText { get; set; }
    [JsonPropertyName("requiredItems")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<ScenarioInteractionItemRequirementDTO> RequiredItems { get; set; }
  }

  internal sealed class ScenarioInteractionVisibilityDTO
  {
    [JsonPropertyName("initial")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? Initial { get; set; }
    [JsonPropertyName("matchMode")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string MatchMode { get; set; }
    [JsonPropertyName("conditions")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<ScenarioConditionDTO> Conditions { get; set; }
  }

  internal sealed class ScenarioInteractionDefinitionDTO
  {
    [JsonPropertyName("entity")] public ScenarioEntityReferenceDTO Entity { get; set; }
    [JsonPropertyName("interaction")] public string Interaction { get; set; }
    [JsonPropertyName("kind")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string Kind { get; set; }
    [JsonPropertyName("handlerKey")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string HandlerKey { get; set; }
    [JsonPropertyName("display")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ScenarioInteractionDisplayDTO Display { get; set; }
    [JsonPropertyName("completionSignal")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string CompletionSignal { get; set; }
    [JsonPropertyName("afterInteract")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string AfterInteract { get; set; }
    [JsonPropertyName("requiredItems")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<ScenarioInteractionItemRequirementDTO> RequiredItems { get; set; }
    [JsonPropertyName("consumeItems")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<ScenarioInteractionItemRequirementDTO> ConsumeItems { get; set; }
    [JsonPropertyName("extras")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public Dictionary<string, string> Extras { get; set; }
    [JsonPropertyName("itemSubmission")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ScenarioInteractionSubmissionDTO ItemSubmission { get; set; }
    [JsonPropertyName("scenarioIdentifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string ScenarioIdentifier { get; set; }
    [JsonPropertyName("startNodeIdentifier")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public string StartNodeIdentifier { get; set; }
    [JsonPropertyName("activateObjects")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string> ActivateObjects { get; set; }
    [JsonPropertyName("deactivateObjects")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public List<string> DeactivateObjects { get; set; }
    [JsonPropertyName("visibility")] [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public ScenarioInteractionVisibilityDTO Visibility { get; set; }
  }
}
