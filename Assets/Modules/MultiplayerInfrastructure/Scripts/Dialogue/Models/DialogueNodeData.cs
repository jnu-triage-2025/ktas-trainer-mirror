using System.Collections.Generic;
using UnityEngine;

namespace MultiplayerInfrastructure.Dialogue
{
  /// <summary>
  /// 대화 노드 데이터
  /// </summary>
  [System.Serializable]
  public class DialogueNodeData
  {
    [SerializeField] private string _nodeId;
    [SerializeField] private string _speakerName;
    [SerializeField] private string _dialogueText;
    [SerializeField] private Sprite _portrait;
    [SerializeField] private List<DialogueSelectionData> _selections = new();
    [SerializeField] private string _nextNodeId;
    [SerializeField] private float _customTypingSpeed = 0.05f;

    #region Properties

    public string NodeId
    {
      get => _nodeId;
      set => _nodeId = value;
    }

    /// <summary>
    /// NodeId의 별칭 (하위 호환성)
    /// </summary>
    public string Identifier
    {
      get => _nodeId;
      set => _nodeId = value;
    }

    public string SpeakerName
    {
      get => _speakerName;
      set => _speakerName = value;
    }

    public string DialogueText
    {
      get => _dialogueText;
      set => _dialogueText = value;
    }

    public Sprite Portrait
    {
      get => _portrait;
      set => _portrait = value;
    }

    public List<DialogueSelectionData> Selections
    {
      get => _selections;
      set => _selections = value ?? new List<DialogueSelectionData>();
    }

    public string NextNodeId
    {
      get => _nextNodeId;
      set => _nextNodeId = value;
    }

    /// <summary>
    /// NextNodeId의 별칭 (하위 호환성)
    /// </summary>
    public string NextDialogueIdentifier
    {
      get => _nextNodeId;
      set => _nextNodeId = value;
    }

    /// <summary>
    /// 커스텀 타이핑 속도
    /// </summary>
    public float CustomTypingSpeed
    {
      get => _customTypingSpeed;
      set => _customTypingSpeed = value;
    }

    /// <summary>
    /// 선택지가 있는지 여부
    /// </summary>
    public bool HasSelections => _selections != null && _selections.Count > 0;

    /// <summary>
    /// 다음 노드가 있는지 여부
    /// </summary>
    public bool HasNextNode => !string.IsNullOrEmpty(_nextNodeId);

    /// <summary>
    /// HasNextNode의 별칭 (하위 호환성)
    /// </summary>
    public bool HasNextDialogue => HasNextNode;

    #endregion

    #region Constructors

    public DialogueNodeData()
    {
      _selections = new List<DialogueSelectionData>();
      _customTypingSpeed = 0.05f;
    }

    public DialogueNodeData(string speakerName, string dialogueText, Sprite portrait = null)
    {
      _speakerName = speakerName;
      _dialogueText = dialogueText;
      _portrait = portrait;
      _selections = new List<DialogueSelectionData>();
      _customTypingSpeed = 0.05f;
    }

    public DialogueNodeData(string nodeId, string speakerName, string dialogueText)
    {
      _nodeId = nodeId;
      _speakerName = speakerName;
      _dialogueText = dialogueText;
      _selections = new List<DialogueSelectionData>();
      _customTypingSpeed = 0.05f;
    }

    /// <summary>
    /// JSON 로더용 전체 파라미터 생성자
    /// </summary>
    public DialogueNodeData(
        string identifier,
        string speakerName,
        string dialogueText,
        string nextDialogueIdentifier,
        float customTypingSpeed,
        List<DialogueSelectionData> selections)
    {
      _nodeId = identifier;
      _speakerName = speakerName;
      _dialogueText = dialogueText;
      _nextNodeId = nextDialogueIdentifier;
      _customTypingSpeed = customTypingSpeed;
      _selections = selections ?? new List<DialogueSelectionData>();
    }

    #endregion

    #region Methods

    public void AddSelection(DialogueSelectionData selection)
    {
      _selections ??= new List<DialogueSelectionData>();
      _selections.Add(selection);
    }

    public void ClearSelections()
    {
      _selections?.Clear();
    }

    public DialogueSelectionData GetSelection(int index)
    {
      if (_selections == null || index < 0 || index >= _selections.Count)
        return null;
      return _selections[index];
    }

    #endregion

    #region Builder Pattern

    public DialogueNodeData WithNodeId(string nodeId)
    {
      _nodeId = nodeId;
      return this;
    }

    public DialogueNodeData WithNextNode(string nextNodeId)
    {
      _nextNodeId = nextNodeId;
      return this;
    }

    public DialogueNodeData WithPortrait(Sprite portrait)
    {
      _portrait = portrait;
      return this;
    }

    public DialogueNodeData WithSelection(DialogueSelectionData selection)
    {
      AddSelection(selection);
      return this;
    }

    public DialogueNodeData WithTypingSpeed(float speed)
    {
      _customTypingSpeed = speed;
      return this;
    }

    #endregion
  }
}
