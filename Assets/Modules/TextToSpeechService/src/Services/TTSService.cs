using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
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
    /// 초기화가 실패해 더 이상 진행되지 않는지 여부 (ONNX 모델 누락, Transcript JSON 로드 실패 등).
    /// true면 IsReady가 영원히 참이 되지 않으므로 대기하지 말고 즉시 재생을 포기해야 한다.
    /// </summary>
    public bool IsInitializationFailed { get; private set; }

    /// <summary>
    /// 동적 세그먼트의 백그라운드 캐싱이 진행 중이거나 아직 완료되지 않음을 나타내는 dirty bit.
    /// PrepareTranscriptVariables 호출 시 true가 되고, 캐싱이 완료되면 false가 됩니다.
    /// </summary>
    public bool IsDynamicCacheDirty => _dynamicCachingCount > 0;

    /// <summary>시나리오가 로드한 프로필을 기존 인스펙터 프로필에 병합한다.</summary>
    public void ConfigureScenarioVoiceProfiles(IEnumerable<TTSVoiceProfile> profiles)
    {
      if (profiles == null)
        return;
      var merged = new List<TTSVoiceProfile>(voiceProfiles ?? Array.Empty<TTSVoiceProfile>());
      foreach (var profile in profiles)
      {
        if (profile == null || string.IsNullOrWhiteSpace(profile.VoiceIdentifier))
          continue;
        var index = merged.FindIndex(value => value != null && value.VoiceIdentifier == profile.VoiceIdentifier);
        if (index >= 0)
          merged[index] = profile;
        else
          merged.Add(profile);
      }
      voiceProfiles = merged.ToArray();

      // 시나리오 전환 후 등록되는 프로필도 즉시 사용할 수 있게 코어를 추가한다.
      // 초기화 중이면 InitializeCoroutine이 voiceProfiles를 읽어 동일하게 처리한다.
      if (!IsReady)
        return;
      string streamingAssets = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(streamingAssets);
      foreach (var profile in voiceProfiles)
      {
        if (profile == null || string.IsNullOrWhiteSpace(profile.VoiceIdentifier)
            || _voiceCores.ContainsKey(profile.VoiceIdentifier))
          continue;
        _voiceCores[profile.VoiceIdentifier] = new TTSCore(
          onnxDir, TTSCore.GetVoiceStylePath(streamingAssets, profile.VoiceStyleName ?? voiceStyleName));
      }
    }

    /// <summary>현재 진행 중인 백그라운드 캐싱 작업 수 (dirty bit 카운터)</summary>
    private int _dynamicCachingCount;

    // ONNX 세션은 동시에 추론하지 않는다. 대화 사전 합성이 플레이 중인 TTS와 경쟁하지 않게 한다.
    private readonly object _synthesisLock = new();

    private readonly object _inlineSynthesisQueueLock = new();
    private readonly Dictionary<string, Task<float[]>> _inlineSynthesisTasks = new();
    private readonly List<InlineSynthesisRequest> _inlinePlaybackQueue = new();
    private readonly List<InlineSynthesisRequest> _inlinePrewarmQueue = new();
    private InlineSynthesisRequest _activeInlineSynthesisRequest;
    private bool _inlineSynthesisWorkerRunning;

    private sealed class InlineSynthesisRequest
    {
      public string CacheKey;
      public string Text;
      public TTSCore Core;
      public string Language;
      public int TotalStep;
      public float Speed;
      public CancellationToken CancellationToken;
      public TaskCompletionSource<float[]> Completion;
      public bool PlaybackRequested;
    }

    // =========================================================================
    // Unity 라이프사이클
    // =========================================================================

    /// <summary>
    /// 초기화 코루틴을 시작할 때 켜고 게임 오브젝트가 비활성화될 때 끄는 표시.
    /// 같은 활성화 구간에서 Awake와 OnEnable이 연달아 호출되어도 코루틴이 중복 실행되지 않게 한다.
    /// </summary>
    private bool _initializationRunning;

    private void Awake()
    {
      TTSEngineSwitch.DisabledChanged -= HandleEngineSwitchChanged;
      TTSEngineSwitch.DisabledChanged += HandleEngineSwitchChanged;
      TryStartInitialization();
    }

    /// <summary>
    /// 게임 오브젝트가 다시 활성화될 때 초기화를 이어서 진행한다.
    ///
    /// 이 컴포넌트는 씬에 배치된 NetworkObject 계층에 속하는데, FishNet은 NetworkManager가
    /// 시작되기 전까지 해당 계층을 비활성화한다. Unity는 게임 오브젝트가 비활성화되는 순간
    /// 실행 중이던 코루틴을 정지시키고 재활성화되어도 재개하지 않으므로, Awake에서만 초기화를
    /// 시작하면 초기화가 영구히 완료되지 않는다. 그래서 활성화될 때마다 상태를 확인해 필요하면
    /// 초기화를 다시 시작한다.
    /// </summary>
    private void OnEnable()
    {
      TryStartInitialization();
    }

    private void OnDisable()
    {
      // 비활성화되는 순간 Unity가 초기화 코루틴을 정지시킨다.
      // 다음 OnEnable에서 다시 시작할 수 있도록 진행 중 표시를 해제한다.
      _initializationRunning = false;
    }

    /// <summary>
    /// 아직 초기화가 끝나지도 실패하지도 않았고 진행 중이지도 않다면 초기화 코루틴을 시작한다.
    /// </summary>
    private void TryStartInitialization()
    {
      if (IsReady || IsInitializationFailed || _initializationRunning)
        return;

      // 설정에서 꺼 둔 상태라면 모델을 적재조차 하지 않는다.
      if (TTSEngineSwitch.IsDisabled)
        return;

      // 비활성 상태에서는 코루틴을 시작할 수 없다. 다시 활성화될 때 OnEnable이 이어서 시도한다.
      if (!isActiveAndEnabled)
        return;

      string sa = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(sa);

      if (!TTSCore.AreModelsPresent(onnxDir))
      {
        Debug.LogError(
          $"[TTSService] ONNX 모델 파일이 없습니다: {onnxDir}\n" +
          "TTS > Download Models 메뉴에서 모델을 먼저 다운로드하세요.");
        IsInitializationFailed = true;
        return;
      }

      _initializationRunning = true;
      StartCoroutine(InitializeCoroutine());
    }

    private void OnDestroy()
    {
      TTSEngineSwitch.DisabledChanged -= HandleEngineSwitchChanged;
      CancelQueuedSynthesis();
      ReleaseCoresDeferred();
    }

    // =========================================================================
    // 엔진 스위치 (설정에서의 TTS 비활성화)
    // =========================================================================

    private void HandleEngineSwitchChanged(bool disabled)
    {
      if (disabled)
      {
        Debug.Log("[TTSService] TTS 엔진을 비활성화합니다. ONNX 세션과 음성 캐시를 메모리에서 내립니다.");
        UnloadEngine();
        return;
      }

      Debug.Log("[TTSService] TTS 엔진을 다시 활성화합니다. 초기화를 새로 시작합니다.");
      TryStartInitialization();
    }

    /// <summary>
    /// 적재한 것을 모두 메모리에서 내린다. 실행 중인 코루틴은 강제로 멈추지 않는다.
    /// 각 코루틴이 스위치를 보고 스스로 빠져나가야, 그 코루틴을 기다리던 호출자도 함께 풀린다.
    /// </summary>
    private void UnloadEngine()
    {
      IsReady = false;
      _initializationRunning = false;

      CancelQueuedSynthesis();
      ReleaseCoresDeferred();

      foreach (var clip in _clipCache.Values)
        if (clip != null)
          Destroy(clip);
      _clipCache.Clear();

      _segmentMap.Clear();
      _transcripts = Array.Empty<SpeechTranscript>();

      // 방금 파괴한 AudioClip이 쓰던 메모리를 바로 돌려받는다.
      Resources.UnloadUnusedAssets();
    }

    /// <summary>
    /// 아직 시작하지 않은 합성 요청을 모두 취소한다.
    ///
    /// 취소한 요청은 진행 중 목록에서도 지운다. 남겨 두면 엔진을 다시 켠 뒤의 같은 대사 요청이
    /// 취소된 작업을 그대로 물려받아 결과를 꺼내다가 터진다.
    /// </summary>
    private void CancelQueuedSynthesis()
    {
      lock (_inlineSynthesisQueueLock)
      {
        foreach (var request in _inlinePlaybackQueue)
        {
          request.Completion.TrySetCanceled();
          _inlineSynthesisTasks.Remove(request.CacheKey);
        }
        foreach (var request in _inlinePrewarmQueue)
        {
          request.Completion.TrySetCanceled();
          _inlineSynthesisTasks.Remove(request.CacheKey);
        }
        _inlinePlaybackQueue.Clear();
        _inlinePrewarmQueue.Clear();
      }
    }

    /// <summary>
    /// ONNX 세션을 해제한다. 추론이 진행 중일 때 해제하면 네이티브 쪽에서 터지므로
    /// 합성 락을 잡을 수 있을 때까지 기다렸다가 백그라운드에서 해제한다.
    /// (메인 스레드가 합성이 끝날 때까지 멈춰 서면 화면이 그대로 굳는다.)
    /// </summary>
    private void ReleaseCoresDeferred()
    {
      var cores = new List<TTSCore>();
      if (_core != null)
        cores.Add(_core);
      _core = null;

      foreach (var voiceCore in _voiceCores.Values)
        if (voiceCore != null)
          cores.Add(voiceCore);
      _voiceCores.Clear();

      if (cores.Count == 0)
        return;

      var synthesisLock = _synthesisLock;
      Task.Run(() =>
      {
        lock (synthesisLock)
          foreach (var core in cores)
            core.Dispose();
      });
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

      string sa = Application.streamingAssetsPath;
      string onnxDir = TTSCore.GetOnnxDir(sa);
      string styleAbsPath = TTSCore.GetVoiceStylePath(sa, voiceStyleName);
      string transcriptPath = Path.Combine(sa, transcriptJsonRelPath);

      // ── 1. JSON 파싱 (메인 스레드) ─────────────────────────────────────────
      if (!File.Exists(transcriptPath))
      {
        Debug.LogError($"[TTSService] Transcript JSON을 찾을 수 없습니다: {transcriptPath}");
        IsInitializationFailed = true;
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
        // 적재 도중에 설정에서 엔진을 껐다면, 여기까지 올린 클립도 도로 내린다.
        if (TTSEngineSwitch.IsDisabled)
        {
          UnloadEngine();
          yield break;
        }

        var segments = _segmentMap[transcript.Identifier];
        for (int i = 0; i < segments.Count; i++)
        {
          if (segments[i].Type != SegmentType.Static)
            continue;

          string bakedPath = TTSCore.GetBakedClipPath(
            Application.streamingAssetsPath, transcript.Identifier, i);

          if (!File.Exists(bakedPath))
            continue;

          string url = "file://" + bakedPath;
          using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
          yield return req.SendWebRequest();

          if (req.result == UnityWebRequest.Result.Success)
          {
            var clip = DownloadHandlerAudioClip.GetContent(req);
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
        lock (_synthesisLock)
        {
          // 락을 기다리는 동안 설정에서 엔진을 껐다면 모델을 적재하지 않는다.
          if (TTSEngineSwitch.IsDisabled)
            return;

          // 초기화가 한 번 중단된 뒤 다시 시작되었을 때, 앞선 시도가 남긴 코어를 그대로 쓴다.
          // 캐시에 이미 담긴 AudioClip이 그 코어의 SampleRate로 만들어졌고,
          // ONNX 세션을 중복 적재하면 메모리만 낭비되기 때문이다.
          _core ??= new TTSCore(onnxDir, styleAbsPath);

          // 추가 voice profile 코어 초기화
          if (voiceProfiles != null)
          {
            foreach (var profile in voiceProfiles)
            {
              if (profile == null || string.IsNullOrEmpty(profile.VoiceIdentifier))
                continue;
              if (_voiceCores.ContainsKey(profile.VoiceIdentifier))
                continue;

              string profileStylePath = TTSCore.GetVoiceStylePath(sa, profile.VoiceStyleName ?? voiceStyleName);
              _voiceCores[profile.VoiceIdentifier] = new TTSCore(onnxDir, profileStylePath);
            }
          }

          foreach (var transcript in _transcripts)
          {
            var segments = _segmentMap[transcript.Identifier];
            for (int i = 0; i < segments.Count; i++)
            {
              var seg = segments[i];
              bool baked = seg.Type == SegmentType.Static
                && _clipCache.ContainsKey(MakeCacheKey(null, seg.Text));
              if (baked)
                continue;

              string text = seg.Type == SegmentType.Static
                ? seg.Text
                : ResolveDefaultValue(transcript, seg.Text);

              string bufferKey = MakeCacheKey(null, text);
              if (text != null && !wavBuffer.ContainsKey(bufferKey))
                wavBuffer[bufferKey] = _core.Synthesize(text, language, totalStep, speed);
            }
          }
        }
      });

      yield return new WaitUntil(() => bgTask.IsCompleted);

      if (TTSEngineSwitch.IsDisabled)
      {
        UnloadEngine();
        yield break;
      }

      if (_core == null)
      {
        // 껐다가 바로 다시 켠 경우다. 새로 시작된 초기화가 상태를 잡고 있으므로 이 시도는 물러난다.
        _initializationRunning = false;
        yield break;
      }

      if (bgTask.IsFaulted)
      {
        Debug.LogError($"[TTSService] 초기화 실패: {bgTask.Exception}");
        IsInitializationFailed = true;
        yield break;
      }

      // ── 4. 메인 스레드: AudioClip 생성 ────────────────────────────────────
      // 초기화 중 wavBuffer는 기본 _core로 합성됨 → _core.SampleRate 사용
      int defaultSampleRate = _core.SampleRate;
      foreach (var kv in wavBuffer)
      {
        if (_clipCache.ContainsKey(kv.Key))
          continue;
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
      if (TTSEngineSwitch.IsDisabled)
        return new List<AudioClip>();

      if (!_segmentMap.TryGetValue(identifier, out var segments))
        throw new ArgumentException($"[TTSService] 알 수 없는 identifier: {identifier}");

      var transcript = FindTranscript(identifier);
      var clips = new List<AudioClip>();

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
      if (TTSEngineSwitch.IsDisabled)
        return null;

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
      if (TTSEngineSwitch.IsDisabled)
        return null;

      return StartCoroutine(PlayTextCoroutine(text, audioSource, scenarioIdentifier, nodeIdentifier, voiceIdentifier));
    }

    /// <summary>
    /// 베이크되지 않은 인라인 대사를 재생 전에 캐시한다. 호출자는 여러 요청을 순차 실행하여
    /// 플레이 중 합성 부하를 제한해야 한다.
    /// </summary>
    public Coroutine PrepareInlineText(
      string text,
      string scenarioIdentifier,
      string nodeIdentifier,
      string voiceIdentifier = null,
      CancellationToken cancellationToken = default)
    {
      if (TTSEngineSwitch.IsDisabled)
        return null;

      return StartCoroutine(PrepareInlineTextCoroutine(
        text, scenarioIdentifier, nodeIdentifier, voiceIdentifier, cancellationToken));
    }

    private IEnumerator PrepareInlineTextCoroutine(
      string text,
      string scenarioIdentifier,
      string nodeIdentifier,
      string voiceIdentifier,
      CancellationToken cancellationToken)
    {
      if (TTSEngineSwitch.IsDisabled || string.IsNullOrWhiteSpace(text))
        yield break;

      string cacheKey = MakeCacheKey(voiceIdentifier, text);
      if (_clipCache.ContainsKey(cacheKey))
        yield break;

      if (!string.IsNullOrEmpty(scenarioIdentifier) && !string.IsNullOrEmpty(nodeIdentifier))
      {
        string bakedPath = TTSCore.GetBakedInlineClipPath(
          Application.streamingAssetsPath, scenarioIdentifier, nodeIdentifier, text, voiceIdentifier);
        if (File.Exists(bakedPath))
          yield break;
      }

      if (!IsReady && !IsInitializationFailed)
        yield return new WaitUntil(() => IsReady || IsInitializationFailed || TTSEngineSwitch.IsDisabled);

      if (TTSEngineSwitch.IsDisabled)
        yield break;

      var core = ResolveCore(voiceIdentifier);
      if (core == null)
        yield break;

      var task = RequestInlineSynthesis(text, voiceIdentifier, core, highPriority: false, cancellationToken);
      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsCanceled || cancellationToken.IsCancellationRequested)
      {
        ReleaseInlineSynthesisTask(cacheKey);
        yield break;
      }

      if (task.IsFaulted)
      {
        Debug.LogWarning($"[TTSService] 인라인 TTS 사전 합성 실패 ({text}): {task.Exception?.GetBaseException().Message}");
        yield break;
      }

      CacheInlineClip(cacheKey, text, task.Result, core.SampleRate);
    }

    private IEnumerator PlayTextCoroutine(
      string text, AudioSource audioSource, string scenarioIdentifier, string nodeIdentifier,
      string voiceIdentifier)
    {
      if (TTSEngineSwitch.IsDisabled || string.IsNullOrWhiteSpace(text))
        yield break;

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
          string url = "file://" + bakedPath;
          using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
          yield return req.SendWebRequest();

          if (req.result == UnityWebRequest.Result.Success)
          {
            var clip = DownloadHandlerAudioClip.GetContent(req);
            clip.name = text;
            _clipCache[cacheKey] = clip;
            yield return PlaySequentially(new List<AudioClip> { clip }, audioSource);
            yield break;
          }

          Debug.LogWarning($"[TTSService] baked inline WAV 로드 실패 ({bakedPath}): {req.error}. 즉석 합성으로 대체합니다.");
        }
      }

      // 즉석 합성 (백그라운드).
      // PlayText는 초기화 완료 전에도 호출될 수 있다(baked WAV는 ONNX 초기화 없이 즉시 재생 가능).
      // baked까지 없으면 즉석 합성이 필요하므로 초기화가 끝날 때까지 대기한다.
      // 모델 누락 등으로 초기화 자체가 실패한 경우 영원히 대기하지 않도록 한다.
      if (!IsReady && !IsInitializationFailed)
        yield return new WaitUntil(() => IsReady || IsInitializationFailed || TTSEngineSwitch.IsDisabled);

      if (TTSEngineSwitch.IsDisabled)
        yield break;

      var core = ResolveCore(voiceIdentifier);
      if (core == null)
      {
        Debug.LogWarning($"[TTSService] PlayText: TTSCore 초기화 실패로 재생을 건너뜁니다 ({text})");
        yield break;
      }

      var task = RequestInlineSynthesis(text, voiceIdentifier, core, highPriority: true);
      yield return new WaitUntil(() => task.IsCompleted);

      if (task.IsFaulted)
      {
        Debug.LogError(
          $"[TTSService] PlayText 합성 실패 ({text}): {task.Exception?.GetBaseException().Message}");
        yield break;
      }

      var synthClip = CacheInlineClip(cacheKey, text, task.Result, core.SampleRate);
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
      if (TTSEngineSwitch.IsDisabled)
      { onDone?.Invoke(); yield break; }

      string cacheKey = MakeCacheKey(voiceIdentifier, text);
      if (_clipCache.ContainsKey(cacheKey))
      { onDone?.Invoke(); yield break; }

      var core = ResolveCore(voiceIdentifier);
      if (core == null)
      { onDone?.Invoke(); yield break; }

      var (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
      float[] wav = null;
      var task = Task.Run(() =>
      {
        lock (_synthesisLock)
          wav = core.Synthesize(text, synthLang, synthStep, synthSpeed);
      });
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
      if (TTSEngineSwitch.IsDisabled)
      {
        onDone?.Invoke();
        yield break;
      }

      if (!_segmentMap.TryGetValue(identifier, out var segments))
      {
        Debug.LogWarning($"[TTSService] PrepareTranscriptVariables: 알 수 없는 identifier: {identifier}");
        onDone?.Invoke();
        yield break;
      }

      var transcript = FindTranscript(identifier);
      var textsToCache = new List<string>();

      foreach (var seg in segments)
      {
        if (seg.Type == SegmentType.Static)
          continue;

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
      var task = Task.Run(() =>
      {
        lock (_synthesisLock)
        {
          foreach (var text in textsToCache)
          {
            if (!wavBuffer.ContainsKey(text))
              wavBuffer[text] = core.Synthesize(text, synthLang, synthStep, synthSpeed);
          }
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
      var core = ResolveCore(voiceIdentifier);
      // 엔진이 내려간 직후라면 합성할 코어가 없다. 호출자는 null 클립을 건너뛴다.
      if (core == null)
        return null;

      var (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
      float[] wav;
      lock (_synthesisLock)
        wav = core.Synthesize(text, synthLang, synthStep, synthSpeed);
      var clip = WavToClip(text, wav, core.SampleRate);
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

    private Task<float[]> RequestInlineSynthesis(
      string text,
      string voiceIdentifier,
      TTSCore core,
      bool highPriority,
      CancellationToken cancellationToken = default)
    {
      string cacheKey = MakeCacheKey(voiceIdentifier, text);
      lock (_inlineSynthesisQueueLock)
      {
        if (_inlineSynthesisTasks.TryGetValue(cacheKey, out var existing))
        {
          if (highPriority)
          {
            var queuedRequest = _inlinePrewarmQueue.Find(value => value.CacheKey == cacheKey);
            if (queuedRequest != null)
            {
              _inlinePrewarmQueue.Remove(queuedRequest);
              queuedRequest.PlaybackRequested = true;
              _inlinePlaybackQueue.Add(queuedRequest);
            }
            else
            {
              // 이미 실행 중인 사전 합성도 재생 요청이 기다리고 있음을 표시한다.
              // 시나리오 종료로 원래 취소 토큰이 취소되어도 결과를 재생 경로에 전달한다.
              queuedRequest = _activeInlineSynthesisRequest?.CacheKey == cacheKey
                ? _activeInlineSynthesisRequest
                : _inlinePlaybackQueue.Find(value => value.CacheKey == cacheKey);
              if (queuedRequest != null)
                queuedRequest.PlaybackRequested = true;
            }
          }
          return existing;
        }

        var (synthLang, synthStep, synthSpeed) = ResolveParams(voiceIdentifier);
        var request = new InlineSynthesisRequest
        {
          CacheKey = cacheKey,
          Text = text,
          Core = core,
          Language = synthLang,
          TotalStep = synthStep,
          Speed = synthSpeed,
          CancellationToken = cancellationToken,
          PlaybackRequested = highPriority,
          Completion = new TaskCompletionSource<float[]>()
        };
        _inlineSynthesisTasks[cacheKey] = request.Completion.Task;
        if (highPriority)
          _inlinePlaybackQueue.Add(request);
        else
          _inlinePrewarmQueue.Add(request);

        if (!_inlineSynthesisWorkerRunning)
        {
          _inlineSynthesisWorkerRunning = true;
          // 작업자는 큐가 비면 스스로 종료하므로 취소 토큰을 넘기지 않는다.
          // 토큰이 이미 취소되었거나 작업자가 실행되기 전에 취소되면 Task.Run이 본문을 실행하지
          // 않은 채 종료하는데, 그러면 _inlineSynthesisWorkerRunning이 true로 남아
          // 이후의 모든 합성 요청이 큐에 쌓인 채 처리되지 않는다.
          _ = Task.Run(ProcessInlineSynthesisQueue);
        }
        return request.Completion.Task;
      }
    }

    private void ProcessInlineSynthesisQueue()
    {
      while (true)
      {
        InlineSynthesisRequest request;
        lock (_inlineSynthesisQueueLock)
        {
          if (_inlinePlaybackQueue.Count > 0)
          {
            request = _inlinePlaybackQueue[0];
            _inlinePlaybackQueue.RemoveAt(0);
          }
          else if (_inlinePrewarmQueue.Count > 0)
          {
            request = _inlinePrewarmQueue[0];
            _inlinePrewarmQueue.RemoveAt(0);
          }
          else
          {
            _inlineSynthesisWorkerRunning = false;
            return;
          }
        }

        try
        {
          lock (_inlineSynthesisQueueLock)
            _activeInlineSynthesisRequest = request;
          if (request.CancellationToken.IsCancellationRequested && !request.PlaybackRequested)
          {
            request.Completion.TrySetCanceled(request.CancellationToken);
            continue;
          }

          float[] wav;
          lock (_synthesisLock)
            wav = request.Core.Synthesize(request.Text, request.Language, request.TotalStep, request.Speed);
          request.Completion.TrySetResult(wav);
        }
        catch (Exception exception)
        {
          request.Completion.TrySetException(exception);
        }
        finally
        {
          // 성공한 작업은 메인 스레드가 AudioClip으로 전환할 때까지 유지한다.
          // 그 사이 재생 요청이 들어와도 동일 PCM 결과를 공유해 중복 합성을 막는다.
          if (!request.Completion.Task.IsCompletedSuccessfully)
          {
            lock (_inlineSynthesisQueueLock)
              _inlineSynthesisTasks.Remove(request.CacheKey);
          }
          lock (_inlineSynthesisQueueLock)
          {
            if (_activeInlineSynthesisRequest == request)
              _activeInlineSynthesisRequest = null;
          }
        }
      }
    }

    private AudioClip CacheInlineClip(string cacheKey, string text, float[] wav, int sampleRate)
    {
      if (_clipCache.TryGetValue(cacheKey, out var cached))
      {
        ReleaseInlineSynthesisTask(cacheKey);
        return cached;
      }
      var clip = WavToClip(text, wav, sampleRate);
      _clipCache[cacheKey] = clip;
      ReleaseInlineSynthesisTask(cacheKey);
      return clip;
    }

    private void ReleaseInlineSynthesisTask(string cacheKey)
    {
      lock (_inlineSynthesisQueueLock)
        _inlineSynthesisTasks.Remove(cacheKey);
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
          if (p == null || p.VoiceIdentifier != voiceIdentifier)
            continue;
          string lang = string.IsNullOrEmpty(p.Language) ? language : p.Language;
          int step = p.TotalStep > 0 ? p.TotalStep : totalStep;
          float spd = p.Speed > 0f ? p.Speed : speed;
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
      if (seg.Type == SegmentType.Static)
        return seg.Text;

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
        if (t.Identifier == identifier)
          return t;
      throw new ArgumentException($"[TTSService] 알 수 없는 identifier: {identifier}");
    }

    private static IEnumerator PlaySequentially(List<AudioClip> clips, AudioSource src)
    {
      foreach (var clip in clips)
      {
        if (clip == null)
          continue;
        if (TTSEngineSwitch.IsDisabled)
          yield break;
        src.clip = clip;
        src.Play();
        yield return new WaitForSeconds(clip.length);
      }
    }
  }
}
