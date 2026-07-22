using System.Collections;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Scenario.Requirements;
using MultiplayerInfrastructure.Session;
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

    [Header("Session")]
    [SerializeField] private string address = "127.0.0.1";
    [SerializeField] private ushort port = 7777;
    [SerializeField] private string sessionName = "IndevSession";

    [Header("Additive Scenes")]
    [SerializeField] private bool loadSystemOverlayScene = true;

    [Header("TriageTrainer Integration")]
    [SerializeField] private bool ensureTriageSupportsOnBootstrapObject = true;

    private bool _bootstrapped;

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
      // Hide FishNet logo/HUD as early as possible, before any scene loading.
      HideFishNetHud();

      if (loadSystemOverlayScene)
      {
        yield return LoadSceneIfNeeded(SystemOverlaySceneName);
      }

      // Ensure newly-loaded scene objects complete Awake/OnEnable before networking starts.
      yield return null;

      PrepareDeferredPlayerSpawning();
      StartHostSession();

      // 개발 씬에서는 기본적으로 외부 접속을 허용한다.
      ConnectionGateService.SetPort(port);
      ConnectionGateService.Open();

      ScenarioRuntimeBootstrapGate.MarkSceneReady(gameObject.scene);
    }

    private static void HideFishNetHud()
    {
      var fishNetSupport = FishNetSupport.Instance ?? FindAnyObjectByType<FishNetSupport>();
      fishNetSupport?.SetNetworkHudCanvasVisible(false);
    }

    private void StartHostSession()
    {
      var fishNetSupport = FishNetSupport.Instance ?? FindAnyObjectByType<FishNetSupport>();
      if (fishNetSupport == null)
      {
        Debug.LogWarning($"{LogPrefix} FishNetSupport was not found in the scene.");
        return;
      }

      var sessionInformation = new SessionInformationModel(address, port, sessionName);

      var started = fishNetSupport.StartSession(sessionInformation, isOpeningServer: true);
      if (!started)
      {
        Debug.LogWarning($"{LogPrefix} FishNetSupport failed to start the host session.");
        return;
      }

      Debug.Log($"{LogPrefix} Host session started. Endpoint={address}:{port}");
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
        yield return null;
    }
  }
}
