#if UNITY_EDITOR
using System;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEngine;

public class ScenarioValidatorWindow : EditorWindow
{
  private const string ExampleScenarioResourcePath = "Scenario/sample_scenario.scenario";

  [SerializeField] private TextAsset scenarioAsset;
  [SerializeField] private bool validateWithSchema = true;

  public static void OpenWindow()
  {
    var window = GetWindow<ScenarioValidatorWindow>("Scenario Validator");
    window.minSize = new Vector2(360f, 220f);
    window.Show();
  }

  private void OnGUI()
  {
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Scenario Validator", EditorStyles.boldLabel);
    EditorGUILayout.HelpBox(
        "검증할 시나리오 TextAsset을 선택한 뒤 Validate 버튼을 눌러 주세요.",
        MessageType.Info);

    using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
    {
      scenarioAsset = (TextAsset)EditorGUILayout.ObjectField(
          new GUIContent("Scenario TextAsset"),
          scenarioAsset,
          typeof(TextAsset),
          false);

      validateWithSchema = EditorGUILayout.Toggle(
          new GUIContent("Validate With Schema"),
          validateWithSchema);

      if (GUILayout.Button("Select Example Scenario", GUILayout.Height(24f)))
      {
        LoadExampleScenarioAsset();
      }
    }

    EditorGUILayout.Space();

    using (new EditorGUI.DisabledScope(scenarioAsset == null))
    {
      if (GUILayout.Button("Validate Scenario", GUILayout.Height(32f)))
      {
        ValidateScenario();
      }
    }

    if (scenarioAsset == null)
    {
      EditorGUILayout.HelpBox(
          "시나리오 TextAsset을 선택해야 검증을 실행할 수 있습니다.",
          MessageType.Warning);
    }
  }

  private void LoadExampleScenarioAsset()
  {
    var asset = Resources.Load<TextAsset>(ExampleScenarioResourcePath);
    if (asset != null)
    {
      scenarioAsset = asset;
      EditorGUIUtility.PingObject(asset);
    }
    else
    {
      EditorUtility.DisplayDialog(
          "Example Scenario",
          $"Resources에서 \"{ExampleScenarioResourcePath}\"를 찾지 못했습니다.",
          "확인");
    }
  }

  private void ValidateScenario()
  {
    try
    {
      var json = LoadScenarioJson();
      var graph = ScenarioGraphLoader.LoadFromJson(json, validateWithSchema);

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

  private string LoadScenarioJson()
  {
    if (scenarioAsset == null)
    {
      throw new InvalidOperationException("검증할 TextAsset이 선택되지 않았습니다.");
    }

    return scenarioAsset.text;
  }
}
#endif
