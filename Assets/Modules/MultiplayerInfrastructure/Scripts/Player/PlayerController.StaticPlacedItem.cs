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

      // 유효한 보상이 하나도 없으면(목록이 비었거나 모든 항목이 무효) 이 아이템은 무시한다(획득 불가).
      if (!staticItem.HasAnyReward)
      {
        Debug.LogWarning($"[PlayerController] StaticPlacedItem '{entityIdentifier}' has no valid pickup reward.");
        return;
      }

      // 유효 보상들을 병렬 배열(식별자/수량)로 수집하여 요청자에게 전달한다.
      CollectValidRewards(staticItem, out var itemIdentifiers, out var amounts);
      if (itemIdentifiers.Length == 0)
      {
        Debug.LogWarning($"[PlayerController] StaticPlacedItem '{entityIdentifier}' has no valid pickup reward.");
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
        itemIdentifiers,
        amounts);
    }

    /// <summary>
    /// 정적 아이템의 유효 보상(식별자가 비어 있지 않은 항목)들을 병렬 배열로 수집합니다(서버 전용).
    /// index 0 은 대표(표시 기준) 보상입니다.
    /// </summary>
    private static void CollectValidRewards(
      StaticPlacedItem staticItem,
      out string[] itemIdentifiers,
      out int[] amounts)
    {
      var rewards = staticItem.PickupRewards;
      var ids = new List<string>(rewards.Count);
      var amts = new List<int>(rewards.Count);

      for (int i = 0; i < rewards.Count; i++)
      {
        var reward = rewards[i];
        if (string.IsNullOrWhiteSpace(reward.ItemIdentifier))
          continue;

        ids.Add(reward.ItemIdentifier);
        amts.Add(reward.Amount);
      }

      itemIdentifiers = ids.ToArray();
      amounts = amts.ToArray();
    }

    /// <summary>
    /// 1회 획득 시 Remains 감소량을 결정합니다(서버 전용).
    /// 유효 보상들의 DecreaseRemains 중 최댓값을 사용하며, 유효 보상이 없으면 0 입니다.
    /// </summary>
    private static int ResolveDecreaseRemains(StaticPlacedItem staticItem)
    {
      var rewards = staticItem.PickupRewards;
      int max = 0;
      for (int i = 0; i < rewards.Count; i++)
      {
        var reward = rewards[i];
        if (string.IsNullOrWhiteSpace(reward.ItemIdentifier))
          continue;

        int d = Mathf.Max(0, reward.DecreaseRemains);
        if (d > max)
          max = d;
      }

      return max;
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
      // 1회 획득으로 Remains 를 얼마나 줄일지는 유효 보상들의 DecreaseRemains 중 최댓값을 사용한다.
      // (단일 보상일 때는 기존과 동일하게 그 항목의 값이 그대로 적용된다.)
      int decreaseBy = ResolveDecreaseRemains(staticItem);

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
          int currentRemains = StaticPlacedItemService.GetGlobalRemains(entityIdentifier, staticItem.InitialRemains);
          if (currentRemains <= 0)
            return false;

          // 예약 복원은 실제로 줄인 양만 되돌려야 한다. 보상 설정의 감소량이 남은 수량보다
          // 큰 경우에도 Remains가 음수가 되거나 복원 후 초기 상한을 넘지 않도록 제한한다.
          int reservedDecrease = Mathf.Min(decreaseBy, currentRemains);
          if (reservedDecrease > 0)
            StaticPlacedItemService.DecreaseGlobalRemains(entityIdentifier, reservedDecrease, staticItem.InitialRemains);

          pending = new PendingStaticPickup
          {
            ClaimantClientId = claimant.ClientId,
            UserIdentifier = null,
            VanishMode = StaticPlacedItemVanishMode.VanishedGlobalOnPickup,
            DecreasedBy = reservedDecrease,
          };
          return true;
        }

        case StaticPlacedItemVanishMode.VanishedLocalOnPickup:
        {
          string userIdentifier = ResolveUserIdentifier(claimant);
          if (string.IsNullOrWhiteSpace(userIdentifier))
            return false;

          StaticPlacedItemService.EnsureLocalRemains(entityIdentifier, userIdentifier, staticItem.InitialRemains);
          int currentRemains = StaticPlacedItemService.GetLocalRemains(entityIdentifier, userIdentifier, staticItem.InitialRemains);
          if (currentRemains <= 0)
            return false;

          int reservedDecrease = Mathf.Min(decreaseBy, currentRemains);
          if (reservedDecrease > 0)
            StaticPlacedItemService.DecreaseLocalRemains(entityIdentifier, userIdentifier, reservedDecrease, staticItem.InitialRemains);

          pending = new PendingStaticPickup
          {
            ClaimantClientId = claimant.ClientId,
            UserIdentifier = userIdentifier,
            VanishMode = StaticPlacedItemVanishMode.VanishedLocalOnPickup,
            DecreasedBy = reservedDecrease,
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
      string[] itemIdentifiers,
      int[] amounts)
    {
      if (itemIdentifiers == null || itemIdentifiers.Length == 0
          || amounts == null || amounts.Length != itemIdentifiers.Length)
      {
        ReportStaticPickupFailure(entityIdentifier);
        return;
      }

      // 지급할 모든 아이템 인스턴스를 먼저 생성한다.
      var items = new List<Item>(itemIdentifiers.Length);
      for (int i = 0; i < itemIdentifiers.Length; i++)
      {
        string itemIdentifier = itemIdentifiers[i];
        if (string.IsNullOrWhiteSpace(itemIdentifier))
          continue;

        var item = Registry.Registry.CreateItemInstance(itemIdentifier);
        if (item == null)
        {
          Debug.LogWarning($"[PlayerController] Failed to create static pickup item '{itemIdentifier}'.");
          ReportStaticPickupFailure(entityIdentifier);
          return;
        }

        item.CurrentStackCount = Mathf.Max(1, amounts[i]);
        items.Add(item);
      }

      if (items.Count == 0)
      {
        ReportStaticPickupFailure(entityIdentifier);
        return;
      }

      // 월드아이템과 동일한 all-or-nothing 규약: 목록의 모든 아이템을 전량 수용 가능할 때만 추가한다.
      // 하나라도 수용 불가하면 아무것도 추가하지 않는다.
      if (!CanAcceptAllItems(items))
      {
        Debug.LogWarning(
          $"[PlayerController] Static pickup confirmation for '{entityIdentifier}', but inventory is full.");
        ReportStaticPickupFailure(entityIdentifier);
        return;
      }

      for (int i = 0; i < items.Count; i++)
      {
        // CanAcceptAllItems 로 전량 수용을 이미 검증했으므로 개별 추가는 성공해야 한다.
        // OnGet 은 TryAddItemToInventory 내부에서 아이템별로 1회 호출된다.
        if (!TryAddItemToInventory(items[i]))
        {
          Debug.LogWarning(
            $"[PlayerController] Static pickup partial-add failure for '{entityIdentifier}' item index {i}.");
          ReportStaticPickupFailure(entityIdentifier);
          return;
        }
      }

      AcknowledgeStaticPickupSuccess(entityIdentifier);
    }

    /// <summary>
    /// 여러 아이템을 인벤토리에 모두 수용할 수 있는지 판정합니다(로컬 클라이언트 전용).
    /// 스택 병합/부분 수용을 정확히 반영하기 위해 슬롯 상태를 복제하여 순차 시뮬레이션합니다.
    /// </summary>
    private bool CanAcceptAllItems(List<Item> items)
    {
      if (items == null || items.Count == 0)
        return false;

      // 단일 아이템이면 기존 경로를 그대로 사용(추가 복제/시뮬레이션 비용 없음).
      if (items.Count == 1)
        return CanAcceptItem(items[0]);

      return CanAcceptAllItemsSimulated(items);
    }

    /// <summary>시뮬레이션용 슬롯 상태(실제 인벤토리를 변경하지 않는 가상 슬롯).</summary>
    private sealed class SimSlot
    {
      public Item Prototype;     // 스택 판정(CanStackWith)용 프로토타입. 빈 슬롯은 채워질 때 설정된다.
      public bool IsEmptyState;  // 현재 비어 있는지(원본 IsEmpty 또는 시뮬 중 아직 채워지지 않음).
      public int CurrentCount;
      public int MaxCount;
    }

    /// <summary>
    /// 슬롯 상태를 복제해 아이템들을 순차로 가상 배치하며 전량 수용 가능 여부를 판정합니다.
    /// <see cref="TryAddItemToInventory"/> 와 동일한 규칙(먼저 스택 병합, 그다음 빈 슬롯 배치)을 따릅니다.
    /// </summary>
    private bool CanAcceptAllItemsSimulated(List<Item> items)
    {
      var sim = new List<SimSlot>(_slots.Count);
      foreach (var slot in _slots)
      {
        if (slot == null)
        {
          sim.Add(new SimSlot { Prototype = null, IsEmptyState = true, CurrentCount = 0, MaxCount = 0 });
          continue;
        }

        bool empty = slot.IsEmpty || slot.ItemInstance == null;
        sim.Add(new SimSlot
        {
          Prototype = empty ? null : slot.ItemInstance,
          IsEmptyState = empty,
          CurrentCount = empty ? 0 : slot.ItemInstance.CurrentStackCount,
          MaxCount = empty ? 0 : slot.ItemInstance.CurrentMaxStackCount,
        });
      }

      foreach (var item in items)
      {
        if (item == null || item.CurrentStackCount <= 0)
          continue;

        if (!SimulatePlaceItem(sim, item))
          return false;
      }

      return true;
    }

    /// <summary>하나의 아이템을 시뮬레이션 슬롯들에 배치 시도합니다. 전량 배치 성공 시 true.</summary>
    private static bool SimulatePlaceItem(List<SimSlot> sim, Item item)
    {
      int remaining = item.CurrentStackCount;

      // 1) 스택 병합 가능한 기존(또는 이미 채워진) 슬롯에 먼저 채운다.
      for (int i = 0; i < sim.Count && remaining > 0; i++)
      {
        var s = sim[i];
        if (s.IsEmptyState || s.Prototype == null)
          continue;
        if (!s.Prototype.CanStackWith(item))
          continue;

        int room = s.MaxCount - s.CurrentCount;
        if (room <= 0)
          continue;

        int moved = Mathf.Min(room, remaining);
        s.CurrentCount += moved;
        remaining -= moved;
      }

      // 2) 남으면 빈 슬롯에 배치한다.
      for (int i = 0; i < sim.Count && remaining > 0; i++)
      {
        var s = sim[i];
        if (!s.IsEmptyState)
          continue;

        int max = item.CurrentMaxStackCount;
        int moved = Mathf.Min(max, remaining);

        s.IsEmptyState = false;
        s.Prototype = item;
        s.MaxCount = max;
        s.CurrentCount = moved;
        remaining -= moved;
      }

      return remaining <= 0;
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

      // NearbyInteractablesDetector는 감지 대상 목록의 "구성원 변화"에만 반응하므로,
      // VanishBehavior가 DisableInteraction/Invisible(콜라이더 유지)인 경우 이 오브젝트는 계속 감지 목록에
      // 남아 다음 구성원 변화(범위 재진입 등)가 있기 전까지 상호작용 힌트 UI가 갱신되지 않는다.
      // 사라짐 적용 직후 즉시 갱신하여, 다음 감지 주기나 다른 인벤토리 변화가 있을 때까지 기다리지 않게 한다.
      RefreshInteractableHintsNow();
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
