using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Json.Schema;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// JSON Schema(Net) 기반 검증 유틸리티.
  /// </summary>
  internal static class ScenarioJsonSchemaValidator
  {
    private static JsonSchema _schema;

#if UNITY_5_3_OR_NEWER
    public static void Validate(string json)
    {
      Validate(
        json,
        ScenarioJsonSchemaProvider.SchemaText,
        nodeType => Enum.TryParse(nodeType, ignoreCase: false, out ScenarioNodeType _),
        interactionType => Enum.TryParse(interactionType, ignoreCase: false, out ScenarioActingNpcInteractionType _));
    }
#endif

    /// <summary>
    /// Unity Resources에 의존하지 않고 시나리오 JSON과 schema text를 검증합니다.
    /// CLI 등 Unity 밖의 호출자는 schema text와 discriminator 값 판별기를 전달합니다.
    /// </summary>
    internal static void Validate(
      string json,
      string schemaText,
      Func<string, bool> isKnownNodeType,
      Func<string, bool> isKnownInteractionType)
    {
      if (string.IsNullOrWhiteSpace(json))
      {
        throw new ScenarioSchemaValidationException("Scenario JSON text is null or empty.");
      }

      EnsureSchemaLoaded(schemaText);

      JsonDocument document;
      try
      {
        StrictJsonPropertyValidator.RejectDuplicateProperties(json);
        document = JsonDocument.Parse(json);
      }
      catch (JsonException ex)
      {
        throw new ScenarioSchemaValidationException("Scenario JSON could not be parsed.", ex);
      }

      using (document)
      {
        var result = _schema.Evaluate(
            document.RootElement,
            new EvaluationOptions
            {
              OutputFormat = OutputFormat.List,
              RequireFormatValidation = false
            });

        if (result.IsValid)
        {
          return;
        }

        // JsonSchema.Net 은 해당 `then` 분기가 적용되지 않더라도 실패한 `if`
        // 조건의 상세를 List 출력에 포함한다. ScenarioNode 는 이 조건들로
        // nodeType 별 분기를 처리하므로, 이 필터가 없으면 모든 유효한 노드가
        // *다른* 모든 노드 타입으로도 보고된다. 실제 required/property/type
        // 오류는 모두 유지하고, 분기 조건 흔적만 무시한다.
        var actionableErrors = result.Details
          .Where(detail => detail.Errors != null && detail.Errors.Any())
          .SelectMany(detail => detail.Errors.Select(error => new
          {
            Location = detail.InstanceLocation.ToString(),
            Message = error.Value
          }))
          .Where(error => !IsNonApplicableDiscriminatorPredicateError(
            document.RootElement,
            error.Location,
            error.Message,
            isKnownNodeType,
            isKnownInteractionType))
          .ToList();

        if (actionableErrors.Count == 0)
          return;

        var builder = new StringBuilder();
        builder.AppendLine("Scenario JSON failed schema validation:");
        foreach (var error in actionableErrors)
        {
          builder.Append(" • ")
                 .Append(error.Location)
                 .Append(": ")
                 .AppendLine(error.Message);
        }

        throw new ScenarioSchemaValidationException(builder.ToString());
      }
    }

    private static bool IsNonApplicableDiscriminatorPredicateError(
      JsonElement root,
      string instanceLocation,
      string message,
      Func<string, bool> isKnownNodeType,
      Func<string, bool> isKnownInteractionType)
    {
      if (string.IsNullOrEmpty(instanceLocation) || string.IsNullOrEmpty(message))
        return false;

      if (instanceLocation.StartsWith("/nodes/", StringComparison.Ordinal)
          && instanceLocation.EndsWith("/nodeType", StringComparison.Ordinal))
      {
        return IsKnownNodeTypePredicateError(root, instanceLocation, message, isKnownNodeType);
      }

      if (instanceLocation.StartsWith("/nodes/", StringComparison.Ordinal)
          && IsNonApplicableNodeAlternativeError(root, instanceLocation, message))
      {
        return true;
      }

      if (instanceLocation.StartsWith("/actingNpcs/", StringComparison.Ordinal)
          && instanceLocation.EndsWith("/interactionType", StringComparison.Ordinal))
      {
        if (message.StartsWith("Expected ", StringComparison.Ordinal))
          return true;
        if (!message.StartsWith("Value should match one of the values specified by the enum", StringComparison.Ordinal))
          return false;
        return TryResolveStringAtPointer(root, instanceLocation, out var interactionType)
               && isKnownInteractionType != null
               && isKnownInteractionType(interactionType);
      }

      return false;
    }

    private static bool IsNonApplicableNodeAlternativeError(
      JsonElement root,
      string instanceLocation,
      string message)
    {
      if (!TryResolveElementAtPointer(root, instanceLocation, out var node)
          || node.ValueKind != JsonValueKind.Object)
      {
        int propertySeparator = instanceLocation.LastIndexOf('/');
        if (propertySeparator <= "/nodes/".Length
            || !TryResolveElementAtPointer(root, instanceLocation.Substring(0, propertySeparator), out node)
            || node.ValueKind != JsonValueKind.Object)
          return false;
      }

      bool HasString(string name) => node.TryGetProperty(name, out var value)
                                     && value.ValueKind == JsonValueKind.String
                                     && !string.IsNullOrWhiteSpace(value.GetString());

      if (HasString("presetIdentifier")
          && message.Contains("actingNpcIdentifier", StringComparison.Ordinal))
        return true;
      if (HasString("targetEntityIdentifier")
          && message.Contains("targetEntityStateKey", StringComparison.Ordinal))
        return true;
      if (HasString("questDefinitionIdentifier")
          && message.Contains("quest", StringComparison.Ordinal))
        return true;
      if (HasString("actingNpcIdentifier")
          && message.Contains("presetIdentifier", StringComparison.Ordinal))
        return true;
      if (node.TryGetProperty("mode", out var mode)
          && mode.ValueKind == JsonValueKind.String
          && mode.GetString() == "Control"
          && (message.Contains("interactOperation", StringComparison.Ordinal)
              || message.Contains("Expected \"Update\"", StringComparison.Ordinal)
              || message.Contains("destinationType", StringComparison.Ordinal)
              || message.Contains("moveMode", StringComparison.Ordinal)))
        return true;
      if (node.TryGetProperty("transitionMode", out var transitionMode)
          && transitionMode.ValueKind == JsonValueKind.String
          && transitionMode.GetString() != "Gradual"
          && message.Contains("Gradual", StringComparison.Ordinal))
        return true;

      return false;
    }

    private static bool IsKnownNodeTypePredicateError(
      JsonElement root,
      string instanceLocation,
      string message,
      Func<string, bool> isKnownNodeType)
    {
      if (message.StartsWith("Expected ", StringComparison.Ordinal))
        return true;
      if (!message.StartsWith("Value should match one of the values specified by the enum", StringComparison.Ordinal))
        return false;

      return TryResolveStringAtPointer(root, instanceLocation, out var nodeType)
             && isKnownNodeType != null
             && isKnownNodeType(nodeType);
    }

    private static bool TryResolveStringAtPointer(JsonElement root, string pointer, out string value)
    {
      value = null;
      var current = root;
      foreach (var rawSegment in pointer.Split('/').Skip(1))
      {
        var segment = rawSegment.Replace("~1", "/").Replace("~0", "~");
        if (current.ValueKind == JsonValueKind.Object)
        {
          if (!current.TryGetProperty(segment, out current))
            return false;
        }
        else if (current.ValueKind == JsonValueKind.Array
                 && int.TryParse(segment, out var index)
                 && index >= 0
                 && index < current.GetArrayLength())
        {
          current = current[index];
        }
        else
        {
          return false;
        }
      }

      if (current.ValueKind != JsonValueKind.String)
        return false;
      value = current.GetString();
      return true;
    }

    private static bool TryResolveElementAtPointer(JsonElement root, string pointer, out JsonElement value)
    {
      value = root;
      foreach (var rawSegment in pointer.Split('/').Skip(1))
      {
        var segment = rawSegment.Replace("~1", "/").Replace("~0", "~");
        if (value.ValueKind == JsonValueKind.Object)
        {
          if (!value.TryGetProperty(segment, out value))
            return false;
        }
        else if (value.ValueKind == JsonValueKind.Array
                 && int.TryParse(segment, out var index)
                 && index >= 0
                 && index < value.GetArrayLength())
        {
          value = value[index];
        }
        else
        {
          return false;
        }
      }

      return true;
    }

    private static void EnsureSchemaLoaded(string schemaText)
    {
      if (_schema != null)
      {
        return;
      }

      try
      {
        // JsonSchema.FromText() 는 기본적으로 SchemaRegistry.Global 을 사용한다.
        // 에디터는 에셋 갱신 뒤에 이 스키마를 의도적으로 다시 만들므로, 같은 $id 를
        // 전역에 등록하면 다음 그래프 열기/저장에서 실패한다. 새 로컬 레지스트리를
        // 쓰면 재생성을 격리하면서도 스키마가 자체 내부 리소스와 앵커를 등록할 수 있다.
        _schema = JsonSchema.FromText(
          schemaText,
          new BuildOptions { SchemaRegistry = new SchemaRegistry() });
      }
      catch (Exception ex)
      {
        throw new InvalidOperationException($"Failed to parse scenario schema text: {ex.Message}", ex);
      }
    }

#if UNITY_EDITOR
    internal static void ClearCachedSchema() => _schema = null;
#endif
  }
}
