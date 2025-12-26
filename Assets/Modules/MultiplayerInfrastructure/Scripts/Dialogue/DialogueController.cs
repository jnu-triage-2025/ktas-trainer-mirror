using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Camera;
using FishNet.Object;

namespace MultiplayerInfrastructure.Dialogue
{
  /// <summary>
  /// 대화 흐름을 제어합니다.
  /// UI 제어는 DialoguePanelUIController에 위임합니다.
  /// </summary>
  public class DialogueController : MonoBehaviour
  {
    #region Serialized Fields

    private static DialogueController _instance;
    public static DialogueController Instance => _instance;

    [Header("References")]
    [SerializeField] private DialoguePanelUIController _uiController;
    [SerializeField] private MainCameraController _camController;
    [SerializeField] private InteractableObjectHintUIController _hintUIController;

    #endregion

    #region Private Fields

    private DialogueSession _currentSession;
    private DialogueNodeData _currentNode;
    private List<DialogueSelectionData> _activeSelections = new List<DialogueSelectionData>();

    #endregion

    #region State

    public enum State
    {
      Inactive,
      Typing,
      WaitingForInput,
      WaitingForSelection
    }

    [SerializeField] private State _state = State.Inactive;

    #endregion

    #region Events

    public event Action OnDialogueStarted;
    public event Action OnDialogueEnded;
    public event Action<DialogueNodeData> OnNodeChanged;
    public event Action<DialogueSelectionData> OnSelectionMade;

    #endregion

    #region Properties

    public bool IsActive => _state != State.Inactive;
    public State CurrentState => _state;
    public DialogueNodeData CurrentNode => _currentNode;
    public DialogueSession CurrentSession => _currentSession;

    #endregion

    #region Unity Lifecycle

    void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(this.gameObject);
        return;
      }

