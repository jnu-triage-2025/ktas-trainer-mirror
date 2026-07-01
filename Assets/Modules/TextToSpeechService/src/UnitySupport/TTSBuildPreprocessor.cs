#if UNITY_EDITOR
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TextToSpeechService.Editor
{
  /// <summary>
  /// 빌드 전처리기: Unity 빌드 시작 전 ONNX 모델 파일이 없으면 자동으로 다운로드합니다.
  ///
  /// 동작:
  ///   1. 모델 파일이 모두 있으면 즉시 통과
  ///   2. 누락 파일이 있으면 HuggingFace에서 동기 다운로드
  ///   3. 다운로드 실패 시 BuildFailedException 으로 빌드 중단
  ///
  /// 다운로드 경로: StreamingAssets/TTS/Models/  → Unity 빌드 시 자동으로 빌드 산출물에 포함
  ///
  /// IOrderedCallback.callbackOrder = 0 (다른 전처리기에 앞서 실행)
  /// </summary>
  public class TTSBuildPreprocessor : IPreprocessBuildWithReport
  {
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
      string sa      = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(sa);
      var    missing = TTSCore.GetMissingModelFiles(onnxDir);

      if (missing.Count == 0)
      {
        Debug.Log("[TTSBuildPreprocessor] 모든 모델 파일이 존재합니다. 통과.");
        WarnIfInlineBakeStale(sa);
        return;
      }

      Debug.LogWarning(
        $"[TTSBuildPreprocessor] 누락된 모델 파일 {missing.Count}개 발견. 다운로드를 시작합니다...");

      try
      {
        DownloadMissingModels(sa, missing);
      }
      catch (Exception ex)
      {
        // 빌드 중단
        throw new BuildFailedException(
          $"[TTS] 모델 파일 다운로드 실패: {ex.Message}\n" +
          "TTS > Download Models 메뉴에서 수동으로 다운로드하거나 네트워크를 확인하세요.");
      }

      AssetDatabase.Refresh();
      Debug.Log("[TTSBuildPreprocessor] 모델 다운로드 완료. 빌드를 계속합니다.");

      WarnIfInlineBakeStale(sa);
    }

    /// <summary>
    /// PlayTTS가 켜진 시나리오 인라인 텍스트 중 bake되지 않았거나 변경(dirty)된 항목이
    /// 있으면 빌드 로그에 경고한다. baked WAV는 StreamingAssets/TTS/BakedInline/ 아래에
    /// 위치하여 빌드 산출물에 자동 포함되지만, 누락분은 런타임 즉석 합성으로만 재생된다.
    /// 빌드를 중단하지는 않는다(모델은 필수, 인라인 bake는 선택).
    /// </summary>
    private static void WarnIfInlineBakeStale(string streamingAssets)
    {
      try
      {
        var scan = MultiplayerInfrastructure.Scenario.ScenarioTTSBakeScanner
          .ScanAllScenarios(streamingAssets);

        if (scan.NeedsBake)
        {
          Debug.LogWarning(
            $"[TTSBuildPreprocessor] 시나리오 인라인 TTS bake가 최신이 아닙니다 " +
            $"(미bake {scan.MissingCount}개, 변경됨 {scan.DirtyCount}개). " +
            "Tools > Text to Speech Service > Bake Scenario Inline Audio 에서 사전 합성하면 " +
            "빌드에 포함되어 런타임 합성 부하를 줄일 수 있습니다.");
        }
      }
      catch (Exception ex)
      {
        Debug.LogWarning($"[TTSBuildPreprocessor] 인라인 bake 스캔 실패(무시): {ex.Message}");
      }
    }

    // =========================================================================
    // 동기 다운로드 (빌드 전처리기는 비동기를 지원하지 않음)
    // =========================================================================

    private static void DownloadMissingModels(string streamingAssets, System.Collections.Generic.List<string> missing)
    {
      // Task.Run + GetAwaiter().GetResult() 로 비동기 HTTP를 동기 컨텍스트에서 실행
      Task.Run(async () =>
      {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Unity-TTSBuildPreprocessor/1.0");
        http.Timeout = TimeSpan.FromMinutes(10); // 대용량 ONNX 파일 대비

        // TTSCore.RequiredOnnxFiles 및 VoiceStyleFiles 기준으로 다운로드
        // missing은 파일명만 담고 있으므로 HF 경로를 재구성
        foreach (var filename in missing)
        {
          string hfRelPath = GetHFPath(filename);
          if (hfRelPath == null)
          {
            Debug.LogWarning($"[TTSBuildPreprocessor] HF 경로를 찾을 수 없습니다: {filename}");
            continue;
          }

          string destDir  = Path.Combine(streamingAssets, TTSCore.DefaultOnnxSubdir);
          string destPath = Path.Combine(destDir, filename);

          Directory.CreateDirectory(destDir);

          Debug.Log($"[TTSBuildPreprocessor] 다운로드 중: {filename}");
          byte[] bytes = await http.GetByteArrayAsync(TTSCore.HFBaseUrl + hfRelPath);
          await File.WriteAllBytesAsync(destPath, bytes);
          Debug.Log($"[TTSBuildPreprocessor] 완료: {filename}");
        }
      }).GetAwaiter().GetResult();
    }

    /// <summary>파일명으로 HuggingFace 상대 경로를 반환합니다.</summary>
    private static string GetHFPath(string filename)
    {
      foreach (var hfPath in TTSCore.RequiredOnnxFiles)
      {
        if (Path.GetFileName(hfPath) == filename)
          return hfPath;
      }
      foreach (var hfPath in TTSCore.VoiceStyleFiles)
      {
        if (Path.GetFileName(hfPath) == filename)
          return hfPath;
      }
      return null;
    }
  }
}
#endif
