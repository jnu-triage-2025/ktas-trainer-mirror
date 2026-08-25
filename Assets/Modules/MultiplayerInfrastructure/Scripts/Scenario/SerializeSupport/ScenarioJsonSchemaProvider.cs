using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MultiplayerInfrastructure.Scenario
{
  internal static class ScenarioJsonSchemaProvider
  {
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
    public static void ForceReload()
    {
      _cachedSchemaText = null;
      ScenarioJsonSchemaValidator.ClearCachedSchema();

      // 브랜치 전환 직후에도 이전 import 결과를 사용하지 않도록 스키마 asset을
      // 강제 재임포트한 뒤 AssetDatabase에서 최신 TextAsset을 다시 읽는다.
      var schemaAsset = Resources.Load<TextAsset>(SchemaResourcePath);
      var assetPath = schemaAsset == null ? null : AssetDatabase.GetAssetPath(schemaAsset);
      if (!string.IsNullOrEmpty(assetPath))
      {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        schemaAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        _cachedSchemaText = schemaAsset?.text;
      }
    }
#endif
  }
}
