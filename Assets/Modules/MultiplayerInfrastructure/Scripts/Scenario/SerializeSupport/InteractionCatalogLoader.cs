using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiplayerInfrastructure.InteractableEntity;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 밖에서도 존재해야 하는 인터렉션 정의(데모 NPC 의 시나리오 시작, 튜토리얼 미끼 등)를
  /// <c>Resources/Interactions/*.json</c> 에서 읽어 레지스트리에 상시 출처(<see cref="GlobalSourceIdentifier"/>)로 적용한다.
  /// 파일 형식은 시나리오 JSON 의 interactions 구역과 같다: <c>{ "interactions": [ ... ] }</c>.
  /// </summary>
  public static class InteractionCatalogLoader
  {
    public const string GlobalSourceIdentifier = "global";
    public const string ResourceFolder = "Interactions";

    private sealed class CatalogDTO
    {
      [JsonPropertyName("interactions")] public List<ScenarioInteractionDefinitionDTO> Interactions { get; set; }
    }

    private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true,
      ReadCommentHandling = JsonCommentHandling.Skip,
      AllowTrailingCommas = true
    };

    private static bool _loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void LoadOnStartup()
    {
      _loaded = false;
      EnsureLoaded();
    }

    /// <summary>Resources/Interactions 의 모든 카탈로그를 한 번 읽어 레지스트리에 적용한다.</summary>
    public static void EnsureLoaded()
    {
      if (_loaded)
        return;
      _loaded = true;

      var definitions = new List<InteractionDefinition>();
      foreach (var asset in Resources.LoadAll<TextAsset>(ResourceFolder))
      {
        if (asset == null || string.IsNullOrWhiteSpace(asset.text))
          continue;
        try
        {
          definitions.AddRange(Parse(asset.text, asset.name));
        }
        catch (Exception ex)
        {
          Debug.LogError($"[InteractionCatalogLoader] Failed to load '{asset.name}': {ex.Message}");
        }
      }

      using (InteractionRegistry.BeginScenarioInitCycle(GlobalSourceIdentifier))
        InteractionRegistry.ApplyScenarioDefinitions(GlobalSourceIdentifier, definitions);
    }

    /// <summary>카탈로그 JSON 을 정의 목록으로 변환한다(테스트·도구용).</summary>
    public static IReadOnlyList<InteractionDefinition> Parse(string json, string context = "catalog")
    {
      var dto = JsonSerializer.Deserialize<CatalogDTO>(json, SerializerOptions);
      return ScenarioInteractionDefinitionConverter.ConvertInteractions(context, dto?.Interactions, null);
    }

    /// <summary>다시 읽게 한다(에디터 도구용).</summary>
    public static void Invalidate() => _loaded = false;
  }
}
