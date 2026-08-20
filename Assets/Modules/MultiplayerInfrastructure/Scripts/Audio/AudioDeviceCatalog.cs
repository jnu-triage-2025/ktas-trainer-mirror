using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 지금 이 기기가 인식하고 있는 오디오 장치 목록을 모아 두는 곳입니다.
  ///
  /// 플랫폼별 열거기를 골라 붙이고 결과를 캐시합니다. 장치를 훑는 일은 운영체제 호출이라
  /// 값이 싸지 않으므로, 설정 창을 열거나 새로 고침을 누를 때만 <see cref="Refresh"/>가 돕니다.
  ///
  /// 출력 방향은 Windows(WASAPI)와 macOS(CoreAudio)에서만 훑을 수 있습니다.
  /// 입력 방향은 <see cref="MicrophoneAudioDeviceEnumerator"/>가 모든 플랫폼에서 맡습니다.
  /// </summary>
  public static class AudioDeviceCatalog
  {
    private static IAudioDeviceEnumerator _nativeEnumerator;
    private static IAudioDeviceEnumerator _inputEnumerator;
    private static bool _initialized;

    private static IReadOnlyList<AudioDeviceDescriptor> _outputDevices = Array.Empty<AudioDeviceDescriptor>();
    private static IReadOnlyList<AudioDeviceDescriptor> _inputDevices = Array.Empty<AudioDeviceDescriptor>();

    /// <summary>장치 목록이 새로 읽힐 때마다 발생합니다.</summary>
    public static event Action Refreshed;

    /// <summary>해당 방향의 장치를 훑을 수 있는 플랫폼이면 true입니다.</summary>
    public static bool IsSupported(AudioDeviceKind kind)
    {
      EnsureInitialized();
      return kind == AudioDeviceKind.Input
        ? _inputEnumerator != null
        : _nativeEnumerator != null && _nativeEnumerator.IsSupported(kind);
    }

    /// <summary>마지막으로 읽은 장치 목록입니다. 아직 한 번도 읽지 않았다면 여기서 읽어 옵니다.</summary>
    public static IReadOnlyList<AudioDeviceDescriptor> GetDevices(AudioDeviceKind kind)
    {
      if (!_initialized)
        Refresh();

      return kind == AudioDeviceKind.Output ? _outputDevices : _inputDevices;
    }

    /// <summary>운영체제가 현재 쓰고 있는 기본 장치입니다. 알아내지 못했으면 null입니다.</summary>
    public static AudioDeviceDescriptor GetSystemDefault(AudioDeviceKind kind)
      => AudioDeviceSelectionResolver.FindSystemDefault(GetDevices(kind));

    /// <summary>운영체제에 다시 물어 목록을 갱신합니다.</summary>
    public static void Refresh()
    {
      EnsureInitialized();

      _outputDevices = SafeEnumerate(_nativeEnumerator, AudioDeviceKind.Output);
      _inputDevices = SafeEnumerate(_inputEnumerator, AudioDeviceKind.Input);
      _initialized = true;

      Refreshed?.Invoke();
    }

    private static void EnsureInitialized()
    {
      if (_inputEnumerator != null || _nativeEnumerator != null)
        return;

      _nativeEnumerator = CreateNativeEnumerator();

      // 입력 목록은 Unity가 쥐고 있지만, "기본 마이크"가 무엇인지는 네이티브 쪽만 안다.
      var nativeEnumerator = _nativeEnumerator;
      _inputEnumerator = new MicrophoneAudioDeviceEnumerator(
        nativeEnumerator == null
          ? (Func<string>)null
          : () => AudioDeviceSelectionResolver
              .FindSystemDefault(nativeEnumerator.Enumerate(AudioDeviceKind.Input))?.DisplayName);
    }

    private static IAudioDeviceEnumerator CreateNativeEnumerator()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
      return new Native.WindowsAudioDeviceEnumerator();
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
      return new Native.MacAudioDeviceEnumerator();
#else
      return null;
#endif
    }

    private static IReadOnlyList<AudioDeviceDescriptor> SafeEnumerate(IAudioDeviceEnumerator enumerator, AudioDeviceKind kind)
    {
      if (enumerator == null || !enumerator.IsSupported(kind))
        return Array.Empty<AudioDeviceDescriptor>();

      try
      {
        return enumerator.Enumerate(kind) ?? Array.Empty<AudioDeviceDescriptor>();
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] {kind} 장치 목록을 읽지 못했습니다: {exception.Message}");
        return Array.Empty<AudioDeviceDescriptor>();
      }
    }
  }
}
