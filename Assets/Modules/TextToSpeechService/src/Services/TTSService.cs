using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace TextToSpeechService
{
  /// <summary>
  /// TTS 인프라 서비스 (Unity MonoBehaviour).
  ///
  /// 역할:
  ///   · Static 세그먼트는 에디터에서 사전 합성된 WAV (StreamingAssets/TTS/Baked/) 를 우선 로드
  ///   · Dynamic 세그먼트(기본값 포함) 및 baked 파일이 없는 Static은 런타임에 TTSCore로 합성
  ///   · 변수 오버라이드 재생 및 사전 준비(PrepareVariable) 지원
  ///
  /// 의존관계:
  ///   · <see cref="TTSCore"/> — Supertonic ONNX 래퍼, UnityEngine 비의존
  ///   · <see cref="TranscriptParser"/> — basetext 세그먼트 파싱
  /// </summary>
  public class TTSService : MonoBehaviour
  {
    // =========================================================================
    // Inspector 설정
    // =========================================================================

    [Header("파일 경로")]
    [Tooltip("SpeechTranscript[] JSON 파일 (StreamingAssets 기준 상대 경로)")]
    [SerializeField] private string transcriptJsonRelPath = "transcripts.json";

    [Tooltip("음성 스타일 파일명 (TTS/Models/voice_styles/ 하위, 확장자 무관)")]
    [SerializeField] private string voiceStyleName = "F1";

    [Header("TTS 파라미터")]
    [Tooltip("TTS 언어 코드 (en, ko, es, pt, fr)")]
    [SerializeField] private string language = "ko";

    [Tooltip("Diffusion 스텝 수 — 높을수록 음질 향상, 속도 저하")]
    [SerializeField] private int totalStep = 5;

    [Tooltip("발화 속도 배율")]
    [SerializeField] private float speed = 1.05f;

    // =========================================================================
    // 내부 상태
    // =========================================================================

    private TTSCore _core;

    /// <summary>텍스트 → AudioClip 캐시 (Static baked + 런타임 합성 결과)</summary>
    private readonly Dictionary<string, AudioClip> _clipCache = new();

    /// <summary>Identifier → 세그먼트 목록</summary>
    private readonly Dictionary<string, List<SpeechSegment>> _segmentMap = new();

    /// <summary>로드된 Transcript 배열</summary>
    private SpeechTranscript[] _transcripts = Array.Empty<SpeechTranscript>();

    /// <summary>초기화 완료 여부</summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// 동적 세그먼트의 백그라운드 캐싱이 진행 중이거나 아직 완료되지 않음을 나타내는 dirty bit.
    /// PrepareTranscriptVariables 호출 시 true가 되고, 캐싱이 완료되면 false가 됩니다.
    /// </summary>
    public bool IsDynamicCacheDirty => _dynamicCachingCount > 0;

    /// <summary>현재 진행 중인 백그라운드 캐싱 작업 수 (dirty bit 카운터)</summary>
    private int _dynamicCachingCount;

    // =========================================================================
    // Unity 라이프사이클
    // =========================================================================

    private void Awake()
    {
      string sa      = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(sa);

      if (!TTSCore.AreModelsPresent(onnxDir))
      {
        Debug.LogError(
          $"[TTSService] ONNX 모델 파일이 없습니다: {onnxDir}\n" +
          "TTS > Download Models 메뉴에서 모델을 먼저 다운로드하세요.");
        return;
      }

      StartCoroutine(InitializeCoroutine());
    }

    private void OnDestroy()
    {
      _core?.Dispose();
    }

    // =========================================================================
    // 초기화
    // =========================================================================

    /// <summary>
    /// 초기화 순서:
    ///  1) JSON 파싱 + baked WAV 로드 (메인 스레드, UnityWebRequest)
    ///  2) 백그라운드: 모델 로드 + Dynamic/fallback WAV 합성
    ///  3) 메인 스레드: 나머지 AudioClip 구성
    /// </summary>
    private IEnumerator InitializeCoroutine()
    {
      Debug.Log("[TTSService] 초기화 시작...");

      string sa               = Application.streamingAssetsPath;
      string onnxDir          = TTSCore.GetOnnxDir(sa);
      string styleAbsPath     = TTSCore.GetVoiceStylePath(sa, voiceStyleName);
      string transcriptPath   = Path.Combine(sa, transcriptJsonRelPath);

      // ── 1. JSON 파싱 (메인 스레드) ─────────────────────────────────────────
      if (!File.Exists(transcriptPath))
      {
        Debug.LogError($"[TTSService] Transcript JSON을 찾을 수 없습니다: {transcriptPath}");
        yield break;
      }

      string rawJson = File.ReadAllText(transcriptPath);
      _transcripts = JsonSerializer.Deserialize<SpeechTranscript[]>(
        rawJson,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
      ) ?? Array.Empty<SpeechTranscript>();

      foreach (var t in _transcripts)
        _segmentMap[t.Identifier] = TranscriptParser.Parse(t);

      // ── 2. 사전 합성(baked) WAV 로드 (UnityWebRequest) ─────────────────────
      foreach (var transcript in _transcripts)
      {
        var segments = _segmentMap[transcript.Identifier];
        for (int i = 0; i < segments.Count; i++)
        {
          if (segments[i].Type != SegmentType.Static) continue;

          string bakedPath = TTSCore.GetBakedClipPath(
            Application.streamingAssetsPath, transcript.Identifier, i);

          if (!File.Exists(bakedPath)) continue;

          string url    = "file://" + bakedPath;
          using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
          yield return req.SendWebRequest();

          if (req.result == UnityWebRequest.Result.Success)
          {
            var clip  = DownloadHandlerAudioClip.GetContent(req);
            clip.name = segments[i].Text;
            _clipCache[segments[i].Text] = clip;
          }
          else
          {
            Debug.LogWarning($"[TTSService] baked WAV 로드 실패 ({bakedPath}): {req.error}");
          }
        }
      }

      // ── 3. 백그라운드: 모델 로드 + Dynamic/fallback 합성 ──────────────────
      var wavBuffer = new Dictionary<string, float[]>();

      var bgTask = Task.Run(() =>
      {
        _core = new TTSCore(onnxDir, styleAbsPath);

        foreach (var transcript in _transcripts)
        {
          var segments = _segmentMap[transcript.Identifier];
          for (int i = 0; i < segments.Count; i++)
          {
            var seg     = segments[i];
            bool baked  = seg.Type == SegmentType.Static && _clipCache.ContainsKey(seg.Text);
            if (baked) continue;

            string text = seg.Type == SegmentType.Static
              ? seg.Text
              : ResolveDefaultValue(transcript, seg.Text);

            if (text != null && !wavBuffer.ContainsKey(text))
              wavBuffer[text] = _core.Synthesize(text, language, totalStep, speed);
          }
        }
      });

      yield return new WaitUntil(() => bgTask.IsCompleted);

      if (bgTask.IsFaulted)
      {
        Debug.LogError($"[TTSService] 초기화 실패: {bgTask.Exception?.GetBaseException().Message}");
        yield break;
      }

      // ── 4. 메인 스레드: AudioClip 생성 ────────────────────────────────────
      foreach (var kv in wavBuffer)
      {
        if (_clipCache.ContainsKey(kv.Key)) continue;
        _clipCache[kv.Key] = WavToClip(kv.Key, kv.Value);
      }

      IsReady = true;
      Debug.Log($"[TTSService] 초기화 완료 (캐시 {_clipCache.Count}개)");
    }

    // =========================================================================
    // 공개 API
    // =========================================================================

    /// <summary>
    /// Identifier에 해당하는 AudioClip 목록을 반환합니다.
    /// Dynamic 세그먼트는 overrideVariables → 기본값 순으로 텍스트를 결정하며,
    /// 캐시에 없으면 즉석 합성합니다.
    /// </summary>
    public List<AudioClip> GetClips(
      string identifier,
      Dictionary<string, string> overrideVariables = null)
    {
      if (!_segmentMap.TryGetValue(identifier, out var segments))
        throw new ArgumentException($"[TTSService] 알 수 없는 identifier: {identifier}");

      var transcript = FindTranscript(identifier);
      var clips      = new List<AudioClip>();

      foreach (var seg in segments)
      {
        string text = ResolveSegmentText(seg, transcript, overrideVariables);
        if (text == null)
        {
          if (seg.Type == SegmentType.Dynamic)
            Debug.LogWarning(
              $"[TTSService] identifier '{identifier}' 의 variable '{seg.Text}' 에 값이 없습니다. 해당 세그먼트를 건너뜁니다.");
          continue;
        }

        if (!_clipCache.TryGetValue(text, out var clip))
          clip = SynthesizeClip(text);

        clips.Add(clip);
      }

      return clips;
    }

    /// <summary>Transcript를 AudioSource를 통해 순서대로 재생하는 Coroutine을 시작합니다.</summary>
    public Coroutine PlayTranscript(
      string identifier,
      AudioSource audioSource,
      Dictionary<string, string> overrideVariables = null)
    {
      return StartCoroutine(
        PlaySequentially(GetClips(identifier, overrideVariables), audioSource));
    }

    /// <summary>
    /// 동적 텍스트를 백그라운드에서 미리 합성합니다.
    /// 게임 로딩 중 변수 값이 확정되는 시점에 호출하세요.
    /// </summary>
    public Coroutine PrepareVariable(string text, Action onDone = null)
    {
      return StartCoroutine(PrepareVariableCoroutine(text, onDone));
    }

    /// <summary>
    /// 지정된 identifier의 동적 세그먼트를 variables 값으로 미리 합성하여 캐시에 저장합니다.
    /// 게임 시작 시 또는 변수 값이 확정되는 시점에 호출하세요.
    /// 캐싱이 진행되는 동안 IsDynamicCacheDirty가 true가 됩니다.
    /// </summary>
    public Coroutine PrepareTranscriptVariables(
      string identifier,
      Dictionary<string, string> variables,
      Action onDone = null)
    {
      return StartCoroutine(PrepareTranscriptVariablesCoroutine(identifier, variables, onDone));
    }

    // =========================================================================
    // 내부 메서드
    // =========================================================================

    private IEnumerator PrepareVariableCoroutine(string text, Action onDone)
    {
      if (_clipCache.ContainsKey(text)) { onDone?.Invoke(); yield break; }

      float[] wav  = null;
      var     task = Task.Run(() => wav = _core.Synthesize(text, language, totalStep, speed));
      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsFaulted)
      {
        Debug.LogError(
          $"[TTSService] PrepareVariable 실패 ({text}): {task.Exception?.GetBaseException().Message}");
        yield break;
      }

      _clipCache[text] = WavToClip(text, wav);
      onDone?.Invoke();
    }

    private IEnumerator PrepareTranscriptVariablesCoroutine(
      string identifier,
      Dictionary<string, string> variables,
      Action onDone)
    {
      if (!_segmentMap.TryGetValue(identifier, out var segments))
      {
        Debug.LogWarning($"[TTSService] PrepareTranscriptVariables: 알 수 없는 identifier: {identifier}");
        onDone?.Invoke();
        yield break;
      }

      var transcript   = FindTranscript(identifier);
      var textsToCache = new List<string>();

      foreach (var seg in segments)
      {
        if (seg.Type == SegmentType.Static) continue;

        string text = variables != null && variables.TryGetValue(seg.Text, out var v)
          ? v
          : ResolveDefaultValue(transcript, seg.Text);

        if (text == null)
        {
          Debug.LogWarning(
            $"[TTSService] PrepareTranscriptVariables: identifier '{identifier}' 의 variable '{seg.Text}' 에 값이 없습니다. 해당 세그먼트를 건너뜁니다.");
          continue;
        }

        if (!_clipCache.ContainsKey(text))
          textsToCache.Add(text);
      }

      if (textsToCache.Count == 0)
      {
        onDone?.Invoke();
        yield break;
      }

      // dirty bit 설정: 캐싱 진행 중
      _dynamicCachingCount++;

      var wavBuffer = new Dictionary<string, float[]>();
      var task      = Task.Run(() =>
      {
        foreach (var text in textsToCache)
        {
          if (!wavBuffer.ContainsKey(text))
            wavBuffer[text] = _core.Synthesize(text, language, totalStep, speed);
        }
      });

      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsFaulted)
      {
        Debug.LogError(
          $"[TTSService] PrepareTranscriptVariables 실패 (identifier: {identifier}): {task.Exception?.GetBaseException().Message}");
        _dynamicCachingCount--;
        onDone?.Invoke();
        yield break;
      }

      // 메인 스레드에서 AudioClip 생성
      foreach (var kv in wavBuffer)
      {
        if (!_clipCache.ContainsKey(kv.Key))
          _clipCache[kv.Key] = WavToClip(kv.Key, kv.Value);
      }

      // dirty bit 해제
      _dynamicCachingCount--;
      onDone?.Invoke();
    }

    /// <summary>동기 즉석 합성 — 메인 스레드에서만 호출하세요.</summary>
    private AudioClip SynthesizeClip(string text)
    {
      float[] wav = _core.Synthesize(text, language, totalStep, speed);
      var     clip = WavToClip(text, wav);
      _clipCache[text] = clip;
      return clip;
    }

    private AudioClip WavToClip(string name, float[] samples)
    {
      var clip = AudioClip.Create(name, samples.Length, channels: 1, _core.SampleRate, stream: false);
      clip.SetData(samples, offsetSamples: 0);
      return clip;
    }

    private static string ResolveSegmentText(
      SpeechSegment seg,
      SpeechTranscript transcript,
      Dictionary<string, string> overrideVariables)
    {
      if (seg.Type == SegmentType.Static) return seg.Text;

      if (overrideVariables != null && overrideVariables.TryGetValue(seg.Text, out var ov))
        return ov;

      return ResolveDefaultValue(transcript, seg.Text);
    }

    private static string ResolveDefaultValue(SpeechTranscript transcript, string key)
    {
      if (transcript.Variables != null && transcript.Variables.TryGetValue(key, out var val))
        return val;
      return null;
    }

    private SpeechTranscript FindTranscript(string identifier)
    {
      foreach (var t in _transcripts)
        if (t.Identifier == identifier) return t;
      throw new ArgumentException($"[TTSService] 알 수 없는 identifier: {identifier}");
    }

    private static IEnumerator PlaySequentially(List<AudioClip> clips, AudioSource src)
    {
      foreach (var clip in clips)
      {
        if (clip == null) continue;
        src.clip = clip;
        src.Play();
        yield return new WaitForSeconds(clip.length);
      }
    }
  }
}
