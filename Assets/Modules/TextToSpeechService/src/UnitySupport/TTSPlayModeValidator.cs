#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TextToSpeechService.Editor
{
  /// <summary>
  /// ONNX 모델 파일의 존재 여부를 감시하여, 모델이 없으면 플레이 모드 진입을 차단합니다.
  ///
  /// 동작:
  ///   · 에디터 시작 시 모델 파일 유무 확인 → 콘솔에 경고
  ///   · 플레이 모드 진입 직전(ExitingEditMode) 재확인 → 없으면 대화상자로 중단
  ///
  /// 모델 경로는 <see cref="TTSPlayModeValidatorSettings"/> EditorPrefs 키로 변경 가능.
  /// </summary>
  [InitializeOnLoad]
  public static class TTSPlayModeValidator
  {
    private const string PrefKey = "TTS.OnnxDirPath";

    /// <summary>
    /// 사용자가 "그대로 재생"(런타임 즉석 합성)을 선택하면 이번 한 번의 플레이 진입에 한해
    /// 시나리오 인라인 TTS bake 검사를 건너뛴다.
    /// </summary>
    private static bool _skipInlineBakeCheckOnce;

    // =========================================================================
    // 정적 생성자 — 에디터 로드 시 한 번 실행
    // =========================================================================

    static TTSPlayModeValidator()
    {
      // 플레이 모드 상태 변경 콜백 등록
      EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

      // 에디터 시작 시 현재 모델 상태 확인
      EditorApplication.delayCall += CheckModelsAndWarn;
    }

    // =========================================================================
    // ONNX 경로 설정 프로퍼티
    // =========================================================================

    public static string OnnxDirPath
    {
      get => EditorPrefs.GetString(PrefKey, TTSCore.GetOnnxDir(Application.streamingAssetsPath));
      set => EditorPrefs.SetString(PrefKey, value);
    }

    // =========================================================================
    // 플레이 모드 진입 전 검증
    // =========================================================================

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
      if (state != PlayModeStateChange.ExitingEditMode) return;

      // ── 1단계: ONNX 모델 다운로드 여부 검사 ─────────────────────────────
      if (!TTSCore.AreModelsPresent(OnnxDirPath))
      {
        // 모델 없음 → 플레이 중단
        EditorApplication.isPlaying = false;

        var missing  = TTSCore.GetMissingModelFiles(OnnxDirPath);
        bool open = EditorUtility.DisplayDialog(
          "TTS 모델 없음",
          $"플레이 모드를 시작할 수 없습니다.\n\n" +
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

    /// <summary>
    /// PlayTTS 플래그가 켜진 시나리오 인라인 텍스트 중 bake되지 않았거나(dirty 포함)
    /// 다시 bake가 필요한 항목이 있으면 사용자에게 bake 여부를 묻는다.
    ///
    /// 선택지:
    ///   · 지금 bake      : 동기 bake 후 그대로 플레이 진행
    ///   · 그대로 재생     : bake 없이 진행(런타임에 즉석 합성)
    ///   · 취소           : 플레이 중단
    /// </summary>
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

      // 플레이 진입을 일단 중단하고 사용자에게 확인.
      EditorApplication.isPlaying = false;

      int choice = EditorUtility.DisplayDialogComplex(
        "시나리오 TTS Bake 필요",
        $"PlayTTS가 켜진 인라인 텍스트 중 사전 합성이 필요한 항목이 있습니다.\n\n" +
        $"  · 미bake: {scan.MissingCount}개\n" +
        $"  · 변경됨(dirty): {scan.DirtyCount}개\n\n" +
        "지금 bake하시겠습니까? bake하지 않으면 런타임에 즉석 합성됩니다.",
        "지금 bake",     // 0
        "취소",          // 1
        "그대로 재생");   // 2

      switch (choice)
      {
        case 0: // 지금 bake 후 재생
          MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.BakeAllNeededSynchronously();
          _skipInlineBakeCheckOnce = true;
          EditorApplication.isPlaying = true;
          break;

        case 2: // 그대로 재생 (즉석 합성)
          _skipInlineBakeCheckOnce = true;
          EditorApplication.isPlaying = true;
          break;

        default: // 취소
          break;
      }
    }

    // =========================================================================
    // 경고 출력 (에디터 시작 시)
    // =========================================================================

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

  // ===========================================================================
  // Project Settings 패널 연동 (선택적)
  // ===========================================================================

  /// <summary>
  /// Project Settings > TTS 패널에서 ONNX 경로를 변경할 수 있습니다.
  /// </summary>
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

          // 현재 모델 상태 표시
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
