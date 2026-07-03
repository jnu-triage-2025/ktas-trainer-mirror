using System;
using System.Collections.Generic;
using FishNet;
using UnityEngine;

namespace MultiplayerInfrastructure.ItemSystem
{
  /// <summary>
  /// <see cref="StaticPlacedItem"/> 의 남은 획득 가능 횟수(Remains)를 서버 권위로 관리하는 정적 서비스입니다.
  ///
  /// <para>
  /// StaticPlacedItem 은 네트워크 오브젝트가 아니라 각 프로세스에 로컬로 존재하는 "맵의 일부"이므로,
  /// 상태의 진실 원천(source of truth)은 서버에만 존재해야 합니다. 이 서비스가 그 역할을 합니다.
  /// </para>
  ///
  /// <para>모든 변이 메서드는 서버에서만 호출되어야 합니다(<see cref="PlayerTagService"/> 와 동일한 규약).</para>
  ///
  /// 키 구성:
  /// - 전역(Global) 모드: entityIdentifier 하나당 Remains 하나.
  /// - 로컬(Local) 모드: (entityIdentifier, userIdentifier) 조합당 Remains 하나.
  /// </summary>
  public static class StaticPlacedItemService
  {
    // entityIdentifier -> Remains (Global 모드)
    private static readonly Dictionary<string, int> _globalRemains = new(StringComparer.Ordinal);

    // entityIdentifier -> (userIdentifier -> Remains) (Local 모드)
    private static readonly Dictionary<string, Dictionary<string, int>> _localRemains = new(StringComparer.Ordinal);

    private static bool IsServerMutationAllowed()
    {
      if (InstanceFinder.IsServerStarted)
        return true;

      Debug.LogWarning("[StaticPlacedItemService] State mutation attempted outside server context. Ignored.");
      return false;
    }

    // ── Global 모드 ──────────────────────────────────────────────────────

    /// <summary>
    /// 전역 Remains 값을 아직 초기화하지 않았다면 <paramref name="initialRemains"/> 로 초기화합니다(서버 전용).
    /// 이미 값이 있으면 유지합니다(멱등).
    /// </summary>
    public static void EnsureGlobalRemains(string entityIdentifier, int initialRemains)
    {
      if (!IsServerMutationAllowed() || string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      if (!_globalRemains.ContainsKey(entityIdentifier))
        _globalRemains[entityIdentifier] = initialRemains;
    }

    /// <summary>전역 Remains 를 조회합니다. 초기화되지 않았다면 <paramref name="fallback"/> 을 반환합니다.</summary>
    public static int GetGlobalRemains(string entityIdentifier, int fallback)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return fallback;

      return _globalRemains.TryGetValue(entityIdentifier, out var remains) ? remains : fallback;
    }

    /// <summary>전역 Remains 를 <paramref name="delta"/> 만큼 감소시키고, 감소 후 값을 반환합니다(서버 전용).</summary>
    public static int DecreaseGlobalRemains(string entityIdentifier, int delta, int fallbackInitial)
    {
      if (!IsServerMutationAllowed() || string.IsNullOrWhiteSpace(entityIdentifier))
        return fallbackInitial;

      if (!_globalRemains.TryGetValue(entityIdentifier, out var remains))
        remains = fallbackInitial;

      remains -= Mathf.Max(0, delta);
      _globalRemains[entityIdentifier] = remains;
      return remains;
    }

    /// <summary>
    /// 선점 감소했던 전역 Remains 를 <paramref name="delta"/> 만큼 되돌립니다(서버 전용).
    /// 픽업 확정 실패/중단 시 예약 복원에 사용됩니다.
    /// </summary>
    public static void RestoreGlobalRemains(string entityIdentifier, int delta)
    {
      if (!IsServerMutationAllowed() || string.IsNullOrWhiteSpace(entityIdentifier) || delta <= 0)
        return;

      // 예약은 항상 감소 이후에 존재하므로 키가 없으면 복원할 것이 없다.
      if (_globalRemains.TryGetValue(entityIdentifier, out var remains))
        _globalRemains[entityIdentifier] = remains + delta;
    }

