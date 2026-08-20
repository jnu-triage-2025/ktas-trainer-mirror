using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 출력 장치 지정을 플랫폼 구현에 넘기고, 필요하면 Unity 오디오 엔진을 다시 엽니다.
  ///
  /// 장치를 지정해도 이미 열려 있는 출력 스트림은 그대로 흘러갑니다. 새 장치로 옮기려면
  /// <see cref="AudioSettings.Reset"/>으로 엔진이 장치를 다시 열게 해야 합니다.
  /// 이때 재생 중이던 소리는 끊기고 <see cref="AudioSettings.OnAudioConfigurationChanged"/>가
  /// 발생하므로, 계속 이어져야 하는 소리가 있다면 그 콜백에서 다시 틀어야 합니다.
  ///
  /// 게임을 켤 때는 다시 열지 않습니다. Windows는 앱별 지정을 레지스트리에 남겨 두었다가
  /// 프로세스가 시작될 때 이미 반영해 주므로, 시작 시점에 엔진을 흔들 이유가 없습니다.
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
    /// 출력을 지정한 장치로 돌립니다.
    /// </summary>
    /// <param name="deviceId">출력 장치 식별자. 빈 문자열이면 지정을 풀고 시스템 설정을 따릅니다.</param>
    /// <param name="restartAudioEngine">
    /// 지금 재생 중인 소리까지 새 장치로 옮길지 여부입니다. 설정 화면에서 사용자가 직접
    /// 바꿨을 때만 true를 넘기세요. 시작 시 저장값을 다시 적용하는 경로에서는 false입니다.
    /// </param>
    /// <param name="failureReason">실패 사유. 성공하거나 지원하지 않는 플랫폼이면 null입니다.</param>
    /// <returns>실제로 경로를 바꿨으면 true.</returns>
    public static bool Apply(string deviceId, bool restartAudioEngine, out string failureReason)
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

      if (restartAudioEngine)
        RestartAudioEngine();

      return true;
    }

    /// <summary>
    /// Unity 오디오 엔진이 출력 장치를 다시 열게 합니다.
    /// 설정을 바꾸지 않고 현재 구성 그대로 다시 여는 것이 목적입니다.
    /// </summary>
    private static void RestartAudioEngine()
    {
      // 에디터에서 플레이 중이 아닐 때는 엔진을 흔들 이유가 없다.
      if (!Application.isPlaying)
        return;

      try
      {
        if (!AudioSettings.Reset(AudioSettings.GetConfiguration()))
          Debug.LogWarning("[AudioDevice] 오디오 엔진을 다시 열지 못했습니다. 게임을 다시 시작하면 반영됩니다.");
      }
      catch (Exception exception)
      {
        Debug.LogWarning($"[AudioDevice] 오디오 엔진 재시작 중 문제가 생겼습니다: {exception.Message}");
      }
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
