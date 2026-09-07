using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  public sealed partial class ScenarioNetworkRelay
  {
    /// <summary>
    /// 역할 등록 유예. 이 시간 안에는 접속 중인 참여자 전원의 역할 등록을 기다리고, 지나면 역할 없는
    /// 참여자를 제외하고 배정한다. 태그 복제가 늦는 정상 상황을 덮을 만큼만 길다.
    /// </summary>
    private const double CompatibilityAllocationGraceSeconds = 10d;

    /// <summary>
    /// 합류 배리어의 최후 상한. 피어별 대기는 모두 복구 상한으로 끝나므로 정상 흐름에서는 이 값에
    /// 닿지 않는다. 알 수 없는 원인으로 한 피어가 완료 보고를 영영 못 보내는 경우에만 나머지 인원을
    /// 풀어 주기 위한 값이며, 임상 단계 하나가 이보다 오래 걸리지 않도록 넉넉하게 잡는다.
    /// </summary>
    private const double CompatibilityJoinLastResortSeconds = 1800d;

    private string _compatibilitySession;
    private string _compatibilityGraph;
    private readonly Dictionary<int, NetworkConnection> _compatibilityParticipants = new();
    private readonly HashSet<int> _departedCompatibilityParticipants = new();
    private readonly Dictionary<string, ScenarioCompletionBarrier> _compatibilityBarriers = new(StringComparer.Ordinal);
    private static string _localCompatibilitySession;
    private static string _localCompatibilityGraph;
    private static readonly Dictionary<string, int> CompatibilityVisits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int[]> _compatibilityAllocations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> _compatibilityAllocationRequestedAt = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> CompatibilityAllocationVisits = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int[]> ReceivedCompatibilityAllocations = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, bool> ReleasedCompatibilityBarriers = new(StringComparer.Ordinal);

    public static string BeginCompatibilitySession(string graphIdentifier, IEnumerable<NetworkConnection> participants)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted) return null;
      _instance._compatibilitySession = Guid.NewGuid().ToString("N");
      _instance._compatibilityGraph = graphIdentifier;
      _instance._compatibilityParticipants.Clear();
      _instance._departedCompatibilityParticipants.Clear();
      _instance._compatibilityBarriers.Clear();
      _instance._compatibilityAllocations.Clear();
      _instance._compatibilityAllocationRequestedAt.Clear();
      foreach (var participant in participants)
        if (participant != null)
          _instance._compatibilityParticipants[participant.ClientId] = participant;
      return _instance._compatibilitySession;
    }

    public static void ConfigureCompatibilitySession(string graphIdentifier, string session)
    {
      _localCompatibilityGraph = graphIdentifier;
      _localCompatibilitySession = session;
      CompatibilityVisits.Clear();
      CompatibilityAllocationVisits.Clear();
      ReceivedCompatibilityAllocations.Clear();
      ReleasedCompatibilityBarriers.Clear();
    }

    /// <summary>이 피어가 지금 <paramref name="graphIdentifier"/> 에 대해 속한 호환 세션. 없으면 null.</summary>
    public static string GetLocalCompatibilitySession(string graphIdentifier)
      => !string.IsNullOrEmpty(_localCompatibilitySession) && _localCompatibilityGraph == graphIdentifier
        ? _localCompatibilitySession
        : null;

    /// <summary>
    /// 이 피어의 흐름이 끝났음을 서버 합류 배리어에 알린다. 접속은 유지한 채 흐름만 끝난 피어가
    /// "완료 보고 없는 참여자"로 남아 나머지 인원의 합류를 막지 않게 한다.
    /// <paramref name="session"/> 은 끝나는 실행이 시작될 때의 세션이어야 하며, 현재 세션과 다르면
    /// (그 사이에 새 실행이 시작된 경우) 서버가 무시한다.
    /// </summary>
    public static void LeaveCompatibilitySession(string graphIdentifier, string session)
    {
      if (_instance == null || !InstanceFinder.IsClientStarted
          || string.IsNullOrEmpty(session) || string.IsNullOrEmpty(graphIdentifier))
        return;
      _instance.CmdLeaveCompatibilitySession(session, graphIdentifier);
    }

    public static IEnumerator WaitForCompatibilityJoin(string graphIdentifier, string nodeIdentifier, Action onRecovery)
    {
      if (_instance == null || !InstanceFinder.IsClientStarted || string.IsNullOrEmpty(_localCompatibilitySession)
          || _localCompatibilityGraph != graphIdentifier)
        yield break;
      string session = _localCompatibilitySession;
      CompatibilityVisits.TryGetValue(nodeIdentifier, out var visit);
      CompatibilityVisits[nodeIdentifier] = ++visit;
      string key = nodeIdentifier + "|" + visit;
      // Only the server can release a join. A short local deadline must not let one
      // peer enter the next clinical stage while another is still doing valid work.
      // The server applies CompatibilityJoinLastResortSeconds as the only upper bound.
      double retryAt = 0d;
      while (session == _localCompatibilitySession && InstanceFinder.IsClientStarted
             && !ReleasedCompatibilityBarriers.ContainsKey(key))
      {
        if (Time.realtimeSinceStartupAsDouble >= retryAt)
        {
          _instance.CmdCompleteCompatibilityParallel(session, graphIdentifier, nodeIdentifier, visit);
          retryAt = Time.realtimeSinceStartupAsDouble + 15d;
        }
        yield return null;
      }
      if (session == _localCompatibilitySession
          && ReleasedCompatibilityBarriers.TryGetValue(key, out var recovered) && recovered)
        onRecovery?.Invoke();
    }

    public static bool HasCompatibilitySession(string graphIdentifier)
      => _instance != null && InstanceFinder.IsClientStarted
         && !string.IsNullOrEmpty(_localCompatibilitySession) && _localCompatibilityGraph == graphIdentifier;

    /// <summary>
    /// 서버가 내려 주는 ByRole 분기 배정을 기다린다. <paramref name="timeoutSeconds"/> 안에 배정이 오지
    /// 않으면 <paramref name="apply"/> 를 부르지 않고 끝나며, 호출자가 로컬 배정으로 복구한다.
    /// </summary>
    public static IEnumerator WaitForCompatibilityAllocation(string graphIdentifier, string nodeIdentifier,
      Action<int[]> apply, double timeoutSeconds = double.PositiveInfinity)
    {
      string session = _localCompatibilitySession;
      CompatibilityAllocationVisits.TryGetValue(nodeIdentifier, out int visit);
      CompatibilityAllocationVisits[nodeIdentifier] = ++visit;
      string key = nodeIdentifier + "|" + visit;
      double retryAt = 0d;
      double deadline = Time.realtimeSinceStartupAsDouble + timeoutSeconds;
      while (session == _localCompatibilitySession && InstanceFinder.IsClientStarted)
      {
        if (ReceivedCompatibilityAllocations.TryGetValue(key, out var owners))
        {
          apply(owners);
          yield break;
        }
        if (Time.realtimeSinceStartupAsDouble >= deadline)
          yield break;
        if (Time.realtimeSinceStartupAsDouble >= retryAt)
        {
          _instance.CmdRequestCompatibilityAllocation(session, graphIdentifier, nodeIdentifier, visit);
          retryAt = Time.realtimeSinceStartupAsDouble + 1d;
        }
        yield return null;
      }
    }

    private bool IsCompatibilityParticipant(NetworkConnection sender, string session, string graphIdentifier)
      => sender != null && session == _compatibilitySession && graphIdentifier == _compatibilityGraph
         && _compatibilityParticipants.TryGetValue(sender.ClientId, out var participant) && participant == sender;

    [ServerRpc(RequireOwnership = false)]
    private void CmdLeaveCompatibilitySession(string session, string graphIdentifier, NetworkConnection sender = null)
    {
      if (!IsCompatibilityParticipant(sender, session, graphIdentifier))
        return;
      _departedCompatibilityParticipants.Add(sender.ClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestCompatibilityAllocation(string session, string graphIdentifier, string nodeIdentifier,
      int visit, NetworkConnection sender = null)
    {
      if (!IsCompatibilityParticipant(sender, session, graphIdentifier) || visit < 1)
        return;
      // 흐름이 끝났다고 알렸던 피어가 다시 배정을 요청하면(같은 세션 안의 재시작) 참여자로 되돌린다.
      _departedCompatibilityParticipants.Remove(sender.ClientId);
      if (!Registry.Registry.TryGetScenarioGraph(graphIdentifier, out ScenarioGraph graph, out _)
          || !graph.TryGetNode(nodeIdentifier, out var node) || node is not ScenarioParallelNode parallel
          || parallel.AllocationType != ScenarioParallelAllocationType.ByRole)
        return;
      string key = nodeIdentifier + "|" + visit;
      if (!_compatibilityAllocations.TryGetValue(key, out var owners))
      {
        var connected = GetConnectedCompatibilityParticipants();
        var controller = ScenarioController.Instance;
        if (controller == null)
          return;
        double now = Time.realtimeSinceStartupAsDouble;
        if (!_compatibilityAllocationRequestedAt.TryGetValue(key, out double requestedAt))
        {
          requestedAt = now;
          _compatibilityAllocationRequestedAt[key] = now;
        }
        bool graceElapsed = now - requestedAt >= CompatibilityAllocationGraceSeconds;
        if (!controller.TryAllocateCompatibilityRoles(graph, parallel, connected, out owners,
              requireEveryParticipantRegistered: !graceElapsed))
        {
          if (!graceElapsed)
            return; // Registration is still in flight. The client retries; no branch is discarded.
          // 유예가 지나도 로스터를 만들 수 없으면(중복 역할 등) 어느 분기도 배정하지 않고 응답한다.
          // 응답이 없으면 네 명이 모두 이 노드에서 멈추므로, 분기를 건너뛰는 쪽이 낫다.
          Debug.LogWarning(
            $"[ScenarioNetworkRelay] Compatibility role allocation for '{nodeIdentifier}' could not be built "
            + $"{CompatibilityAllocationGraceSeconds:0}s after the first request; skipping every branch.");
          owners = Enumerable.Repeat(-1, parallel.Branches?.Count ?? 0).ToArray();
        }
        else if (graceElapsed)
        {
          Debug.LogWarning(
            $"[ScenarioNetworkRelay] Compatibility role allocation for '{nodeIdentifier}' proceeded without "
            + "every connected participant holding a role.");
        }
        _compatibilityAllocations.Add(key, owners);
      }
      TargetCompatibilityAllocation(sender, session, key, owners);
    }

    [TargetRpc]
    private void TargetCompatibilityAllocation(NetworkConnection target, string session, string key, int[] owners)
    {
      if (session == _localCompatibilitySession)
        ReceivedCompatibilityAllocations[key] = owners;
    }

    private HashSet<int> GetConnectedCompatibilityParticipants()
    {
      var clients = InstanceFinder.ServerManager?.Clients;
      return new HashSet<int>(_compatibilityParticipants
        .Where(pair => pair.Value != null && pair.Value.IsActive && clients != null
          && clients.Values.Contains(pair.Value)
          && !_departedCompatibilityParticipants.Contains(pair.Key)).Select(pair => pair.Key));
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompleteCompatibilityParallel(string session, string graphIdentifier, string nodeIdentifier,
      int visit, NetworkConnection sender = null)
    {
      if (!IsCompatibilityParticipant(sender, session, graphIdentifier) || visit < 1)
        return;
      _departedCompatibilityParticipants.Remove(sender.ClientId);
      if (!Registry.Registry.TryGetScenarioGraph(graphIdentifier, out ScenarioGraph graph, out _)
          || !graph.TryGetNode(nodeIdentifier, out var node) || node is not ScenarioParallelNode parallel
          || parallel.WaitMode != ScenarioWaitMode.All)
        return;
      string key = nodeIdentifier + "|" + visit;
      if (!_compatibilityBarriers.TryGetValue(key, out var barrier))
      {
        barrier = new ScenarioCompletionBarrier(_compatibilityParticipants.Keys,
          Time.realtimeSinceStartupAsDouble + CompatibilityJoinLastResortSeconds);
        _compatibilityBarriers.Add(key, barrier);
        StartCoroutine(ReleaseCompatibilityBarrier(session, graphIdentifier, key, barrier));
      }
      barrier.Complete(sender.ClientId);
      // A late participant must receive a release that was already sent to the other participants.
      if (barrier.Released)
        TargetReleaseCompatibilityBarrier(sender, session, key, barrier.TimedOut);
    }

    private IEnumerator ReleaseCompatibilityBarrier(string session, string graphIdentifier, string key, ScenarioCompletionBarrier barrier)
    {
      while (session == _compatibilitySession)
      {
        var connected = GetConnectedCompatibilityParticipants();
        if (barrier.Evaluate(connected, Time.realtimeSinceStartupAsDouble))
        {
          if (barrier.TimedOut)
            Debug.LogWarning(
              $"[ScenarioNetworkRelay] Compatibility join '{key}' released by the last-resort deadline; "
              + "some participants never reported completion.");
          foreach (int clientId in connected)
            TargetReleaseCompatibilityBarrier(_compatibilityParticipants[clientId], session, key, barrier.TimedOut);
          yield break;
        }
        yield return null;
      }
    }

    [TargetRpc]
    private void TargetReleaseCompatibilityBarrier(NetworkConnection target, string session, string key, bool timedOut)
    {
      if (session == _localCompatibilitySession)
        ReleasedCompatibilityBarriers[key] = timedOut;
    }
  }
}
