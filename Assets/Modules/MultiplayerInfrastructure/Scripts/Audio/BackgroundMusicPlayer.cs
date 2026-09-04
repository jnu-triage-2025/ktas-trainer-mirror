using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>배경음악을 연결해 재생하기 위한 확장 지점입니다. 클립은 인스펙터에서 지정합니다.</summary>
  [RequireComponent(typeof(AudioSource))]
  public sealed class BackgroundMusicPlayer : MonoBehaviour
  {
    [SerializeField] private AudioClip _musicClip;
    [SerializeField] private bool _playOnAwake = true;
    [SerializeField] private bool _loop = true;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    private AudioSource _audioSource;

    private void Awake()
    {
      _audioSource = GetComponent<AudioSource>();
      _audioSource.loop = _loop;
      _audioSource.playOnAwake = false;
      _audioSource.spatialBlend = 0f;
      ApplyVolume();
      AudioVolumeSettings.Changed += ApplyVolume;
      if (_playOnAwake && _musicClip != null) Play();
    }

    public void Play()
    {
      if (_musicClip == null) return;
      _audioSource.clip = _musicClip;
      _audioSource.Play();
    }

    public void Stop() => _audioSource.Stop();

    private void ApplyVolume()
    {
      if (_audioSource != null) _audioSource.volume = _volume * AudioVolumeSettings.BgmVolume;
    }

    private void OnDestroy() => AudioVolumeSettings.Changed -= ApplyVolume;
  }
}
