using System;
using UnityEngine;
using TriageTrainer.InteractableEntity;
using FishNet.Object;

namespace TriageTrainer.Dialogue
{
  /// <summary>
  /// 상호작용 시 대화를 시작하는 컴포넌트입니다.
  /// NPC 또는 오브젝트에 부착합니다.
  /// </summary>
  public class DialogueInteractable : NetworkBehaviour, IInteractable
  {
    #region Serialized Fields

    [Header("Display Settings")]
    [SerializeField] private string _displayText = "대화하기";
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private Color _displayColor = Color.white;

    [Header("Dialogue Settings")]
    [SerializeField] private TextAsset _dialogueJson;

    #endregion

    #region Private Fields

    private DialogueSession _cachedSession;

    #endregion

    #region IInteractable

    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public Color DisplayColor => _displayColor;

    #endregion

    #region Events

    public static event Action<DialogueSession> OnDialogueRequested;

    #endregion

    #region IInteractable.Interact

    public void Interact(Transform interactor)
    {
      var session = GetDialogueSession();

      if (session == null)
      {
        Debug.LogError($"[DialogueInteractable] {gameObject.name}: 유효한 대화 세션이 없습니다.");
        return;
      }

      Debug.Log($"[DialogueInteractable] {gameObject.name}: 대화 세션을 시작합니다.");

      OnDialogueRequested?.Invoke(session);
    }

    #endregion

    #region Private Methods

    private DialogueSession GetDialogueSession()
    {
      if (_cachedSession != null)
        return _cachedSession;

      if (_dialogueJson == null)
        return null;

      _cachedSession = DialogueJsonLoader.Load(_dialogueJson);
      return _cachedSession;
    }

    #endregion

    #region Public API

    public void SetDialogue(TextAsset dialogueJson)
    {
      _dialogueJson = dialogueJson;
      _cachedSession = null;
    }

    public void SetDialogue(DialogueSession session)
    {
      _cachedSession = session;
    }

    #endregion

#if UNITY_EDITOR
    private void Reset()
    {
      _displayText = "대화하기";
      _displayColor = Color.white;
    }

    private void OnValidate()
    {
      _cachedSession = null;
    }
#endif
  }
}
