using System;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
using UnityEngine;
using UnityEngine.Rendering;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    private const string Level1RapidInfuserItemIdentifier = "level1_rapid_infuser";
    private const string Level1RapidInfuserEntityPresetIdentifier = "level1_rapid_infuser";
    private const float PlaceableDistance = 1.75f;
    private const float PlaceableRequestTimeout = 3f;

    private string _pendingPlaceableItemIdentifier;
    private string _pendingPlaceableEntityPresetIdentifier;
    private int _pendingPlaceableClaimantClientId = -1;
    private float _pendingPlaceableExpiresAt;
    private float _localPlaceableRequestExpiresAt;
    private GameObject _placeablePreview;
    private Material _placeablePreviewMaterial;

    /// <summary>
    /// 소유 클라이언트의 로컬 인벤토리와 서버의 월드 스폰을 예약/확정 프로토콜로 연결한다.
    /// 인벤토리는 서버 PlayerController 복제본에 존재하지 않으므로 서버에서 직접 소비하지 않는다.
    /// </summary>
    public void RequestPlaceHeldEntityPreset(string itemIdentifier, string entityPresetIdentifier)
    {
      if (!IsSupportedPlaceable(itemIdentifier, entityPresetIdentifier))
        return;
      if (CountItemInInventory(itemIdentifier) < 1)
        return;
      if (Time.unscaledTime < _localPlaceableRequestExpiresAt)
        return;

      _localPlaceableRequestExpiresAt = Time.unscaledTime + PlaceableRequestTimeout;

      if (!IsSpawned)
      {
        ConsumeAndPlaceOffline(itemIdentifier, entityPresetIdentifier);
        return;
      }

      if (IsServerStarted)
      {
        // 호스트는 서버와 소유 클라이언트 인벤토리가 같은 인스턴스다.
        ConsumeAndPlaceOffline(itemIdentifier, entityPresetIdentifier);
        return;
      }

      CmdRequestPlaceHeldEntityPreset(itemIdentifier, entityPresetIdentifier);
    }

    [ServerRpc]
    private void CmdRequestPlaceHeldEntityPreset(
      string itemIdentifier,
      string entityPresetIdentifier,
      NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid ||
          Owner == null || !Owner.IsValid || sender.ClientId != Owner.ClientId ||
          !IsSupportedPlaceable(itemIdentifier, entityPresetIdentifier))
        return;

      if (!string.IsNullOrEmpty(_pendingPlaceableItemIdentifier) &&
          Time.unscaledTime < _pendingPlaceableExpiresAt)
        return;

      _pendingPlaceableItemIdentifier = itemIdentifier;
      _pendingPlaceableEntityPresetIdentifier = entityPresetIdentifier;
      _pendingPlaceableClaimantClientId = sender.ClientId;
      _pendingPlaceableExpiresAt = Time.unscaledTime + PlaceableRequestTimeout;

      TargetConfirmPlaceableItemConsumption(sender, itemIdentifier, entityPresetIdentifier);
    }

    [TargetRpc]
    private void TargetConfirmPlaceableItemConsumption(
      NetworkConnection connection,
      string itemIdentifier,
      string entityPresetIdentifier)
    {
      _localPlaceableRequestExpiresAt = 0f;

      if (!IsSupportedPlaceable(itemIdentifier, entityPresetIdentifier) ||
          CountItemInInventory(itemIdentifier) < 1 ||
          RemoveItemFromInventory(itemIdentifier, 1) != 1)
      {
        CmdReportPlaceableItemConsumptionFailure(itemIdentifier, entityPresetIdentifier);
        return;
      }

      CmdAcknowledgePlaceableItemConsumption(itemIdentifier, entityPresetIdentifier);
    }

    [ServerRpc]
    private void CmdAcknowledgePlaceableItemConsumption(
      string itemIdentifier,
      string entityPresetIdentifier,
      NetworkConnection sender = null)
    {
      if (!MatchesPendingPlaceable(itemIdentifier, entityPresetIdentifier, sender))
        return;

      ClearPendingPlaceable();
      if (!TrySpawnPlaceableEntityPreset(itemIdentifier, entityPresetIdentifier))
        TargetRefundPlaceableItem(sender, itemIdentifier);
    }

    [ServerRpc]
    private void CmdReportPlaceableItemConsumptionFailure(
      string itemIdentifier,
      string entityPresetIdentifier,
      NetworkConnection sender = null)
    {
      if (MatchesPendingPlaceable(itemIdentifier, entityPresetIdentifier, sender))
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

    private void ConsumeAndPlaceOffline(string itemIdentifier, string entityPresetIdentifier)
    {
      _localPlaceableRequestExpiresAt = 0f;
      if (RemoveItemFromInventory(itemIdentifier, 1) != 1)
        return;

      if (TrySpawnPlaceableEntityPreset(itemIdentifier, entityPresetIdentifier))
        return;

      var refund = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (refund != null && !TryAddItemToInventory(refund))
        TryDropItemInFront(refund);
    }

    private bool TrySpawnPlaceableEntityPreset(string itemIdentifier, string entityPresetIdentifier)
    {
      GetPlaceablePose(out Vector3 position, out Quaternion rotation);
      if (!Registry.Registry.TrySpawnEntityPreset(
            entityPresetIdentifier,
            position,
            rotation,
            out _,
            out _,
            out string error))
      {
        Debug.LogWarning(
          $"[PlayerController] Failed to spawn placeable '{itemIdentifier}' from " +
          $"entity preset '{entityPresetIdentifier}': {error}",
          this);
        return false;
      }
      return true;
    }

    /// <summary>
    /// 로컬 소유자에게만 설치 위치의 고스트 모델을 표시한다. 고스트는 어떠한 충돌/상호작용/네트워크 동작도 하지 않는다.
    /// </summary>
    private void Update_PlaceableItemPreview()
    {
      bool shouldShow = IsOwner &&
                        HandlingItem != null &&
                        IsSupportedPlaceable(
                          HandlingItem.CurrentIdentifier,
                          Level1RapidInfuserEntityPresetIdentifier) &&
                        CountItemInInventory(Level1RapidInfuserItemIdentifier) > 0;
      if (!shouldShow)
      {
        SetPlaceablePreviewVisible(false);
        return;
      }

      if (_placeablePreview == null && !TryCreatePlaceablePreview())
        return;

      GetPlaceablePose(out Vector3 position, out Quaternion rotation);
      _placeablePreview.transform.SetPositionAndRotation(position, rotation);
      SetPlaceablePreviewVisible(true);
    }

    private bool TryCreatePlaceablePreview()
    {
      if (!Registry.Registry.TryGetEntityPreset(
            Level1RapidInfuserEntityPresetIdentifier,
            out var preset) || preset?.Prefab == null)
        return false;

      _placeablePreview = UnityEngine.Object.Instantiate(preset.Prefab);
      _placeablePreview.name = "PlacementPreview_Level1RapidInfuser";

      foreach (var behaviour in _placeablePreview.GetComponentsInChildren<Behaviour>(true))
        behaviour.enabled = false;
      foreach (var collider in _placeablePreview.GetComponentsInChildren<Collider>(true))
        collider.enabled = false;
      foreach (var body in _placeablePreview.GetComponentsInChildren<Rigidbody>(true))
        body.isKinematic = true;

      var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent");
      if (shader == null)
      {
        Debug.LogWarning("[PlayerController] Placement preview shader was not found.", this);
        UnityEngine.Object.Destroy(_placeablePreview);
        _placeablePreview = null;
        return false;
      }

      _placeablePreviewMaterial = new Material(shader) { name = "PlacementPreviewMaterial" };
      Color tint = new Color(0.25f, 0.9f, 1f, 0.38f);
      if (_placeablePreviewMaterial.HasProperty("_BaseColor"))
        _placeablePreviewMaterial.SetColor("_BaseColor", tint);
      if (_placeablePreviewMaterial.HasProperty("_Color"))
        _placeablePreviewMaterial.SetColor("_Color", tint);
      if (_placeablePreviewMaterial.HasProperty("_Surface"))
        _placeablePreviewMaterial.SetFloat("_Surface", 1f);
      _placeablePreviewMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
      _placeablePreviewMaterial.SetOverrideTag("RenderType", "Transparent");
      _placeablePreviewMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
      _placeablePreviewMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
      _placeablePreviewMaterial.SetInt("_ZWrite", 0);
      _placeablePreviewMaterial.renderQueue = (int)RenderQueue.Transparent;

      foreach (var renderer in _placeablePreview.GetComponentsInChildren<Renderer>(true))
        renderer.sharedMaterial = _placeablePreviewMaterial;
      _placeablePreview.SetActive(false);
      return true;
    }

    private void SetPlaceablePreviewVisible(bool visible)
    {
      if (_placeablePreview != null && _placeablePreview.activeSelf != visible)
        _placeablePreview.SetActive(visible);
    }

    private void OnDestroy_PlaceableItemPreview()
    {
      if (_placeablePreview != null)
        UnityEngine.Object.Destroy(_placeablePreview);
      if (_placeablePreviewMaterial != null)
        UnityEngine.Object.Destroy(_placeablePreviewMaterial);
    }

    private void GetPlaceablePose(out Vector3 position, out Quaternion rotation)
    {
      Vector3 forward = transform.forward;
      forward.y = 0f;
      if (forward.sqrMagnitude < 0.001f)
        forward = Vector3.forward;
      forward.Normalize();

      position = transform.position + forward * PlaceableDistance;
      rotation = Quaternion.LookRotation(forward, Vector3.up);
    }

    private bool MatchesPendingPlaceable(
      string itemIdentifier,
      string entityPresetIdentifier,
      NetworkConnection sender)
    {
      return sender != null && sender.IsValid &&
             sender.ClientId == _pendingPlaceableClaimantClientId &&
             Time.unscaledTime <= _pendingPlaceableExpiresAt &&
             string.Equals(itemIdentifier, _pendingPlaceableItemIdentifier, StringComparison.Ordinal) &&
             string.Equals(entityPresetIdentifier, _pendingPlaceableEntityPresetIdentifier, StringComparison.Ordinal);
    }

    private void ClearPendingPlaceable()
    {
      _pendingPlaceableItemIdentifier = null;
      _pendingPlaceableEntityPresetIdentifier = null;
      _pendingPlaceableClaimantClientId = -1;
      _pendingPlaceableExpiresAt = 0f;
    }

    private static bool IsSupportedPlaceable(string itemIdentifier, string entityPresetIdentifier)
    {
      return string.Equals(itemIdentifier, Level1RapidInfuserItemIdentifier, StringComparison.Ordinal) &&
             string.Equals(entityPresetIdentifier, Level1RapidInfuserEntityPresetIdentifier, StringComparison.Ordinal);
    }
  }
}
