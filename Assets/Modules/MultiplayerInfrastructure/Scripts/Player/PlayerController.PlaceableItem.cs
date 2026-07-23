using System;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const string Level1RapidInfuserItemIdentifier = "level1_rapid_infuser";
    private const float PlaceableDistance = 1.75f;
    private const float PlaceableRequestTimeout = 3f;

    private string _pendingPlaceableItemIdentifier;
    private string _pendingPlaceablePresetIdentifier;
    private int _pendingPlaceableClaimantClientId = -1;
    private float _pendingPlaceableExpiresAt;
    private float _localPlaceableRequestExpiresAt;

    /// <summary>
    /// 소유 클라이언트의 로컬 인벤토리와 서버의 월드 스폰을 예약/확정 프로토콜로 연결한다.
    /// 인벤토리는 서버 PlayerController 복제본에 존재하지 않으므로 서버에서 직접 소비하지 않는다.
    /// </summary>
    public void RequestPlaceHeldEntityPreset(string itemIdentifier, string presetIdentifier)
    {
      if (!IsSupportedPlaceable(itemIdentifier, presetIdentifier))
        return;
      if (CountItemInInventory(itemIdentifier) < 1)
        return;
      if (Time.unscaledTime < _localPlaceableRequestExpiresAt)
        return;

      _localPlaceableRequestExpiresAt = Time.unscaledTime + PlaceableRequestTimeout;

      if (!IsSpawned)
      {
        ConsumeAndPlaceOffline(itemIdentifier, presetIdentifier);
        return;
      }

      if (IsServerStarted)
      {
        // 호스트는 서버와 소유 클라이언트 인벤토리가 같은 인스턴스다.
        ConsumeAndPlaceOffline(itemIdentifier, presetIdentifier);
        return;
      }

      CmdRequestPlaceHeldEntityPreset(itemIdentifier, presetIdentifier);
    }

    [ServerRpc]
    private void CmdRequestPlaceHeldEntityPreset(
      string itemIdentifier,
      string presetIdentifier,
      NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid ||
          Owner == null || !Owner.IsValid || sender.ClientId != Owner.ClientId ||
          !IsSupportedPlaceable(itemIdentifier, presetIdentifier))
        return;

      if (!string.IsNullOrEmpty(_pendingPlaceableItemIdentifier) &&
          Time.unscaledTime < _pendingPlaceableExpiresAt)
        return;

      _pendingPlaceableItemIdentifier = itemIdentifier;
      _pendingPlaceablePresetIdentifier = presetIdentifier;
      _pendingPlaceableClaimantClientId = sender.ClientId;
      _pendingPlaceableExpiresAt = Time.unscaledTime + PlaceableRequestTimeout;

      TargetConfirmPlaceableItemConsumption(sender, itemIdentifier, presetIdentifier);
    }

    [TargetRpc]
    private void TargetConfirmPlaceableItemConsumption(
      NetworkConnection connection,
      string itemIdentifier,
      string presetIdentifier)
    {
      _localPlaceableRequestExpiresAt = 0f;

      if (!IsSupportedPlaceable(itemIdentifier, presetIdentifier) ||
          CountItemInInventory(itemIdentifier) < 1 ||
          RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        CmdReportPlaceableItemConsumptionFailure(itemIdentifier, presetIdentifier);
        return;
      }

      CmdAcknowledgePlaceableItemConsumption(itemIdentifier, presetIdentifier);
    }

    [ServerRpc]
    private void CmdAcknowledgePlaceableItemConsumption(
      string itemIdentifier,
      string presetIdentifier,
      NetworkConnection sender = null)
    {
      if (!MatchesPendingPlaceable(itemIdentifier, presetIdentifier, sender))
        return;

      ClearPendingPlaceable();
      if (!TrySpawnPlaceablePreset(presetIdentifier))
        TargetRefundPlaceableItem(sender, itemIdentifier);
    }

    [ServerRpc]
    private void CmdReportPlaceableItemConsumptionFailure(
      string itemIdentifier,
      string presetIdentifier,
      NetworkConnection sender = null)
    {
      if (MatchesPendingPlaceable(itemIdentifier, presetIdentifier, sender))
        ClearPendingPlaceable();
    }

    [TargetRpc]
    private void TargetRefundPlaceableItem(NetworkConnection connection, string itemIdentifier)
    {
      var refund = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (refund == null || TryAddItemToInventory(refund))
        return;

      // 스폰 실패 보상까지 인벤토리에 들어갈 수 없다면 아이템을 월드에 남겨 유실을 막는다.
      TryDropItemInFront(refund);
    }

    private void ConsumeAndPlaceOffline(string itemIdentifier, string presetIdentifier)
    {
      _localPlaceableRequestExpiresAt = 0f;
      if (RemoveItemFromInventory(itemIdentifier, 1) != 1)
        return;

      if (TrySpawnPlaceablePreset(presetIdentifier))
        return;

      var refund = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (refund != null && !TryAddItemToInventory(refund))
        TryDropItemInFront(refund);
    }

    private bool TrySpawnPlaceablePreset(string presetIdentifier)
    {
      Vector3 forward = transform.forward;
      forward.y = 0f;
      if (forward.sqrMagnitude < 0.001f)
        forward = Vector3.forward;
      forward.Normalize();

      Vector3 position = transform.position + forward * PlaceableDistance;
      Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
      if (Registry.Registry.TrySpawnEntityPreset(
            presetIdentifier, position, rotation, out _, out _, out string error))
        return true;

      Debug.LogWarning($"[PlayerController] Placeable '{presetIdentifier}' spawn failed: {error}", this);
      return false;
    }

    private bool MatchesPendingPlaceable(
      string itemIdentifier,
      string presetIdentifier,
      NetworkConnection sender)
    {
      return sender != null && sender.IsValid &&
             sender.ClientId == _pendingPlaceableClaimantClientId &&
             Time.unscaledTime <= _pendingPlaceableExpiresAt &&
             string.Equals(itemIdentifier, _pendingPlaceableItemIdentifier, StringComparison.Ordinal) &&
             string.Equals(presetIdentifier, _pendingPlaceablePresetIdentifier, StringComparison.Ordinal);
    }

    private void ClearPendingPlaceable()
    {
      _pendingPlaceableItemIdentifier = null;
      _pendingPlaceablePresetIdentifier = null;
      _pendingPlaceableClaimantClientId = -1;
      _pendingPlaceableExpiresAt = 0f;
    }

    private static bool IsSupportedPlaceable(string itemIdentifier, string presetIdentifier)
    {
      return string.Equals(itemIdentifier, Level1RapidInfuserItemIdentifier, StringComparison.Ordinal) &&
             string.Equals(itemIdentifier, presetIdentifier, StringComparison.Ordinal);
    }
  }
}
