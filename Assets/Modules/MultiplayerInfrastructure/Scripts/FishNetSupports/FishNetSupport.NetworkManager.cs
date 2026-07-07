using System.Collections.Generic;
using FishNet.Component.Spawning;
using FishNet.Example;
using FishNet.Managing;
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
    private LocalConnectionState _serverStateAssumed = LocalConnectionState.Stopped;
    private LocalConnectionState _clientStateAssumed = LocalConnectionState.Stopped;
    private bool _deferredPlayerSpawningPrepared;
    private bool _serverConnectionStateSubscribed;
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

    private void TrySubscribeServerConnectionState()
    {
      if (_serverConnectionStateSubscribed)
        return;

      if (networkManager.IsUnityNull() || networkManager.ServerManager == null)
        return;

      networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;
      networkManager.ServerManager.OnServerConnectionState += ServerManager_OnServerConnectionState;
      _serverConnectionStateSubscribed = true;
    }

    private void UnsubscribeServerConnectionState()
    {
      if (!_serverConnectionStateSubscribed)
        return;

      if (!networkManager.IsUnityNull() && networkManager.ServerManager != null)
        networkManager.ServerManager.OnServerConnectionState -= ServerManager_OnServerConnectionState;

      _serverConnectionStateSubscribed = false;
    }

    private void ServerManager_OnServerConnectionState(ServerConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Started)
      {
        // 새 서버 세션 시작: 이전 세션에서 남은 StaticPlacedItem 상태를 초기화한다.
        // static Dictionary는 도메인 리로드 없이는 유지되므로 명시적 초기화가 필요하다.
        StaticPlacedItemService.ClearAll();

        _deferredPlayerSpawningPrepared = false;
        PrepareDeferredPlayerSpawning();
        return;
      }

      if (args.ConnectionState == LocalConnectionState.Stopped)
      {
        // 서버 세션 종료: 상태를 정리하여 다음 세션이 깨끗한 상태로 시작하도록 한다.
        StaticPlacedItemService.ClearAll();
        _deferredPlayerSpawningPrepared = false;
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
