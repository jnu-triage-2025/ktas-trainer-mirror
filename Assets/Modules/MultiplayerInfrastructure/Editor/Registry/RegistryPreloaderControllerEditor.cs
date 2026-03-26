#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Scenario;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Registry
{
  [CustomEditor(typeof(RegistryPreloaderController))]
  public sealed class RegistryPreloaderControllerEditor : UnityEditor.Editor
  {
    private bool _validateScenarioWithSchema = true;

    public override void OnInspectorGUI()
    {
      DrawDefaultInspector();

      EditorGUILayout.Space(10f);
      EditorGUILayout.LabelField("Resource Integrity Debugger", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "Preloader에 연결된 SO를 기준으로 식별자/중복/누락 참조/시나리오 파싱 가능 여부를 검사합니다.",
        MessageType.Info);

      _validateScenarioWithSchema = EditorGUILayout.ToggleLeft(
        "ScenarioGraph 스키마 검증 포함",
        _validateScenarioWithSchema);

      using (new EditorGUILayout.HorizontalScope())
      {
        if (GUILayout.Button("Validate This Preloader", GUILayout.Height(28f)))
        {
          ValidateSingle((RegistryPreloaderController)target, _validateScenarioWithSchema);
        }
      }
    }

    private static void ValidateSingle(RegistryPreloaderController preloader, bool validateScenarioWithSchema)
    {
      if (preloader == null)
      {
        EditorUtility.DisplayDialog("Registry Preloader Validation", "검사할 Preloader가 없습니다.", "확인");
        return;
      }

      var report = RegistryPreloaderValidationReport.Create();
      RegistryPreloaderValidator.Validate(preloader, validateScenarioWithSchema, report);
      report.FlushLogs();

      EditorGUIUtility.PingObject(preloader);
      EditorUtility.DisplayDialog(
        "Registry Preloader Validation",
        report.BuildSummary($"검사 대상: {preloader.gameObject.name}"),
        "확인");
    }
  }

  internal static class RegistryPreloaderValidator
  {
    public static void Validate(
      RegistryPreloaderController preloader,
      bool validateScenarioWithSchema,
      RegistryPreloaderValidationReport report)
    {
      if (preloader == null)
      {
        report.AddError("Preloader가 null입니다.", null);
        return;
      }

      report.AddInfo($"=== Validate '{preloader.gameObject.name}' ===", preloader);

      ValidateScenarioGraph(preloader.preloadScenarioGraphSO, validateScenarioWithSchema, report);
      ValidateIconSprite(preloader.preloadIconSpriteSO, report);
      ValidateNpc(preloader.preloadNpcSO, report);
      ValidateWaypoint(preloader.preloadWaypointSO, report);
      ValidateEntity(preloader.preloadEntitySO, report);
      ValidateInteractableEntity(preloader.preloadInteractableEntitySO, report);
      ValidateUi(preloader.preloadUIControllerSO, report);
    }

    internal static void ValidateScenarioGraph(
      RegistryPreloadScenarioGraphSO so,
      bool validateScenarioWithSchema,
      RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadScenarioGraphSO가 비어 있습니다.", null);
        return;
      }

      if (so.scenarioGraphRegistryRequirements == null)
      {
        report.AddWarning("ScenarioGraph 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.scenarioGraphRegistryRequirements.Length; i++)
      {
        var req = so.scenarioGraphRegistryRequirements[i];
        string row = $"ScenarioGraph[{i}]";

        if (string.IsNullOrWhiteSpace(req.identifier))
        {
          report.AddError($"{row}: identifier가 비어 있습니다.", so);
          continue;
        }

        if (!used.Add(req.identifier))
        {
          report.AddError($"{row}: 중복 identifier '{req.identifier}'가 있습니다.", so);
        }

        if (req.scenarioGraphAsset == null)
        {
          report.AddError($"{row}: scenarioGraphAsset이 비어 있습니다.", so);
          continue;
        }

        if (string.IsNullOrWhiteSpace(req.scenarioGraphAsset.text))
        {
          report.AddError($"{row}: scenarioGraphAsset 텍스트가 비어 있습니다.", req.scenarioGraphAsset);
          continue;
        }

        try
        {
          ScenarioGraphLoader.LoadFromJson(req.scenarioGraphAsset.text, validateScenarioWithSchema);
        }
        catch (Exception ex)
        {
          report.AddError(
            $"{row}: 시나리오 파싱 실패 (identifier: '{req.identifier}') - {ex.Message}",
            req.scenarioGraphAsset);
        }
      }
    }

    internal static void ValidateIconSprite(RegistryPreloadIconSpriteSO so, RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadIconSpriteSO가 비어 있습니다.", null);
        return;
      }

      if (so.iconSpriteRegistryRequirements == null)
      {
        report.AddWarning("IconSprite 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.iconSpriteRegistryRequirements.Length; i++)
      {
        var req = so.iconSpriteRegistryRequirements[i];
        string row = $"IconSprite[{i}]";

        if (string.IsNullOrWhiteSpace(req.identifier))
        {
          report.AddError($"{row}: identifier가 비어 있습니다.", so);
          continue;
        }

        if (!used.Add(req.identifier))
        {
          report.AddError($"{row}: 중복 identifier '{req.identifier}'가 있습니다.", so);
        }

        if (req.sprite == null)
        {
          report.AddError($"{row}: sprite 참조가 비어 있습니다.", so);
          continue;
        }

        EnsurePersistentAsset(req.sprite, row, report);
      }
    }

    internal static void ValidateNpc(RegistryPreloadNpcSO so, RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadNpcSO가 비어 있습니다.", null);
        return;
      }

      if (so.npcRegistryRequirements == null)
      {
        report.AddWarning("NPC 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.npcRegistryRequirements.Length; i++)
      {
        var req = so.npcRegistryRequirements[i];
        string row = $"Npc[{i}]";

        if (string.IsNullOrWhiteSpace(req.Identifier))
        {
          report.AddError($"{row}: Identifier가 비어 있습니다.", so);
          continue;
        }

        if (!used.Add(req.Identifier))
        {
          report.AddError($"{row}: 중복 Identifier '{req.Identifier}'가 있습니다.", so);
        }

        if (req.GameObjectRef == null)
        {
          report.AddError($"{row}: GameObjectRef 참조가 비어 있습니다.", so);
          continue;
        }

        EnsurePersistentAsset(req.GameObjectRef, row, report);
      }
    }

    internal static void ValidateWaypoint(
      RegistryPreloadWaypointSO so,
      RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadWaypointSO가 비어 있습니다.", null);
        return;
      }

      if (so.waypointRequirements == null)
      {
        report.AddWarning("Waypoint 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.waypointRequirements.Length; i++)
      {
        var req = so.waypointRequirements[i];
        string row = $"Waypoint[{i}]";

        if (string.IsNullOrWhiteSpace(req.Identifier))
        {
          report.AddError($"{row}: Identifier가 비어 있습니다.", so);
          continue;
        }

        if (!used.Add(req.Identifier))
        {
          report.AddError($"{row}: 중복 Identifier '{req.Identifier}'가 있습니다.", so);
        }
      }
    }

    internal static void ValidateInteractableEntity(
      RegistryPreloadInteractableEntitySO so,
      RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadInteractableEntitySO가 비어 있습니다.", null);
        return;
      }

      if (so.interactableEntityRequirements == null)
      {
        report.AddWarning("InteractableEntity 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.interactableEntityRequirements.Length; i++)
      {
        var req = so.interactableEntityRequirements[i];
        string row = $"InteractableEntity[{i}]";

        if (string.IsNullOrWhiteSpace(req.Identifier))
        {
          report.AddError($"{row}: Identifier가 비어 있습니다.", so);
          continue;
        }

        if (!used.Add(req.Identifier))
        {
          report.AddError($"{row}: 중복 Identifier '{req.Identifier}'가 있습니다.", so);
        }
      }
    }

    internal static void ValidateEntity(RegistryPreloadEntitySO so, RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadEntitySO가 비어 있습니다.", null);
        return;
      }

      if (so.entityRegistryRequirements == null)
      {
        report.AddWarning("Entity 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.entityRegistryRequirements.Length; i++)
      {
        var req = so.entityRegistryRequirements[i];
        string row = $"Entity[{i}]";

        if (string.IsNullOrWhiteSpace(req.identifier))
        {
          report.AddError($"{row}: identifier가 비어 있습니다.", so);
          continue;
        }

        if (!used.Add(req.identifier))
        {
          report.AddError($"{row}: 중복 identifier '{req.identifier}'가 있습니다.", so);
        }

        if (req.objectRef == null)
        {
          report.AddError($"{row}: objectRef 참조가 비어 있습니다.", so);
          continue;
        }

        EnsurePersistentAsset(req.objectRef, row, report);
      }
    }

    internal static void ValidateUi(RegistryPreloadUIControllerSO so, RegistryPreloaderValidationReport report)
    {
      if (so == null)
      {
        report.AddWarning("preloadUIControllerSO가 비어 있습니다.", null);
        return;
      }

      if (so.uiControllerRegistryRequirements == null)
      {
        report.AddWarning("UIController 요구 사항 배열이 null입니다.", so);
        return;
      }

      var used = new HashSet<string>(StringComparer.Ordinal);
      for (int i = 0; i < so.uiControllerRegistryRequirements.Length; i++)
      {
        var req = so.uiControllerRegistryRequirements[i];
        string row = $"UI[{i}]";

        if (req.controllerRef == null)
        {
          report.AddError($"{row}: controllerRef 참조가 비어 있습니다.", so);
          continue;
        }

        EnsurePersistentAsset(req.controllerRef.gameObject, row, report);

        string effectiveKey = string.IsNullOrWhiteSpace(req.identifier)
          ? Registry.TypeKey(req.controllerRef.GetType())
          : req.identifier;

        if (string.IsNullOrWhiteSpace(effectiveKey))
        {
          report.AddError($"{row}: 유효한 identifier를 만들 수 없습니다.", so);
          continue;
        }

        if (!used.Add(effectiveKey))
        {
          report.AddError($"{row}: 중복 identifier '{effectiveKey}'가 있습니다.", so);
        }
      }
    }

    private static void EnsurePersistentAsset(
      UnityEngine.Object reference,
      string row,
      RegistryPreloaderValidationReport report)
    {
      if (reference == null)
        return;

      if (EditorUtility.IsPersistent(reference))
        return;

      report.AddWarning(
        $"{row}: 프로젝트 에셋이 아닌 Scene 객체 참조입니다. (대상: {reference.name})",
        reference);
    }
  }

  internal sealed class RegistryPreloaderValidationReport
  {
    private readonly List<RegistryPreloaderValidationIssue> _issues = new();

    private RegistryPreloaderValidationReport()
    {
    }

    public int ErrorCount { get; private set; }
    public int WarningCount { get; private set; }
    public int InfoCount { get; private set; }

    public static RegistryPreloaderValidationReport Create()
      => new RegistryPreloaderValidationReport();

    public void AddError(string message, UnityEngine.Object context)
    {
      ErrorCount++;
      _issues.Add(new RegistryPreloaderValidationIssue(LogType.Error, message, context));
    }

    public void AddWarning(string message, UnityEngine.Object context)
    {
      WarningCount++;
      _issues.Add(new RegistryPreloaderValidationIssue(LogType.Warning, message, context));
    }

    public void AddInfo(string message, UnityEngine.Object context)
    {
      InfoCount++;
      _issues.Add(new RegistryPreloaderValidationIssue(LogType.Log, message, context));
    }

    public void FlushLogs()
    {
      foreach (var issue in _issues)
      {
        string prefix = "[RegistryPreloaderValidator]";

        if (issue.LogType == LogType.Error)
        {
          Debug.LogError($"{prefix} {issue.Message}", issue.Context);
          continue;
        }

        if (issue.LogType == LogType.Warning)
        {
          Debug.LogWarning($"{prefix} {issue.Message}", issue.Context);
          continue;
        }

        Debug.Log($"{prefix} {issue.Message}", issue.Context);
      }
    }

    public string BuildSummary(string target)
    {
      string status = ErrorCount == 0
        ? "무결성 검사 통과"
        : "무결성 검사 실패";

      return
        $"{status}\n\n" +
        $"{target}\n" +
        $"Error: {ErrorCount}\n" +
        $"Warning: {WarningCount}\n" +
        $"Info: {InfoCount}\n\n" +
        "상세 내용은 Console 로그를 확인하세요.";
    }
  }

  internal readonly struct RegistryPreloaderValidationIssue
  {
    public LogType LogType { get; }
    public string Message { get; }
    public UnityEngine.Object Context { get; }

    public RegistryPreloaderValidationIssue(LogType logType, string message, UnityEngine.Object context)
    {
      LogType = logType;
      Message = message;
      Context = context;
    }
  }
}
#endif
