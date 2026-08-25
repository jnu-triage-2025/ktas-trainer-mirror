#if UNITY_EDITOR
using System;
using MultiplayerInfrastructure.Definitions;
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
      PlayerCharacter,
      ProblemSet,
      MultiplayerInfrastructureRegisterSupport
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
    [SerializeField] private RegistryPreloadPlayerCharacterSO playerCharacterSO;
    [SerializeField] private RegistryPreloadProblemSetSO problemSetSO;

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
        "검사 대상을 선택한 뒤 Validate를 실행하세요. SO 타겟은 객체 지정이 필요하고, MultiplayerInfrastructureRegisterSupport 타겟은 등록/리소스 무결성을 직접 검사합니다.",
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
        bool canValidate = CanValidateCurrentTarget();
        var selectedSo = GetSelectedSo();

        using (new EditorGUI.DisabledScope(!canValidate))
        {
          if (GUILayout.Button("Validate Selected Target", GUILayout.Height(28f)))
          {
            ValidateSelectedTarget();
          }
        }

        using (new EditorGUI.DisabledScope(selectedSo == null))
        {
          if (GUILayout.Button("Ping Selected SO", GUILayout.Height(28f)))
          {
            EditorGUIUtility.PingObject(selectedSo);
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

        case ValidationTarget.PlayerCharacter:
          playerCharacterSO = (RegistryPreloadPlayerCharacterSO)EditorGUILayout.ObjectField(
            "PlayerCharacter SO",
            playerCharacterSO,
            typeof(RegistryPreloadPlayerCharacterSO),
            false);
          break;

        case ValidationTarget.ProblemSet:
          problemSetSO = (RegistryPreloadProblemSetSO)EditorGUILayout.ObjectField(
            "ProblemSet SO",
            problemSetSO,
            typeof(RegistryPreloadProblemSetSO),
            false);
          break;

        case ValidationTarget.MultiplayerInfrastructureRegisterSupport:
          EditorGUILayout.HelpBox(
            "MultiplayerInfrastructureRegisterSupport.RegisterAllItems() 기준으로 아이템 등록/생성/리소스 유효성을 검사합니다.",
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
        ValidationTarget.PlayerCharacter => playerCharacterSO,
        ValidationTarget.ProblemSet => problemSetSO,
        _ => null
      };
    }

    private bool CanValidateCurrentTarget()
    {
      return selectedTarget == ValidationTarget.MultiplayerInfrastructureRegisterSupport || GetSelectedSo() != null;
    }

    private void ValidateSelectedTarget()
    {
      if (selectedTarget == ValidationTarget.MultiplayerInfrastructureRegisterSupport)
      {
        ValidateMultiplayerInfrastructureRegisterSupportTarget();
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

        case ValidationTarget.PlayerCharacter:
          RegistryPreloaderValidator.ValidatePlayerCharacter(playerCharacterSO, report);
          break;

        case ValidationTarget.ProblemSet:
          RegistryPreloaderValidator.ValidateProblemSet(problemSetSO, report);
          break;
      }

      report.FlushLogs();
      EditorGUIUtility.PingObject(selected);
      EditorUtility.DisplayDialog(
        "Registry Preloader Validation",
        report.BuildSummary($"검사 대상: {selected.name} ({selectedTarget})"),
        "확인");
    }

    private void ValidateMultiplayerInfrastructureRegisterSupportTarget()
    {
      var report = RegistryPreloaderValidationReport.Create();
      report.AddInfo("=== Validate 'MultiplayerInfrastructureRegisterSupport' ===", null);

      const string supportTypeName = "TriageTrainer.Items.MultiplayerInfrastructureRegisterSupport";
      var supportType = FindTypeByFullName(supportTypeName);
      if (supportType == null)
      {
        report.AddError($"타입을 찾을 수 없습니다: {supportTypeName}", null);
        report.FlushLogs();
        EditorUtility.DisplayDialog(
          "Registry Preloader Validation",
          report.BuildSummary($"검사 대상: {supportTypeName}"),
          "확인");
        return;
      }

      var registerAllItems = supportType.GetMethod("RegisterAllItems", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
      if (registerAllItems == null)
      {
        report.AddError("RegisterAllItems() 정적 메서드를 찾을 수 없습니다.", null);
        report.FlushLogs();
        EditorUtility.DisplayDialog(
          "Registry Preloader Validation",
          report.BuildSummary($"검사 대상: {supportTypeName}"),
          "확인");
        return;
      }

      registerAllItems.Invoke(null, null);

      var registeredItems = Registry.GetAll<Type>(RegistryType.Item);
      if (registeredItems.Count == 0)
      {
        report.AddError("RegistryType.Item에 등록된 항목이 없습니다.", null);
      }

      int index = 0;
      foreach (var pair in registeredItems)
      {
        string identifier = pair.Key;
        string row = $"RegisteredItem[{index}]";
        index++;

        if (string.IsNullOrWhiteSpace(identifier))
        {
          report.AddError($"{row}: identifier가 비어 있습니다.", null);
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

        if (string.IsNullOrWhiteSpace(item.CurrentIdentifier))
        {
          report.AddError($"{row}: 생성된 아이템 CurrentIdentifier가 비어 있습니다. ({identifier})", null);
        }

        string iconPath = $"{DefaultsItemRegistry.ItemTexturesPath}/{identifier}";
        var icon = Resources.Load<Sprite>(iconPath);
        if (icon == null)
        {
          report.AddError($"{row}: 아이콘 리소스가 없습니다. (Resources/{iconPath})", null);
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
        report.BuildSummary($"검사 대상: {supportTypeName}"),
        "확인");
    }

    private static Type FindTypeByFullName(string fullName)
    {
      foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
      {
        var type = assembly.GetType(fullName, false);
        if (type != null)
          return type;
      }

      return null;
    }
  }
}
#endif
