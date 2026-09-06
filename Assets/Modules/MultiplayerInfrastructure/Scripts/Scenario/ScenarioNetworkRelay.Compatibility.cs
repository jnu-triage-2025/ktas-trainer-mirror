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
    private static readonly Dictionary<string, bool> ReleasedCompatibilityBarriers = new(StringComparer.Ordinal);

    public static string BeginCompatibilitySession(string graphIdentifier, IEnumerable<NetworkConnection> participants)
    {
      if (_instance == null || !InstanceFinder.IsServerStarted) return null;
      _instance._compatibilitySession = Guid.NewGuid().ToString("N");
      _instance._compatibilityGraph = graphIdentifier;
      _instance._compatibilityParticipants.Clear();
      _instance._compatibilityBarriers.Clear();
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
      _instance.CmdCompleteCompatibilityParallel(session, graphIdentifier, nodeIdentifier, visit);
      // The server releases the rendezvous after 180 seconds. Allow transport time for that decision.
      double deadline = Time.realtimeSinceStartupAsDouble + 195d;
      while (session == _localCompatibilitySession && !ReleasedCompatibilityBarriers.ContainsKey(key)
             && Time.realtimeSinceStartupAsDouble < deadline)
        yield return null;
      if (session == _localCompatibilitySession
          && (!ReleasedCompatibilityBarriers.TryGetValue(key, out var timedOut) || timedOut))
        onRecovery?.Invoke();
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
        barrier = new ScenarioCompletionBarrier(_compatibilityParticipants.Keys, Time.realtimeSinceStartupAsDouble + 180d);
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
        var clients = InstanceFinder.ServerManager?.Clients;
        var connected = new HashSet<int>(_compatibilityParticipants
          .Where(pair => pair.Value != null && pair.Value.IsActive && clients != null
            && clients.Values.Contains(pair.Value)).Select(pair => pair.Key));
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
