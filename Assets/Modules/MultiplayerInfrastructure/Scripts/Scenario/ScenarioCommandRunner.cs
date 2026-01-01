using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// Server-side helper that dispatches scenario graphs to specific players via RPC.
  /// </summary>
  public class ScenarioCommandRunner : NetworkBehaviour
  {
    [SerializeField] private ScenarioRegistry _registry;

    void Awake()
    {
      if (_registry == null)
        _registry = GetComponent<ScenarioRegistry>();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
      if (_registry == null)
        _registry = GetComponent<ScenarioRegistry>();
    }
#endif

    public bool TryExecuteScenario(string scenarioIdentifier, IEnumerable<NetworkConnection> targets, out string error)
    {
      error = string.Empty;

      if (!IsServer)
      {
        error = "Scenario execution can only be invoked on the server.";
        return false;
      }

      if (_registry == null)
      {
        error = "Scenario registry is not configured on the server.";
        return false;
      }

      if (!_registry.TryGetScenarioJson(scenarioIdentifier, out string scenarioJson, out error))
        return false;

      bool anyTarget = false;
      foreach (var target in targets ?? Array.Empty<NetworkConnection>())
      {
        if (target == null)
          continue;

        anyTarget = true;
        int ownerId = target.ClientId >= 0 ? (int)target.ClientId : -1;
        TargetRunScenario(target, scenarioIdentifier, scenarioJson, ownerId);
      }

      if (!anyTarget)
      {
        error = "No target players were matched.";
        return false;
      }

      return true;
    }

    [TargetRpc]
    private void TargetRunScenario(NetworkConnection conn, string scenarioIdentifier, string scenarioJson, int ownerClientId)
    {
      if (string.IsNullOrWhiteSpace(scenarioJson))
      {
        Debug.LogWarning("[ScenarioCommandRunner] Scenario json is empty; aborting.");
        return;
      }

      ScenarioGraph graph;
      try
      {
        graph = ScenarioGraphLoader.LoadFromJson(scenarioJson);
      }
      catch (Exception ex)
      {
        Debug.LogError($"[ScenarioCommandRunner] Failed to deserialize scenario '{scenarioIdentifier}': {ex.Message}");
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
