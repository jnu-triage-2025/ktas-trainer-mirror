#if UNITY_E2E || UNITY_EDITOR
using Newtonsoft.Json.Linq;
namespace MultiplayerInfrastructure.Scenario
{
  public sealed partial class ScenarioNetworkRelay
  {
    internal static void AutomationSendClientSignal(string signalId, string parameterJson, int count)
    {
      if (FishNet.InstanceFinder.IsServerStarted) throw new System.InvalidOperationException("REMOTE_CLIENT_REQUIRED");
      if (_instance == null || !_instance.IsClientInitialized) throw new System.InvalidOperationException("CLIENT_NOT_CONNECTED");
      for (int i = 0; i < count; i++) _instance.CmdRaiseScenarioSignal(signalId, parameterJson, _instance._receivedSignalEpoch);
    }
    internal static JObject AutomationRoleAllocations()
    {
      return new JObject {
        ["server"] = _instance != null && FishNet.InstanceFinder.IsServerStarted ? new JObject {
          ["sessionId"] = _instance._compatibilitySession, ["graphId"] = _instance._compatibilityGraph,
          ["allocations"] = JObject.FromObject(_instance._compatibilityAllocations)
        } : null,
        ["client"] = new JObject { ["sessionId"] = _localCompatibilitySession,
          ["graphId"] = _localCompatibilityGraph,
          ["allocations"] = JObject.FromObject(ReceivedCompatibilityAllocations) }
      };
    }
    internal static JObject AutomationCompatibility(string graphId)
    {
      if (!Registry.Registry.TryGetScenarioGraph(graphId, out ScenarioGraph graph, out var error))
        return new JObject { ["supported"] = false, ["reason"] = error };
      bool unsupported = TryGetUnsupportedAuthoritativeNode(graph, out var node);
      return new JObject { ["supported"] = !unsupported, ["unsupportedNodeId"] = node?.Identifier,
        ["unsupportedNodeType"] = node?.GetType().Name };
    }
  }
}
#endif
