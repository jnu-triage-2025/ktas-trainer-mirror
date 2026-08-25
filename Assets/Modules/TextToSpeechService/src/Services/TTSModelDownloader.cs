using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TextToSpeechService
{
  /// <summary>
  /// Supertonic ONNX 모델 · 음성 스타일 파일을 HuggingFace에서 HTTP로 직접 내려받는 순수 C# 코어.
  ///
  /// UnityEngine / UnityEditor에 의존하지 않으므로 에디터 창, 빌드 전처리기 등
  /// 어느 컨텍스트에서도 재사용할 수 있다. UI·경고·프롬프트는 호출 측(예: MultiplayerInfrastructure)의 책임이다.
  /// </summary>
  public static class TTSModelDownloader
  {
    /// <summary>
    /// 다운로드 대상 매니페스트: (HF 상대 경로, StreamingAssets 기준 저장 서브디렉터리).
    /// TTSCore의 필수 파일/음성 스타일 목록과 일치한다.
    /// </summary>
    public static readonly (string hfPath, string subDir)[] DownloadManifest =
    {
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

    /// <summary>매니페스트 기준으로 현재 존재하는 파일 수를 센다.</summary>
    public static int CountPresentFiles(string streamingAssetsPath)
    {
      int count = 0;
      foreach (var (hfPath, subDir) in DownloadManifest)
      {
        string destPath = Path.Combine(streamingAssetsPath, subDir, Path.GetFileName(hfPath));
        if (File.Exists(destPath))
          count++;
      }
      return count;
    }

    /// <summary>다운로드 진행 상황 콜백 인자.</summary>
    public readonly struct Progress
    {
      public Progress(string fileName, int doneCount, int totalCount)
      {
        FileName = fileName;
        DoneCount = doneCount;
        TotalCount = totalCount;
      }

      public string FileName { get; }
      public int DoneCount { get; }
      public int TotalCount { get; }
      public float Ratio => TotalCount == 0 ? 1f : (float)DoneCount / TotalCount;
    }

    /// <summary>
    /// 매니페스트 전체(또는 누락분)를 비동기로 다운로드한다.
    /// </summary>
    /// <param name="streamingAssetsPath">Application.streamingAssetsPath</param>
    /// <param name="onlyMissing">true이면 이미 존재하는 파일은 건너뛴다.</param>
    /// <param name="onProgress">파일 단위 진행 콜백(호출 스레드는 백그라운드).</param>
    /// <param name="token">취소 토큰.</param>
    public static async Task DownloadManifestAsync(
      string streamingAssetsPath,
      bool onlyMissing,
      Action<Progress> onProgress = null,
      CancellationToken token = default)
    {
      using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
      http.DefaultRequestHeaders.Add("User-Agent", "Unity-TTSModelDownloader/1.0");

      int total = DownloadManifest.Length;
      for (int i = 0; i < total; i++)
      {
        token.ThrowIfCancellationRequested();

        var (hfPath, subDir) = DownloadManifest[i];
        string destDir = Path.Combine(streamingAssetsPath, subDir);
        string filename = Path.GetFileName(hfPath);
        string destPath = Path.Combine(destDir, filename);

        if (onlyMissing && File.Exists(destPath))
        {
          onProgress?.Invoke(new Progress(filename, i + 1, total));
          continue;
        }

        Directory.CreateDirectory(destDir);
        using var response = await http.GetAsync(TTSCore.HFBaseUrl + hfPath, token);
        response.EnsureSuccessStatusCode();
        byte[] bytes = await response.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(destPath, bytes, token);

        onProgress?.Invoke(new Progress(filename, i + 1, total));
      }
    }

    /// <summary>
    /// 누락된 필수 모델 파일만 동기적으로 다운로드한다(빌드 전처리기 등 동기 컨텍스트용).
    /// missing은 파일명만 담고 있으므로 HF 상대 경로를 재구성한다.
    /// </summary>
    public static void DownloadMissingModelsSync(
      string streamingAssetsPath, IEnumerable<string> missingFileNames)
    {
      Task.Run(async () =>
      {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        http.DefaultRequestHeaders.Add("User-Agent", "Unity-TTSBuildPreprocessor/1.0");

        foreach (var filename in missingFileNames)
        {
          string hfRelPath = GetHFPath(filename);
          if (hfRelPath == null)
            continue;

          string destDir = Path.Combine(streamingAssetsPath, TTSCore.DefaultOnnxSubdir);
          string destPath = Path.Combine(destDir, filename);
          Directory.CreateDirectory(destDir);

          byte[] bytes = await http.GetByteArrayAsync(TTSCore.HFBaseUrl + hfRelPath);
          await File.WriteAllBytesAsync(destPath, bytes);
        }
      }).GetAwaiter().GetResult();
    }

    /// <summary>파일명으로 HuggingFace 상대 경로를 역추적한다.</summary>
    public static string GetHFPath(string filename)
    {
      foreach (var hfPath in TTSCore.RequiredOnnxFiles)
        if (Path.GetFileName(hfPath) == filename)
          return hfPath;

      foreach (var hfPath in TTSCore.VoiceStyleFiles)
        if (Path.GetFileName(hfPath) == filename)
          return hfPath;

      return null;
    }
  }
}
