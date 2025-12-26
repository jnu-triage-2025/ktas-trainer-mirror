using System;
using System.Collections.Generic;
using TriageTrainer.InteractableEntity;
using TriageTrainer.Dialogue;
using TriageTrainer.UIDocuments;
using TriageTrainer.Registry;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace TriageTrainer.UI
{
  /// <summary>
  /// 대화 UI 패널을 제어하는 컨트롤러.
  /// InteractableObjectHintUIController와 연동하여 대화 선택지를 표시합니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class DialoguePanelUIController : UIControllerABC, IUIOverlay
  {
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private UIDocument _uiDocument;

    [Header("Settings")]
    [SerializeField] private float _typingSpeed = 0.05f;
    [SerializeField] private bool _autoShowOnDialogueStart = true;

    #endregion

    #region Private Fields

    private VisualElement _root;
    private VisualElement _dialoguePanel;
    private DialogueElement _dialogueElement;
    private Label _speakerNameLabel;
    private Label _dialogueTextLabel;
    private VisualElement _portraitImage;
    private VisualElement _waitingIndicator;

    [SerializeField] private InteractableObjectHintUIController _interactableHintUI;
    [SerializeField] private DialogueRunner _currentRunner;
    [SerializeField] private DialogueNodeData _currentNode;

    [SerializeField] private bool _isTyping;
    [SerializeField] private bool _isWaitingForInput;
    [SerializeField] private string _fullText;
    [SerializeField] private int _currentCharIndex;
    [SerializeField] private float _lastTypeTime;

    // 현재 선택지들을 IInteractable로 래핑
    private List<DialogueSelectionInteractable> _currentSelections = new();

    #endregion

    #region Events

    public UnityEvent OnDialogueStarted = new();
    public UnityEvent OnDialogueEnded = new();
    public UnityEvent<DialogueNodeData> OnNodeDisplayed = new();
    public UnityEvent<int> OnSelectionMade = new();

    /// <summary>
    /// 타이핑이 완료되었을 때 발생
    /// </summary>
    public UnityEvent OnTypingCompleted = new();

    /// <summary>
    /// 다음 진행이 요청되었을 때 발생 (선택지 없이 진행할 때)
    /// </summary>
    public UnityEvent OnAdvanceRequested = new();

    public event Action OverlayPushed;
    public event Action OverlayPopped;

    #endregion

    #region Properties

    /// <summary>
    /// 대화가 진행 중인지 여부
    /// </summary>
    public bool IsDialogueActive => _currentRunner != null;

    /// <summary>
    /// 현재 텍스트 타이핑 중인지 여부
    /// </summary>
    public bool IsTyping => _isTyping;

    /// <summary>
    /// 선택지가 표시되어 있는지 여부
    /// </summary>
    public bool HasActiveSelections => _currentSelections != null && _currentSelections.Count > 0;

    /// <summary>
    /// 입력 대기 중인지 여부
    /// </summary>
    public bool IsWaitingForInput => _isWaitingForInput;

    #endregion

    #region Unity Lifecycle

    protected virtual void Awake()
    {
      base.Awake();

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      CacheVisualElements();
    }

    private void OnEnable()
    {
      CacheVisualElements();
    }

    private void Update()
    {
      if (_isTyping)
      {
        UpdateTyping();
      }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// InteractableHintUI 컨트롤러를 설정합니다.
    /// PlayerController에서 호출됩니다.
    /// </summary>
    public void SetInteractableHintUI(InteractableObjectHintUIController hintUI)
    {
      _interactableHintUI = hintUI;

      if (_interactableHintUI != null)
      {
        Debug.Log("[DialoguePanelUI] InteractableHintUI connected");
      }
    }

    private void CacheVisualElements()
    {
      if (_uiDocument == null) return;

      _root = _uiDocument.rootVisualElement;
      if (_root == null) return;

      // UXML uses a custom DialogueElement named "dialogue-element"; keep a VisualElement fallback.
      _dialogueElement = _root.Q<DialogueElement>("dialogue-element");
      _dialoguePanel = _dialogueElement != null ? _dialogueElement : _root.Q<VisualElement>("dialogue-panel");
      _speakerNameLabel = _root.Q<Label>("speaker-name");
      _dialogueTextLabel = _root.Q<Label>("dialogue-text");
      _portraitImage = _root.Q<VisualElement>("portrait-image");
      _waitingIndicator = _root.Q<VisualElement>("waiting-indicator");

      // 초기 상태: 패널 숨김
      if (_dialoguePanel != null)
      {
        _dialoguePanel.style.display = DisplayStyle.None;
      }

      if (_dialogueElement != null)
      {
        _dialogueElement.Hide();
        _dialogueElement.OnDialogueClicked += HandleDialogueClicked;
      }
    }

    #endregion

    #region Dialogue Control

    /// <summary>
    /// 대화를 시작합니다.
    /// </summary>
    public void StartDialogue(DialogueRunner runner)
    {
      if (runner == null)
      {
        Debug.LogError("[DialoguePanelUI] Cannot start dialogue with null runner");
        return;
      }

      _currentRunner = runner;

      // InteractableHintUI를 대화 모드로 전환
      if (_interactableHintUI != null)
      {
        _interactableHintUI.EnterDialogueMode();
      }

      // 패널 표시
      if (_autoShowOnDialogueStart)
      {
        ShowPanel();
      }

      // Treat dialogue UI as an overlay so player input/camera lock is paused and cursor is free.
      if (!UIOverlayStack.IsTop(this))
        UIOverlayStack.Push(this);

      OnDialogueStarted?.Invoke();
      Debug.Log("[DialoguePanelUI] Dialogue started");
    }

    /// <summary>
    /// 대화를 종료합니다.
    /// </summary>
    public void EndDialogue()
    {
      _currentRunner = null;
      _currentNode = null;
      _isTyping = false;
      _isWaitingForInput = false;

      // 선택지 정리
      ClearSelections();

      // InteractableHintUI를 일반 모드로 복원
      if (_interactableHintUI != null)
      {
        _interactableHintUI.ExitDialogueMode();
      }

      // 패널 숨김
      HidePanel();

      OnDialogueEnded?.Invoke();
      Debug.Log("[DialoguePanelUI] Dialogue ended");

      // Remove overlay when dialogue ends.
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
    }

    /// <summary>
    /// 대화 노드를 표시합니다.
    /// </summary>
    public void DisplayNode(DialogueNodeData node)
    {
      if (node == null) return;

      _currentNode = node;
      _isWaitingForInput = false;

      // 화자 이름 설정
      if (_speakerNameLabel != null)
      {
        _speakerNameLabel.text = node.SpeakerName ?? "";
      }

      // 초상화 설정
      if (_portraitImage != null && node.Portrait != null)
      {
        _portraitImage.style.backgroundImage = new StyleBackground(node.Portrait);
        _portraitImage.style.display = DisplayStyle.Flex;
      }
      else if (_portraitImage != null)
      {
        _portraitImage.style.display = DisplayStyle.None;
      }

      // 대화 텍스트 타이핑 시작
      StartTyping(node.DialogueText ?? "");

      // 선택지는 타이핑 완료 후 표시
      OnNodeDisplayed?.Invoke(node);
    }

    /// <summary>
    /// 대화 노드를 표시합니다. (DialogueController 호환용 별칭)
    /// </summary>
    public void ShowNode(DialogueNodeData node)
    {
      DisplayNode(node);
    }

    /// <summary>
    /// 선택지를 표시합니다.
    /// </summary>
    public void DisplaySelections(List<DialogueSelectionData> selections)
    {
      ClearSelections();

      if (selections == null || selections.Count == 0)
      {
        Debug.Log("[DialoguePanelUI] No selections to display");
        return;
      }

      // 선택지를 IInteractable로 래핑
      for (int i = 0; i < selections.Count; i++)
      {
        var selectionData = selections[i];
        var interactable = new DialogueSelectionInteractable(
            selectionData,
            i,
            OnSelectionInteracted
        );
        _currentSelections.Add(interactable);
      }

      // InteractableHintUI에 선택지 설정
      if (_interactableHintUI != null && _interactableHintUI.IsDialogueMode)
      {
        Debug.Log($"[DialoguePanelUI] Setting {_currentSelections.Count} dialogue selections in InteractableHintUI");
        var interactables = new List<IInteractable>(_currentSelections);
        _interactableHintUI.SetDialogueSelections(interactables);
      }

      Debug.Log($"[DialoguePanelUI] Displayed {selections.Count} selections");
    }

    /// <summary>
    /// 현재 선택된 옵션을 선택합니다.
    /// PlayerController에서 호출됩니다.
    /// </summary>
    public void TrySelectCurrentOption()
    {
      // 타이핑 중이면 스킵
      if (_isTyping)
      {
        SkipTyping();
        return;
      }

      // 선택지가 있으면 현재 선택된 것을 실행
      if (_interactableHintUI != null && _interactableHintUI.HasDialogueSelection())
      {
        _interactableHintUI.ExecuteSelectedDialogueSelection(null);
        return;
      }

      // 선택지가 없으면 다음 진행 요청
      if (!HasActiveSelections)
      {
        OnAdvanceRequested?.Invoke();

        if (_currentRunner != null)
        {
          _currentRunner.Advance();
        }
      }
    }

    /// <summary>
    /// 특정 인덱스의 선택지를 선택합니다.
    /// </summary>
    public void SelectOption(int index)
    {
      if (index < 0 || index >= _currentSelections.Count)
      {
        Debug.LogWarning($"[DialoguePanelUI] Invalid selection index: {index}");
        return;
      }

      var selection = _currentSelections[index];
      OnSelectionInteracted(selection.SelectionData, index);
    }

    #endregion

    #region Selection Handling

    private void OnSelectionInteracted(DialogueSelectionData selectionData, int index)
    {
      Debug.Log($"[DialoguePanelUI] Selection made: {index} - {selectionData.SelectionText}");

      // 선택지 콜백 실행
      selectionData.InvokeCallback();

      // 선택 이벤트 발생
      OnSelectionMade?.Invoke(index);

      // 선택지 정리
      ClearSelections();

      // DialogueRunner에 선택 전달
      if (_currentRunner != null)
      {
        _currentRunner.SelectOption(index);
      }
    }

    private void ClearSelections()
    {
      _currentSelections.Clear();

      if (_interactableHintUI != null && _interactableHintUI.IsDialogueMode)
      {
        _interactableHintUI.ClearDialogueSelections();
      }
    }

    #endregion

    #region Typing Effect

    private void StartTyping(string text)
    {
      _fullText = text;
      _currentCharIndex = 0;
      _isTyping = true;
      _isWaitingForInput = false;
      _lastTypeTime = Time.time;

      if (_dialogueTextLabel != null)
      {
        _dialogueTextLabel.text = "";
      }

      SetWaitingIndicatorVisible(false);
    }

    private void UpdateTyping()
    {
      if (!_isTyping) return;

      if (Time.time - _lastTypeTime >= _typingSpeed)
      {
        _lastTypeTime = Time.time;
        _currentCharIndex++;

        if (_dialogueTextLabel != null)
        {
          _dialogueTextLabel.text = _fullText.Substring(0, Mathf.Min(_currentCharIndex, _fullText.Length));
        }

        if (_currentCharIndex >= _fullText.Length)
        {
          FinishTyping();
        }
      }
    }

    private void FinishTyping()
    {
      _isTyping = false;

      if (_dialogueTextLabel != null)
      {
        _dialogueTextLabel.text = _fullText;
      }

      // 타이핑 완료 이벤트
      OnTypingCompleted?.Invoke();

      // 타이핑 완료 후 선택지 표시
      if (_currentNode != null && _currentNode.Selections != null && _currentNode.Selections.Count > 0)
      {
        DisplaySelections(_currentNode.Selections);
      }
      else
      {
        // 선택지가 없으면 입력 대기 상태로
        SetWaitingForInput(true);
      }
    }

    /// <summary>
    /// 타이핑을 스킵하고 전체 텍스트를 즉시 표시합니다.
    /// </summary>
    public void SkipTyping()
    {
      if (!_isTyping) return;
      FinishTyping();
    }

    /// <summary>
    /// 타이핑을 완료합니다. (SkipTyping 별칭)
    /// </summary>
    public void CompleteTyping()
    {
      SkipTyping();
    }

    #endregion

    #region Waiting State

    /// <summary>
    /// 입력 대기 상태 설정
    /// </summary>
    public void SetWaitingForInput(bool waiting)
    {
      _isWaitingForInput = waiting;
      SetWaitingIndicatorVisible(waiting);
    }

    private void SetWaitingIndicatorVisible(bool visible)
    {
      if (_waitingIndicator != null)
      {
        _waitingIndicator.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
      }
    }

    #endregion

    #region Panel Visibility

    /// <summary>
    /// 패널 표시
    /// </summary>
    public void ShowPanel()
    {
      Show();
    }

    /// <summary>
    /// 패널 표시 (별칭)
    /// </summary>
    public void Show()
    {
      if (_dialoguePanel != null)
      {
        _dialoguePanel.style.display = DisplayStyle.Flex;
      }
      else
      {
        Debug.LogWarning("[DialoguePanelUI] Show() called but _dialoguePanel is null (check UXML id 'dialogue-panel' or 'dialogue-element')");
      }

      _dialogueElement?.Show();
    }

    /// <summary>
    /// 패널 숨김
    /// </summary>
    public void HidePanel()
    {
      Hide();
    }

    /// <summary>
    /// 패널 숨김 (별칭)
    /// </summary>
    public void Hide()
    {
      if (_dialoguePanel != null)
      {
        _dialoguePanel.style.display = DisplayStyle.None;
      }

      _dialogueElement?.Hide();
    }

    // IUIOverlay

    public void OnOverlayPushed()
    {
      var player = CurrentSessionPlayInfoRegistry.Get<Player.PlayerController>();
      Debug.Log($"[DialoguePanelUI] OnOverlayPushed: PlayerController found: {player != null}");
      player?.EnterUIOverlayMode();
      Debug.Log("[DialoguePanelUI] OnOverlayPushed: Entered UI overlay mode for player");
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      var player = CurrentSessionPlayInfoRegistry.Get<Player.PlayerController>();
      player?.ExitUIOverlayMode();
      OverlayPopped?.Invoke();
    }

    #endregion

    #region DialogueElement Interaction

    private void HandleDialogueClicked()
    {
      // Clicking on the dialogue area should behave like advancing/confirming.
      TrySelectCurrentOption();
    }

    /// <summary>
    /// 패널 토글
    /// </summary>
    public void TogglePanel()
    {
      if (_dialoguePanel == null) return;

      if (_dialoguePanel.style.display == DisplayStyle.None)
        Show();
      else
        Hide();
    }

    #endregion
  }

  #region DialogueSelectionInteractable

  /// <summary>
  /// 대화 선택지를 IInteractable로 래핑하는 클래스.
  /// InteractableObjectHintUIController에서 표시할 수 있도록 합니다.
  /// </summary>
  public class DialogueSelectionInteractable : IInteractable
  {
    private readonly DialogueSelectionData _selectionData;
    private readonly int _index;
    private readonly Action<DialogueSelectionData, int> _onInteract;

    public DialogueSelectionInteractable(
        DialogueSelectionData selectionData,
        int index,
        Action<DialogueSelectionData, int> onInteract)
    {
      _selectionData = selectionData;
      _index = index;
      _onInteract = onInteract;
    }

    public DialogueSelectionData SelectionData => _selectionData;
    public int Index => _index;

    // IInteractable 구현
    public string DisplayText => _selectionData?.SelectionText ?? $"선택지 {_index + 1}";
    public Sprite DisplayIcon => _selectionData?.Icon;
    public Color DisplayColor => _selectionData?.DisplayColor ?? Color.white;

    public void Interact(Transform interactor)
    {
      _onInteract?.Invoke(_selectionData, _index);
    }

    // IInteractable의 다른 필수 멤버들
    public bool CanInteract(Transform interactor) => true;
    public float InteractionDistance => float.MaxValue;
    public Transform Transform => null;
  }

  #endregion
}
