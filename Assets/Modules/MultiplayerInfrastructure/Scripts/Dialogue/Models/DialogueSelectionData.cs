using System;
using UnityEngine;

namespace MultiplayerInfrastructure.Dialogue
{
  /// <summary>
  /// 대화 선택지 데이터
  /// </summary>
  [Serializable]
  public class DialogueSelectionData
  {
    [SerializeField] private string _identifier;
    [SerializeField] private string _selectionText;
    [SerializeField] private Sprite _icon;
    [SerializeField] private Color _displayColor = Color.white;
    [SerializeField] private string _targetNodeId;
    [SerializeField] private string _nextDialogueIdentifier;

    // 런타임 콜백 (직렬화되지 않음)
    [NonSerialized] private Action _callback;

    #region Properties

    /// <summary>
    /// 선택지 고유 식별자
    /// </summary>
    public string Identifier
    {
      get => _identifier;
      set => _identifier = value;
    }

    public string SelectionText
    {
      get => _selectionText;
      set => _selectionText = value;
    }

    /// <summary>
    /// SelectionText의 별칭 (하위 호환성)
    /// </summary>
    public string DisplayText
    {
      get => _selectionText;
      set => _selectionText = value;
    }

    public Sprite Icon
    {
      get => _icon;
      set => _icon = value;
    }

    public Color DisplayColor
    {
      get => _displayColor;
      set => _displayColor = value;
    }

    public string TargetNodeId
    {
      get => _targetNodeId;
      set => _targetNodeId = value;
    }

    /// <summary>
    /// 다음 대화 식별자 (다른 대화 파일로 이동할 때 사용)
    /// </summary>
    public string NextDialogueIdentifier
    {
      get => _nextDialogueIdentifier;
      set => _nextDialogueIdentifier = value;
    }

    /// <summary>
    /// 다음 대화가 설정되어 있는지 여부
    /// </summary>
    public bool HasNextDialogue => !string.IsNullOrEmpty(_nextDialogueIdentifier);

    #endregion

    #region Constructors

    public DialogueSelectionData()
    {
      _displayColor = Color.white;
    }

    public DialogueSelectionData(string text, string targetNodeId = null)
    {
      _selectionText = text;
      _targetNodeId = targetNodeId;
      _displayColor = Color.white;
    }

    public DialogueSelectionData(string text, string targetNodeId, string nextDialogueIdentifier)
    {
      _selectionText = text;
      _targetNodeId = targetNodeId;
      _nextDialogueIdentifier = nextDialogueIdentifier;
      _displayColor = Color.white;
    }

    /// <summary>
    /// JSON 로더용 전체 파라미터 생성자
    /// </summary>
    public DialogueSelectionData(string identifier, string displayText, string nextDialogueIdentifier, bool _)
    {
      _identifier = identifier;
      _selectionText = displayText;
      _nextDialogueIdentifier = nextDialogueIdentifier;
      _displayColor = Color.white;
    }

    public DialogueSelectionData(string text, Sprite icon, Color displayColor, string targetNodeId = null)
    {
      _selectionText = text;
      _icon = icon;
      _displayColor = displayColor;
      _targetNodeId = targetNodeId;
    }

    #endregion

    #region Callback Methods

    /// <summary>
    /// 선택 시 실행할 콜백 설정
    /// </summary>
    public void SetCallback(Action callback)
    {
      _callback = callback;
    }

    /// <summary>
    /// 콜백 제거
    /// </summary>
    public void ClearCallback()
    {
      _callback = null;
    }

    /// <summary>
    /// 콜백 실행
    /// </summary>
    public void InvokeCallback()
    {
      _callback?.Invoke();
    }

    /// <summary>
    /// 콜백이 설정되어 있는지 여부
    /// </summary>
    public bool HasCallback => _callback != null;

    #endregion

    #region Builder Methods

    public DialogueSelectionData WithIdentifier(string identifier)
    {
      _identifier = identifier;
      return this;
    }

    public DialogueSelectionData WithIcon(Sprite icon)
    {
      _icon = icon;
      return this;
    }

    public DialogueSelectionData WithColor(Color color)
    {
      _displayColor = color;
      return this;
    }

    public DialogueSelectionData WithNextDialogue(string nextDialogueIdentifier)
    {
      _nextDialogueIdentifier = nextDialogueIdentifier;
      return this;
    }

    public DialogueSelectionData WithTargetNode(string targetNodeId)
    {
      _targetNodeId = targetNodeId;
      return this;
    }

    #endregion
  }
}
