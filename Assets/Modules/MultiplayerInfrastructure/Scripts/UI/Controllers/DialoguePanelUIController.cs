using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Scenario;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace MultiplayerInfrastructure.UI
{
  /// <summary>
  /// 시나리오 UI 패널을 제어하는 컨트롤러.
  /// InteractableObjectHintUIController와 연동하여 시나리오 선택지를 표시합니다.
  /// </summary>
  [RequireComponent(typeof(UIDocument))]
  public class DialoguePanelUIController : UIControllerABC, IUIOverlay
  {
    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private UIDocument _uiDocument;

    [Header("Settings")]
    [SerializeField] private float _typingSpeed = 0.05f;
    [SerializeField] private bool _autoShowOnScenarioStart = true;

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
    [SerializeField] private ScenarioController _currentController;
    [SerializeField] private IScenarioNode _currentNode;

    [SerializeField] private bool _isTyping;
    [SerializeField] private bool _isWaitingForInput;
    [SerializeField] private string _fullText;
    [SerializeField] private int _currentCharIndex;
    [SerializeField] private float _lastTypeTime;

    [SerializeField] private DialogueInputContext _inputContext = DialogueInputContext.None;
    private IReadOnlyList<ScenarioChoiceOption> _pendingChoiceOptions;

    // 현재 선택지들을 IInteract로 래핑
    private List<ScenarioSelectionInteractable> _currentSelections = new();

    private enum DialogueInputContext
    {
      None,
      Dialogue,
      Choice,
    }

    #endregion

    #region Events

    public UnityEvent OnScenarioStarted = new();
    public UnityEvent OnScenarioEnded = new();
    public UnityEvent<IScenarioNode> OnNodeDisplayed = new();
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
    /// 시나리오가 진행 중인지 여부
    /// </summary>
    public bool IsScenarioActive => _currentController != null;

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

    protected override void Awake()
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

    private void OnDisable()
    {
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
    /// ScenarioController에서 호출됩니다.
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
      }
    }

    #endregion

    #region Scenario Control

    /// <summary>
    /// 시나리오 시작
    /// </summary>
    public void StartScenario(ScenarioController controller)
    {
      if (controller.IsUnityNull())
      {
        Debug.LogError("[DialoguePanelUI] Cannot start scenario with null controller");
        return;
      }

      _currentController = controller;

      // InteractableHintUI를 시나리오 모드로 전환
      if (!_interactableHintUI.IsUnityNull())
      {
        _interactableHintUI.EnterDialogueMode();
      }

      // 패널 표시
      if (_autoShowOnScenarioStart)
      {
        ShowPanel();
      }

      // Treat scenario UI as an overlay so player input/camera lock is paused and cursor is free.
      if (!UIOverlayStack.IsTop(this))
        UIOverlayStack.Push(this);

      OnScenarioStarted?.Invoke();
      Debug.Log("[DialoguePanelUI] Scenario started");
    }

    /// <summary>
    /// 시나리오 종료
    /// </summary>
    public void EndScenario()
    {
      _currentController = null;
      _currentNode = null;
      _isTyping = false;
      _isWaitingForInput = false;
      _inputContext = DialogueInputContext.None;
      _pendingChoiceOptions = null;

      // 선택지 정리
      ClearSelections();

      // InteractableHintUI를 일반 모드로 복원
      if (!_interactableHintUI.IsUnityNull())
      {
        _interactableHintUI.ExitDialogueMode();
      }

      // 패널 숨김
      HidePanel();

      OnScenarioEnded?.Invoke();
      Debug.Log("[DialoguePanelUI] Scenario ended");

      // Remove overlay when scenario ends.
      if (UIOverlayStack.IsTop(this))
        UIOverlayStack.Pop();
    }

    /// <summary>
    /// 대화 노드 표시
    /// </summary>
    public void DisplayDialogue(string speakerName, string dialogueContent, string portraitIdentifier)
    {
      _inputContext = DialogueInputContext.Dialogue;
      _pendingChoiceOptions = null;
      ClearSelections();
      PresentTextNode(speakerName, dialogueContent, portraitIdentifier);

      OnNodeDisplayed?.Invoke(null); // TODO: pass node if needed
    }

    /// <summary>
    /// 선택지 표시
    /// </summary>
    public void DisplayChoice(string speakerName, string dialogueContent, string portraitIdentifier, IReadOnlyList<ScenarioChoiceOption> options)
    {
      _inputContext = DialogueInputContext.Choice;
      _pendingChoiceOptions = options;
      ClearSelections();
      PresentTextNode(speakerName, dialogueContent, portraitIdentifier);

      OnNodeDisplayed?.Invoke(null); // TODO: pass node if needed
    }

    private void ShowChoices(IReadOnlyList<ScenarioChoiceOption> options)
    {
      ClearSelections();

      if (options == null || options.Count == 0)
      {
        Debug.Log("[DialoguePanelUI] No options to display");
        SetWaitingForInput(true);
        return;
      }

      // 선택지를 IInteract로 래핑
      for (int i = 0; i < options.Count; i++)
      {
        var option = options[i];
        var interactable = new ScenarioSelectionInteractable(
            option,
            i,
            OnSelectionInteracted
        );
        _currentSelections.Add(interactable);
      }

      // InteractableHintUI에 선택지 설정
      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
      {
        Debug.Log($"[DialoguePanelUI] Setting {_currentSelections.Count} scenario selections in InteractableHintUI");
        var interacts = new List<IInteract>(_currentSelections);
        _interactableHintUI.SetDialogueSelections(interacts);
      }

      SetWaitingForInput(false);

      Debug.Log($"[DialoguePanelUI] Displayed {options.Count} options");
    }

    /// <summary>
    /// 현재 선택된 옵션을 선택합니다.
    /// ScenarioController에서 호출됩니다.
    /// </summary>
    public void TrySelectCurrentOption()
    {
      // 타이핑 중이면 스킵
      if (_isTyping)
      {
        SkipTyping();
        return;
      }

      // Choice: 재생 완료 후 입력은 현재 선택지를 확정.
      if (_inputContext == DialogueInputContext.Choice)
      {
        if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.HasDialogueSelection())
        {
          _interactableHintUI.ExecuteSelectedDialogueSelection(null);
          return;
        }

        if (!HasActiveSelections && _pendingChoiceOptions != null)
        {
          ShowChoices(_pendingChoiceOptions);
        }
        return;
      }

      // Dialogue: 재생 완료 후 입력은 다음 노드 진행.
      if (_inputContext == DialogueInputContext.Dialogue)
      {
        OnAdvanceRequested?.Invoke();

        if (!_currentController.IsUnityNull())
        {
          _currentController.Advance();
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
      OnSelectionInteracted(selection.Option, index);
    }

    #endregion

    #region Selection Handling

    private void OnSelectionInteracted(ScenarioChoiceOption option, int index)
    {
      Debug.Log($"[DialoguePanelUI] Option selected: {index} - {option.DisplayText}");

      // 선택 이벤트 발생
      OnSelectionMade?.Invoke(index);

      // 선택지 정리
      ClearSelections();
      _pendingChoiceOptions = null;
      _inputContext = DialogueInputContext.None;

      // ScenarioController에 선택 전달
      if (!_currentController.IsUnityNull())
      {
        _currentController.SelectOption(index);
      }
    }

    private void ClearSelections()
    {
      _currentSelections.Clear();

      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
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

      if (_inputContext == DialogueInputContext.Choice)
      {
        ShowChoices(_pendingChoiceOptions);
        return;
      }

      if (!HasActiveSelections)
      {
        SetWaitingForInput(true);
      }
    }

    private void PresentTextNode(string speakerName, string dialogueContent, string portraitIdentifier)
    {
      _isWaitingForInput = false;

      if (_speakerNameLabel != null)
      {
        _speakerNameLabel.text = speakerName ?? "";
      }

      if (_portraitImage != null && !string.IsNullOrEmpty(portraitIdentifier))
      {
        var portrait = Resources.Load<Sprite>(portraitIdentifier);
        if (portrait != null)
        {
          _portraitImage.style.backgroundImage = new StyleBackground(portrait);
          _portraitImage.style.display = DisplayStyle.Flex;
        }
        else
        {
          _portraitImage.style.display = DisplayStyle.None;
        }
      }
      else if (_portraitImage != null)
      {
        _portraitImage.style.display = DisplayStyle.None;
      }

      StartTyping(dialogueContent ?? "");
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
      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      OverlayPopped?.Invoke();
    }

    #endregion

    #region DialogueElement Interaction

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

  #region ScenarioSelectionInteractable

  /// <summary>
  /// 시나리오 선택지를 IInteract로 래핑하는 클래스.
  /// InteractableObjectHintUIController에서 표시할 수 있도록 합니다.
  /// </summary>
  public class ScenarioSelectionInteractable : IInteract
  {
    private readonly ScenarioChoiceOption _option;
    private readonly int _index;
    private readonly Action<ScenarioChoiceOption, int> _onInteract;

    public ScenarioSelectionInteractable(
        ScenarioChoiceOption option,
        int index,
        Action<ScenarioChoiceOption, int> onInteract)
    {
      _option = option;
      _index = index;
      _onInteract = onInteract;
    }

    public ScenarioChoiceOption Option => _option;
    public int Index => _index;

    // IInteract 구현
    public string DisplayText => _option?.DisplayText ?? $"선택지 {_index + 1}";
    public Sprite DisplayIcon
    {
      get
      {
        if (_option != null && !string.IsNullOrEmpty(_option.DisplayIconIdentifier))
        {
          return Resources.Load<Sprite>(_option.DisplayIconIdentifier);
        }
        return null;
      }
    }
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => _option?.DisplayColor ?? Color.white;

    public void Interact(Transform interactor)
    {
      _onInteract?.Invoke(_option, _index);
    }
  }

  #endregion
}
