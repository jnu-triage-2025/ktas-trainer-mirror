using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// <see cref="StaticObjectDisplayment"/> (에디터 사전 배치되어 있고 다른 흐름에 의해 표시/비표시될 수 있는
  /// 정적 오브젝트)의 "표시(설치/적용) 확정 및 전역 동기화"를 서버 권위로 처리합니다.
  ///
  /// <para>
  /// <see cref="StaticObjectDisplayment"/> 는 네트워크 오브젝트가 아니므로 자체 RPC 를 가질 수 없습니다.
  /// 따라서 <see cref="StaticPlacedItem"/> 픽업과 동일하게, NetworkBehaviour 인 PlayerController 를 통해
  /// 서버 권위 프로토콜을 태웁니다. 표시 상태의 진실 원천은 서버의 <see cref="StaticObjectDisplaymentService"/> 입니다.
  /// </para>
  ///
  /// 흐름(픽업의 예약/확정 구조와 동일):
  /// 1. (Owner) <see cref="TryApplyStaticObjectDisplayment"/> → 서버로 요청.
  /// 2. (Server) 거리/중복/서버 인벤토리를 검증하고 요구 아이템을 차감합니다.
  /// 3. (Server) 표시 상태를 확정하고 모든 클라이언트에 표시(Show)를 브로드캐스트합니다.
  /// 4. (Claimant) TargetRpc 로 서버의 소비 결과를 로컬 인벤토리에 반영합니다.
  /// </summary>
  public partial class PlayerController
  {
    private const float MaxStaticObjectDisplaymentApplyDistance = 4f;
    private const float MaxStaticObjectDisplaymentApplyDistanceSqr =
      MaxStaticObjectDisplaymentApplyDistance * MaxStaticObjectDisplaymentApplyDistance;

    /// <summary>
    /// 서버 전용: 승인되었으나 아직 확정(성공/실패)되지 않은 표시 적용 예약(entityIdentifier → 요청자).
    /// PlayerController 인스턴스 간 공유(정적 아이템 <c>_pendingStaticPickups</c> 와 동일한 static 설계).
    /// </summary>
    private static readonly Dictionary<string, int> _pendingStaticObjectApplies = new(StringComparer.Ordinal);

    /// <summary>
    /// Owner 로컬 전용: 확정을 기다리는 중인 요청의 만료 시각(entityIdentifier → 만료 <see cref="Time.unscaledTime"/>).
    /// 연타로 인한 중복 요청을 방지하되, 승인이 조용히 거부되어도 쿨다운 후 재시도가 가능하도록 시간 기반으로 관리한다.
    /// </summary>
    private readonly Dictionary<string, float> _inFlightStaticObjectApplyExpiry = new(StringComparer.Ordinal);

    private const float StaticObjectApplyInFlightTimeout = 2f;

    /// <summary>
    /// 서버 전역 표시 상태를 해제하고 요청자에게 아이템 하나를 지급합니다.
    /// 맵에 사전 배치된 설치물을 회수할 때 사용합니다.
    /// </summary>
    public void TryClearStaticObjectDisplaymentAndGrantItem(string entityIdentifier, string itemIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(itemIdentifier))
        return;
      if (!IsSpawned || IsServerStarted)
        ServerClearStaticObjectDisplaymentAndGrantItem(entityIdentifier, itemIdentifier, Owner);
      else
        CmdClearStaticObjectDisplaymentAndGrantItem(entityIdentifier, itemIdentifier);
    }

    [ServerRpc]
    private void CmdClearStaticObjectDisplaymentAndGrantItem(
      string entityIdentifier, string itemIdentifier, NetworkConnection sender = null)
    {
      ServerClearStaticObjectDisplaymentAndGrantItem(entityIdentifier, itemIdentifier, sender ?? Owner);
    }

    private void ServerClearStaticObjectDisplaymentAndGrantItem(
      string entityIdentifier, string itemIdentifier, NetworkConnection claimant)
    {
      if (claimant == null || (Owner != null && Owner.IsValid && claimant.ClientId != Owner.ClientId))
        return;
      if (!TryGetStaticObjectDisplayment(entityIdentifier, out var displayment) || displayment == null ||
          !StaticObjectDisplaymentService.IsShown(entityIdentifier))
        return;
      if (!displayment.TryGetServerSharedItemExchange(out string authoritativeItemIdentifier, out _)
          || !string.Equals(itemIdentifier, authoritativeItemIdentifier, StringComparison.Ordinal))
        return;
      float sqrDistance = (displayment.transform.position - ResolveServerPickupOriginPosition()).sqrMagnitude;
      if (sqrDistance > MaxStaticObjectDisplaymentApplyDistanceSqr)
        return;
      if (!StaticObjectDisplaymentService.ClearShown(entityIdentifier))
        return;

      displayment.OnHiddenConfirmed();
      RpcHideStaticObjectDisplaymentGlobal(entityIdentifier);
      TargetGrantStaticObjectDisplaymentItem(claimant, authoritativeItemIdentifier);
    }

    [TargetRpc]
    private void TargetGrantStaticObjectDisplaymentItem(NetworkConnection connection, string itemIdentifier)
    {
      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
        return;
      if (!TryAddItemToInventory(item))
        TryDropItemInFront(item);
    }

    // ── 진입점 (Owner) ─────────────────────────────────────────────────────

    /// <summary>
    /// 정적 오브젝트의 표시(설치/적용)를 시도합니다. 요구 아이템 식별자/수량을 함께 전달하며,
    /// 서버 승인 후 요청자 클라이언트에서 해당 아이템을 인벤토리에서 소비합니다.
    /// 아이템 소비가 필요 없으면 <paramref name="requiredItemIdentifier"/> 를 비우면 됩니다.
    /// </summary>
    public void TryApplyStaticObjectDisplayment(string entityIdentifier, string requiredItemIdentifier = null, int consumeCount = 0)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      entityIdentifier = entityIdentifier.Trim();

      if (!TryGetStaticObjectDisplayment(entityIdentifier, out var displayment) || displayment == null)
      {
        Debug.LogWarning($"[PlayerController] TryApplyStaticObjectDisplayment failed: '{entityIdentifier}' not found.");
        return;
      }

      // 로컬 in-flight 가드(연타 방지). 확정 시 즉시 해제되고, 확정이 오지 않아도 쿨다운 만료 후 재시도 가능.
      if (_inFlightStaticObjectApplyExpiry.TryGetValue(entityIdentifier, out var expiry)
          && Time.unscaledTime < expiry)
        return;

      _inFlightStaticObjectApplyExpiry[entityIdentifier] = Time.unscaledTime + StaticObjectApplyInFlightTimeout;

      RequestApplyStaticObjectDisplayment(entityIdentifier, requiredItemIdentifier ?? string.Empty, Mathf.Max(0, consumeCount));
    }

    private void RequestApplyStaticObjectDisplayment(string entityIdentifier, string requiredItemIdentifier, int consumeCount)
    {
      // 네트워크가 없는 단독 실행(에디터 단독 플레이) 또는 서버 호스트면 서버 로직을 직접 실행.
      if (!IsSpawned || IsServerStarted)
      {
        ServerApproveApplyStaticObjectDisplayment(entityIdentifier, requiredItemIdentifier, consumeCount, Owner);
        return;
      }

      CmdRequestApplyStaticObjectDisplayment(entityIdentifier, requiredItemIdentifier, consumeCount);
    }

    [ServerRpc]
    private void CmdRequestApplyStaticObjectDisplayment(
      string entityIdentifier, string requiredItemIdentifier, int consumeCount, NetworkConnection sender = null)
    {
      ServerApproveApplyStaticObjectDisplayment(entityIdentifier, requiredItemIdentifier, consumeCount, sender ?? Owner);
    }

    // ── 서버 승인 (거리/중복 검증 + 예약) ─────────────────────────────────────

    private void ServerApproveApplyStaticObjectDisplayment(
      string entityIdentifier, string requiredItemIdentifier, int consumeCount, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      if (Owner != null && Owner.IsValid && claimant.ClientId != Owner.ClientId)
        return;

      if (!TryGetStaticObjectDisplayment(entityIdentifier, out var displayment) || displayment == null)
        return;
      if (!displayment.TryGetServerSharedItemExchange(
            out string authoritativeItemIdentifier, out int authoritativeConsumeCount)
          || !string.Equals(requiredItemIdentifier, authoritativeItemIdentifier, StringComparison.Ordinal)
          || consumeCount != authoritativeConsumeCount)
        return;

      // 이미 표시(설치/적용)된 상태면 무시(멱등, 중복 소비 방지).
      if (StaticObjectDisplaymentService.IsShown(entityIdentifier))
        return;

      // 동일 오브젝트에 대한 미확정 예약이 이미 있으면 거부(단일 사용 보장 · 동시 승인 방지).
      if (_pendingStaticObjectApplies.ContainsKey(entityIdentifier))
        return;

      // 거리 검증(픽업과 동일 규약).
      var claimantPosition = ResolveServerPickupOriginPosition();
      // Wall-mounted displayments (for example the suction unit) are above a
      // grounded player's origin.  Interaction discovery is planar, so using
      // a 3D server distance made an offered wall interaction impossible to
      // approve solely because of its mounting height.
      Vector3 offset = displayment.transform.position - claimantPosition;
      float sqrDistance = offset.x * offset.x + offset.z * offset.z;
      if (sqrDistance > MaxStaticObjectDisplaymentApplyDistanceSqr)
      {
        Debug.LogWarning(
          $"[PlayerController] Reject static object apply '{entityIdentifier}': too far. " +
          $"distance={Mathf.Sqrt(sqrDistance):0.00}m, limit={MaxStaticObjectDisplaymentApplyDistance:0.00}m");
        return;
      }

      // 공유 상태를 클라이언트의 성공 보고만으로 바꾸지 않는다. 서버 인스턴스가 실제 수량을
      // 확인하고 차감할 수 있을 때만 설치를 확정한다. 서버에 인벤토리 근거가 없는 원격
      // 클라이언트 요청은 실패 안전 방식으로 거부된다.
      if (!string.IsNullOrWhiteSpace(authoritativeItemIdentifier) && authoritativeConsumeCount > 0)
      {
        if (CountItemInInventory(authoritativeItemIdentifier) < authoritativeConsumeCount)
          return;
        if (RemoveItemFromInventory(authoritativeItemIdentifier, authoritativeConsumeCount)
            != authoritativeConsumeCount)
          return;
      }

      _pendingStaticObjectApplies[entityIdentifier] = claimant.ClientId;

      TargetConfirmApplyStaticObjectDisplayment(
        claimant, entityIdentifier, authoritativeItemIdentifier, authoritativeConsumeCount);
      ServerConfirmStaticObjectApplySuccess(entityIdentifier, claimant);
    }

    // ── 요청자 확정: 아이템 소비 후 성공/실패 보고 ────────────────────────────

    [TargetRpc]
    private void TargetConfirmApplyStaticObjectDisplayment(
      NetworkConnection conn, string entityIdentifier, string requiredItemIdentifier, int consumeCount)
    {
      ClearInFlightStaticObjectApply(entityIdentifier);

      // 호스트는 위 서버 처리와 같은 PlayerController 인스턴스를 사용하므로 다시 차감하지 않는다.
      // 원격 소유자는 서버가 이미 확정한 소비 결과를 로컬 표현에 반영한다.
      if (!IsServerStarted && !string.IsNullOrWhiteSpace(requiredItemIdentifier) && consumeCount > 0)
      {
        int removed = RemoveItemFromInventory(requiredItemIdentifier, consumeCount);
        if (removed < consumeCount)
        {
          Debug.LogError(
            $"[PlayerController] Static object apply '{entityIdentifier}': local inventory diverged from server ({removed}/{consumeCount}).");
        }
      }
    }

    private void AcknowledgeStaticObjectApplySuccess(string entityIdentifier)
    {
      ClearInFlightStaticObjectApply(entityIdentifier);

      if (!IsSpawned || IsServerStarted)
        ServerConfirmStaticObjectApplySuccess(entityIdentifier, Owner);
      else
        CmdAcknowledgeApplyStaticObjectDisplaymentSuccess(entityIdentifier);
    }

    private void ReportStaticObjectApplyFailure(string entityIdentifier)
    {
      ClearInFlightStaticObjectApply(entityIdentifier);

      if (!IsSpawned || IsServerStarted)
        ServerCancelStaticObjectApply(entityIdentifier, Owner);
      else
        CmdReportApplyStaticObjectDisplaymentFailure(entityIdentifier);
    }

    private void ClearInFlightStaticObjectApply(string entityIdentifier)
    {
      if (!string.IsNullOrWhiteSpace(entityIdentifier))
        _inFlightStaticObjectApplyExpiry.Remove(entityIdentifier);
    }

    [ServerRpc]
    private void CmdAcknowledgeApplyStaticObjectDisplaymentSuccess(string entityIdentifier, NetworkConnection sender = null)
    {
      ServerConfirmStaticObjectApplySuccess(entityIdentifier, sender ?? Owner);
    }

    [ServerRpc]
    private void CmdReportApplyStaticObjectDisplaymentFailure(string entityIdentifier, NetworkConnection sender = null)
    {
      ServerCancelStaticObjectApply(entityIdentifier, sender ?? Owner);
    }

    // ── 서버 성공 확정: 상태 확정 + 표시 브로드캐스트 ─────────────────────────

    private void ServerConfirmStaticObjectApplySuccess(string entityIdentifier, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      // 예약이 존재하고, 그 예약이 이 요청자의 것일 때만 진행(무단/재전송 ack 차단).
      if (!_pendingStaticObjectApplies.TryGetValue(entityIdentifier, out var claimantClientId))
        return;

      if (claimantClientId != claimant.ClientId)
        return;

      // 예약 소비(단일 사용).
      _pendingStaticObjectApplies.Remove(entityIdentifier);

      // 서버 권위 상태 확정(멱등). 이번 호출로 새로 확정되었으면 서버 확정 훅을 1회 호출한다.
      bool newlyShown = StaticObjectDisplaymentService.SetShown(entityIdentifier);
      if (newlyShown
          && TryGetStaticObjectDisplayment(entityIdentifier, out var displayment)
          && displayment != null)
      {
        // "최초 확정 시 정확히 한 번" 부수효과(예: 시나리오 신호)를 서버 컨텍스트에서 처리한다.
        using (ScenarioSignalPlayerContext.Push(claimant))
          displayment.OnShownConfirmed();
      }

      // 모든 클라이언트에 표시(설치/적용)를 브로드캐스트.
      RpcApplyStaticObjectDisplaymentGlobal(entityIdentifier);
    }

    /// <summary>
    /// 시나리오 준비 단계에서 정적 장비를 서버 권위로 설치하고 모든 관찰자에게 표시한다.
    /// 아이템 소비·소유자 검증이 필요한 플레이어 상호작용 경로와 달리, 이미 시나리오가 보장한
    /// 상태를 복원하는 용도로만 사용한다.
    /// </summary>
    public bool ApplyStaticObjectDisplaymentForScenario(string entityIdentifier)
    {
      if (!IsServerStarted || string.IsNullOrWhiteSpace(entityIdentifier))
        return false;

      bool newlyShown = StaticObjectDisplaymentService.SetShown(entityIdentifier);
      if (newlyShown
          && TryGetStaticObjectDisplayment(entityIdentifier, out var displayment)
          && displayment != null)
      {
        displayment.OnShownConfirmed();
      }

      // 이미 서버 상태에 있던 장비도 이 준비 진입을 계기로 현재 관찰자에게 다시 표시한다.
      RpcApplyStaticObjectDisplaymentGlobal(entityIdentifier);
      return true;
    }

    // ── 서버 실패/중단: 예약 제거(상태 변이 없음) ─────────────────────────────

    private void ServerCancelStaticObjectApply(string entityIdentifier, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      if (!_pendingStaticObjectApplies.TryGetValue(entityIdentifier, out var claimantClientId))
        return;

      if (claimantClientId != claimant.ClientId)
        return;

      _pendingStaticObjectApplies.Remove(entityIdentifier);
    }

    /// <summary>
    /// 이 클라이언트가 점유했지만 확정하지 못한 표시 적용 예약을 제거한다(서버 전용).
    /// 접속 종료 시 <see cref="OnStopServer"/> 에서 호출된다.
    /// </summary>
    private void CancelPendingStaticObjectAppliesForClaimant()
    {
      if (Owner == null || !Owner.IsValid)
        return;

      int claimantId = Owner.ClientId;
      var toRemove = new List<string>();
      foreach (var kvp in _pendingStaticObjectApplies)
      {
        if (kvp.Value == claimantId)
          toRemove.Add(kvp.Key);
      }

      foreach (var entityIdentifier in toRemove)
        _pendingStaticObjectApplies.Remove(entityIdentifier);
    }

    // ── 표시 적용 (로컬 표현) ─────────────────────────────────────────────────

    // 신규 접속자 동기화는 SyncStaticObjectDisplaymentsToConnection(TargetRpc) 로 별도 처리하므로
    // BufferLast 는 사용하지 않는다(엔티티별 구분이 안 되어 마지막 1건만 버퍼되기 때문).
    [ObserversRpc]
    private void RpcApplyStaticObjectDisplaymentGlobal(string entityIdentifier)
    {
      ApplyStaticObjectDisplaymentLocal(entityIdentifier);
    }

    [TargetRpc]
    private void TargetApplyStaticObjectDisplaymentLocal(NetworkConnection conn, string entityIdentifier)
    {
      ApplyStaticObjectDisplaymentLocal(entityIdentifier);
    }

    private void ApplyStaticObjectDisplaymentLocal(string entityIdentifier)
    {
      if (TryGetStaticObjectDisplayment(entityIdentifier, out var displayment) && displayment != null)
        displayment.ApplyShownFromNetwork();

      // 표시 상태 변화로 상호작용 노출 조건이 달라질 수 있으므로 힌트를 즉시 갱신한다.
      RefreshInteractableHintsNow();
    }

    [ObserversRpc]
    private void RpcHideStaticObjectDisplaymentGlobal(string entityIdentifier)
    {
      if (TryGetStaticObjectDisplayment(entityIdentifier, out var displayment) && displayment != null)
        displayment.ApplyHiddenFromNetwork();
      RefreshInteractableHintsNow();
    }

    // ── 신규 접속자 동기화 (서버에서 호출) ───────────────────────────────────

    /// <summary>
    /// 새로 접속한 연결에 대해, 이미 표시(설치/적용)된 정적 오브젝트들을 그 연결에서만 표시 상태로 맞춰줍니다(서버 전용).
    /// </summary>
    private void SyncStaticObjectDisplaymentsToConnection(NetworkConnection connection)
    {
      if (connection == null || !IsServerStarted)
        return;

      foreach (var entityIdentifier in StaticObjectDisplaymentService.GetAllShown())
      {
        if (string.IsNullOrWhiteSpace(entityIdentifier))
          continue;

        TargetApplyStaticObjectDisplaymentLocal(connection, entityIdentifier);
      }
    }

    // ── 헬퍼 ─────────────────────────────────────────────────────────────────

    private static bool TryGetStaticObjectDisplayment(string entityIdentifier, out StaticObjectDisplayment displayment)
    {
      displayment = null;
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return false;

      if (!Registry.Registry.TryGetEntity(entityIdentifier, out var descriptor) || descriptor == null)
        return false;

      var go = descriptor.GameObject;
      if (go == null)
        return false;

      return go.TryGetComponent(out displayment) && displayment != null;
    }
  }
}
