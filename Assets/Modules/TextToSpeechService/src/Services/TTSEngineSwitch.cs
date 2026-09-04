using System;

namespace TextToSpeechService
{
  /// <summary>
  /// TTS 엔진을 쓸지 말지를 프로세스 전역으로 정하는 스위치.
  ///
  /// <see cref="SetDisabled"/> 에 true를 넣으면 <see cref="TTSService"/> 가 ONNX 세션과
  /// 합성해 둔 AudioClip을 메모리에서 내리고, 이후의 합성·재생 요청을 조용히 건너뛴다.
  /// 아직 엔진을 올리지 않은 상태에서 켜져 있으면 모델을 적재하지도 않는다.
  /// 다시 false를 넣으면 초기화를 처음부터 다시 시작한다.
  ///
  /// 이 모듈은 스위치 상태만 본다. 설정 저장이나 UI 연결은 상위 모듈이 담당한다.
  /// </summary>
  public static class TTSEngineSwitch
  {
    /// <summary>true면 TTS 엔진을 적재하지도 사용하지도 않는다.</summary>
    public static bool IsDisabled { get; private set; }

    /// <summary>스위치가 바뀔 때 새 값을 알린다.</summary>
    public static event Action<bool> DisabledChanged;

    /// <summary>스위치를 바꾼다. 값이 그대로면 아무 일도 하지 않는다.</summary>
    public static void SetDisabled(bool disabled)
    {
      if (IsDisabled == disabled)
        return;

      IsDisabled = disabled;
      DisabledChanged?.Invoke(disabled);
    }
  }
}
