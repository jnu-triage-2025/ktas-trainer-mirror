using System;
using System.Collections.Generic;
using FishNet;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

namespace MultiplayerInfrastructure.Quest
{
  /// <summary>
  /// 플레이어별 퀘스트 상태 플래그 풀(Quest State Flag Pool)을 관리하는 정적 서비스.
  ///
  /// <para>
  /// 풀은 플레이어 한 명당 문자열 집합 하나다. 퀘스트 진행이 "지금 이 플레이어에게 무엇이 요구되는가"를
  /// 표현해야 할 때 임의로 정한 식별자를 이 집합에 넣고, 그것을 소비하는 쪽(상호작용 노출 판정 등)이
  /// 보유 여부를 묻는다. 값은 PlayerQuestStateFlag 레지스트리에 UserDescriptor.Identifier(UUID)를
  /// 키로 저장한다.
  /// </para>
  ///
  /// <para>
  /// 역할 태그(<see cref="PlayerTagService"/>)와 저장 구조는 같지만 쓰임이 다르다. 역할 태그는 세션 내내
  /// 유지되는 배역이고, 이 풀은 퀘스트 단계마다 켜졌다 꺼지는 진행 상태다. 두 저장소를 나눠 두어야
  /// 병렬 브랜치 배정(requiredPlayerTags)이 퀘스트 상태 문자열에 영향을 받지 않는다.
  /// </para>
  ///
  /// <para>
  /// 변경은 서버 권위다. 서버가 값을 바꾸면 소유 플레이어의 <see cref="PlayerController"/>가 전체
  /// 옵저버에게 스냅샷을 복제하므로, 각 피어는 모든 플레이어의 플래그를 읽을 수 있다. 네트워크가
  /// 꺼진 오프라인 컨텍스트에서는 로컬 저장소만 갱신한다.
  /// </para>
  /// </summary>
  public static class PlayerQuestStateFlagService
  {
    /// <summary>플래그 집합이 바뀐 플레이어의 UserDescriptor.Identifier 를 전달한다.</summary>
    public static event Action<string> FlagsChanged;

    /// <summary>
    /// 지금 실행 중인 콘텐츠가 쓰는 플래그 어휘. 인스펙터 같은 도구가 고를 수 있는 값을 제시하려면
    /// 어떤 문자열이 의미를 갖는지 알아야 하는데, 풀 자체는 아직 비어 있을 수 있다.
    /// </summary>
    private static readonly SortedSet<string> _knownFlags = new(StringComparer.Ordinal);

    /// <summary>복제 스냅샷을 현재 값과 비교할 때 쓰는 재사용 버퍼.</summary>
    private static readonly HashSet<string> _replaceBuffer = new(StringComparer.Ordinal);

    // ── 내부 헬퍼 ────────────────────────────────────────────────────────

    /// <summary>
    /// 서버이거나 네트워크가 아예 꺼진 컨텍스트에서만 값을 바꿀 수 있다.
    /// 도구가 조작 가능 여부를 미리 알 수 있도록 공개한다.
    /// </summary>
    public static bool CanMutate => InstanceFinder.IsServerStarted || InstanceFinder.IsOffline;

    /// <summary>서버로 위임할 변경 요청의 대상 지정 방식.</summary>
    internal enum QuestStateFlagScope : byte
    {
      /// <summary>UserDescriptor.Identifier 한 명.</summary>
      Identifier = 0,

      /// <summary>역할 태그 하나를 보유한 모든 플레이어.</summary>
      Tag = 1,

      /// <summary>역할 태그 중 하나라도 보유한 모든 플레이어.</summary>
      AnyTag = 2,

      /// <summary>접속한 모든 플레이어.</summary>
      All = 3,
    }

