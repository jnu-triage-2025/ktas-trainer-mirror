#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerInfrastructure.ItemSystem;
using TriageTrainer.ItemDefinitions;
using TriageTrainer.Items;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  public sealed class RegistryPreloaderValidationPanel : EditorWindow
  {
    private enum ValidationTarget
    {
      ScenarioGraph,
      IconSprite,
      Npc,
      Waypoint,
      Entity,
      InteractableEntity,
      UiController,
      TriageItemRegistrar
    }

    [SerializeField] private ValidationTarget selectedTarget;
    [SerializeField] private bool validateScenarioWithSchema = true;

    [SerializeField] private RegistryPreloadScenarioGraphSO scenarioGraphSO;
    [SerializeField] private RegistryPreloadIconSpriteSO iconSpriteSO;
    [SerializeField] private RegistryPreloadNpcSO npcSO;
    [SerializeField] private RegistryPreloadWaypointSO waypointSO;
    [SerializeField] private RegistryPreloadEntitySO entitySO;
    [SerializeField] private RegistryPreloadInteractableEntitySO interactableEntitySO;
    [SerializeField] private RegistryPreloadUIControllerSO uiControllerSO;

    [MenuItem("Tools/Multiplayer Infrastructure/Registry Preloader: Validate Registering Resources")]
    private static void Open()
    {
      var window = GetWindow<RegistryPreloaderValidationPanel>("Preloader Validation");
      window.minSize = new Vector2(520f, 260f);
      window.Show();
    }

    private void OnGUI()
    {
      EditorGUILayout.LabelField("Registry Preloader Validation Panel", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "Registry 등록에 사용하는 ScriptableObject 타입을 고른 뒤, 해당 SO를 지정하고 Validate를 실행하세요.",
        MessageType.Info);

      selectedTarget = (ValidationTarget)EditorGUILayout.EnumPopup("Validate Target", selectedTarget);

      if (selectedTarget == ValidationTarget.ScenarioGraph)
      {
        validateScenarioWithSchema = EditorGUILayout.ToggleLeft(
          "ScenarioGraph 스키마 검증 포함",
          validateScenarioWithSchema);
      }

      EditorGUILayout.Space(6f);
      DrawSelectedTargetField();

      EditorGUILayout.Space(8f);
      using (new EditorGUILayout.HorizontalScope())
      {
        using (new EditorGUI.DisabledScope(!CanValidateCurrentTarget()))
        {
          if (GUILayout.Button("Validate Selected Target", GUILayout.Height(28f)))
          {
            ValidateSelectedTarget();
          }

          if (GUILayout.Button("Ping Selected SO", GUILayout.Height(28f)))
          {
            var selected = GetSelectedSo();
            if (selected != null)
              EditorGUIUtility.PingObject(selected);
          }
        }
      }
    }

    private void DrawSelectedTargetField()
    {
      switch (selectedTarget)
      {
        case ValidationTarget.ScenarioGraph:
          scenarioGraphSO = (RegistryPreloadScenarioGraphSO)EditorGUILayout.ObjectField(
            "ScenarioGraph SO",
            scenarioGraphSO,
            typeof(RegistryPreloadScenarioGraphSO),
            false);
          break;

        case ValidationTarget.IconSprite:
          iconSpriteSO = (RegistryPreloadIconSpriteSO)EditorGUILayout.ObjectField(
            "IconSprite SO",
            iconSpriteSO,
            typeof(RegistryPreloadIconSpriteSO),
            false);
          break;

        case ValidationTarget.Npc:
          npcSO = (RegistryPreloadNpcSO)EditorGUILayout.ObjectField(
            "Npc SO",
            npcSO,
            typeof(RegistryPreloadNpcSO),
            false);
          break;

        case ValidationTarget.Waypoint:
          waypointSO = (RegistryPreloadWaypointSO)EditorGUILayout.ObjectField(
            "Waypoint SO",
            waypointSO,
            typeof(RegistryPreloadWaypointSO),
            false);
          break;

        case ValidationTarget.Entity:
          entitySO = (RegistryPreloadEntitySO)EditorGUILayout.ObjectField(
            "Entity SO",
            entitySO,
            typeof(RegistryPreloadEntitySO),
            false);
          break;

        case ValidationTarget.InteractableEntity:
          interactableEntitySO = (RegistryPreloadInteractableEntitySO)EditorGUILayout.ObjectField(
            "InteractableEntity SO",
            interactableEntitySO,
            typeof(RegistryPreloadInteractableEntitySO),
            false);
          break;

        case ValidationTarget.UiController:
          uiControllerSO = (RegistryPreloadUIControllerSO)EditorGUILayout.ObjectField(
            "UIController SO",
            uiControllerSO,
            typeof(RegistryPreloadUIControllerSO),
            false);
          break;

        case ValidationTarget.TriageItemRegistrar:
          EditorGUILayout.HelpBox(
            "TriageItemRegistrar.RegisterAll() 기준으로 아이템 식별자 등록/생성/모델 리소스 유효성을 검사합니다.",
            MessageType.Info);
          break;
      }
    }

    private ScriptableObject GetSelectedSo()
    {
      return selectedTarget switch
      {
        ValidationTarget.ScenarioGraph => scenarioGraphSO,
        ValidationTarget.IconSprite => iconSpriteSO,
        ValidationTarget.Npc => npcSO,
        ValidationTarget.Waypoint => waypointSO,
        ValidationTarget.Entity => entitySO,
        ValidationTarget.InteractableEntity => interactableEntitySO,
        ValidationTarget.UiController => uiControllerSO,
        _ => null
      };
    }

    private bool CanValidateCurrentTarget()
    {
      return selectedTarget == ValidationTarget.TriageItemRegistrar || GetSelectedSo() != null;
    }

    private void ValidateSelectedTarget()
    {
      if (selectedTarget == ValidationTarget.TriageItemRegistrar)
      {
        ValidateTriageItemRegistrarTarget();
        return;
      }

      var selected = GetSelectedSo();
      if (selected == null)
      {
        EditorUtility.DisplayDialog("Registry Preloader Validation", "선택된 ScriptableObject가 없습니다.", "확인");
        return;
      }

      var report = RegistryPreloaderValidationReport.Create();

      switch (selectedTarget)
      {
        case ValidationTarget.ScenarioGraph:
          RegistryPreloaderValidator.ValidateScenarioGraph(scenarioGraphSO, validateScenarioWithSchema, report);
          break;

        case ValidationTarget.IconSprite:
          RegistryPreloaderValidator.ValidateIconSprite(iconSpriteSO, report);
          break;

        case ValidationTarget.Npc:
          RegistryPreloaderValidator.ValidateNpc(npcSO, report);
          break;

        case ValidationTarget.Waypoint:
          RegistryPreloaderValidator.ValidateWaypoint(waypointSO, report);
          break;

        case ValidationTarget.Entity:
          RegistryPreloaderValidator.ValidateEntity(entitySO, report);
          break;

        case ValidationTarget.InteractableEntity:
          RegistryPreloaderValidator.ValidateInteractableEntity(interactableEntitySO, report);
          break;

        case ValidationTarget.UiController:
          RegistryPreloaderValidator.ValidateUi(uiControllerSO, report);
          break;
      }

      report.FlushLogs();
      EditorGUIUtility.PingObject(selected);
      EditorUtility.DisplayDialog(
        "Registry Preloader Validation",
        report.BuildSummary($"검사 대상: {selected.name} ({selectedTarget})"),
        "확인");
    }

    private void ValidateTriageItemRegistrarTarget()
    {
      var report = RegistryPreloaderValidationReport.Create();
      report.AddInfo("=== Validate 'TriageItemRegistrar' ===", null);

      TriageItemRegistrar.RegisterAll();

      var definitions = CollectMedicalItemDefinitions(report);
      var used = new HashSet<string>(StringComparer.Ordinal);

      for (int i = 0; i < definitions.Count; i++)
      {
        var defType = definitions[i];
        string row = $"TriageItem[{i}]";
        var field = defType.GetField("Identifier", BindingFlags.Public | BindingFlags.Static);
        string identifier = field?.GetValue(null) as string;

        if (string.IsNullOrWhiteSpace(identifier))
        {
          report.AddError($"{row}: {defType.Name}.Identifier가 비어 있습니다.", null);
          continue;
        }

        if (!used.Add(identifier))
        {
          report.AddError($"{row}: 중복 identifier '{identifier}'가 있습니다.", null);
          continue;
        }

        if (!Registry.Contains(RegistryType.Item, identifier))
        {
          report.AddError($"{row}: Registry Item 미등록 identifier '{identifier}'.", null);
          continue;
        }

        var item = Registry.CreateItemInstance(identifier);
        if (item == null)
        {
          report.AddError($"{row}: identifier '{identifier}' 인스턴스 생성 실패.", null);
          continue;
        }

        if (item is not Item typedItem)
        {
          report.AddError($"{row}: identifier '{identifier}' 결과가 Item 타입이 아닙니다.", null);
          continue;
        }

        if (string.IsNullOrWhiteSpace(typedItem.CurrentIdentifier))
        {
          report.AddError($"{row}: 생성된 아이템 CurrentIdentifier가 비어 있습니다. ({identifier})", null);
        }

        string modelPath = $"Models/Items/{identifier}";
        var modelPrefab = Resources.Load<GameObject>(modelPath);
        if (modelPrefab == null)
        {
          report.AddError($"{row}: 아이템 모델 리소스가 없습니다. (Resources/{modelPath})", null);
        }
      }

      report.FlushLogs();
      EditorUtility.DisplayDialog(
        "Registry Preloader Validation",
        report.BuildSummary($"검사 대상: {nameof(TriageItemRegistrar)}"),
        "확인");
    }

    private static List<Type> CollectMedicalItemDefinitions(RegistryPreloaderValidationReport report)
    {
      var result = new List<Type>();

      var allTypes = TypeCache.GetTypesDerivedFrom<MedicalItem>();
      foreach (var t in allTypes)
      {
        if (t == null || t.IsAbstract)
          continue;

        var field = t.GetField("Identifier", BindingFlags.Public | BindingFlags.Static);
        if (field == null || field.FieldType != typeof(string))
        {
          report.AddWarning($"{t.Name}: public const string Identifier가 없어 검사에서 제외합니다.", null);
          continue;
        }

        result.Add(t);
      }

      return result;
    }
  }
}
#endif
