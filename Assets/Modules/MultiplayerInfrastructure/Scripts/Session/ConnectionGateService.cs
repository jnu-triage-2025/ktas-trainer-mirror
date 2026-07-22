using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Logging;
using UnityEngine;

namespace MultiplayerInfrastructure.Session
{
  /// <summary>
  /// 서버의 외부 접속 허용/불가(커넥션 게이트) 상태를 관리하는 정적 서비스.
  /// 직렬화하지 않으며, 런타임 중에만 유효하다.
  ///
  /// - 게이트가 닫히면 새로운 외부 접속을 거부하고 LAN 브로드캐스트를 중단한다.
  /// - 게이트가 열리면 "로컬 포트 OOO에 서버가 개방되었습니다." 메시지를
  ///   인게임 채팅과 로그로 발송하고, LAN 브로드캐스트를 재개한다.
  /// - 이미 접속한 클라이언트의 연결은 끊지 않는다.
  /// </summary>
  public static class ConnectionGateService
  {
    private const string LogPrefix = "[ConnectionGate]";

    private static bool _isOpen = true;
    private static ushort _port;

    // LAN 브로드캐스트 재개를 위해 저장하는 설정.
    private static string _lanSessionName;
    private static int _lanGamePort;
    private static bool _lanBroadcastConfigured;

    /// <summary>외부 접속 허용 여부. 기본값은 true(허용).</summary>
    public static bool IsOpen => _isOpen;

    /// <summary>현재 서버 포트.</summary>
    public static ushort Port => _port;

    /// <summary>
    /// 서버 포트를 설정한다. 부트스트래퍼가 세션 시작 시 호출한다.
    /// </summary>
    public static void SetPort(ushort port)
    {
      _port = port;
    }

    /// <summary>
    /// LAN 브로드캐스트 재개에 필요한 설정을 저장한다.
    /// IngameSceneBootstrapper가 LAN 디스커버리를 시작할 때 호출한다.
    /// </summary>
    public static void SetLanBroadcastConfig(string sessionName, int gamePort)
    {
      _lanSessionName = sessionName;
      _lanGamePort = gamePort;
      _lanBroadcastConfigured = true;
    }

    /// <summary>
    /// 외부 접속을 허용 상태로 전환한다.
    /// 닫힌 상태에서 열린 상태로 전환될 때 채팅/로그 알림과 LAN 브로드캐스트를 재개한다.
    /// 이미 열린 상태이면 아무 동작도 하지 않는다.
    /// </summary>
    public static void Open()
    {
      if (_isOpen)
        return;

      _isOpen = true;

      string message = $"로컬 포트 {_port}에 서버가 개방되었습니다.";

      // 인게임 채팅 브로드캐스트
      var chatService = Object.FindAnyObjectByType<ChatService>();
      chatService?.BroadcastSystemMessage(message);

      // 로그 기록
      GameLogService.WriteSystem(message);
      Debug.Log($"{LogPrefix} {message}");

      // LAN 브로드캐스트 재개
      if (_lanBroadcastConfigured)
      {
        var discovery = LanDiscoveryService.Instance;
        if (discovery != null)
        {
          discovery.StartBroadcast(_lanSessionName, _lanGamePort);
          Debug.Log($"{LogPrefix} LAN 브로드캐스트를 재개했습니다.");
        }
      }
    }

    /// <summary>
    /// 외부 접속을 불가 상태로 전환한다.
    /// 열린 상태에서 닫힌 상태로 전환될 때 LAN 브로드캐스트를 중단한다.
    /// 이미 닫힌 상태이면 아무 동작도 하지 않는다.
    /// 이미 접속한 클라이언트의 연결은 유지된다.
    /// </summary>
    public static void Close()
    {
      if (!_isOpen)
        return;

      _isOpen = false;

      // LAN 브로드캐스트 중단
      LanDiscoveryService.Instance?.StopBroadcast();
      Debug.Log($"{LogPrefix} 외부 접속이 차단되었습니다. LAN 브로드캐스트를 중단했습니다.");

      GameLogService.WriteSystem("외부 접속이 차단되었습니다.");
    }

    /// <summary>
    /// 세션 종료 시 상태를 초기화한다. 다음 세션에서 깨끗한 상태로 시작하도록 한다.
    /// </summary>
    public static void ResetState()
    {
      _isOpen = true;
      _port = 0;
      _lanSessionName = null;
      _lanGamePort = 0;
      _lanBroadcastConfigured = false;
    }
  }
}
