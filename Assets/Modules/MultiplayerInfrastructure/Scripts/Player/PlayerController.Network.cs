using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Server;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using UnityEngine;

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

    private sealed class PendingWorldItemDrop
    {
      public Item Item { get; set; }
    }

    private static readonly System.Collections.Generic.Dictionary<string, PendingWorldItemPickup> _pendingWorldItemPickups = new(StringComparer.Ordinal);
    private const float MaxWorldItemPickupDistance = 4f;
    private const float MaxWorldItemPickupDistanceSqr = MaxWorldItemPickupDistance * MaxWorldItemPickupDistance;
    private const float WorldItemTransformSyncInterval = 0.1f;
    private const float MaxWorldItemDropDistance = 4f;
    private const float MaxWorldItemThrowForce = 20f;
    private const float WorldItemDropQuotaWindowSeconds = 1f;
    private const int MaxWorldItemDropsPerQuotaWindow = 8;
    private const int MaxSerializedDerivedAttributesLength = 16384;
    private static PlayerController _worldItemTransformSyncAuthority;
    private float _nextWorldItemTransformSyncTime;
    private readonly Dictionary<int, PendingWorldItemDrop> _pendingWorldItemDrops = new();
    private readonly Queue<float> _serverWorldItemDropTimes = new();
    private int _nextWorldItemDropRequestId;

    // ── SyncVars ─────────────────────────────────────────────────────────────
    // 서버가 설정하고 모든 클라이언트로 자동 전파됩니다.

    private readonly SyncVar<string> _entityIdentifier = new SyncVar<string>();
    private readonly SyncVar<string> _userIdentifier = new SyncVar<string>();
    private readonly SyncVar<string> _userDisplayName = new SyncVar<string>();

    /// <summary>이 PlayerController가 나타내는 엔티티의 전역 식별자.</summary>
    public string EntityIdentifier => _entityIdentifier.Value;

    /// <summary>이 PlayerController가 나타내는 플레이어의 Identifier(UUID).</summary>
    public string UserIdentifier => _userIdentifier.Value;

    /// <summary>이 PlayerController가 나타내는 플레이어의 DisplayName.</summary>
    public string UserDisplayName => _userDisplayName.Value;

    // ── 서버 생명주기 ─────────────────────────────────────────────────────────

    public override void OnStartServer()
    {
      base.OnStartServer();

      _worldItemTransformSyncAuthority ??= this;

      // 서버가 UserDescriptor를 발급하고 SyncVar에 설정
      var descriptor = UserDescriptor.CreateDefault();
      _entityIdentifier.Value = BuildPlayerEntityIdentifier(descriptor.Identifier);
      _userIdentifier.Value = descriptor.Identifier;
      _userDisplayName.Value = descriptor.DisplayName;

      if (ServerBanService.IsBanned(descriptor.DisplayName))
      {
        Debug.LogWarning($"[PlayerController] Banned player '{descriptor.DisplayName}' attempted to connect.");
        Owner?.Disconnect(true);
        return;
      }

      PlayerGamemodeService.RegisterPlayer(this);
      UserDescriptorService.Register(Owner.ClientId, descriptor);
      BroadcastConnectionMessage($"{descriptor.DisplayName}가 들어왔습니다.");
      RegisterPlayerEntity();
      OnStartServer_PlayerModel();
      InitializeRunningSpeedMultiplierServer();
      SyncPlayerTagsToObservers();
      SyncQuestStateFlagsToObservers();
    }

    public override void OnSpawnServer(NetworkConnection connection)
    {
      base.OnSpawnServer(connection);
      SyncExistingWorldItemsToConnection(connection);
      SyncStaticPlacedItemsToConnection(connection);
      SyncStaticObjectDisplaymentsToConnection(connection);
    }

    public override void OnStopServer()
    {
      string displayName = _userDisplayName.Value;

      if (ReferenceEquals(_worldItemTransformSyncAuthority, this))
        _worldItemTransformSyncAuthority = null;

      PlayerGamemodeService.UnregisterPlayer(this);
      Registry.Registry.UnregisterEntity(_entityIdentifier.Value);
      PlayerTagService.ClearTags(_userIdentifier.Value);
      PlayerQuestStateFlagService.ClearFlags(_userIdentifier.Value);
      UserDescriptorService.Unregister(_userIdentifier.Value);
      if (!string.IsNullOrWhiteSpace(displayName))
        BroadcastConnectionMessage($"{displayName}가 나갔습니다.");

      // 이 플레이어가 승인받았으나 확정(성공/실패)하지 못한 정적 아이템 픽업 예약이 있으면
      // 선점 감소한 Remains 를 복원한다. 아래 ClearUser 보다 먼저 수행해야 Local 예약 복원이 유효하다.
      RestorePendingStaticPickupsForClaimant();

      // 표시(설치/적용) 확정을 받지 못한 정적 오브젝트 표시 예약을 제거한다(상태 변이는 없음).
      CancelPendingStaticObjectAppliesForClaimant();

      // StaticPlacedItem 의 Local 모드 상태는 기본적으로 재접속 시 초기화한다.
      // 접속 종료 시 이 유저의 개별 Remains 상태를 제거한다.
      StaticPlacedItemService.ClearUser(_userIdentifier.Value);

      // 이 플레이어가 점유(claim)했지만 확정(ack/failure)하지 못한 픽업이 있으면
      // 아이템이 영구 유실되지 않도록 월드에 되돌린다.
      RestorePendingPickupsForClaimant();
      _serverWorldItemDropTimes.Clear();

      base.OnStopServer();
    }

    private static void BroadcastConnectionMessage(string message)
    {
      if (Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out var chatService))
        chatService.BroadcastSystemMessage(message);
    }

    private void UpdateServerWorldItemTransforms()
    {
      if (!IsServerStarted)
        return;

      // 현재 담당 플레이어가 종료된 경우 남아 있는 서버 플레이어가 이어받는다.
      _worldItemTransformSyncAuthority ??= this;
      if (!ReferenceEquals(_worldItemTransformSyncAuthority, this) ||
          Time.time < _nextWorldItemTransformSyncTime)
        return;

      _nextWorldItemTransformSyncTime = Time.time + WorldItemTransformSyncInterval;
      foreach (var pair in Registry.Registry.GetAllEntities(EntityType.ItemObject))
      {
        var descriptor = pair.Value;
        var itemObject = descriptor?.GameObject?.GetComponent<ItemObject>();
        if (itemObject == null)
          continue;

        RpcSyncWorldItemTransform(
          descriptor.Identifier,
          itemObject.AuthoritativePosition,
          itemObject.AuthoritativeRotation,
          itemObject.IsGrounded);
      }
    }

    [ObserversRpc]
    private void RpcSyncWorldItemTransform(
      string entityIdentifier,
      Vector3 position,
      Quaternion rotation,
      bool grounded)
    {
      var itemObject = Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier);
      itemObject?.ApplyAuthoritativeState(position, rotation, grounded);
    }

    /// <summary>
    /// 접속 종료 등으로 픽업 확정이 오지 않은 항목을 월드에 다시 스폰한다(서버 전용).
    /// </summary>
    private void RestorePendingPickupsForClaimant()
    {
      if (Owner == null || !Owner.IsValid)
        return;

      int claimantId = Owner.ClientId;
      var toRestore = new List<KeyValuePair<string, PendingWorldItemPickup>>();
      foreach (var kvp in _pendingWorldItemPickups)
      {
        if (kvp.Value != null && kvp.Value.ClaimantClientId == claimantId)
          toRestore.Add(kvp);
      }

      foreach (var kvp in toRestore)
      {
        _pendingWorldItemPickups.Remove(kvp.Key);
        var pending = kvp.Value;
        RpcSpawnDroppedWorldItem(
          kvp.Key,
          pending.ItemIdentifier,
          pending.StackCount,
          pending.Durability,
          pending.CooldownRemainingMilliseconds,
          pending.SerializedDerivedAttributes ?? string.Empty,
          pending.Position,
          pending.Rotation,
          Vector3.zero);
      }
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
      OnStartClient_AnyPeer_PlayerModel();

      // 이후 DisplayName 변경(서버 반영) 시 갱신
      _userDisplayName.OnChange += OnDisplayNameChanged;
    }

    /// <summary>PlayerController가 어느 클라이언트에서 디스폰될 때 호출됩니다.</summary>
    private void OnStopClient_AnyPeer()
    {
      _userDisplayName.OnChange -= OnDisplayNameChanged;
      OnStopClient_AnyPeer_PlayerModel();
      Registry.Registry.UnregisterEntity(_entityIdentifier.Value);
      PlayerTagService.ClearTags(_userIdentifier.Value);
      PlayerQuestStateFlagService.ClearFlags(_userIdentifier.Value);
      UserDescriptorService.Unregister(_userIdentifier.Value);

      RestoreAllPendingWorldItemDrops();
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

    /// <summary>
    /// 서버에서 현재 플레이어의 퀘스트 상태 플래그 풀을 모든 옵저버에게 동기화합니다.
    /// 상호작용 노출 판정이 각 피어에서 로컬로 이뤄지므로, 서버 기록만으로는 화면에 반영되지 않습니다.
    /// </summary>
    public void SyncQuestStateFlagsToObservers()
    {
      if (!IsServerStarted || string.IsNullOrWhiteSpace(_userIdentifier.Value))
      {
        return;
      }

      var currentFlags = PlayerQuestStateFlagService.GetFlags(_userIdentifier.Value);
      var snapshot = new string[currentFlags.Count];
      int index = 0;
      foreach (string flag in currentFlags)
      {
        snapshot[index++] = flag;
      }

      RpcApplyQuestStateFlags(_userIdentifier.Value, snapshot);
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcApplyQuestStateFlags(string userIdentifier, string[] flags)
    {
      PlayerQuestStateFlagService.ReplaceFlags(userIdentifier, flags);
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
      if (!UserDescriptorService.TryNormalizeDisplayName(displayName, out string normalized, out string error))
      {
        Debug.LogWarning($"[PlayerController] Rejected display name: {error}");
        return;
      }
      if (ServerBanService.IsBanned(normalized))
      {
        Debug.LogWarning($"[PlayerController] Banned display name '{normalized}' attempted to connect.");
        Owner?.Disconnect(true);
        return;
      }
      if (UserDescriptorService.IsDisplayNameInUse(normalized, _userIdentifier.Value))
      {
        Debug.LogWarning($"[PlayerController] Rejected duplicate display name '{normalized}'.");
        return;
      }
      _userDisplayName.Value = normalized;
      // 서버 측 서비스도 즉시 갱신
      UserDescriptorService.UpdateDisplayName(_userIdentifier.Value, normalized);
      Registry.Registry.UpdateEntityDisplayName(_entityIdentifier.Value, normalized);
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
        return SpawnWorldItemLocal(
          fallbackEntityId,
          itemData.CurrentIdentifier,
          itemData.CurrentStackCount,
          itemData.CurrentDurability,
          itemData.CurrentCooldownRemainingMilliseconds,
          itemData.CurrentSerializedDerivedAttributes,
          position,
          Quaternion.identity,
          throwForce);
      }

      if (IsServerStarted)
      {
        return ServerSpawnDroppedWorldItem(
          itemData.CurrentIdentifier,
          itemData.CurrentStackCount,
          itemData.CurrentDurability,
          itemData.CurrentCooldownRemainingMilliseconds,
          itemData.CurrentSerializedDerivedAttributes,
          position,
          Quaternion.identity,
          throwForce);
      }

      int requestId = NextWorldItemDropRequestId();
      _pendingWorldItemDrops[requestId] = new PendingWorldItemDrop
      {
        Item = itemData.Clone()
      };
      CmdSpawnDroppedWorldItem(
        requestId,
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
      int requestId,
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce)
    {
      bool accepted = ServerSpawnDroppedWorldItem(
        itemIdentifier,
        stackCount,
        durability,
        cooldownRemainingMilliseconds,
        serializedDerivedAttributes,
        position,
        rotation,
        throwForce,
        requireInventoryBacking: true);
      if (Owner != null && Owner.IsValid)
        TargetCompleteDroppedWorldItemRequest(Owner, requestId, accepted);
    }

    private bool ServerSpawnDroppedWorldItem(
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce,
      bool requireInventoryBacking = false)
    {
      if (!TryValidateDroppedWorldItemRequest(
            itemIdentifier,
            stackCount,
            durability,
            cooldownRemainingMilliseconds,
            serializedDerivedAttributes,
            position,
            rotation,
            throwForce,
            out string normalizedIdentifier))
        return false;

      // 서버 복제본의 실제 인벤토리에서 동일 상태의 아이템을 소비해야만 월드 스폰을 승인한다.
      // 클라이언트가 RPC 인자를 조작해 등록 아이템을 임의 생성하는 경로를 차단한다.
      if (requireInventoryBacking && !TryConsumeMatchingInventoryDrop(
            normalizedIdentifier, stackCount, durability, cooldownRemainingMilliseconds,
            serializedDerivedAttributes))
      {
        Debug.LogWarning($"[PlayerController] Rejected unbacked world item drop '{normalizedIdentifier}'.", this);
        return false;
      }

      string entityIdentifier = BuildDroppedItemEntityIdentifier();
      RpcSpawnDroppedWorldItem(
        entityIdentifier,
        normalizedIdentifier,
        stackCount,
        durability,
        cooldownRemainingMilliseconds,
        serializedDerivedAttributes ?? string.Empty,
        position,
        rotation,
        throwForce);
      return true;
    }

    [TargetRpc]
    private void TargetCompleteDroppedWorldItemRequest(
      NetworkConnection connection,
      int requestId,
      bool accepted)
    {
      if (!_pendingWorldItemDrops.Remove(requestId, out var pending) || pending?.Item == null)
        return;

      if (accepted)
        return;

      RestorePendingWorldItemDrop(pending.Item);
    }

    private bool TryValidateDroppedWorldItemRequest(
      string itemIdentifier,
      int stackCount,
      int durability,
      float cooldownRemainingMilliseconds,
      string serializedDerivedAttributes,
      Vector3 position,
      Quaternion rotation,
      Vector3 throwForce,
      out string normalizedIdentifier)
    {
      normalizedIdentifier = string.IsNullOrWhiteSpace(itemIdentifier)
        ? string.Empty
        : itemIdentifier.Trim();
      if (string.IsNullOrEmpty(normalizedIdentifier)
          || !IsFinite(position)
          || !IsFinite(rotation)
          || !IsFinite(throwForce)
          || !IsFinite(cooldownRemainingMilliseconds)
          || cooldownRemainingMilliseconds < 0f
          || throwForce.sqrMagnitude > MaxWorldItemThrowForce * MaxWorldItemThrowForce
          || (position - transform.position).sqrMagnitude > MaxWorldItemDropDistance * MaxWorldItemDropDistance
          || (serializedDerivedAttributes?.Length ?? 0) > MaxSerializedDerivedAttributesLength
          || !TryConsumeWorldItemDropQuota())
        return false;

      var definition = Registry.Registry.CreateItemInstance(normalizedIdentifier);
      if (definition == null
          || stackCount < 1
          || stackCount > definition.CurrentMaxStackCount
          || durability < 0
          || (definition.HasCurrentDurability && durability > definition.CurrentMaxDurability)
          || (!definition.HasCurrentDurability && durability != 0)
          || cooldownRemainingMilliseconds > Mathf.Max(0f, definition.CurrentCooldownMilliseconds))
        return false;

      if (!string.IsNullOrWhiteSpace(serializedDerivedAttributes))
      {
        try
        {
          definition.SetCurrentSerializedDerivedAttributes(serializedDerivedAttributes);
        }
        catch (Exception ex)
        {
          Debug.LogWarning($"[PlayerController] Rejected invalid dropped item attributes: {ex.Message}");
          return false;
        }
      }

      return true;
    }

    private bool TryConsumeWorldItemDropQuota()
    {
      float cutoff = Time.unscaledTime - WorldItemDropQuotaWindowSeconds;
      while (_serverWorldItemDropTimes.Count > 0 && _serverWorldItemDropTimes.Peek() < cutoff)
        _serverWorldItemDropTimes.Dequeue();
      if (_serverWorldItemDropTimes.Count >= MaxWorldItemDropsPerQuotaWindow)
        return false;
      _serverWorldItemDropTimes.Enqueue(Time.unscaledTime);
      return true;
    }

    private int NextWorldItemDropRequestId()
    {
      _nextWorldItemDropRequestId++;
      if (_nextWorldItemDropRequestId <= 0)
        _nextWorldItemDropRequestId = 1;
      return _nextWorldItemDropRequestId;
    }

    private void RestoreAllPendingWorldItemDrops()
    {
      if (_pendingWorldItemDrops.Count == 0)
        return;

      var pending = new List<Item>();
      foreach (var each in _pendingWorldItemDrops.Values)
      {
        if (each?.Item != null)
          pending.Add(each.Item);
      }
      _pendingWorldItemDrops.Clear();
      for (int i = 0; i < pending.Count; i++)
        RestorePendingWorldItemDrop(pending[i]);
    }

    private void RestorePendingWorldItemDrop(Item item)
    {
      if (item == null || TryAddItemToInventory(item))
        return;
      Debug.LogError($"[PlayerController] Failed to restore rejected world item drop '{item.CurrentIdentifier}'.", this);
    }

    private static bool IsFinite(Vector3 value) =>
      IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);

    private static bool IsFinite(Quaternion value) =>
      IsFinite(value.x) && IsFinite(value.y)
      && IsFinite(value.z) && IsFinite(value.w)
      && value.x * value.x + value.y * value.y + value.z * value.z + value.w * value.w
      > 0.000001f;

    private static bool IsFinite(float value) =>
      !float.IsNaN(value) && !float.IsInfinity(value);

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
      if (Owner == null || !Owner.IsValid
          || !_pendingWorldItemPickups.TryGetValue(entityIdentifier, out var pending)
          || pending == null || pending.ClaimantClientId != Owner.ClientId)
        return;
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

      var claimantPosition = ResolveServerPickupOriginPosition();
      var pickupPoint = ResolveWorldItemPickupPoint(itemObject, claimantPosition);
      float sqrDistance = (pickupPoint - claimantPosition).sqrMagnitude;
      if (sqrDistance > MaxWorldItemPickupDistanceSqr)
      {
        Debug.LogWarning(
          $"[PlayerController] Reject pickup '{entityIdentifier}': too far from player. " +
          $"distance={Mathf.Sqrt(sqrDistance):0.00}m, limit={MaxWorldItemPickupDistance:0.00}m, " +
          $"player={claimantPosition}, itemRoot={itemObject.transform.position}, pickupPoint={pickupPoint}");
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

    private Vector3 ResolveServerPickupOriginPosition()
    {
      if (TryGetComponent<CharacterController>(out var characterController))
      {
        return transform.TransformPoint(characterController.center);
      }

      if (TryGetComponent<CapsuleCollider>(out var capsuleCollider))
      {
        return transform.TransformPoint(capsuleCollider.center);
      }

      return transform.position;
    }

    private static Vector3 ResolveWorldItemPickupPoint(ItemObject itemObject, Vector3 referencePosition)
    {
      if (itemObject == null)
        return referencePosition;

      bool foundCandidate = false;
      float bestDistanceSqr = float.MaxValue;
      Vector3 bestPoint = itemObject.transform.position;

      var colliders = itemObject.GetComponentsInChildren<Collider>(includeInactive: false);
      for (int i = 0; i < colliders.Length; i++)
      {
        var collider = colliders[i];
        if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
          continue;

        var point = collider.ClosestPoint(referencePosition);
        float sqr = (point - referencePosition).sqrMagnitude;
        if (sqr < bestDistanceSqr)
        {
          bestDistanceSqr = sqr;
          bestPoint = point;
          foundCandidate = true;
        }
      }

      if (foundCandidate)
        return bestPoint;

      var renderers = itemObject.GetComponentsInChildren<Renderer>(includeInactive: false);
      for (int i = 0; i < renderers.Length; i++)
      {
        var renderer = renderers[i];
        if (renderer == null || !renderer.enabled)
          continue;

        var point = renderer.bounds.ClosestPoint(referencePosition);
        float sqr = (point - referencePosition).sqrMagnitude;
        if (sqr < bestDistanceSqr)
        {
          bestDistanceSqr = sqr;
          bestPoint = point;
          foundCandidate = true;
        }
      }

      return foundCandidate ? bestPoint : itemObject.transform.position;
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

      // 전량 수용 가능 여부를 먼저 확인한다(all-or-nothing).
      // 부분 추가 후 실패 보고를 하면 서버가 원래 스택 전체를 다시 스폰하여
      // 부분 추가된 만큼 아이템이 복제되는 버그가 발생한다.
      if (!CanAcceptItem(item) || !TryAddItemToInventory(item))
      {
        Debug.LogWarning($"[PlayerController] Pickup confirmation received for '{entityIdentifier}', but inventory is full.");
        CmdReportPickupWorldItemFailure(entityIdentifier);
        return;
      }

      // OnGet 은 TryAddItemToInventory 내부에서 1회 호출된다(중복 호출 금지).
      CmdAcknowledgePickupWorldItemSuccess(entityIdentifier);
    }

    private static string BuildDroppedItemEntityIdentifier()
      => $"item:{Guid.NewGuid():N}";

    private bool SpawnWorldItemLocal(
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
        return false;

      if (Registry.Registry.Get<ItemObject>(RegistryType.Entity, entityIdentifier) != null)
        return false;

      var item = Registry.Registry.CreateItemInstance(itemIdentifier);
      if (item == null)
      {
        Debug.LogWarning($"[PlayerController] Failed to create dropped item '{itemIdentifier}'.");
        return false;
      }

      item.CurrentStackCount = Mathf.Max(1, stackCount);
      if (item.HasCurrentDurability)
        item.CurrentDurability = Mathf.Max(0, durability);
      item.CurrentCooldownRemainingMilliseconds = Mathf.Max(0f, cooldownRemainingMilliseconds);

      if (!string.IsNullOrWhiteSpace(serializedDerivedAttributes))
        item.SetCurrentSerializedDerivedAttributes(serializedDerivedAttributes);

      var itemObject = ItemObject.Spawn(item, position, throwForce, entityIdentifier);
      if (itemObject != null)
      {
        itemObject.transform.rotation = rotation;
        return true;
      }
      return false;
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
