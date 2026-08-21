namespace MultiplayerInfrastructure.Audio
{
  /// <summary>오디오 장치의 방향 구분입니다.</summary>
  public enum AudioDeviceKind
  {
    /// <summary>스피커, 헤드폰 등 소리를 내보내는 장치입니다.</summary>
    Output = 0,

    /// <summary>마이크 등 소리를 받아들이는 장치입니다.</summary>
    Input = 1,
  }
}
