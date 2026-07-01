#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using TextToSpeechService;

namespace MultiplayerInfrastructure.Editor.TTS
{
  /// <summary>
  /// 빌드 전처리기: 빌드 시작 전 ONNX 모델 파일이 없으면 자동 다운로드하고,
  /// 시나리오 인라인 TTS bake 상태가 오래되었으면 경고한다.
  ///
  /// 통합 정책(빌드 게이팅/경고)만 담당하며, 실제 다운로드 로직은
  /// TextToSpeechService 모듈의 <see cref="TTSModelDownloader"/> 코어에 위임한다.
  ///
  /// 다운로드 경로: StreamingAssets/TTS/Models/ → Unity가 빌드 산출물에 자동 포함.
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
        TTSModelDownloader.DownloadMissingModelsSync(sa, missing);
      }
      catch (Exception ex)
      {
        throw new BuildFailedException(
          $"[TTS] 모델 파일 다운로드 실패: {ex.Message}\n" +
          "TTS > Download Models 메뉴에서 수동으로 다운로드하거나 네트워크를 확인하세요.");
      }

      AssetDatabase.Refresh();
      Debug.Log("[TTSBuildPreprocessor] 모델 다운로드 완료. 빌드를 계속합니다.");

      WarnIfInlineBakeStale(sa);
    }

    /// <summary>
    /// PlayTTS가 켜진 시나리오 인라인 텍스트 중 미bake/dirty 항목이 있으면 경고한다.
    /// baked WAV는 StreamingAssets/TTS/BakedInline/ 아래에서 빌드 산출물에 자동 포함되며,
    /// 누락분은 런타임 즉석 합성으로만 재생된다. 빌드를 중단하지는 않는다.
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
  }
}
#endif
