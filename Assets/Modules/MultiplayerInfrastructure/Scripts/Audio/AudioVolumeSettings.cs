using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>게임 전체, 효과음, 배경음악의 음량을 한 곳에서 관리합니다.</summary>
  public static class AudioVolumeSettings
  {
    private const string MasterKey = "audio.masterVolume";
    private const string SfxKey = "audio.sfxVolume";
    private const string BgmKey = "audio.bgmVolume";

    public static event Action Changed;

    public static float MasterVolume { get; private set; } = 1f;
    public static float SfxVolume { get; private set; } = 1f;
    public static float BgmVolume { get; private set; } = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
      MasterVolume = Load(MasterKey);
      SfxVolume = Load(SfxKey);
      BgmVolume = Load(BgmKey);
      ApplyMasterVolume();
    }

    public static void SetMasterVolume(float value) => Set(MasterKey, value, v => MasterVolume = v, true);
    public static void SetSfxVolume(float value) => Set(SfxKey, value, v => SfxVolume = v, false);
    public static void SetBgmVolume(float value) => Set(BgmKey, value, v => BgmVolume = v, false);

    private static void Set(string key, float value, Action<float> assign, bool master)
    {
      var clamped = Mathf.Clamp01(value);
      assign(clamped);
      PlayerPrefs.SetFloat(key, clamped);
      PlayerPrefs.Save();
      if (master) ApplyMasterVolume();
      Changed?.Invoke();
    }

    private static float Load(string key) => PlayerPrefs.GetFloat(key, 1f);

    private static void ApplyMasterVolume()
    {
      AudioListener.volume = MasterVolume;
    }
  }
}
