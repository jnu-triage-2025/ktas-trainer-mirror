using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace MultiplayerInfrastructure.Scenario.Requirements
{
  public static class ScenarioRequirementsWriter
  {
    public static byte[] WriteUtf8(ScenarioRequirementsDocument document)
    {
      if (document == null) throw new ArgumentNullException(nameof(document));
      using var stream = new MemoryStream();
      using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
      {
        WriteDocument(writer, document.DTO);
      }
      var bytes = stream.ToArray();
      var result = new byte[bytes.Length + 1];
      Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
      result[result.Length - 1] = (byte)'\n';
      return result;
    }

    public static string WriteJson(ScenarioRequirementsDocument document)
      => Encoding.UTF8.GetString(WriteUtf8(document));

    private static void WriteDocument(Utf8JsonWriter writer, ScenarioRequirementsDocumentDTO dto)
    {
      writer.WriteStartObject();
      writer.WriteString("format", "scenario-ingame-requirements");
      writer.WriteNumber("schemaVersion", 1);
      writer.WriteString("scenarioIdentifier", dto.ScenarioIdentifier);
      writer.WritePropertyName("source"); writer.WriteStartObject();
      writer.WriteString("scenarioSha256", dto.Source.ScenarioSha256);
      if (!string.IsNullOrWhiteSpace(dto.Source.GeneratedAtUtc)) writer.WriteString("generatedAtUtc", dto.Source.GeneratedAtUtc);
      writer.WriteEndObject();
      writer.WritePropertyName("declarations"); writer.WriteStartArray();
      foreach (var declaration in (dto.Declarations ?? new List<ScenarioRequirementDeclarationDTO>()).OrderBy(DeclarationKey, StringComparer.Ordinal)) WriteDeclaration(writer, declaration);
      writer.WriteEndArray();
      writer.WritePropertyName("suppressions"); writer.WriteStartArray();
      foreach (var suppression in (dto.Suppressions ?? new List<ScenarioRequirementSuppressionDTO>()).OrderBy(value => value.Selector.Kind).ThenBy(value => value.Selector.Identifier, StringComparer.Ordinal))
      {
        writer.WriteStartObject(); WriteSelector(writer, suppression.Selector); writer.WriteString("reason", suppression.Reason); writer.WriteString("owner", suppression.Owner); writer.WriteString("expiresOn", suppression.ExpiresOn); writer.WriteEndObject();
      }
      writer.WriteEndArray(); writer.WriteEndObject();
    }

    private static void WriteDeclaration(Utf8JsonWriter writer, ScenarioRequirementDeclarationDTO value)
    {
      writer.WriteStartObject(); WriteSelector(writer, value.Selector); writer.WriteString("operation", value.Operation.ToString());
      if (value.Capabilities != null && value.Capabilities.Count > 0) { writer.WritePropertyName("capabilities"); writer.WriteStartArray(); foreach (var item in value.Capabilities.Distinct().OrderBy(item => item.ToString(), StringComparer.Ordinal)) writer.WriteStringValue(item.ToString()); writer.WriteEndArray(); }
      if (value.Scope.HasValue) writer.WriteString("scope", value.Scope.Value.ToString());
      if (value.Authority.HasValue) writer.WriteString("authority", value.Authority.Value.ToString());
      if (value.Availability.HasValue) writer.WriteString("availability", value.Availability.Value.ToString());
      if (value.Cardinality != null) { writer.WritePropertyName("cardinality"); writer.WriteStartObject(); if (value.Cardinality.Minimum.HasValue) writer.WriteNumber("minimum", value.Cardinality.Minimum.Value); if (value.Cardinality.Maximum.HasValue) writer.WriteNumber("maximum", value.Cardinality.Maximum.Value); else writer.WriteNull("maximum"); writer.WriteEndObject(); }
      if (value.MustProve.HasValue) writer.WriteBoolean("mustProve", value.MustProve.Value);
      if (value.OccurrenceSelector != null) { writer.WritePropertyName("occurrenceSelector"); writer.WriteStartObject(); writer.WriteString("nodeIdentifier", value.OccurrenceSelector.NodeIdentifier); writer.WriteString("fieldPath", value.OccurrenceSelector.FieldPath); writer.WriteEndObject(); }
      if (value.Binding != null) { writer.WritePropertyName("binding"); writer.WriteStartObject(); writer.WriteString("mode", value.Binding.Mode.ToString()); if (!string.IsNullOrWhiteSpace(value.Binding.FactoryIdentifier)) writer.WriteString("factoryIdentifier", value.Binding.FactoryIdentifier); if (!string.IsNullOrWhiteSpace(value.Binding.ProviderIdentifier)) writer.WriteString("providerIdentifier", value.Binding.ProviderIdentifier); writer.WriteEndObject(); }
      if (value.Configuration != null) { writer.WritePropertyName("configuration"); writer.WriteStartObject(); WriteVector(writer, "position", value.Configuration.Position); WriteVector(writer, "rotationEuler", value.Configuration.RotationEuler); WriteVector(writer, "scale", value.Configuration.Scale); if (!string.IsNullOrWhiteSpace(value.Configuration.ParentBindingKey)) writer.WriteString("parentBindingKey", value.Configuration.ParentBindingKey); writer.WriteEndObject(); }
      if (!string.IsNullOrWhiteSpace(value.Notes)) writer.WriteString("notes", value.Notes);
      writer.WriteEndObject();
    }

    private static void WriteSelector(Utf8JsonWriter writer, ScenarioRequirementSelectorDTO selector)
    { writer.WritePropertyName("selector"); writer.WriteStartObject(); writer.WriteString("kind", selector.Kind.ToString()); writer.WriteString("identifier", selector.Identifier.Trim()); writer.WriteEndObject(); }
    private static string DeclarationKey(ScenarioRequirementDeclarationDTO value) => $"{(int)value.Selector.Kind:D3}\u001f{value.Selector.Identifier.Trim()}\u001f{value.Operation}\u001f{value.OccurrenceSelector?.NodeIdentifier}\u001f{value.OccurrenceSelector?.FieldPath}";
    private static void WriteVector(Utf8JsonWriter writer, string name, ScenarioRequirementVector3DTO value) { if (value == null) return; writer.WritePropertyName(name); writer.WriteStartObject(); writer.WriteNumber("x", value.X); writer.WriteNumber("y", value.Y); writer.WriteNumber("z", value.Z); writer.WriteEndObject(); }
  }
}
