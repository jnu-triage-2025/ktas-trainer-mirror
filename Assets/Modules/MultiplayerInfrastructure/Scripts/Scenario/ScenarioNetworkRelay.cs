using FishNet;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
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
  /// 수행하도록 Dialogue·Choice의 표현 RPC를 제공한다. 다른 노드의 역할별 표현은 후속 단계에서
  /// 같은 경로로 확장한다.
  /// 단일 플레이어(호스트 단독)에서는 서버=클라 이므로 동작이 기존과 동일하다.
  /// </summary>
  public sealed class ScenarioNetworkRelay : NetworkBehaviour
  {
    private static ScenarioNetworkRelay _instance;

    /// <summary>씬에 배치된 중계기 인스턴스(없으면 null).</summary>
    public static ScenarioNetworkRelay Instance => _instance;

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

      if (!Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out var error))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Failed to load authoritative scenario '{scenarioIdentifier}': {error}");
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

      if (!Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out var error))
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
    public static void RaiseAuthoritative(string normalizedSignalId)
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
        ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
        if (_instance != null)
        {
          _instance.RpcMirrorRaiseScenarioSignal(normalizedSignalId);
        }
        return;
      }

      // 클라이언트 컨텍스트: 중계기로 서버 보고.
      if (_instance != null && InstanceFinder.IsClientStarted)
      {
        _instance.CmdRaiseScenarioSignal(normalizedSignalId);
        return;
      }

      // 네트워크 비활성/중계기 부재: 로컬 폴백.
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
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

      return normalizedSignalId.StartsWith(ScenarioInteractionSignals.Prefix, System.StringComparison.Ordinal);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRaiseScenarioSignal(string normalizedSignalId)
    {
      if (!IsValidClientSignal(normalizedSignalId))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected invalid client signal raise: '{normalizedSignalId}'");
        return;
      }

      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
      // 서버 기록 후 모든 클라이언트(호스트 포함)에 미러링하여
      // 각 피어 로컬의 Validator 폴링이 통과되도록 한다.
      RpcMirrorRaiseScenarioSignal(normalizedSignalId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdClearScenarioSignal(string normalizedSignalId)
    {
      if (!IsValidClientSignal(normalizedSignalId))
      {
        Debug.LogWarning($"[ScenarioNetworkRelay] Rejected invalid client signal clear: '{normalizedSignalId}'");
        return;
      }

      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
      RpcMirrorClearScenarioSignal(normalizedSignalId);
    }

    /// <summary>
    /// 서버의 권위 신호 기록을 모든 클라이언트 로컬 레지스트리로 미러링한다.
    /// </summary>
    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorRaiseScenarioSignal(string normalizedSignalId)
    {
      // 호스트(서버=클라)에서는 이미 서버 경로에서 기록되었으므로 중복 기록해도 무해(idempotent)하다.
      ScenarioInteractionSignals.RegisterLocal(normalizedSignalId);
    }

    [ObserversRpc(BufferLast = false)]
    private void RpcMirrorClearScenarioSignal(string normalizedSignalId)
    {
      ScenarioInteractionSignals.UnregisterLocal(normalizedSignalId);
    }
  }
}
