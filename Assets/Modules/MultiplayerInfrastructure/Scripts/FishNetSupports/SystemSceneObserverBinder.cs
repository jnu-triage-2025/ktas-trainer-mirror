using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.FishNetSupports
{
  /// <summary>
  /// IngameScene가 SystemOverlayScene/OverworldScene을 Unity SceneManager로 Additive 로드하기 때문에,
  /// 이들 씬은 FishNet의 SceneManager 로드 파이프라인이나 Global Scene 등록을 거치지 않는다.
  ///
  /// FishNet은 non-global Scene NetworkObject를, 해당 씬에 연결(Connection)이 등록(AddConnectionToScene)된
  /// 클라이언트에게만 Observer로 스폰한다. 그런데 이 프로젝트는 FishNet Scene 관리를 사용하지 않으므로
  /// (Global Scene도 없고, 클라이언트는 start scene으로 빈 브로드캐스트를 보낸다) 원격 클라이언트의 Connection은
  /// SystemOverlayScene에 절대 등록되지 않는다.
  ///
  /// 그 결과 SystemOverlayScene에 배치된 ChatService(및 다른 백엔드 Scene NetworkObject)는 호스트에서만
  /// 스폰되고 원격 클라이언트에서는 스폰/관측되지 않아, ServerRpc/ObserversRpc가 동작하지 않는다
  /// (= Chat/Command Service가 동작하지 않는 증상).
  ///
  /// 이 컴포넌트는 각 클라이언트가 start scene 로드를 마치는 시점(OnClientLoadedStartScenes)에,
  /// 지정된 시스템/백엔드 씬들에 해당 Connection을 등록하여 공유 Scene NetworkObject들이 모든
  /// 클라이언트에게 관측되도록 한다. 서버(호스트)에서만 동작한다.
  /// </summary>
  [DisallowMultipleComponent]
  public sealed class SystemSceneObserverBinder : MonoBehaviour
  {
    /// <summary>
    /// 공유 Scene NetworkObject가 위치한, Connection을 강제로 등록할 씬 이름 목록.
    /// IngameScene이 Additive로 로드하는 씬들과 일치한다.
    /// </summary>
    private static readonly string[] DefaultSystemSceneNames =
    {
      "SystemOverlayScene",
      "OverworldScene",
    };

    [SerializeField] private NetworkManager _networkManager;
    [SerializeField] private string[] _systemSceneNames = DefaultSystemSceneNames;

    private bool _subscribed;

    private void Awake()
    {
      ResolveNetworkManager();
    }

    private void OnEnable()
    {
      TrySubscribe();
    }

    private void OnDisable()
    {
      Unsubscribe();
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

    public void Configure(NetworkManager networkManager, string[] systemSceneNames = null)
    {
      if (networkManager != null)
        _networkManager = networkManager;

      if (systemSceneNames != null && systemSceneNames.Length > 0)
        _systemSceneNames = systemSceneNames;

      TrySubscribe();
      // 이미 접속을 마친 클라이언트가 있을 수 있으므로 즉시 한 번 바인딩을 시도한다.
      BindAllConnectedClients();
    }

    private void TrySubscribe()
    {
      if (_subscribed)
        return;

      if (!ResolveNetworkManager() || _networkManager.SceneManager == null)
        return;

      _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;
      _networkManager.SceneManager.OnClientLoadedStartScenes += SceneManager_OnClientLoadedStartScenes;
      _subscribed = true;
    }

    private void Unsubscribe()
    {
      if (!_subscribed)
        return;

      if (_networkManager != null && _networkManager.SceneManager != null)
        _networkManager.SceneManager.OnClientLoadedStartScenes -= SceneManager_OnClientLoadedStartScenes;

      _subscribed = false;
    }

    private void SceneManager_OnClientLoadedStartScenes(NetworkConnection conn, bool asServer)
    {
      // 씬-Connection 등록(관측자 재구성)은 서버 권한 작업이므로 서버 쪽에서만 수행한다.
      if (!asServer)
        return;

      BindConnection(conn);
    }

    private void BindAllConnectedClients()
    {
      if (!ResolveNetworkManager())
        return;

      var clients = _networkManager.ServerManager?.Clients;
      if (clients == null)
        return;

      foreach (var pair in clients)
        BindConnection(pair.Value);
    }

    private void BindConnection(NetworkConnection conn)
    {
      if (conn == null || !conn.IsValid)
        return;

      if (!ResolveNetworkManager() || _networkManager.SceneManager == null)
        return;

      foreach (var scene in EnumerateSystemScenes())
      {
        // 이미 등록된 씬이면 건너뛴다.
        if (conn.Scenes.Contains(scene))
          continue;

        _networkManager.SceneManager.AddConnectionToScene(conn, scene);
      }
    }

    private IEnumerable<Scene> EnumerateSystemScenes()
    {
      if (_systemSceneNames == null)
        yield break;

      for (int i = 0; i < _systemSceneNames.Length; i++)
      {
        var sceneName = _systemSceneNames[i];
        if (string.IsNullOrWhiteSpace(sceneName))
          continue;

        var scene = SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid() && scene.isLoaded)
          yield return scene;
      }
    }
  }
}
