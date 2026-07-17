using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  internal sealed class ScenarioRequirementsDocumentDTO
  {
    [JsonPropertyName("format")] public string Format { get; set; }
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
    [JsonPropertyName("scenarioIdentifier")] public string ScenarioIdentifier { get; set; }
    [JsonPropertyName("source")] public ScenarioRequirementsSourceDTO Source { get; set; }
    [JsonPropertyName("declarations")] public List<ScenarioRequirementDeclarationDTO> Declarations { get; set; }
    [JsonPropertyName("suppressions")] public List<ScenarioRequirementSuppressionDTO> Suppressions { get; set; }
  }
  internal sealed class ScenarioRequirementsSourceDTO { [JsonPropertyName("scenarioSha256")] public string ScenarioSha256 { get; set; } [JsonPropertyName("generatedAtUtc")] public string GeneratedAtUtc { get; set; } }
  internal sealed class ScenarioRequirementSelectorDTO { [JsonPropertyName("kind")] public ScenarioRequirementKind Kind { get; set; } [JsonPropertyName("identifier")] public string Identifier { get; set; } }
  internal sealed class ScenarioRequirementOccurrenceSelectorDTO { [JsonPropertyName("nodeIdentifier")] public string NodeIdentifier { get; set; } [JsonPropertyName("fieldPath")] public string FieldPath { get; set; } }
  internal sealed class ScenarioRequirementCardinalityDTO { [JsonPropertyName("minimum")] public int? Minimum { get; set; } [JsonPropertyName("maximum")] public int? Maximum { get; set; } }
  internal sealed class ScenarioRequirementBindingDTO { [JsonPropertyName("mode")] public ScenarioRequirementBindingMode Mode { get; set; } [JsonPropertyName("factoryIdentifier")] public string FactoryIdentifier { get; set; } [JsonPropertyName("providerIdentifier")] public string ProviderIdentifier { get; set; } }
  internal sealed class ScenarioRequirementVector3DTO { [JsonPropertyName("x")] public double X { get; set; } [JsonPropertyName("y")] public double Y { get; set; } [JsonPropertyName("z")] public double Z { get; set; } }
  internal sealed class ScenarioRequirementConfigurationDTO { [JsonPropertyName("position")] public ScenarioRequirementVector3DTO Position { get; set; } [JsonPropertyName("rotationEuler")] public ScenarioRequirementVector3DTO RotationEuler { get; set; } [JsonPropertyName("scale")] public ScenarioRequirementVector3DTO Scale { get; set; } [JsonPropertyName("parentBindingKey")] public string ParentBindingKey { get; set; } }
  internal sealed class ScenarioRequirementDeclarationDTO
  {
    [JsonPropertyName("selector")] public ScenarioRequirementSelectorDTO Selector { get; set; }
    [JsonPropertyName("operation")] public ScenarioRequirementDeclarationOperation Operation { get; set; }
    [JsonPropertyName("capabilities")] public List<ScenarioRequirementCapability> Capabilities { get; set; }
    [JsonPropertyName("scope")] public ScenarioRequirementScope? Scope { get; set; }
    [JsonPropertyName("authority")] public ScenarioRequirementAuthority? Authority { get; set; }
    [JsonPropertyName("availability")] public ScenarioRequirementAvailability? Availability { get; set; }
    [JsonPropertyName("cardinality")] public ScenarioRequirementCardinalityDTO Cardinality { get; set; }
    [JsonPropertyName("mustProve")] public bool? MustProve { get; set; }
    [JsonPropertyName("occurrenceSelector")] public ScenarioRequirementOccurrenceSelectorDTO OccurrenceSelector { get; set; }
    [JsonPropertyName("binding")] public ScenarioRequirementBindingDTO Binding { get; set; }
    [JsonPropertyName("configuration")] public ScenarioRequirementConfigurationDTO Configuration { get; set; }
    [JsonPropertyName("notes")] public string Notes { get; set; }
  }
  internal sealed class ScenarioRequirementSuppressionDTO { [JsonPropertyName("selector")] public ScenarioRequirementSelectorDTO Selector { get; set; } [JsonPropertyName("reason")] public string Reason { get; set; } [JsonPropertyName("owner")] public string Owner { get; set; } [JsonPropertyName("expiresOn")] public string ExpiresOn { get; set; } }

  internal sealed class ScenarioRequirementCandidatesDTO { [JsonPropertyName("format")] public string Format { get; set; } [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; } [JsonPropertyName("scenarioIdentifier")] public string ScenarioIdentifier { get; set; } [JsonPropertyName("candidates")] public List<ScenarioRequirementCandidateDTO> Candidates { get; set; } }
  internal sealed class ScenarioRequirementCandidateDTO { [JsonPropertyName("kind")] public ScenarioRequirementKind Kind { get; set; } [JsonPropertyName("identifier")] public string Identifier { get; set; } [JsonPropertyName("capabilities")] public List<ScenarioRequirementCapability> Capabilities { get; set; } [JsonPropertyName("evidence")] public List<ScenarioRequirementEvidenceDTO> Evidence { get; set; } [JsonPropertyName("suggestedBinding")] public ScenarioRequirementSuggestedBindingDTO SuggestedBinding { get; set; } [JsonPropertyName("confidence")] public double Confidence { get; set; } [JsonPropertyName("reviewRequired")] public bool ReviewRequired { get; set; } [JsonPropertyName("notes")] public string Notes { get; set; } }
  internal sealed class ScenarioRequirementEvidenceDTO { [JsonPropertyName("nodeIdentifier")] public string NodeIdentifier { get; set; } [JsonPropertyName("nodeType")] public string NodeType { get; set; } [JsonPropertyName("fieldPath")] public string FieldPath { get; set; } }
  internal sealed class ScenarioRequirementSuggestedBindingDTO { [JsonPropertyName("mode")] public ScenarioRequirementBindingMode Mode { get; set; } [JsonPropertyName("factoryIdentifier")] public string FactoryIdentifier { get; set; } [JsonPropertyName("scope")] public ScenarioRequirementScope? Scope { get; set; } [JsonPropertyName("authority")] public ScenarioRequirementAuthority? Authority { get; set; } }
}
