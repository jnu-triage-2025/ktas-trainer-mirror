using System;
using UnityEngine;

namespace TextToSpeechService
{
  /// <summary>
  /// 하나의 TTS 음성 캐릭터(목소리)를 정의하는 프로파일.
  ///
  /// <para>
  /// <see cref="TTSService"/> 인스펙터의 <c>Voice Profiles</c> 목록에 여러 프로파일을 등록해
  /// 각각에 고유한 <see cref="VoiceIdentifier"/>를 부여할 수 있다. 시나리오 그래프 노드의
  /// <c>TtsVoiceIdentifier</c> 필드에 이 식별자를 지정하면 해당 목소리로 재생된다.
  /// </para>
  ///
  /// <para>
  /// <see cref="VoiceIdentifier"/>가 null/빈 문자열이거나 노드에서 지정하지 않으면
  /// <see cref="TTSService"/>의 기존 단일 기본 목소리로 폴백된다.
  /// </para>
  /// </summary>
  [Serializable]
  public sealed class TTSVoiceProfile
  {
    [Tooltip("시나리오 노드에서 참조할 고유 식별자. 예: \"narrator\", \"doctor\", \"patient\"")]
    public string VoiceIdentifier;

    [Tooltip("음성 스타일 파일명 (TTS/Models/voice_styles/ 하위, 확장자 무관). 예: \"F1\", \"M2\"")]
    public string VoiceStyleName = "F1";

    [Tooltip("TTS 언어 코드 (en, ko, es, pt, fr). 비어 있으면 TTSService 기본값 사용.")]
    public string Language;

    [Tooltip("발화 속도 배율. 0 이하이면 TTSService 기본값 사용.")]
    public float Speed;

    [Tooltip("Diffusion 스텝 수. 0 이하이면 TTSService 기본값 사용.")]
    public int TotalStep;
  }
}
