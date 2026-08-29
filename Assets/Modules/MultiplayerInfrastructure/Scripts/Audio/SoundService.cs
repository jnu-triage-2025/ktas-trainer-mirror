using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 지정한 위치에서 효과음을 재생합니다. 네트워크 동기화 없이 호출한 클라이언트의 로컬에서만 재생됩니다.
  /// </summary>
  public sealed class SoundService : MonoBehaviour
  {
    private const float DefaultVolume = 1f;
    private const float DefaultSpatialBlend = 1f;

    /// <summary>
    /// 지정 위치에서 효과음을 재생합니다.
    /// </summary>
    /// <param name="soundResourceIdentifier">Resources/Sound 아래의 클립 식별자입니다.</param>
    /// <param name="position">3D 감쇠 기준이 되는 월드 좌표입니다.</param>
    /// <param name="volume">0에서 1 사이의 음량입니다.</param>
    /// <param name="spatialBlend">0은 2D, 1은 완전한 3D 재생입니다.</param>
    /// <returns>재생을 시작했으면 true입니다.</returns>
    public static bool PlayAtPosition(
      string soundResourceIdentifier,
      Vector3 position,
      float volume = DefaultVolume,
      float spatialBlend = DefaultSpatialBlend)
    {
      if (!IsValidSoundResourceIdentifier(soundResourceIdentifier))
        return false;

      var clip = LoadClip(soundResourceIdentifier);
      if (clip == null)
      {
        Debug.LogWarning($"[SoundService] 효과음 '{soundResourceIdentifier}'을(를) Resources/Sound에서 찾지 못했습니다.");
        return false;
      }

      var soundObject = new GameObject($"SFX: {soundResourceIdentifier}");
      soundObject.transform.position = position;
      var source = soundObject.AddComponent<AudioSource>();
      source.spatialBlend = Mathf.Clamp01(spatialBlend);
      source.PlayOneShot(clip, Mathf.Clamp01(volume));
      Destroy(soundObject, clip.length);
      return true;
    }

    /// <summary>Resources 식별자가 안전한 형식인지 확인합니다.</summary>
    public static bool IsValidSoundResourceIdentifier(string soundResourceIdentifier)
    {
      return !string.IsNullOrWhiteSpace(soundResourceIdentifier)
             && !soundResourceIdentifier.StartsWith('/')
             && !soundResourceIdentifier.Contains("..", StringComparison.Ordinal)
             && !soundResourceIdentifier.Contains("\\", StringComparison.Ordinal);
    }

    private static AudioClip LoadClip(string soundResourceIdentifier)
    {
      var resourcePath = soundResourceIdentifier.StartsWith("Sound/", StringComparison.Ordinal)
        ? soundResourceIdentifier
        : $"Sound/{soundResourceIdentifier}";
      return Resources.Load<AudioClip>(resourcePath);
    }
  }
}
