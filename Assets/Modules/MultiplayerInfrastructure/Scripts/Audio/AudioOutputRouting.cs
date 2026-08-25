using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 출력 장치 지정을 플랫폼 구현에 넘기고, 오디오 엔진을 다시 여는 수단을 제공합니다.
  ///
  /// 두 가지 일을 일부러 갈라 두었습니다.
  ///   - <see cref="Apply"/>는 운영체제에 "앞으로는 이 장치로" 라고 알리기만 합니다. 부작용이 없습니다.
  ///   - <see cref="RestartAudioEngine"/>은 이미 열려 있는 스트림을 새 장치로 옮깁니다. 이쪽이 파괴적입니다.
  ///
  /// <see cref="AudioSettings.Reset"/>은 재생 중인 소리를 모두 끊고, <c>Microphone.Start</c>로
  /// 만든 <see cref="AudioClip"/>까지 무효로 만듭니다. 그래서 언제 부를지는 부르는 쪽이 정합니다.
  /// 게임을 켤 때는 부르지 않습니다. Windows는 앱별 지정을 레지스트리에 남겨 두었다가 프로세스가
  /// 시작될 때 이미 반영해 주므로, 시작 시점에 엔진을 흔들 이유가 없습니다.
  /// </summary>
  public static class AudioOutputRouting
  {
    private static IAudioOutputRouter _router;
    private static bool _routerResolved;

    /// <summary>이 플랫폼에서 출력 경로를 실제로 바꿀 수 있으면 true입니다.</summary>
    public static bool IsSupported
    {
      get
      {
        var router = ResolveRouter();
        return router != null && router.IsSupported;
      }
    }

    /// <summary>
    /// 앞으로 열릴 출력 스트림이 나갈 장치를 지정합니다. 이미 흐르고 있는 소리는 건드리지 않으므로,
    /// 지금 재생 중인 것까지 옮기려면 <see cref="RestartAudioEngine"/>을 따로 부르세요.
    /// </summary>
    /// <param name="deviceId">출력 장치 식별자. 빈 문자열이면 지정을 풀고 시스템 설정을 따릅니다.</param>
    /// <param name="failureReason">실패 사유. 성공하거나 지원하지 않는 플랫폼이면 null입니다.</param>
    /// <returns>실제로 경로를 바꿨으면 true.</returns>
    public static bool Apply(string deviceId, out string failureReason)
    {
      failureReason = null;

      var router = ResolveRouter();
      if (router == null || !router.IsSupported)
        return false;

      if (!router.TryRoute(deviceId ?? string.Empty, out failureReason))
      {
        Debug.LogWarning($"[AudioDevice] 출력 장치를 바꾸지 못했습니다. 시스템 설정을 그대로 따릅니다. {failureReason}");
        return false;
      }

      return true;
    }

    /// <summary>
    /// Unity 오디오 엔진이 출력 장치를 다시 열게 합니다.
    /// 설정을 바꾸지 않고 현재 구성 그대로 다시 여는 것이 목적입니다.
    ///
    /// <b>재생 중인 소리가 모두 끊기고 마이크 클립도 무효가 됩니다.</b>
    /// 소리가 잦아든 뒤에 부르세요.
    /// </summary>
    public static bool RestartAudioEngine()
    {
      // 에디터에서 플레이 중이 아닐 때는 엔진을 흔들 이유가 없다.
      if (!Application.isPlaying)
        return false;

      try
      {
        if (AudioSettings.Reset(AudioSettings.GetConfiguration()))
          return true;

        Debug.LogWarning("[AudioDevice] 오디오 엔진을 다시 열지 못했습니다. 게임을 다시 시작하면 반영됩니다.");
        return false;
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] 오디오 엔진 재시작 중 문제가 생겼습니다: {exception.Message}");
        return false;
      }
    }

    /// <summary>
    /// 지금 소리를 내고 있는 <see cref="AudioSource"/>가 하나라도 있으면 true입니다.
    /// 엔진을 다시 열어도 되는 순간인지 가늠하는 데 씁니다.
    /// </summary>
    public static bool IsAnyAudioPlaying()
    {
      var sources = UnityEngine.Object.FindObjectsByType<AudioSource>(
        FindObjectsInactive.Exclude, FindObjectsSortMode.None);

      for (int i = 0; i < sources.Length; i++)
      {
        if (sources[i] != null && sources[i].isPlaying)
          return true;
      }

      return false;
    }

    private static IAudioOutputRouter ResolveRouter()
    {
      if (_routerResolved)
        return _router;

      _routerResolved = true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
      _router = new Native.WindowsAudioOutputRouter();
#else
      _router = null;
#endif
      return _router;
    }
  }
}
