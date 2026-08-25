using System.Collections;
using System.Net;
using System.Net.Sockets;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.UI;
using TriageTrainer.MultiplayerInfrastructureSupports;
using TriageTrainer.Scenario;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrainer.SceneBootstrapper
{
  /// <summary>
  /// IndevScene용 자동 부트스트래퍼입니다.
  /// IntroScene 흐름 없이 씬을 직접 재생하면, 자동으로 호스트(서버+클라이언트) 세션을 시작하고
  /// SystemOverlayScene만 애디티브 로드합니다.
  /// 이후 프리팹 게임 오브젝트로 씬에 배치하여 사용합니다.
  ///
  /// IndevScene은 자체 월드/스폰포인트를 갖춘 개발 씬이므로 OverworldScene을 로드하지 않습니다.
  /// OverworldScene을 함께 로드하면 동명 스폰포인트(spawnpoint-commons)가 PlayerSpawnPointRegistry에서
  /// IndevScene 스폰포인트를 덮어써 플레이어가 OverworldScene 위로 스폰되고, 월드 콘텐츠가 중복됩니다.
  /// </summary>
  [DefaultExecutionOrder(-1000)]
  public class IndevSceneBootstrapper : MonoBehaviour
  {
    private const string LogPrefix = "[IndevSceneBootstrapper]";
    private const string SystemOverlaySceneName = "SystemOverlayScene";
    private const string ConnectionFailureSceneName = "NetworkSessionFailureScene";

    [Header("Session")]
    [SerializeField] private string address = "127.0.0.1";
    [SerializeField] private ushort port = 37891;
    [SerializeField] private string sessionName = "IndevSession";

    [Header("Additive Scenes")]
    [SerializeField] private bool loadSystemOverlayScene = true;

    [Header("TriageTrainer Integration")]
    [SerializeField] private bool ensureTriageSupportsOnBootstrapObject = true;

    private bool _bootstrapped;
    private IndevConnectionFailureOverlay _connectionFailureOverlay;

    private void Start()
    {
      if (_bootstrapped)
        return;

      _bootstrapped = true;
      EnsureTriageSupports();
      StartCoroutine(BootstrapRoutine());
    }

    private void EnsureTriageSupports()
    {
      if (!ensureTriageSupportsOnBootstrapObject)
        return;

      if (GetComponent<RegisteringMultiplayerInfrastructureSupport>() == null)
      {
        gameObject.AddComponent<RegisteringMultiplayerInfrastructureSupport>();
      }

      if (GetComponent<TriageScenarioEventBootstrap>() == null)
      {
        gameObject.AddComponent<TriageScenarioEventBootstrap>();
      }
    }

    private IEnumerator BootstrapRoutine()
    {
      using (LoadingScreen.Begin())
      {
        // Hide FishNet logo/HUD as early as possible, before any scene loading.
        HideFishNetHud();

        if (loadSystemOverlayScene)
        {
          yield return LoadSceneIfNeeded(SystemOverlaySceneName);
        }

        yield return LoadSceneIfNeeded(ConnectionFailureSceneName);
        _connectionFailureOverlay = FindAnyObjectByType<IndevConnectionFailureOverlay>();
        if (_connectionFailureOverlay == null)
        {
          Debug.LogError($"{LogPrefix} Required scene '{ConnectionFailureSceneName}' or its overlay could not be loaded. Network startup was cancelled.");
          yield break;
        }

        // Ensure newly-loaded scene objects complete Awake/OnEnable before networking starts.
        yield return null;

        PrepareDeferredPlayerSpawning();
        StartSessionOrConnectToExistingServer();

      }
    }

    private static void HideFishNetHud()
    {
      var fishNetSupport = FishNetSupport.Instance ?? FindAnyObjectByType<FishNetSupport>();
      fishNetSupport?.SetNetworkHudCanvasVisible(false);
    }

    private void StartSessionOrConnectToExistingServer()
    {
      _connectionFailureOverlay.BeginConnectionAttempt(address, port);

      var fishNetSupport = FishNetSupport.Instance ?? FindAnyObjectByType<FishNetSupport>();
      if (fishNetSupport == null)
      {
        _connectionFailureOverlay.ShowConnectionError("네트워크 세션 서비스를 찾을 수 없습니다.");
        Debug.LogWarning($"{LogPrefix} FishNetSupport was not found in the scene.");
        return;
      }

      var sessionInformation = new SessionInformationModel(address, port, sessionName);

      bool portOccupied = IsPortOccupied(address, port, out string probeError);
      bool isOpeningServer = !portOccupied;
      if (!string.IsNullOrWhiteSpace(probeError))
      {
        Debug.LogWarning($"{LogPrefix} 포트 점유 여부를 확인하지 못했습니다: {probeError}. 서버 시작을 시도합니다.");
      }

      if (portOccupied)
      {
        Debug.Log($"{LogPrefix} 포트 {port}가 이미 사용 중입니다. 기존 서버에 클라이언트로 접속합니다.");
      }

      var started = fishNetSupport.StartSession(sessionInformation, isOpeningServer);
      if (!started)
      {
        string reason = isOpeningServer
          ? "서버 세션을 시작할 수 없습니다."
          : "기존 서버에 연결을 시작할 수 없습니다.";
        _connectionFailureOverlay.ShowConnectionError(reason);
        Debug.LogWarning($"{LogPrefix} FishNetSupport failed to start the requested session.");
        return;
      }

      // 서버를 실제로 연 경우에만 외부 접속 게이트와 LAN 브로드캐스트를 연다.
      ConnectionGateService.SetPort(port);
      if (isOpeningServer)
        ConnectionGateService.Open();
      else
        ConnectionGateService.Close();

      Debug.Log($"{LogPrefix} Session started. Mode={(isOpeningServer ? "Host" : "Client")}, Endpoint={address}:{port}");

      if (isOpeningServer)
        StartCoroutine(FallbackToExistingServerIfHostBindFails(fishNetSupport, sessionInformation));
    }

    private IEnumerator FallbackToExistingServerIfHostBindFails(
      FishNetSupport fishNetSupport,
      SessionInformationModel sessionInformation)
    {
      const float timeoutSeconds = 3f;
      float deadline = Time.realtimeSinceStartup + timeoutSeconds;
      while (Time.realtimeSinceStartup < deadline)
      {
        if (fishNetSupport.IsServerStarted || fishNetSupport.IsClientStarted)
          yield break;
        yield return null;
      }

      if (fishNetSupport.IsServerStarted || fishNetSupport.IsClientStarted)
        yield break;

      Debug.LogWarning($"{LogPrefix} Host binding did not reach a started state. Retrying as a client against the existing server.");
      if (!fishNetSupport.ConnectToExistingServer(sessionInformation))
      {
        _connectionFailureOverlay.ShowConnectionError("기존 서버에 연결을 시작할 수 없습니다.");
        yield break;
      }

      ConnectionGateService.Close();
    }

    private static bool IsPortOccupied(string host, ushort targetPort, out string error)
    {
      error = null;
      IPAddress[] addresses;
      try
      {
        addresses = Dns.GetHostAddresses(string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host);
      }
      catch (SocketException ex)
      {
        error = ex.Message;
        return false;
      }

      bool hasIpv4Address = false;
      for (int i = 0; i < addresses.Length; i++)
      {
        if (addresses[i].AddressFamily != AddressFamily.InterNetwork)
          continue;

        hasIpv4Address = true;

        try
        {
          using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
          {
            ExclusiveAddressUse = true,
          };
          socket.Bind(new IPEndPoint(addresses[i], targetPort));
        }
        catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
          return true;
        }
        catch (SocketException ex)
        {
          error = ex.Message;
          return false;
        }
      }

      if (!hasIpv4Address)
      {
        error = $"'{host}'에 IPv4 주소가 없습니다.";
      }

      return false;
    }

    private static void PrepareDeferredPlayerSpawning()
    {
      var fishNetSupport = FishNetSupport.Instance ?? FindAnyObjectByType<FishNetSupport>();
      if (fishNetSupport == null)
      {
        Debug.LogWarning($"{LogPrefix} FishNetSupport was not found; deferred player spawning cannot be prepared.");
        return;
      }

      fishNetSupport.PrepareDeferredPlayerSpawning();
    }

    private static IEnumerator LoadSceneIfNeeded(string sceneName)
    {
      if (string.IsNullOrWhiteSpace(sceneName))
        yield break;

      var scene = SceneManager.GetSceneByName(sceneName);
      if (scene.IsValid() && scene.isLoaded)
        yield break;

      var operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
      if (operation == null)
      {
        Debug.LogWarning($"{LogPrefix} Failed to start additive load for scene '{sceneName}'.");
        yield break;
      }

      while (!operation.isDone)
      {
        LoadingScreen.Report(DefaultsLoadingScreen.GetSceneLoadingMessage(sceneName), operation.progress / 0.9f);
        yield return null;
      }
    }
  }
}