    /// <summary>
    /// 서버가 아닌 피어에서 발생한 변경을 서버로 넘긴다.
    ///
    /// <para>
    /// 병렬 역할 브랜치는 배정된 클라이언트에서만 실행된다. 그 브랜치가 여는 상호작용 플래그를
    /// 여기서 조용히 버리면, 담당자가 호스트가 아닌 순간 그 단계의 상호작용이 어느 피어에도
    /// 열리지 않아 다음 게이트가 영원히 막힌다. 그래서 클라이언트 컨텍스트에서는 무시하지 않고
    /// 서버에 요청을 올려, 기록과 복제는 그대로 서버 권위로 남긴다.
    /// </para>
    /// </summary>
    private static bool Relay(QuestStateFlagScope scope, string[] targets, string flag, bool value)
    {
      if (Scenario.ScenarioNetworkRelay.RequestQuestStateFlagChange(scope, targets, flag.Trim(), value))
        return true;

      Debug.LogWarning(
        "[PlayerQuestStateFlagService] Flag mutation attempted outside server context "
        + $"and could not be relayed to the server (scope={scope}, flag='{flag}'). Ignored.");
      return false;
    }

    private static string[] ToArray(IReadOnlyList<string> values)
    {
      var result = new string[values.Count];
      for (int i = 0; i < values.Count; i++)
        result[i] = values[i];
      return result;
    }

    /// <summary>
    /// 중계기가 서버에서 호출하는 적용 진입점. 권위 검사는 호출자(<c>ServerRpc</c>)가 마친 뒤이며,
    /// 대상 판정은 서버의 세션/태그 상태로 여기서 다시 수행한다.
    /// </summary>
    internal static void ApplyRelayed(QuestStateFlagScope scope, string[] targets, string flag, bool value)
    {
      if (!CanMutate || string.IsNullOrWhiteSpace(flag))
        return;

      switch (scope)
      {
        case QuestStateFlagScope.Identifier:
          if (targets != null && targets.Length > 0)
          {
            if (value)
              Set(targets[0], flag);
            else
              Unset(targets[0], flag);
          }
          break;
        case QuestStateFlagScope.Tag:
          if (targets != null && targets.Length > 0)
            SetForTag(targets[0], flag, value);
          break;
        case QuestStateFlagScope.AnyTag:
          if (targets != null && targets.Length > 0)
            SetForAnyTag(targets, flag, value);
          break;
        case QuestStateFlagScope.All:
          SetForAll(flag, value);
          break;
      }
    }

    private static HashSet<string> GetOrCreateFlagSet(string identifier)
    {
      if (Registry.Registry.TryGet<HashSet<string>>(
            RegistryType.PlayerQuestStateFlag, identifier, out var existing))
        return existing;

      var set = new HashSet<string>(StringComparer.Ordinal);
      Registry.Registry.Register(RegistryType.PlayerQuestStateFlag, identifier, set);
      return set;
    }

    private static void NotifyChanged(string identifier)
    {
      SyncOwnerFlags(identifier);
      RaiseChanged(identifier);
    }

    private static void SyncOwnerFlags(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier)
          || !Registry.Registry.TryGetEntityByOwnerUserIdentifier(identifier, out var descriptor))
        return;

      var playerObject = descriptor?.GameObject;
      if (playerObject == null
          || !playerObject.TryGetComponent<PlayerController>(out var controller)
          || controller == null)
        return;

