#if UNITY_E2E || UNITY_EDITOR
using Newtonsoft.Json.Linq;
namespace MultiplayerInfrastructure.UI
{
  public partial class DialoguePanelUIController
  {
    private string _automationNodeId, _automationGraphId;
    private long _automationPresentationRevision;
    internal void AutomationSetNode(string graphId,string nodeId) {
      _automationGraphId=graphId;_automationNodeId=nodeId;_automationPresentationRevision++;
    }
    internal JObject AutomationSnapshot()
    {
      var choices = new JArray();
      if (_pendingChoiceOptions != null)
        for (int index = 0; index < _pendingChoiceOptions.Count; index++)
        {
          var option = _pendingChoiceOptions[index];
          choices.Add(new JObject { ["index"] = index, ["text"] = option.DisplayText,
            ["choiceId"] = _automationNodeId + "#" + index, ["nextNodeId"] = option.NextNodeIdentifier });
        }
      return new JObject {
        ["graphId"] = _automationGraphId ?? _owningGraphIdentifier, ["nodeId"] = _automationNodeId,
        ["isTextAnimating"] = _isTyping, ["canAdvance"] = _isWaitingForInput,
        ["hasChoices"] = HasActiveSelections, ["choices"] = choices,
        ["selectedIndex"] = _interactableHintUI == null ? -1 : _interactableHintUI.GetSelectedIndex(),
        ["phase"] = _inputContext.ToString(), ["presentationRevision"] = _automationPresentationRevision,
        ["text"] = _fullText
      };
    }
  }
}
#endif
