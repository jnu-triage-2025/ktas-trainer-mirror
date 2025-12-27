using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 JSON 스키마(TextAsset)를 로드하고 캐시합니다.
  /// </summary>
  internal static class ScenarioJsonSchemaProvider
  {
    /// <summary>
    /// Resources 폴더 기준 경로 (확장자 제외).
    /// 예: Assets/Resources/Scenario/scenario_schema.json → "Scenario/scenario_schema"
    /// </summary>
    private const string SchemaResourcePath = "Schema/scenario.schema";

    private static string _cachedSchemaText;

    public static string SchemaText => _cachedSchemaText ??= LoadSchemaText();

    private static string LoadSchemaText()
    {
      var asset = Resources.Load<TextAsset>(SchemaResourcePath);
      if (asset == null)
      {
        throw new InvalidOperationException(
            $"Scenario schema TextAsset not found at Resources/{SchemaResourcePath}.json");
      }

      return asset.text;
    }

#if UNITY_EDITOR
    public static void ForceReload() => _cachedSchemaText = null;
#endif
  }
}
