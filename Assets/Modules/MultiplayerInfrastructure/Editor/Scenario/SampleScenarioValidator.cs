#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using MultiplayerInfrastructure.Scenario;
using System.Text.Json;

public static class SampleScenarioValidator
{
  [MenuItem("TriageTrainer/Multiplayer Infrastructure/Multiplayer Scenario/Validate Sample Scenario")]
  public static void ValidateSampleScenario()
  {
    try
    {
      var json = LoadScenarioJson();

      Debug.Log($"Loaded scenario json length: {json?.Length}");
      Debug.Log(json.Substring(0, Math.Min(json.Length, 200)));
      var graph = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema: true);
      EditorUtility.DisplayDialog(
          "Scenario Validation",
          $"Scenario loaded successfully.\nNode count: {graph.Nodes.Count}",
          "OK");
    }
    catch (Exception ex)
    {
      EditorUtility.DisplayDialog(
          "Scenario Validation Failed",
          ex.Message,
          "Close");
      throw;
    }
    
  }

  private const string ScenarioResourcePath = "Scenario/sample_scenario";

  private static string LoadScenarioJson()
  {
    var textAsset = Resources.Load<TextAsset>(ScenarioResourcePath);
    if (textAsset != null) return textAsset.text;

    throw new FileNotFoundException(
        $"Resources '{ScenarioResourcePath}'에서 sample_scenario.json을 찾을 수 없습니다.");
  }
}
#endif
