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
  ///   · 다중 목소리 프로파일(<see cref="TTSVoiceProfile"/>) 지원 — 노드별로 목소리를 교체할 수 있다.
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

    [Tooltip("기본 음성 스타일 파일명 (TTS/Models/voice_styles/ 하위, 확장자 무관). Voice Profiles에 포함되지 않은 요청의 폴백으로 사용된다.")]
    [SerializeField] private string voiceStyleName = "F1";

    [Header("TTS 파라미터")]
    [Tooltip("TTS 언어 코드 (en, ko, es, pt, fr)")]
    [SerializeField] private string language = "ko";

    [Tooltip("Diffusion 스텝 수 — 높을수록 음질 향상, 속도 저하")]
    [SerializeField] private int totalStep = 5;

    [Tooltip("발화 속도 배율")]
    [SerializeField] private float speed = 1.05f;

    [Header("다중 목소리 프로파일")]
    [Tooltip(
      "등장인물별 목소리 프로파일 목록. 각 프로파일에 고유한 VoiceIdentifier를 설정하고 " +
      "시나리오 노드의 TtsVoiceIdentifier 필드에서 참조한다. " +
      "비어 있거나 식별자가 일치하지 않으면 위의 기본 설정(voiceStyleName 등)을 사용한다.")]
    [SerializeField] private TTSVoiceProfile[] voiceProfiles = Array.Empty<TTSVoiceProfile>();

    // =========================================================================
    // 내부 상태
    // =========================================================================

    /// <summary>기본(폴백) TTSCore 인스턴스</summary>
    private TTSCore _core;

    /// <summary>voiceIdentifier → TTSCore 인스턴스 (프로파일별 독립 스타일)</summary>
    private readonly Dictionary<string, TTSCore> _voiceCores = new();

    /// <summary>텍스트 → AudioClip 캐시 (Static baked + 런타임 합성 결과)
    /// 캐시 키: "{voiceIdentifier}\0{text}" — voiceIdentifier가 null/빈 문자열이면 "\0{text}"</summary>
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

    /// <summary>시나리오가 로드한 프로필을 기존 인스펙터 프로필에 병합한다.</summary>
    public void ConfigureScenarioVoiceProfiles(IEnumerable<TTSVoiceProfile> profiles)
    {
      if (profiles == null) return;
      var merged = new List<TTSVoiceProfile>(voiceProfiles ?? Array.Empty<TTSVoiceProfile>());
      foreach (var profile in profiles)
      {
        if (profile == null || string.IsNullOrWhiteSpace(profile.VoiceIdentifier)) continue;
        var index = merged.FindIndex(value => value != null && value.VoiceIdentifier == profile.VoiceIdentifier);
        if (index >= 0) merged[index] = profile;
        else merged.Add(profile);
      }
      voiceProfiles = merged.ToArray();

      // 시나리오 전환 후 등록되는 프로필도 즉시 사용할 수 있게 코어를 추가한다.
      // 초기화 중이면 InitializeCoroutine이 voiceProfiles를 읽어 동일하게 처리한다.
      if (!IsReady) return;
      string streamingAssets = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(streamingAssets);
      foreach (var profile in voiceProfiles)
      {
        if (profile == null || string.IsNullOrWhiteSpace(profile.VoiceIdentifier)
            || _voiceCores.ContainsKey(profile.VoiceIdentifier)) continue;
        _voiceCores[profile.VoiceIdentifier] = new TTSCore(
          onnxDir, TTSCore.GetVoiceStylePath(streamingAssets, profile.VoiceStyleName ?? voiceStyleName));
      }
    }

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
      foreach (var vc in _voiceCores.Values)
        vc?.Dispose();
      _voiceCores.Clear();
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
            // baked Static 세그먼트는 기본(null) voiceIdentifier로 캐시한다.
            _clipCache[MakeCacheKey(null, segments[i].Text)] = clip;
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

        // 추가 voice profile 코어 초기화
        if (voiceProfiles != null)
        {
          foreach (var profile in voiceProfiles)
          {
            if (profile == null || string.IsNullOrEmpty(profile.VoiceIdentifier)) continue;
            if (_voiceCores.ContainsKey(profile.VoiceIdentifier)) continue;

            string profileStylePath = TTSCore.GetVoiceStylePath(sa, profile.VoiceStyleName ?? voiceStyleName);
            _voiceCores[profile.VoiceIdentifier] = new TTSCore(onnxDir, profileStylePath);
          }
        }

        foreach (var transcript in _transcripts)
        {
          var segments = _segmentMap[transcript.Identifier];
          for (int i = 0; i < segments.Count; i++)
          {
            var seg    = segments[i];
            bool baked = seg.Type == SegmentType.Static
              && _clipCache.ContainsKey(MakeCacheKey(null, seg.Text));
            if (baked) continue;

            string text = seg.Type == SegmentType.Static
              ? seg.Text
              : ResolveDefaultValue(transcript, seg.Text);

            string bufferKey = MakeCacheKey(null, text);
            if (text != null && !wavBuffer.ContainsKey(bufferKey))
              wavBuffer[bufferKey] = _core.Synthesize(text, language, totalStep, speed);
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
      // 초기화 중 wavBuffer는 기본 _core로 합성됨 → _core.SampleRate 사용
      int defaultSampleRate = _core.SampleRate;
      foreach (var kv in wavBuffer)
      {
        if (_clipCache.ContainsKey(kv.Key)) continue;
        // 캐시 키에서 텍스트 이름 부분만 추출해 AudioClip 이름으로 사용
        _clipCache[kv.Key] = WavToClip(ExtractTextFromCacheKey(kv.Key), kv.Value, defaultSampleRate);
      }

      IsReady = true;
      Debug.Log($"[TTSService] 초기화 완료 (캐시 {_clipCache.Count}개, 프로파일 {_voiceCores.Count}개)");
    }

    // =========================================================================
    // 공개 API
    // =========================================================================

    /// <summary>
    /// 등록된 voice profile 식별자 목록을 반환합니다(기본 목소리 제외).
    /// 에디터 도구나 런타임 진단에서 사용할 수 있습니다.
    /// </summary>
    public IReadOnlyCollection<string> GetVoiceIdentifiers() => _voiceCores.Keys;

    /// <summary>
    /// Identifier에 해당하는 AudioClip 목록을 반환합니다.
    /// Dynamic 세그먼트는 overrideVariables → 기본값 순으로 텍스트를 결정하며,
    /// 캐시에 없으면 즉석 합성합니다.
    /// </summary>
    /// <param name="identifier">Transcript 식별자</param>
    /// <param name="overrideVariables">동적 세그먼트 변수 오버라이드</param>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public List<AudioClip> GetClips(
      string identifier,
      Dictionary<string, string> overrideVariables = null,
      string voiceIdentifier = null)
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

        string cacheKey = MakeCacheKey(voiceIdentifier, text);
        if (!_clipCache.TryGetValue(cacheKey, out var clip))
          clip = SynthesizeClip(text, voiceIdentifier);

        clips.Add(clip);
      }

      return clips;
    }

    /// <summary>Transcript를 AudioSource를 통해 순서대로 재생하는 Coroutine을 시작합니다.</summary>
    /// <param name="identifier">Transcript 식별자</param>
    /// <param name="audioSource">재생에 사용할 AudioSource</param>
    /// <param name="overrideVariables">동적 세그먼트 변수 오버라이드</param>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public Coroutine PlayTranscript(
      string identifier,
      AudioSource audioSource,
      Dictionary<string, string> overrideVariables = null,
      string voiceIdentifier = null)
    {
      return StartCoroutine(
        PlaySequentially(GetClips(identifier, overrideVariables, voiceIdentifier), audioSource));
    }

    /// <summary>
    /// transcripts.json에 등록되지 않은 임의의 인라인 텍스트(시나리오 그래프의
    /// Dialogue/Choice/Quiz 콘텐츠 등)를 TTS로 재생합니다.
    ///
    /// 우선순위:
    ///   1) (scenarioIdentifier, nodeIdentifier, text, voiceIdentifier) 에 대한 사전 합성(baked) WAV
    ///   2) 런타임 즉석 합성 (baked가 없거나 로드 실패 시)
    ///
    /// scenarioIdentifier/nodeIdentifier 가 주어지면 baked WAV 조회에 사용된다.
    /// 둘 중 하나라도 없으면(즉석 재생 목적) 캐시/합성만 수행한다.
    /// </summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public Coroutine PlayText(
      string text,
      AudioSource audioSource,
      string scenarioIdentifier = null,
      string nodeIdentifier = null,
      string voiceIdentifier = null)
    {
      return StartCoroutine(PlayTextCoroutine(text, audioSource, scenarioIdentifier, nodeIdentifier, voiceIdentifier));
    }

    private IEnumerator PlayTextCoroutine(
      string text, AudioSource audioSource, string scenarioIdentifier, string nodeIdentifier,
      string voiceIdentifier)
    {
      if (string.IsNullOrWhiteSpace(text)) yield break;

      string cacheKey = MakeCacheKey(voiceIdentifier, text);

      // 이미 캐시에 있으면 바로 재생
      if (_clipCache.TryGetValue(cacheKey, out var cached))
      {
        yield return PlaySequentially(new List<AudioClip> { cached }, audioSource);
        yield break;
      }

      // baked WAV 조회 (scenario/node 정보가 있을 때만)
      if (!string.IsNullOrEmpty(scenarioIdentifier) && !string.IsNullOrEmpty(nodeIdentifier))
      {
        string bakedPath = TTSCore.GetBakedInlineClipPath(
          Application.streamingAssetsPath, scenarioIdentifier, nodeIdentifier, text, voiceIdentifier);

        if (File.Exists(bakedPath))
        {
          string url    = "file://" + bakedPath;
          using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
          yield return req.SendWebRequest();

          if (req.result == UnityWebRequest.Result.Success)
          {
            var clip  = DownloadHandlerAudioClip.GetContent(req);
            clip.name = text;
            _clipCache[cacheKey] = clip;
            yield return PlaySequentially(new List<AudioClip> { clip }, audioSource);
            yield break;
          }

          Debug.LogWarning($"[TTSService] baked inline WAV 로드 실패 ({bakedPath}): {req.error}. 즉석 합성으로 대체합니다.");
        }
      }

      // 즉석 합성 (백그라운드)
      var core = ResolveCore(voiceIdentifier);
      if (core == null)
      {
        Debug.LogWarning("[TTSService] PlayText: TTSCore가 초기화되지 않았습니다.");
        yield break;
      }

      var (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
      float[] wav  = null;
      var     task = Task.Run(() => wav = core.Synthesize(text, synthLang, synthStep, synthSpeed));
      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsFaulted)
      {
        Debug.LogError(
          $"[TTSService] PlayText 합성 실패 ({text}): {task.Exception?.GetBaseException().Message}");
        yield break;
      }

      var synthClip = WavToClip(text, wav, core.SampleRate);
      _clipCache[cacheKey] = synthClip;
      yield return PlaySequentially(new List<AudioClip> { synthClip }, audioSource);
    }

    /// <summary>
    /// 동적 텍스트를 백그라운드에서 미리 합성합니다.
    /// 게임 로딩 중 변수 값이 확정되는 시점에 호출하세요.
    /// </summary>
    public Coroutine PrepareVariable(string text, Action onDone = null, string voiceIdentifier = null)
    {
      return StartCoroutine(PrepareVariableCoroutine(text, onDone, voiceIdentifier));
    }

    /// <summary>
    /// 지정된 identifier의 동적 세그먼트를 variables 값으로 미리 합성하여 캐시에 저장합니다.
    /// 게임 시작 시 또는 변수 값이 확정되는 시점에 호출하세요.
    /// 캐싱이 진행되는 동안 IsDynamicCacheDirty가 true가 됩니다.
    /// </summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public Coroutine PrepareTranscriptVariables(
      string identifier,
      Dictionary<string, string> variables,
      Action onDone = null,
      string voiceIdentifier = null)
    {
      return StartCoroutine(PrepareTranscriptVariablesCoroutine(identifier, variables, onDone, voiceIdentifier));
    }

    // =========================================================================
    // 내부 메서드
    // =========================================================================

    private IEnumerator PrepareVariableCoroutine(string text, Action onDone, string voiceIdentifier)
    {
      string cacheKey = MakeCacheKey(voiceIdentifier, text);
      if (_clipCache.ContainsKey(cacheKey)) { onDone?.Invoke(); yield break; }

      var core = ResolveCore(voiceIdentifier);
      if (core == null) { onDone?.Invoke(); yield break; }

      var (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
      float[] wav  = null;
      var     task = Task.Run(() => wav = core.Synthesize(text, synthLang, synthStep, synthSpeed));
      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsFaulted)
      {
        Debug.LogError(
          $"[TTSService] PrepareVariable 실패 ({text}): {task.Exception?.GetBaseException().Message}");
        yield break;
      }

      _clipCache[cacheKey] = WavToClip(text, wav, core.SampleRate);
      onDone?.Invoke();
    }

    private IEnumerator PrepareTranscriptVariablesCoroutine(
      string identifier,
      Dictionary<string, string> variables,
      Action onDone,
      string voiceIdentifier)
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

        string cacheKey = MakeCacheKey(voiceIdentifier, text);
        if (!_clipCache.ContainsKey(cacheKey))
          textsToCache.Add(text);
      }

      if (textsToCache.Count == 0)
      {
        onDone?.Invoke();
        yield break;
      }

      // dirty bit 설정: 캐싱 진행 중
      _dynamicCachingCount++;

      var core = ResolveCore(voiceIdentifier);
      if (core == null)
      {
        _dynamicCachingCount--;
        onDone?.Invoke();
        yield break;
      }

      var (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
      var wavBuffer = new Dictionary<string, float[]>();
      var task      = Task.Run(() =>
      {
        foreach (var text in textsToCache)
        {
          if (!wavBuffer.ContainsKey(text))
            wavBuffer[text] = core.Synthesize(text, synthLang, synthStep, synthSpeed);
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
      int coreSampleRate = core.SampleRate;
      foreach (var kv in wavBuffer)
      {
        string ck = MakeCacheKey(voiceIdentifier, kv.Key);
        if (!_clipCache.ContainsKey(ck))
          _clipCache[ck] = WavToClip(kv.Key, kv.Value, coreSampleRate);
      }

      // dirty bit 해제
      _dynamicCachingCount--;
      onDone?.Invoke();
    }

    /// <summary>동기 즉석 합성 — 메인 스레드에서만 호출하세요.</summary>
    private AudioClip SynthesizeClip(string text, string voiceIdentifier = null)
    {
      var   core = ResolveCore(voiceIdentifier);
      var   (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
      float[] wav = core.Synthesize(text, synthLang, synthStep, synthSpeed);
      var     clip = WavToClip(text, wav, core.SampleRate);
      string cacheKey = MakeCacheKey(voiceIdentifier, text);
      _clipCache[cacheKey] = clip;
      return clip;
    }

    /// <summary>
    /// PCM 샘플 배열에서 AudioClip을 생성한다.
    /// sampleRate는 반드시 합성에 사용한 TTSCore의 SampleRate를 전달해야 한다.
    /// </summary>
    private static AudioClip WavToClip(string name, float[] samples, int sampleRate)
    {
      var clip = AudioClip.Create(name, samples.Length, channels: 1, sampleRate, stream: false);
      clip.SetData(samples, offsetSamples: 0);
      return clip;
    }

    // =========================================================================
    // Voice Profile 헬퍼
    // =========================================================================

    /// <summary>voiceIdentifier에 맞는 TTSCore 인스턴스를 반환한다. 없으면 기본(_core)을 반환.</summary>
    private TTSCore ResolveCore(string voiceIdentifier)
    {
      if (!string.IsNullOrEmpty(voiceIdentifier)
          && _voiceCores.TryGetValue(voiceIdentifier, out var vc))
        return vc;
      return _core;
    }

    /// <summary>voiceIdentifier에 맞는 합성 파라미터(언어, 스텝, 속도)를 반환한다.</summary>
    private (string lang, int step, float spd) ResolveParams(string voiceIdentifier)
    {
      if (!string.IsNullOrEmpty(voiceIdentifier) && voiceProfiles != null)
      {
        foreach (var p in voiceProfiles)
        {
          if (p == null || p.VoiceIdentifier != voiceIdentifier) continue;
          string lang = string.IsNullOrEmpty(p.Language) ? language : p.Language;
          int    step = p.TotalStep > 0 ? p.TotalStep : totalStep;
          float  spd  = p.Speed > 0f    ? p.Speed     : speed;
          return (lang, step, spd);
        }
      }
      return (language, totalStep, speed);
    }

    /// <summary>캐시 딕셔너리 키를 생성한다. voiceIdentifier가 없으면 "\0text" 형식.</summary>
    private static string MakeCacheKey(string voiceIdentifier, string text)
    {
      if (string.IsNullOrEmpty(voiceIdentifier))
        return "\0" + text;
      return voiceIdentifier + "\0" + text;
    }

    /// <summary>캐시 키에서 텍스트 부분을 추출한다.</summary>
    private static string ExtractTextFromCacheKey(string cacheKey)
    {
      int idx = cacheKey.IndexOf('\0');
      return idx >= 0 ? cacheKey.Substring(idx + 1) : cacheKey;
    }

    // =========================================================================
    // 공통 유틸리티
    // =========================================================================

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
