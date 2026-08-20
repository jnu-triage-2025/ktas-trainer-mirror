using System.Collections.Generic;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 운영체제에 물어 오디오 장치 목록을 받아오는 계층입니다.
  ///
  /// Unity에는 출력 장치를 훑는 API가 없어서 플랫폼마다 네이티브 호출로 구현합니다.
  /// (Windows: WASAPI / macOS: CoreAudio) 지원하지 않는 플랫폼에서는
  /// <see cref="IsSupported"/>가 false를 돌려주고, 설정 화면은 시스템 설정 항목만 보여줍니다.
  /// </summary>
  public interface IAudioDeviceEnumerator
  {
    /// <summary>이 구현이 해당 방향의 장치를 훑을 수 있으면 true입니다.</summary>
    bool IsSupported(AudioDeviceKind kind);

    /// <summary>
    /// 지금 쓸 수 있는 장치를 모두 돌려줍니다. 실패하면 빈 목록을 돌려주고 예외를 던지지 않습니다.
    /// 운영체제 기본 장치에는 <see cref="AudioDeviceDescriptor.IsSystemDefault"/>가 서 있습니다.
    /// </summary>
    IReadOnlyList<AudioDeviceDescriptor> Enumerate(AudioDeviceKind kind);
  }
}
