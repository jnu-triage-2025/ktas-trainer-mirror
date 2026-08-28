using System.Collections;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Server;
using MultiplayerInfrastructure.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.FishNetSupports
{
  /// <summary>
  /// Facade component for FishNet runtime controls from scene scripts.
  /// This class is split by concerns using partial declarations.
  /// </summary>
  [DisallowMultipleComponent]
  public partial class FishNetSupport : MonoBehaviour
  {
    [Header("References")]
    [Tooltip("If empty, FishNetSupport will query NetworkManager in hierarchy at runtime.")]
    [SerializeField] private FishNet.Managing.NetworkManager networkManager;

    [Header("Player Prefab Fallback")]
    [Tooltip("PlayerSpawner의 _playerPrefab이 비어 있을 때 자동으로 할당할 Player 프리팹. " +
             "비어 있으면 Resources/Player 경로를 시도합니다.")]
    [SerializeField] private FishNet.Object.NetworkObject fallbackPlayerPrefab;

    [Header("Network HUD")]
    [Tooltip("When true, NetworkHudCanvas objects under NetworkManager are hidden when a session starts.")]
    [SerializeField] private bool hideNetworkHudCanvasOnSessionStart = true;

    [Header("Fallback")]
    [Tooltip("When IntroScene launch data exists and IngameSceneBootstrapper is missing, start session automatically.")]
    [SerializeField] private bool autoStartFromRegistryWhenNoBootstrapper = true;

    private static FishNetSupport _instance;
    public static FishNetSupport Instance => _instance;

    private void Awake()
    {
      if (_instance == null)
      {
        _instance = this;
        ResolveNetworkManagerInHierarchy();
        return;
      }

      if (_instance == this)
      {
        ResolveNetworkManagerInHierarchy();
        return;
      }

      // Static singleton can remain when domain reload is disabled.
      // If previous instance is inactive or effectively missing, replace it.
      if (!_instance || !_instance.isActiveAndEnabled)
      {
        _instance = this;
        ResolveNetworkManagerInHierarchy();
        return;
      }

      if (_instance != null)
      {
        bool currentResolved = ResolveNetworkManagerInHierarchy();
        bool existingResolved = _instance.ResolveNetworkManagerInHierarchy();

        if (!existingResolved && currentResolved)
        {
          _instance.enabled = false;
          _instance = this;
        }
        else
        {
          Debug.LogWarning($"[FishNetSupport] Duplicate instance detected. Keeping existing instance '{_instance.gameObject.name}' and disabling '{gameObject.name}'.");
          enabled = false;
          return;
        }
      }

      _instance = this;
      ResolveNetworkManagerInHierarchy();
    }

    private void OnDestroy()
    {
      // Unity 에디터에서 Play Mode를 종료할 때 씬 오브젝트가 먼저 정리되면
      // FishNet이 생성한 Player(Clone)이 씬에 남아 있다는 경고가 발생할 수 있다.
      // 네트워크 연결을 먼저 종료해 PlayerController의 OnStop* 생명주기와
      // FishNet 디스폰 처리가 완료되도록 한다.
      if (networkManager != null)
      {
        if (networkManager.ClientManager != null && networkManager.ClientManager.Started)
          networkManager.ClientManager.StopConnection();

        if (networkManager.ServerManager != null && networkManager.ServerManager.Started)
          networkManager.ServerManager.StopConnection(true);
      }

      if (_instance == this)
        _instance = null;
    }

    private void Start()
    {
      // OnEnable may run before the NetworkManager finishes Awake, in which case the
      // server-connection-state subscription silently fails. Retry once everything is initialized
      // so deferred player spawning is always prepared when the server starts.
      ResolveNetworkManagerInHierarchy();

      if (!autoStartFromRegistryWhenNoBootstrapper)
        return;

      if (FindAnyObjectByType<UnitySceneSupports.IngameScene.IngameSceneBootstrapper>() != null)
        return;

      StartCoroutine(HandleSessionInformationAlreadyConfiguredRoutine());
    }

    /// <summary>
    /// Applies launch info and starts host/client according to the mode.
    /// When <paramref name="startLocalClient"/> is false the local client is skipped,
    /// which is how a dedicated (headless) server runs.
    /// </summary>
    public bool StartSession(
      SessionInformationModel sessionInformation,
      bool isOpeningServer,
      bool startLocalClient = true)
    {
      if (sessionInformation == null)
      {
        Debug.LogWarning("[FishNetSupport] StartSession called with null sessionInformation.");
        return false;
      }

      if (!ResolveNetworkManagerInHierarchy())
      {
        Debug.LogWarning("[FishNetSupport] NetworkManager was not found in hierarchy.");
        return false;
      }

      ConfigureTransport(sessionInformation);

      if (isOpeningServer)
      {
        PrepareDeferredPlayerSpawning();
        StartServer();
      }

      if (startLocalClient)
        StartClient();

      if (hideNetworkHudCanvasOnSessionStart)
        SetNetworkHudCanvasVisible(false);

      return true;
    }

    /// <summary>
    /// 데디케이티드 서버(헤드리스)로 세션을 시작합니다.
    /// 로컬 클라이언트를 붙이지 않으며, 지정한 주소로 서버 소켓을 바인딩합니다.
    /// </summary>
    public bool StartDedicatedServer(SessionInformationModel sessionInformation, string bindAddress)
    {
      if (sessionInformation == null)
      {
        Debug.LogWarning("[FishNetSupport] StartDedicatedServer called with null sessionInformation.");
        return false;
      }

      if (!ResolveNetworkManagerInHierarchy())
      {
        Debug.LogWarning("[FishNetSupport] NetworkManager was not found in hierarchy.");
        return false;
      }

      ConfigureServerBindAddress(bindAddress);
      return StartSession(sessionInformation, isOpeningServer: true, startLocalClient: false);
    }

    public bool IsServerStarted => networkManager != null && networkManager.ServerManager.Started;
    public bool IsClientStarted => networkManager != null && networkManager.ClientManager.Started;

    public bool ConnectToExistingServer(SessionInformationModel sessionInformation)
    {
      if (sessionInformation == null || !ResolveNetworkManagerInHierarchy())
        return false;

      ConfigureTransport(sessionInformation);
      if (IsServerStarted)
        StopServer();
      if (IsClientStarted)
        StopClient();
      StartClient();
      return true;
    }

    private void HandleSessionInformationAlreadyConfigured()
    {
      if (!Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene))
        return;

      var sessionInformation = Registry.Registry.Get<SessionInformationModel>(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.SessionInformation);
      if (sessionInformation == null)
      {
        Debug.LogWarning("[FishNetSupport] No session information found in runtime registry.");
        return;
      }

      var isOpeningServer = Registry.Registry.Get<bool>(
        RegistryType.RuntimeState,
        RegistryGlobalKeys.IsOpeningServer);

      StartSession(sessionInformation, isOpeningServer);
    }

    /// <summary>
    /// 데디케이티드 서버 모드에서 레지스트리에 등록된 실행 정보로 서버 전용 세션을 시작합니다.
    /// </summary>
    private void StartDedicatedServerFromRegistry()
    {
      var sessionInformation = Registry.Registry.Get<SessionInformationModel>(
        RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation);
      if (sessionInformation == null)
      {
        Debug.LogError("[FishNetSupport] 데디케이티드 서버 실행 정보가 없어 세션을 시작할 수 없습니다.");
        return;
      }

      var bindAddress = DedicatedServerRuntime.Options?.BindAddress ?? sessionInformation.Address;
      if (!StartDedicatedServer(sessionInformation, bindAddress))
        Debug.LogError("[FishNetSupport] 데디케이티드 서버 세션을 시작하지 못했습니다.");
    }

    private IEnumerator HandleSessionInformationAlreadyConfiguredRoutine()
    {
      if (!Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene))
        yield break;

      // 데디케이티드 서버에는 표시할 UI가 없으므로 연결 실패 오버레이 씬을 요구하지 않는다.
      if (DedicatedServerRuntime.IsActive)
      {
        StartDedicatedServerFromRegistry();
        yield break;
      }

      const string failureSceneName = "NetworkSessionFailureScene";
      var failureScene = SceneManager.GetSceneByName(failureSceneName);
      if (!failureScene.IsValid() || !failureScene.isLoaded)
      {
        var operation = SceneManager.LoadSceneAsync(failureSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
          Debug.LogError($"[FishNetSupport] Failed to load required scene '{failureSceneName}'. Network startup was cancelled.");
          yield break;
        }
        while (!operation.isDone)
          yield return null;
      }

      var overlay = FindAnyObjectByType<TriageTrainer.SceneBootstrapper.IndevConnectionFailureOverlay>();
      if (overlay == null)
      {
        Debug.LogError($"[FishNetSupport] Required scene '{failureSceneName}' has no connection failure overlay. Network startup was cancelled.");
        yield break;
      }

      var sessionInformation = Registry.Registry.Get<SessionInformationModel>(
        RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation);
      if (sessionInformation == null)
      {
        overlay.ShowConnectionError("세션 정보가 없습니다.");
        yield break;
      }

      var isOpeningServer = Registry.Registry.Get<bool>(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer);
      overlay.BeginConnectionAttempt(sessionInformation.Address, sessionInformation.Port);
      if (!StartSession(sessionInformation, isOpeningServer))
        overlay.ShowConnectionError("네트워크 세션을 시작할 수 없습니다.");
    }
  }
}
