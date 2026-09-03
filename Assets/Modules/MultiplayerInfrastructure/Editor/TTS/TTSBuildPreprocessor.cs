#if UNITY_EDITOR
using System;
using System.IO;
using TextToSpeechService;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;

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
  public class TTSBuildPreprocessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
  {
    private const string OnnxRuntimePackagePath = "Packages/Microsoft.ML.OnnxRuntime.1.20.1/runtimes";
    private const string OnnxRuntimeLibraryName = "libonnxruntime.dylib";

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
      string sa = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(sa);
      var missing = TTSCore.GetMissingModelFiles(onnxDir);

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
    /// NuGet 패키지의 runtimes 하위 dylib은 Unity가 macOS 플러그인으로 자동 배치하지 않을 수 있다.
    /// 범용 macOS 플레이어에는 arm64와 x86_64 바이너리를 합친 dylib을 Frameworks에 명시적으로 넣는다.
    /// </summary>
    public void OnPostprocessBuild(BuildReport report)
    {
      if (report.summary.platform != BuildTarget.StandaloneOSX)
        return;

      string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
      if (string.IsNullOrWhiteSpace(projectRoot))
        throw new BuildFailedException("[TTS] 프로젝트 루트를 해석하지 못해 ONNX Runtime을 빌드에 포함할 수 없습니다.");

      string arm64Source = Path.Combine(projectRoot, OnnxRuntimePackagePath, "osx-arm64", "native", OnnxRuntimeLibraryName);
      string x64Source = Path.Combine(projectRoot, OnnxRuntimePackagePath, "osx-x64", "native", OnnxRuntimeLibraryName);
      if (!File.Exists(arm64Source) || !File.Exists(x64Source))
      {
        throw new BuildFailedException(
          $"[TTS] macOS ONNX Runtime 네이티브 라이브러리가 없습니다. arm64='{arm64Source}', x64='{x64Source}'");
      }

      string frameworksDirectory = Path.Combine(report.summary.outputPath, "Contents", "Frameworks");
      Directory.CreateDirectory(frameworksDirectory);
      string destination = Path.Combine(frameworksDirectory, OnnxRuntimeLibraryName);
      CreateUniversalMacOsLibrary(arm64Source, x64Source, destination);

      if (!File.Exists(destination) || new FileInfo(destination).Length == 0)
      {
        throw new BuildFailedException(
          $"[TTS] macOS 빌드에 ONNX Runtime을 포함하지 못했습니다: {destination}");
      }

      Debug.Log($"[TTSBuildPreprocessor] ONNX Runtime을 macOS Frameworks에 포함했습니다: {destination}");
    }

    private static void CreateUniversalMacOsLibrary(string arm64Source, string x64Source, string destination)
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = "/usr/bin/lipo",
        Arguments = $"-create -output \"{destination}\" \"{arm64Source}\" \"{x64Source}\"",
        UseShellExecute = false,
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        CreateNoWindow = true,
      };

      using var process = Process.Start(startInfo);
      if (process == null)
        throw new BuildFailedException("[TTS] lipo를 시작하지 못해 macOS ONNX Runtime 범용 라이브러리를 만들 수 없습니다.");

      string output = process.StandardOutput.ReadToEnd();
      string error = process.StandardError.ReadToEnd();
      process.WaitForExit();
      if (process.ExitCode != 0)
      {
        throw new BuildFailedException(
          $"[TTS] lipo가 ONNX Runtime 범용 라이브러리 생성에 실패했습니다 (exit={process.ExitCode}).\n{output}\n{error}");
      }
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
