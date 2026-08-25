namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 게임 소리가 나갈 출력 장치를 실제로 바꾸는 계층입니다.
  ///
  /// Unity는 출력 경로를 고르는 API를 열어두지 않았고, FMOD도 UnityPlayer에 정적으로 묶여 있어
  /// 손댈 수 없습니다. 그래서 운영체제 쪽에서 우회합니다. 지금은 Windows만 구현되어 있습니다.
  /// </summary>
  public interface IAudioOutputRouter
  {
    /// <summary>이 플랫폼에서 출력 경로를 바꿀 수 있으면 true입니다.</summary>
    public bool IsSupported { get; }

    /// <summary>
    /// 출력을 지정한 장치로 돌립니다. 빈 식별자를 넘기면 지정을 풀고 시스템 설정을 따릅니다.
    /// </summary>
    /// <param name="deviceId">출력 장치 식별자. 빈 문자열이면 지정 해제.</param>
    /// <param name="failureReason">실패했을 때 사람이 읽을 사유. 성공하면 null.</param>
    public bool TryRoute(string deviceId, out string failureReason);
  }
}
