using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 도메인의 서버 권한(authoritative) 신호 중계기.
  ///
  /// 설계 근거(G-8, P1 단계):
  /// - 시나리오 게이팅에 쓰이는 완료 신호는 <see cref="ScenarioInteractionSignals"/> 를 통해
  ///   <c>RegistryType.RuntimeState</c> 레지스트리(정적·비네트워크)에 기록된다.
  /// - 인터랙션은 각 클라이언트 컨텍스트에서 일어나므로, 신호가 클라이언트 로컬에만 남으면
  ///   "한 플레이어의 행동이 다른 플레이어 브랜치의 게이트를 통과시키는" 다인 협력이 성립하지 않는다.
  /// - 이 중계기는 클라이언트가 올린 신호를 <see cref="ServerRpc"/> 로 서버에 보고하여
  ///   서버의 단일 권위 RuntimeState 에 기록되게 한다. (Validator 판정은 서버에서 수행)
  ///
  /// 사용:
  /// - 게임플레이 코드는 그대로 <see cref="ScenarioInteractionSignals.Raise"/> 만 호출하면 된다.
  ///   서버 컨텍스트면 직접 기록, 클라이언트 컨텍스트면 이 중계기를 통해 서버로 보고된다.
  ///
  /// 추가 범위(P3 1차): 서버가 단일 그래프 상태기를 실행하고, 클라이언트는 시작/표시/입력 보고만
  /// 수행하도록 Dialogue·Choice의 표현 RPC를 제공한다. 이 경로는 지원 노드 집합으로 검증된
  /// 그래프에만 사용하며, 병렬 역할 브랜치와 미구현 표현 노드는 기존 호환 경로로 폴백한다.
  /// 단일 플레이어(호스트 단독)에서는 서버=클라 이므로 동작이 기존과 동일하다.
  /// </summary>
  public sealed class ScenarioNetworkRelay : NetworkBehaviour
  {
    private static ScenarioNetworkRelay _instance;
    public static event Func<string, string, bool, NetworkConnection, bool> LineTopologyRequestReceived;
    public static event Action<long, string, string, bool> LineTopologyMirrored;
    public static event Action<int, string, string, bool, bool> LineTopologyRequestCompleted;
    public static event Action<long> LineTopologySnapshotBegan;
    public static event Action<long> LineTopologySnapshotEnded;
    public static event Func<IEnumerable<LineTopologyPair>> LineTopologySnapshotRequested;
    private const int MaxSignalUpdatesPerPlayerPerWindow = 30;
    private const float SignalUpdateWindowSeconds = 1f;
    private const int MaxLineTopologyUpdatesPerPlayerPerWindow = 20;
    private const int MaxLineTopologySnapshotsPerPlayerPerWindow = 2;
    private static readonly Dictionary<string, Queue<float>> SignalUpdateTimesByPlayer = new(StringComparer.Ordinal);
    private static readonly Dictionary<int, Queue<float>> LineTopologyUpdateTimesByClient = new();
    private static readonly Dictionary<int, Queue<float>> LineTopologySnapshotTimesByClient = new();
    private static readonly ScenarioClientSignalAuthorization ClientSignalAuthorization = new();

    /// <summary>
    /// 이번 그래프 실행에서 이미 진단을 남긴 미선언 client-origin 신호들.
    ///
    /// <para>
    /// 미선언 신호는 상호작용을 반복할 때마다 다시 올라오므로, 발신마다 경고를 남기면 콘솔과
    /// 세션 로그가 같은 문장으로 뒤덮여 정작 다른 진단을 읽을 수 없게 된다. 그래서 신호 식별자별로
    /// 한 번만 남기고, 그 이후의 같은 신호는 조용히 무시한다. 이 집합은
    /// <see cref="ConfigureClientSignalAuthorization"/> 와 <see cref="ClearClientSignalAuthorization"/>,
    /// 그리고 서버 수명주기(<see cref="OnStartServer"/> / <see cref="OnStopServer"/>)에서 비운다.
    /// 따라서 시나리오를 다시 시작하면 같은 신호가 다시 한 번 보고된다.
    /// </para>
    /// </summary>
    private static readonly HashSet<string> ReportedUndeclaredClientSignals = new(StringComparer.Ordinal);
    private long _lineTopologyVersion;
    private readonly List<ActingNpcConfiguration> _actingNpcConfigurations = new List<ActingNpcConfiguration>();
    private readonly List<NpcControlState> _npcControlStates = new List<NpcControlState>();

    private sealed class ActingNpcConfiguration
    {
      public string GraphIdentifier;
      public string ActingNpcIdentifier;
      public NetworkObject NetworkObject;
    }

    private sealed class NpcControlState
    {
      public NetworkObject NetworkObject;
      public string DisplayName;
      public bool HasDisplayName;
      public bool? ShowOverheadName;
      public readonly Dictionary<string, NpcInteractState> Interacts =
        new Dictionary<string, NpcInteractState>(StringComparer.Ordinal);
    }

    private sealed class NpcInteractState
    {
      public bool? IsAttached;
      public bool? IsEnabled;
    }

    /// <summary>씬에 배치된 중계기 인스턴스(없으면 null).</summary>
    public static ScenarioNetworkRelay Instance => _instance;

    public readonly struct LineTopologyPair
    {
      public LineTopologyPair(string first, string second)
      {
        First = first;
        Second = second;
      }

      public string First { get; }
      public string Second { get; }
    }

    public static bool RequestLineTopologyChange(string first, string second, bool connected, int requestId = 0)
    {
      if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
        return false;

      if (InstanceFinder.IsServerStarted)
        return false;
      if (_instance == null || !InstanceFinder.IsClientStarted)
        return false;

      _instance.CmdRequestLineTopologyChange(first, second, connected, requestId);
      return true;
    }

    public static void PublishLineTopologyChange(string first, string second, bool connected)
    {
      if (InstanceFinder.IsServerStarted && !string.IsNullOrWhiteSpace(first)
          && !string.IsNullOrWhiteSpace(second))
        _instance?.RpcMirrorLineTopologyChange(++_instance._lineTopologyVersion, first, second, connected);
    }

    private static bool ProcessLineTopologyChange(
      string first, string second, bool connected, NetworkConnection sender)
    {
      var handler = LineTopologyRequestReceived;
      if (handler == null || !handler.Invoke(first, second, connected, sender))
        return false;
      _instance?.RpcMirrorLineTopologyChange(++_instance._lineTopologyVersion, first, second, connected);
      return true;
    }

    /// <summary>서버 권위 신호 파라미터 저장소를 모든 피어에서 비운다.</summary>
    public static void FlushSignalParametersAuthoritative()
    {
      if (InstanceFinder.IsServerStarted)
      {
        ScenarioSignalParameterStore.FlushLocal();
        SignalUpdateTimesByPlayer.Clear();
        if (_instance != null)
          _instance.RpcMirrorFlushSignalParameters();
        return;
      }

      if (InstanceFinder.IsOffline || _instance == null)
      {
        ScenarioSignalParameterStore.FlushLocal();
        SignalUpdateTimesByPlayer.Clear();
      }
    }

    /// <summary>
    /// 서버에서 그래프를 한 번만 시작하고, 선택된 클라이언트에는 표시 전용 세션을 준비시킨다.
    /// 중계기가 없는 레거시 씬에서는 false를 반환하여 호출자가 기존 호환 경로를 선택할 수 있다.
    /// </summary>
    public static bool TryStartAuthoritativeScenario(
      string scenarioIdentifier,
      int ownerClientId,
      IEnumerable<NetworkConnection> targets)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted || string.IsNullOrWhiteSpace(scenarioIdentifier))
        return false;

      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out var error))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Failed to load authoritative scenario '{scenarioIdentifier}': {error}");
        return false;
      }

      if (TryGetUnsupportedAuthoritativeNode(graph, out var unsupportedNode))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Scenario '{scenarioIdentifier}' contains unsupported authoritative node "
          + $"'{unsupportedNode.Identifier}' ({unsupportedNode.GetType().Name}); using the compatibility execution path.");
        return false;
      }

      var resolvedTargets = (targets ?? Enumerable.Empty<NetworkConnection>())
        .Where(target => target != null)
        .GroupBy(target => target.ClientId)
        .Select(group => group.First())
        .ToList();
      if (resolvedTargets.Count == 0 || ScenarioController.Instance == null)
        return false;

      foreach (var target in resolvedTargets)
        _instance.TargetBeginPresentationScenario(target, scenarioIdentifier, ownerClientId);

      ScenarioController.Instance.StartAuthoritativeScenario(graph, null, ownerClientId >= 0 ? ownerClientId : (int?)null);
      return true;
    }

    private static bool TryGetUnsupportedAuthoritativeNode(ScenarioGraph graph, out IScenarioNode unsupportedNode)
    {
      foreach (var node in graph.Nodes.Values)
      {
        // 이 노드들은 서버 상태기만 실행하면 되고, 클라이언트별 별도 표현/입력 명세가 없다.
        // Dialogue와 Choice만 현재 Target/Observers RPC로 완전한 표시·입력 왕복을 지원한다.
        if (node is ScenarioDialogueNode
            || node is ScenarioChoiceNode
            || node is ScenarioDelayNode
            || node is ScenarioValidatorNode
            || node is ScenarioServerInternalSignalNode
            || node is ScenarioSignalListenerNode
            || node is ScenarioSignalCounterNode
            || node is ScenarioEntityStateSignalBindingNode
            || node is ScenarioStateUpdateNode
            || node is ScenarioNPCControlNode
            || node is ScenarioManualEntrypointNode
            || node is ScenarioReturnToOriginNode
            || node is ScenarioLifecycleNode)
          continue;

        unsupportedNode = node;
        return true;
      }

      unsupportedNode = null;
      return false;
    }

    /// <summary>서버가 현재 노드를 모든 표시 참여자에게 전달한다.</summary>
    public static void PresentAuthoritativeNode(string graphIdentifier, string nodeIdentifier)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted
          || string.IsNullOrWhiteSpace(graphIdentifier) || string.IsNullOrWhiteSpace(nodeIdentifier))
        return;

      _instance.ObserversPresentScenarioNode(graphIdentifier, nodeIdentifier);
    }

    /// <summary>병렬 역할 브랜치의 표현 노드를 배정된 클라이언트 한 명에게만 전달한다.</summary>
    public static void PresentAuthoritativeNodeToClient(int clientId, string graphIdentifier, string nodeIdentifier)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted
          || string.IsNullOrWhiteSpace(graphIdentifier) || string.IsNullOrWhiteSpace(nodeIdentifier))
        return;

      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return;

      foreach (var pair in clients)
      {
        if (pair.Value != null && pair.Value.ClientId == clientId)
        {
          _instance.TargetPresentRoleNode(pair.Value, graphIdentifier, nodeIdentifier);
          return;
        }
      }
    }

    /// <summary>
    /// 지정한 시나리오가 발행한 퀘스트만 모든 피어에서 제거한다. QuestManager 는 피어마다 따로
    /// 들고 있어서 서버에서 지우는 것만으로는 클라이언트 화면의 퀘스트가 남는다.
    /// </summary>
    /// <param name="scenarioIdentifier">
    /// 대상 시나리오 식별자. 비어 있으면 출처를 가릴 수 없으므로 아무것도 지우지 않는다.
    /// </param>
    public static void ClearScenarioQuestsAuthoritative(string scenarioIdentifier)
    {
      if (string.IsNullOrWhiteSpace(scenarioIdentifier))
      {
        Debug.LogWarning("[ScenarioNetworkRelay] Skipped quest clear: scenario identifier is missing.");
        return;
      }

      ClearScenarioQuestsLocal(scenarioIdentifier);

      if (_instance != null && InstanceFinder.IsServerStarted)
        _instance.ObserversClearScenarioQuests(scenarioIdentifier);
    }

    // ExcludeServer: 호스트는 바로 위에서 이미 로컬로 지웠다. RPC 는 전송 큐를 거쳐 한 틱 뒤에
    // 도착하므로, 호스트가 이것까지 받으면 그 사이 준비 체인이 새로 발행한 퀘스트를 지워 버린다.
    [ObserversRpc(BufferLast = false, ExcludeServer = true)]
    private void ObserversClearScenarioQuests(string scenarioIdentifier)
      => ClearScenarioQuestsLocal(scenarioIdentifier);

    /// <summary>
    /// 이 피어의 QuestManager 에서 해당 시나리오가 발행한 퀘스트만 제거한다.
    /// 튜토리얼 리졸버 등 시나리오 밖 출처가 등록한 퀘스트는 건드리지 않는다.
    /// </summary>
    private static void ClearScenarioQuestsLocal(string scenarioIdentifier)
    {
      var manager = Registry.Registry.Get<Quest.QuestManager>(
        RegistryType.Service, Registry.Registry.TypeKey<Quest.QuestManager>());
      if (manager == null)
        return;

      // Quests 는 스냅샷이지만, RemoveQuest 가 내부 컬렉션을 바꾸므로 식별자를 먼저 모아 둔다.
      var targets = manager.Quests
        .Where(quest => quest != null
                        && !string.IsNullOrWhiteSpace(quest.Id)
                        && string.Equals(quest.SourceScenarioIdentifier, scenarioIdentifier, StringComparison.Ordinal))
        .Select(quest => quest.Id)
        .ToList();

      foreach (var questId in targets)
        manager.RemoveQuest(questId);
    }

    /// <summary>
    /// 호환 실행 경로(대상 클라이언트마다 독립 상태기가 도는 구성)에서 모든 피어가 각자
    /// 같은 지점으로 건너뛰게 한다. 서버 권위 실행 중이면 서버 커서 하나만 옮기면 되므로
    /// 이 브로드캐스트를 쓰지 않는다.
    /// </summary>
    /// <returns>브로드캐스트를 실제로 보냈으면 true.</returns>
    public static bool BroadcastManualEntry(string entrypointIdentifier, bool clearState)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted || string.IsNullOrWhiteSpace(entrypointIdentifier))
        return false;

      _instance.ObserversEnterManualEntrypoint(entrypointIdentifier, clearState);
      return true;
    }

    /// <summary>호환 실행 경로의 모든 피어에서 실행 중인 시나리오를 종료한다.</summary>
    public static bool BroadcastScenarioEnd()
    {
      if (_instance == null || !InstanceFinder.IsServerStarted)
        return false;

      _instance.ObserversEndCompatibilityScenario();
      return true;
    }

    /// <summary>호환 실행 경로의 모든 피어에서 실행 중인 시나리오를 다시 시작한다.</summary>
    public static bool BroadcastScenarioRestart(string entrypointIdentifier)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted)
        return false;

      _instance.ObserversRestartCompatibilityScenario(entrypointIdentifier);
      return true;
    }

    // ExcludeServer: 호스트는 자기 상태기를 호출부에서 직접 옮긴다.
    [ObserversRpc(BufferLast = false, ExcludeServer = true)]
    private void ObserversEnterManualEntrypoint(string entrypointIdentifier, bool clearState)
    {
      if (ScenarioController.Instance != null)
        ScenarioController.Instance.EnterManualEntrypointFromRelay(entrypointIdentifier, clearState);
    }

    [ObserversRpc(BufferLast = false, ExcludeServer = true)]
    private void ObserversEndCompatibilityScenario()
    {
      ScenarioController.Instance?.EndScenario();
    }

    [ObserversRpc(BufferLast = false, ExcludeServer = true)]
    private void ObserversRestartCompatibilityScenario(string entrypointIdentifier)
    {
      ScenarioController.Instance?.RestartScenario(entrypointIdentifier);
    }

    /// <summary>
    /// 서버가 재생 위치를 건너뛰었을 때 표시 피어에 남은 대화 UI 를 내리게 한다.
    /// </summary>
    public static void DismissAuthoritativePresentation(string graphIdentifier)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted || string.IsNullOrWhiteSpace(graphIdentifier))
        return;

      _instance.ObserversDismissPresentationUI(graphIdentifier);
    }

    // ExcludeServer: 호스트는 스킵 시점에 이미 로컬 UI 를 내렸다. 한 틱 뒤 도착하는 RPC 까지
    // 받으면 그 사이 준비 체인이 띄운 대화창을 다시 닫아 버린다.
    [ObserversRpc(BufferLast = false, ExcludeServer = true)]
    private void ObserversDismissPresentationUI(string graphIdentifier)
    {
      if (ScenarioController.Instance != null)
        ScenarioController.Instance.DismissPresentationUI(graphIdentifier);
    }

    /// <summary>
    /// 역할 브랜치 표시를 받은 클라이언트 한 명의 대화 UI 만 내린다.
    /// 브랜치 대화가 autoAdvanceSeconds 로 자동 종료될 때, 다른 참가자의 화면을 건드리지 않고
    /// 해당 클라이언트의 잔여 대화창만 정리하기 위해 사용한다.
    /// </summary>
    public static void DismissAuthoritativePresentationForClient(int clientId, string graphIdentifier)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted || string.IsNullOrWhiteSpace(graphIdentifier))
        return;

      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return;

      foreach (var pair in clients)
      {
        if (pair.Value != null && pair.Value.ClientId == clientId)
        {
          _instance.TargetDismissPresentationUI(pair.Value, graphIdentifier);
          return;
        }
      }
    }

    [TargetRpc]
    private void TargetDismissPresentationUI(NetworkConnection conn, string graphIdentifier)
    {
      // 호스트의 Controller 는 권위 상태기와 같은 인스턴스다. 서버 실행기가 직접 UI 를
      // 관리하므로, 뒤늦게 도착한 RPC 로 진행 중인 표시를 되돌리지 않는다.
      if (InstanceFinder.IsServerStarted)
        return;

      if (ScenarioController.Instance != null)
        ScenarioController.Instance.DismissPresentationUI(graphIdentifier);
    }

    /// <summary>
    /// 서버가 실행한 연출 전용 이벤트를 표시 피어에서도 실행시킨다.
    ///
    /// 역할 브랜치 밖의 일반 InvokeEvent 노드는 그래프를 순회하는 권위 피어에서만 실행된다.
    /// 반면 상호작용에서 시작되는 연출은 상호작용한 피어의 로컬 상태로 남으므로, 그 연출을 끝내는
    /// 이벤트를 전달하지 않으면 해당 피어에서 연출과 입력 제약이 영구히 남는다. 시작을 로컬에서
    /// 수행하고 종료만 서버가 통지하는 연출은 이 중계를 통해 종료를 전 피어에 도달시킨다.
    /// </summary>
    public static void InvokePresentationEventAuthoritative(string eventIdentifier)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted
          || string.IsNullOrWhiteSpace(eventIdentifier))
        return;

      string graphIdentifier = ScenarioController.Instance?.CurrentGraph?.Identifier;
      if (string.IsNullOrWhiteSpace(graphIdentifier))
        return;

      _instance.ObserversInvokePresentationEvent(graphIdentifier, eventIdentifier);
    }

    // ExcludeServer: 호스트는 권위 경로에서 같은 이벤트를 이미 실행했다. RPC 까지 받으면 같은
    // 연출 종료가 두 번 적용된다.
    [ObserversRpc(BufferLast = false, ExcludeServer = true)]
    private void ObserversInvokePresentationEvent(string graphIdentifier, string eventIdentifier)
    {
      if (ScenarioController.Instance != null)
        ScenarioController.Instance.RunPresentationEvent(graphIdentifier, eventIdentifier);
    }

    /// <summary>서버가 권위 시나리오의 종료를 표시 참여자에게 전달한다.</summary>
    public static void EndAuthoritativePresentation(string graphIdentifier)
    {
      if (_instance != null && InstanceFinder.IsServerStarted && !string.IsNullOrWhiteSpace(graphIdentifier))
        _instance.ObserversEndPresentationScenario(graphIdentifier);
    }

    /// <summary>
    /// 서버에서 preset spawn 후 적용한 actingNpc 식별자와 인라인 상호작용을 동일한 NetworkObject의
    /// 원격 클라이언트 인스턴스에도 적용한다.
    /// </summary>
    public static bool PublishScenarioActingNpcConfiguration(
      string graphIdentifier,
      string actingNpcIdentifier,
      NetworkObject actorObject)
    {
      if (!InstanceFinder.IsServerStarted)
        return true;
      if (_instance == null || actorObject == null
          || string.IsNullOrWhiteSpace(graphIdentifier) || string.IsNullOrWhiteSpace(actingNpcIdentifier))
        return false;

      _instance.RememberActingNpcConfiguration(graphIdentifier, actingNpcIdentifier, actorObject);
      _instance.ObserversConfigureScenarioActingNpc(graphIdentifier, actingNpcIdentifier, actorObject);
      return true;
    }

    public static void PublishNPCControlUpdate(
      NetworkObject actorObject,
      ScenarioNPCControlNode node)
    {
      if (!InstanceFinder.IsServerStarted || _instance == null || actorObject == null || node == null)
        return;

      _instance.RememberNPCControlUpdate(actorObject, node);
      _instance.ObserversUpdateNPCControl(
        actorObject, node.Identifier, node.DisplayName,
        node.ShowOverheadName.HasValue, node.ShowOverheadName.GetValueOrDefault(),
        (int)node.InteractOperation, node.InteractableIdentifier,
        node.InteractEnabled.HasValue, node.InteractEnabled.GetValueOrDefault());
    }

    public override void OnStartServer()
    {
      base.OnStartServer();
      ScenarioSignalParameterStore.FlushLocal();
      SignalUpdateTimesByPlayer.Clear();
      LineTopologyUpdateTimesByClient.Clear();
      LineTopologySnapshotTimesByClient.Clear();
      ClientSignalAuthorization.ClearScenario();
      ReportedUndeclaredClientSignals.Clear();
      InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
    }

    public override void OnStartClient()
    {
      base.OnStartClient();
      if (!IsServerStarted)
        StartCoroutine(RequestLineTopologySnapshotWithRetry());
    }

    private IEnumerator RequestLineTopologySnapshotWithRetry()
    {
      for (int attempt = 0; attempt < 3 && IsClientStarted && !IsServerStarted; attempt++)
      {
        CmdRequestLineTopologySnapshot();
        yield return new WaitForSecondsRealtime(1f);
      }
    }

    public override void OnStopServer()
    {
      if (InstanceFinder.NetworkManager?.ServerManager != null)
        InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
      _actingNpcConfigurations.Clear();
      _npcControlStates.Clear();
      ScenarioSignalParameterStore.FlushLocal();
      SignalUpdateTimesByPlayer.Clear();
      ClientSignalAuthorization.ClearAll();
      ReportedUndeclaredClientSignals.Clear();
      base.OnStopServer();
    }

    private void RememberNPCControlUpdate(NetworkObject actorObject, ScenarioNPCControlNode node)
    {
      _npcControlStates.RemoveAll(value => value == null || value.NetworkObject == null);
      var state = _npcControlStates.FirstOrDefault(value => value.NetworkObject == actorObject);
      if (state == null)
      {
        state = new NpcControlState { NetworkObject = actorObject };
        _npcControlStates.Add(state);
      }

      if (!string.IsNullOrWhiteSpace(node.DisplayName))
      {
        state.DisplayName = node.DisplayName.Trim();
        state.HasDisplayName = true;
      }
      if (node.ShowOverheadName.HasValue)
        state.ShowOverheadName = node.ShowOverheadName;

      if (node.InteractOperation == ScenarioNPCInteractCrudOperation.None
          || node.InteractOperation == ScenarioNPCInteractCrudOperation.Read
          || string.IsNullOrWhiteSpace(node.InteractableIdentifier))
        return;

      string identifier = node.InteractableIdentifier.Trim();
      if (!state.Interacts.TryGetValue(identifier, out var interactState))
      {
        interactState = new NpcInteractState();
        state.Interacts.Add(identifier, interactState);
      }

      switch (node.InteractOperation)
      {
        case ScenarioNPCInteractCrudOperation.Create:
          interactState.IsAttached = true;
          break;
        case ScenarioNPCInteractCrudOperation.Delete:
          interactState.IsAttached = false;
          break;
        case ScenarioNPCInteractCrudOperation.Update:
          interactState.IsEnabled = node.InteractEnabled;
          break;
      }
    }

    private void RememberActingNpcConfiguration(
      string graphIdentifier, string actingNpcIdentifier, NetworkObject networkObject)
    {
      _actingNpcConfigurations.RemoveAll(value => value == null || value.NetworkObject == null
        || value.NetworkObject == networkObject);
      _actingNpcConfigurations.Add(new ActingNpcConfiguration
      {
        GraphIdentifier = graphIdentifier,
        ActingNpcIdentifier = actingNpcIdentifier,
        NetworkObject = networkObject
      });
    }

    private void OnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
      if (args.ConnectionState == RemoteConnectionState.Stopped && connection != null)
      {
        ClientSignalAuthorization.Revoke(connection.ClientId);
        return;
      }
      if (args.ConnectionState != RemoteConnectionState.Started || connection == null)
        return;
      StartCoroutine(SendActingNpcConfigurationsAfterSpawn(connection));
    }

    private IEnumerator SendActingNpcConfigurationsAfterSpawn(NetworkConnection connection)
    {
      // 새 접속자의 NetworkObject spawn 메시지가 먼저 처리되도록 한 프레임 양보한다.
      yield return null;

      // TargetRpc는 대상 접속자가 이 NetworkObject의 observer로 등록된 상태여야 한다.
      // 한 프레임만으로 spawn/observer 등록이 완료되지 않을 수 있으므로,
      // observer로 등록될 때까지(또는 접속이 끊기거나 타임아웃) 기다린다.
      float observerWaitSeconds = 5f;
      while (NetworkObject != null
             && connection != null
             && connection.IsActive
             && !NetworkObject.Observers.Contains(connection))
      {
        observerWaitSeconds -= Time.unscaledDeltaTime;
        if (observerWaitSeconds <= 0f)
        {
          Debug.LogWarning(
            "[ScenarioNetworkRelay] Timed out waiting for observer registration; skipping late-join snapshot.");
          yield break;
        }
        yield return null;
      }

      if (NetworkObject == null || connection == null || !connection.IsActive)
        yield break;

      _actingNpcConfigurations.RemoveAll(value => value == null || value.NetworkObject == null);
      foreach (var configuration in _actingNpcConfigurations)
        TargetConfigureScenarioActingNpc(connection, configuration.GraphIdentifier,
          configuration.ActingNpcIdentifier, configuration.NetworkObject);

      // 재접속 클라이언트의 정적 미러에 이전 세션 값이 남지 않도록, 스냅샷 적용 전에 비운다.
      TargetFlushSignalParameters(connection);
      foreach (var signal in ScenarioSignalParameterStore.GetAll())
      {
        TargetMirrorScenarioSignalParameter(connection, signal.SignalIdentifier,
          signal.PlayerIdentifier, signal.PlayerDisplayName, signal.ParameterJson,
          signal.OccurredAtUtcTicks, signal.Sequence);
      }

      _npcControlStates.RemoveAll(value => value == null || value.NetworkObject == null);
      foreach (var state in _npcControlStates)
      {
        TargetUpdateNPCControl(
          connection, state.NetworkObject, "late-join-display",
          state.HasDisplayName ? state.DisplayName : null,
          state.ShowOverheadName.HasValue, state.ShowOverheadName.GetValueOrDefault(),
          (int)ScenarioNPCInteractCrudOperation.None, null, false, false);

        foreach (var pair in state.Interacts)
        {
          if (pair.Value.IsAttached.HasValue)
          {
            TargetUpdateNPCControl(
              connection, state.NetworkObject, "late-join-interact-membership",
              null, false, false,
              (int)(pair.Value.IsAttached.Value
                ? ScenarioNPCInteractCrudOperation.Create
                : ScenarioNPCInteractCrudOperation.Delete),
              pair.Key, false, false);
          }
          if (pair.Value.IsEnabled.HasValue)
          {
            TargetUpdateNPCControl(
              connection, state.NetworkObject, "late-join-interact-enabled",
              null, false, false, (int)ScenarioNPCInteractCrudOperation.Update,
              pair.Key, true, pair.Value.IsEnabled.Value);
          }
        }
      }
    }

    /// <summary>
    /// 서버가 병렬 노드의 확정 배정표를 각 접속자에게 TargetRpc로 전달한다.
    /// 빈 배열도 전송해, 해당 parallel node에서 역할을 받지 못한 클라이언트가 이전 배정을
    /// 잘못 재사용하지 않게 한다.
    /// </summary>
    public static void PublishParallelAssignments(
      string graphIdentifier,
      string parallelNodeIdentifier,
      IReadOnlyDictionary<ScenarioParallelBranch, int?> allocation)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted
          || string.IsNullOrWhiteSpace(graphIdentifier) || string.IsNullOrWhiteSpace(parallelNodeIdentifier)
          || allocation == null)
        return;

      var branchesByClient = allocation
        .Where(pair => pair.Key != null && pair.Value.HasValue && !string.IsNullOrWhiteSpace(pair.Key.Identifier))
        .GroupBy(pair => pair.Value.Value)
        .ToDictionary(
          group => group.Key,
          group => group.Select(pair => pair.Key.Identifier).Distinct(StringComparer.Ordinal).ToArray());
      var clients = InstanceFinder.ServerManager?.Clients;
      if (clients == null)
        return;

      foreach (var pair in clients)
      {
        var connection = pair.Value;
        if (connection == null)
          continue;

        branchesByClient.TryGetValue((int)connection.ClientId, out var branchIdentifiers);
        _instance.TargetAssignParallelBranches(
          connection,
          graphIdentifier,
          parallelNodeIdentifier,
          branchIdentifiers ?? Array.Empty<string>());
      }
    }

    /// <summary>표시 클라이언트가 대화 계속 입력을 서버에 보고한다.</summary>
    public static void RequestAdvance(string graphIdentifier, string nodeIdentifier)
    {
      if (_instance != null && InstanceFinder.IsClientStarted)
        _instance.CmdRequestAdvance(graphIdentifier, nodeIdentifier);
    }

    /// <summary>표시 클라이언트가 Choice 선택을 서버에 보고한다.</summary>
    public static void RequestChoiceSelection(string graphIdentifier, string nodeIdentifier, int optionIndex)
    {
      if (_instance != null && InstanceFinder.IsClientStarted)
        _instance.CmdRequestChoiceSelection(graphIdentifier, nodeIdentifier, optionIndex);
    }

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(gameObject);
        return;
      }

      _instance = this;
    }

    private void OnDestroy()
    {
      if (_instance == this)
      {
        _instance = null;
      }
    }

    [TargetRpc]
    private void TargetBeginPresentationScenario(NetworkConnection conn, string scenarioIdentifier, int ownerClientId)
    {
      // 호스트의 Controller는 서버 권위 상태기와 같은 인스턴스다. TargetRpc가 비동기로
      // 도착해 이를 표시 전용 모드로 덮어쓰지 않도록, 호스트는 서버 실행기의 직접 UI 표시를 쓴다.
      if (InstanceFinder.IsServerStarted)
        return;

      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out var error))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Failed to load presentation scenario '{scenarioIdentifier}': {error}");
        return;
      }

      if (ScenarioController.Instance == null)
      {
        Debug.LogWarning("[ScenarioNetworkRelay] ScenarioController is missing on presentation client.");
        return;
      }

      ScenarioController.Instance.BeginPresentationScenario(graph, ownerClientId >= 0 ? ownerClientId : (int?)null);
    }

    [TargetRpc]
    private void TargetAssignParallelBranches(
      NetworkConnection conn,
      string graphIdentifier,
      string parallelNodeIdentifier,
      string[] branchIdentifiers)
    {
      ScenarioParallelAssignmentState.Apply(graphIdentifier, parallelNodeIdentifier, branchIdentifiers);
    }

    [TargetRpc]
    private void TargetPresentRoleNode(NetworkConnection conn, string graphIdentifier, string nodeIdentifier)
    {
      ScenarioController.Instance?.PresentAuthoritativeNode(graphIdentifier, nodeIdentifier, roleScoped: true);
    }

    [ObserversRpc]
    private void ObserversPresentScenarioNode(string graphIdentifier, string nodeIdentifier)
    {
      // 호스트에서도 서버 권위 실행기와 표시 UI가 같은 Controller 인스턴스일 수 있다.
      // 그 경우 서버가 UI를 직접 실행하지 않으므로, 별도 표시 인스턴스가 없으면 무시한다.
      ScenarioController.Instance?.PresentAuthoritativeNode(graphIdentifier, nodeIdentifier);
    }

    [ObserversRpc]
    private void ObserversEndPresentationScenario(string graphIdentifier)
    {
      ScenarioController.Instance?.EndPresentationScenario(graphIdentifier);
    }

    [ObserversRpc(BufferLast = false)]
    private void ObserversConfigureScenarioActingNpc(
      string graphIdentifier,
      string actingNpcIdentifier,
      NetworkObject actorObject)
    {
      ApplyScenarioActingNpcConfiguration(graphIdentifier, actingNpcIdentifier, actorObject);
    }

    /// <summary>
    /// ActingNpc 구성을 이 피어에 적용한다. Observers/Target 두 수신 경로가 공유한다.
    ///
    /// <para>
    /// client RPC 의 본문에서 다른 client RPC 메서드를 호출하면 안 된다. FishNet 코드젠은 원본
    /// 메서드를 "송신부"로 치환하고 송신부에 IsServer 가드를 삽입하므로, 클라이언트에서 실행되는
    /// RPC 본문이 다른 client RPC 를 호출하면 그 호출은 조용히 무시된다. 따라서 공통 처리는
    /// 반드시 이 메서드처럼 RPC 가 아닌 일반 메서드로 분리해 각 수신부가 직접 호출해야 한다.
    /// </para>
    /// </summary>
    private void ApplyScenarioActingNpcConfiguration(
      string graphIdentifier,
      string actingNpcIdentifier,
      NetworkObject actorObject)
    {
      // 호스트는 서버 경로에서 이미 동일 인스턴스를 구성했다.
      if (InstanceFinder.IsServerStarted)
        return;
      if (actorObject == null)
      {
        Debug.LogWarning(
          $"[ScenarioNetworkRelay] ActingNpc '{actingNpcIdentifier}' NetworkObject is unavailable on presentation client.");
        return;
      }
      if (!Registry.Registry.TryGetScenarioGraph(graphIdentifier, out var graph, out var error))
      {
        Debug.LogWarning(
          $"[ScenarioNetworkRelay] Failed to resolve actingNpc graph '{graphIdentifier}': {error}");
        return;
      }

      var actingNpc = graph.ActingNpcs?.FirstOrDefault(
        value => value != null && string.Equals(value.Identifier, actingNpcIdentifier, StringComparison.Ordinal));
      var npc = actorObject.GetComponentInChildren<Entity.Npc>(true);
      if (actingNpc == null || npc == null)
      {
        Debug.LogWarning(
          $"[ScenarioNetworkRelay] Could not configure actingNpc '{actingNpcIdentifier}' on presentation client.");
        return;
      }

      npc.ConfigureScenarioActingNpc(actingNpc);
    }

    [ObserversRpc(BufferLast = false)]
    private void ObserversUpdateNPCControl(
      NetworkObject actorObject,
      string nodeIdentifier,
      string displayName,
      bool hasShowOverheadName,
      bool showOverheadName,
      int interactOperation,
      string interactableIdentifier,
      bool hasInteractEnabled,
      bool interactEnabled)
    {
      ApplyNPCControlUpdateOnPeer(
        actorObject, nodeIdentifier, displayName,
        hasShowOverheadName, showOverheadName, interactOperation,
        interactableIdentifier, hasInteractEnabled, interactEnabled);
    }

    /// <summary>
    /// NPC 제어 갱신을 이 피어에 적용한다. Observers/Target 두 수신 경로가 공유한다.
    /// client RPC 본문에서 다른 client RPC 를 호출하면 안 되는 이유는
    /// <see cref="ApplyScenarioActingNpcConfiguration"/> 의 설명을 참고한다.
    /// </summary>
    private void ApplyNPCControlUpdateOnPeer(
      NetworkObject actorObject,
      string nodeIdentifier,
      string displayName,
      bool hasShowOverheadName,
      bool showOverheadName,
      int interactOperation,
      string interactableIdentifier,
      bool hasInteractEnabled,
      bool interactEnabled)
    {
      if (InstanceFinder.IsServerStarted || actorObject == null)
        return;

      var npc = actorObject.GetComponentInChildren<Entity.Npc>(true);
      ScenarioController.ApplyNPCControlUpdate(
        npc, nodeIdentifier, displayName,
        hasShowOverheadName ? showOverheadName : (bool?)null,
        (ScenarioNPCInteractCrudOperation)interactOperation,
        interactableIdentifier,
        hasInteractEnabled ? interactEnabled : (bool?)null);
    }

    [TargetRpc]
    private void TargetUpdateNPCControl(
      NetworkConnection connection,
      NetworkObject actorObject,
      string nodeIdentifier,
      string displayName,
      bool hasShowOverheadName,
      bool showOverheadName,
      int interactOperation,
      string interactableIdentifier,
      bool hasInteractEnabled,
      bool interactEnabled)
    {
      ApplyNPCControlUpdateOnPeer(
        actorObject, nodeIdentifier, displayName,
        hasShowOverheadName, showOverheadName, interactOperation,
        interactableIdentifier, hasInteractEnabled, interactEnabled);
    }

    [TargetRpc]
    private void TargetConfigureScenarioActingNpc(
      NetworkConnection connection, string graphIdentifier, string actingNpcIdentifier, NetworkObject actorObject)
    {
      ApplyScenarioActingNpcConfiguration(graphIdentifier, actingNpcIdentifier, actorObject);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestAdvance(string graphIdentifier, string nodeIdentifier, NetworkConnection sender = null)
    {
      if (sender == null || ScenarioController.Instance == null
          || !ScenarioController.Instance.TryAdvanceFromPresentation(sender.ClientId, graphIdentifier, nodeIdentifier))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected dialogue advance from client {sender?.ClientId.ToString() ?? "unknown"}.");
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestChoiceSelection(string graphIdentifier, string nodeIdentifier, int optionIndex, NetworkConnection sender = null)
    {
      if (sender == null || ScenarioController.Instance == null
          || !ScenarioController.Instance.TrySelectOptionFromPresentation(sender.ClientId, graphIdentifier, nodeIdentifier, optionIndex))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected choice selection from client {sender?.ClientId.ToString() ?? "unknown"}.");
      }
    }

    /// <summary>
    /// 신호를 권위적으로 올린다. 서버면 즉시 기록, 클라이언트면 서버로 보고한다.
    /// 중계기가 없거나 네트워크가 비활성이면 로컬에 기록(단일 플레이어/오프라인 폴백).
    /// </summary>
    public static void RaiseAuthoritative(string normalizedSignalId, string parameterJson = null)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return;
      }

      // 서버 컨텍스트: 직접 권위 기록 후 전체 클라이언트에 미러링.
      // (Validator 판정은 각 피어 로컬에서 폴링되므로, 서버 기록만으로는
      //  다른 클라이언트의 게이트가 통과되지 않는다.)
      if (InstanceFinder.IsServerStarted)
      {
        if (ScenarioSignalPlayerContext.TryGetCurrent(out string playerIdentifier, out string playerDisplayName))
        {
          RaiseOnServer(normalizedSignalId, parameterJson, playerIdentifier, playerDisplayName, null);
          return;
        }
        RaiseOnServer(normalizedSignalId, parameterJson,
          ScenarioSignalParameterStore.ServerPlayerIdentifier,
          ScenarioSignalParameterStore.ServerPlayerIdentifier, null);
        return;
      }

      // 클라이언트 컨텍스트: 중계기로 서버 보고.
      if (_instance != null && InstanceFinder.IsClientStarted)
      {
        _instance.CmdRaiseScenarioSignal(normalizedSignalId, parameterJson);
        return;
      }

      // 네트워크 비활성/중계기 부재: 로컬 폴백.
      if (ScenarioSignalPlayerContext.TryGetCurrent(out string localPlayerIdentifier, out string localPlayerDisplayName))
      {
        RaiseOnServer(normalizedSignalId, parameterJson, localPlayerIdentifier, localPlayerDisplayName, null);
        return;
      }
      RaiseOnServer(normalizedSignalId, parameterJson,
        ScenarioSignalParameterStore.ServerPlayerIdentifier,
        ScenarioSignalParameterStore.ServerPlayerIdentifier, null);
    }

    /// <summary>서버 명령·시스템이 명시한 플레이어 귀속으로 신호를 기록한다.</summary>
    public static bool RaiseAuthoritativeForPlayer(string normalizedSignalId, string parameterJson,
      string playerIdentifier, string playerDisplayName, NetworkConnection sender = null)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
        return false;

      if (InstanceFinder.IsServerStarted || InstanceFinder.IsOffline || _instance == null)
        return RaiseOnServer(normalizedSignalId, parameterJson, playerIdentifier, playerDisplayName, sender);

      // 클라이언트가 임의의 플레이어 귀속을 지정할 수 없게 한다. 이 경로는 서버 명령 전용이다.
      _instance.CmdRaiseScenarioSignal(normalizedSignalId, parameterJson);
      return true;
    }

    /// <summary>
    /// 서버가 그래프 밖의 검증된 gameplay RPC에 일시적인 client signal capability를 부여한다.
    /// 기본적으로 raise만 허용하며 clear는 별도로 명시해야 한다.
    /// </summary>
    public static bool GrantClientSignalCapability(int clientId, string signalId,
      bool allowClear = false, bool isPrefix = false)
    {
      if (!InstanceFinder.IsServerStarted || clientId < 0 || string.IsNullOrWhiteSpace(signalId))
        return false;
      ClientSignalAuthorization.Grant(clientId, ScenarioInteractionSignals.Normalize(signalId), allowClear, isPrefix);
      return true;
    }

    public static void RevokeClientSignalCapabilities(int clientId)
      => ClientSignalAuthorization.Revoke(clientId);

    internal static void ConfigureClientSignalAuthorization(ScenarioGraph graph)
    {
      ClientSignalAuthorization.ConfigureScenario(graph);
      // 선언 목록이 그래프마다 다르므로, 이전 실행에서 보고한 신호를 그대로 두면 새 그래프에서
      // 같은 신호가 미선언 상태여도 진단이 나오지 않는다. 실행이 바뀔 때마다 억제 상태를 비운다.
      ReportedUndeclaredClientSignals.Clear();
    }

    internal static void ClearClientSignalAuthorization()
    {
      ClientSignalAuthorization.ClearAll();
      ReportedUndeclaredClientSignals.Clear();
    }

    /// <summary>
    /// SignalCounter, SignalListener처럼 그래프 엔진이 계산하는 서버 전용 출력의 실행 주체인지 판정한다.
    /// 네트워크가 없는 테스트와 단일 플레이 환경에서는 기존 로컬 실행을 유지한다.
    /// </summary>
    internal static bool CanEmitServerOwnedSignalOutput()
      => InstanceFinder.IsServerStarted || InstanceFinder.IsOffline || _instance == null;

    private static bool RaiseOnServer(string normalizedSignalId, string parameterJson,
      string playerIdentifier, string playerDisplayName, NetworkConnection sender)
    {
      if (!TryValidateAuthoritativeSignalInput(normalizedSignalId, parameterJson, out string validationError))
      {
        ReportRejectedInput(normalizedSignalId, parameterJson, validationError, sender);
        return false;
      }
      if (!ScenarioSignalParameterStore.TryValidateJson(parameterJson, out _))
      {
        ReportInvalidParameter(normalizedSignalId, parameterJson, sender);
        return false;
      }
      if (!TryConsumeSignalUpdateQuota(playerIdentifier, out string rateLimitError))
      {
        ReportRateLimit(normalizedSignalId, playerIdentifier, rateLimitError, sender);
        return false;
      }

      // 파라미터 저장소는 감사·조회용 부가 상태다. 포화되어도 실제 시나리오 신호는 반드시 발생시킨다.
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
      if (!ScenarioSignalParameterStore.CanRecord(normalizedSignalId, playerIdentifier, out string capacityError))
      {
        ReportStorageLimit(normalizedSignalId, playerIdentifier, capacityError, sender);
        if (_instance != null && InstanceFinder.IsServerStarted)
          _instance.RpcMirrorRaiseScenarioSignalOnly(normalizedSignalId);
        return false;
      }

      ScenarioSignalParameter value = ScenarioSignalParameterStore.RecordAuthoritative(
        normalizedSignalId, playerIdentifier, playerDisplayName, parameterJson);
      if (_instance != null && InstanceFinder.IsServerStarted)
      {
        _instance.RpcMirrorRaiseScenarioSignal(normalizedSignalId, value.PlayerIdentifier,
          value.PlayerDisplayName, value.ParameterJson, value.OccurredAtUtcTicks, value.Sequence);
      }
      return true;
    }

    /// <summary>
    /// 역할 브랜치가 실행한 이벤트를 나머지 피어에서도 실행하게 한다.
    ///
    /// <para>
    /// 호환 실행 경로에서 역할 브랜치는 담당 피어 한 곳에서만 돈다. 그래서 이벤트 핸들러가 담고
    /// 있는 서버 전용 처리(퀘스트 상태 플래그, 라인 연결, 조건부 신호 리스너 출력)와 모든 피어에
    /// 보여야 하는 표현이 담당자가 호스트가 아닐 때 어디에도 적용되지 않는다. 메인 체인 이벤트는
    /// 모든 피어가 각자 실행하므로 이 문제가 없으며, 이 전달은 역할 브랜치를 같은 범위로 맞춘다.
    /// </para>
    /// </summary>
    public static void PublishBranchEventToPeers(
      string graphIdentifier, string eventIdentifier, int originClientId)
    {
      if (_instance == null
          || string.IsNullOrWhiteSpace(graphIdentifier)
          || string.IsNullOrWhiteSpace(eventIdentifier))
        return;

      if (InstanceFinder.IsServerStarted)
      {
        _instance.ObserversRunBranchEvent(graphIdentifier, eventIdentifier, originClientId);
        return;
      }

      if (InstanceFinder.IsClientStarted)
        _instance.CmdRunBranchEvent(graphIdentifier, eventIdentifier, originClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRunBranchEvent(
      string graphIdentifier, string eventIdentifier, int originClientId, NetworkConnection sender = null)
    {
      // 발신자가 자기 자신을 원본으로 지목한 요청만 받는다. 그래야 다른 피어를 원본으로 위장해
      // 특정 피어만 이벤트를 건너뛰게 만들 수 없다.
      if (sender == null || !sender.IsValid || sender.ClientId != originClientId)
        return;

      if (!IsDeclaredScenarioEvent(graphIdentifier, eventIdentifier))
      {
        Debug.LogWarning(
          $"[ScenarioNetworkRelay] Rejected branch event '{eventIdentifier}' that graph "
          + $"'{graphIdentifier}' does not declare.");
        return;
      }

      ObserversRunBranchEvent(graphIdentifier, eventIdentifier, originClientId);
    }

    // ExcludeServer 를 쓰지 않는다. 발신자가 클라이언트일 때 서버도 이 이벤트를 실행해야 하며,
    // 발신자 자신은 originClientId 대조로 건너뛴다.
    [ObserversRpc(BufferLast = false)]
    private void ObserversRunBranchEvent(
      string graphIdentifier, string eventIdentifier, int originClientId)
    {
      ScenarioController.Instance?.RunBranchEventFromPeer(graphIdentifier, eventIdentifier, originClientId);
    }

    /// <summary>그래프가 InvokeEvent 노드로 선언한 이벤트 식별자인지 확인한다.</summary>
    private static bool IsDeclaredScenarioEvent(string graphIdentifier, string eventIdentifier)
    {
      if (!Registry.Registry.TryGetScenarioGraph(graphIdentifier, out ScenarioGraph graph, out _)
          || graph?.Nodes == null)
        return false;

      foreach (var node in graph.Nodes.Values)
      {
        if (node is ScenarioInvokeEventNode invoke
            && string.Equals(invoke.EventIdentifier, eventIdentifier, StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    /// <summary>
    /// 역할 브랜치가 올린 서버 내부 신호 Resolve 를 나머지 피어에도 반영하게 한다.
    ///
    /// <para>
    /// 내부 신호 레지스트리는 피어마다 따로 있다. 호환 실행 경로에서 Register 를 기다리는
    /// 브랜치와 Resolve 를 올리는 브랜치가 서로 다른 담당자에게 배정되면, 이 전달 없이는
    /// 기다리는 쪽 피어에 Resolve 가 닿지 않아 병렬 합류가 영원히 끝나지 않는다.
    /// 브랜치 이벤트 전달(<see cref="PublishBranchEventToPeers"/>)과 같은 경로와 검증을 쓴다.
    /// </para>
    /// </summary>
    public static void PublishBranchInternalSignalToPeers(
      string graphIdentifier, string targetIdentifier, string signalIdentifier, int originClientId)
    {
      if (_instance == null
          || string.IsNullOrWhiteSpace(graphIdentifier)
          || string.IsNullOrWhiteSpace(signalIdentifier))
        return;

      string normalizedTarget = ScenarioServerInternalSignalRegistry.NormalizeTarget(targetIdentifier);
      if (InstanceFinder.IsServerStarted)
      {
        _instance.ObserversResolveBranchInternalSignal(
          graphIdentifier, normalizedTarget, signalIdentifier, originClientId);
        return;
      }

      if (InstanceFinder.IsClientStarted)
        _instance.CmdResolveBranchInternalSignal(
          graphIdentifier, normalizedTarget, signalIdentifier, originClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdResolveBranchInternalSignal(
      string graphIdentifier, string targetIdentifier, string signalIdentifier, int originClientId,
      NetworkConnection sender = null)
    {
      // 발신자가 자기 자신을 원본으로 지목한 요청만 받는다. 다른 피어를 원본으로 위장해
      // 특정 피어의 반영만 건너뛰게 만들 수 없어야 한다.
      if (sender == null || !sender.IsValid || sender.ClientId != originClientId)
        return;

      if (!IsDeclaredInternalSignalResolve(graphIdentifier, targetIdentifier, signalIdentifier))
      {
        Debug.LogWarning(
          $"[ScenarioNetworkRelay] Rejected internal signal resolve '{targetIdentifier}::{signalIdentifier}' "
          + $"that graph '{graphIdentifier}' does not declare.");
        return;
      }

      ObserversResolveBranchInternalSignal(graphIdentifier, targetIdentifier, signalIdentifier, originClientId);
    }

    // ExcludeServer 를 쓰지 않는다. 발신자가 클라이언트일 때 서버(호스트)의 레지스트리에도 반영해야
    // 호스트가 맡은 브랜치의 Register 가 풀린다. 발신자 자신은 originClientId 대조로 건너뛴다.
    [ObserversRpc(BufferLast = false)]
    private void ObserversResolveBranchInternalSignal(
      string graphIdentifier, string targetIdentifier, string signalIdentifier, int originClientId)
    {
      ScenarioController.Instance?.ResolveBranchInternalSignalFromPeer(
        graphIdentifier, targetIdentifier, signalIdentifier, originClientId);
    }

    /// <summary>그래프가 ServerInternalSignal(Resolve) 노드로 선언한 대상·신호 쌍인지 확인한다.</summary>
    internal static bool IsDeclaredInternalSignalResolve(
      string graphIdentifier, string targetIdentifier, string signalIdentifier)
    {
      if (string.IsNullOrWhiteSpace(signalIdentifier)
          || !Registry.Registry.TryGetScenarioGraph(graphIdentifier, out ScenarioGraph graph, out _)
          || graph?.Nodes == null)
        return false;

      return IsDeclaredInternalSignalResolve(graph, targetIdentifier, signalIdentifier);
    }

    internal static bool IsDeclaredInternalSignalResolve(
      ScenarioGraph graph, string targetIdentifier, string signalIdentifier)
    {
      if (graph?.Nodes == null || string.IsNullOrWhiteSpace(signalIdentifier))
        return false;

      string normalizedTarget = ScenarioServerInternalSignalRegistry.NormalizeTarget(targetIdentifier);
      string normalizedSignal = signalIdentifier.Trim();
      foreach (var node in graph.Nodes.Values)
      {
        if (node is ScenarioServerInternalSignalNode internalSignal
            && internalSignal.Operation == ScenarioServerInternalSignalOperationType.Resolve
            && !string.IsNullOrWhiteSpace(internalSignal.SignalIdentifier)
            && string.Equals(internalSignal.SignalIdentifier.Trim(), normalizedSignal, StringComparison.Ordinal)
            && string.Equals(
              ScenarioServerInternalSignalRegistry.NormalizeTarget(internalSignal.TargetIdentifier),
              normalizedTarget,
              StringComparison.Ordinal))
          return true;
      }

      return false;
    }

    /// <summary>
    /// 플레이어 퀘스트 상태 플래그 변경을 서버로 올린다.
    ///
    /// <para>
    /// 호환 실행 경로에서 병렬 역할 브랜치는 배정된 클라이언트에서만 실행된다. 그 브랜치가 부르는
    /// 상호작용 개방 이벤트가 플래그 풀을 건드리므로, 담당자가 호스트가 아니면 변경이 서버에
    /// 닿지 못한다. 기록과 복제는 서버가 하고, 클라이언트는 요청만 올린다.
    /// </para>
    /// </summary>
    /// <returns>서버에 요청을 보냈거나 이미 서버 컨텍스트여서 위임이 필요 없으면 false 가 아닌 값.</returns>
    internal static bool RequestQuestStateFlagChange(
      Quest.PlayerQuestStateFlagService.QuestStateFlagScope scope,
      string[] targets,
      string flag,
      bool value)
    {
      if (_instance == null || !InstanceFinder.IsClientStarted || InstanceFinder.IsServerStarted
          || string.IsNullOrWhiteSpace(flag))
        return false;

      _instance.CmdApplyQuestStateFlag((byte)scope, targets ?? Array.Empty<string>(), flag, value);
      return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdApplyQuestStateFlag(byte scope, string[] targets, string flag, bool value,
      NetworkConnection sender = null)
    {
      if (sender == null || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || descriptor == null || string.IsNullOrWhiteSpace(descriptor.Identifier))
      {
        Debug.LogWarning(
          "[ScenarioNetworkRelay] Rejected quest state flag change from an unknown sender.");
        return;
      }

      if (!Enum.IsDefined(typeof(Quest.PlayerQuestStateFlagService.QuestStateFlagScope), scope))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected quest state flag change with unknown scope {scope}.");
        return;
      }

      // 실행 중인 콘텐츠가 어휘를 선언했다면 그 안의 플래그만 받는다. 어휘를 선언하지 않는
      // 콘텐츠까지 막으면 기존 동작이 바뀌므로, 선언이 없을 때만 통과시킨다.
      var knownFlags = Quest.PlayerQuestStateFlagService.KnownFlags;
      if (knownFlags.Count > 0 && !knownFlags.Contains(flag))
      {
        Debug.LogWarning(
          $"[ScenarioNetworkRelay] Rejected undeclared quest state flag '{flag}' from client {sender.ClientId}.");
        return;
      }

      Quest.PlayerQuestStateFlagService.ApplyRelayed(
        (Quest.PlayerQuestStateFlagService.QuestStateFlagScope)scope, targets, flag, value);
    }

    /// <summary>신호를 권위적으로 내린다(사이클 반복 등에서 재설정).</summary>
    public static void ClearAuthoritative(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return;
      }

      if (InstanceFinder.IsServerStarted)
      {
        ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
        if (_instance != null)
        {
          _instance.RpcMirrorClearScenarioSignal(normalizedSignalId);
        }
        return;
      }

      if (_instance != null && InstanceFinder.IsClientStarted)
      {
        _instance.CmdClearScenarioSignal(normalizedSignalId);
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }

    /// <summary>신호 식별자 최대 길이(자원 고갈 방지용 방어선).</summary>
    private const int MaxSignalIdentifierLength = 256;
    private const int MaxSignalParameterLength = 4096;

    /// <summary>
    /// 클라이언트가 보고한 신호 식별자의 서버측 검증.
    /// 정규화 접두사(sig.)와 길이 상한을 강제하여, 임의 문자열 주입으로 인한
    /// 레지스트리 오염/무한 증식(1→N 증폭)을 차단한다.
    /// </summary>
    private static bool IsValidClientSignal(string normalizedSignalId)
    {
      if (string.IsNullOrWhiteSpace(normalizedSignalId))
      {
        return false;
      }

      if (normalizedSignalId.Length > MaxSignalIdentifierLength)
      {
        return false;
      }

      return normalizedSignalId.StartsWith(ScenarioInteractionSignals.Prefix, System.StringComparison.Ordinal)
        && !normalizedSignalId.Any(char.IsControl);
    }

    private static bool TryValidateAuthoritativeSignalInput(string normalizedSignalId, string parameterJson, out string error)
    {
      error = string.Empty;
      if (!IsValidClientSignal(normalizedSignalId))
      {
        error = "시그널 식별자는 'sig.' 접두사, 최대 256자, 제어문자 없음 조건을 만족해야 합니다.";
        return false;
      }
      if (parameterJson != null && parameterJson.Length > MaxSignalParameterLength)
      {
        error = $"시그널 매개변수는 최대 {MaxSignalParameterLength}자까지 허용됩니다.";
        return false;
      }
      return true;
    }

    private static bool TryConsumeSignalUpdateQuota(string playerIdentifier, out string error)
    {
      error = string.Empty;
      if (string.IsNullOrWhiteSpace(playerIdentifier))
        playerIdentifier = ScenarioSignalParameterStore.ServerPlayerIdentifier;

      float now = Time.realtimeSinceStartup;
      if (!SignalUpdateTimesByPlayer.TryGetValue(playerIdentifier, out var times))
      {
        times = new Queue<float>();
        SignalUpdateTimesByPlayer.Add(playerIdentifier, times);
      }
      while (times.Count > 0 && now - times.Peek() >= SignalUpdateWindowSeconds)
        times.Dequeue();
      if (times.Count >= MaxSignalUpdatesPerPlayerPerWindow)
      {
        error = $"플레이어별 시그널 갱신 한도({MaxSignalUpdatesPerPlayerPerWindow}/{SignalUpdateWindowSeconds:0.#}초)에 도달했습니다.";
        return false;
      }
      times.Enqueue(now);
      return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRaiseScenarioSignal(string normalizedSignalId, string parameterJson, NetworkConnection sender = null)
    {
      if (!IsValidClientSignal(normalizedSignalId))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected invalid client signal raise: '{normalizedSignalId}'");
        return;
      }

      if (sender == null || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || string.IsNullOrWhiteSpace(descriptor.Identifier))
      {
        ReportRejectedInput(normalizedSignalId, parameterJson,
          "발신 플레이어를 서버 세션에서 확인할 수 없습니다.", sender);
        return;
      }
      if (!ClientSignalAuthorization.CanRaise(
            sender.ClientId, descriptor.Identifier, normalizedSignalId, out string authorizationError,
            out var raiseRejection))
      {
        if (raiseRejection == ScenarioClientSignalRejection.Undeclared)
          ReportUndeclaredClientSignalRaise(normalizedSignalId, authorizationError, sender, descriptor.Identifier);
        else
          ReportRejectedInput(normalizedSignalId, parameterJson, authorizationError, sender);
        return;
      }

      string playerIdentifier = descriptor.Identifier;
      string playerDisplayName = descriptor.DisplayName;
      RaiseOnServer(normalizedSignalId, parameterJson, playerIdentifier, playerDisplayName, sender);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdClearScenarioSignal(string normalizedSignalId, NetworkConnection sender = null)
    {
      if (!IsValidClientSignal(normalizedSignalId))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected invalid client signal clear: '{normalizedSignalId}'");
        return;
      }

      if (sender == null || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || string.IsNullOrWhiteSpace(descriptor.Identifier))
      {
        ReportRejectedInput(normalizedSignalId, null,
          "발신 플레이어를 서버 세션에서 확인할 수 없습니다.", sender);
        return;
      }
      if (!ClientSignalAuthorization.CanClear(sender.ClientId, normalizedSignalId,
            out string authorizationError, out var clearRejection))
      {
        if (clearRejection == ScenarioClientSignalRejection.Undeclared)
          ReportIgnoredInput(normalizedSignalId, authorizationError);
        else
          ReportRejectedInput(normalizedSignalId, null, authorizationError, sender);
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
      RpcMirrorClearScenarioSignal(normalizedSignalId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestLineTopologyChange(
      string first, string second, bool connected, int requestId, NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid)
        return;
      if (!IsValidLineTopologyIdentifier(first) || !IsValidLineTopologyIdentifier(second)
          || !TryConsumeLineTopologyUpdateQuota(sender.ClientId))
      {
        TargetLineTopologyRequestCompleted(sender, requestId, first, second, connected, accepted: false);
        return;
      }
      bool accepted = ProcessLineTopologyChange(first, second, connected, sender);
      TargetLineTopologyRequestCompleted(sender, requestId, first, second, connected, accepted);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestLineTopologySnapshot(NetworkConnection sender = null)
    {
      if (sender == null || !sender.IsValid || !TryConsumeLineTopologySnapshotQuota(sender.ClientId))
        return;
      long version = _lineTopologyVersion;
      TargetBeginLineTopologySnapshot(sender, version);
      foreach (var callback in LineTopologySnapshotRequested?.GetInvocationList()
               ?? Array.Empty<Delegate>())
      {
        if (callback is not Func<IEnumerable<LineTopologyPair>> provider)
          continue;
        var pairs = provider.Invoke();
        if (pairs == null)
          continue;
        foreach (var pair in pairs)
          TargetMirrorLineTopologyChange(sender, version, pair.First, pair.Second, true);
      }
      TargetEndLineTopologySnapshot(sender, version);
    }

    private static bool IsValidLineTopologyIdentifier(string value) =>
      !string.IsNullOrWhiteSpace(value) && value.Length <= 256 && !value.Any(char.IsControl);

    private static bool TryConsumeLineTopologyUpdateQuota(int clientId)
    {
      float now = Time.realtimeSinceStartup;
      if (!LineTopologyUpdateTimesByClient.TryGetValue(clientId, out var times))
      {
        times = new Queue<float>();
        LineTopologyUpdateTimesByClient.Add(clientId, times);
      }
      while (times.Count > 0 && now - times.Peek() >= SignalUpdateWindowSeconds)
        times.Dequeue();
      if (times.Count >= MaxLineTopologyUpdatesPerPlayerPerWindow)
        return false;
      times.Enqueue(now);
      return true;
    }

    private static bool TryConsumeLineTopologySnapshotQuota(int clientId)
    {
      float now = Time.realtimeSinceStartup;
      if (!LineTopologySnapshotTimesByClient.TryGetValue(clientId, out var times))
      {
        times = new Queue<float>();
        LineTopologySnapshotTimesByClient.Add(clientId, times);
      }
      while (times.Count > 0 && now - times.Peek() >= SignalUpdateWindowSeconds)
        times.Dequeue();
      if (times.Count >= MaxLineTopologySnapshotsPerPlayerPerWindow)
        return false;
      times.Enqueue(now);
      return true;
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorLineTopologyChange(long version, string first, string second, bool connected)
      => LineTopologyMirrored?.Invoke(version, first, second, connected);

    [TargetRpc]
    private void TargetMirrorLineTopologyChange(
      NetworkConnection target, long version, string first, string second, bool connected)
      => LineTopologyMirrored?.Invoke(version, first, second, connected);

    [TargetRpc]
    private void TargetBeginLineTopologySnapshot(NetworkConnection target, long version)
      => LineTopologySnapshotBegan?.Invoke(version);

    [TargetRpc]
    private void TargetEndLineTopologySnapshot(NetworkConnection target, long version)
      => LineTopologySnapshotEnded?.Invoke(version);

    [TargetRpc]
    private void TargetLineTopologyRequestCompleted(
      NetworkConnection target, int requestId, string first, string second, bool connected, bool accepted)
      => LineTopologyRequestCompleted?.Invoke(requestId, first, second, connected, accepted);

    /// <summary>
    /// 서버의 권위 신호 기록을 모든 클라이언트 로컬 레지스트리로 미러링한다.
    /// </summary>
    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorRaiseScenarioSignal(string normalizedSignalId, string playerIdentifier,
      string playerDisplayName, string parameterJson, long occurredAtUtcTicks, long sequence)
    {
      // 호스트(서버=클라)에서는 이미 서버 경로에서 기록되었으므로 중복 기록해도 무해(idempotent)하다.
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
      ScenarioSignalParameterStore.ApplyMirror(new ScenarioSignalParameter(normalizedSignalId,
        playerIdentifier, playerDisplayName, parameterJson, occurredAtUtcTicks, sequence));
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorRaiseScenarioSignalOnly(string normalizedSignalId)
      => ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);

    [TargetRpc]
    private void TargetMirrorScenarioSignalParameter(NetworkConnection connection, string normalizedSignalId,
      string playerIdentifier, string playerDisplayName, string parameterJson, long occurredAtUtcTicks, long sequence)
    {
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
      ScenarioSignalParameterStore.ApplyMirror(new ScenarioSignalParameter(normalizedSignalId,
        playerIdentifier, playerDisplayName, parameterJson, occurredAtUtcTicks, sequence));
    }

    [TargetRpc]
    private void TargetFlushSignalParameters(NetworkConnection connection)
      => ScenarioSignalParameterStore.ApplySnapshotFromServer(null);

    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorFlushSignalParameters()
      => ScenarioSignalParameterStore.FlushLocal();

    private static void ReportInvalidParameter(string normalizedSignalId, string parameterJson, NetworkConnection sender)
    {
      string message = $"시그널 ({normalizedSignalId})의 매개변수 ({parameterJson ?? "null"})는 올바른 JSON 형식이 아닙니다.";
      Debug.LogError($"[ScenarioNetworkRelay] {message}");
      GameLogService.WriteSignal($"Signal parameter rejected: signal={ScenarioSignalParameterStore.FormatForLog(normalizedSignalId)}, parameter={ScenarioSignalParameterStore.FormatForLog(parameterJson)}", ScenarioSignalParameterStore.FormatForLog(normalizedSignalId));
      if (sender != null
          && Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out var chat))
        chat.SendSystemMessage(sender, message);
    }

    private static void ReportRejectedInput(string normalizedSignalId, string parameterJson,
      string reason, NetworkConnection sender)
    {
      string message = $"시그널 ({normalizedSignalId})을 수락하지 못했습니다: {reason}";
      Debug.LogWarning($"[ScenarioNetworkRelay] {message}");
      GameLogService.WriteSignal($"Signal rejected: signal={ScenarioSignalParameterStore.FormatForLog(normalizedSignalId)}, parameter={ScenarioSignalParameterStore.FormatForLog(parameterJson)}, reason={ScenarioSignalParameterStore.FormatForLog(reason)}", ScenarioSignalParameterStore.FormatForLog(normalizedSignalId));
      if (sender != null
          && Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out var chat))
        chat.SendSystemMessage(sender, message);
    }

    /// <summary>
    /// 활성 시나리오가 선언하지 않은 client-origin 신호를 기록만 하고 무시한다.
    ///
    /// <para>
    /// 아이템 획득, 환자 클릭, 존 진입 같은 일상적인 상호작용은 게이트로 쓰이지 않을 때에도
    /// 신호를 올린다. 이런 신호가 거부되는 것은 인가 정책이 의도대로 동작한 결과이지 결함이 아니므로,
    /// 발신자에게 시스템 메시지를 보내지 않는다. 다만 선언 누락을 추적할 수 있도록
    /// 게임 로그에는 그대로 남긴다.
    /// </para>
    /// </summary>
    private static void ReportIgnoredInput(string normalizedSignalId, string reason)
    {
      GameLogService.WriteSignal(
        $"Signal ignored: signal={ScenarioSignalParameterStore.FormatForLog(normalizedSignalId)}, reason={ScenarioSignalParameterStore.FormatForLog(reason)}",
        ScenarioSignalParameterStore.FormatForLog(normalizedSignalId));
    }

    /// <summary>
    /// 그래프가 선언하지 않아 거부된 client-origin raise 를 운영자가 볼 수 있는 진단으로 남긴다.
    ///
    /// <para>
    /// 이 거부는 호스트에서 재현되지 않는다. 호스트가 올리는 신호는
    /// <see cref="RaiseAuthoritative"/> 의 서버 분기에서 허용 목록을 거치지 않고 바로 기록되는 반면,
    /// 원격 클라이언트의 같은 신호만 <see cref="CmdRaiseScenarioSignal"/> 를 거쳐 여기로 온다.
    /// 그래서 선언을 빠뜨리면 호스트로 확인할 때는 정상으로 보이고 나머지 참가자만 막히며,
    /// 그 신호를 기다리는 게이트에는 타임아웃이 없으므로 결과는 세션 정지다.
    /// 침묵을 없애는 것이 목적이므로 거부 자체는 그대로 유지하고 진단만 추가한다.
    /// </para>
    ///
    /// <para>
    /// 같은 신호가 반복 발신될 때 로그가 폭주하지 않도록, 그래프 실행 한 번당 신호별로 한 번만 남긴다.
    /// 억제 상태(<see cref="ReportedUndeclaredClientSignals"/>)는 시나리오가 바뀌거나 끝날 때 비워진다.
    /// </para>
    /// </summary>
    private static void ReportUndeclaredClientSignalRaise(
      string normalizedSignalId, string reason, NetworkConnection sender, string playerIdentifier)
    {
      if (!ReportedUndeclaredClientSignals.Add(normalizedSignalId))
        return;

      string signalTag = ScenarioSignalParameterStore.FormatForLog(normalizedSignalId);
      string clientText = sender != null ? sender.ClientId.ToString() : "unknown";
      string playerText = ScenarioSignalParameterStore.FormatForLog(
        string.IsNullOrWhiteSpace(playerIdentifier) ? "unknown" : playerIdentifier);
      string graphText = ScenarioSignalParameterStore.FormatForLog(
        ScenarioController.Instance?.CurrentGraph?.Identifier ?? "none");

      string message =
        $"Undeclared client signal ignored: signal={signalTag}, client={clientText}, player={playerText}, "
        + $"graph={graphText}, reason={ScenarioSignalParameterStore.FormatForLog(reason)}. "
        + "Declare this signal in the graph's clientSignalIdentifiers, or cover it with one of its "
        + "clientSignalPrefixes; otherwise only the host can raise it and every remote participant "
        + "stays blocked at the gate that waits for it. "
        + "(Reported once per signal for each scenario run.)";

      Debug.LogWarning($"[ScenarioNetworkRelay] {message}");
      GameLogService.WriteSignal(message, signalTag);
    }

    private static void ReportStorageLimit(string normalizedSignalId, string playerIdentifier,
      string error, NetworkConnection sender)
    {
      string message = $"시그널 ({normalizedSignalId})의 매개변수를 저장하지 못했습니다: {error}";
      Debug.LogWarning($"[ScenarioNetworkRelay] {message}");
      GameLogService.WriteSignal($"Signal parameter rejected: signal={ScenarioSignalParameterStore.FormatForLog(normalizedSignalId)}, player={ScenarioSignalParameterStore.FormatForLog(playerIdentifier)}, reason={ScenarioSignalParameterStore.FormatForLog(error)}", ScenarioSignalParameterStore.FormatForLog(normalizedSignalId));
      if (sender != null
          && Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out var chat))
        chat.SendSystemMessage(sender, message);
    }

    private static void ReportRateLimit(string normalizedSignalId, string playerIdentifier,
      string error, NetworkConnection sender)
    {
      string message = $"시그널 ({normalizedSignalId})을 수락하지 못했습니다: {error}";
      Debug.LogWarning($"[ScenarioNetworkRelay] {message}");
      GameLogService.WriteSignal($"Signal rate limited: signal={ScenarioSignalParameterStore.FormatForLog(normalizedSignalId)}, player={ScenarioSignalParameterStore.FormatForLog(playerIdentifier)}, reason={ScenarioSignalParameterStore.FormatForLog(error)}", ScenarioSignalParameterStore.FormatForLog(normalizedSignalId));
      if (sender != null
          && Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out var chat))
        chat.SendSystemMessage(sender, message);
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorClearScenarioSignal(string normalizedSignalId)
    {
      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }
  }

  /// <summary>
  /// client-origin 신호가 거부된 이유의 분류. 보고 수준을 정하는 데 사용한다.
  /// </summary>
  public enum ScenarioClientSignalRejection
  {
    /// <summary>거부되지 않았다.</summary>
    None,

    /// <summary>
    /// 서버가 검증한 gameplay 상태에서만 발생할 수 있는 신호를 클라이언트가 올리려 했다.
    /// 코드 결함이거나 위조 시도이므로 경고와 발신자 통지를 유지한다.
    /// </summary>
    ServerOnlySignal,

    /// <summary>
    /// 활성 시나리오가 이 신호를 client-origin 으로 선언하지 않았다.
    /// 게이트로 쓰이지 않는 일상적인 상호작용(아이템 획득, 존 진입 등)에서 정상적으로 발생하므로
    /// 정책이 의도대로 동작한 결과이며 결함이 아니다.
    /// </summary>
    Undeclared
  }

  /// <summary>
  /// Client-origin generic signal RPC의 서버측 capability 집합. 그래프가 소비하는 입력만 허용하고,
  /// 그래프가 생성하는 출력은 동일 식별자가 validator에 있어도 client 입력에서 제외한다.
  /// </summary>
  public sealed class ScenarioClientSignalAuthorization
  {
    private readonly HashSet<string> _expectedRaises = new(StringComparer.Ordinal);
    private readonly HashSet<string> _allowedPrefixes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _serverOnlySignals = new(StringComparer.Ordinal);
    private readonly Dictionary<int, List<Capability>> _capabilities = new();

    private readonly struct Capability
    {
      public readonly string Signal;
      public readonly bool AllowClear;
      public readonly bool IsPrefix;

      public Capability(string signal, bool allowClear, bool isPrefix)
      {
        Signal = signal;
        AllowClear = allowClear;
        IsPrefix = isPrefix;
      }

      public bool Matches(string signal) => IsPrefix
        ? signal.StartsWith(Signal, StringComparison.Ordinal)
        : string.Equals(signal, Signal, StringComparison.Ordinal);
    }

    public void ConfigureScenario(ScenarioGraph graph)
    {
      ClearAll();
      if (graph == null)
        return;

      // 서버 전용으로 분류하는 대상은 "그래프 엔진이 서버에서 스스로 계산해 내보내는" 출력뿐이다.
      // 아이템 제출과 ActingNpc 상호작용의 완료 신호는 그래프가 식별자를 지정할 뿐이고, 실제 발신은
      // 제출 UI(ItemSubmissionInteractable.NotifySubmissionCompleted)와 상호작용 디스패치
      // (ScenarioActingNpcSignalInteract.Interact)가 조작한 클라이언트에서 수행한다. 이 둘을 서버 전용으로
      // 분류하면 호스트가 아닌 플레이어는 해당 단계를 영영 완료할 수 없다. 게다가 제출 검증과 아이템 소모
      // 자체가 클라이언트 권위이므로, 서버 전용 분류가 실제로 막아 주는 위조도 없다. 그래서 이 둘은
      // client-origin 으로 두고, 허용 여부는 그래프의 client signal 선언이 정하도록 한다.
      foreach (var node in graph.Nodes.Values)
      {
        switch (node)
        {
          case ScenarioSignalCounterNode counter:
            AddServerOnly(counter.OutputSignalIdentifier);
            break;
          case ScenarioSignalListenerNode listener:
            AddServerOnly(listener.OutputSignalIdentifier);
            break;
          case ScenarioEntityStateSignalBindingNode binding:
            AddServerOnly(binding.OutputSignalIdentifier);
            break;
        }
      }

      foreach (var signal in graph.ClientSignalIdentifiers ?? Array.Empty<string>())
        AddExpected(signal);
      foreach (var prefix in graph.ClientSignalPrefixes ?? Array.Empty<string>())
        AddPrefix(prefix);

      _expectedRaises.ExceptWith(_serverOnlySignals);
    }

    public void ClearScenario()
    {
      _expectedRaises.Clear();
      _allowedPrefixes.Clear();
      _serverOnlySignals.Clear();
    }

    public void ClearAll()
    {
      ClearScenario();
      _capabilities.Clear();
    }

    public void Grant(int clientId, string normalizedSignalId, bool allowClear = false, bool isPrefix = false)
    {
      if (clientId < 0 || string.IsNullOrWhiteSpace(normalizedSignalId))
        return;
      if (!_capabilities.TryGetValue(clientId, out var values))
      {
        values = new List<Capability>();
        _capabilities.Add(clientId, values);
      }
      values.Add(new Capability(normalizedSignalId, allowClear, isPrefix));
    }

    public void Revoke(int clientId) => _capabilities.Remove(clientId);

    public bool CanRaise(int clientId, string playerIdentifier, string normalizedSignalId, out string error)
      => CanRaise(clientId, playerIdentifier, normalizedSignalId, out error, out _);

    public bool CanRaise(int clientId, string playerIdentifier, string normalizedSignalId, out string error,
      out ScenarioClientSignalRejection rejection)
    {
      if (HasCapability(clientId, normalizedSignalId, requireClear: false))
      {
        error = null;
        rejection = ScenarioClientSignalRejection.None;
        return true;
      }
      if (_serverOnlySignals.Contains(normalizedSignalId))
      {
        error = "서버 검증 gameplay 상태에서만 발생할 수 있는 신호입니다.";
        rejection = ScenarioClientSignalRejection.ServerOnlySignal;
        return false;
      }
      if (_expectedRaises.Contains(normalizedSignalId))
      {
        error = null;
        rejection = ScenarioClientSignalRejection.None;
        return true;
      }

      foreach (var prefix in _allowedPrefixes)
      {
        if (normalizedSignalId.StartsWith(prefix, StringComparison.Ordinal))
        {
          error = null;
          rejection = ScenarioClientSignalRejection.None;
          return true;
        }
      }

      error = "활성 시나리오가 이 client-origin 신호를 기대하지 않으며 capability도 없습니다.";
      rejection = ScenarioClientSignalRejection.Undeclared;
      return false;
    }

    public bool CanClear(int clientId, string normalizedSignalId, out string error)
      => CanClear(clientId, normalizedSignalId, out error, out _);

    public bool CanClear(int clientId, string normalizedSignalId, out string error,
      out ScenarioClientSignalRejection rejection)
    {
      if (HasCapability(clientId, normalizedSignalId, requireClear: true))
      {
        error = null;
        rejection = ScenarioClientSignalRejection.None;
        return true;
      }
      error = "client-origin signal clear는 명시적인 clear capability 없이는 허용되지 않습니다.";
      rejection = ScenarioClientSignalRejection.Undeclared;
      return false;
    }

    private bool HasCapability(int clientId, string signal, bool requireClear)
      => _capabilities.TryGetValue(clientId, out var values)
         && values.Any(value => (!requireClear || value.AllowClear) && value.Matches(signal));

    private void AddExpected(string signal)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signal);
      if (!string.IsNullOrWhiteSpace(normalized))
        _expectedRaises.Add(normalized);
    }

    private void AddPrefix(string signal)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signal);
      if (!string.IsNullOrWhiteSpace(normalized))
        _allowedPrefixes.Add(normalized);
    }

    private void AddServerOnly(string signal)
    {
      string normalized = ScenarioInteractionSignals.Normalize(signal);
      if (!string.IsNullOrWhiteSpace(normalized))
        _serverOnlySignals.Add(normalized);
    }

  }
}
