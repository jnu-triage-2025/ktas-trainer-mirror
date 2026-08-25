#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TextToSpeechService;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 프로젝트 내 시나리오 그래프를 스캔하여, PlayTTS 플래그가 켜져 있고 변수를 포함하지 않는
  /// 인라인 텍스트(Dialogue/Choice/Quiz)를 사전 합성(bake)하여
  /// StreamingAssets/TTS/BakedInline/ 에 WAV로 저장하는 에디터 창.
  ///
  /// 저장 경로 규칙:
  ///   · 기본 목소리:  TTS/BakedInline/{scenarioIdentifier}/{nodeIdentifier}_{hash}.wav
  ///   · 지정 목소리: TTS/BakedInline/{scenarioIdentifier}/{voiceIdentifier}/{nodeIdentifier}_{hash}.wav
  ///
  /// 메뉴: Tools > Text to Speech Service > Bake Scenario Inline Audio
  /// </summary>
  public class ScenarioInlineAudioBakerWindow : EditorWindow
  {
    // =========================================================================
    // 설정
    // =========================================================================

    private string _onnxDirPath = "";
    private string _voiceStylePath = "";
    private string _language = "ko";
    private int _totalStep = 5;
    private float _speed = 1.05f;
    // voice 경로가 변경된 첫 베이크에서 기존 flat-path 파일이 orphan으로 삭제되는 것을 방지하기 위해
    // 기본값을 false로 설정한다. 사용자가 명시적으로 활성화해야 한다.
    private bool _cleanStale = false;

    // 추가 목소리 프로파일 (VoiceIdentifier → TTSVoiceProfile)
    // 시나리오 노드에 ttsVoiceIdentifier가 설정된 경우 이 매핑으로 스타일을 결정한다.
    private readonly List<TTSVoiceProfile> _extraVoiceProfiles = new();
    private Vector2 _profileScroll;
    private bool _showVoiceProfiles = true;

    private void OnEnable()
    {
      string sa = Application.streamingAssetsPath;
      _onnxDirPath = TTSCore.GetOnnxDir(sa);
      _voiceStylePath = TTSCore.GetVoiceStylePath(sa, "F1");
      Rescan();
    }

    // =========================================================================
    // 상태
    // =========================================================================

    private bool _isBaking;
    private float _progress;
    private string _statusText = "";
    private string _lastError = "";
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
        "PlayTTS가 켜진 Dialogue/DisinteractableDialogue/Choice/Quiz 노드의 변수 없는 인라인 텍스트를 사전 합성합니다.",
        MessageType.Info);

      EditorGUILayout.Space(4);

      using (new EditorGUI.DisabledScope(_isBaking))
      {
        _onnxDirPath = EditorGUILayout.TextField("ONNX 디렉터리", _onnxDirPath);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("기본 목소리 설정", EditorStyles.boldLabel);
        _voiceStylePath = EditorGUILayout.TextField("음성 스타일", _voiceStylePath);
        _language = EditorGUILayout.TextField("언어 코드", _language);
        _totalStep = EditorGUILayout.IntField("Diffusion 스텝", _totalStep);
        _speed = EditorGUILayout.FloatField("속도 배율", _speed);

        EditorGUILayout.Space(4);

        // 추가 목소리 프로파일 편집 UI
        _showVoiceProfiles = EditorGUILayout.Foldout(_showVoiceProfiles, $"추가 목소리 프로파일 ({_extraVoiceProfiles.Count}개)", true);
        if (_showVoiceProfiles)
        {
          EditorGUI.indentLevel++;
          EditorGUILayout.HelpBox(
            "시나리오 노드에 ttsVoiceIdentifier가 설정된 경우 이 목록의 해당 프로파일로 bake됩니다.\n" +
            "VoiceIdentifier는 노드 JSON의 ttsVoiceIdentifier 값과 정확히 일치해야 합니다.",
            MessageType.Info);

          _profileScroll = EditorGUILayout.BeginScrollView(_profileScroll, GUILayout.MaxHeight(160));
          for (int pi = 0; pi < _extraVoiceProfiles.Count; pi++)
          {
            var p = _extraVoiceProfiles[pi];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"프로파일 #{pi + 1}", EditorStyles.boldLabel, GUILayout.Width(90));
            if (GUILayout.Button("삭제", GUILayout.Width(42)))
            {
              _extraVoiceProfiles.RemoveAt(pi);
              EditorGUILayout.EndHorizontal();
              EditorGUILayout.EndVertical();
              break;
            }
            EditorGUILayout.EndHorizontal();

            p.VoiceIdentifier = EditorGUILayout.TextField("VoiceIdentifier", p.VoiceIdentifier);
            p.VoiceStyleName = EditorGUILayout.TextField("음성 스타일명", p.VoiceStyleName);
            p.Language = EditorGUILayout.TextField("언어 코드 (빈칸=기본)", p.Language);
            p.TotalStep = EditorGUILayout.IntField("Diffusion 스텝 (0=기본)", p.TotalStep);
            p.Speed = EditorGUILayout.FloatField("속도 배율 (0=기본)", p.Speed);
            EditorGUILayout.EndVertical();
          }
          EditorGUILayout.EndScrollView();

          if (GUILayout.Button("+ 프로파일 추가"))
            _extraVoiceProfiles.Add(new TTSVoiceProfile { VoiceStyleName = "F1", Language = "ko" });

          EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(4);
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
          string voiceLabel = string.IsNullOrEmpty(job.VoiceIdentifier) ? "" : $" [{job.VoiceIdentifier}]";
          EditorGUILayout.LabelField(
            $"[{state}]{voiceLabel} {job.ScenarioIdentifier}/{job.NodeIdentifier}  \"{Truncate(job.Text, 40)}\"",
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
      if (_scan == null || !_scan.HasOrphans)
        return;

      bool ok = EditorUtility.DisplayDialog(
        "미사용 baked 파일 정리",
        $"현재 어떤 시나리오 노드에서도 사용하지 않는 baked 파일 {_scan.OrphanCount}개를 삭제합니다.\n계속하시겠습니까?",
        "삭제", "취소");
      if (!ok)
        return;

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
      _lastError = "";
      _isBaking = true;
      _progress = 0f;
      _statusText = "준비 중...";
      _log.Clear();

      if (!TTSCore.AreModelsPresent(_onnxDirPath))
      {
        _lastError = "ONNX 모델이 없습니다. 먼저 Tools > Text to Speech Service > Download Models 를 실행하세요.";
        _isBaking = false;
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
        if (onlyNeeded && job.IsBaked && !job.IsDirty)
          continue;
        jobs.Add(job);
      }

      string onnxDir = _onnxDirPath;
      string stylePath = _voiceStylePath;
      string lang = _language;
      int steps = _totalStep;
      float spd = _speed;
      bool cleanStale = _cleanStale;
      var log = _log;
      var sa = Application.streamingAssetsPath;

      // 추가 voice profile 스냅샷 (비동기 작업 중 UI 변경 방지)
      var profileSnapshot = new List<TTSVoiceProfile>(_extraVoiceProfiles.Count);
      foreach (var p in _extraVoiceProfiles)
        profileSnapshot.Add(new TTSVoiceProfile
        {
          VoiceIdentifier = p.VoiceIdentifier,
          VoiceStyleName = p.VoiceStyleName,
          Language = p.Language,
          TotalStep = p.TotalStep,
          Speed = p.Speed
        });

      _ = Task.Run(() =>
      {
        // voice identifier → TTSCore 캐시 (style path → core)
        var coreByStylePath = new Dictionary<string, TTSCore>(StringComparer.Ordinal);

        try
        {
          // 기본 코어 생성
          coreByStylePath[stylePath] = new TTSCore(onnxDir, stylePath);

          // 추가 프로파일 코어 미리 생성
          foreach (var p in profileSnapshot)
          {
            if (string.IsNullOrEmpty(p.VoiceIdentifier) || string.IsNullOrEmpty(p.VoiceStyleName))
              continue;
            string pStylePath = TTSCore.GetVoiceStylePath(sa, p.VoiceStyleName);
            if (!coreByStylePath.ContainsKey(pStylePath))
              coreByStylePath[pStylePath] = new TTSCore(onnxDir, pStylePath);
          }

          // voiceIdentifier → (stylePath, lang, step, spd) 매핑 구성
          var voiceParamsMap = new Dictionary<string, (string sp, string lg, int st, float sd)>(StringComparer.Ordinal);
          foreach (var p in profileSnapshot)
          {
            if (string.IsNullOrEmpty(p.VoiceIdentifier))
              continue;
            string pSp = TTSCore.GetVoiceStylePath(sa, string.IsNullOrEmpty(p.VoiceStyleName) ? "F1" : p.VoiceStyleName);
            string pLg = string.IsNullOrEmpty(p.Language) ? lang : p.Language;
            int pSt = p.TotalStep > 0 ? p.TotalStep : steps;
            float pSd = p.Speed > 0f ? p.Speed : spd;
            voiceParamsMap[p.VoiceIdentifier] = (pSp, pLg, pSt, pSd);
          }

          for (int i = 0; i < jobs.Count; i++)
          {
            var job = jobs[i];
            float prog = jobs.Count == 0 ? 1f : (float)i / jobs.Count;

            UpdateProgress(prog, $"합성 중 {job.ScenarioIdentifier}/{job.NodeIdentifier}");

            // 변경/미사용 stale 파일 정리
            if (cleanStale && job.StaleBakedPaths != null)
            {
              foreach (var stale in job.StaleBakedPaths)
              {
                try
                { if (File.Exists(stale)) File.Delete(stale); }
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

            // voice identifier에 맞는 core/params 결정
            string jobSp = stylePath;
            string jobLg = lang;
            int jobSt = steps;
            float jobSd = spd;

            if (!string.IsNullOrEmpty(job.VoiceIdentifier)
                && voiceParamsMap.TryGetValue(job.VoiceIdentifier, out var vp))
            {
              jobSp = vp.sp;
              jobLg = vp.lg;
              jobSt = vp.st;
              jobSd = vp.sd;
            }
            else if (!string.IsNullOrEmpty(job.VoiceIdentifier))
            {
              AppendLog(log, $"[WARN] VoiceIdentifier '{job.VoiceIdentifier}' 에 대한 프로파일이 없습니다. 기본 스타일로 bake합니다.");
            }

            if (!coreByStylePath.TryGetValue(jobSp, out var core))
            {
              core = new TTSCore(onnxDir, jobSp);
              coreByStylePath[jobSp] = core;
            }

            float[] wav = core.Synthesize(job.Text, jobLg, jobSt, jobSd);

            Directory.CreateDirectory(Path.GetDirectoryName(job.ExpectedBakedPath)!);
            Supertonic.Helper.WriteWavFile(job.ExpectedBakedPath, wav, core.SampleRate);

            string voiceLabel = string.IsNullOrEmpty(job.VoiceIdentifier) ? "" : $" [{job.VoiceIdentifier}]";
            AppendLog(log, $"[OK]{voiceLabel}   {job.ScenarioIdentifier}/{job.NodeIdentifier}  \"{Truncate(job.Text, 30)}\"");
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
          foreach (var c in coreByStylePath.Values)
            try
            { c?.Dispose(); }
            catch { /* ignore */ }

          EditorApplication.delayCall += () =>
          {
            _isBaking = false;
            _progress = 1f;
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
        _progress = progress;
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
      if (string.IsNullOrEmpty(text))
        return "";
      return text.Length <= max ? text : text.Substring(0, max) + "…";
    }
  }
}
#endif
