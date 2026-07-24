using System;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const string Level1RapidInfuserItemIdentifier = "level1_rapid_infuser";
    private const string Level1RapidInfuserResourcePath = "Models/Entities/level1_rapid_infuser";
    private const float PlaceableDistance = 1.75f;
    private const float PlaceableRequestTimeout = 3f;

    private string _pendingPlaceableItemIdentifier;
    private string _pendingPlaceableResourcePath;
    private int _pendingPlaceableClaimantClientId = -1;
    private float _pendingPlaceableExpiresAt;
    private float _localPlaceableRequestExpiresAt;

    /// <summary>
    /// 소유 클라이언트의 로컬 인벤토리와 서버의 월드 스폰을 예약/확정 프로토콜로 연결한다.
    /// 인벤토리는 서버 PlayerController 복제본에 존재하지 않으므로 서버에서 직접 소비하지 않는다.
    /// </summary>
    public void RequestPlaceHeldEntityResource(string itemIdentifier, string resourcePath)
    {
      if (!IsSupportedPlaceable(itemIdentifier, resourcePath))
        return;
      if (CountItemInInventory(itemIdentifier) < 1)
        return;
      if (Time.unscaledTime < _localPlaceableRequestExpiresAt)
        return;

      _localPlaceableRequestExpiresAt = Time.unscaledTime + PlaceableRequestTimeout;

      if (!IsSpawned)
      {
        ConsumeAndPlaceOffline(itemIdentifier, resourcePath);
        return;
      }

      if (IsServerStarted)
      {
        // 호스트는 서버와 소유 클라이언트 인벤토리가 같은 인스턴스다.
        ConsumeAndPlaceOffline(itemIdentifier, resourcePath);
        return;
      }

      CmdRequestPlaceHeldEntityResource(itemIdentifier, resourcePath);
    }

    [ServerRpc]
    private void CmdRequestPlaceHeldEntityResource(
      string itemIdentifier,
      string resourcePath,
      NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid ||
          Owner == null || !Owner.IsValid || sender.ClientId != Owner.ClientId ||
          !IsSupportedPlaceable(itemIdentifier, resourcePath))
        return;

      if (!string.IsNullOrEmpty(_pendingPlaceableItemIdentifier) &&
          Time.unscaledTime < _pendingPlaceableExpiresAt)
        return;

      _pendingPlaceableItemIdentifier = itemIdentifier;
      _pendingPlaceableResourcePath = resourcePath;
      _pendingPlaceableClaimantClientId = sender.ClientId;
      _pendingPlaceableExpiresAt = Time.unscaledTime + PlaceableRequestTimeout;

      TargetConfirmPlaceableItemConsumption(sender, itemIdentifier, resourcePath);
    }

    [TargetRpc]
    private void TargetConfirmPlaceableItemConsumption(
      NetworkConnection connection,
      string itemIdentifier,
      string resourcePath)
    {
      _localPlaceableRequestExpiresAt = 0f;

      if (!IsSupportedPlaceable(itemIdentifier, resourcePath) ||
          CountItemInInventory(itemIdentifier) < 1 ||
          RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        CmdReportPlaceableItemConsumptionFailure(itemIdentifier, resourcePath);
        return;
      }

      CmdAcknowledgePlaceableItemConsumption(itemIdentifier, resourcePath);
    }

    [ServerRpc]
    private void CmdAcknowledgePlaceableItemConsumption(
      string itemIdentifier,
      string resourcePath,
      NetworkConnection sender = null)
    {
      if (!MatchesPendingPlaceable(itemIdentifier, resourcePath, sender))
        return;

      ClearPendingPlaceable();
      if (!TrySpawnPlaceableResource(itemIdentifier, resourcePath))
        TargetRefundPlaceableItem(sender, itemIdentifier);
    }

    [ServerRpc]
    private void CmdReportPlaceableItemConsumptionFailure(
      string itemIdentifier,
      string resourcePath,
      NetworkConnection sender = null)
    {
      if (MatchesPendingPlaceable(itemIdentifier, resourcePath, sender))
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

    private void ConsumeAndPlaceOffline(string itemIdentifier, string resourcePath)
    {
      _localPlaceableRequestExpiresAt = 0f;
      if (RemoveItemFromInventory(itemIdentifier, 1) != 1)
        return;

      if (TrySpawnPlaceableResource(itemIdentifier, resourcePath))
        return;

      var refund = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (refund != null && !TryAddItemToInventory(refund))
        TryDropItemInFront(refund);
    }

    private bool TrySpawnPlaceableResource(string itemIdentifier, string resourcePath)
    {
      Vector3 forward = transform.forward;
      forward.y = 0f;
      if (forward.sqrMagnitude < 0.001f)
        forward = Vector3.forward;
      forward.Normalize();

      Vector3 position = transform.position + forward * PlaceableDistance;
      Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);
      var prefab = Resources.Load<GameObject>(resourcePath);
      if (prefab == null)
      {
        Debug.LogWarning(
          $"[PlayerController] Placeable '{itemIdentifier}' resource was not found at " +
          $"'Resources/{resourcePath}.prefab'.",
          this);
        return false;
      }

      var prefabNetworkObject = prefab.GetComponent<NetworkObject>();
      var identifierReceiver = prefab.GetComponent<ISpawnedEntityIdentifierReceiver>();
      if (identifierReceiver == null)
      {
        Debug.LogWarning(
          $"[PlayerController] Placeable resource '{resourcePath}' must contain an " +
          $"{nameof(ISpawnedEntityIdentifierReceiver)} component.",
          prefab);
        return false;
      }

      if (IsServerStarted && prefabNetworkObject == null)
      {
        Debug.LogWarning(
          $"[PlayerController] Networked placeable resource '{resourcePath}' " +
          $"must contain a spawnable {nameof(NetworkObject)}.",
          prefab);
        return false;
      }

      var spawned = UnityEngine.Object.Instantiate(prefab, position, rotation);
      if (spawned == null)
        return false;

      string runtimeIdentifier = $"{itemIdentifier}:{Guid.NewGuid():N}";
      spawned.GetComponent<ISpawnedEntityIdentifierReceiver>()
        .ApplySpawnedEntityIdentifier(runtimeIdentifier);

      if (IsServerStarted)
      {
        var networkObject = spawned.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
          UnityEngine.Object.Destroy(spawned);
          return false;
        }

        InstanceFinder.ServerManager.Spawn(spawned);
      }

      return true;
    }

    private bool MatchesPendingPlaceable(
      string itemIdentifier,
      string resourcePath,
      NetworkConnection sender)
    {
      return sender != null && sender.IsValid &&
             sender.ClientId == _pendingPlaceableClaimantClientId &&
             Time.unscaledTime <= _pendingPlaceableExpiresAt &&
             string.Equals(itemIdentifier, _pendingPlaceableItemIdentifier, StringComparison.Ordinal) &&
             string.Equals(resourcePath, _pendingPlaceableResourcePath, StringComparison.Ordinal);
    }

    private void ClearPendingPlaceable()
    {
      _pendingPlaceableItemIdentifier = null;
      _pendingPlaceableResourcePath = null;
      _pendingPlaceableClaimantClientId = -1;
      _pendingPlaceableExpiresAt = 0f;
    }

    private static bool IsSupportedPlaceable(string itemIdentifier, string resourcePath)
    {
      return string.Equals(itemIdentifier, Level1RapidInfuserItemIdentifier, StringComparison.Ordinal) &&
             string.Equals(resourcePath, Level1RapidInfuserResourcePath, StringComparison.Ordinal);
    }
  }
}