    // ── Local 모드 ───────────────────────────────────────────────────────

    /// <summary>특정 유저의 Local Remains 를 아직 초기화하지 않았다면 초기화합니다(서버 전용, 멱등).</summary>
    public static void EnsureLocalRemains(string entityIdentifier, string userIdentifier, int initialRemains)
    {
      if (!IsServerMutationAllowed()
          || string.IsNullOrWhiteSpace(entityIdentifier)
          || string.IsNullOrWhiteSpace(userIdentifier))
        return;

      if (!_localRemains.TryGetValue(entityIdentifier, out var perUser))
      {
        perUser = new Dictionary<string, int>(StringComparer.Ordinal);
        _localRemains[entityIdentifier] = perUser;
      }

      if (!perUser.ContainsKey(userIdentifier))
        perUser[userIdentifier] = initialRemains;
    }

    /// <summary>특정 유저의 Local Remains 를 조회합니다. 없으면 <paramref name="fallback"/> 을 반환합니다.</summary>
    public static int GetLocalRemains(string entityIdentifier, string userIdentifier, int fallback)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(userIdentifier))
        return fallback;

      if (_localRemains.TryGetValue(entityIdentifier, out var perUser)
          && perUser.TryGetValue(userIdentifier, out var remains))
        return remains;

      return fallback;
    }

    /// <summary>특정 유저의 Local Remains 를 <paramref name="delta"/> 만큼 감소시키고, 감소 후 값을 반환합니다(서버 전용).</summary>
    public static int DecreaseLocalRemains(string entityIdentifier, string userIdentifier, int delta, int fallbackInitial)
    {
      if (!IsServerMutationAllowed()
          || string.IsNullOrWhiteSpace(entityIdentifier)
          || string.IsNullOrWhiteSpace(userIdentifier))
        return fallbackInitial;

      if (!_localRemains.TryGetValue(entityIdentifier, out var perUser))
      {
        perUser = new Dictionary<string, int>(StringComparer.Ordinal);
        _localRemains[entityIdentifier] = perUser;
      }

      if (!perUser.TryGetValue(userIdentifier, out var remains))
        remains = fallbackInitial;

      remains -= Mathf.Max(0, delta);
      perUser[userIdentifier] = remains;
      return remains;
    }

    /// <summary>
    /// 선점 감소했던 특정 유저의 Local Remains 를 <paramref name="delta"/> 만큼 되돌립니다(서버 전용).
    /// 픽업 확정 실패/중단 시 예약 복원에 사용됩니다.
    /// </summary>
    public static void RestoreLocalRemains(string entityIdentifier, string userIdentifier, int delta)
    {
      if (!IsServerMutationAllowed()
          || string.IsNullOrWhiteSpace(entityIdentifier)
          || string.IsNullOrWhiteSpace(userIdentifier)
          || delta <= 0)
        return;

      if (_localRemains.TryGetValue(entityIdentifier, out var perUser)
          && perUser.TryGetValue(userIdentifier, out var remains))
        perUser[userIdentifier] = remains + delta;
    }

    /// <summary>
    /// 특정 유저의 모든 Local 상태를 제거합니다(서버 전용).
    /// 기본적으로 플레이어가 접속을 종료하면 호출하여 재접속 시 상태를 초기화합니다.
    /// </summary>
    public static void ClearUser(string userIdentifier)
    {
      if (!IsServerMutationAllowed() || string.IsNullOrWhiteSpace(userIdentifier))
        return;

      foreach (var perUser in _localRemains.Values)
        perUser.Remove(userIdentifier);
    }

    /// <summary>특정 엔티티의 모든 상태(전역 + 모든 유저)를 제거합니다(서버 전용).</summary>
    public static void ClearEntity(string entityIdentifier)
    {
      if (!IsServerMutationAllowed() || string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      _globalRemains.Remove(entityIdentifier);
      _localRemains.Remove(entityIdentifier);
    }

    /// <summary>모든 상태를 초기화합니다(예: 씬 리로드/서버 재시작 시).</summary>
    public static void ClearAll()
    {
      _globalRemains.Clear();
      _localRemains.Clear();
    }
  }
}
