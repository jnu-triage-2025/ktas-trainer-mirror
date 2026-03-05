using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TextToSpeechService
{
  /// <summary>
  /// Supertonic TTS 엔진의 순수 C# 래퍼.
  /// UnityEngine에 의존하지 않으므로 에디터 스크립트와 런타임 코드 양쪽에서 사용할 수 있습니다.
  /// </summary>
  public class TTSCore : IDisposable
  {
    // =========================================================================
    // 경로 상수
    // =========================================================================

    // ── StreamingAssets 기준 상대 경로 ──────────────────────────────────────
    // 에디터:  Application.streamingAssetsPath = {ProjectRoot}/Assets/StreamingAssets
    // 빌드:    Unity가 자동으로 플랫폼별 StreamingAssets 경로로 복사

    /// <summary>ONNX 모델 폴더 (StreamingAssets 기준 상대 경로)</summary>
    public const string DefaultOnnxSubdir    = "TTS/Models/onnx";

    /// <summary>음성 스타일 폴더 (StreamingAssets 기준 상대 경로)</summary>
    public const string DefaultStyleSubdir   = "TTS/Models/voice_styles";

    /// <summary>사전 합성(baked) WAV 폴더 (StreamingAssets 기준 상대 경로)</summary>
    public const string BakedAudioSubdir     = "TTS/Baked";

    // HuggingFace 다운로드 베이스 URL (git-lfs 없이 직접 HTTP)
    // /resolve/{revision}/{path} 엔드포인트는 git-lfs·Xet 스토리지를 투명하게
    // 프록시하여 실제 바이너리를 반환합니다.
    public const string HFBaseUrl =
      "https://huggingface.co/Supertone/supertonic-2/resolve/main/";

    /// <summary>ONNX 모델 디렉터리에 필요한 파일 목록 (HF 상대 경로)</summary>
    public static readonly string[] RequiredOnnxFiles =
    {
      "onnx/duration_predictor.onnx",
      "onnx/text_encoder.onnx",
      "onnx/vector_estimator.onnx",
      "onnx/vocoder.onnx",
      "onnx/tts.json",
      "onnx/unicode_indexer.json",
    };

    /// <summary>다운로드 가능한 음성 스타일 파일 목록 (HF 상대 경로)</summary>
    public static readonly string[] VoiceStyleFiles =
    {
      "voice_styles/F1.json",
      "voice_styles/F2.json",
      "voice_styles/F3.json",
      "voice_styles/F4.json",
      "voice_styles/F5.json",
      "voice_styles/M1.json",
      "voice_styles/M2.json",
      "voice_styles/M3.json",
      "voice_styles/M4.json",
      "voice_styles/M5.json",
    };

    // =========================================================================
    // StreamingAssets 기반 경로 헬퍼 (runtime · editor 공용)
    // =========================================================================

    /// <summary>절대 ONNX 디렉터리 경로를 반환합니다.</summary>
    /// <param name="streamingAssetsPath">Application.streamingAssetsPath</param>
    public static string GetOnnxDir(string streamingAssetsPath) =>
      Path.Combine(streamingAssetsPath, DefaultOnnxSubdir);

    /// <summary>절대 음성 스타일 파일 경로를 반환합니다.</summary>
    /// <param name="streamingAssetsPath">Application.streamingAssetsPath</param>
    /// <param name="styleName">파일명 (확장자 선택, 예: "F1" 또는 "F1.json")</param>
    public static string GetVoiceStylePath(string streamingAssetsPath, string styleName)
    {
      string fileName = styleName.EndsWith(".json") ? styleName : styleName + ".json";
      return Path.Combine(streamingAssetsPath, DefaultStyleSubdir, fileName);
    }

    /// <summary>절대 음성 스타일 디렉터리 경로를 반환합니다.</summary>
    public static string GetVoiceStyleDir(string streamingAssetsPath) =>
      Path.Combine(streamingAssetsPath, DefaultStyleSubdir);

    // =========================================================================
    // 상태
    // =========================================================================


    private readonly Supertonic.TextToSpeech _tts;
    private readonly Supertonic.Style _style;

    /// <summary>합성된 오디오의 샘플레이트</summary>
    public int SampleRate => _tts.SampleRate;

    // =========================================================================
    // 생성자
    // =========================================================================

    /// <param name="onnxDir">ONNX 모델 디렉터리 절대 경로</param>
    /// <param name="voiceStylePath">음성 스타일 JSON 파일 절대 경로</param>
    public TTSCore(string onnxDir, string voiceStylePath)
    {
      _tts   = Supertonic.Helper.LoadTextToSpeech(onnxDir, useGpu: false);
      _style = Supertonic.Helper.LoadVoiceStyle(new List<string> { voiceStylePath });
    }

    // =========================================================================
    // 합성
    // =========================================================================

    /// <summary>
    /// 텍스트를 TTS 합성하여 원시 PCM float[] 샘플을 반환합니다.
    /// </summary>
    /// <param name="text">합성할 텍스트</param>
    /// <param name="lang">언어 코드 (en, ko, es, pt, fr)</param>
    /// <param name="totalStep">Diffusion 스텝 수</param>
    /// <param name="speed">발화 속도 배율</param>
    /// <returns>PCM 샘플 배열 (SampleRate Hz, mono, float32)</returns>
    public float[] Synthesize(string text, string lang, int totalStep = 5, float speed = 1.05f)
    {
      var (wav, _) = _tts.Call(text, lang, _style, totalStep, speed);
      return wav;
    }

    // =========================================================================
    // 유틸리티
    // =========================================================================

    /// <summary>
    /// ONNX 모델 디렉터리에 필수 파일이 모두 존재하는지 확인합니다.
    /// </summary>
    /// <param name="onnxDir">ONNX 모델 디렉터리 절대 경로</param>
    /// <returns>누락된 파일 목록 (비어 있으면 모두 존재)</returns>
    public static List<string> GetMissingModelFiles(string onnxDir)
    {
      var missing = new List<string>();
      foreach (var hfRelPath in RequiredOnnxFiles)
      {
        // HF 상대 경로의 파일명 부분만 추출 (onnx/foo.onnx → foo.onnx)
        string filename  = Path.GetFileName(hfRelPath);
        string localPath = Path.Combine(onnxDir, filename);
        if (!File.Exists(localPath))
          missing.Add(filename);
      }
      return missing;
    }

    /// <summary>ONNX 모델 디렉터리에 필수 파일이 모두 존재하는지 확인합니다.</summary>
    public static bool AreModelsPresent(string onnxDir) =>
      GetMissingModelFiles(onnxDir).Count == 0;

    // =========================================================================
    // Baked 오디오 경로 헬퍼
    // =========================================================================

    /// <summary>
    /// Static 세그먼트에 대한 사전 합성(baked) WAV 파일의 절대 경로를 반환합니다.
    /// </summary>
    /// <param name="streamingAssetsPath">Application.streamingAssetsPath</param>
    /// <param name="identifier">Transcript 식별자</param>
    /// <param name="segmentIndex">세그먼트 인덱스</param>
    public static string GetBakedClipPath(
      string streamingAssetsPath, string identifier, int segmentIndex)
    {
      return Path.Combine(
        streamingAssetsPath, BakedAudioSubdir,
        identifier, $"{segmentIndex}.wav");
    }

    public void Dispose() { /* ONNX 세션은 GC에서 해제됨 */ }
  }
}
