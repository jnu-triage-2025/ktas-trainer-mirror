using System.Collections.Generic;

namespace MultiplayerInfrastructure.Dialogue
{
  /// <summary>
  /// 하나의 대화 시나리오 전체를 나타냅니다.
  /// </summary>
  public class DialogueSession
  {
    #region Properties

    public string DialogueId { get; }
    public DialogueMetadata Metadata { get; }
    public IReadOnlyList<DialogueNodeData> Nodes { get; }

    #endregion

    #region Private Fields

    private readonly Dictionary<string, DialogueNodeData> _nodeLookup;

    #endregion

    #region Constructor

    public DialogueSession(
        string dialogueId,
        DialogueMetadata metadata,
        List<DialogueNodeData> nodes)
    {
      DialogueId = dialogueId;
      Metadata = metadata ?? new DialogueMetadata();
      Nodes = nodes ?? new List<DialogueNodeData>();

      _nodeLookup = new Dictionary<string, DialogueNodeData>();
      foreach (var node in Nodes)
      {
        if (!string.IsNullOrEmpty(node.Identifier))
        {
          _nodeLookup[node.Identifier] = node;
        }
      }
    }

    #endregion

    #region Public Methods

    public DialogueNodeData GetFirstNode()
    {
      return Nodes.Count > 0 ? Nodes[0] : null;
    }

    public DialogueNodeData GetNode(string identifier)
    {
      if (string.IsNullOrEmpty(identifier))
        return null;

      _nodeLookup.TryGetValue(identifier, out var node);
      return node;
    }

    public DialogueNodeData GetNextNode(DialogueNodeData currentNode)
    {
      if (currentNode == null)
        return null;

      return GetNode(currentNode.NextDialogueIdentifier);
    }

    public DialogueNodeData GetNextNode(DialogueSelectionData selection)
    {
      if (selection == null)
        return null;

      return GetNode(selection.NextDialogueIdentifier);
    }

    #endregion
  }
}