      controller.SyncQuestStateFlagsToObservers();
    }

    /// <summary>
    /// 플래그 변경을 알리고, 이 피어의 조작 플레이어가 대상이면 상호작용 힌트를 즉시 다시 계산한다.
    /// 플래그가 상호작용 노출을 좌우하므로, 갱신이 없으면 다음 감지 주기까지 이전 목록이 남는다.
    /// </summary>
    private static void RaiseChanged(string identifier)
    {
      FlagsChanged?.Invoke(identifier);

      var localPlayer = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      if (localPlayer != null
          && string.Equals(localPlayer.UserIdentifier, identifier, StringComparison.Ordinal))
        localPlayer.RefreshInteractableHintsNow();
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>플래그 하나를 플레이어의 풀에 넣는다. 이미 있으면 아무 일도 하지 않는다.</summary>
    /// <returns>실제로 추가되었으면 true(서버에 위임한 경우 요청을 보냈으면 true).</returns>
    public static bool Set(string identifier, string flag)
    {
      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(flag))
        return false;

      if (!CanMutate)
        return Relay(QuestStateFlagScope.Identifier, new[] { identifier }, flag, true);

      if (!GetOrCreateFlagSet(identifier).Add(flag.Trim()))
        return false;

      NotifyChanged(identifier);
      return true;
    }

    /// <summary>플래그 하나를 플레이어의 풀에서 뺀다.</summary>
    /// <returns>실제로 제거되었으면 true(서버에 위임한 경우 요청을 보냈으면 true).</returns>
    public static bool Unset(string identifier, string flag)
    {
      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(flag))
        return false;

      if (!CanMutate)
        return Relay(QuestStateFlagScope.Identifier, new[] { identifier }, flag, false);

      if (!GetOrCreateFlagSet(identifier).Remove(flag.Trim()))
        return false;

      NotifyChanged(identifier);
      return true;
    }

    /// <summary>지정한 역할 태그를 보유한 모든 플레이어에게 플래그를 적용하거나 해제한다.</summary>
    /// <returns>실제로 상태가 바뀐 플레이어 수.</returns>
    public static int SetForTag(string tag, string flag, bool value = true)
    {
      if (string.IsNullOrWhiteSpace(tag) || string.IsNullOrWhiteSpace(flag))
        return 0;

      // 역할 브랜치는 배정된 클라이언트에서만 실행되므로, 이 호출이 원격 클라이언트에서
      // 출발할 수 있다. 대상 판정은 서버의 세션/태그 상태로 다시 해야 정확하다.
      if (!CanMutate)
        return Relay(QuestStateFlagScope.Tag, new[] { tag }, flag, value) ? 1 : 0;

      int changed = 0;
      foreach (var pair in UserDescriptorService.GetAll())
      {
        string identifier = pair.Value?.Identifier;
        if (string.IsNullOrWhiteSpace(identifier)
            || !PlayerTagService.HasTagOnIdentifier(identifier, tag))
          continue;

        if (value ? Set(identifier, flag) : Unset(identifier, flag))
          changed++;
      }

      return changed;
    }

    /// <summary>지정한 역할 태그 중 하나라도 보유한 플레이어에게 플래그를 적용하거나 해제한다.</summary>
    public static int SetForAnyTag(IReadOnlyList<string> tags, string flag, bool value = true)
    {
      if (tags == null || tags.Count == 0 || string.IsNullOrWhiteSpace(flag))
        return 0;

      if (!CanMutate)
        return Relay(QuestStateFlagScope.AnyTag, ToArray(tags), flag, value) ? 1 : 0;

      int changed = 0;
      foreach (var pair in UserDescriptorService.GetAll())
      {
        string identifier = pair.Value?.Identifier;
        if (string.IsNullOrWhiteSpace(identifier) || !HasAnyTag(identifier, tags))
          continue;

        if (value ? Set(identifier, flag) : Unset(identifier, flag))
          changed++;
      }

      return changed;
    }

    private static bool HasAnyTag(string identifier, IReadOnlyList<string> tags)
    {
      for (int i = 0; i < tags.Count; i++)
      {
        if (PlayerTagService.HasTagOnIdentifier(identifier, tags[i]))
          return true;
      }

      return false;
    }

    /// <summary>접속한 모든 플레이어에게 플래그를 적용하거나 해제한다.</summary>
    public static int SetForAll(string flag, bool value = true)
    {
      if (string.IsNullOrWhiteSpace(flag))
        return 0;

      if (!CanMutate)
        return Relay(QuestStateFlagScope.All, Array.Empty<string>(), flag, value) ? 1 : 0;

      int changed = 0;
      foreach (var pair in UserDescriptorService.GetAll())
      {
        string identifier = pair.Value?.Identifier;
        if (string.IsNullOrWhiteSpace(identifier))
          continue;

        if (value ? Set(identifier, flag) : Unset(identifier, flag))
          changed++;
      }

      return changed;
    }

    /// <summary>플레이어가 플래그를 보유하고 있는지 확인한다. 읽기는 모든 피어에서 허용된다.</summary>
    public static bool Has(string identifier, string flag)
    {
      if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(flag))
        return false;

      return Registry.Registry.TryGet<HashSet<string>>(
               RegistryType.PlayerQuestStateFlag, identifier, out var flags)
             && flags.Contains(flag.Trim());
    }

    /// <summary>플레이어의 플래그 집합을 읽기 전용으로 반환한다. 없으면 빈 집합.</summary>
    public static IReadOnlyCollection<string> GetFlags(string identifier)
    {
      if (!string.IsNullOrWhiteSpace(identifier)
          && Registry.Registry.TryGet<HashSet<string>>(
            RegistryType.PlayerQuestStateFlag, identifier, out var flags))
        return flags;

      return Array.Empty<string>();
    }

    /// <summary>
    /// 네트워크로 동기화된 스냅샷으로 로컬 집합을 교체한다. 서버/클라이언트 공용이며,
    /// 권위 검사를 거치지 않으므로 복제 경로에서만 호출한다.
    /// </summary>
    public static void ReplaceFlags(string identifier, IReadOnlyList<string> flags)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      var current = GetOrCreateFlagSet(identifier);
      _replaceBuffer.Clear();
      if (flags != null)
      {
        for (int i = 0; i < flags.Count; i++)
        {
          if (!string.IsNullOrWhiteSpace(flags[i]))
            _replaceBuffer.Add(flags[i]);
        }
      }

      // 스냅샷은 값이 그대로여도 도착한다. 특히 스폰 시점의 빈 스냅샷까지 변경으로 취급하면
      // 상호작용 힌트 재계산이 스폰 처리 도중 불려 나가므로, 실제로 달라졌을 때만 알린다.
      if (current.SetEquals(_replaceBuffer))
        return;

      current.Clear();
      foreach (string flag in _replaceBuffer)
        current.Add(flag);

      RaiseChanged(identifier);
    }

    /// <summary>플레이어 한 명의 플래그 저장소를 비운다. 접속 종료/디스폰 정리 용도.</summary>
    public static void ClearFlags(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      Registry.Registry.Unregister(RegistryType.PlayerQuestStateFlag, identifier);
      RaiseChanged(identifier);
    }

    // ── 플래그 어휘 ───────────────────────────────────────────────────────

    /// <summary>
    /// 콘텐츠가 쓰는 플래그 어휘를 등록한다. 풀에 값이 없어도 도구가 고를 수 있게 하려는 목적이며,
    /// 플래그 판정에는 관여하지 않는다. 풀은 여기 없는 문자열도 그대로 담는다.
    /// </summary>
    public static void RegisterKnownFlags(IEnumerable<string> flags)
    {
      if (flags == null)
        return;

      foreach (string flag in flags)
      {
        if (!string.IsNullOrWhiteSpace(flag))
          _knownFlags.Add(flag.Trim());
      }
    }

    /// <summary>등록된 플래그 어휘를 사전순으로 반환한다.</summary>
    public static IReadOnlyCollection<string> KnownFlags => _knownFlags;

    /// <summary>등록된 플래그 어휘를 비운다. 콘텐츠가 끝날 때 호출한다.</summary>
    public static void ClearKnownFlags() => _knownFlags.Clear();

    /// <summary>
    /// 접속한 모든 플레이어의 플래그를 비운다. 시나리오가 시작·종료될 때 이전 회차의 상태가
    /// 남지 않게 한다.
    ///
    /// <para>
    /// 전체 비우기는 서버에서만 수행하고 복제로 전파한다. 클라이언트가 자기 판단으로 비우면,
    /// 뒤늦게 접속해 버퍼된 스냅샷을 받은 피어가 그 스냅샷을 지워 버릴 수 있다.
    /// </para>
    /// </summary>
    public static void ClearAll()
    {
      if (!CanMutate)
        return;

      foreach (var pair in UserDescriptorService.GetAll())
      {
        string identifier = pair.Value?.Identifier;
        if (string.IsNullOrWhiteSpace(identifier))
          continue;

        if (!Registry.Registry.TryGet<HashSet<string>>(
              RegistryType.PlayerQuestStateFlag, identifier, out var flags)
            || flags.Count == 0)
          continue;

        flags.Clear();
        if (InstanceFinder.IsServerStarted)
          SyncOwnerFlags(identifier);
        RaiseChanged(identifier);
      }
    }
  }
}
