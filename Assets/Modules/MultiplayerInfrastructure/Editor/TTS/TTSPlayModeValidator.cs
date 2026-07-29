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

    /// <summary>
    /// "무시" + "앞으로도 계속 무시하기" 선택 시 저장되는 영구 설정 키.
    /// true인 동안은 ONNX 모델이 없어도 플레이 모드 진입 시 경고/프롬프트 없이 그대로 진행한다.
    /// 이 설정은 사용자가 실수로 영구히 켜 둔 채 잊어버릴 수 있으므로, 반드시 별도로 끌 수 있는
    /// 에디터 UI가 필요하다 → Edit(Unity) > Project Settings > TTS 패널의 체크박스에서 해제 가능.
    /// </summary>
    private const string IgnorePermanentlyPrefKey = "TTS.IgnoreMissingModelsPermanently";

    /// <summary>"그대로 재생"(런타임 즉석 합성) 선택 시 이번 플레이 진입 1회 bake 검사를 건너뛴다.</summary>
    private static bool _skipInlineBakeCheckOnce;

    /// <summary>"무시" 선택(1회) 시 이번 플레이 진입 1회 모델 존재 검사를 건너뛴다.</summary>
    private static bool _skipModelCheckOnce;

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

    /// <summary>
    /// true면 ONNX 모델 누락 경고를 항상 무시하고 플레이 모드를 계속 진행한다.
    /// Project Settings > TTS 패널에서 언제든 다시 끌 수 있다.
    /// </summary>
    public static bool IgnoreMissingModelsPermanently
    {
      get => EditorPrefs.GetBool(IgnorePermanentlyPrefKey, false);
      set => EditorPrefs.SetBool(IgnorePermanentlyPrefKey, value);
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
      if (state != PlayModeStateChange.ExitingEditMode) return;

      // ── 1단계: ONNX 모델 다운로드 여부 검사 ─────────────────────────────
      if (_skipModelCheckOnce)
      {
        _skipModelCheckOnce = false;
      }
      else if (IgnoreMissingModelsPermanently)
      {
        // 사용자가 "앞으로도 계속 무시하기"를 선택한 상태. 검사를 건너뛰고 그대로 진행한다.
        // Project Settings > TTS 패널에서 다시 켤 수 있다.
      }
      else if (!TTSCore.AreModelsPresent(OnnxDirPath))
      {
        EditorApplication.isPlaying = false;

        var missing = TTSCore.GetMissingModelFiles(OnnxDirPath);
        var result = TTSMissingModelsDialog.Show(OnnxDirPath, missing);

        switch (result.Choice)
        {
          case TTSMissingModelsDialog.DialogChoice.OpenDownloadWindow:
            TTSModelDownloaderWindow.OpenWindow();
            return;

          case TTSMissingModelsDialog.DialogChoice.Ignore:
            if (result.IgnorePermanently)
              IgnoreMissingModelsPermanently = true;

            _skipModelCheckOnce = true;
            EditorApplication.isPlaying = true;
            break;

          default: // 취소
            return;
        }
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

      // bake가 필요하거나(미bake/dirty) 사용하지 않는 baked 파일(orphan)이 있으면 안내한다.
      if (!scan.NeedsBake && !scan.HasOrphans) return;

      EditorApplication.isPlaying = false;

      // 상태 메시지 구성
      var lines = new System.Text.StringBuilder();
      if (scan.NeedsBake)
      {
        lines.AppendLine("PlayTTS가 켜진 인라인 텍스트 중 사전 합성이 필요한 항목이 있습니다.");
        lines.AppendLine($"  · 미bake: {scan.MissingCount}개");
        lines.AppendLine($"  · 변경됨(dirty): {scan.DirtyCount}개");
      }
      if (scan.HasOrphans)
      {
        if (scan.NeedsBake) lines.AppendLine();
        lines.AppendLine($"더 이상 사용하지 않는 baked 파일: {scan.OrphanCount}개");
      }
      lines.AppendLine();

      if (scan.NeedsBake)
        lines.Append("지금 bake하시겠습니까? bake하지 않으면 런타임에 즉석 합성됩니다.");
      else
        lines.Append("사용하지 않는 baked 파일을 정리하시겠습니까?");

      // 버튼 구성:
      //   · bake가 필요하면: [지금 bake + 정리] / [취소] / [그대로 재생]
      //   · orphan만 있으면:  [지금 정리] / [취소] / [그대로 재생]
      string primaryLabel = scan.NeedsBake
        ? (scan.HasOrphans ? "지금 bake 하고 정리" : "지금 bake")
        : "지금 정리";

      int choice = EditorUtility.DisplayDialogComplex(
        "시나리오 TTS Bake 검사",
        lines.ToString(),
        primaryLabel,   // 0
        "취소",          // 1
        "그대로 재생");   // 2

      switch (choice)
      {
        case 0: // 주 작업(bake 및/또는 orphan 정리) 후 재생
          if (scan.NeedsBake)
          {
            // 씬에 존재하는 TTSService로부터 voice profile 정보를 가져온다.
            // 씬이 열려 있지 않은 경우 프로파일 없이 기본 목소리만으로 bake한다.
            TTSVoiceProfile[] voiceProfiles = null;
#if UNITY_2023_1_OR_NEWER
            var ttsService = UnityEngine.Object.FindFirstObjectByType<TextToSpeechService.TTSService>();
#else
            var ttsService = UnityEngine.Object.FindObjectOfType<TextToSpeechService.TTSService>();
#endif
            // TTSService의 voiceProfiles 필드는 SerializedObject를 통해 접근한다.
            if (ttsService != null)
            {
              var so = new UnityEditor.SerializedObject(ttsService);
              var profilesProp = so.FindProperty("voiceProfiles");
              if (profilesProp != null && profilesProp.isArray)
              {
                var list = new System.Collections.Generic.List<TTSVoiceProfile>(profilesProp.arraySize);
                for (int pIdx = 0; pIdx < profilesProp.arraySize; pIdx++)
                {
                  var elem = profilesProp.GetArrayElementAtIndex(pIdx);
                  if (elem == null) continue;
                  list.Add(new TTSVoiceProfile
                  {
                    VoiceIdentifier = elem.FindPropertyRelative("VoiceIdentifier")?.stringValue,
                    VoiceStyleName  = elem.FindPropertyRelative("VoiceStyleName")?.stringValue,
                    Language        = elem.FindPropertyRelative("Language")?.stringValue,
                    TotalStep       = elem.FindPropertyRelative("TotalStep")?.intValue ?? 0,
                    Speed           = elem.FindPropertyRelative("Speed")?.floatValue ?? 0f
                  });
                }
                voiceProfiles = list.ToArray();
              }
            }
            MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner.BakeAllNeededSynchronously(
              voiceProfiles: voiceProfiles);
          }

          if (scan.HasOrphans)
          {
            int removed = MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner
              .DeleteOrphans(scan.OrphanedBakedPaths);
            if (removed > 0)
            {
              AssetDatabase.Refresh();
              Debug.Log($"[TTS] 사용하지 않는 baked 파일 {removed}개를 정리했습니다.");
            }
          }

          _skipInlineBakeCheckOnce = true;
          EditorApplication.isPlaying = true;
          break;

        case 2: // 그대로 재생 (정리/생성 없이 진행, 런타임 즉석 합성)
          _skipInlineBakeCheckOnce = true;
          EditorApplication.isPlaying = true;
          break;

        default: // 취소
          break;
      }
    }

    public static void CheckModelsAndWarn()
    {
      if (TTSCore.AreModelsPresent(OnnxDirPath)) return;

      var missing = TTSCore.GetMissingModelFiles(OnnxDirPath);

      if (IgnoreMissingModelsPermanently)
      {
        Debug.Log(
          $"[TTS] ONNX 모델 파일 {missing.Count}개가 없지만, '앞으로도 계속 무시하기' 설정으로 인해 경고를 건너뜁니다. " +
          "(Project Settings > TTS 에서 다시 켤 수 있습니다)");
        return;
      }

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

          EditorGUILayout.Space(12);
          EditorGUILayout.LabelField("플레이 모드 경고", EditorStyles.boldLabel);

          EditorGUI.BeginChangeCheck();
          bool ignorePermanently = EditorGUILayout.ToggleLeft(
            "ONNX 모델 누락 경고를 앞으로도 계속 무시하기",
            TTSPlayModeValidator.IgnoreMissingModelsPermanently);
          if (EditorGUI.EndChangeCheck())
            TTSPlayModeValidator.IgnoreMissingModelsPermanently = ignorePermanently;

          EditorGUILayout.HelpBox(
            "플레이 모드 진입 시 표시되는 'TTS 모델 없음' 대화상자에서 '무시' + '앞으로도 계속 무시하기'를 " +
            "선택하면 이 설정이 켜집니다. 여기서 언제든 다시 끌 수 있습니다.",
            MessageType.None);
        },
        keywords = new System.Collections.Generic.HashSet<string> { "TTS", "ONNX", "Supertonic" },
      };
  }
}
#endif
