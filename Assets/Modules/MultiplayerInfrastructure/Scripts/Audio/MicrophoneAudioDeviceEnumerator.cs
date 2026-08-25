using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 입력 장치를 <see cref="Microphone.devices"/>로 훑는 구현입니다.
  ///
  /// 입력만큼은 네이티브가 아니라 Unity 목록을 기준으로 삼습니다. 녹음을 시작할 때
  /// <see cref="Microphone.Start"/>에 넘길 수 있는 값이 이 이름뿐이라, 여기서 얻은 이름을
  /// 그대로 식별자로 써야 설정값과 실제 녹음 장치가 어긋나지 않습니다.
  ///
  /// 기본 장치 판별은 네이티브 열거기가 알려준 기본 입력 장치 이름과 맞춰봅니다.
  /// 맞는 항목이 없으면 Unity가 첫 번째로 돌려준 장치를 기본으로 봅니다.
  /// (<see cref="Microphone.Start"/>에 null을 넘겼을 때 잡히는 장치와 같습니다.)
  /// </summary>
  public sealed class MicrophoneAudioDeviceEnumerator : IAudioDeviceEnumerator
  {
    private readonly Func<string> _systemDefaultNameProvider;

    public MicrophoneAudioDeviceEnumerator(Func<string> systemDefaultNameProvider = null)
    {
      _systemDefaultNameProvider = systemDefaultNameProvider;
    }

    public bool IsSupported(AudioDeviceKind kind) => kind == AudioDeviceKind.Input;

    public IReadOnlyList<AudioDeviceDescriptor> Enumerate(AudioDeviceKind kind)
    {
      if (kind != AudioDeviceKind.Input)
        return Array.Empty<AudioDeviceDescriptor>();

      string[] names;
      try
      {
        names = Microphone.devices;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] 마이크 목록을 읽지 못했습니다: {exception.Message}");
        return Array.Empty<AudioDeviceDescriptor>();
      }

      if (names == null || names.Length == 0)
        return Array.Empty<AudioDeviceDescriptor>();

      int defaultIndex = ResolveDefaultIndex(names);

      var devices = new List<AudioDeviceDescriptor>(names.Length);
      for (int i = 0; i < names.Length; i++)
      {
        if (string.IsNullOrWhiteSpace(names[i]))
          continue;
        devices.Add(new AudioDeviceDescriptor(names[i], names[i], i == defaultIndex));
      }

      return devices;
    }

    private int ResolveDefaultIndex(string[] names)
    {
      var nativeDefault = SafeGetSystemDefaultName();
      if (string.IsNullOrWhiteSpace(nativeDefault))
        return 0;

      for (int i = 0; i < names.Length; i++)
      {
        if (string.Equals(names[i], nativeDefault, StringComparison.OrdinalIgnoreCase))
          return i;
      }

      // Windows는 Unity 쪽 이름이 "마이크(장치)"처럼 엔드포인트 이름을 감싸는 경우가 있어
      // 정확히 같지 않을 때 부분 일치까지 본다.
      for (int i = 0; i < names.Length; i++)
      {
        if (names[i] != null &&
            (names[i].IndexOf(nativeDefault, StringComparison.OrdinalIgnoreCase) >= 0 ||
             nativeDefault.IndexOf(names[i], StringComparison.OrdinalIgnoreCase) >= 0))
          return i;
      }

      return 0;
    }

    private string SafeGetSystemDefaultName()
    {
      if (_systemDefaultNameProvider == null)
        return null;

      try
      {
        return _systemDefaultNameProvider();
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] 기본 마이크 이름을 확인하지 못했습니다: {exception.Message}");
        return null;
      }
    }
  }
}
