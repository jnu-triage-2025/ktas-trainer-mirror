#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using TextToSpeechService;
using UnityEditor;
using UnityEngine;

namespace MultiplayerInfrastructure.Editor.TTS
{
  /// <summary>
  /// transcripts.json의 Static 세그먼트를 사전 합성(bake)하는 에디터 창.
  ///
  /// UI/프롬프트만 담당하며, 실제 합성/파일 저장 로직은
  /// TextToSpeechService 모듈의 <see cref="TTSStaticBaker"/> 코어에 위임한다.
  ///
  /// 저장 경로: Assets/StreamingAssets/TTS/Baked/{identifier}/{segmentIndex}.wav
  /// 메뉴: Tools > Text to Speech Service > Bake Static Audio
  /// </summary>
  public class TTSStaticAudioBakerWindow : EditorWindow
  {
    private string _transcriptJsonPath = "";
    private string _onnxDirPath = "";
    private string _voiceStylePath = "";
    private string _language = "ko";
    private int _totalStep = 5;
    private float _speed = 1.05f;
    private bool _overwrite = false;

    private bool _isBaking;
    private float _progress;
    private string _statusText = "";
    private string _lastError = "";
    private Vector2 _scroll;
    private readonly List<string> _log = new();

    private void OnEnable()
    {
      string sa = Application.streamingAssetsPath;
      _transcriptJsonPath = Path.Combine(sa, "transcripts.json");
      _onnxDirPath = TTSCore.GetOnnxDir(sa);
      _voiceStylePath = TTSCore.GetVoiceStylePath(sa, "F1");
    }

    [MenuItem("Tools/Text to Speech Service/Bake Static Audio")]
    public static void OpenWindow()
    {
      var win = GetWindow<TTSStaticAudioBakerWindow>("TTS Static Audio Baker");
      win.minSize = new Vector2(500, 380);
      win.Show();
    }

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
        _onnxDirPath = EditorGUILayout.TextField("ONNX 디렉터리", _onnxDirPath);
        _voiceStylePath = EditorGUILayout.TextField("음성 스타일", _voiceStylePath);

        EditorGUILayout.Space(4);
        _language = EditorGUILayout.TextField("언어 코드", _language);
        _totalStep = EditorGUILayout.IntField("Diffusion 스텝", _totalStep);
        _speed = EditorGUILayout.FloatField("속도 배율", _speed);
        _overwrite = EditorGUILayout.Toggle("기존 파일 덮어쓰기", _overwrite);
      }

      if (!string.IsNullOrEmpty(_lastError))
        EditorGUILayout.HelpBox(_lastError, MessageType.Error);

      EditorGUILayout.Space(8);

      if (_isBaking)
      {
        EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 20), _progress, _statusText);
      }
      else
      {
        if (GUILayout.Button("Static 오디오 굽기"))
          StartBaking();
      }

      EditorGUILayout.Space(6);
      EditorGUILayout.LabelField("작업 로그", EditorStyles.boldLabel);
      _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(120));
      foreach (var line in _log)
        EditorGUILayout.LabelField(line, EditorStyles.miniLabel);
      EditorGUILayout.EndScrollView();
    }

    private void StartBaking()
    {
      _lastError = "";
      _isBaking = true;
      _progress = 0f;
      _statusText = "준비 중...";
      _log.Clear();

      if (!File.Exists(_transcriptJsonPath))
      {
        _lastError = $"Transcript JSON을 찾을 수 없습니다: {_transcriptJsonPath}";
        _isBaking = false;
        return;
      }

      if (!TTSCore.AreModelsPresent(_onnxDirPath))
      {
        _lastError = "ONNX 모델이 없습니다. 먼저 Tools > Text to Speech Service > Download Models 를 실행하세요.";
        _isBaking = false;
        return;
      }

      string transcriptJson = _transcriptJsonPath;
      string onnxDir = _onnxDirPath;
      string stylePath = _voiceStylePath;
      string lang = _language;
      int steps = _totalStep;
      float spd = _speed;
      bool overwrite = _overwrite;
      string streamingRoot = Application.streamingAssetsPath;
      var log = _log;

      _ = Task.Run(() =>
      {
        try
        {
          TTSStaticBaker.BakeAll(
            transcriptJson, streamingRoot, onnxDir, stylePath,
            lang, steps, spd, overwrite,
            onProgress: p =>
            {
              EditorApplication.delayCall += () =>
              {
                _progress = p.Ratio;
                _statusText = p.Skipped
                  ? $"건너뜀 {p.Identifier}/{p.Index}.wav"
                  : $"합성 중 {p.Identifier}/{p.Index}.wav";
                log.Add(p.Skipped
                  ? $"[SKIP] {p.Identifier}/{p.Index}.wav"
                  : $"[OK]   {p.Identifier}/{p.Index}.wav");
                Repaint();
              };
            });

          EditorApplication.delayCall += () =>
          {
            AssetDatabase.Refresh();
            log.Add("완료.");
            Debug.Log("[TTSStaticAudioBaker] 굽기 완료.");
          };
        }
        catch (Exception ex)
        {
          EditorApplication.delayCall += () => _lastError = $"오류: {ex.Message}";
          Debug.LogError($"[TTSStaticAudioBaker] {ex}");
        }
        finally
        {
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
  }
}
#endif
