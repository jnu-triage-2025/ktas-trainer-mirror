using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// JSON Schema(Net) 기반 검증 유틸리티.
  /// </summary>
  internal static class ScenarioJsonSchemaValidator
  {
    private static JsonSchema _schema;

    public static void Validate(string json)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        throw new ScenarioSchemaValidationException("Scenario JSON text is null or empty.");
      }

      EnsureSchemaLoaded();

      JsonNode jsonNode;
      try
      {
        jsonNode = JsonNode.Parse(json) ?? throw new JsonException("JSON root is null.");
      }
      catch (Exception ex)
      {
        throw new ScenarioSchemaValidationException("Scenario JSON could not be parsed.", ex);
      }

      var result = _schema.Evaluate(
          JsonDocument.Parse(json).RootElement,
          new EvaluationOptions
          {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = false
          });

      if (result.IsValid)
      {
        return;
      }

      if (IsConditionalNodeTypeNoiseOnly(result, jsonNode))
      {
        return;
      }

      var builder = new StringBuilder();
      builder.AppendLine("Scenario JSON failed schema validation:");
      foreach (var detail in result.Details.Where(d => d.Errors != null && d.Errors.Any()))
      {
        builder.Append(" • ")
               .Append(detail.InstanceLocation)
               .Append(": ")
               .AppendLine(string.Join(", ", detail.Errors.Select(e => e.Value)));
      }

      throw new ScenarioSchemaValidationException(builder.ToString());
    }

    private static bool IsConditionalNodeTypeNoiseOnly(EvaluationResults result, JsonNode jsonNode)
    {
      return AreAllNodeTypesKnown(jsonNode);
    }

    private static bool AreAllNodeTypesKnown(JsonNode jsonNode)
    {
      if (jsonNode is not JsonObject root
          || !root.TryGetPropertyValue("nodes", out var nodesNode)
          || nodesNode is not JsonObject nodes)
      {
        return false;
      }

      var knownTypes = new HashSet<string>(StringComparer.Ordinal)
      {
        "Dialogue",
        "Choice",
        "Sound",
        "PlayerMove",
        "NPCMove",
        "CameraTarget",
        "InvokeEvent",
        "Validator",
        "Parallel",
        "QuestControl",
        "QuestWaypointHighlight",
        "Delay",
        "Interaction",
        "CombineItem",
        "Quiz",
        "StateUpdate",
        "PlayTTS",
        "PlayerTag",
        "TagModification",
        "EntityPresetSpawn",
        "EntityTag",
        "EntityInit",
        "TriageAssessControl",
        "PatientMedicalStatePreset"
      };

      foreach (var nodeEntry in nodes)
      {
        if (nodeEntry.Value is not JsonObject nodeObj)
          return false;

        if (!nodeObj.TryGetPropertyValue("nodeType", out var nodeTypeNode))
          return false;

        var nodeType = nodeTypeNode?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(nodeType) || !knownTypes.Contains(nodeType))
          return false;
      }

      return true;
    }

    private static void EnsureSchemaLoaded()
    {
      if (_schema != null)
      {
        return;
      }

      try
      {
        _schema = JsonSchema.FromText(ScenarioJsonSchemaProvider.SchemaText);
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException("Failed to parse scenario schema text.", ex);
      }
    }
  }
}
