using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

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
    public const string DefaultOnnxSubdir = "TTS/Models/onnx";

    /// <summary>음성 스타일 폴더 (StreamingAssets 기준 상대 경로)</summary>
    public const string DefaultStyleSubdir = "TTS/Models/voice_styles";

    /// <summary>사전 합성(baked) WAV 폴더 (StreamingAssets 기준 상대 경로)</summary>
    public const string BakedAudioSubdir = "TTS/Baked";

    /// <summary>
    /// 시나리오 그래프의 인라인 텍스트(Dialogue/Choice/Quiz 등)를 사전 합성한
    /// WAV 폴더 (StreamingAssets 기준 상대 경로).
    /// transcripts.json 기반 <see cref="BakedAudioSubdir"/> 와 분리하여 관리한다.
    /// </summary>
    public const string BakedInlineAudioSubdir = "TTS/BakedInline";

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
      _tts = Supertonic.Helper.LoadTextToSpeech(onnxDir, useGpu: false);
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
        string filename = Path.GetFileName(hfRelPath);
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

    // =========================================================================
    // 인라인 텍스트 Baked 경로 / 해시 헬퍼
    // =========================================================================

    /// <summary>
    /// 시나리오 그래프의 인라인 텍스트에 대한 사전 합성(baked) WAV 파일의 절대 경로를 반환합니다.
    ///
    /// 파일명 규칙:
    ///   {BakedInlineAudioSubdir}/{scenarioIdentifier}/{nodeIdentifier}_{hash}.wav
    ///
    /// scenarioIdentifier·nodeIdentifier 는 사람이 식별할 수 있도록 접두어로 붙이고,
    /// hash 는 텍스트 내용 변경(=dirty) 감지를 위한 결정적 해시이다.
    ///
    /// voiceIdentifier 가 null/빈 문자열이면 기존 경로 구조(voice 폴더 없음)를 유지한다.
    /// </summary>
    /// <param name="streamingAssetsPath">Application.streamingAssetsPath</param>
    /// <param name="scenarioIdentifier">시나리오 그래프 식별자</param>
    /// <param name="nodeIdentifier">노드 식별자</param>
    /// <param name="text">합성 대상 텍스트</param>
    /// <param name="voiceIdentifier">
    /// 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리 경로를 사용한다.
    /// </param>
    public static string GetBakedInlineClipPath(
      string streamingAssetsPath, string scenarioIdentifier, string nodeIdentifier, string text,
      string voiceIdentifier = null)
    {
      string safeScenario = SanitizeForFileName(scenarioIdentifier);
      string safeNode = SanitizeForFileName(nodeIdentifier);
      string hash = ComputeTextHash(text);

      if (string.IsNullOrEmpty(voiceIdentifier))
      {
        return Path.Combine(
          streamingAssetsPath, BakedInlineAudioSubdir,
          safeScenario, $"{safeNode}_{hash}.wav");
      }

      string safeVoice = SanitizeForFileName(voiceIdentifier);
      return Path.Combine(
        streamingAssetsPath, BakedInlineAudioSubdir,
        safeScenario, safeVoice, $"{safeNode}_{hash}.wav");
    }

    /// <summary>인라인 텍스트에 대한 결정적 콘텐츠 해시(짧은 hex)를 계산합니다.</summary>
    public static string ComputeTextHash(string text)
    {
      using var sha = SHA256.Create();
      byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? string.Empty));
      var sb = new StringBuilder(16);
      for (int i = 0; i < 8; i++) // 앞 8바이트(16 hex)면 충돌 방지에 충분
        sb.Append(bytes[i].ToString("x2"));
      return sb.ToString();
    }

    /// <summary>파일/폴더명에 사용할 수 없는 문자를 '_' 로 치환합니다.</summary>
    private static string SanitizeForFileName(string value)
    {
      if (string.IsNullOrEmpty(value))
        return "_";
      var invalid = Path.GetInvalidFileNameChars();
      var sb = new StringBuilder(value.Length);
      foreach (char c in value)
        sb.Append(Array.IndexOf(invalid, c) >= 0 ? '_' : c);
      return sb.ToString();
    }

    /// <summary>
    /// ONNX 세션을 네이티브 메모리까지 해제한다.
    ///
    /// 세션을 들고 있는 <see cref="Supertonic.TextToSpeech"/> 는 Supertone의 MIT 배포본을
    /// 그대로 둔 파일이라 IDisposable을 구현하지 않는다. 그 파일을 고치는 대신 여기서
    /// 리플렉션으로 IDisposable 필드를 찾아 해제한다. 배포본의 필드 구성이 달라지면
    /// 해제할 대상이 없을 뿐, 예외로 번지지는 않는다.
    /// </summary>
    public void Dispose()
    {
      if (_disposed)
        return;
      _disposed = true;

      DisposeDisposableFields(_tts);
      GC.SuppressFinalize(this);
    }

    private bool _disposed;

    private static void DisposeDisposableFields(object owner)
    {
      if (owner == null)
        return;

      var fields = owner.GetType().GetFields(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

      foreach (var field in fields)
      {
        if (!typeof(IDisposable).IsAssignableFrom(field.FieldType))
          continue;

        try
        {
          (field.GetValue(owner) as IDisposable)?.Dispose();
        }
        catch (Exception)
        {
          // 이미 해제되었거나 해제할 수 없는 필드는 건너뛴다.
        }
      }
    }
  }
}
