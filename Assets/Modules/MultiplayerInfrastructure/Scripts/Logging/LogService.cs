using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Logging
{
  /// <summary>
  /// 로그 서비스의 생명주기를 관리하는 MonoBehaviour.
  ///
  /// 씬에 배치하면 FishNet 서버/클라이언트 연결 상태에 따라
  /// <see cref="GameLogService"/>를 자동으로 초기화·종료한다.
  ///
  /// ## 동작
  /// - 서버 시작(Started) → context="server"로 GameLogService 초기화
  /// - 클라이언트 시작(Started) → context="client:{uuid}"로 GameLogService 초기화
  ///   (단, 서버와 같은 프로세스인 호스트는 서버 컨텍스트를 우선한다)
  /// - 서버/클라이언트 중단(Stopped) → GameLogService 종료
  /// - OnApplicationQuit → 안전하게 Shutdown 호출
  ///
  /// ## 씬 배치
  /// 씬의 적절한 오브젝트에 컴포넌트로 추가하거나, NetworkManager 오브젝트에 함께 붙인다.
  /// </summary>
  public class LogService : MonoBehaviour
  {
    private NetworkManager _networkManager;
    private bool _serverStarted;
    private bool _clientStarted;

    private void Awake()
    {
      _networkManager = GetComponent<NetworkManager>();
      if (_networkManager == null)
        _networkManager = FindAnyObjectByType<NetworkManager>();
    }

    private void OnEnable()
    {
      if (_networkManager == null)
        _networkManager = FindAnyObjectByType<NetworkManager>();

      if (_networkManager != null)
      {
        _networkManager.ServerManager.OnServerConnectionState += OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState += OnClientConnectionState;
      }
    }

    private void OnDisable()
    {
      if (_networkManager != null)
      {
        _networkManager.ServerManager.OnServerConnectionState -= OnServerConnectionState;
        _networkManager.ClientManager.OnClientConnectionState -= OnClientConnectionState;
      }
    }

    private void OnApplicationQuit()
    {
      if (GameLogService.IsInitialized)
      {
        GameLogService.WriteSystem("Application quit.");
        GameLogService.Shutdown();
      }
    }

    // ── FishNet 이벤트 ────────────────────────────────────────────────────────

    private void OnServerConnectionState(ServerConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Started)
      {
        _serverStarted = true;

        // 서버 시작: 이미 초기화된 경우(재시작 등) StreamWriter를 먼저 안전하게 닫는다.
        if (GameLogService.IsInitialized)
        {
          GameLogService.WriteSystem("Server restarting — closing current log session.");
          GameLogService.Shutdown();
        }

        // 새 세션 UUID 생성 후 로그 서비스 초기화
        GameSessionService.Reset();
        GameLogService.Initialize("server");

        GameLogService.WriteSystem($"Server started. session-{GameSessionService.SessionId}");
        Debug.Log($"[LogService] Server started, log context=server.");
        return;
      }

      if (args.ConnectionState == LocalConnectionState.Stopped)
      {
        _serverStarted = false;
        if (!_clientStarted)
        {
          // 클라이언트도 중단된 경우에만 완전 종료
          GameLogService.WriteSystem("Server stopped. Session ended.");
          GameLogService.Shutdown();
        }
        else
        {
          GameLogService.WriteSystem("Server stopped (client still running).");
        }
      }
    }

    private void OnClientConnectionState(ClientConnectionStateArgs args)
    {
      if (args.ConnectionState == LocalConnectionState.Started)
      {
        _clientStarted = true;

        if (!GameLogService.IsInitialized)
        {
          // 순수 클라이언트: 로컬 세션 UUID로 초기화
          GameSessionService.EnsureInitialized();
          string ownerId = GameSessionService.SessionId;

          // 로컬 플레이어 UUID가 있으면 컨텍스트에 포함
          string context = $"client:{ownerId[..8]}"; // UUID 앞 8자만 사용
          GameLogService.Initialize(context);
          GameLogService.WriteSystem($"Client connected. session-{GameSessionService.SessionId}");
          Debug.Log($"[LogService] Client started, log context={context}.");
        }
        else if (!_serverStarted)
        {
          // 서버가 없는 순수 클라이언트인데 이미 초기화된 경우
          GameLogService.WriteSystem("Client connection started.");
        }
        return;
      }

      if (args.ConnectionState == LocalConnectionState.Stopped)
      {
        _clientStarted = false;
        if (!_serverStarted)
        {
          GameLogService.WriteSystem("Client disconnected. Session ended.");
          GameLogService.Shutdown();
        }
        else
        {
          GameLogService.WriteSystem("Client disconnected (server still running).");
        }
      }
    }
  }
}
