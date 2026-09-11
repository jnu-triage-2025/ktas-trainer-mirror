using System;
using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
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
  public partial class DialoguePanelUIController : UIControllerABC, IUIOverlay
  {
    /// <summary>
    /// 시나리오 대화의 speakerName / dialogueContent에서 플레이어 이름으로 치환되는
    /// 이스케이프 워드입니다.
    ///
    /// JSON 시나리오 작성 예:
    /// <code>
    /// { "speakerName": "{PLAYER_NAME}", "dialogueContent": "안녕하세요, {PLAYER_NAME}씨!" }
    /// </code>
    /// </summary>
    public const string PlayerNamePlaceholder = "{PLAYER_NAME}";

    #region Serialized Fields

    [Header("UI References")]
    [SerializeField] private UIDocument _uiDocument;

    [Header("Settings")]
    [SerializeField] private float _typingSpeed = 0.05f;
    [SerializeField] private bool _autoShowOnScenarioStart = false;

    #endregion

    #region Private Fields

    private VisualElement _root;
    private VisualElement _dialoguePanel;
    private DialogueElement _dialogueElement;
    private DialogueElement _dialogueElementWithClickHandler;
    private Label _speakerNameLabel;
    private Label _dialogueTextLabel;
    private VisualElement _portraitImage;
    private VisualElement _waitingIndicator;

    [SerializeField] private InteractableObjectHintUIController _interactableHintUI;
    [SerializeField] private ScenarioController _currentController;
    [SerializeField] private IScenarioNode _currentNode;

    // 대화창 계열 UI(Dialogue/Choice/Quiz)를 현재 점유 중인 시나리오 흐름(그래프) 식별자.
    // 두 개 이상의 흐름이 동시에 대화창을 점유하려는 충돌을 감지하는 데 사용한다.
    private string _owningGraphIdentifier;

    [SerializeField] private bool _isTyping;
    [SerializeField] private bool _isWaitingForInput;
    [SerializeField] private string _fullText;
    [SerializeField] private int _currentCharIndex;
    [SerializeField] private float _lastTypeTime;
    [SerializeField] private bool _currentDialogueInteractionRequired;

    // 재생 중 마지막으로 글자가 진행된 unscaled 시각. 건너뛰기 설정이 꺼져 있을 때
    // 재생이 멈춰 버린 상황(타임스케일 정지 등)을 감지해 입력 잠금이 영구화되지 않도록 한다.
    private float _lastTypeProgressUnscaledTime;

    /// <summary>
    /// 건너뛰기 설정이 꺼져 있어도, 이 시간(초, unscaled) 동안 글자가 하나도 진행되지 않으면
    /// 재생이 멈춘 것으로 보고 진행 입력으로 재생을 끝낼 수 있게 허용한다.
    /// 타이핑 간격이 이보다 길게 설정된 경우에는 간격의 배수로 늘려 잡는다.
    /// </summary>
    public const float TypingStallGraceSeconds = 1.5f;

    // 대화창이 진행/선택 입력을 소비한 마지막 프레임 번호.
    // 선택 확정 직후 오버레이가 pop 되어도, 같은 프레임의 F/스페이스/엔터/좌클릭이
    // 주변 Interactable 상호작용이나 아이템 사용으로 흘러가지 않도록 입력 경로에서 확인한다.
    private int _lastInputConsumedFrame = -1;

    /// <summary>힌트 UI를 끝내 확보하지 못했다는 사실을 이미 보고했는지 여부(로그 반복 방지).</summary>
    private bool _reportedMissingHintUI;

    [SerializeField] private DialogueInputContext _inputContext = DialogueInputContext.None;
    private IReadOnlyList<ScenarioChoiceOption> _pendingChoiceOptions;
    private readonly Queue<TransientDialogueRequest> _transientDialogueQueue = new();
    private Coroutine _transientDialogueRoutine;

    // 현재 선택지들을 IInteract로 래핑
    private List<ScenarioSelectionInteractable> _currentSelections = new();

    private enum DialogueInputContext
    {
      None,
      Dialogue,
      Choice,
    }

    private readonly struct TransientDialogueRequest
    {
      public readonly string SpeakerName;
      public readonly string Content;
      public readonly string PortraitIdentifier;
      public readonly float Duration;
      public readonly Action OnFinished;

      public TransientDialogueRequest(string speakerName, string content, string portraitIdentifier, float duration, Action onFinished)
      {
        SpeakerName = speakerName;
        Content = content;
        PortraitIdentifier = portraitIdentifier;
        Duration = duration;
        OnFinished = onFinished;
      }
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

    /// <summary>
    /// 이번 프레임에 대화 진행/선택 입력을 이미 소비했는지 여부.
    /// 선택지를 확정하면 대화창이 곧바로 오버레이 스택에서 빠지기 때문에,
    /// 같은 프레임의 입력이 월드 상호작용으로 이어지는 것을 이 값으로 차단한다.
    /// </summary>
    public bool HasConsumedInputThisFrame => _lastInputConsumedFrame == Time.frameCount;

    /// <summary>
    /// 대화창이 지금 실제로 대화/선택지를 표시하며 입력을 기다리는 중인지 여부.
    /// 오버레이 스택에는 올라가 있는데 이 값이 false 이면 표시할 것이 없는 잔여 항목이므로,
    /// 입력 경로와 시나리오 정리 경로가 그 항목을 스택에서 걷어내도 된다.
    /// </summary>
    public bool IsPresenting =>
      _isTyping
      || _isWaitingForInput
      || _inputContext != DialogueInputContext.None
      || HasActiveSelections;

    /// <summary>현재 대화창 계열 UI를 점유 중인 그래프 식별자(없으면 null).</summary>
    public string CurrentDialogueOwner => _owningGraphIdentifier;

    #endregion

    #region Dialogue Ownership (동시 점유 충돌 감지)

    /// <summary>
    /// <paramref name="owningGraphIdentifier"/> 이외의 다른 흐름이 대화창을 점유 중이면 true.
    /// 점유자가 없거나 동일 식별자이면 false.
    /// </summary>
    public bool IsDialogueOwnedByOther(string owningGraphIdentifier)
    {
      if (string.IsNullOrEmpty(_owningGraphIdentifier))
        return false;

      return !string.Equals(_owningGraphIdentifier, owningGraphIdentifier, StringComparison.Ordinal);
    }

    /// <summary>대화창 점유자를 등록/갱신한다.</summary>
    public void MarkDialogueOwner(string owningGraphIdentifier)
    {
      _owningGraphIdentifier = owningGraphIdentifier;
    }

    /// <summary>대화창 점유자를 해제한다(시나리오 종료 시).</summary>
    public void ClearDialogueOwner()
    {
      _owningGraphIdentifier = null;
    }

    #endregion

    #region Unity Lifecycle

    protected override void Awake()
    {
      base.Awake();

      if (_uiDocument == null)
        _uiDocument = GetComponent<UIDocument>();

      if (_dialoguePanel == null || _dialogueTextLabel == null)
        CacheVisualElements();
    }

    private void OnEnable()
    {
      CacheVisualElements();
    }

    private void OnDisable()
    {
      UnregisterDialogueElementClickHandler();
    }

    private void Update()
    {
      if (_isTyping)
      {
        UpdateTyping();
      }

      TryStartQueuedTransientDialogue();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// InteractableHintUI 컨트롤러를 설정합니다.
    /// ScenarioController에서 호출됩니다.
    /// </summary>
    public void SetInteractableHintUI(InteractableObjectHintUIController hintUI)
    {
      if (hintUI.IsUnityNull())
        return;

      _interactableHintUI = hintUI;
      _reportedMissingHintUI = false;
      Debug.Log("[DialoguePanelUI] InteractableHintUI connected");
    }

    /// <summary>
    /// 힌트 UI 참조를 확보한다. 아직 비어 있으면 레지스트리에서 직접 조회한다.
    ///
    /// <para>선택지 확정은 힌트 UI 목록을 거치므로, 이 참조가 비어 있으면 Choice/Quiz 대화창이
    /// 확정 수단을 갖지 못한 채 입력만 소비하며 영원히 닫히지 않는다. 플레이어 컨트롤러의
    /// 바인딩이 늦어지는 경우에도 스스로 복구할 수 있도록 여기서 지연 조회한다.</para>
    /// </summary>
    private InteractableObjectHintUIController EnsureInteractableHintUI()
    {
      if (!_interactableHintUI.IsUnityNull())
        return _interactableHintUI;

      var resolved = Registry.Registry.Get<InteractableObjectHintUIController>(
        RegistryType.UI, Registry.Registry.TypeKey<InteractableObjectHintUIController>());
      if (!resolved.IsUnityNull())
      {
        _interactableHintUI = resolved;
        _reportedMissingHintUI = false;
      }

      return _interactableHintUI;
    }

    private void CacheVisualElements()
    {
      if (_uiDocument == null)
        return;

      _root = _uiDocument.rootVisualElement;
      if (_root == null)
        return;

      _dialogueElement = _root.Q<DialogueElement>("dialogue-element");
      RegisterDialogueElementClickHandler();
      _dialoguePanel = _dialogueElement;
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

    private void RegisterDialogueElementClickHandler()
    {
      if (_dialogueElementWithClickHandler == _dialogueElement)
        return;

      UnregisterDialogueElementClickHandler();
      if (_dialogueElement == null)
        return;

      _dialogueElement.OnDialogueClicked += HandleDialogueClicked;
      _dialogueElementWithClickHandler = _dialogueElement;
    }

    private void UnregisterDialogueElementClickHandler()
    {
      if (_dialogueElementWithClickHandler == null)
        return;

      _dialogueElementWithClickHandler.OnDialogueClicked -= HandleDialogueClicked;
      _dialogueElementWithClickHandler = null;
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

      // InteractableHintUI 를 시나리오 시작 시점에 무조건 Dialogue 모드로 전환하지 않는다.
      // 예전에는 여기서 EnterDialogueMode() 를 호출해 힌트 목록을 비웠는데, 이는 대화창 UI 를
      // 실제로 표시하지 않는(신호 대기 등 배경 감시용) 시나리오에서도 월드 상호작용 힌트를
      // 지워버려, 시나리오가 끝나기 전까지 Interactable 이 모두 사라져 보이는 버그의 원인이었다.
      // Dialogue/Choice/Quiz 처럼 실제로 대화창을 점유하는 노드가 표시될 때
      // DisplayDialogue/DisplayChoice 내부의 EnsureDialogueModeActive() 가 그 시점에
      // 지연 전환하므로, 시작 시점에 미리 전환할 필요가 없다.

      // 시나리오 시작 시점에는 패널을 열지 않는다. 실제 대화/선택/퀴즈 노드가 표시될 때
      // DisplayDialogue/DisplayChoice 내부의 EnsureOverlayActive() 가 패널 표시 + 오버레이 push 를
      // 수행한다. (신호 대기 등 UI 없는 노드로만 구성된 시나리오가 빈 패널을 띄우고 플레이어
      // 입력을 잠그던 문제를 방지)
      // _autoShowOnScenarioStart 가 명시적으로 true 인 경우에만(레거시 옵트인) 즉시 표시한다.
      if (_autoShowOnScenarioStart)
      {
        ShowPanel();

        // 시나리오 UI 를 오버레이로 취급하여 플레이어 입력/카메라 잠금을 멈추고 커서를 자유롭게 둔다.
        if (!UIOverlayStack.IsTop(this))
          UIOverlayStack.Push(this);
      }

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

      // 대화창 점유자 해제. 누락하면 다음 시나리오가 계속 충돌로 오판된다.
      _owningGraphIdentifier = null;

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

      // 시나리오가 끝나면 오버레이를 제거한다.
      UIOverlayStack.Remove(this);
    }

    /// <summary>
    /// 표시 전용 역할 브랜치의 현재 노드 UI만 닫고 시나리오 프레젠테이션 연결은 유지한다.
    /// 다음 TargetRpc 노드가 같은 컨트롤러를 다시 사용할 수 있다.
    /// </summary>
    public void DismissPresentationNode()
    {
      _isTyping = false;
      _isWaitingForInput = false;
      _inputContext = DialogueInputContext.None;
      _pendingChoiceOptions = null;
      ClearSelections();

      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
        _interactableHintUI.ExitDialogueMode();

      HidePanel();
      UIOverlayStack.Remove(this);
    }

    /// <summary>
    /// 표시 중인 내용이 없는데 오버레이 스택에 남아 있는 항목만 걷어낸다.
    /// 시나리오 정리 경로가 "대화창이 떠 있지 않다"고 판단해 <see cref="DismissPresentationNode"/> 를
    /// 건너뛰더라도, 스택 항목이 남아 있으면 플레이어 입력이 계속 막히므로 여기서 함께 정리한다.
    /// </summary>
    public void ReleaseOverlay()
    {
      UIOverlayStack.Remove(this);
    }

    /// <summary>
    /// 대화 노드 표시
    /// </summary>
    public void DisplayDialogue(string speakerName, string dialogueContent, string portraitIdentifier)
    {
      DisplayDialogue(speakerName, dialogueContent, portraitIdentifier, false);
    }

    public void DisplayDialogue(string speakerName, string dialogueContent, string portraitIdentifier, bool interactionRequired)
    {
      RestoreInteractivePresentation();
      EnsureDialogueModeActive();
      _inputContext = DialogueInputContext.Dialogue;
      _currentDialogueInteractionRequired = interactionRequired;
      _pendingChoiceOptions = null;
      ClearSelections();
      EnsureOverlayActive();
      PresentTextNode(speakerName, dialogueContent, portraitIdentifier);

      OnNodeDisplayed?.Invoke(null); // TODO: 필요하면 노드를 전달한다
    }

    /// <summary>
    /// 입력과 overlay를 점유하지 않고 대화 UI에 전체 텍스트를 즉시 표시한다.
    /// </summary>
    public void DisplayDisinteractableDialogue(string speakerName, string dialogueContent, string portraitIdentifier)
    {
      UIOverlayStack.Remove(this);

      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
        _interactableHintUI.ExitDialogueMode();

      _isTyping = false;
      _isWaitingForInput = false;
      _inputContext = DialogueInputContext.None;
      _currentDialogueInteractionRequired = false;
      _pendingChoiceOptions = null;
      ClearSelections();

      if (_speakerNameLabel != null)
        _speakerNameLabel.text = ResolvePlaceholders(speakerName ?? string.Empty);

      SetPortrait(portraitIdentifier);
      _fullText = ResolvePlaceholders(dialogueContent ?? string.Empty);
      _currentCharIndex = _fullText.Length;
      if (_dialogueTextLabel != null)
        _dialogueTextLabel.text = _fullText;

      SetWaitingIndicatorVisible(false);
      if (_dialoguePanel != null)
      {
        _dialoguePanel.style.opacity = 0f;
      }
      SetPickingModeRecursive(_root, PickingMode.Ignore);
      Show();
      OnNodeDisplayed?.Invoke(null);
    }

    public void SetDisinteractableDialogueOpacity(float opacity)
    {
      if (_dialoguePanel != null)
        _dialoguePanel.style.opacity = Mathf.Clamp01(opacity);
    }

    public void HideDisinteractableDialogue()
    {
      Hide();
      // 비상호작용 안내를 표시할 때 문서 루트 전체의 픽킹을 끈다.
      // 패널만 복구하면 루트의 Ignore 상태가 남아 이후 UI 입력이 막힌다.
      RestoreInteractivePresentation();
    }

    /// <summary>
    /// 시나리오 그래프와 무관한 짧은 안내 대화를 안전하게 표시한다.
    /// 기존 Dialogue/Choice가 UI를 점유 중이면 요청을 큐에 보관해 종료 후 재생하므로,
    /// 시나리오 대화의 입력 상태·선택지·오버레이를 덮어쓰지 않는다.
    /// </summary>
    public bool TryPresentTransientDialogue(
      string speakerName,
      string dialogueContent,
      float duration = 3f,
      string portraitIdentifier = null,
      Action onFinished = null)
    {
      if (_dialoguePanel == null || _dialogueTextLabel == null)
        CacheVisualElements();
      if (_dialoguePanel == null || _dialogueTextLabel == null)
      {
        Debug.LogWarning("[DialoguePanelUI] Transient dialogue cannot be presented because required UI elements are unavailable.", this);
        return false;
      }

      _transientDialogueQueue.Enqueue(new TransientDialogueRequest(
        speakerName ?? string.Empty,
        dialogueContent ?? string.Empty,
        portraitIdentifier,
        Mathf.Max(0f, duration),
        onFinished));
      TryStartQueuedTransientDialogue();
      return true;
    }

    private void TryStartQueuedTransientDialogue()
    {
      if (_transientDialogueRoutine != null || _transientDialogueQueue.Count == 0 || !CanPresentTransientDialogue())
        return;
      _transientDialogueRoutine = StartCoroutine(PresentTransientDialogueRoutine(_transientDialogueQueue.Dequeue()));
    }

    private bool CanPresentTransientDialogue()
    {
      // 현재 시나리오가 살아 있더라도 UI를 실제로 점유하지 않는 신호 대기 상태라면 표시할 수 있다.
      // 반대로 대화/선택 입력·오버레이가 활성화된 경우에는 종료 뒤로 미룬다.
      return _inputContext == DialogueInputContext.None && !_isTyping && !_isWaitingForInput &&
             !HasActiveSelections && UIOverlayStack.IsEmpty();
    }

    private System.Collections.IEnumerator PresentTransientDialogueRoutine(TransientDialogueRequest request)
    {
      DisplayDisinteractableDialogue(request.SpeakerName, request.Content, request.PortraitIdentifier);
      // DisplayDisinteractableDialogue는 기존 fade 연출의 시작값(0)을 설정한다.
      // 즉석 안내는 별도 fade를 사용하지 않으므로 바로 보이는 상태로 전환한다.
      SetDisinteractableDialogueOpacity(1f);
      float elapsed = 0f;
      while (elapsed < request.Duration)
      {
        elapsed += Time.unscaledDeltaTime;
        yield return null;
      }
      HideDisinteractableDialogue();
      _transientDialogueRoutine = null;
      request.OnFinished?.Invoke();
    }

    /// <summary>
    /// 선택지 표시
    /// </summary>
    public void DisplayChoice(string speakerName, string dialogueContent, string portraitIdentifier, IReadOnlyList<ScenarioChoiceOption> options)
    {
      RestoreInteractivePresentation();
      EnsureDialogueModeActive();
      _inputContext = DialogueInputContext.Choice;
      _currentDialogueInteractionRequired = false;
      _pendingChoiceOptions = options;
      ClearSelections();
      EnsureOverlayActive();
      PresentTextNode(speakerName, dialogueContent, portraitIdentifier);

      OnNodeDisplayed?.Invoke(null); // TODO: 필요하면 노드를 전달한다
    }

    private void ShowChoices(IReadOnlyList<ScenarioChoiceOption> options)
    {
      // 선택지를 힌트 UI에 넣기 직전에 Dialogue 모드를 한 번 더 보장한다.
      // 타이핑이 진행되는 사이 다른 흐름(즉석 안내 대화 등)이 ExitDialogueMode()를 호출했다면
      // 힌트 UI에는 선택지 대신 주변 월드 Interactable 목록이 남고, 확정 입력이 선택지가 아니라
      // 엉뚱한 오브젝트와의 상호작용으로 처리된다.
      EnsureDialogueModeActive();
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
      else if (_interactableHintUI.IsUnityNull())
      {
        Debug.LogWarning(
          "[DialoguePanelUI] InteractableHintUI가 없어 선택지를 표시하지 못했습니다. 확정 입력 시 다시 시도합니다.",
          this);
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
      // UI Toolkit ClickEvent와 PlayerController의 레거시 마우스 입력은 같은 프레임에
      // 모두 발생할 수 있다. 하나의 클릭이 다음 노드를 두 번 진행시키지 않도록 막는다.
      if (HasConsumedInputThisFrame)
        return;

      // 타이핑 중의 진행 입력은 설정에 따라 재생을 건너뛰거나 소비만 한다.
      // 어느 쪽이든 이 입력은 대화창의 것이므로 먼저 소비 표시를 해서, 같은 프레임의
      // F/좌클릭이 주변 Interactable 상호작용으로 새는 것을 막는다.
      if (_isTyping)
      {
        MarkInputConsumed();

        // 건너뛰기가 꺼져 있으면 재생을 끝까지 이어 간다. 재생이 끝나면 FinishTyping 이
        // 입력 대기 또는 선택지 표시로 넘기므로 시나리오 흐름은 그대로 이어진다.
        // 단, 재생이 멈춰 버린 상태에서는 설정과 무관하게 끝내어 입력 잠금이 영구화되지 않게 한다.
        if (DialogueSkipPreference.IsEnabled || IsTypingStalled())
          SkipTyping();
        return;
      }

      // Choice: 재생 완료 후 입력은 현재 선택지를 확정.
      if (_inputContext == DialogueInputContext.Choice)
      {
        // 선택지를 실제로 확정하지 못하는 경우(아직 선택지가 만들어지지 않은 프레임 등)에도
        // 이 입력은 대화창의 것이다. 소비 표시를 먼저 해서 같은 프레임의 F/좌클릭이
        // 주변 Interactable 상호작용으로 새는 것을 막는다.
        MarkInputConsumed();

        var hintUI = EnsureInteractableHintUI();
        if (!hintUI.IsUnityNull() && hintUI.HasDialogueSelection())
        {
          hintUI.ExecuteSelectedDialogueSelection(null);
          return;
        }

        // 선택지가 아직 힌트 UI에 반영되지 않았다면(모드 전환 경쟁 등) 다시 표시하고
        // 이번 입력은 흘린다. 여기서 그냥 반환하면 확정 수단이 없어 대화가 멈춘다.
        if (!hintUI.IsUnityNull() && _pendingChoiceOptions != null)
        {
          ShowChoices(_pendingChoiceOptions);
          return;
        }

        // 힌트 UI를 끝내 확보하지 못하면 선택지를 표시할 수단도, 확정할 수단도 없다.
        // 이 상태를 그대로 두면 대화창이 입력만 소비하며 영원히 닫히지 않고 플레이어 조작까지
        // 잠기므로, 원인을 로그로 남기고 최소한 입력 잠금은 해제한다.
        // (선택 결과는 평가 기록에 반영되는 값이므로 임의로 확정하지 않는다.)
        if (!_reportedMissingHintUI)
        {
          _reportedMissingHintUI = true;
          Debug.LogError(
            "[DialoguePanelUI] InteractableObjectHintUIController를 찾지 못해 선택지를 표시하거나 확정할 수 없습니다. "
            + "대화창 입력 잠금만 해제합니다. 씬에 힌트 UI 컨트롤러가 존재하는지 확인해야 합니다.",
            this);
        }

        DismissDialogue();
        return;
      }

      // Dialogue: 재생 완료 후 입력은 다음 노드 진행.
      if (_inputContext == DialogueInputContext.Dialogue)
      {
        MarkInputConsumed();

        if (_currentDialogueInteractionRequired)
        {
          DismissDialogue();
          OnAdvanceRequested?.Invoke();

          if (!_currentController.IsUnityNull())
          {
            _currentController.SubmitLocalAdvance();
          }
          return;
        }

        // 일반 대화도 다음 노드가 퀘스트/신호 대기처럼 UI를 표시하지 않는 노드일 수 있다.
        // 이때 패널과 오버레이를 먼저 해제하지 않으면 이전 대화가 화면에 남는다.
        DismissDialogue();
        OnAdvanceRequested?.Invoke();

        if (!_currentController.IsUnityNull())
        {
          _currentController.SubmitLocalAdvance();
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

    /// <summary>
    /// 이번 프레임의 진행/선택 입력을 대화창이 소비했음을 표시한다.
    /// </summary>
    private void MarkInputConsumed()
    {
      _lastInputConsumedFrame = Time.frameCount;
    }

    private void OnSelectionInteracted(ScenarioChoiceOption option, int index)
    {
      Debug.Log($"[DialoguePanelUI] Option selected: {index} - {option.DisplayText}");

      // 힌트 목록 행을 직접 클릭해 들어오는 경로(UI Toolkit 이벤트)도 같은 프레임 입력을 소비한다.
      MarkInputConsumed();

      // 선택 이벤트 발생
      OnSelectionMade?.Invoke(index);

      // 선택지 정리
      ClearSelections();
      _pendingChoiceOptions = null;
      _inputContext = DialogueInputContext.None;

      // 선택 완료 직후 이전 문제/선택지 패널을 닫는다. 다음 노드가 Delay 또는
      // 월드 상호작용이면 새 UI가 뜰 때까지 이전 문제지가 남아 혼동을 주기 때문이다.
      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
        _interactableHintUI.ExitDialogueMode();
      HidePanel();
      UIOverlayStack.Remove(this);

      // ScenarioController에 선택 전달
      if (!_currentController.IsUnityNull())
      {
        _currentController.SubmitLocalOptionSelection(index);
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
      // Dialogue overlays can pause scaled gameplay time.  Typing is UI
      // presentation and must still progress while that overlay is active.
      _lastTypeTime = Time.unscaledTime;
      _lastTypeProgressUnscaledTime = Time.unscaledTime;

      if (_dialogueTextLabel != null)
      {
        _dialogueTextLabel.text = "";
      }

      SetWaitingIndicatorVisible(false);
    }

    private void UpdateTyping()
    {
      if (!_isTyping)
        return;

      // 표시할 문장이 없으면 즉시 완료 처리한다. 건너뛰기가 꺼진 상태에서 재생이
      // 끝나지 않으면 입력 대기 상태로 넘어가지 못하므로 방어적으로 끝낸다.
      if (string.IsNullOrEmpty(_fullText))
      {
        FinishTyping();
        return;
      }

      if (Time.unscaledTime - _lastTypeTime >= _typingSpeed)
      {
        _lastTypeTime = Time.unscaledTime;
        _lastTypeProgressUnscaledTime = Time.unscaledTime;
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

    /// <summary>
    /// 텍스트 내의 이스케이프 워드를 런타임 값으로 치환합니다.
    ///
    /// 지원되는 플레이스홀더:
    ///   <c>{PLAYER_NAME}</c> — IntroScene에서 입력한 플레이어 표시 이름.
    ///   값이 없으면 빈 문자열로 치환됩니다.
    /// </summary>
    private static string ResolvePlaceholders(string text)
    {
      if (string.IsNullOrEmpty(text))
        return text;

      if (text.Contains(PlayerNamePlaceholder))
      {
        var playerName = Registry.Registry.Get<string>(
          RegistryType.RuntimeState, RegistryGlobalKeys.UserDisplayName);
        text = text.Replace(PlayerNamePlaceholder, playerName ?? string.Empty);
      }

      return text;
    }

    private void PresentTextNode(string speakerName, string dialogueContent, string portraitIdentifier)
    {
      _isWaitingForInput = false;

      if (_speakerNameLabel != null)
      {
        _speakerNameLabel.text = ResolvePlaceholders(speakerName ?? "");
      }

      SetPortrait(portraitIdentifier);

      StartTyping(ResolvePlaceholders(dialogueContent ?? ""));
    }

    private void SetPortrait(string portraitIdentifier)
    {
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
    }

    /// <summary>
    /// 타이핑을 스킵하고 전체 텍스트를 즉시 표시합니다.
    ///
    /// <see cref="DialogueSkipPreference"/> 는 사용자 입력 경로(<see cref="TrySelectCurrentOption"/>)에서만
    /// 확인합니다. 이 메서드를 직접 호출하는 내부·외부 흐름은 설정과 무관하게 재생을 끝내므로,
    /// 시나리오 제어 코드가 필요할 때 언제든 표시를 완료시킬 수 있습니다.
    /// </summary>
    public void SkipTyping()
    {
      if (!_isTyping)
        return;
      FinishTyping();
    }

    /// <summary>
    /// 재생이 멈춘 상태인지 여부. 건너뛰기 설정이 꺼져 있을 때 입력 잠금이 영구화되지 않도록
    /// 마지막 글자 진행 이후 흐른 unscaled 시간을 기준으로 판정합니다.
    /// </summary>
    private bool IsTypingStalled()
    {
      return IsTypingStalled(Time.unscaledTime - _lastTypeProgressUnscaledTime, _typingSpeed);
    }

    /// <summary>
    /// 마지막 글자 진행 이후 <paramref name="secondsSinceProgress"/>초가 흘렀을 때 재생이 멈춘 것으로
    /// 볼지 판정합니다. 허용 시간은 <see cref="TypingStallGraceSeconds"/>와 타이핑 간격의 4배 중
    /// 큰 값이므로, 느린 타이핑 설정에서도 정상 재생을 멈춤으로 오판하지 않습니다.
    /// </summary>
    public static bool IsTypingStalled(float secondsSinceProgress, float typingSpeed)
    {
      float grace = Mathf.Max(TypingStallGraceSeconds, Mathf.Max(0f, typingSpeed) * 4f);
      return secondsSinceProgress > grace;
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
        _dialoguePanel.style.opacity = 1f;
        SetPickingModeRecursive(_dialoguePanel, PickingMode.Position);
      }

      _dialogueElement?.Hide();
    }

    private static void SetPickingModeRecursive(VisualElement element, PickingMode pickingMode)
    {
      if (element == null)
        return;

      element.pickingMode = pickingMode;
      for (int i = 0; i < element.childCount; i++)
        SetPickingModeRecursive(element[i], pickingMode);
    }

    private void RestoreInteractivePresentation()
    {
      // Choices belong to a separate UIDocument. Keep the fullscreen document root
      // transparent to picking when restoring the visible dialogue panel.
      if (_root != null)
        _root.pickingMode = PickingMode.Ignore;
      SetPickingModeRecursive(_dialoguePanel, PickingMode.Position);
      if (_dialoguePanel != null)
        _dialoguePanel.style.opacity = 1f;
    }

    public void DismissDialogue()
    {
      _isTyping = false;
      _isWaitingForInput = false;
      _inputContext = DialogueInputContext.None;
      _pendingChoiceOptions = null;
      _currentDialogueInteractionRequired = false;
      _fullText = string.Empty;
      _currentCharIndex = 0;
      ClearSelections();
      Hide();

      UIOverlayStack.Remove(this);

      // interaction-required 대화가 닫힌 뒤에는 월드 상호작용 힌트를 즉시 다시 보이도록
      // 일반 모드로 복귀한다. 이후 다음 Dialogue/Choice 표시 시 다시 Dialogue 모드로 전환된다.
      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
        _interactableHintUI.ExitDialogueMode();
    }

    private void EnsureDialogueModeActive()
    {
      var hintUI = EnsureInteractableHintUI();
      if (!hintUI.IsUnityNull() && !hintUI.IsDialogueMode)
        hintUI.EnterDialogueMode();
    }

    public void EnsureOverlayActive()
    {
      if (!UIOverlayStack.IsTop(this))
      {
        UIOverlayStack.Push(this);
      }

      Show();
    }

    // IUIOverlay

    // 다른 오버레이에 가려져 표시만 숨긴 상태인지 여부. 덮개가 닫혀 최상단으로 돌아올 때만 복구한다.
    // (처음 표시되는 경로에서도 OnOverlayPushed 가 호출되므로, 이 플래그 없이 복구하면
    //  재생이 시작되기도 전에 선택지가 먼저 나타난다.)
    private bool _hiddenWhileCovered;

    public void OnOverlayPushed()
    {
      // 커맨드 채팅이나 서버가 내려보낸 문제지에 잠시 가려졌다가 다시 최상단으로 돌아온 경우,
      // 가려지는 동안 유지해 둔 대화/선택지 상태를 화면에 다시 올린다.
      if (_hiddenWhileCovered)
      {
        _hiddenWhileCovered = false;

        if (IsPresenting)
        {
          Show();

          // 가려져 있는 사이 다른 흐름이 힌트 UI 모드를 되돌렸다면 선택지가 사라진 채 확정 수단이 없어진다.
          // 이미 표시했던 선택지는 힌트 UI에 다시 채워 넣는다.
          if (_inputContext == DialogueInputContext.Choice && HasActiveSelections && _pendingChoiceOptions != null)
          {
            var hintUI = EnsureInteractableHintUI();
            if (!hintUI.IsUnityNull() && !hintUI.HasDialogueSelection())
              ShowChoices(_pendingChoiceOptions);
          }
        }
      }

      OverlayPushed?.Invoke();
    }

    public void OnOverlayPopped()
    {
      // 다른 오버레이가 위에 Push 되어 잠시 가려진 것뿐이면 표시만 숨기고 상태는 그대로 둔다.
      // 여기서 상태까지 지우면 덮개가 닫힌 뒤 대화창이 보이지 않는 채 스택 최상단에 남아
      // 진행 키도 Escape 도 통하지 않는 입력 잠금이 된다.
      if (UIOverlayStack.Contains(this))
      {
        _hiddenWhileCovered = true;
        Hide();
        OverlayPopped?.Invoke();
        return;
      }

      _hiddenWhileCovered = false;

      // UIOverlayStack.Clear(), 강제 제거 등 정상 진행 입력 이외의 경로에서도
      // 보이는 패널·선택지·포인터 상태가 남지 않도록 표시 상태를 복구한다.
      _isTyping = false;
      _isWaitingForInput = false;
      _inputContext = DialogueInputContext.None;
      _pendingChoiceOptions = null;
      _currentDialogueInteractionRequired = false;
      ClearSelections();
      if (!_interactableHintUI.IsUnityNull() && _interactableHintUI.IsDialogueMode)
        _interactableHintUI.ExitDialogueMode();
      Hide();
      RestoreInteractivePresentation();
      OverlayPopped?.Invoke();
    }

    #endregion

    #region DialogueElement Interaction

    private void HandleDialogueClicked()
    {
      TrySelectCurrentOption();
    }

    /// <summary>
    /// 패널 토글
    /// </summary>
    public void TogglePanel()
    {
      if (_dialoguePanel == null)
        return;

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
  public class ScenarioSelectionInteractable : IInteract, InteractableEntity.IInteractionRegistryExempt
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