      _instance = this;
    }

    public void RegisterReferences
    (
      DialoguePanelUIController uiController,
      MainCameraController camController,
      InteractableObjectHintUIController hintUIController
    )
    {
      _uiController = uiController;
      _camController = camController;
      _hintUIController = hintUIController;
    }

    private void OnEnable()
    {
      SubscribeExternalEvents();
      SubscribeUIEvents();
    }

    private void OnDisable()
    {
      UnsubscribeExternalEvents();
      UnsubscribeUIEvents();
      ClearSelectionCallbacks();
    }

    #endregion

    #region Event Subscriptions

    private void SubscribeExternalEvents()
    {
      DialogueInteractable.OnDialogueRequested += HandleDialogueRequested;
      DialogueTriggerZone.OnDialogueRequested += HandleDialogueRequested;
    }

    private void UnsubscribeExternalEvents()
    {
      DialogueInteractable.OnDialogueRequested -= HandleDialogueRequested;
      DialogueTriggerZone.OnDialogueRequested -= HandleDialogueRequested;
    }

    private void SubscribeUIEvents()
    {
      if (_uiController != null)
      {
        // UnityEvent는 AddListener 사용
        _uiController.OnTypingCompleted.AddListener(HandleTypingCompleted);
        _uiController.OnAdvanceRequested.AddListener(HandleAdvanceRequested);
      }
    }

    private void UnsubscribeUIEvents()
    {
      if (_uiController != null)
      {
        // UnityEvent는 RemoveListener 사용
        _uiController.OnTypingCompleted.RemoveListener(HandleTypingCompleted);
        _uiController.OnAdvanceRequested.RemoveListener(HandleAdvanceRequested);
      }
    }

    #endregion

    #region External Event Handlers

    private void HandleDialogueRequested(DialogueSession session)
    {
      if (session == null)
      {
        Debug.LogError("[DialogueController] Received null session");
        return;
      }

      StartDialogue(session);
    }

    #endregion

    #region Public API

    public void StartDialogue(DialogueSession session, string entryIdentifier = null)
    {
      if (session == null || session.Nodes.Count == 0)
      {
        Debug.LogWarning("[DialogueController] Invalid session");
        return;
      }

      if (_uiController == null)
      {
        Debug.LogWarning("[DialogueController] UI controller is missing; dialogue cannot display UI");
      }

      _currentSession = session;

      DialogueNodeData entryNode;
      if (!string.IsNullOrEmpty(entryIdentifier))
      {
        entryNode = session.GetNode(entryIdentifier);
        if (entryNode == null)
        {
          Debug.LogError($"[DialogueController] Entry node '{entryIdentifier}' not found");
          return;
        }
      }
      else
      {
        entryNode = session.GetFirstNode();
      }

      // Push dialogue UI as overlay: unlock cursor, disable movement, and allow overlay-aware input handling.
      if (_uiController != null && !UIOverlayStack.IsTop(_uiController))
      {
        UIOverlayStack.Push(_uiController);
      }

      // Make sure the dialogue panel is visible before displaying content.
      _uiController?.ShowPanel();
      EnterDialogueMode();
      ShowNode(entryNode);

      OnDialogueStarted?.Invoke();
    }

    public void EndDialogue()
    {
      ClearSelectionCallbacks();

      _currentSession = null;
      _currentNode = null;
      _state = State.Inactive;

      _uiController?.Hide();
      ExitDialogueMode();

      if (_uiController != null && UIOverlayStack.IsTop(_uiController))
      {
        UIOverlayStack.Pop();
      }

      OnDialogueEnded?.Invoke();
    }

    public void JumpToNode(string identifier)
    {
      if (_currentSession == null)
        return;

      var node = _currentSession.GetNode(identifier);
      if (node != null)
      {
        ClearSelectionCallbacks();
        ShowNode(node);
      }
    }

    public void SkipTyping()
    {
      if (_state == State.Typing)
      {
        _uiController?.CompleteTyping();
      }
    }

    /// <summary>
    /// 현재 선택된 선택지로 진행합니다. (PlayerController에서 호출)
    /// </summary>
    public void ConfirmSelection()
    {
      if (_state != State.WaitingForSelection)
        return;

      if (_hintUIController == null)
        return;

      var selected = _hintUIController.GetSelected();

      // DialogueSelectionInteractable에서 원본 데이터 추출
      if (selected is DialogueSelectionInteractable selectionInteractable)
      {
        HandleSelectionChosen(selectionInteractable.SelectionData);
      }
    }

    #endregion

    #region UI Event Handlers

    private void HandleTypingCompleted()
    {
      if (_currentNode == null)
      {
        EndDialogue();
        return;
      }

      if (_currentNode.HasSelections)
      {
        ShowSelections();
      }
      else
      {
        _state = State.WaitingForInput;
        _uiController?.SetWaitingForInput(true);
        ClearHintUISelections();
      }
    }

    private void HandleAdvanceRequested()
    {
      switch (_state)
      {
        case State.Typing:
          SkipTyping();
          break;

        case State.WaitingForInput:
          AdvanceToNext();
          break;

        case State.WaitingForSelection:
          // 선택지 대기 중 - ConfirmSelection()으로 처리
          break;
      }
    }

    #endregion

    #region Dialogue Flow

    private void ShowNode(DialogueNodeData node)
    {
      _currentNode = node;
      _state = State.Typing;

      _uiController?.ShowNode(node);
      ClearHintUISelections();

      OnNodeChanged?.Invoke(node);
    }

    private void AdvanceToNext()
    {
      if (_currentNode == null || _currentSession == null)
      {
        EndDialogue();
        return;
      }

      if (_currentNode.HasNextNode)
      {
        var nextNode = _currentSession.GetNextNode(_currentNode);
        if (nextNode != null)
        {
          ClearSelectionCallbacks();
          ShowNode(nextNode);
        }
        else
        {
          EndDialogue();
        }
      }
      else
      {
        EndDialogue();
      }
    }

    #endregion

    #region Selection Handling

    private void ShowSelections()
    {
      _state = State.WaitingForSelection;

      ClearSelectionCallbacks();
      _activeSelections.Clear();
      _activeSelections.AddRange(_currentNode.Selections);

      // 각 선택지에 콜백 설정
      foreach (var selection in _activeSelections)
      {
        // 클로저를 사용하여 해당 selection 캡처
        var capturedSelection = selection;
        selection.SetCallback(() => HandleSelectionChosen(capturedSelection));
      }

      ShowSelectionsOnHintUI();
    }

    private void HandleSelectionChosen(DialogueSelectionData selection)
    {
      if (_state != State.WaitingForSelection)
        return;

      OnSelectionMade?.Invoke(selection);

      ClearSelectionCallbacks();
      ClearHintUISelections();

      if (selection.HasNextDialogue && _currentSession != null)
      {
        var nextNode = _currentSession.GetNextNode(selection);
        if (nextNode != null)
        {
          ShowNode(nextNode);
        }
        else
        {
          EndDialogue();
        }
      }
      else
      {
        EndDialogue();
      }
    }

    private void ClearSelectionCallbacks()
    {
      foreach (var selection in _activeSelections)
      {
        selection.ClearCallback();
      }
      _activeSelections.Clear();
    }

    #endregion

    #region HintUI Integration

    private void EnterDialogueMode()
    {
      _hintUIController?.EnterDialogueMode();
    }

    private void ExitDialogueMode()
    {
      _hintUIController?.ExitDialogueMode();
    }

    private void ShowSelectionsOnHintUI()
    {
      if (_hintUIController == null)
        return;

      // DialogueSelectionData를 DialogueSelectionInteractable로 래핑
      var interactables = new List<IInteractable>();
      for (int i = 0; i < _activeSelections.Count; i++)
      {
        var selection = _activeSelections[i];
        var interactable = new DialogueSelectionInteractable(
            selection,
            i,
            (data, index) => HandleSelectionChosen(data)
        );
        interactables.Add(interactable);
      }

      _hintUIController.SetDialogueSelections(interactables);
    }

    private void ClearHintUISelections()
    {
      _hintUIController?.ClearDialogueSelections();
    }

    #endregion
  }
}
