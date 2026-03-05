#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TextToSpeechService.Editor
{
  /// <summary>
  /// Supertonic ONNX 모델 파일을 HuggingFace에서 HTTP로 직접 다운로드하는 에디터 창.
  /// 메뉴: TTS > Download Models
  /// </summary>
  public class TTSModelDownloaderWindow : EditorWindow
  {
    // =========================================================================
    // 설정
    // =========================================================================

    private string _onnxDestDir  = "Assets/Modules/TextToSpeechService/Supertonic/onnx";
    private string _styleDestDir = "Assets/Modules/TextToSpeechService/Supertonic/voice_styles";

    // 다운로드할 HF 파일 목록: (HF 상대 경로, 로컬 저장 서브디렉터리)
    private static readonly (string hfPath, string subDir)[] DownloadManifest = {
      ("onnx/duration_predictor.onnx",  TTSCore.DefaultOnnxSubdir),
      ("onnx/text_encoder.onnx",        TTSCore.DefaultOnnxSubdir),
      ("onnx/vector_estimator.onnx",    TTSCore.DefaultOnnxSubdir),
      ("onnx/vocoder.onnx",             TTSCore.DefaultOnnxSubdir),
      ("onnx/tts.json",                 TTSCore.DefaultOnnxSubdir),
      ("onnx/unicode_indexer.json",     TTSCore.DefaultOnnxSubdir),
      ("voice_styles/F1.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/F2.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/F3.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/F4.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/F5.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/M1.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/M2.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/M3.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/M4.json",          TTSCore.DefaultStyleSubdir),
      ("voice_styles/M5.json",          TTSCore.DefaultStyleSubdir),
    };

    // =========================================================================
    // 상태
    // =========================================================================

    private bool   _isDownloading;
    private string _currentFile = "";
    private float  _totalProgress;   // 0–1
    private int    _doneCount;
    private string _lastError = "";
    private CancellationTokenSource _cts;

    // =========================================================================
    // 메뉴 / 창 열기
    // =========================================================================

    [MenuItem("Tools/Text to Speech Service/Download Models")]
    public static void OpenWindow()
    {
      var win = GetWindow<TTSModelDownloaderWindow>("TTS Model Downloader");
      win.minSize = new Vector2(480, 280);
      win.Show();
    }

    // =========================================================================
    // GUI
    // =========================================================================

    private void OnGUI()
    {
      string sa = Application.streamingAssetsPath;

      EditorGUILayout.Space(8);
      EditorGUILayout.LabelField("Supertonic Model Downloader", EditorStyles.boldLabel);
      EditorGUILayout.HelpBox(
        "TTS 모델과 음성 스타일 파일을 다운로드하고 빌드에 포함하도록 합니다. (~270 MB)",
        MessageType.Info);

      EditorGUILayout.Space(4);
      using (new EditorGUI.DisabledGroupScope(true))
      {
        EditorGUILayout.TextField("ONNX 저장 경로",  Path.Combine(sa, TTSCore.DefaultOnnxSubdir));
        EditorGUILayout.TextField("스타일 저장 경로", Path.Combine(sa, TTSCore.DefaultStyleSubdir));
      }

      EditorGUILayout.Space(8);

      // 파일 상태 요약
      int presentCount = CountPresentFiles(sa);
      int totalFiles   = DownloadManifest.Length;
      EditorGUILayout.LabelField("파일 상태",
        $"{presentCount} / {totalFiles}개 존재", EditorStyles.miniLabel);

      if (!string.IsNullOrEmpty(_lastError))
        EditorGUILayout.HelpBox(_lastError, MessageType.Error);

      EditorGUILayout.Space(8);

      if (_isDownloading)
      {
        EditorGUI.ProgressBar(
          EditorGUILayout.GetControlRect(false, 20),
          _totalProgress,
          $"{_doneCount}/{totalFiles}  {_currentFile}");

        EditorGUILayout.Space(4);
        if (GUILayout.Button("취소"))
          _cts?.Cancel();
      }
      else
      {
        using (new EditorGUILayout.HorizontalScope())
        {
          if (GUILayout.Button("모두 다운로드"))
            StartDownload(sa, onlyMissing: false);

          if (GUILayout.Button("누락 파일만 다운로드"))
            StartDownload(sa, onlyMissing: true);
        }
      }
    }

    // =========================================================================
    // 다운로드 로직
    // =========================================================================

    private void StartDownload(string streamingAssets, bool onlyMissing)
    {
      _isDownloading = true;
      _lastError     = "";
      _doneCount     = 0;
      _totalProgress = 0f;
      _cts           = new CancellationTokenSource();
      var token      = _cts.Token;
      var sa         = streamingAssets;

      _ = Task.Run(async () =>
      {
        try
        {
          using var http = new HttpClient();
          http.DefaultRequestHeaders.Add("User-Agent", "Unity-TTSModelDownloader/1.0");

          int total = DownloadManifest.Length;
          for (int i = 0; i < total; i++)
          {
            token.ThrowIfCancellationRequested();

            var (hfPath, localSubDir) = DownloadManifest[i];
            string destDir  = LocalPath(sa, localSubDir);
            string filename = Path.GetFileName(hfPath);
            string destPath = Path.Combine(destDir, filename);

            if (onlyMissing && File.Exists(destPath))
            {
              UpdateProgress(filename, i + 1, total);
              continue;
            }

            UpdateStatus(filename);

            Directory.CreateDirectory(destDir);
            using var response = await http.GetAsync(TTSCore.HFBaseUrl + hfPath, token);
            response.EnsureSuccessStatusCode();
            byte[] bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(destPath, bytes, token);

            UpdateProgress(filename, i + 1, total);
          }

          EditorApplication.delayCall += () =>
          {
            AssetDatabase.Refresh();
            Debug.Log("[TTSModelDownloader] 다운로드 완료.");
          };
        }
        catch (OperationCanceledException)
        {
          Debug.Log("[TTSModelDownloader] 다운로드 취소됨.");
        }
        catch (Exception ex)
        {
          EditorApplication.delayCall += () => _lastError = $"다운로드 오류: {ex.Message}";
          Debug.LogError($"[TTSModelDownloader] {ex}");
        }
        finally
        {
          EditorApplication.delayCall += () =>
          {
            _isDownloading = false;
            Repaint();
          };
        }
      }, token);
    }

    private void UpdateStatus(string filename)
    {
      EditorApplication.delayCall += () =>
      {
        _currentFile = filename;
        Repaint();
      };
    }

    private void UpdateProgress(string filename, int done, int total)
    {
      EditorApplication.delayCall += () =>
      {
        _currentFile   = filename;
        _doneCount     = done;
        _totalProgress = (float)done / total;
        Repaint();
      };
    }

    private int CountPresentFiles(string streamingAssets)
    {
      int count = 0;
      foreach (var (hfPath, localSubDir) in DownloadManifest)
      {
        string destPath = Path.Combine(LocalPath(streamingAssets, localSubDir), Path.GetFileName(hfPath));
        if (File.Exists(destPath)) count++;
      }
      return count;
    }

    private void OnDisable() => _cts?.Cancel();

    private static string LocalPath(string basePath, string subDir) =>
      Path.Combine(basePath, subDir);
  }
}
#endif
