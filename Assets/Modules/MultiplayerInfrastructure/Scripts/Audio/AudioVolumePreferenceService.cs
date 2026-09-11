using PlayerPrefs = MultiplayerInfrastructure.Automation.ProfilePlayerPrefs;
using System;
using MultiplayerInfrastructure.Performance;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 전체 사운드 볼륨을 적용하고 저장하는 서비스입니다.
  /// </summary>
  public class AudioVolumePreferenceService : MonoBehaviour
  {
    private const string PlayerPrefsKey = "MultiplayerInfrastructure.AudioVolume";
    private const float DefaultVolume = 1f;

    /// <summary>볼륨이 변경되었을 때 새 값(0~1)을 알립니다.</summary>
    public event Action<float> OnVolumeChanged;

    /// <summary>현재 전체 사운드 볼륨입니다. 0은 음소거이고 1은 최대 볼륨입니다.</summary>
    public float CurrentVolume { get; private set; } = DefaultVolume;

    public static AudioVolumePreferenceService Instance
      => Registry.Registry.Get<AudioVolumePreferenceService>(
        RegistryType.Service, Registry.Registry.TypeKey<AudioVolumePreferenceService>());

    /// <summary>서비스가 아직 로드되지 않은 시작 화면에서도 볼륨을 적용합니다.</summary>
    public static AudioVolumePreferenceService GetOrCreateInstance()
    {
      var service = Instance;
      if (service != null)
        return service;

      service = UnityEngine.Object.FindAnyObjectByType<AudioVolumePreferenceService>();
      if (service != null)
      {
        Registry.Registry.Register(
          RegistryType.Service,
          Registry.Registry.TypeKey<AudioVolumePreferenceService>(),
          service);
        return service;
      }

      var host = new GameObject(nameof(AudioVolumePreferenceService));
      UnityEngine.Object.DontDestroyOnLoad(host);
      return host.AddComponent<AudioVolumePreferenceService>();
    }

    private void Awake()
    {
      Registry.Registry.Register(
        RegistryType.Service,
        Registry.Registry.TypeKey<AudioVolumePreferenceService>(),
        this);
      LoadAndApply();
    }

    private void OnDestroy()
    {
      if (Instance == this)
      {
        Registry.Registry.Unregister(
          RegistryType.Service,
          Registry.Registry.TypeKey<AudioVolumePreferenceService>());
      }
    }

    /// <summary>전체 사운드 볼륨을 즉시 적용하고 저장합니다.</summary>
    public void SetVolume(float volume) => ApplyVolume(volume, persist: true);

    /// <summary>저장된 전체 사운드 볼륨을 읽어 즉시 적용합니다. 읽기만 하므로 다시 저장하지 않습니다.</summary>
    public void LoadAndApply()
    {
      ApplyVolume(PlayerPrefs.GetFloat(PlayerPrefsKey, DefaultVolume), persist: false);
    }

    /// <summary>저장된 값을 지우고 기본 볼륨을 적용합니다.</summary>
    public void ResetToDefault()
    {
      ClearStoredValue();
      ApplyVolume(DefaultVolume, persist: false);
    }

    /// <summary>저장된 전체 사운드 볼륨을 지웁니다.</summary>
    public static void ClearStoredValue()
    {
      PlayerPrefs.DeleteKey(PlayerPrefsKey);
      PlayerPrefs.Save();
    }

    private void ApplyVolume(float volume, bool persist)
    {
      CurrentVolume = Mathf.Clamp01(volume);
      if (!MppmLiteMode.IsActive)
        AudioListener.volume = CurrentVolume;
      if (persist)
      {
        PlayerPrefs.SetFloat(PlayerPrefsKey, CurrentVolume);
        PlayerPrefs.Save();
      }
      OnVolumeChanged?.Invoke(CurrentVolume);
    }
  }
}
