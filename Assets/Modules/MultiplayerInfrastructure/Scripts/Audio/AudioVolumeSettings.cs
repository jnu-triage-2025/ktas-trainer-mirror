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
    private const float DefaultVolume = 1f;

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

    private static float Load(string key) => PlayerPrefs.GetFloat(key, DefaultVolume);

    /// <summary>저장된 세 음량 값을 지웁니다.</summary>
    public static void ClearStoredValues()
    {
      PlayerPrefs.DeleteKey(MasterKey);
      PlayerPrefs.DeleteKey(SfxKey);
      PlayerPrefs.DeleteKey(BgmKey);
      PlayerPrefs.Save();
    }

    /// <summary>저장된 값을 지우고 세 음량을 모두 기본값(최대)으로 되돌립니다.</summary>
    public static void ResetToDefault()
    {
      ClearStoredValues();
      MasterVolume = DefaultVolume;
      SfxVolume = DefaultVolume;
      BgmVolume = DefaultVolume;
      ApplyMasterVolume();
      Changed?.Invoke();
    }

    private static void ApplyMasterVolume()
    {
      AudioListener.volume = MasterVolume;
    }
  }
}
