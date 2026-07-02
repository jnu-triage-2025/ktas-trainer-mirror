using System.Collections.Generic;
using UnityEngine;
using TextToSpeechService;

namespace MultiplayerInfrastructure.TTS
{
  /// <summary>
  /// MultiplayerInfrastructure 측 TTS 재생 서비스.
  ///
  /// TextToSpeechService 모듈(<see cref="TextToSpeechService.TTSService"/>)에 의존하여
  /// 시나리오 흐름에서의 음성 재생을 담당한다. TextToSpeechService 모듈은 순수 합성/파일 처리만
  /// 책임지고, 게임 통합(컴포넌트 배치·AudioSource 관리·시나리오 연동)은 이 서비스가 담당한다.
  ///
  /// 배치:
  ///   · 이 컴포넌트를 GameObject에 붙이면 <see cref="TextToSpeechService.TTSService"/> 와
  ///     <see cref="AudioSource"/> 가 자동으로 보장(없으면 추가)된다.
  ///   · <see cref="MultiplayerInfrastructure.Scenario.ScenarioController"/> 의
  ///     _ttsService / _ttsAudioSource 필드에 각각 이 컴포넌트와 <see cref="AudioSource"/> 를 연결한다.
  /// </summary>
  [RequireComponent(typeof(TextToSpeechService.TTSService))]
  [RequireComponent(typeof(AudioSource))]
  public sealed class ScenarioTTSService : MonoBehaviour
  {
    [Tooltip("음성 재생에 사용할 AudioSource. 비어 있으면 같은 오브젝트의 AudioSource를 사용한다.")]
    [SerializeField] private AudioSource _audioSource;

    private TextToSpeechService.TTSService _core;

    /// <summary>재생에 사용하는 AudioSource (외부 연결용). null이면 지연 확보한다.</summary>
    public AudioSource AudioSource
    {
      get
      {
        if (_audioSource == null)
          _audioSource = GetComponent<AudioSource>();
        return _audioSource;
      }
    }

    /// <summary>내부 TTS 엔진 초기화 완료 여부.</summary>
    public bool IsReady => _core != null && _core.IsReady;

    /// <summary>동적 세그먼트 백그라운드 캐싱 진행 중 여부.</summary>
    public bool IsDynamicCacheDirty => _core != null && _core.IsDynamicCacheDirty;

    private void Awake()
    {
      _core = GetComponent<TextToSpeechService.TTSService>();
      if (_audioSource == null)
        _audioSource = GetComponent<AudioSource>();
    }

    /// <summary>
    /// transcripts.json에 등록된 identifier의 음성을 재생한다.
    /// </summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public Coroutine PlayTranscript(
      string identifier,
      AudioSource audioSource = null,
      Dictionary<string, string> overrideVariables = null,
      string voiceIdentifier = null)
    {
      if (_core == null) return null;
      return _core.PlayTranscript(identifier, audioSource ?? _audioSource, overrideVariables, voiceIdentifier);
    }

    /// <summary>
    /// 시나리오 그래프의 인라인 텍스트(Dialogue/Choice/Quiz)를 재생한다.
    /// baked WAV가 있으면 우선 재생하고 없으면 즉석 합성한다.
    /// </summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public Coroutine PlayText(
      string text,
      AudioSource audioSource = null,
      string scenarioIdentifier = null,
      string nodeIdentifier = null,
      string voiceIdentifier = null)
    {
      if (_core == null) return null;
      return _core.PlayText(text, audioSource ?? _audioSource, scenarioIdentifier, nodeIdentifier, voiceIdentifier);
    }

    /// <summary>지정된 identifier의 동적 세그먼트를 미리 합성해 캐시한다.</summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null 또는 빈 문자열이면 기본 목소리를 사용한다.
    /// </param>
    public Coroutine PrepareTranscriptVariables(
      string identifier,
      Dictionary<string, string> variables,
      System.Action onDone = null,
      string voiceIdentifier = null)
    {
      if (_core == null) return null;
      return _core.PrepareTranscriptVariables(identifier, variables, onDone, voiceIdentifier);
    }

#if UNITY_EDITOR
    private void Reset()
    {
      _audioSource = GetComponent<AudioSource>();
    }
#endif
  }
}
