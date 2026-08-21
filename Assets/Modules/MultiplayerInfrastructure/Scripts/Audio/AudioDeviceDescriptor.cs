using System;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 운영체제가 인식하고 있는 오디오 장치 한 개를 나타냅니다.
  ///
  /// <see cref="Id"/>는 설정 저장에 쓰는 식별자입니다. 장치를 뽑았다가 다시 꽂거나 이름을 바꿔도
  /// 되도록 같은 값이 유지되도록, 플랫폼별로 아래 값을 사용합니다.
  ///   - Windows 출력: WASAPI 엔드포인트 ID 문자열
  ///   - macOS 출력: CoreAudio 장치 UID
  ///   - 입력(공통): <see cref="UnityEngine.Microphone"/>이 쓰는 장치 이름
  ///     (Unity에 이름 말고 다른 손잡이가 없어서 이름을 그대로 식별자로 씁니다.)
  ///
  /// 빈 문자열 ID는 "특정 장치를 고르지 않음", 즉 시스템 설정을 따른다는 뜻으로 예약되어 있습니다.
  /// <see cref="AudioDeviceSelectionResolver.SystemDefaultId"/>를 참고하세요.
  /// </summary>
  public sealed class AudioDeviceDescriptor : IEquatable<AudioDeviceDescriptor>
  {
    public AudioDeviceDescriptor(string id, string displayName, bool isSystemDefault = false)
    {
      Id = id ?? string.Empty;
      DisplayName = string.IsNullOrWhiteSpace(displayName) ? Id : displayName;
      IsSystemDefault = isSystemDefault;
    }

    /// <summary>설정 저장에 쓰는 장치 식별자입니다.</summary>
    public string Id { get; }

    /// <summary>설정 화면에 보여줄 장치 이름입니다.</summary>
    public string DisplayName { get; }

    /// <summary>운영체제가 현재 기본 장치로 쓰고 있으면 true입니다.</summary>
    public bool IsSystemDefault { get; }

    public bool Equals(AudioDeviceDescriptor other)
      => other != null && string.Equals(Id, other.Id, StringComparison.Ordinal);

    public override bool Equals(object obj) => Equals(obj as AudioDeviceDescriptor);

    public override int GetHashCode() => Id.GetHashCode();

    public override string ToString()
      => IsSystemDefault ? $"{DisplayName} (기본)" : DisplayName;
  }
}
