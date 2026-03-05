#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TextToSpeechService.Editor
{
  /// <summary>
  /// Transcript JSON의 Static 세그먼트를 에디터에서 사전 합성하여
  /// StreamingAssets/TTS/Baked/ 에 WAV 파일로 저장하는 에디터 창.
  ///
  /// 저장 경로 규칙:
  ///   Assets/StreamingAssets/TTS/Baked/{identifier}/{segmentIndex}.wav
  ///
  /// 메뉴: TTS > Bake Static Audio
  /// </summary>
  public class StaticAudioBakerWindow : EditorWindow
  {
    // =========================================================================
    // 설정
    // =========================================================================

    private string _transcriptJsonPath = "";
    private string _onnxDirPath        = "";
    private string _voiceStylePath     = "";
    private string _language   = "ko";
    private int    _totalStep  = 5;
    private float  _speed      = 1.05f;
    private bool   _overwrite  = false;

    private void OnEnable()
    {
      string sa      = Application.streamingAssetsPath;
      _transcriptJsonPath = Path.Combine(sa, "transcripts.json");
      _onnxDirPath        = TTSCore.GetOnnxDir(sa);
      _voiceStylePath     = TTSCore.GetVoiceStylePath(sa, "F1");
    }

    // =========================================================================
    // 상태
    // =========================================================================

    private bool   _isBaking;
    private float  _progress;
    private string _statusText = "";
    private string _lastError  = "";
    private Vector2 _scroll;

    // 결과 로그
    private readonly List<string> _log = new();

    // =========================================================================
    // 메뉴 / 창 열기
    // =========================================================================

    [MenuItem("Tools/Text to Speech Service/Bake Static Audio")]
    public static void OpenWindow()
    {
      var win = GetWindow<StaticAudioBakerWindow>("TTS Static Audio Baker");
      win.minSize = new Vector2(500, 380);
      win.Show();
    }

    // =========================================================================
    // GUI
    // =========================================================================

    private void OnGUI()
    {
      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Static Audio Baker", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "Transcript JSON의 Static 세그먼트를 TTS 합성하여 WAV 파일로 저장합니다.",
        MessageType.Info);

      EditorGUILayout.Space(4);

      using (new EditorGUI.DisabledScope(_isBaking))
      {
        _transcriptJsonPath = EditorGUILayout.TextField("Transcript JSON", _transcriptJsonPath);
        _onnxDirPath        = EditorGUILayout.TextField("ONNX 디렉터리",   _onnxDirPath);
        _voiceStylePath     = EditorGUILayout.TextField("음성 스타일",      _voiceStylePath);

        EditorGUILayout.Space(4);
        _language  = EditorGUILayout.TextField("언어 코드", _language);
        _totalStep = EditorGUILayout.IntField("Diffusion 스텝", _totalStep);
        _speed     = EditorGUILayout.FloatField("속도 배율",  _speed);
        _overwrite = EditorGUILayout.Toggle("기존 파일 덮어쓰기", _overwrite);
      }

      if (!string.IsNullOrEmpty(_lastError))
        EditorGUILayout.HelpBox(_lastError, MessageType.Error);

      EditorGUILayout.Space(8);

      if (_isBaking)
      {
        EditorGUI.ProgressBar(
          EditorGUILayout.GetControlRect(false, 20),
          _progress,
          _statusText);
      }
      else
      {
        if (GUILayout.Button("Static 오디오 굽기"))
          StartBaking();
      }

      // 로그
      EditorGUILayout.Space(6);
      EditorGUILayout.LabelField("작업 로그", EditorStyles.boldLabel);
      _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120));
      foreach (var line in _log)
        EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
      EditorGUILayout.EndScrollView();
    }

    // =========================================================================
    // 굽기 로직
    // =========================================================================

    private void StartBaking()
    {
      _lastError  = "";
      _isBaking   = true;
      _progress   = 0f;
      _statusText = "준비 중...";
      _log.Clear();

      // 유효성 검증
      if (!File.Exists(_transcriptJsonPath))
      {
        _lastError = $"Transcript JSON을 찾을 수 없습니다: {_transcriptJsonPath}";
        _isBaking  = false;
        return;
      }

      if (!TTSCore.AreModelsPresent(_onnxDirPath))
      {
        _lastError = $"ONNX 모델이 없습니다. 먼저 TTS > Download Models 를 실행하세요.";
        _isBaking  = false;
        return;
      }

      string   transcriptJson = _transcriptJsonPath;
      string   onnxDir        = _onnxDirPath;
      string   stylePath      = _voiceStylePath;
      string   lang           = _language;
      int      steps          = _totalStep;
      float    spd            = _speed;
      bool     overwrite      = _overwrite;
      string   streamingRoot  = Application.streamingAssetsPath;

      var capturedLog = _log;

      _ = Task.Run(() =>
      {
        try
        {
          // JSON 파싱
          string rawJson      = File.ReadAllText(transcriptJson);
          var    transcripts  = JsonSerializer.Deserialize<SpeechTranscript[]>(
            rawJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
          ) ?? Array.Empty<SpeechTranscript>();

          // TTS 엔진 초기화
          using var core = new TTSCore(onnxDir, stylePath);

          // 처리할 작업 수집
          var tasks = new List<(string identifier, int index, string text)>();
          foreach (var transcript in transcripts)
          {
            var segments = TranscriptParser.Parse(transcript);
            for (int i = 0; i < segments.Count; i++)
            {
              if (segments[i].Type == SegmentType.Static)
                tasks.Add((transcript.Identifier, i, segments[i].Text));
            }
          }

          for (int t = 0; t < tasks.Count; t++)
          {
            var (identifier, idx, text) = tasks[t];
            float prog = (float)t / tasks.Count;

            string outPath = TTSCore.GetBakedClipPath(streamingRoot, identifier, idx);

            if (!overwrite && File.Exists(outPath))
            {
              AppendLog(capturedLog, $"[SKIP] {identifier}/{idx}.wav");
              UpdateProgress(prog, $"건너뜀 {identifier}/{idx}.wav");
              continue;
            }

            UpdateProgress(prog, $"합성 중 {identifier}/{idx}.wav");

            float[] wav = core.Synthesize(text, lang, steps, spd);

            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
            Supertonic.Helper.WriteWavFile(outPath, wav, core.SampleRate);

            AppendLog(capturedLog, $"[OK]   {identifier}/{idx}.wav  \"{TruncateText(text, 30)}\"");
          }

          AppendLog(capturedLog, $"완료 — {tasks.Count}개 Static 세그먼트 처리됨");

          EditorApplication.delayCall += () =>
          {
            AssetDatabase.Refresh();
            Debug.Log("[StaticAudioBaker] 굽기 완료.");
          };
        }
        catch (Exception ex)
        {
          EditorApplication.delayCall += () =>
            _lastError = $"오류: {ex.Message}";
          Debug.LogError($"[StaticAudioBaker] {ex}");
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

    private static string TruncateText(string text, int max) =>
      text.Length <= max ? text : text[..max] + "…";
  }
}
#endif
