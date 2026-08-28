using System;
using System.Collections.Generic;

using FishNet.Managing;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerInfrastructure.Server
{
  /// <summary>
  /// 데디케이티드 서버(헤드리스) 실행을 부트스트랩합니다.
  ///
  /// Dedicated Server 서브타겟으로 빌드하면 <c>UNITY_SERVER</c>가 정의되어 자동으로 활성화되며,
  /// 일반 플레이어 빌드에서도 <c>-dedicatedServer</c>(또는 <c>-server</c>) 인자를 주면 활성화됩니다.
  /// 활성화되면 IntroScene의 UI 흐름을 건너뛰고, 커맨드라인 옵션으로 구성한 세션 정보를
  /// 런타임 레지스트리에 등록한 뒤 시작 씬(<see cref="DedicatedServerOptions.StartScene"/>)으로 진입합니다.
  /// 이후 세션 시작은 IngameSceneBootstrapper가 담당합니다.
  /// </summary>
  public static class DedicatedServerRuntime
  {
    /// <summary>현재 실행이 데디케이티드 서버 모드인지 여부입니다.</summary>
    public static bool IsActive { get; private set; }

    /// <summary>데디케이티드 서버 모드에서 사용 중인 옵션입니다. 비활성 상태에서는 <c>null</c>입니다.</summary>
    public static DedicatedServerOptions Options { get; private set; }

    /// <summary>
    /// 서버 빌드 여부와 커맨드라인 스위치를 근거로 데디케이티드 모드 활성화를 판단합니다.
    /// </summary>
    public static bool ShouldActivate(bool isServerBuild, bool explicitlyRequested)
      => isServerBuild || explicitlyRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
      // 도메인 리로드가 비활성인 환경에서 이전 실행 상태가 남지 않도록 초기화한다.
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      IsActive = false;
      Options = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
#if UNITY_SERVER
      const bool isServerBuild = true;
#else
      const bool isServerBuild = false;
#endif

      var options = DedicatedServerOptions.Parse(
        Environment.GetCommandLineArgs(),
        SessionConfigurationService.Current);

      IsActive = ShouldActivate(isServerBuild, options.IsExplicitlyRequested);
      if (!IsActive)
        return;

      Options = options;

      for (int i = 0; i < options.Warnings.Count; i++)
        Debug.LogWarning($"[DedicatedServer] {options.Warnings[i]}");

      ApplyRuntimeSettings(options);
      RegisterLaunchRequest(options);

      // NetworkManager는 서버 빌드에서 Start 시점에 서버를 자동으로 개방한다.
      // 그 시점에는 포트와 바인딩 주소가 아직 적용되지 않았으므로 자동 개방을 차단하고,
      // 세션 부트스트랩이 전송 설정을 마친 뒤에 서버를 개방하도록 한다.
      SceneManager.sceneLoaded -= HandleSceneLoaded;
      SceneManager.sceneLoaded += HandleSceneLoaded;

      Debug.Log($"[DedicatedServer] 데디케이티드 서버 모드로 시작합니다. {options}");
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnterStartScene()
    {
      if (!IsActive || Options == null)
        return;

      var startScene = Options.StartScene;
      if (string.IsNullOrWhiteSpace(startScene))
        return;

      var activeScene = SceneManager.GetActiveScene();
      if (string.Equals(activeScene.name, startScene, StringComparison.Ordinal))
        return;

      if (!Application.CanStreamedLevelBeLoaded(startScene))
      {
        Debug.LogError(
          $"[DedicatedServer] 시작 씬 '{startScene}'을(를) 로드할 수 없습니다. " +
          "Build Settings의 씬 목록에 포함되어 있는지 확인하세요.");
        return;
      }

      Debug.Log($"[DedicatedServer] 시작 씬 '{startScene}'으로 전환합니다.");
      SceneManager.LoadScene(startScene, LoadSceneMode.Single);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) => DisableFishNetHeadlessAutoStart();

    /// <summary>
    /// FishNet NetworkManager의 헤드리스 자동 서버 개방을 비활성화합니다.
    /// 씬이 로드될 때마다 호출되며, NetworkManager의 Awake가 끝난 뒤이자
    /// Start가 실행되기 전에 적용됩니다.
    /// </summary>
    public static void DisableFishNetHeadlessAutoStart()
    {
      var networkManagers = UnityEngine.Object.FindObjectsByType<NetworkManager>(
        FindObjectsInactive.Include,
        FindObjectsSortMode.None);

      for (int i = 0; i < networkManagers.Length; i++)
      {
        var serverManager = networkManagers[i] == null ? null : networkManagers[i].ServerManager;
        if (serverManager == null || !serverManager.GetStartOnHeadless())
          continue;

        serverManager.SetStartOnHeadless(false);
        Debug.Log("[DedicatedServer] NetworkManager의 헤드리스 자동 서버 개방을 비활성화했습니다.");
      }
    }

    /// <summary>서버 실행에 맞도록 프레임 레이트와 백그라운드 실행 설정을 조정합니다.</summary>
    private static void ApplyRuntimeSettings(DedicatedServerOptions options)
    {
      Application.runInBackground = true;
      QualitySettings.vSyncCount = 0;
      Application.targetFrameRate = options.TargetFrameRate > 0 ? options.TargetFrameRate : -1;
    }

    /// <summary>
    /// IntroScene이 등록하던 실행 정보를 커맨드라인 옵션으로부터 직접 등록합니다.
    /// IngameSceneBootstrapper는 이 값을 읽어 세션을 시작합니다.
    /// </summary>
    private static void RegisterLaunchRequest(DedicatedServerOptions options)
    {
      var sessionInformation = CreateSessionInformation(options);

      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.SessionInformation, sessionInformation);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.IsOpeningServer, true);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.UseLanDiscovery, options.UseLanDiscovery);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.LoadedFromIntroScene, true);
      Registry.Registry.Register(RegistryType.RuntimeState, RegistryGlobalKeys.IsDedicatedServer, true);

      if (options.DatapackIds.Count > 0)
      {
        Registry.Registry.Register(
          RegistryType.RuntimeState,
          RegistryGlobalKeys.SelectedDatapackIds,
          new List<string>(options.DatapackIds));
      }
    }

    /// <summary>커맨드라인 옵션으로 세션 정보를 구성합니다.</summary>
    public static SessionInformationModel CreateSessionInformation(DedicatedServerOptions options)
    {
      if (options == null)
        throw new ArgumentNullException(nameof(options));

      return new SessionInformationModel(
        address: options.BindAddress,
        port: options.Port,
        sessionName: options.SessionName);
    }
  }
}
