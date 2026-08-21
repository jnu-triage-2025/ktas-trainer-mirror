namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// Windows 오디오 엔드포인트 ID를 장치 인터페이스 경로로 바꿉니다.
  ///
  /// 앱별 기본 장치 지정 API는 <c>IMMDevice::GetId</c>가 주는 날것의 엔드포인트 ID가 아니라
  /// 아래 형태의 장치 인터페이스 경로를 받습니다.
  ///
  ///   \\?\SWD#MMDEVAPI#{엔드포인트 ID}#{장치 인터페이스 GUID}
  ///
  /// 플랫폼 가드 밖에 두어 어느 플랫폼에서든 테스트할 수 있게 했습니다.
  /// </summary>
  public static class AudioEndpointPath
  {
    private const string MMDeviceApiToken = @"\\?\SWD#MMDEVAPI#";

    /// <summary>DEVINTERFACE_AUDIO_RENDER</summary>
    private const string RenderInterfaceSuffix = "#{e6327cad-dcec-4949-ae8a-991e976a79d2}";

    /// <summary>DEVINTERFACE_AUDIO_CAPTURE</summary>
    private const string CaptureInterfaceSuffix = "#{2eef81be-33fa-4800-9670-1cd474972c3f}";

    /// <summary>
    /// 엔드포인트 ID를 장치 인터페이스 경로로 감쌉니다.
    /// 빈 식별자(시스템 설정)면 빈 문자열을 돌려주므로, 부르는 쪽에서 "지정 해제"로 다루면 됩니다.
    /// </summary>
    public static string ForWindowsEndpoint(string endpointId, AudioDeviceKind kind)
    {
      if (string.IsNullOrWhiteSpace(endpointId))
        return string.Empty;

      var suffix = kind == AudioDeviceKind.Output ? RenderInterfaceSuffix : CaptureInterfaceSuffix;
      return $"{MMDeviceApiToken}{endpointId.Trim()}{suffix}";
    }

    /// <summary>장치 인터페이스 경로에서 다시 엔드포인트 ID만 벗겨냅니다.</summary>
    public static string UnwrapWindowsEndpoint(string devicePath)
    {
      if (string.IsNullOrWhiteSpace(devicePath))
        return string.Empty;

      var value = devicePath.Trim();
      if (value.StartsWith(MMDeviceApiToken, System.StringComparison.OrdinalIgnoreCase))
        value = value.Substring(MMDeviceApiToken.Length);

      foreach (var suffix in new[] { RenderInterfaceSuffix, CaptureInterfaceSuffix })
      {
        if (value.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase))
        {
          value = value.Substring(0, value.Length - suffix.Length);
          break;
        }
      }

      return value;
    }
  }
}
