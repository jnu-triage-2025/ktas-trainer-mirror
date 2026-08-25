using FishNet;
using FishNet.Connection;
using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Audio
{
  /// <summary>
  /// 서버가 승인한 위치 기반 효과음을 모든 관측자에게 같은 위치에서 재생합니다.
  /// 클립 자체는 전송하지 않고, 모든 클라이언트에 포함된 Resources/Sound 식별자만 전송합니다.
  /// </summary>
  public sealed class SoundService : NetworkBehaviour
  {
    private const int MaxRequestsPerClientPerSecond = 20;
    private const float DefaultVolume = 1f;
    private const float DefaultSpatialBlend = 1f;

    private static SoundService _instance;
    private static readonly Dictionary<int, Queue<float>> RequestTimesByClient = new();

    /// <summary>씬에 배치된 Sound Service 인스턴스입니다. 없으면 null입니다.</summary>
    public static SoundService Instance => _instance;

    private void Awake()
    {
      _instance = this;
    }

    private void OnDestroy()
    {
      if (_instance == this)
        _instance = null;
      RequestTimesByClient.Clear();
    }

    /// <summary>
    /// 지정 위치에서 효과음을 재생하도록 서버에 요청합니다.
    /// 서버에서 호출하면 즉시 모든 관측자에게 전파하고, 오프라인에서는 로컬에서만 재생합니다.
    /// </summary>
    /// <param name="soundResourceIdentifier">Resources/Sound 아래의 클립 식별자입니다.</param>
    /// <param name="position">3D 감쇠 기준이 되는 월드 좌표입니다.</param>
    /// <param name="volume">0에서 1 사이의 음량입니다.</param>
    /// <param name="spatialBlend">0은 2D, 1은 완전한 3D 재생입니다.</param>
    /// <returns>요청을 수락했으면 true입니다.</returns>
    public static bool PlayAtPosition(
      string soundResourceIdentifier,
      Vector3 position,
      float volume = DefaultVolume,
      float spatialBlend = DefaultSpatialBlend)
    {
      if (!IsValidSoundResourceIdentifier(soundResourceIdentifier))
        return false;

      volume = Mathf.Clamp01(volume);
      spatialBlend = Mathf.Clamp01(spatialBlend);

      if (InstanceFinder.IsOffline)
      {
        PlayLocally(soundResourceIdentifier, position, volume, spatialBlend);
        return true;
      }

      if (_instance == null)
      {
        Debug.LogWarning("[SoundService] 네트워크 세션에 SoundService가 없습니다. 효과음을 재생하지 않습니다.");
        return false;
      }

      if (InstanceFinder.IsServerStarted)
      {
        if (LoadClip(soundResourceIdentifier) == null)
        {
          Debug.LogWarning($"[SoundService] 효과음 '{soundResourceIdentifier}'을(를) Resources/Sound에서 찾지 못했습니다.");
          return false;
        }

        _instance.PlayObserversRpc(soundResourceIdentifier, position, volume, spatialBlend);
        return true;
      }

      if (!InstanceFinder.IsClientStarted)
        return false;

      _instance.RequestPlayServerRpc(soundResourceIdentifier, position, volume, spatialBlend);
      return true;
    }

    /// <summary>Resources 식별자가 네트워크 메시지에 실을 수 있는 안전한 형식인지 확인합니다.</summary>
    public static bool IsValidSoundResourceIdentifier(string soundResourceIdentifier)
    {
      return !string.IsNullOrWhiteSpace(soundResourceIdentifier)
             && !soundResourceIdentifier.StartsWith("/", StringComparison.Ordinal)
             && !soundResourceIdentifier.Contains("..", StringComparison.Ordinal)
             && !soundResourceIdentifier.Contains("\\", StringComparison.Ordinal);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestPlayServerRpc(
      string soundResourceIdentifier,
      Vector3 position,
      float volume,
      float spatialBlend,
      NetworkConnection sender = null)
    {
      if (sender == null || !IsValidSoundResourceIdentifier(soundResourceIdentifier))
        return;

      if (!CanAcceptRequest(sender.ClientId))
      {
        Debug.LogWarning($"[SoundService] 클라이언트 {sender.ClientId}의 효과음 요청 빈도가 제한을 초과했습니다.");
        return;
      }

      // 서버가 존재하지 않는 리소스를 전파하지 않도록 먼저 확인합니다.
      if (LoadClip(soundResourceIdentifier) == null)
      {
        Debug.LogWarning($"[SoundService] 효과음 '{soundResourceIdentifier}'을(를) Resources/Sound에서 찾지 못했습니다.");
        return;
      }

      PlayObserversRpc(soundResourceIdentifier, position, Mathf.Clamp01(volume), Mathf.Clamp01(spatialBlend));
    }

    [ObserversRpc]
    private void PlayObserversRpc(string soundResourceIdentifier, Vector3 position, float volume, float spatialBlend)
    {
      PlayLocally(soundResourceIdentifier, position, volume, spatialBlend);
    }

    private static bool CanAcceptRequest(int clientId)
    {
      float now = Time.unscaledTime;
      if (!RequestTimesByClient.TryGetValue(clientId, out var times))
      {
        times = new Queue<float>();
        RequestTimesByClient.Add(clientId, times);
      }

      while (times.Count > 0 && now - times.Peek() >= 1f)
        times.Dequeue();

      if (times.Count >= MaxRequestsPerClientPerSecond)
        return false;

      times.Enqueue(now);
      return true;
    }

    private static void PlayLocally(string soundResourceIdentifier, Vector3 position, float volume, float spatialBlend)
    {
      var clip = LoadClip(soundResourceIdentifier);
      if (clip == null)
      {
        Debug.LogWarning($"[SoundService] 효과음 '{soundResourceIdentifier}'을(를) Resources/Sound에서 찾지 못했습니다.");
        return;
      }

      var soundObject = new GameObject($"SFX: {soundResourceIdentifier}");
      soundObject.transform.position = position;
      var source = soundObject.AddComponent<AudioSource>();
      source.spatialBlend = Mathf.Clamp01(spatialBlend);
      source.PlayOneShot(clip, Mathf.Clamp01(volume));
      Destroy(soundObject, clip.length);
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
