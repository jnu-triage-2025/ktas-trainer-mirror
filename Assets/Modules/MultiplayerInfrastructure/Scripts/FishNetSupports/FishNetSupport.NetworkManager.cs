using System.Collections.Generic;
using System.Reflection;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Example;
using FishNet.Managing;
using FishNet.Managing.Logging;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using MultiplayerInfrastructure.ItemSystem;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.FishNetSupports
{
  public partial class FishNetSupport
  {
    private const string FallbackSpawnIdentifier = "spawnpoint-commons";

    private static readonly FieldInfo PlayerSpawnerPrefabField =
      typeof(PlayerSpawner).GetField("_playerPrefab", BindingFlags.Instance | BindingFlags.NonPublic);

    private LocalConnectionState _serverStateAssumed = LocalConnectionState.Stopped;
    private LocalConnectionState _clientStateAssumed = LocalConnectionState.Stopped;
    private bool _deferredPlayerSpawningPrepared;
    private bool _systemSceneObserverPrepared;
    private bool _serverConnectionStateSubscribed;
    private bool _remoteConnectionStateSubscribed;
    private SystemSceneObserverBinder _systemSceneObserverBinder;
    private readonly List<GameObject> _hiddenHudObjects = new List<GameObject>();

    private void OnEnable()
    {
      ResolveNetworkManagerInHierarchy();
      TrySubscribeServerConnectionState();
    }

    private void OnDisable()
    {
      UnsubscribeServerConnectionState();
    }

    /// <summary>
    /// Tries to resolve a NetworkManager from this object first, then scene hierarchy.
    /// </summary>
    public bool ResolveNetworkManagerInHierarchy()
    {
      if (!networkManager.IsUnityNull())
      {
        TrySubscribeServerConnectionState();
        return true;
      }

      networkManager = GetComponent<NetworkManager>();
      if (!networkManager.IsUnityNull())
      {
        TrySubscribeServerConnectionState();
        return true;
      }

      networkManager = FindAnyObjectByType<NetworkManager>();
      if (networkManager.IsUnityNull())
        return false;

      TrySubscribeServerConnectionState();
      return true;
    }

    public void SetNetworkHudCanvasVisible(bool visible)
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (!visible)
      {
        HideNetworkHudCanvases();
        return;
      }

      RestoreHiddenNetworkHudCanvases();
    }

    public void ConfigureTransport(SessionInformationModel sessionInformation)
    {
      if (sessionInformation == null)
        return;

      if (!ResolveNetworkManagerInHierarchy())
        return;

      var transport = networkManager.TransportManager?.Transport;
      if (transport == null)
      {
        Debug.LogWarning("[FishNetSupport] No transport found on NetworkManager.");
        return;
      }

      transport.SetClientAddress(sessionInformation.Address);
      transport.SetPort(sessionInformation.Port);

      Debug.Log($"[FishNetSupport] Transport configured for {sessionInformation.Address}:{sessionInformation.Port}");
    }

    /// <summary>
    /// 서버 소켓이 바인딩할 주소를 설정합니다.
    /// 데디케이티드 서버는 클라이언트 접속 주소가 아니라 이 값으로 수신 인터페이스를 결정합니다.
    /// </summary>
    public void ConfigureServerBindAddress(string bindAddress)
    {
      if (string.IsNullOrWhiteSpace(bindAddress))
        return;

      if (!ResolveNetworkManagerInHierarchy())
        return;

      var transport = networkManager.TransportManager?.Transport;
      if (transport == null)
      {
        Debug.LogWarning("[FishNetSupport] No transport found on NetworkManager.");
        return;
      }

      var addressType = bindAddress.Contains(":") ? IPAddressType.IPv6 : IPAddressType.IPv4;
      transport.SetServerBindAddress(bindAddress, addressType);

      Debug.Log($"[FishNetSupport] Server bind address configured to {bindAddress} ({addressType}).");
    }

    public void PrepareDeferredPlayerSpawning()
    {
      if (_deferredPlayerSpawningPrepared)
        return;

      if (!ResolveNetworkManagerInHierarchy())
        return;

      RegisterSceneSpawnPointProviders();

      var playerSpawners = networkManager.GetComponentsInChildren<PlayerSpawner>(true);
      if (playerSpawners == null || playerSpawners.Length == 0)
      {
        Debug.LogWarning("[FishNetSupport] No PlayerSpawner found for deferred spawning setup.");
        return;
      }

      var requiredSpawnIdentifier = ResolveRequiredSpawnIdentifier();
      ConfigurePlayerSpawnerSpawns(playerSpawners, requiredSpawnIdentifier);
      EnsurePlayerPrefabAssigned(playerSpawners);

      for (int i = 0; i < playerSpawners.Length; i++)
      {
        var playerSpawner = playerSpawners[i];
        if (playerSpawner == null)
          continue;

        var deferredSpawner = playerSpawner.GetComponent<FishNetDeferredPlayerSpawner>();
        if (deferredSpawner == null)
          deferredSpawner = playerSpawner.gameObject.AddComponent<FishNetDeferredPlayerSpawner>();

        deferredSpawner.ConfigureFromPlayerSpawner(playerSpawner);
        deferredSpawner.SetRequiredSpawnIdentifier(requiredSpawnIdentifier);
        FishNetDeferredPlayerSpawner.DisableLegacyPlayerSpawner(playerSpawner);
      }

      _deferredPlayerSpawningPrepared = true;
      Debug.Log($"[FishNetSupport] Deferred player spawning prepared for {playerSpawners.Length} PlayerSpawner(s).");
    }

    /// <summary>
    /// Additive 로드된 시스템/백엔드 씬(SystemOverlayScene 등)의 공유 Scene NetworkObject가
    /// 모든 클라이언트에게 관측되도록, 접속하는 각 Connection을 해당 씬들에 등록하는 바인더를 준비한다.
    /// 이 처리가 없으면 ChatService 등 non-global Scene NetworkObject가 원격 클라이언트에서 스폰되지 않아
    /// Chat/Command Service가 동작하지 않는다.
    /// </summary>
    public void PrepareSystemSceneObserverBinding()
    {
      if (_systemSceneObserverPrepared)
        return;

      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_systemSceneObserverBinder == null)
      {
        _systemSceneObserverBinder = networkManager.GetComponentInChildren<SystemSceneObserverBinder>(true);
        if (_systemSceneObserverBinder == null)
          _systemSceneObserverBinder = networkManager.gameObject.AddComponent<SystemSceneObserverBinder>();
      }

      _systemSceneObserverBinder.Configure(networkManager);

      _systemSceneObserverPrepared = true;
      Debug.Log("[FishNetSupport] System scene observer binding prepared.");
    }

    private void TrySubscribeServerConnectionState()
    {
      if (_serverConnectionStateSubscribed)
        return;

      if (networkManager.IsUnityNull() || networkManager.ServerManager == null)
        return;

      networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
      networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
      _serverConnectionStateSubscribed = true;

      TrySubscribeRemoteConnectionState();
    }

    private void UnsubscribeServerConnectionState()
    {
      if (!_serverConnectionStateSubscribed)
        return;

      if (!networkManager.IsUnityNull() && networkManager.ServerManager != null)
        networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;

      _serverConnectionStateSubscribed = false;

      UnsubscribeRemoteConnectionState();
    }

    private void TrySubscribeRemoteConnectionState()
    {
      if (_remoteConnectionStateSubscribed)
        return;

      if (networkManager.IsUnityNull() || networkManager.ServerManager == null)
        return;

      networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;
      networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;
      _remoteConnectionStateSubscribed = true;
    }

    private void UnsubscribeRemoteConnectionState()
    {
      if (!_remoteConnectionStateSubscribed)
        return;

      if (!networkManager.IsUnityNull() && networkManager.ServerManager != null)
        networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;

      _remoteConnectionStateSubscribed = false;
    }

    /// <summary>
    /// 원격 클라이언트의 연결 상태 변경을 처리한다.
    /// 커넥션 게이트가 닫혀 있으면 새로운 외부 접속을 즉시 거부한다.
    /// 이미 접속한 클라이언트에게는 영향을 주지 않는다.
    /// </summary>
    private void ServerManager_OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
    {
      if (args.ConnectionState != RemoteConnectionState.Started)
        return;

      if (ConnectionGateService.IsOpen)
        return;

      // 게이트가 닫혀 있으므로 새로운 외부 접속을 거부한다.
      Debug.Log($"[FishNetSupport] 커넥션 게이트가 닫혀 있어 ClientId {conn.ClientId}의 접속을 거부합니다.");
      conn.Kick(KickReason.UnexpectedProblem, LoggingType.Common,
        $"Connection {conn.ClientId} rejected: connection gate is closed.");
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Started)
      {
        // 새 서버 세션 시작: 이전 세션에서 남은 StaticPlacedItem 상태를 초기화한다.
        // static Dictionary는 도메인 리로드 없이는 유지되므로 명시적 초기화가 필요하다.
        StaticPlacedItemService.ClearAll();
        StaticObjectDisplaymentService.ClearAll();
        ConnectionGateService.ResetState();
        MultiplayerInfrastructure.Variable.SessionVariableService.ClearSessionState();

        _deferredPlayerSpawningPrepared = false;
        PrepareDeferredPlayerSpawning();

        _systemSceneObserverPrepared = false;
        PrepareSystemSceneObserverBinding();
        return;
      }

      if (args.ConnectionState == LocalConnectionState.Stopped)
      {
        // 서버 세션 종료: 상태를 정리하여 다음 세션이 깨끗한 상태로 시작하도록 한다.
        StaticPlacedItemService.ClearAll();
        StaticObjectDisplaymentService.ClearAll();
        ConnectionGateService.ResetState();
        MultiplayerInfrastructure.Variable.SessionVariableService.ClearSessionState();
        _deferredPlayerSpawningPrepared = false;
        _systemSceneObserverPrepared = false;
      }
    }

    private static void RegisterSceneSpawnPointProviders()
    {
      var providers = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
      for (int i = 0; i < providers.Length; i++)
      {
        if (providers[i] is not IPlayerSpawnPointProvider provider)
          continue;

        PlayerSpawnPointRegistry.Register(provider);
      }
    }

    private static string ResolveRequiredSpawnIdentifier()
    {
      var identifier = Registry.Registry.Get<string>(RegistryType.RuntimeState, RegistryGlobalKeys.DefaultCommonSpawnPoint);
      return string.IsNullOrWhiteSpace(identifier) ? FallbackSpawnIdentifier : identifier;
    }

    /// <summary>
    /// PlayerSpawner의 <c>_playerPrefab</c>이 null이면 자동으로 할당합니다.
    ///
    /// 할당 순서:
    ///   1. <see cref="fallbackPlayerPrefab"/> (인스펙터에서 할당된 fallback)
    ///   2. <c>Resources/Player</c> 경로 로드
    ///
    /// <see cref="PlayerSpawner.SetPlayerPrefab"/> 공개 API를 사용하여 할당하므로,
    /// 이후 <see cref="FishNetDeferredPlayerSpawner.ConfigureFromPlayerSpawner"/>에서
    /// 리플렉션으로 읽을 때 올바른 값이 반환됩니다.
    /// </summary>
    private void EnsurePlayerPrefabAssigned(PlayerSpawner[] playerSpawners)
    {
      if (playerSpawners == null || playerSpawners.Length == 0)
        return;

      NetworkObject resolvedFallback = null;

      for (int i = 0; i < playerSpawners.Length; i++)
      {
        var spawner = playerSpawners[i];
        if (spawner == null)
          continue;

        // 리플렉션으로 현재 _playerPrefab 확인
        var currentPrefab = PlayerSpawnerPrefabField?.GetValue(spawner) as NetworkObject;
        if (currentPrefab != null)
          continue;

        // Fallback 해결 (지연 — 첫 번째로 필요한 시점에 한 번만)
        if (resolvedFallback == null)
          resolvedFallback = ResolveFallbackPlayerPrefab();

        if (resolvedFallback == null)
        {
          Debug.LogWarning(
            "[FishNetSupport] PlayerSpawner에 Player 프리팹이 할당되지 않았고, " +
            "fallback 프리팹도 찾을 수 없습니다. " +
            "FishNetSupport 인스펙터의 fallbackPlayerPrefab을 할당하거나, " +
            "Player 프리팹을 Resources/Player 경로에 배치하세요.");
          return;
        }

        spawner.SetPlayerPrefab(resolvedFallback);
        Debug.Log($"[FishNetSupport] PlayerSpawner '{spawner.gameObject.name}'의 " +
                  $"Player 프리팹을 자동으로 할당했습니다: {resolvedFallback.name}");
      }
    }

    private NetworkObject ResolveFallbackPlayerPrefab()
    {
      // 1. 인스펙터에서 할당된 fallback
      if (fallbackPlayerPrefab != null)
        return fallbackPlayerPrefab;

      // 2. Resources/Player 경로 로드
      var loaded = Resources.Load<NetworkObject>("Player");
      if (loaded != null)
      {
        Debug.Log("[FishNetSupport] Resources/Player에서 Player 프리팹을 로드했습니다.");
        return loaded;
      }

      return null;
    }

    private static void ConfigurePlayerSpawnerSpawns(PlayerSpawner[] playerSpawners, string spawnIdentifier)
    {
      if (playerSpawners == null || playerSpawners.Length == 0)
        return;

      if (!PlayerSpawnPointRegistry.TryGet(spawnIdentifier, out var spawnTransform))
      {
        for (int i = 0; i < playerSpawners.Length; i++)
        {
          if (playerSpawners[i] == null)
            continue;

          playerSpawners[i].Spawns = System.Array.Empty<Transform>();
        }

        Debug.LogWarning($"[FishNetSupport] SpawnPoint '{spawnIdentifier}' is not available. PlayerSpawner.Spawns will remain empty.");
        return;
      }

      for (int i = 0; i < playerSpawners.Length; i++)
      {
        if (playerSpawners[i] == null)
          continue;

        playerSpawners[i].Spawns = new[] { spawnTransform };
      }

      Debug.Log($"[FishNetSupport] PlayerSpawner.Spawns configured to '{spawnIdentifier}' at {spawnTransform.position}.");
    }

    public void StartServer()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_serverStateAssumed == LocalConnectionState.Started)
      {
        Debug.LogWarning("[FishNetSupport] StartServer called but server is already started.");
        return;
      }

      networkManager.ServerManager.StartConnection();
      _serverStateAssumed = LocalConnectionState.Started;
    }

    public void StopServer()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_serverStateAssumed == LocalConnectionState.Stopped)
      {
        Debug.LogWarning("[FishNetSupport] StopServer called but server is already stopped.");
        return;
      }

      networkManager.ServerManager.StopConnection(true);
      _serverStateAssumed = LocalConnectionState.Stopped;
    }

    public void StartClient()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_clientStateAssumed == LocalConnectionState.Started)
      {
        Debug.LogWarning("[FishNetSupport] StartClient called but client is already started.");
        return;
      }

      networkManager.ClientManager.StartConnection();
      _clientStateAssumed = LocalConnectionState.Started;
    }

    public void StopClient()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      if (_clientStateAssumed == LocalConnectionState.Stopped)
      {
        Debug.LogWarning("[FishNetSupport] StopClient called but client is already stopped.");
        return;
      }

      networkManager.ClientManager.StopConnection();
      _clientStateAssumed = LocalConnectionState.Stopped;
    }

    private void HideNetworkHudCanvases()
    {
      if (!ResolveNetworkManagerInHierarchy())
        return;

      var huds = networkManager.GetComponentsInChildren<NetworkHudCanvases>(true);
      foreach (var hud in huds)
      {
        var hudObject = hud.gameObject;
        if (!hudObject.activeSelf)
          continue;

        hudObject.SetActive(false);
        _hiddenHudObjects.Add(hudObject);
      }
    }

    private void RestoreHiddenNetworkHudCanvases()
    {
      foreach (var hudObject in _hiddenHudObjects)
      {
        if (hudObject.IsUnityNull())
          continue;

        hudObject.SetActive(true);
      }

      _hiddenHudObjects.Clear();
    }
  }
}
