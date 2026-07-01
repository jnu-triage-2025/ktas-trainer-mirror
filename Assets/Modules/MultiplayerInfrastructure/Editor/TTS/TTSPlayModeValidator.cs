#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TextToSpeechService;

namespace MultiplayerInfrastructure.Editor.TTS
{
  /// <summary>
  /// 플레이 모드 진입 시 TTS 사용 준비 상태를 검사하고 사용자에게 안내/프롬프트하는 에디터 훅.
  ///
  /// 이 클래스는 MultiplayerInfrastructure의 통합 정책(경고/프롬프트/bake 유도)만 담당하며,
  /// 모델 존재 검사·경로·bake 여부 판정 등 핵심 로직은 TextToSpeechService 모듈
  /// (<see cref="TTSCore"/>) 및 시나리오 스캐너(<see cref="MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner"/>)에 위임한다.
  ///
  /// 동작:
  ///   1. ONNX 모델 다운로드 여부 검사 → 없으면 플레이 중단 + 다운로드 안내
  ///   2. 시나리오 인라인 TTS bake 여부 검사 → 필요 시 bake/그대로 재생/취소 프롬프트
  /// </summary>
  [InitializeOnLoad]
  public static class TTSPlayModeValidator
  {
    private const string PrefKey = "TTS.OnnxDirPath";

    /// <summary>"그대로 재생"(런타임 즉석 합성) 선택 시 이번 플레이 진입 1회 bake 검사를 건너뛴다.</summary>
    private static bool _skipInlineBakeCheckOnce;

    static TTSPlayModeValidator()
    {
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
      EditorApplication.delayCall += CheckModelsAndWarn;
    }

    public static string OnnxDirPath
    {
      get => EditorPrefs.GetString(PrefKey, TTSCore.GetOnnxDir(Application.streamingAssetsPath));
      set => EditorPrefs.SetString(PrefKey, value);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
      if (state != PlayModeStateChange.ExitingEditMode) return;

      // ── 1단계: ONNX 모델 다운로드 여부 검사 ─────────────────────────────
      if (!TTSCore.AreModelsPresent(OnnxDirPath))
      {
        EditorApplication.isPlaying = false;

        var missing = TTSCore.GetMissingModelFiles(OnnxDirPath);
        bool open = EditorUtility.DisplayDialog(
          "TTS 모델 없음",
          "플레이 모드를 시작할 수 없습니다.\n\n" +
          $"다음 ONNX 모델 파일이 없습니다 ({OnnxDirPath}):\n" +
          $"  · {string.Join("\n  · ", missing)}\n\n" +
          "TTS > Download Models 에서 다운로드하시겠습니까?",
          "다운로드 창 열기", "취소");

        if (open)
          TTSModelDownloaderWindow.OpenWindow();
        return;
      }

      // ── 2단계: 시나리오 인라인 TTS bake 여부 검사 ───────────────────────
      CheckInlineBakeAndPrompt();
    }

    private static void CheckInlineBakeAndPrompt()
    {
      if (_skipInlineBakeCheckOnce)
      {
        _skipInlineBakeCheckOnce = false;
        return;
      }

      MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.ScanResult scan;
      try
      {
        scan = MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner
          .ScanAllScenarios(Application.streamingAssetsPath);
      }
      catch (System.Exception ex)
      {
        Debug.LogWarning($"[TTS] 인라인 bake 스캔 실패(무시하고 진행): {ex.Message}");
        return;
      }

      if (!scan.NeedsBake) return;

      EditorApplication.isPlaying = false;

      int choice = EditorUtility.DisplayDialogComplex(
        "시나리오 TTS Bake 필요",
        "PlayTTS가 켜진 인라인 텍스트 중 사전 합성이 필요한 항목이 있습니다.\n\n" +
        $"  · 미bake: {scan.MissingCount}개\n" +
        $"  · 변경됨(dirty): {scan.DirtyCount}개\n\n" +
        "지금 bake하시겠습니까? bake하지 않으면 런타임에 즉석 합성됩니다.",
        "지금 bake",    // 0
        "취소",         // 1
        "그대로 재생");  // 2

      switch (choice)
      {
        case 0:
          MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.BakeAllNeededSynchronously();
          _skipInlineBakeCheckOnce = true;
          EditorApplication.isPlaying = true;
          break;

        case 2:
          _skipInlineBakeCheckOnce = true;
          EditorApplication.isPlaying = true;
          break;

        default:
          break;
      }
    }

    public static void CheckModelsAndWarn()
    {
      if (TTSCore.AreModelsPresent(OnnxDirPath)) return;

      var missing = TTSCore.GetMissingModelFiles(OnnxDirPath);
      Debug.LogWarning(
        $"[TTS] ONNX 모델 파일 {missing.Count}개가 없습니다: {OnnxDirPath}\n" +
        $"누락 파일: {string.Join(", ", missing)}\n" +
        "TTS > Download Models 메뉴를 사용하여 다운로드하세요.");
    }
  }

  /// <summary>Project Settings > TTS 패널에서 ONNX 경로를 변경할 수 있다.</summary>
  internal static class TTSValidatorSettingsProvider
  {
    [SettingsProvider]
    public static SettingsProvider CreateProvider() =>
      new SettingsProvider("Project/TTS", SettingsScope.Project)
      {
        label = "TTS",
        guiHandler = _ =>
        {
          EditorGUILayout.Space(8);
          EditorGUILayout.LabelField("모델 설정", EditorStyles.boldLabel);

          EditorGUI.BeginChangeCheck();
          string newPath = EditorGUILayout.TextField(
            "ONNX 디렉터리", TTSPlayModeValidator.OnnxDirPath);
          if (EditorGUI.EndChangeCheck())
            TTSPlayModeValidator.OnnxDirPath = newPath;

          EditorGUILayout.Space(4);

          var missing = TTSCore.GetMissingModelFiles(TTSPlayModeValidator.OnnxDirPath);
          if (missing.Count == 0)
          {
            EditorGUILayout.HelpBox("모든 모델 파일이 준비되어 있습니다.", MessageType.Info);
          }
          else
          {
            EditorGUILayout.HelpBox(
              $"누락된 파일 {missing.Count}개:\n  · {string.Join("\n  · ", missing)}",
              MessageType.Warning);

            if (GUILayout.Button("TTS > Download Models 열기"))
              TTSModelDownloaderWindow.OpenWindow();
          }
        },
        keywords = new System.Collections.Generic.HashSet<string> { "TTS", "ONNX", "Supertonic" },
      };
  }
}
#endif
