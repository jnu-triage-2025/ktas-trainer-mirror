using FishNet.Connection;
using FishNet.Component.Spawning;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using System.Reflection;
using UnityEngine;

namespace MultiplayerInfrastructure.FishNetSupports
{
  [DisallowMultipleComponent]
  public sealed class FishNetDeferredPlayerSpawner : MonoBehaviour
  {
    private static readonly FieldInfo PlayerSpawnerPrefabField =
      typeof(PlayerSpawner).GetField("_playerPrefab", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo PlayerSpawnerAddToDefaultSceneField =
      typeof(PlayerSpawner).GetField("_addToDefaultScene", BindingFlags.Instance | BindingFlags.NonPublic);

    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private NetworkObject _playerPrefab;
    [SerializeField] private bool _addToDefaultScene = true;
    [SerializeField] private string _requiredSpawnIdentifier = "spawnpoint-commons";

    private void Awake()
    {
      ResolveNetworkManager();
    }

    private void OnEnable()
    {
      TrySubscribe();
      PlayerSpawnPointRegistry.Changed += HandleSpawnPointsChanged;
    }

    private void OnDisable()
    {
      PlayerSpawnPointRegistry.Changed -= HandleSpawnPointsChanged;
      Unsubscribe();
    }

    private void HandleSpawnPointsChanged()
    {
      TrySpawnWaitingClients();
    }

    private void TrySubscribe()
    {
      if (!ResolveNetworkManager())
        return;

      _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
      _networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;

      _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
      _networkManager.SceneManager.OnLoadEnd += SceneManager_OnLoadEnd;
    }

    private void Unsubscribe()
    {
      if (_networkManager == null)
        return;

      _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
      _networkManager.SceneManager.OnLoadEnd -= SceneManager_OnLoadEnd;
    }

    private bool ResolveNetworkManager()
    {
      if (_networkManager != null)
        return true;

      _networkManager = GetComponentInParent<NetworkManager>();
      if (_networkManager != null)
        return true;

      _networkManager = FindAnyObjectByType<NetworkManager>();
      return _networkManager != null;
    }

    public void ConfigureFromPlayerSpawner(PlayerSpawner source)
    {
      if (source == null)
        return;

      if (_networkManager == null)
        _networkManager = source.GetComponentInParent<NetworkManager>();

      if (_playerPrefab == null && PlayerSpawnerPrefabField != null)
      {
        _playerPrefab = PlayerSpawnerPrefabField.GetValue(source) as NetworkObject;
      }

      if (PlayerSpawnerAddToDefaultSceneField != null)
      {
        var value = PlayerSpawnerAddToDefaultSceneField.GetValue(source);
        if (value is bool addToDefaultScene)
          _addToDefaultScene = addToDefaultScene;
      }
    }

    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
      if (!asServer)
        return;

      TrySpawnForConnection(conn);
    }

    private void SceneManager_OnLoadEnd(SceneLoadEndEventArgs _)
    {
      TrySpawnWaitingClients();
    }

    private void TrySpawnWaitingClients()
    {
      if (!ResolveNetworkManager())
        return;

      var clients = _networkManager.ServerManager?.Clients;
      if (clients == null)
        return;

      foreach (var pair in clients)
      {
        TrySpawnForConnection(pair.Value);
      }
    }

    private void TrySpawnForConnection(NetworkConnection conn)
    {
      if (conn == null || !conn.IsValid)
        return;

      if (conn.FirstObject != null)
        return;

      if (_playerPrefab == null)
      {
        Debug.LogWarning("[FishNetDeferredPlayerSpawner] Player prefab is not assigned.");
        return;
      }

      if (!PlayerSpawnPointRegistry.TryGet(_requiredSpawnIdentifier, out var spawnTransform))
        return;

      var spawned = _networkManager.GetPooledInstantiated(_playerPrefab, spawnTransform.position, spawnTransform.rotation, true);
      _networkManager.ServerManager.Spawn(spawned, conn);

      if (_addToDefaultScene)
        _networkManager.SceneManager.AddOwnerToDefaultScene(spawned);
    }
  }
}
