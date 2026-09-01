using System.Reflection;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
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

    private static readonly MethodInfo PlayerSpawnerOnDestroyMethod =
      typeof(PlayerSpawner).GetMethod("OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);

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

    public void SetRequiredSpawnIdentifier(string identifier)
    {
      if (string.IsNullOrWhiteSpace(identifier))
        return;

      _requiredSpawnIdentifier = identifier;
      TrySpawnWaitingClients();
    }

    public static void DisableLegacyPlayerSpawner(PlayerSpawner source)
    {
      if (source == null)
        return;

      // PlayerSpawner 는 Awake 에서 구독하고 OnDestroy 에서만 해제한다.
      // 여기서 OnDestroy 를 호출하면 FishNet 소스를 수정하지 않고도 스폰 콜백을 뗄 수 있다.
      PlayerSpawnerOnDestroyMethod?.Invoke(source, null);
      source.enabled = false;
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
      if (!ResolveNetworkManager())
        return;

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
      {
        Debug.Log($"[FishNetDeferredPlayerSpawner] SpawnPoint '{_requiredSpawnIdentifier}' is not available yet.");
        return;
      }

      var spawned = _networkManager.GetPooledInstantiated(_playerPrefab, spawnTransform.position, spawnTransform.rotation, true);
      _networkManager.ServerManager.Spawn(spawned, conn);

      Debug.Log($"[FishNetDeferredPlayerSpawner] Spawned client {conn.ClientId} at '{_requiredSpawnIdentifier}' ({spawnTransform.position}).");

      if (_addToDefaultScene)
        _networkManager.SceneManager.AddOwnerToDefaultScene(spawned);
    }
  }
}
