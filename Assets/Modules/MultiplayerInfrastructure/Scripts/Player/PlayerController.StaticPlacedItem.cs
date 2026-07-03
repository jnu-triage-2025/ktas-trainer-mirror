using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// <see cref="StaticPlacedItem"/> (에디터 사전 배치 정적 아이템)의 획득(Pickup) 및 사라짐(Vanish) 동기화를
  /// 서버 권위로 처리합니다.
  ///
  /// <para>
  /// StaticPlacedItem 은 네트워크 오브젝트가 아니므로 자체 RPC 를 가질 수 없습니다.
  /// 따라서 <see cref="ItemObject"/> 월드아이템 픽업과 동일하게, NetworkBehaviour 인
  /// PlayerController 를 통해 서버-권위 프로토콜을 태웁니다.
  /// 상태(Remains)의 진실 원천은 서버의 <see cref="StaticPlacedItemService"/> 입니다.
  /// </para>
  ///
  /// 흐름(월드아이템과 동일한 예약/확정 구조):
  /// 1. (Owner) <see cref="TryPickupStaticPlacedItem"/> → 서버로 요청.
  /// 2. (Server) 거리/Remains 검증 후, Remains 를 <b>선점 감소(예약)</b> 하고 요청자에게 획득을 확정(TargetRpc).
  /// 3. (Claimant) 전량 수용 가능하면 인벤토리에 추가하고 성공 보고, 아니면 실패 보고.
  /// 4a. (Server) 성공 보고 시: 예약을 소비하고, 예약 후 Remains 가 0 이하면 사라짐 처리를 브로드캐스트(모드별).
  /// 4b. (Server) 실패 보고/접속 종료 시: 선점 감소한 Remains 를 복원하고 예약을 제거.
  ///
  /// <para>
  /// 승인 시점에 Remains 를 선점하고 예약을 단일 사용(single-use)으로 소비하므로,
  /// (a) 무단 ack 호출로 인한 상태 변조, (b) vanish RPC 도착 전 연타로 인한 double-grant,
  /// (c) ack 재전송으로 인한 과다 감소를 모두 차단한다.
  /// </para>
  /// </summary>
  public partial class PlayerController
  {
    private const float MaxStaticPlacedItemPickupDistance = 4f;
    private const float MaxStaticPlacedItemPickupDistanceSqr =
      MaxStaticPlacedItemPickupDistance * MaxStaticPlacedItemPickupDistance;

    /// <summary>
    /// 서버 전용: 승인되었으나 아직 확정(성공/실패)되지 않은 정적 아이템 픽업 예약.
    /// 키는 entityIdentifier. 승인 시점에 Remains 를 이미 선점 감소했으므로, 확정/복원 시 이 정보를 사용한다.
    /// PlayerController 인스턴스 간 공유(월드아이템 <c>_pendingWorldItemPickups</c> 와 동일한 static 설계).
    /// </summary>
    private sealed class PendingStaticPickup
    {
      public int ClaimantClientId { get; set; }
      public string UserIdentifier { get; set; }
      public StaticPlacedItemVanishMode VanishMode { get; set; }
      public int DecreasedBy { get; set; }
    }

    private static readonly Dictionary<string, PendingStaticPickup> _pendingStaticPickups =
      new(StringComparer.Ordinal);

    /// <summary>
    /// Owner 로컬 전용: 확정을 기다리는 중인 요청의 만료 시각(entityIdentifier → 만료 <see cref="Time.unscaledTime"/>).
    /// 연타로 인한 중복 요청을 방지하되, 서버가 승인을 조용히 거부해 왕복이 완료되지 않는 경우에도
    /// 짧은 쿨다운 뒤 재시도가 가능하도록 시간 기반으로 관리한다(영구 잠김 방지).
    /// </summary>
    private readonly Dictionary<string, float> _inFlightStaticPickupExpiry = new(StringComparer.Ordinal);

    /// <summary>in-flight 요청이 확정 없이 방치될 때 재시도를 허용하기까지의 쿨다운(초).</summary>
    private const float StaticPickupInFlightTimeout = 2f;

    // ── 진입점 (Owner) ─────────────────────────────────────────────────────

    /// <summary>
    /// 정적 아이템 획득을 시도합니다. <see cref="StaticPlacedItem.Interact"/> 에서 호출됩니다.
    /// </summary>
    public void TryPickupStaticPlacedItem(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      entityIdentifier = entityIdentifier.Trim();

      if (!TryGetStaticPlacedItem(entityIdentifier, out var staticItem) || staticItem == null)
      {
        Debug.LogWarning($"[PlayerController] TryPickupStaticPlacedItem failed: '{entityIdentifier}' not found.");
        return;
      }

      // 로컬 in-flight 가드: 아직 쿨다운이 유효한 동일 아이템이면 중복 요청하지 않는다(연타 방지).
      // 확정(성공/실패)되면 즉시 해제되고, 확정이 오지 않아도 쿨다운 만료 후 재시도가 가능하다.
      if (_inFlightStaticPickupExpiry.TryGetValue(entityIdentifier, out var expiry)
          && Time.unscaledTime < expiry)
        return;

      _inFlightStaticPickupExpiry[entityIdentifier] = Time.unscaledTime + StaticPickupInFlightTimeout;

      RequestPickupStaticPlacedItem(entityIdentifier);
    }

    private void RequestPickupStaticPlacedItem(string entityIdentifier)
    {
      // 네트워크가 없는 단독 실행(에디터 단독 플레이) 또는 서버 호스트면 서버 로직을 직접 실행.
      if (!IsSpawned || IsServerStarted)
      {
        ServerApprovePickupStaticPlacedItem(entityIdentifier, Owner);
        return;
      }

      CmdRequestPickupStaticPlacedItem(entityIdentifier);
    }

    [ServerRpc]
    private void CmdRequestPickupStaticPlacedItem(string entityIdentifier, NetworkConnection sender = null)
    {
      ServerApprovePickupStaticPlacedItem(entityIdentifier, sender ?? Owner);
    }

    // ── 서버 승인 (Remains 선점 + 예약 기록) ──────────────────────────────────

    private void ServerApprovePickupStaticPlacedItem(string entityIdentifier, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      if (Owner != null && Owner.IsValid && claimant.ClientId != Owner.ClientId)
        return;

      if (!TryGetStaticPlacedItem(entityIdentifier, out var staticItem) || staticItem == null)
        return;

      var reward = staticItem.PickupReward;
      if (string.IsNullOrWhiteSpace(reward.ItemIdentifier))
      {
        Debug.LogWarning($"[PlayerController] StaticPlacedItem '{entityIdentifier}' has empty item identifier.");
        return;
      }

      // 동일 아이템에 대한 미확정 예약이 이미 있으면 거부(단일 사용 보장 · 동시 승인 방지).
      if (_pendingStaticPickups.ContainsKey(entityIdentifier))
        return;

      // 거리 검증 (월드아이템 픽업과 동일 규약).
      var claimantPosition = ResolveServerPickupOriginPosition();
      float sqrDistance = (staticItem.transform.position - claimantPosition).sqrMagnitude;
      if (sqrDistance > MaxStaticPlacedItemPickupDistanceSqr)
      {
        Debug.LogWarning(
          $"[PlayerController] Reject static pickup '{entityIdentifier}': too far. " +
          $"distance={Mathf.Sqrt(sqrDistance):0.00}m, limit={MaxStaticPlacedItemPickupDistance:0.00}m");
        return;
      }

      // 모드별 Remains 검증 및 선점 감소(예약). AlwaysExists 는 상태를 두지 않는다.
      if (!ServerTryReserveStaticPickup(staticItem, entityIdentifier, claimant, out var pending))
        return;

      _pendingStaticPickups[entityIdentifier] = pending;

      TargetConfirmPickupStaticPlacedItem(
        claimant,
        entityIdentifier,
        reward.ItemIdentifier,
        reward.Amount);
    }

    /// <summary>
    /// 모드에 따라 획득 가능 여부를 확인하고, 가능하면 Remains 를 선점 감소하여 예약을 만듭니다(서버 전용).
    /// AlwaysExists 는 상태 변이 없이 예약만 만듭니다.
    /// </summary>
    private bool ServerTryReserveStaticPickup(
      StaticPlacedItem staticItem,
      string entityIdentifier,
      NetworkConnection claimant,
      out PendingStaticPickup pending)
    {
      pending = null;
      int decreaseBy = Mathf.Max(0, staticItem.PickupReward.DecreaseRemains);

      switch (staticItem.VanishMode)
      {
        case StaticPlacedItemVanishMode.AlwaysExists:
          pending = new PendingStaticPickup
          {
            ClaimantClientId = claimant.ClientId,
            UserIdentifier = null,
            VanishMode = StaticPlacedItemVanishMode.AlwaysExists,
            DecreasedBy = 0,
          };
          return true;

        case StaticPlacedItemVanishMode.VanishedGlobalOnPickup:
        {
          StaticPlacedItemService.EnsureGlobalRemains(entityIdentifier, staticItem.InitialRemains);
          if (StaticPlacedItemService.GetGlobalRemains(entityIdentifier, staticItem.InitialRemains) <= 0)
            return false;

          if (decreaseBy > 0)
            StaticPlacedItemService.DecreaseGlobalRemains(entityIdentifier, decreaseBy, staticItem.InitialRemains);

          pending = new PendingStaticPickup
          {
            ClaimantClientId = claimant.ClientId,
            UserIdentifier = null,
            VanishMode = StaticPlacedItemVanishMode.VanishedGlobalOnPickup,
            DecreasedBy = decreaseBy,
          };
          return true;
        }

        case StaticPlacedItemVanishMode.VanishedLocalOnPickup:
        {
          string userIdentifier = ResolveUserIdentifier(claimant);
          if (string.IsNullOrWhiteSpace(userIdentifier))
            return false;

          StaticPlacedItemService.EnsureLocalRemains(entityIdentifier, userIdentifier, staticItem.InitialRemains);
          if (StaticPlacedItemService.GetLocalRemains(entityIdentifier, userIdentifier, staticItem.InitialRemains) <= 0)
            return false;

          if (decreaseBy > 0)
            StaticPlacedItemService.DecreaseLocalRemains(entityIdentifier, userIdentifier, decreaseBy, staticItem.InitialRemains);

          pending = new PendingStaticPickup
          {
            ClaimantClientId = claimant.ClientId,
            UserIdentifier = userIdentifier,
            VanishMode = StaticPlacedItemVanishMode.VanishedLocalOnPickup,
            DecreasedBy = decreaseBy,
          };
          return true;
        }

        default:
          return false;
      }
    }

    // ── 요청자 확정 ────────────────────────────────────────────────────────

    [TargetRpc]
    private void TargetConfirmPickupStaticPlacedItem(
      NetworkConnection conn,
      string entityIdentifier,
      string itemIdentifier,
      int amount)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
      {
        ReportStaticPickupFailure(entityIdentifier);
        return;
      }

      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
      {
        Debug.LogWarning($"[PlayerController] Failed to create static pickup item '{itemIdentifier}'.");
        ReportStaticPickupFailure(entityIdentifier);
        return;
      }

      item.CurrentStackCount = Mathf.Max(1, amount);

      // 월드아이템과 동일한 all-or-nothing 규약: 전량 수용 가능할 때만 추가.
      if (!CanAcceptItem(item) || !TryAddItemToInventory(item))
      {
        Debug.LogWarning(
          $"[PlayerController] Static pickup confirmation for '{entityIdentifier}', but inventory is full.");
        ReportStaticPickupFailure(entityIdentifier);
        return;
      }

      // OnGet 은 TryAddItemToInventory 내부에서 1회 호출된다.
      AcknowledgeStaticPickupSuccess(entityIdentifier);
    }

    private void AcknowledgeStaticPickupSuccess(string entityIdentifier)
    {
      ClearInFlightStaticPickup(entityIdentifier);

      if (!IsSpawned || IsServerStarted)
        ServerConfirmStaticPickupSuccess(entityIdentifier, Owner);
      else
        CmdAcknowledgePickupStaticPlacedItemSuccess(entityIdentifier);
    }

    private void ReportStaticPickupFailure(string entityIdentifier)
    {
      ClearInFlightStaticPickup(entityIdentifier);

      // 선점 감소한 Remains 를 서버에서 복원해야 하므로 실패를 반드시 보고한다.
      if (!IsSpawned || IsServerStarted)
        ServerRestoreStaticPickup(entityIdentifier, Owner);
      else
        CmdReportPickupStaticPlacedItemFailure(entityIdentifier);
    }

    private void ClearInFlightStaticPickup(string entityIdentifier)
    {
      if (!string.IsNullOrWhiteSpace(entityIdentifier))
        _inFlightStaticPickupExpiry.Remove(entityIdentifier);
    }

    [ServerRpc]
    private void CmdAcknowledgePickupStaticPlacedItemSuccess(string entityIdentifier, NetworkConnection sender = null)
    {
      ServerConfirmStaticPickupSuccess(entityIdentifier, sender ?? Owner);
    }

    [ServerRpc]
    private void CmdReportPickupStaticPlacedItemFailure(string entityIdentifier, NetworkConnection sender = null)
    {
      ServerRestoreStaticPickup(entityIdentifier, sender ?? Owner);
    }

    // ── 서버 성공 확정: 예약 소비 + 사라짐 브로드캐스트 ────────────────────────

    private void ServerConfirmStaticPickupSuccess(string entityIdentifier, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      // 예약이 존재하고, 그 예약이 이 요청자의 것일 때만 진행(무단/재전송 ack 차단).
      if (!_pendingStaticPickups.TryGetValue(entityIdentifier, out var pending) || pending == null)
        return;

      if (pending.ClaimantClientId != claimant.ClientId)
        return;

      // 예약 소비(단일 사용).
      _pendingStaticPickups.Remove(entityIdentifier);

      if (!TryGetStaticPlacedItem(entityIdentifier, out var staticItem) || staticItem == null)
        return;

      switch (pending.VanishMode)
      {
        case StaticPlacedItemVanishMode.AlwaysExists:
          return;

        case StaticPlacedItemVanishMode.VanishedGlobalOnPickup:
        {
          int remaining = StaticPlacedItemService.GetGlobalRemains(entityIdentifier, staticItem.InitialRemains);
          if (remaining <= 0)
            RpcVanishStaticPlacedItemGlobal(entityIdentifier);
          return;
        }

        case StaticPlacedItemVanishMode.VanishedLocalOnPickup:
        {
          string userIdentifier = pending.UserIdentifier;
          if (string.IsNullOrWhiteSpace(userIdentifier))
            return;

          int remaining = StaticPlacedItemService.GetLocalRemains(entityIdentifier, userIdentifier, staticItem.InitialRemains);
          if (remaining <= 0)
            TargetVanishStaticPlacedItemLocal(claimant, entityIdentifier);
          return;
        }
      }
    }

    // ── 서버 실패/중단 복원: 선점 감소한 Remains 되돌리고 예약 제거 ────────────

    private void ServerRestoreStaticPickup(string entityIdentifier, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      if (!_pendingStaticPickups.TryGetValue(entityIdentifier, out var pending) || pending == null)
        return;

      if (pending.ClaimantClientId != claimant.ClientId)
        return;

      RestoreReservedRemains(entityIdentifier, pending);
      _pendingStaticPickups.Remove(entityIdentifier);
    }

    /// <summary>선점 감소했던 Remains 를 복원한다(서버 전용). 음수 delta 로 재증가.</summary>
    private static void RestoreReservedRemains(string entityIdentifier, PendingStaticPickup pending)
    {
      if (pending == null || pending.DecreasedBy <= 0)
        return;

      switch (pending.VanishMode)
      {
        case StaticPlacedItemVanishMode.VanishedGlobalOnPickup:
          StaticPlacedItemService.RestoreGlobalRemains(entityIdentifier, pending.DecreasedBy);
          break;

        case StaticPlacedItemVanishMode.VanishedLocalOnPickup:
          if (!string.IsNullOrWhiteSpace(pending.UserIdentifier))
            StaticPlacedItemService.RestoreLocalRemains(entityIdentifier, pending.UserIdentifier, pending.DecreasedBy);
          break;
      }
    }

    /// <summary>
    /// 이 클라이언트가 점유했지만 확정하지 못한 정적 픽업 예약을 복원한다(서버 전용).
    /// 접속 종료 시 <see cref="OnStopServer"/> 에서 호출된다.
    /// </summary>
    private void RestorePendingStaticPickupsForClaimant()
    {
      if (Owner == null || !Owner.IsValid)
        return;

      int claimantId = Owner.ClientId;
      var toRestore = new List<string>();
      foreach (var kvp in _pendingStaticPickups)
      {
        if (kvp.Value != null && kvp.Value.ClaimantClientId == claimantId)
          toRestore.Add(kvp.Key);
      }

      foreach (var entityIdentifier in toRestore)
      {
        if (_pendingStaticPickups.TryGetValue(entityIdentifier, out var pending))
        {
          RestoreReservedRemains(entityIdentifier, pending);
          _pendingStaticPickups.Remove(entityIdentifier);
        }
      }
    }

    // ── 사라짐 적용 (로컬 표현) ───────────────────────────────────────────────

    // 신규 접속자 동기화는 SyncStaticPlacedItemsToConnection(TargetRpc) 로 별도 처리하므로
    // BufferLast 는 사용하지 않는다(엔티티별 구분이 안 되어 마지막 1건만 버퍼되기 때문).
    [ObserversRpc]
    private void RpcVanishStaticPlacedItemGlobal(string entityIdentifier)
    {
      ApplyStaticPlacedItemVanishedLocal(entityIdentifier);
    }

    [TargetRpc]
    private void TargetVanishStaticPlacedItemLocal(NetworkConnection conn, string entityIdentifier)
    {
      ApplyStaticPlacedItemVanishedLocal(entityIdentifier);
    }

    private void ApplyStaticPlacedItemVanishedLocal(string entityIdentifier)
    {
      if (TryGetStaticPlacedItem(entityIdentifier, out var staticItem) && staticItem != null)
        staticItem.ApplyVanished();
    }

    // ── 신규 접속자 동기화 (서버에서 호출) ───────────────────────────────────

    /// <summary>
    /// 새로 접속한 연결에 대해, 이미 전역적으로 사라진(Global Remains &lt;= 0) 정적 아이템들을
    /// 그 연결에서만 사라진 상태로 맞춰줍니다(서버 전용).
    /// Local 모드는 재접속 시 초기화 정책이므로 여기서 복원하지 않습니다.
    /// </summary>
    private void SyncStaticPlacedItemsToConnection(NetworkConnection connection)
    {
      if (connection == null || !IsServerStarted)
        return;

      foreach (var pair in Registry.Registry.GetAllEntities(EntityType.StaticPlacedItem))
      {
        var go = pair.Value?.GameObject;
        if (go == null || !go.TryGetComponent<StaticPlacedItem>(out var staticItem) || staticItem == null)
          continue;

        if (staticItem.VanishMode != StaticPlacedItemVanishMode.VanishedGlobalOnPickup)
          continue;

        int remaining = StaticPlacedItemService.GetGlobalRemains(pair.Key, staticItem.InitialRemains);
        if (remaining <= 0)
          TargetVanishStaticPlacedItemLocal(connection, pair.Key);
      }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static bool TryGetStaticPlacedItem(string entityIdentifier, out StaticPlacedItem staticItem)
    {
      staticItem = null;
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return false;

      if (!Registry.Registry.TryGetEntity(entityIdentifier, out var descriptor) || descriptor == null)
        return false;

      var go = descriptor.GameObject;
      if (go == null)
        return false;

      return go.TryGetComponent(out staticItem) && staticItem != null;
    }

    private string ResolveUserIdentifier(NetworkConnection claimant)
    {
      // 이 PlayerController 의 소유자 요청이면 자신의 UserIdentifier 를 사용.
      if (!string.IsNullOrWhiteSpace(UserIdentifier))
        return UserIdentifier;

      // 폴백: claimant 의 ClientId 로 엔티티를 찾아 UserIdentifier 조회.
      if (claimant != null
          && Registry.Registry.TryGetEntityByClientId(claimant.ClientId, out var descriptor)
          && descriptor != null)
        return descriptor.OwnerUserIdentifier;

      return null;
    }
  }
}
