using System;
using UnityEngine;

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
    public static void ForceReload() => _cachedSchemaText = null;
#endif
  }
}
