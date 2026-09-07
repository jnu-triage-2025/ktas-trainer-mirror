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
    private string _compatibilitySession;
    private string _compatibilityGraph;
    private readonly Dictionary<int, NetworkConnection> _compatibilityParticipants = new();
    private readonly Dictionary<string, ScenarioCompletionBarrier> _compatibilityBarriers = new(StringComparer.Ordinal);
    private static string _localCompatibilitySession;
    private static string _localCompatibilityGraph;
    private static readonly Dictionary<string, int> CompatibilityVisits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int[]> _compatibilityAllocations = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int> CompatibilityAllocationVisits = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, int[]> ReceivedCompatibilityAllocations = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, bool> ReleasedCompatibilityBarriers = new(StringComparer.Ordinal);

    public static string BeginCompatibilitySession(string graphIdentifier, IEnumerable<NetworkConnection> participants)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted) return null;
      _instance._compatibilitySession = Guid.NewGuid().ToString("N");
      _instance._compatibilityGraph = graphIdentifier;
      _instance._compatibilityParticipants.Clear();
      _instance._compatibilityBarriers.Clear();
      _instance._compatibilityAllocations.Clear();
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

    public static IEnumerator WaitForCompatibilityAllocation(string graphIdentifier, string nodeIdentifier,
      Action<int[]> apply)
    {
      string session = _localCompatibilitySession;
      CompatibilityAllocationVisits.TryGetValue(nodeIdentifier, out int visit);
      CompatibilityAllocationVisits[nodeIdentifier] = ++visit;
      string key = nodeIdentifier + "|" + visit;
      double retryAt = 0d;
      while (session == _localCompatibilitySession && InstanceFinder.IsClientStarted)
      {
        if (ReceivedCompatibilityAllocations.TryGetValue(key, out var owners))
        {
          apply(owners);
          yield break;
        }
        if (Time.realtimeSinceStartupAsDouble >= retryAt)
        {
          _instance.CmdRequestCompatibilityAllocation(session, graphIdentifier, nodeIdentifier, visit);
          retryAt = Time.realtimeSinceStartupAsDouble + 1d;
        }
        yield return null;
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdRequestCompatibilityAllocation(string session, string graphIdentifier, string nodeIdentifier,
      int visit, NetworkConnection sender = null)
    {
      if (sender == null || session != _compatibilitySession || graphIdentifier != _compatibilityGraph || visit < 1
          || !_compatibilityParticipants.TryGetValue(sender.ClientId, out var participant) || participant != sender)
        return;
      if (!Registry.Registry.TryGetScenarioGraph(graphIdentifier, out ScenarioGraph graph, out _)
          || !graph.TryGetNode(nodeIdentifier, out var node) || node is not ScenarioParallelNode parallel
          || parallel.AllocationType != ScenarioParallelAllocationType.ByRole)
        return;
      string key = nodeIdentifier + "|" + visit;
      if (!_compatibilityAllocations.TryGetValue(key, out var owners))
      {
        var connected = GetConnectedCompatibilityParticipants();
        var controller = ScenarioController.Instance;
        if (controller == null || !controller.TryAllocateCompatibilityRoles(graph, parallel, connected, out owners))
          return; // Registration is still in flight. The client retries; no branch is discarded.
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
          && clients.Values.Contains(pair.Value)).Select(pair => pair.Key));
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdCompleteCompatibilityParallel(string session, string graphIdentifier, string nodeIdentifier,
      int visit, NetworkConnection sender = null)
    {
      if (sender == null || session != _compatibilitySession || graphIdentifier != _compatibilityGraph || visit < 1
          || !_compatibilityParticipants.TryGetValue(sender.ClientId, out var participant) || participant != sender)
        return;
      if (!Registry.Registry.TryGetScenarioGraph(graphIdentifier, out ScenarioGraph graph, out _)
          || !graph.TryGetNode(nodeIdentifier, out var node) || node is not ScenarioParallelNode parallel
          || parallel.WaitMode != ScenarioWaitMode.All)
        return;
      string key = nodeIdentifier + "|" + visit;
      if (!_compatibilityBarriers.TryGetValue(key, out var barrier))
      {
        barrier = new ScenarioCompletionBarrier(_compatibilityParticipants.Keys);
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
