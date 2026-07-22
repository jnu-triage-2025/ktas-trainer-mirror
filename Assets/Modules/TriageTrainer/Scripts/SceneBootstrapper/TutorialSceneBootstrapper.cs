using System.Collections;
using MultiplayerInfrastructure.FishNetSupports;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.Scenario.Requirements;
using MultiplayerInfrastructure.Session;
using TriageTrainer.MultiplayerInfrastructureSupports;
using TriageTrainer.Scenario;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TriageTrainer.SceneBootstrapper
{
  /// <summary>
  /// TutorialScene용 자동 부트스트래퍼입니다.
  /// IntroScene 흐름 없이 씬을 직접 재생하면, 자동으로 호스트(서버+클라이언트) 세션을 시작하고
  /// SystemOverlayScene을 애디티브 로드합니다.
  /// 이후 프리팹 게임 오브젝트로 씬에 배치하여 사용합니다.
  /// </summary>
  [DefaultExecutionOrder(-1000)]
  public class TutorialSceneBootstrapper : MonoBehaviour
  {
    private const string LogPrefix = "[TutorialSceneBootstrapper]";
    private const string SystemOverlaySceneName = "SystemOverlayScene";

    [Header("Session")]
    [SerializeField] private string address = "127.0.0.1";
    [SerializeField] private ushort port = 7777;
    [SerializeField] private string sessionName = "TutorialSession";

    [Header("Additive Scenes")]
    [SerializeField] private bool loadSystemOverlayScene = true;

    [Header("TriageTrainer Integration")]
    [SerializeField] private bool ensureTriageSupportsOnBootstrapObject = true;

    [Header("Auto Scenario Start")]
    [Tooltip("튜토리얼 시나리오 JSON 에셋. 비워두면 scenarioResourcePath에서 로드합니다.")]
    [SerializeField] private TextAsset scenarioJson;
    [Tooltip("scenarioJson가 비어 있을 때 Resources에서 로드할 경로.")]
    [SerializeField] private string scenarioResourcePath = "Scenario/tutorial.scenario";
    [Tooltip("시나리오 시작 노드 식별자. 비워두면 그래프의 첫 노드를 사용합니다.")]
    [SerializeField] private string startNodeIdentifier;
    [Tooltip("호스트 세션 시작 후 시나리오를 시작하기 전 대기할 프레임 수. 플레이어 스폰 완료를 기다립니다.")]
    [SerializeField, Min(1)] private int scenarioStartDelayFrames = 2;

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

      // 튜토리얼 씬에서는 외부 접속을 차단한다.
      ConnectionGateService.SetPort(port);
      ConnectionGateService.Close();

      ScenarioRuntimeBootstrapGate.MarkSceneReady(gameObject.scene);

      yield return StartScenarioAfterDelay();
    }

    /// <summary>
    /// 플레이어 스폰 및 씬 초기화가 완료될 때까지 기다린 뒤, 튜토리얼 시나리오를 자동 시작합니다.
    /// </summary>
    private IEnumerator StartScenarioAfterDelay()
    {
      for (int i = 0; i < scenarioStartDelayFrames; i++)
        yield return null;

      var graph = LoadScenarioGraph();
      if (graph == null)
      {
        Debug.LogWarning($"{LogPrefix} 시나리오 그래프를 로드하지 못했습니다. 자동 시작을 건너뜁니다.");
        yield break;
      }

      var controller = ScenarioController.Instance ?? FindAnyObjectByType<ScenarioController>();
      if (controller == null)
      {
        Debug.LogWarning($"{LogPrefix} ScenarioController를 찾을 수 없습니다. 자동 시작을 건너뜁니다.");
        yield break;
      }

      string startId = string.IsNullOrWhiteSpace(startNodeIdentifier) ? null : startNodeIdentifier;
      controller.StartScenario(graph, startId);
      Debug.Log($"{LogPrefix} 튜토리얼 시나리오를 자동 시작했습니다. graph={graph.Identifier}, startNode={startId ?? "<first>"}");
    }

    private ScenarioGraph LoadScenarioGraph()
    {
      string json = null;

      if (scenarioJson != null)
      {
        json = scenarioJson.text;
      }
      else if (!string.IsNullOrWhiteSpace(scenarioResourcePath))
      {
        var loaded = Resources.Load<TextAsset>(scenarioResourcePath);
        if (loaded != null)
        {
          json = loaded.text;
        }
        else
        {
          Debug.LogWarning($"{LogPrefix} Resources에서 시나리오 JSON을 찾을 수 없습니다: '{scenarioResourcePath}'");
          return null;
        }
      }

      if (string.IsNullOrWhiteSpace(json))
      {
        Debug.LogWarning($"{LogPrefix} 시나리오 JSON 텍스트가 비어 있습니다.");
        return null;
      }

      return ScenarioGraphLoader.LoadFromJson(json);
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
