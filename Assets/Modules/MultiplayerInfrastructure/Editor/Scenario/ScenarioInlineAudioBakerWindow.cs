#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using TextToSpeechService;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 프로젝트 내 시나리오 그래프를 스캔하여, PlayTTS 플래그가 켜져 있고 변수를 포함하지 않는
  /// 인라인 텍스트(Dialogue/Choice/Quiz)를 사전 합성(bake)하여
  /// StreamingAssets/TTS/BakedInline/ 에 WAV로 저장하는 에디터 창.
  ///
  /// 저장 경로 규칙:
  ///   Assets/StreamingAssets/TTS/BakedInline/{scenarioIdentifier}/{nodeIdentifier}_{hash}.wav
  ///
  /// 메뉴: Tools > Text to Speech Service > Bake Scenario Inline Audio
  /// </summary>
  public class ScenarioInlineAudioBakerWindow : EditorWindow
  {
    // =========================================================================
    // 설정
    // =========================================================================

    private string _onnxDirPath    = "";
    private string _voiceStylePath = "";
    private string _language  = "ko";
    private int    _totalStep = 5;
    private float  _speed     = 1.05f;
    private bool   _cleanStale = true;

    private void OnEnable()
    {
      string sa       = Application.streamingAssetsPath;
      _onnxDirPath    = TTSCore.GetOnnxDir(sa);
      _voiceStylePath = TTSCore.GetVoiceStylePath(sa, "F1");
      Rescan();
    }

    // =========================================================================
    // 상태
    // =========================================================================

    private bool    _isBaking;
    private float   _progress;
    private string  _statusText = "";
    private string  _lastError  = "";
    private Vector2 _scroll;
    private readonly List<string> _log = new();

    private ScenarioTTSBakeScanner.ScanResult _scan;

    // =========================================================================
    // 메뉴 / 창 열기
    // =========================================================================

    [MenuItem("Tools/Text to Speech Service/Bake Scenario Inline Audio")]
    public static void OpenWindow()
    {
      var win = GetWindow<ScenarioInlineAudioBakerWindow>("Scenario Inline TTS Baker");
      win.minSize = new Vector2(540, 420);
      win.Show();
    }

    // =========================================================================
    // GUI
    // =========================================================================

    private void OnGUI()
    {
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Scenario Inline Audio Baker", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "PlayTTS가 켜진 Dialogue/Choice/Quiz 노드의 변수 없는 인라인 텍스트를 사전 합성합니다.",
        MessageType.Info);

      EditorGUILayout.Space(4);

      using (new EditorGUI.DisabledScope(_isBaking))
      {
        _onnxDirPath    = EditorGUILayout.TextField("ONNX 디렉터리", _onnxDirPath);
        _voiceStylePath = EditorGUILayout.TextField("음성 스타일",   _voiceStylePath);
        _language  = EditorGUILayout.TextField("언어 코드", _language);
        _totalStep = EditorGUILayout.IntField("Diffusion 스텝", _totalStep);
        _speed     = EditorGUILayout.FloatField("속도 배율",  _speed);
        _cleanStale = EditorGUILayout.Toggle("변경/미사용 파일 정리", _cleanStale);

        EditorGUILayout.Space(4);
        if (GUILayout.Button("다시 스캔"))
          Rescan();
      }

      EditorGUILayout.Space(6);

      if (_scan != null)
      {
        EditorGUILayout.LabelField("스캔 결과",
          $"작업 {_scan.Jobs.Count}개  ·  미bake {_scan.MissingCount}개  ·  dirty {_scan.DirtyCount}개  ·  미사용 {_scan.OrphanCount}개",
          EditorStyles.miniLabel);
      }

      if (!string.IsNullOrEmpty(_lastError))
        EditorGUILayout.HelpBox(_lastError, MessageType.Error);

      EditorGUILayout.Space(6);

      if (_isBaking)
      {
        EditorGUI.ProgressBar(
          EditorGUILayout.GetControlRect(false, 20), _progress, _statusText);
      }
      else
      {
        using (new EditorGUILayout.HorizontalScope())
        {
          using (new EditorGUI.DisabledScope(_scan == null || _scan.Jobs.Count == 0))
          {
            if (GUILayout.Button("미bake/변경분만 굽기"))
              StartBaking(onlyNeeded: true);

            if (GUILayout.Button("전체 다시 굽기"))
              StartBaking(onlyNeeded: false);
          }
        }

        // 사용하지 않는(orphan) baked 파일 정리
        using (new EditorGUI.DisabledScope(_scan == null || !_scan.HasOrphans))
        {
          if (GUILayout.Button($"미사용 baked 파일 정리 ({(_scan != null ? _scan.OrphanCount : 0)}개)"))
            CleanOrphans();
        }
      }

      // 작업 목록
      EditorGUILayout.Space(6);
      EditorGUILayout.LabelField("작업 / 로그", EditorStyles.boldLabel);
      _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

      if (_log.Count > 0)
      {
        foreach (var line in _log)
          EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
      }
      else if (_scan != null)
      {
        foreach (var job in _scan.Jobs)
        {
          string state = job.IsDirty ? "DIRTY" : (job.IsBaked ? "OK" : "MISSING");
          EditorGUILayout.LabelField(
            $"[{state}] {job.ScenarioIdentifier}/{job.NodeIdentifier}  \"{Truncate(job.Text, 40)}\"",
            EditorStyles.miniLabel);
        }
      }

      EditorGUILayout.EndScrollView();
    }

    // =========================================================================
    // 스캔
    // =========================================================================

    private void Rescan()
    {
      _lastError = "";
      try
      {
        _scan = ScenarioTTSBakeScanner.ScanAllScenarios(Application.streamingAssetsPath);
      }
      catch (Exception ex)
      {
        _lastError = $"스캔 오류: {ex.Message}";
        Debug.LogError($"[ScenarioInlineAudioBaker] {ex}");
      }
    }

    /// <summary>현재 스캔 결과의 미사용(orphan) baked 파일을 삭제한다.</summary>
    private void CleanOrphans()
    {
      if (_scan == null || !_scan.HasOrphans) return;

      bool ok = EditorUtility.DisplayDialog(
        "미사용 baked 파일 정리",
        $"현재 어떤 시나리오 노드에서도 사용하지 않는 baked 파일 {_scan.OrphanCount}개를 삭제합니다.\n계속하시겠습니까?",
        "삭제", "취소");
      if (!ok) return;

      int removed = ScenarioTTSBakeScanner.DeleteOrphans(_scan.OrphanedBakedPaths);
      AssetDatabase.Refresh();
      _log.Clear();
      _log.Add($"미사용 baked 파일 {removed}개 삭제 완료.");
      Debug.Log($"[ScenarioInlineAudioBaker] 미사용 baked 파일 {removed}개 삭제.");
      Rescan();
    }

    // =========================================================================
    // 굽기
    // =========================================================================

    private void StartBaking(bool onlyNeeded)
    {
      _lastError  = "";
      _isBaking   = true;
      _progress   = 0f;
      _statusText = "준비 중...";
      _log.Clear();

      if (!TTSCore.AreModelsPresent(_onnxDirPath))
      {
        _lastError = "ONNX 모델이 없습니다. 먼저 Tools > Text to Speech Service > Download Models 를 실행하세요.";
        _isBaking  = false;
        return;
      }

      Rescan();
      if (_scan == null)
      {
        _isBaking = false;
        return;
      }

      // 작업 목록 결정
      var jobs = new List<ScenarioTTSBakeScanner.InlineTTSJob>();
      foreach (var job in _scan.Jobs)
      {
        if (onlyNeeded && job.IsBaked && !job.IsDirty) continue;
        jobs.Add(job);
      }

      string onnxDir    = _onnxDirPath;
      string stylePath  = _voiceStylePath;
      string lang       = _language;
      int    steps      = _totalStep;
      float  spd        = _speed;
      bool   cleanStale = _cleanStale;
      var    log        = _log;

      _ = Task.Run(() =>
      {
        try
        {
          using var core = new TTSCore(onnxDir, stylePath);

          for (int i = 0; i < jobs.Count; i++)
          {
            var   job  = jobs[i];
            float prog = jobs.Count == 0 ? 1f : (float)i / jobs.Count;

            UpdateProgress(prog, $"합성 중 {job.ScenarioIdentifier}/{job.NodeIdentifier}");

            // 변경/미사용 stale 파일 정리
            if (cleanStale && job.StaleBakedPaths != null)
            {
              foreach (var stale in job.StaleBakedPaths)
              {
                try { if (File.Exists(stale)) File.Delete(stale); }
                catch (Exception e) { AppendLog(log, $"[WARN] stale 삭제 실패 {stale}: {e.Message}"); }
              }
            }

            if (job.IsBaked && !job.IsDirty && !onlyNeeded)
            {
              // 전체 다시 굽기: 기존 파일도 덮어씀
            }
            else if (job.IsBaked && !job.IsDirty)
            {
              AppendLog(log, $"[SKIP] {job.ScenarioIdentifier}/{job.NodeIdentifier}");
              continue;
            }

            float[] wav = core.Synthesize(job.Text, lang, steps, spd);

            Directory.CreateDirectory(Path.GetDirectoryName(job.ExpectedBakedPath)!);
            Supertonic.Helper.WriteWavFile(job.ExpectedBakedPath, wav, core.SampleRate);

            AppendLog(log, $"[OK]   {job.ScenarioIdentifier}/{job.NodeIdentifier}  \"{Truncate(job.Text, 30)}\"");
          }

          AppendLog(log, $"완료 — {jobs.Count}개 인라인 세그먼트 처리됨");

          EditorApplication.delayCall += () =>
          {
            AssetDatabase.Refresh();
            Rescan();
            Debug.Log("[ScenarioInlineAudioBaker] 굽기 완료.");
          };
        }
        catch (Exception ex)
        {
          EditorApplication.delayCall += () => _lastError = $"오류: {ex.Message}";
          Debug.LogError($"[ScenarioInlineAudioBaker] {ex}");
        }
        finally
        {
          EditorApplication.delayCall += () =>
          {
            _isBaking   = false;
            _progress   = 1f;
            _statusText = "완료";
            Repaint();
          };
        }
      });
    }

    // =========================================================================
    // 진행/로그 유틸
    // =========================================================================

    private void UpdateProgress(float progress, string status)
    {
      EditorApplication.delayCall += () =>
      {
        _progress   = progress;
        _statusText = status;
        Repaint();
      };
    }

    private static void AppendLog(List<string> log, string msg)
    {
      EditorApplication.delayCall += () => log.Add(msg);
    }

    private static string Truncate(string text, int max)
    {
      if (string.IsNullOrEmpty(text)) return "";
      return text.Length <= max ? text : text.Substring(0, max) + "…";
    }
  }
}
#endif
