#if UNITY_E2E || UNITY_EDITOR
using System;
using MultiplayerInfrastructure.Automation;
using Newtonsoft.Json.Linq;
namespace MultiplayerInfrastructure.Scenario
{
  public partial class ScenarioController
  {
    partial void AutomationPresentation(IScenarioNode node)
    {
      if (AutomationConfiguration.Valid && _uiController != null) _uiController.AutomationSetNode(_currentGraph?.Identifier,node?.Identifier);
    }
    private string _automationExecutionId;
    partial void AutomationEvent(string type, string key, string value, int? clientId, long activationId)
    {
      if (!AutomationConfiguration.Valid) return;
      try {
        if (type == "scenario.started") _automationExecutionId = Guid.NewGuid().ToString("N");
        var payload = new JObject { ["graphId"] = _currentGraph?.Identifier, ["executionId"] = _automationExecutionId,
          ["nodeActivationId"] = activationId, ["clientId"] = clientId };
        if (key != null) { payload["key"] = key; payload["value"] = value; }
        if (type == "quest.roleFlagsChanged") payload["flags"] = new JArray(_questStateFlagsByRole[value]);
        AutomationEvents.Publish(type, _executionMode == ExecutionMode.ServerAuthoritative ? "server" : "client", payload);
      } catch { }
    }
    internal JObject AutomationSnapshot() => new JObject {
      ["executionId"] = _automationExecutionId, ["graphId"] = _currentGraph?.Identifier, ["nodeId"] = _currentNode?.Identifier,
      ["state"] = _state.ToString(), ["executionMode"] = _executionMode.ToString(),
      ["side"] = _executionMode == ExecutionMode.ServerAuthoritative ? "server" : "client",
      ["nodeActivationId"] = _activeMainNodeVisitSequence,
      ["presentationToken"] = _receivedPresentationToken ?? _mainPresentationToken,
      ["branchPresentationTokens"] = JObject.FromObject(_branchPresentationTokens),
      ["branchesByClientId"] = JObject.FromObject(_activeRoleBranchIdentifiersByClientId),
      ["stateValues"] = JObject.FromObject(_stateStore),
      ["questFlagsByRole"] = JObject.FromObject(_questStateFlagsByRole),
      ["recoveryNotes"] = new JArray(_recoveryNotes)
    };
  }
}
#endif
