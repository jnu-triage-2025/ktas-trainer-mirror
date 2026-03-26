using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;
using FishNet.Connection;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController : NetworkBehaviour
  {
    private sealed class PendingWorldItemPickup
    {
      public int ClaimantClientId { get; set; }
      public string ItemIdentifier { get; set; }
      public int StackCount { get; set; }
      public int Durability { get; set; }
      public float CooldownRemainingMilliseconds { get; set; }
      public string SerializedDerivedAttributes { get; set; }
      public Vector3 Position { get; set; }
      public Quaternion Rotation { get; set; }
    }

    private static readonly System.Collections.Generic.Dictionary<string, PendingWorldItemPickup> _pendingWorldItemPickups = new(StringComparer.Ordinal);

    // ── SyncVars ─────────────────────────────────────────────────────────────
    // 서버가 설정하고 모든 클라이언트로 자동 전파됩니다.

    private readonly SyncVar<string> _entityIdentifier = new SyncVar<string>();
    private readonly SyncVar<string> _userIdentifier  = new SyncVar<string>();
    private readonly SyncVar<string> _userDisplayName = new SyncVar<string>();

    /// <summary>이 PlayerController가 나타내는 엔티티의 전역 식별자.</summary>
    public string EntityIdentifier => _entityIdentifier.Value;

    /// <summary>이 PlayerController가 나타내는 플레이어의 Identifier(UUID).</summary>
    public string UserIdentifier  => _userIdentifier.Value;

    /// <summary>이 PlayerController가 나타내는 플레이어의 DisplayName.</summary>
    public string UserDisplayName => _userDisplayName.Value;

    // ── 서버 생명주기 ─────────────────────────────────────────────────────────

    public override void OnStartServer()
    {
      base.OnStartServer();

      // 서버가 UserDescriptor를 발급하고 SyncVar에 설정
      var descriptor = UserDescriptor.CreateDefault();
      _entityIdentifier.Value = BuildPlayerEntityIdentifier(descriptor.Identifier);
      _userIdentifier.Value  = descriptor.Identifier;
      _userDisplayName.Value = descriptor.DisplayName;

      PlayerGamemodeService.RegisterPlayer(this);
      UserDescriptorService.Register(Owner.ClientId, descriptor);
      RegisterPlayerEntity();
      SyncPlayerTagsToObservers();
      SyncExistingWorldItemsToConnection(Owner);
    }

    public override void OnStopServer()
    {
      PlayerGamemodeService.UnregisterPlayer(this);
      Registry.Registry.UnregisterEntity(_entityIdentifier.Value);
      PlayerTagService.ClearTags(_userIdentifier.Value);
      UserDescriptorService.Unregister(_userIdentifier.Value);
      base.OnStopServer();
    }

    // ── 클라이언트 생명주기 (모든 클라이언트 — owner 무관) ────────────────────

    /// <summary>
    /// PlayerController가 어느 클라이언트에서 스폰될 때마다 호출됩니다.
    /// SyncVar에서 UserDescriptor를 읽어 로컬 UserDescriptorService에 등록합니다.
    /// </summary>
    private void OnStartClient_AnyPeer()
    {
      // SyncVar 초기값으로 등록 (스폰 패킷에 이미 포함된 값)
      var descriptor = new UserDescriptor(_userIdentifier.Value, _userDisplayName.Value);
      UserDescriptorService.Register(Owner.ClientId, descriptor);
      RegisterPlayerEntity();

      // 이후 DisplayName 변경(서버 반영) 시 갱신
      _userDisplayName.OnChange += OnDisplayNameChanged;
    }

    /// <summary>PlayerController가 어느 클라이언트에서 디스폰될 때 호출됩니다.</summary>
    private void OnStopClient_AnyPeer()
    {
      _userDisplayName.OnChange -= OnDisplayNameChanged;
      Registry.Registry.UnregisterEntity(_entityIdentifier.Value);
      PlayerTagService.ClearTags(_userIdentifier.Value);
      UserDescriptorService.Unregister(_userIdentifier.Value);
    }

    private void OnDisplayNameChanged(string prev, string next, bool asServer)
    {
      UserDescriptorService.UpdateDisplayName(_userIdentifier.Value, next);
      Registry.Registry.UpdateEntityDisplayName(_entityIdentifier.Value, next);
    }

    /// <summary>
    /// 서버에서 현재 플레이어의 태그 목록을 모든 옵저버에게 동기화합니다.
    /// </summary>
    public void SyncPlayerTagsToObservers()
    {
      if (!IsServerStarted || string.IsNullOrWhiteSpace(_userIdentifier.Value))
      {
        return;
      }

      var currentTags = PlayerTagService.GetTags(_userIdentifier.Value);
      var snapshot = new string[currentTags.Count];
      for (int i = 0; i < currentTags.Count; i++)
      {
        snapshot[i] = currentTags[i];
      }

      RpcApplyPlayerTags(_userIdentifier.Value, snapshot);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcApplyPlayerTags(string userIdentifier, string[] tags)
    {
      PlayerTagService.ReplaceTags(userIdentifier, tags);
    }

    // ── Owner 전용 초기화 ─────────────────────────────────────────────────────

    private void OnStartClient_Network()
    {
      // Registry에 저장된 DisplayName이 있으면 서버에 전달
      var name = Registry.Registry.Get<string>(RegistryType.RuntimeState, RegistryGlobalKeys.UserDisplayName);
      if (!string.IsNullOrWhiteSpace(name))
        CmdSetDisplayName(name);

      var cam = MainCameraController.Instance
                ?? Registry.Registry.Get<MainCameraController>(RegistryType.Service, Registry.Registry.TypeKey<MainCameraController>());
      if (cam != null)
        cam.SetTarget(this);
    }

    // ── ServerRpc ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Owner가 서버에 DisplayName을 전달합니다. 서버가 SyncVar를 갱신하면 모든 클라이언트에 전파됩니다.
    /// </summary>
    [ServerRpc]
    private void CmdSetDisplayName(string displayName)
    {
      if (string.IsNullOrWhiteSpace(displayName)) return;
      _userDisplayName.Value = displayName;
      // 서버 측 서비스도 즉시 갱신
      UserDescriptorService.UpdateDisplayName(_userIdentifier.Value, displayName);
      Registry.Registry.UpdateEntityDisplayName(_entityIdentifier.Value, displayName);
    }

    private void RegisterPlayerEntity()
    {
      if (string.IsNullOrWhiteSpace(_entityIdentifier.Value))
        return;

      Registry.Registry.RegisterEntity(
        _entityIdentifier.Value,
        EntityType.Player,
        gameObject,
        displayName: _userDisplayName.Value,
        ownerUserIdentifier: _userIdentifier.Value,
        clientId: Owner?.ClientId,
        isNetworked: true);
    }

    private static string BuildPlayerEntityIdentifier(string userIdentifier)
      => $"player:{userIdentifier}";

    private void SyncExistingWorldItemsToConnection(NetworkConnection connection)
    {
      if (connection == null)
        return;

      TargetResetWorldItems(connection);

      foreach (var pair in Registry.Registry.GetAllEntities(EntityType.ItemObject))
      {
        var descriptor = pair.Value;
        var gameObject = descriptor?.GameObject;
        if (gameObject == null || !gameObject.TryGetComponent<ItemObject>(out var itemObject) || itemObject.Item == null)
          continue;

        TargetSpawnWorldItem(
          connection,
          descriptor.Identifier,
          itemObject.Item.CurrentIdentifier,
          itemObject.Item.CurrentStackCount,
          itemObject.Item.CurrentDurability,
          itemObject.Item.CurrentCooldownRemainingMilliseconds,
          itemObject.Item.CurrentSerializedDerivedAttributes ?? string.Empty,
          itemObject.transform.position,
          itemObject.transform.rotation);
      }
    }

    [TargetRpc]
    private void TargetResetWorldItems(NetworkConnection conn)
    {
      if (IsServerStarted)
        return;

      var worldItems = Registry.Registry.GetAllEntities(EntityType.ItemObject);
      if (worldItems == null || worldItems.Count == 0)
        return;

      var toDestroy = new List<GameObject>();
      foreach (var pair in worldItems)
      {
        var itemGameObject = pair.Value?.GameObject;
        if (itemGameObject != null)
          toDestroy.Add(itemGameObject);
      }

      foreach (var itemGameObject in toDestroy)
      {
        if (itemGameObject != null)
          Destroy(itemGameObject);
      }
    }

    [TargetRpc]
    private void TargetSpawnWorldItem(
      NetworkConnection conn,
      string entityIdentifier,
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation)
    {
      SpawnWorldItemLocal(
        entityIdentifier,
        itemIdentifier,
        stackCount,
        durability,
        cooldownRemainingMilliseconds,
        serializedDerivedAttributes,
        position,
        rotation,
        Vector3.zero);
    }

    internal bool RequestDropWorldItem(Item itemData, Vector3 position, Vector3 throwForce)
    {
      if (itemData == null || string.IsNullOrWhiteSpace(itemData.CurrentIdentifier))
        return false;

      if (!IsSpawned)
      {
        string fallbackEntityId = BuildDroppedItemEntityIdentifier();
        SpawnWorldItemLocal(
          fallbackEntityId,
          itemData.CurrentIdentifier,
          itemData.CurrentStackCount,
          itemData.CurrentDurability,
          itemData.CurrentCooldownRemainingMilliseconds,
          itemData.CurrentSerializedDerivedAttributes,
          position,
          Quaternion.identity,
          throwForce);
        return true;
      }

      if (IsServerStarted)
      {
        ServerSpawnDroppedWorldItem(
          itemData.CurrentIdentifier,
          itemData.CurrentStackCount,
          itemData.CurrentDurability,
          itemData.CurrentCooldownRemainingMilliseconds,
          itemData.CurrentSerializedDerivedAttributes,
          position,
          Quaternion.identity,
          throwForce);
        return true;
      }

      CmdSpawnDroppedWorldItem(
        itemData.CurrentIdentifier,
        itemData.CurrentStackCount,
        itemData.CurrentDurability,
        itemData.CurrentCooldownRemainingMilliseconds,
        itemData.CurrentSerializedDerivedAttributes,
        position,
        Quaternion.identity,
        throwForce);
      return true;
    }

    internal void RequestDestroyWorldItem(ItemObject itemObject)
    {
      if (itemObject == null)
        return;

      if (string.IsNullOrWhiteSpace(itemObject.Identifier))
      {
        Destroy(itemObject.gameObject);
        return;
      }

      if (!IsSpawned)
      {
        DestroyWorldItemLocal(itemObject.Identifier);
        return;
      }

      if (IsServerStarted)
      {
        ServerDestroyWorldItem(itemObject.Identifier);
        return;
      }

      CmdDestroyWorldItem(itemObject.Identifier);
    }

    internal void RequestPickupWorldItem(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      if (!IsSpawned)
      {
        DestroyWorldItemLocal(entityIdentifier);
        return;
      }

      if (IsServerStarted)
      {
        ServerApprovePickupWorldItem(entityIdentifier, Owner);
        return;
      }

      CmdRequestPickupWorldItem(entityIdentifier);
    }

    [ServerRpc]
    private void CmdSpawnDroppedWorldItem(
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce)
    {
      ServerSpawnDroppedWorldItem(
        itemIdentifier,
        stackCount,
        durability,
        cooldownRemainingMilliseconds,
        serializedDerivedAttributes,
        position,
        rotation,
        throwForce);
    }

    private void ServerSpawnDroppedWorldItem(
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce)
    {
      string entityIdentifier = BuildDroppedItemEntityIdentifier();
      RpcSpawnDroppedWorldItem(
        entityIdentifier,
        itemIdentifier,
        stackCount,
        durability,
        cooldownRemainingMilliseconds,
        serializedDerivedAttributes ?? string.Empty,
        position,
        rotation,
        throwForce);
    }

    [ObserversRpc]
    private void RpcSpawnDroppedWorldItem(
      string entityIdentifier,
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce)
    {
      SpawnWorldItemLocal(
        entityIdentifier,
        itemIdentifier,
        stackCount,
        durability,
        cooldownRemainingMilliseconds,
        serializedDerivedAttributes,
        position,
        rotation,
        throwForce);
    }

    [ServerRpc]
    private void CmdDestroyWorldItem(string entityIdentifier)
    {
      ServerDestroyWorldItem(entityIdentifier);
    }

    [ServerRpc]
    private void CmdRequestPickupWorldItem(string entityIdentifier, NetworkConnection sender = null)
    {
      ServerApprovePickupWorldItem(entityIdentifier, sender ?? Owner);
    }

    private void ServerDestroyWorldItem(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      RpcDestroyWorldItem(entityIdentifier);
    }

    private void ServerApprovePickupWorldItem(string entityIdentifier, NetworkConnection claimant)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || claimant == null)
        return;

      if (Owner != null && claimant.ClientId != Owner.ClientId)
        return;

      var itemObject = Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier);
      if (itemObject == null || itemObject.Item == null)
        return;

      if (_pendingWorldItemPickups.ContainsKey(entityIdentifier))
        return;

      float sqrDistance = (itemObject.transform.position - transform.position).sqrMagnitude;
      if (sqrDistance > 9f)
      {
        Debug.LogWarning($"[PlayerController] Reject pickup '{entityIdentifier}': too far from player.");
        return;
      }

      var item = itemObject.Item;
      _pendingWorldItemPickups[entityIdentifier] = new PendingWorldItemPickup
      {
        ClaimantClientId = claimant.ClientId,
        ItemIdentifier = item.CurrentIdentifier,
        StackCount = item.CurrentStackCount,
        Durability = item.CurrentDurability,
        CooldownRemainingMilliseconds = item.CurrentCooldownRemainingMilliseconds,
        SerializedDerivedAttributes = item.CurrentSerializedDerivedAttributes ?? string.Empty,
        Position = itemObject.transform.position,
        Rotation = itemObject.transform.rotation,
      };

      TargetConfirmPickupWorldItem(
        claimant,
        entityIdentifier,
        item.CurrentIdentifier,
        item.CurrentStackCount,
        item.CurrentDurability,
        item.CurrentCooldownRemainingMilliseconds,
        item.CurrentSerializedDerivedAttributes ?? string.Empty);

      RpcDestroyWorldItem(entityIdentifier);
    }

    [ServerRpc]
    private void CmdAcknowledgePickupWorldItemSuccess(string entityIdentifier, NetworkConnection sender = null)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || sender == null)
        return;

      if (!_pendingWorldItemPickups.TryGetValue(entityIdentifier, out var pending))
        return;

      if (pending.ClaimantClientId != sender.ClientId)
        return;

      _pendingWorldItemPickups.Remove(entityIdentifier);
    }

    [ServerRpc]
    private void CmdReportPickupWorldItemFailure(string entityIdentifier, NetworkConnection sender = null)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || sender == null)
        return;

      if (!_pendingWorldItemPickups.TryGetValue(entityIdentifier, out var pending))
        return;

      if (pending.ClaimantClientId != sender.ClientId)
        return;

      _pendingWorldItemPickups.Remove(entityIdentifier);
      RpcSpawnDroppedWorldItem(
        entityIdentifier,
        pending.ItemIdentifier,
        pending.StackCount,
        pending.Durability,
        pending.CooldownRemainingMilliseconds,
        pending.SerializedDerivedAttributes ?? string.Empty,
        pending.Position,
        pending.Rotation,
        Vector3.zero);
    }

    [ObserversRpc]
    private void RpcDestroyWorldItem(string entityIdentifier)
    {
      DestroyWorldItemLocal(entityIdentifier);
    }

    [TargetRpc]
    private void TargetConfirmPickupWorldItem(
      NetworkConnection conn,
      string entityIdentifier,
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes)
    {
      if (string.IsNullOrWhiteSpace(itemIdentifier))
        return;

      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
        return;

      item.CurrentStackCount = Mathf.Max(1, stackCount);
      if (item.HasCurrentDurability)
        item.CurrentDurability = Mathf.Max(0, durability);
      item.CurrentCooldownRemainingMilliseconds = Mathf.Max(0f, cooldownRemainingMilliseconds);
      if (!string.IsNullOrWhiteSpace(serializedDerivedAttributes))
        item.SetCurrentSerializedDerivedAttributes(serializedDerivedAttributes);

      if (!TryAddItemToInventory(item))
      {
        Debug.LogWarning($"[PlayerController] Pickup confirmation received for '{entityIdentifier}', but inventory is full.");
        CmdReportPickupWorldItemFailure(entityIdentifier);
        return;
      }

      item.OnGet(this);
      CmdAcknowledgePickupWorldItemSuccess(entityIdentifier);
    }

    private static string BuildDroppedItemEntityIdentifier()
      => $"item:{Guid.NewGuid():N}";

    private void SpawnWorldItemLocal(
      string entityIdentifier,
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier) || string.IsNullOrWhiteSpace(itemIdentifier))
        return;

      if (Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier) != null)
        return;

      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
      {
        Debug.LogWarning($"[PlayerController] Failed to create dropped item '{itemIdentifier}'.");
        return;
      }

      item.CurrentStackCount = Mathf.Max(1, stackCount);
      if (item.HasCurrentDurability)
        item.CurrentDurability = Mathf.Max(0, durability);
      item.CurrentCooldownRemainingMilliseconds = Mathf.Max(0f, cooldownRemainingMilliseconds);

      if (!string.IsNullOrWhiteSpace(serializedDerivedAttributes))
        item.SetCurrentSerializedDerivedAttributes(serializedDerivedAttributes);

      var itemObject = ItemObject.Spawn(item, position, throwForce, entityIdentifier);
      if (itemObject != null)
        itemObject.transform.rotation = rotation;
    }

    private void DestroyWorldItemLocal(string entityIdentifier)
    {
      if (string.IsNullOrWhiteSpace(entityIdentifier))
        return;

      var itemObject = Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier);
      if (itemObject != null)
      {
        Destroy(itemObject.gameObject);
        return;
      }

      var gameObject = Registry.Registry.Get<GameObject>(RegistryType.Entity, entityIdentifier);
      if (gameObject != null)
        Destroy(gameObject);
    }
  }
}

