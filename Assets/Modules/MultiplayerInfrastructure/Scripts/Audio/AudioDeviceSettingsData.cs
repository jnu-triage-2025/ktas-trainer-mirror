using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 오디오 장치 선택을 담는 직렬화 대상 설정값입니다.
  ///
  /// 그래픽 설정(<c>GraphicsSettingsData</c>)과 같은 규약을 씁니다.
  /// <see cref="JsonUtility"/>로 직렬화해 <see cref="PlayerPrefs"/>에 문자열로 보관합니다.
  ///
  /// 장치 이름 필드는 표시용 캐시입니다. 저장 당시의 이름을 기억해 두었다가,
  /// 다음 실행에서 그 장치가 사라졌을 때 어떤 장치가 없어졌는지 안내에 쓰기 위한 값입니다.
  /// 실제 대조는 언제나 식별자로 합니다.
  /// </summary>
  [Serializable]
  public class AudioDeviceSettingsData
  {
    /// <summary>출력 장치 식별자입니다. 빈 문자열이면 시스템 설정을 따릅니다.</summary>
    public string OutputDeviceId = AudioDeviceSelectionResolver.SystemDefaultId;

    /// <summary>저장 당시의 출력 장치 이름(표시용 캐시)입니다.</summary>
    public string OutputDeviceName = string.Empty;

    /// <summary>입력 장치 식별자입니다. 빈 문자열이면 시스템 설정을 따릅니다.</summary>
    public string InputDeviceId = AudioDeviceSelectionResolver.SystemDefaultId;

    /// <summary>저장 당시의 입력 장치 이름(표시용 캐시)입니다.</summary>
    public string InputDeviceName = string.Empty;

    public string GetDeviceId(AudioDeviceKind kind)
      => kind == AudioDeviceKind.Output ? OutputDeviceId : InputDeviceId;

    public string GetDeviceName(AudioDeviceKind kind)
      => kind == AudioDeviceKind.Output ? OutputDeviceName : InputDeviceName;

    public void SetDevice(AudioDeviceKind kind, string deviceId, string deviceName)
    {
      if (kind == AudioDeviceKind.Output)
      {
        OutputDeviceId = deviceId ?? string.Empty;
        OutputDeviceName = deviceName ?? string.Empty;
      }
      else
      {
        InputDeviceId = deviceId ?? string.Empty;
        InputDeviceName = deviceName ?? string.Empty;
      }
    }

    /// <summary>null 필드를 빈 문자열로 메우고 앞뒤 공백을 걷어냅니다.</summary>
    public void Sanitize()
    {
      OutputDeviceId = Normalize(OutputDeviceId);
      OutputDeviceName = Normalize(OutputDeviceName);
      InputDeviceId = Normalize(InputDeviceId);
      InputDeviceName = Normalize(InputDeviceName);

      // 식별자가 비면 이름 캐시도 의미가 없으므로 같이 지운다.
      if (OutputDeviceId.Length == 0)
        OutputDeviceName = string.Empty;
      if (InputDeviceId.Length == 0)
        InputDeviceName = string.Empty;
    }

    public AudioDeviceSettingsData Clone() => new AudioDeviceSettingsData
    {
      OutputDeviceId = OutputDeviceId,
      OutputDeviceName = OutputDeviceName,
      InputDeviceId = InputDeviceId,
      InputDeviceName = InputDeviceName,
    };

    private static string Normalize(string value)
      => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
  }
}
