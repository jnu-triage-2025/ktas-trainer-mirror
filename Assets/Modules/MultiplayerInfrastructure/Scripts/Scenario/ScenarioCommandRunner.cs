using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using MultiplayerInfrastructure.Registry;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// Server-side helper that dispatches scenario graphs to specific players via RPC.
  /// Scenarios are pre-registered into the global Registry on Awake (server and client).
  /// </summary>
  public class ScenarioCommandRunner : NetworkBehaviour
  {
    [SerializeField] private ScenarioGraphRegistryRequirement[] _scenarios = Array.Empty<ScenarioGraphRegistryRequirement>();

    void Awake()
    {
      foreach (var req in _scenarios)
      {
        if (string.IsNullOrWhiteSpace(req.identifier) || req.scenarioGraphAsset == null)
          continue;

        Registry.Registry.Register(RegistryType.ScenarioGraph, req.identifier, req.scenarioGraphAsset);
      }
    }

    public bool TryExecuteScenario(string scenarioIdentifier, IEnumerable<NetworkConnection> targets, out string error)
    {
      error = string.Empty;

      if (!IsServer)
      {
        error = "Scenario execution can only be invoked on the server.";
        return false;
      }

      if (!Registry.Registry.Contains(RegistryType.ScenarioGraph, scenarioIdentifier))
      {
        error = $"Scenario '{scenarioIdentifier}' is not registered.";
        return false;
      }

      bool anyTarget = false;
      foreach (var target in targets ?? Array.Empty<NetworkConnection>())
      {
        if (target == null)
          continue;

        anyTarget = true;
        int ownerId = target.ClientId >= 0 ? (int)target.ClientId : -1;
        TargetRunScenario(target, scenarioIdentifier, ownerId);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }

    [TargetRpc]
    private void TargetRunScenario(NetworkConnection conn, string scenarioIdentifier, int ownerClientId)
    {
      if (!Registry.Registry.TryGetScenarioGraph(scenarioIdentifier, out ScenarioGraph graph, out string error))
      {
        Debug.LogWarning($"[ScenarioCommandRunner] Failed to load scenario '{scenarioIdentifier}': {error}");
        return;
      }

      if (ScenarioController.Instance == null)
      {
        Debug.LogWarning("[ScenarioCommandRunner] ScenarioController is missing on this client.");
        return;
      }

      int? owner = ownerClientId >= 0 ? ownerClientId : (int?)null;
      ScenarioController.Instance.StartScenario(graph, null, owner);
    }
  }
}
