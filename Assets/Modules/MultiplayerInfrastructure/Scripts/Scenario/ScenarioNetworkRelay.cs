using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    private const int MaxSignalUpdatesPerPlayerPerWindow = 30;
    private const float SignalUpdateWindowSeconds = 1f;
    private static readonly Dictionary<string, Queue<float>> SignalUpdateTimesByPlayer = new(StringComparer.Ordinal);
    private static readonly ScenarioClientSignalAuthorization ClientSignalAuthorization = new();
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
        // 이 노드들은 서버 상태기만 실행하면 되고, 클라이언트별 별도 표현/입력 계약이 없다.
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
            || node is ScenarioNPCControlNode)
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
      ClientSignalAuthorization.ClearScenario();
      InstanceFinder.NetworkManager.ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
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
      ObserversUpdateNPCControl(
        actorObject, nodeIdentifier, displayName,
        hasShowOverheadName, showOverheadName, interactOperation,
        interactableIdentifier, hasInteractEnabled, interactEnabled);
    }

    [TargetRpc]
    private void TargetConfigureScenarioActingNpc(
      NetworkConnection connection, string graphIdentifier, string actingNpcIdentifier, NetworkObject actorObject)
    {
      ObserversConfigureScenarioActingNpc(graphIdentifier, actingNpcIdentifier, actorObject);
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
      => ClientSignalAuthorization.ConfigureScenario(graph);

    internal static void ClearClientSignalAuthorization()
      => ClientSignalAuthorization.ClearAll();

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
            sender.ClientId, descriptor.Identifier, normalizedSignalId, out string authorizationError))
      {
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

      string authorizationError = null;
      if (sender == null || !UserDescriptorService.TryGetByClientId(sender.ClientId, out var descriptor)
          || string.IsNullOrWhiteSpace(descriptor.Identifier)
          || !ClientSignalAuthorization.CanClear(sender.ClientId, normalizedSignalId, out authorizationError))
      {
        ReportRejectedInput(normalizedSignalId, null,
          authorizationError ?? "발신 플레이어를 서버 세션에서 확인할 수 없습니다.", sender);
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
      RpcMirrorClearScenarioSignal(normalizedSignalId);
    }

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
          case ScenarioItemSubmissionConfigNode submission:
            AddServerOnly(submission.CompletionSignalIdentifier);
            break;
        }
      }

      foreach (var actingNpc in graph.ActingNpcs ?? Array.Empty<ScenarioActingNpcDefinition>())
      foreach (var interaction in actingNpc?.Interactions ?? Array.Empty<ScenarioActingNpcInteractionDefinition>())
        AddServerOnly(interaction?.CompletionSignalIdentifier);

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
    {
      if (HasCapability(clientId, normalizedSignalId, requireClear: false))
      {
        error = null;
        return true;
      }
      if (_serverOnlySignals.Contains(normalizedSignalId))
      {
        error = "서버 검증 gameplay 상태에서만 발생할 수 있는 신호입니다.";
        return false;
      }
      if (_expectedRaises.Contains(normalizedSignalId))
      {
        error = null;
        return true;
      }

      foreach (var prefix in _allowedPrefixes)
      {
        if (normalizedSignalId.StartsWith(prefix, StringComparison.Ordinal))
        {
          error = null;
          return true;
        }
      }

      error = "활성 시나리오가 이 client-origin 신호를 기대하지 않으며 capability도 없습니다.";
      return false;
    }

    public bool CanClear(int clientId, string normalizedSignalId, out string error)
    {
      if (HasCapability(clientId, normalizedSignalId, requireClear: true))
      {
        error = null;
        return true;
      }
      error = "client-origin signal clear는 명시적인 clear capability 없이는 허용되지 않습니다.";
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
