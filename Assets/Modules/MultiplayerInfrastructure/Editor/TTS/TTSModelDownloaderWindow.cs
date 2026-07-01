#if UNITY_EDITOR
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using TextToSpeechService;

namespace MultiplayerInfrastructure.Editor.TTS
{
  /// <summary>
  /// Supertonic ONNX 모델·음성 스타일 파일을 HuggingFace에서 다운로드하는 에디터 창.
  ///
  /// UI/프롬프트만 담당하며, 실제 HTTP 다운로드 로직은
  /// TextToSpeechService 모듈의 <see cref="TTSModelDownloader"/> 코어에 위임한다.
  ///
  /// 메뉴: Tools > Text to Speech Service > Download Models
  /// </summary>
  public class TTSModelDownloaderWindow : EditorWindow
  {
    private bool   _isDownloading;
    private string _currentFile = "";
    private float  _totalProgress;
    private int    _doneCount;
    private string _lastError = "";
    private CancellationTokenSource _cts;

    [MenuItem("Tools/Text to Speech Service/Download Models")]
    public static void OpenWindow()
    {
      var win = GetWindow<TTSModelDownloaderWindow>("TTS Model Downloader");
      win.minSize = new Vector2(480, 280);
      win.Show();
    }

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
        EditorGUILayout.TextField("ONNX 저장 경로",  System.IO.Path.Combine(sa, TTSCore.DefaultOnnxSubdir));
        EditorGUILayout.TextField("스타일 저장 경로", System.IO.Path.Combine(sa, TTSCore.DefaultStyleSubdir));
      }

      EditorGUILayout.Space(8);

      int presentCount = TTSModelDownloader.CountPresentFiles(sa);
      int totalFiles   = TTSModelDownloader.DownloadManifest.Length;
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

    private void StartDownload(string streamingAssets, bool onlyMissing)
    {
      _isDownloading = true;
      _lastError     = "";
      _doneCount     = 0;
      _totalProgress = 0f;
      _cts           = new CancellationTokenSource();
      var token      = _cts.Token;

      _ = Task.Run(async () =>
      {
        try
        {
          await TTSModelDownloader.DownloadManifestAsync(
            streamingAssets, onlyMissing,
            onProgress: p => EditorApplication.delayCall += () =>
            {
              _currentFile   = p.FileName;
              _doneCount     = p.DoneCount;
              _totalProgress = p.Ratio;
              Repaint();
            },
            token: token);

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

    private void OnDisable() => _cts?.Cancel();
  }
}
#endif
