using System;

namespace TextToSpeechService
{
  /// <summary>
  /// TTS 재생 단위를 나타내는 세그먼트.
  /// Static: 고정 텍스트 (basetext의 변수가 아닌 부분)
  /// Dynamic: 변수 키 (basetext의 {variable-key} 부분)
  /// </summary>
  public enum SegmentType
  {
    Static,
    Dynamic,
  }

  [Serializable]
  public struct SpeechSegment
  {
    /// <summary>세그먼트 타입 (Static / Dynamic)</summary>
    public SegmentType Type { get; set; }

    /// <summary>
    /// Static: 실제 발화 텍스트
    /// Dynamic: 변수 키 (예: "patient-a")
    /// </summary>
    public string Text { get; set; }
  }
}
