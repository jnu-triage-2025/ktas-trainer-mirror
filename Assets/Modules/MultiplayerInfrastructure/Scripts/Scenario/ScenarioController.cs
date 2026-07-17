using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using MultiplayerInfrastructure.Scenario.Preflight;
using MultiplayerInfrastructure.Scenario.Requirements;
using MultiplayerInfrastructure.TTS;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Logging;
using FishNet.Object;
using FishNet;
using FishNet.Connection;
using Unity.VisualScripting;
using TriageTrainer.Entity;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 시나리오 흐름을 제어합니다.
  /// UI 제어는 ScenarioPanelUIController에 위임합니다.
  /// </summary>
  public class ScenarioController : MonoBehaviour
  {
    #region Serialized Fields

    private static ScenarioController _instance;
    public static ScenarioController Instance => _instance;

    [Header("References")]
    [SerializeField] private DialoguePanelUIController _uiController;
    [SerializeField] private MainCameraController _camController;
    [SerializeField] private InteractableObjectHintUIController _hintUIController;
    [SerializeField] private ScenarioTTSService _ttsService;
    [SerializeField] private AudioSource _ttsAudioSource;

    [Header("Preflight (사전 요구사항 검증)")]
    [Tooltip("시나리오 시작 직전에 그래프가 요구하는 씬/레지스트리 요소가 준비되어 있는지 점검한다.")]
    [SerializeField] private bool _preflightEnabled = true;
    [SerializeField]
    private ScenarioPreflightPolicy _preflightPolicy = ScenarioPreflightPolicy.Default;
    [SerializeField] private ScenarioRuntimeValidationMode _runtimeRequirementsValidationMode = ScenarioRuntimeValidationMode.ReportOnly;
    [SerializeField, Min(0f)] private float _runtimeRequirementsReadinessTimeoutSeconds = 10f;
    [SerializeField, Min(0.01f)] private float _runtimeRequirementsReadinessPollIntervalSeconds = 0.1f;

    [Header("Concurrency (동시 실행 충돌)")]
    [Tooltip("두 개 이상의 시나리오 흐름이 동시에 대화창 UI(Dialogue/Choice/Quiz)를 점유하려 할 때의 처리 정책.")]
    [SerializeField]
    private ScenarioConcurrencyConflictPolicy _concurrencyConflictPolicy = ScenarioConcurrencyConflictPolicy.Warn;

    [Header("Validator Block Logging")]
    [Tooltip("Validator 게이트가 조건 미충족으로 진행을 막기 시작할 때 오류를 기록할 대상.")]
    [SerializeField]
    private ScenarioValidatorBlockLogTarget _validatorBlockLogTargets =
      ScenarioValidatorBlockLogTarget.UnityConsole | ScenarioValidatorBlockLogTarget.SessionLog;

    /// <summary>동시 대화창 점유 충돌 처리 정책. 인게임 커맨드로 런타임 변경 가능.</summary>
    public ScenarioConcurrencyConflictPolicy ConcurrencyConflictPolicy
    {
      get => _concurrencyConflictPolicy;
      set => _concurrencyConflictPolicy = value;
    }

    public ScenarioValidatorBlockLogTarget ValidatorBlockLogTargets
    {
      get => _validatorBlockLogTargets;
      set => _validatorBlockLogTargets = value;
    }

    #endregion

    #region Private Fields

    private ScenarioGraph _currentGraph;
    private IScenarioNode _currentNode;
    private List<ScenarioChoiceOption> _activeOptions = new();
    private ScenarioQuizNode _activeQuizNode;
    private readonly Dictionary<string, GraphVisitHistory> _graphVisitHistory = new Dictionary<string, GraphVisitHistory>();
    private readonly Dictionary<string, string> _stateStore = new Dictionary<string, string>();
    private int? _scenarioOwnerClientId;
    private ChatUIController _chatUIController;
    private ChatService _chatService;
    private Coroutine _dialogueAutoAdvanceRoutine;
    private Coroutine _runtimeRequirementsWaitRoutine;

    private sealed class GraphVisitHistory
    {
      public readonly Dictionary<string, List<int>> NodeVisitOrders = new Dictionary<string, List<int>>();
      public int Sequence;
    }

    /// <summary>
    /// 브랜치 체인(<see cref="RunBranchChain"/>)이 노드를 실행하는 동안 0보다 크다.
    /// 브랜치 내부의 노드 실행기(QuestControl/StateUpdate/PlayerTag/InvokeEvent 등)는
    /// 내부적으로 전역 <see cref="Advance"/> 를 호출하는데, 이 값이 0보다 큰 동안에는
    /// 전역 진행을 무시(no-op)하여 브랜치 노드의 부수효과가 전역 시나리오 커서를
    /// 끌고 가지 않도록 격리한다. 브랜치는 <see cref="IScenarioNode.NextIdentifier"/> 로 직접 이동한다.
    /// 동시 실행되는 여러 브랜치/지연 코루틴을 고려해 카운터로 관리한다.
    /// </summary>
    private int _globalAdvanceSuppressionDepth;

    #endregion

    #region State

    public enum State
    {
      Inactive,
      ExecutingDialogue,
      ExecutingChoice,
      ExecutingSound,
      ExecutingPlayerMove,
      ExecutingNPCMove,
      ExecutingCameraTarget,
      ExecutingInvokeEvent,
      ExecutingValidator,
      ExecutingParallel,
      ExecutingQuestControl,
      ExecutingQuestWaypointHighlight,
      ExecutingDelay,
      ExecutingInteraction,
      ExecutingCombineItem,
      ExecutingQuiz,
      ExecutingStateUpdate,
      ExecutingTTS,
      ExecutingPlayerTag,
      ExecutingEntityPresetSpawn,
      ExecutingEntityTag,
      ExecutingEntityInit,
      ExecutingTriageAssessControl,
      ExecutingPatientMedicalStatePreset,
      ExecutingItemSubmissionConfig,
      ExecutingNpcInteractControl,
      ExecutingTimeControl,
      ExecutingDisinteractableDialogue,
    }

    [SerializeField] private State _state = State.Inactive;

    #endregion

    #region Events

    public event Action OnScenarioStarted;
    public event Action OnScenarioEnded;
    public event Action<IScenarioNode> OnNodeChanged;
    public event Action<ScenarioChoiceOption> OnOptionSelected;

    /// <summary>
    /// WaitForCondition 게이트가 WaitTimeoutSeconds 안에 조건을 충족하지 못해 타임아웃 정책이
    /// 적용될 때 1회 발생한다. 평가 기록(루브릭의 "미수행" 판정 등, G-3)에서 구독할 수 있다.
    /// 인자: (타임아웃된 Validator 노드, 적용된 타임아웃 정책).
    /// </summary>
    public event Action<ScenarioValidatorNode, ScenarioValidatorWaitTimeoutBehavior> OnValidatorWaitTimeout;

    #endregion

    #region Properties

    public bool IsActive => _state != State.Inactive;
    public State CurrentState => _state;
    public IScenarioNode CurrentNode => _currentNode;
    public ScenarioGraph CurrentGraph => _currentGraph;
    public IReadOnlyList<int> GetNodeVisitOrders(string graphIdentifier, string nodeIdentifier)
    {
      if (string.IsNullOrWhiteSpace(graphIdentifier) || string.IsNullOrWhiteSpace(nodeIdentifier))
      {
        return Array.Empty<int>();
      }

      if (_graphVisitHistory.TryGetValue(graphIdentifier, out var history)
          && history != null
          && history.NodeVisitOrders.TryGetValue(nodeIdentifier, out var orders)
          && orders != null)
      {
        return orders;
      }

      return Array.Empty<int>();
    }

    public IReadOnlyList<int> GetNodeVisitOrders(string nodeIdentifier)
    {
      if (_currentGraph == null)
      {
        return Array.Empty<int>();
      }

      return GetNodeVisitOrders(_currentGraph.Identifier, nodeIdentifier);
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        Destroy(gameObject);
        return;
      }
      _instance = this;
    }

    private void Reset()
    {
      // 컴포넌트를 처음 붙일 때 Preflight 정책을 안전한 기본값(콘솔/인게임챗 둘 다 경고 + 계속 진행)으로 초기화한다.
      _preflightEnabled = true;
      _preflightPolicy = ScenarioPreflightPolicy.Default;
      _runtimeRequirementsValidationMode = ScenarioRuntimeValidationMode.ReportOnly;
      // 동시 실행 충돌 정책 기본값: 경고 후 계속 진행(기존 동작 유지).
      _concurrencyConflictPolicy = ScenarioConcurrencyConflictPolicy.Warn;
      _validatorBlockLogTargets =
        ScenarioValidatorBlockLogTarget.UnityConsole | ScenarioValidatorBlockLogTarget.SessionLog;
    }

    public void RegisterReferences(
      DialoguePanelUIController uiController,
      MainCameraController camController,
      InteractableObjectHintUIController hintUIController)
    {
      _uiController = uiController;
      _camController = camController;
      _hintUIController = hintUIController;
    }

    private void ResolveUIControllers()
    {
      if (_uiController.IsUnityNull())
        _uiController = Registry.Registry.Get<DialoguePanelUIController>(RegistryType.UI, Registry.Registry.TypeKey<DialoguePanelUIController>());

      if (_hintUIController.IsUnityNull())
        _hintUIController = Registry.Registry.Get<InteractableObjectHintUIController>(RegistryType.UI, Registry.Registry.TypeKey<InteractableObjectHintUIController>());
    }

    /// <summary>
    /// TTS 재생 서비스를 확보한다. 인스펙터에 연결되지 않았다면(예: 프리팹이 아닌 씬의
    /// 단독 ScenarioController) 씬에서 찾고, 그래도 없으면 런타임에 자동 생성한다.
    /// 자동 생성 시 ScenarioTTSService(및 RequireComponent에 의한 TTSService·AudioSource)가
    /// 함께 붙으며, 해당 AudioSource를 재생 대상으로 사용한다.
    /// </summary>
    private void ResolveTTSService()
    {
      if (!_ttsService.IsUnityNull())
      {
        if (_ttsAudioSource == null)
          _ttsAudioSource = _ttsService.AudioSource;
        return;
      }

      // 씬에 이미 존재하는 서비스 탐색
#if UNITY_2023_1_OR_NEWER
      var existing = UnityEngine.Object.FindFirstObjectByType<ScenarioTTSService>();
#else
      var existing = UnityEngine.Object.FindObjectOfType<ScenarioTTSService>();
#endif
      if (existing != null)
      {
        _ttsService = existing;
        if (_ttsAudioSource == null)
          _ttsAudioSource = existing.AudioSource;
        return;
      }

      // 자동 생성 (RequireComponent로 TTSService·AudioSource가 함께 추가됨)
      var go = new GameObject("ScenarioTTSService (auto)");
      _ttsService = go.AddComponent<ScenarioTTSService>();
      if (_ttsAudioSource == null)
        _ttsAudioSource = _ttsService.AudioSource;

      Debug.Log("[ScenarioController] ScenarioTTSService가 연결되지 않아 자동 생성했습니다.");
    }

    private void Start()
    {
      // 이벤트 구독
      ScenarioInteractable.OnScenarioRequested += HandleScenarioRequested;
      ScenarioTriggerZone.OnScenarioRequested += HandleScenarioRequested;
    }

    private void OnDestroy()
    {
      // 이벤트 구독 해제
      ScenarioInteractable.OnScenarioRequested -= HandleScenarioRequested;
      ScenarioTriggerZone.OnScenarioRequested -= HandleScenarioRequested;

      if (_instance == this)
      {
        _instance = null;
      }
    }

    #endregion

    #region Public API

    /// <summary>
    /// 시나리오 시작
    /// </summary>
    public void StartScenario(ScenarioGraph graph, string startNodeIdentifier = null)
    {
      StartScenario(graph, startNodeIdentifier, null);
    }

    public void StartScenario(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
    {
      StartScenarioInternal(graph, startNodeIdentifier, ownerClientId, false);
    }

    private void StartScenarioInternal(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId, bool requirementsAlreadyValidated)
    {
      if (graph == null)
      {
        Debug.LogError("[ScenarioController] Cannot start scenario with null graph");
        return;
      }

      if (_runtimeRequirementsWaitRoutine != null)
      {
        StopCoroutine(_runtimeRequirementsWaitRoutine);
        _runtimeRequirementsWaitRoutine = null;
      }

      var runtimeValidationMode = requirementsAlreadyValidated
        ? ScenarioRuntimeValidationMode.Off
        : _runtimeRequirementsValidationMode;
      if (runtimeValidationMode == ScenarioRuntimeValidationMode.ReportOnly
          && _preflightPolicy.MissingBehavior == ScenarioPreflightMissingBehavior.AbortStart)
        runtimeValidationMode = ScenarioRuntimeValidationMode.AbortScenarioStart;
      // Runtime requirements mode is the authoritative switch.  The legacy
      // preflight checkbox must not silently disable an explicitly configured
      // strict runtime gate.
      if (!requirementsAlreadyValidated && runtimeValidationMode != ScenarioRuntimeValidationMode.Off)
      {
        var runtimeValidation = ScenarioRuntimeRequirementsValidator.Validate(
          graph,
          runtimeValidationMode,
          CreateRuntimeRequirementsValidationContext());
        if (_preflightPolicy.WarnToConsole)
        {
          foreach (var diagnostic in runtimeValidation.Diagnostics.Where(value => value.Severity >= ScenarioRequirementDiagnosticSeverity.Warning))
            Debug.LogWarning($"[ScenarioRuntime] {diagnostic.Code}: {diagnostic.Message}");
        }
        if (runtimeValidation.Readiness == ScenarioRuntimeReadiness.NotReady
            && (runtimeValidationMode == ScenarioRuntimeValidationMode.AbortScenarioStart
                || runtimeValidationMode == ScenarioRuntimeValidationMode.AbortSessionBootstrap))
        {
          _runtimeRequirementsWaitRoutine = StartCoroutine(WaitForRuntimeRequirementsAndStart(graph, startNodeIdentifier, ownerClientId, runtimeValidationMode));
          return;
        }
        if (runtimeValidation.ShouldAbort)
        {
          if (_preflightPolicy.WarnToConsole)
            Debug.LogError($"[ScenarioRuntime] Aborting scenario '{graph.Identifier}' before changing current scenario state.");
          if (_preflightPolicy.WarnToInGameChat)
            AppendSystemChatMessage($"[ScenarioRuntime] Scenario '{graph.Identifier}' could not start because requirements are unresolved.");
          return;
        }
      }

      // The canonical runtime validator is the sole gate whenever enabled.
      // Retain legacy preflight only for explicit runtime-validation Off mode.
      if (!requirementsAlreadyValidated
          && _preflightEnabled
          && runtimeValidationMode == ScenarioRuntimeValidationMode.Off
          && !ScenarioPreflight.Run(graph, _preflightPolicy, AppendSystemChatMessage, out _))
      {
        return;
      }

      // 이전 시나리오 실행에서 남은 코루틴(병렬 브랜치 등)이 있으면 새 시나리오 시작 전에 정리한다.
      StopAllCoroutines();
      CancelDialogueAutoAdvance();
      // StopAllCoroutines 로 강제 종료된 브랜치 코루틴은 finally 가 실행되지 않아
      // 억제 카운터가 불균형 상태로 남을 수 있으므로 명시적으로 초기화한다.
      _globalAdvanceSuppressionDepth = 0;
      ResetNodeVisitOrders(graph.Identifier);
      ScenarioInteractionSignals.ClearAllInternalSignals();

      // 이전 시나리오에서 남았을 수 있는 모든 타이머/표시를 새 시나리오 시작 시 정리한다.
      ScenarioTimeRelay.ClearAllAuthoritative();

      _currentGraph = graph;
      _scenarioOwnerClientId = ownerClientId;

      // 시나리오가 요구하는 퀘스트 정의 include를 선로딩한다.
      QuestDefinitionRegistry.EnsureIncludesLoaded(graph.QuestDefinitionIncludes);

      ResolveUIControllers();
      ResolveTTSService();

      // 이전 실행이 비상호작용 대화 fade 도중 중단된 경우 남은 UI 상태를 정리한다.
      if (!_uiController.IsUnityNull())
        _uiController.HideDisinteractableDialogue();

      // 새 시나리오 시작은 이전 실행을 강제 정리한 직후이므로, 대화창 점유 상태를 초기화한다.
      // (이전 실행이 EndScenario 를 거치지 않고 덮어써진 경우, 스테일 점유자가 새 시나리오
      //  자신의 대화 노드를 오탐(충돌)하게 만드는 것을 방지한다.)
      _branchPromptActive = false;
      if (!_uiController.IsUnityNull())
        _uiController.ClearDialogueOwner();

      // 시작 노드 찾기
      string startId = startNodeIdentifier;
      if (string.IsNullOrEmpty(startId))
      {
        // 첫 번째 노드 찾기 (임시 로직)
        foreach (var node in graph.Nodes.Values)
        {
          startId = node.Identifier;
          break;
        }
      }

      if (!graph.TryGetNode(startId, out var startNode))
      {
        Debug.LogError($"[ScenarioController] Start node '{startId}' not found");
        return;
      }

      _currentNode = startNode;
      _state = State.Inactive; // 초기화

      // UI 시작
      if (_uiController != null)
      {
        _uiController.SetInteractableHintUI(_hintUIController);
        _uiController.StartScenario(this);
      }

      OnScenarioStarted?.Invoke();
      Debug.Log("[ScenarioController] Scenario started");
      try
      {
        GameLogService.WriteScenario(
          $"Scenario started: graph={graph.Identifier}, startNode={startId}, ownerClientId={ownerClientId?.ToString() ?? "null"}",
          graph.Identifier);
      }
      catch { /* 로그 실패는 시나리오 실행에 영향 없음 */ }

      // PlayTTS 노드의 동적 세그먼트를 백그라운드에서 미리 합성 (캐싱)
      PrewarmTTSCache();

      // 첫 노드 실행
      ExecuteNode(startNode);
    }

    private static ScenarioRuntimeValidationContext CreateRuntimeRequirementsValidationContext()
    {
      var offline = InstanceFinder.IsOffline;
      var isServer = offline || InstanceFinder.IsServerStarted;
      var isClient = offline || InstanceFinder.IsClientStarted;
      return new ScenarioRuntimeValidationContext(
        ScenarioRequirementAuthority.Any,
        isServer,
        isClient,
        isServer && isClient);
    }

    private IEnumerator WaitForRuntimeRequirementsAndStart(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId, ScenarioRuntimeValidationMode validationMode)
    {
      ScenarioRuntimeValidationResult completed = null;
      yield return ScenarioRuntimeRequirementsValidator.WaitUntilReady(
        graph,
        validationMode,
        CreateRuntimeRequirementsValidationContext(),
        _runtimeRequirementsReadinessTimeoutSeconds,
        _runtimeRequirementsReadinessPollIntervalSeconds,
        value => completed = value);
      _runtimeRequirementsWaitRoutine = null;
      if (completed == null || completed.ShouldAbort)
      {
        if (_preflightPolicy.WarnToConsole)
          Debug.LogError($"[ScenarioRuntime] Aborting scenario '{graph.Identifier}' because requirements did not become ready.");
        if (_preflightPolicy.WarnToInGameChat)
          AppendSystemChatMessage($"[ScenarioRuntime] Scenario '{graph.Identifier}' could not start because requirements are unresolved.");
        yield break;
      }

      // The successful readiness result is already the canonical validation
      // result for this start request.  Do not re-enter via Off mode, which
      // would invoke the legacy preflight and could reverse that decision.
      StartScenarioInternal(graph, startNodeIdentifier, ownerClientId, true);
    }

    /// <summary>
    /// 시나리오 종료
    /// </summary>
    public void EndScenario()
    {
      // 로그 기록을 위해 그래프 ID를 먼저 캡처 (_currentGraph는 이후 null로 초기화됨)
      string endingGraphId = _currentGraph?.Identifier;
      CancelDialogueAutoAdvance();

      // 아직 진행 중인 시나리오 코루틴(특히 WaitMode.None 으로 전역 시나리오보다 오래
      // 살아남는 병렬 브랜치)을 모두 정리한다. 이를 누락하면 그래프가 해제된 뒤에도
      // 브랜치 체인이 계속 돌면서 _currentGraph 역참조에서 NullReferenceException 이 발생한다.
      StopAllCoroutines();
      ScenarioInteractionSignals.ClearAllInternalSignals();

      // 시나리오가 남긴 모든 타이머/표시를 정리한다.
      // 명시적 정리 없이 종료(또는 조기/오류 종료)하더라도 다음 시나리오로 새어 나가지 않게 한다.
      ScenarioTimeRelay.ClearAllAuthoritative();

      _currentGraph = null;
      _currentNode = null;
      _state = State.Inactive;
      // 강제 종료된 브랜치 코루틴의 finally 가 실행되지 않을 수 있으므로 억제 카운터를 초기화한다.
      _globalAdvanceSuppressionDepth = 0;
      // 브랜치 Choice/Quiz 대기 중 종료된 경우 남은 인터셉터/프롬프트 상태를 정리한다.
      // (StopAllCoroutines 로 강제 종료된 프롬프트 코루틴의 finally 가 실행되지 않을 수 있음)
      _branchOptionInterceptor = null;
      _branchPromptActive = false;

      ClearOptions();
      _stateStore.Clear();

      // UI 종료
      if (!_uiController.IsUnityNull())
      {
        _uiController.EndScenario();
      }

      // 시나리오가 진행되는 동안 힌트 UI 가 Dialogue 모드로 캐시만 갱신하고 화면에 반영하지
      // 않았거나, Dialogue 모드로 전환된 적이 없어 복원 대상 캐시가 실제 근처 상황과 어긋날 수
      // 있다. 종료 직후 로컬 플레이어에게 근처 Interactable 을 다시 인식(재갱신)시켜, 시나리오가
      // 끝났을 때 월드 상호작용 힌트가 확실히 현재 상태로 복구되도록 한다.
      var localPlayer = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      localPlayer?.RefreshInteractableHintsNow();

      OnScenarioEnded?.Invoke();
      try
      {
        GameLogService.WriteScenario(
          $"Scenario ended: graph={endingGraphId ?? "unknown"}",
          endingGraphId);
      }
      catch { /* 로그 실패는 시나리오 종료에 영향 없음 */ }
    }

    /// <summary>
    /// 다음 노드로 진행
    /// </summary>
    public void Advance()
    {
      // 브랜치 체인이 노드를 실행하는 동안에는 전역 진행을 무시한다.
      // 브랜치는 NextIdentifier 로 직접 이동하므로, 브랜치 노드 실행기가 호출한
      // Advance 가 전역 _currentNode 를 끌고 가서 시나리오를 조기 종료시키는 것을 막는다.
      if (_globalAdvanceSuppressionDepth > 0)
      {
        return;
      }

      // 진행되는 즉시 대기 중인 Dialogue 자동 진행 타이머를 취소하여 중복 진행을 막는다.
      CancelDialogueAutoAdvance();

      if (_currentNode == null || string.IsNullOrEmpty(_currentNode.NextIdentifier))
      {
        EndScenario();
        return;
      }

      if (!_currentGraph.TryGetNode(_currentNode.NextIdentifier, out var nextNode))
      {
        Debug.LogError($"[ScenarioController] Next node '{_currentNode.NextIdentifier}' not found");
        EndScenario();
        return;
      }

      _currentNode = nextNode;
      ExecuteNode(nextNode);
    }

    /// <summary>
    /// 선택지 선택
    /// </summary>
    public void SelectOption(int index)
    {
      if (index < 0 || index >= _activeOptions.Count)
      {
        Debug.LogWarning($"[ScenarioController] Invalid option index: {index}");
        return;
      }

      // 브랜치 체인에서 실행 중인 Choice/Quiz 는 전역 커서를 움직이지 않고
      // 인터셉터로 선택 결과만 전달한다.
      if (_branchOptionInterceptor != null)
      {
        var interceptor = _branchOptionInterceptor;
        _branchOptionInterceptor = null;
        interceptor(index);
        return;
      }

      if (_state == State.ExecutingQuiz && _activeQuizNode != null)
      {
        HandleQuizSelection(index, _activeQuizNode);
        return;
      }

      var option = _activeOptions[index];
      OnOptionSelected?.Invoke(option);
      try
      {
        GameLogService.WriteScenario(
          $"Option selected: index={index}, text='{option.DisplayText}', nextNode='{option.NextNodeIdentifier}', graph={_currentGraph?.Identifier}",
          _currentGraph?.Identifier);
      }
      catch { /* 로그 실패는 선택지 처리에 영향 없음 */ }

      ClearOptions();

      if (!string.IsNullOrEmpty(option.NextNodeIdentifier))
      {
        if (!_currentGraph.TryGetNode(option.NextNodeIdentifier, out var nextNode))
        {
          Debug.LogError($"[ScenarioController] Next node '{option.NextNodeIdentifier}' not found");
          EndScenario();
          return;
        }

        _currentNode = nextNode;
        ExecuteNode(nextNode);
      }
      else
      {
        EndScenario();
      }
    }

    #endregion

    #region Event Handlers

    private void HandleScenarioRequested(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
    {
      if (IsActive)
      {
        Debug.LogWarning("[ScenarioController] Scenario already active, ignoring request");
        return;
      }

      StartScenario(graph, startNodeIdentifier, ownerClientId);
    }

    #endregion

    #region Node Execution

    private void ExecuteNode(IScenarioNode node)
    {
      RecordNodeVisit(node);
      LogNodeExecution(node);
      try
      {
        GameLogService.WriteScenario(
          $"Node executed: id='{node?.Identifier}', type={node?.NodeType}, graph={_currentGraph?.Identifier}",
          _currentGraph?.Identifier);
      }
      catch { /* 로그 실패는 노드 실행에 영향 없음 */ }
      OnNodeChanged?.Invoke(node);

      switch (node)
      {
        case ScenarioDialogueNode dialogue:
          ExecuteDialogueNode(dialogue);
          break;
        case ScenarioDisinteractableDialogueNode dialogue:
          StartCoroutine(ExecuteDisinteractableDialogueNode(dialogue));
          break;
        case ScenarioChoiceNode choice:
          ExecuteChoiceNode(choice);
          break;
        case ScenarioSoundNode sound:
          StartCoroutine(ExecuteSoundNode(sound));
          break;
        case ScenarioPlayerMoveNode move:
          StartCoroutine(ExecutePlayerMoveNode(move));
          break;
        case ScenarioNPCMoveNode npcMove:
          StartCoroutine(ExecuteNPCMoveNode(npcMove));
          break;
        case ScenarioCameraTargetNode camera:
          StartCoroutine(ExecuteCameraTargetNode(camera));
          break;
        case ScenarioInvokeEventNode invoke:
          StartCoroutine(ExecuteInvokeEventNode(invoke));
          break;
        case ScenarioServerInternalSignalNode internalSignal:
          StartCoroutine(ExecuteServerInternalSignalNode(internalSignal));
          break;
        case ScenarioValidatorNode validator:
          StartCoroutine(ExecuteValidatorNode(validator));
          break;
        case ScenarioParallelNode parallel:
          StartCoroutine(ExecuteParallelNode(parallel));
          break;
        case ScenarioQuestControlNode questControl:
          ExecuteQuestControlNode(questControl);
          break;
        case ScenarioQuestWaypointHighlightNode waypointHighlight:
          ExecuteQuestWaypointHighlightNode(waypointHighlight);
          break;
        case ScenarioDelayNode delay:
          StartCoroutine(ExecuteDelayNode(delay));
          break;
        case ScenarioInteractionNode interaction:
          StartCoroutine(ExecuteInteractionNode(interaction));
          break;
        case ScenarioCombineItemNode combineItem:
          StartCoroutine(ExecuteCombineItemNode(combineItem));
          break;
        case ScenarioQuizNode quiz:
          ExecuteQuizNode(quiz);
          break;
        case ScenarioStateUpdateNode stateUpdate:
          ExecuteStateUpdateNode(stateUpdate);
          break;
        case ScenarioPlayTTSNode playTTS:
          StartCoroutine(ExecutePlayTTSNode(playTTS));
          break;
        case ScenarioPlayerTagNode playerTag:
          ExecutePlayerTagNode(playerTag);
          break;
        case ScenarioEntityPresetSpawnNode entityPresetSpawn:
          ExecuteEntityPresetSpawnNode(entityPresetSpawn);
          break;
        case ScenarioEntityTagNode entityTag:
          ExecuteEntityTagNode(entityTag);
          break;
        case ScenarioEntityInitNode entityInit:
          ExecuteEntityInitNode(entityInit);
          break;
        case ScenarioTriageAssessControlNode triageAssess:
          ExecuteTriageAssessControlNode(triageAssess);
          break;
        case ScenarioPatientMedicalStatePresetNode patientPreset:
          StartCoroutine(ExecutePatientMedicalStatePresetNode(patientPreset));
          break;
        case ScenarioItemSubmissionConfigNode itemSubmission:
          ExecuteItemSubmissionConfigNode(itemSubmission);
          break;
        case ScenarioNpcInteractControlNode npcInteractControl:
          ExecuteNpcInteractControlNode(npcInteractControl);
          break;
        case ScenarioChatPrintNode chatPrint:
          ExecuteChatPrintNode(chatPrint);
          break;
        case ScenarioExecuteCommandNode executeCommand:
          ExecuteExecuteCommandNode(executeCommand);
          break;
        case ScenarioTimeControlNode timeControl:
          ExecuteTimeControlNode(timeControl);
          break;
        default:
          Debug.LogWarning($"[ScenarioController] Unsupported node type: {node.GetType().Name}");
          Advance();
          break;
      }
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    private void LogNodeExecution(IScenarioNode node)
    {
      if (node == null)
      {
        Debug.LogWarning("[ScenarioController] ExecuteNode called with null node.");
        return;
      }

      int? localClientId = null;
      var localConn = InstanceFinder.ClientManager?.Connection;
      if (localConn != null)
      {
        localClientId = (int)localConn.ClientId;
      }

      Debug.Log(
        $"[ScenarioController] Executing node: id='{node.Identifier}', type={node.NodeType}, " +
        $"ownerClientId={_scenarioOwnerClientId?.ToString() ?? "null"}, localClientId={localClientId?.ToString() ?? "null"}");
    }

    private void ResetNodeVisitOrders()
    {
      _graphVisitHistory.Clear();
    }

    private void ResetNodeVisitOrders(string graphIdentifier)
    {
      if (string.IsNullOrWhiteSpace(graphIdentifier))
      {
        return;
      }

      _graphVisitHistory[graphIdentifier] = new GraphVisitHistory();
    }

    private void RecordNodeVisit(IScenarioNode node)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.Identifier))
      {
        return;
      }

      if (_currentGraph == null || string.IsNullOrWhiteSpace(_currentGraph.Identifier))
      {
        return;
      }

      if (!_graphVisitHistory.TryGetValue(_currentGraph.Identifier, out var history) || history == null)
      {
        history = new GraphVisitHistory();
        _graphVisitHistory[_currentGraph.Identifier] = history;
      }

      history.Sequence++;
      if (!history.NodeVisitOrders.TryGetValue(node.Identifier, out var orders) || orders == null)
      {
        orders = new List<int>();
        history.NodeVisitOrders[node.Identifier] = orders;
      }

      orders.Insert(0, history.Sequence);
    }

    /// <summary>
    /// 대화창 계열 UI(Dialogue/Choice/Quiz) 점유를 시도한다. 이미 다른 흐름이 점유 중이거나,
    /// 같은 그래프 안에서 이미 프롬프트가 표시 중(브랜치 동시 프롬프트)이면 충돌로 보고
    /// <see cref="_concurrencyConflictPolicy"/> 를 적용한다.
    /// 반환값 true = 점유 성공(계속 진행), false = 이 흐름/노드는 취소되어야 함.
    /// </summary>
    private bool TryClaimDialogueUI()
    {
      if (_uiController.IsUnityNull())
        return true; // 대화창 UI 가 없으면 동시 점유 개념 자체가 없다.

      // 브랜치 내부 프롬프트는 RunBranchPrompt 가 _branchPromptActive 로 같은 그래프 내
      // 동시 프롬프트를 이미 직렬화하므로, 그 경우 _branchPromptActive 를 충돌로 보지 않는다.
      // 전역(비브랜치) 경로에서는 _branchPromptActive 가 곧 "다른 브랜치 흐름이 점유 중"을 뜻한다.
      return TryClaimDialogueUI(considerBranchPrompt: true);
    }

    /// <summary>
    /// <see cref="TryClaimDialogueUI()"/> 의 코어. <paramref name="considerBranchPrompt"/> 가 true 면
    /// 같은 그래프 안에서 이미 프롬프트가 표시 중인 것도 충돌로 취급한다(전역 경로용).
    /// 브랜치 경로는 자체 직렬화가 있으므로 false 로 호출해 교차 그래프 점유만 검사한다.
    /// </summary>
    private bool TryClaimDialogueUI(bool considerBranchPrompt)
    {
      if (_uiController.IsUnityNull())
        return true;

      string owner = _currentGraph != null ? _currentGraph.Identifier : "(unknown)";

      // 충돌 조건: (1) 다른 그래프/흐름이 이미 점유 중이거나,
      //           (2) (전역 경로 한정) 같은 그래프 안에서 이미 프롬프트가 표시 중.
      bool conflict = _uiController.IsDialogueOwnedByOther(owner)
                   || (considerBranchPrompt && _branchPromptActive);
      if (!conflict)
      {
        _uiController.MarkDialogueOwner(owner);
        return true;
      }

      string existingOwner = _uiController.CurrentDialogueOwner ?? owner;
      string msg = $"[ScenarioController] 대화창 UI 동시 점유 충돌: '{owner}' 가 "
                 + $"'{existingOwner}' 점유 중 대화창을 요청함.";

      switch (_concurrencyConflictPolicy)
      {
        case ScenarioConcurrencyConflictPolicy.Warn:
          // 경고 후 undefined behavior(기존처럼 덮어쓰며 그대로 진행).
          Debug.LogWarning(msg + " (WARN: 경고 후 계속 진행)");
          AppendSystemChatMessage(msg + " (WARN)");
          _uiController.MarkDialogueOwner(owner);
          return true;

        case ScenarioConcurrencyConflictPolicy.Cancel:
          // 뒤에 요청한 흐름만 취소. 먼저 점유한 흐름은 보존.
          Debug.LogWarning(msg + " (CANCEL: 뒤에 요청한 흐름 취소)");
          AppendSystemChatMessage(msg + " (CANCEL)");
          return false;

        case ScenarioConcurrencyConflictPolicy.Panic:
          // 진행 중인 모든 시나리오를 안전 종료.
          Debug.LogError(msg + " (PANIC: 전체 시나리오 중단)");
          AppendSystemChatMessage(msg + " (PANIC)");
          EndScenario();
          return false;
      }

      return true;
    }

    private void ExecuteDialogueNode(ScenarioDialogueNode node)
    {
      _state = State.ExecutingDialogue;

      // 이전 노드의 잔여 타이머가 있다면 정리.
      CancelDialogueAutoAdvance();

      // 대화창 점유 충돌 검사(정책 적용). Cancel/Panic 이면 여기서 중단.
      if (!_uiController.IsUnityNull() && !TryClaimDialogueUI())
        return;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayDialogue(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier, node.InteractionRequired);

        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, node.DialogueContent, node.TtsVoiceIdentifier);

        // AutoAdvanceSeconds 가 양수이면 표시 후 해당 시간 경과 시 자동 진행.
        // 그 전에 사용자가 Advance() 를 호출하면 타이머는 취소된다(중복 진행 방지).
        if (node.AutoAdvanceSeconds.HasValue && node.AutoAdvanceSeconds.Value > 0f)
        {
          _dialogueAutoAdvanceRoutine = StartCoroutine(DialogueAutoAdvanceRoutine(node.AutoAdvanceSeconds.Value, node.InteractionRequired));
        }
      }
      else
      {
        // UI 없으면 바로 진행
        Advance();
      }
    }

    private IEnumerator DialogueAutoAdvanceRoutine(float seconds, bool interactionRequired)
    {
      yield return new WaitForSeconds(seconds);
      _dialogueAutoAdvanceRoutine = null;
      // 타이머 만료 시에만 자동 진행. (사용자 입력으로 이미 진행되었다면 CancelDialogueAutoAdvance 로 취소됨)
      if (_state == State.ExecutingDialogue)
      {
        if (interactionRequired)
          yield break;

        Advance();
      }
    }

    private IEnumerator ExecuteDisinteractableDialogueNode(ScenarioDisinteractableDialogueNode node)
    {
      _state = State.ExecutingDisinteractableDialogue;

      if (!_uiController.IsUnityNull() && !TryClaimDialogueUI())
      {
        if (_currentGraph != null)
          Advance();
        yield break;
      }

      double fadeInSeconds = node.FadeInDuration.ToSeconds();
      double displaySeconds = node.DisplayDuration.ToSeconds();
      double fadeOutSeconds = node.FadeOutDuration.ToSeconds();

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayDisinteractableDialogue(
          node.SpeakerName,
          node.DialogueContent,
          node.PortraitSpriteIdentifier);
        yield return FadeDisinteractableDialogue(0f, 1f, fadeInSeconds);
      }
      else
      {
        yield return WaitRealtime(fadeInSeconds);
      }

      yield return WaitRealtime(displaySeconds);

      if (!_uiController.IsUnityNull())
      {
        yield return FadeDisinteractableDialogue(1f, 0f, fadeOutSeconds);
        _uiController.HideDisinteractableDialogue();
      }
      else
      {
        yield return WaitRealtime(fadeOutSeconds);
      }

      Advance();
    }

    private IEnumerator FadeDisinteractableDialogue(float from, float to, double durationSeconds)
    {
      if (durationSeconds <= 0d)
      {
        _uiController.SetDisinteractableDialogueOpacity(to);
        yield break;
      }

      double elapsed = 0d;
      while (elapsed < durationSeconds)
      {
        elapsed += Time.unscaledDeltaTime;
        _uiController.SetDisinteractableDialogueOpacity(
          Mathf.Lerp(from, to, (float)Math.Min(1d, elapsed / durationSeconds)));
        yield return null;
      }
    }

    private static IEnumerator WaitRealtime(double seconds)
    {
      if (seconds <= 0d)
        yield break;

      double end = Time.realtimeSinceStartupAsDouble + seconds;
      while (Time.realtimeSinceStartupAsDouble < end)
        yield return null;
    }

    private void CancelDialogueAutoAdvance()
    {
      if (_dialogueAutoAdvanceRoutine != null)
      {
        StopCoroutine(_dialogueAutoAdvanceRoutine);
        _dialogueAutoAdvanceRoutine = null;
      }
    }

    private void ExecuteChoiceNode(ScenarioChoiceNode node)
    {
      // 대화창 점유 충돌 검사(정책 적용). Cancel/Panic 이면 여기서 중단.
      if (!_uiController.IsUnityNull() && !TryClaimDialogueUI())
        return;

      _state = State.ExecutingChoice;
      PresentChoice(node);
    }

    /// <summary>
    /// Choice 노드의 UI 표시/TTS/_activeOptions 설정 공용 루틴.
    /// 전역 경로(<see cref="ExecuteChoiceNode"/>)와 브랜치 경로가 함께 사용한다.
    /// </summary>
    private void PresentChoice(ScenarioChoiceNode node)
    {
      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayChoice(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier, node.Options);

        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, node.DialogueContent, node.TtsVoiceIdentifier);
      }

      _activeOptions = new List<ScenarioChoiceOption>(node.Options);
    }

    private IEnumerator ExecuteSoundNode(ScenarioSoundNode node)
    {
      _state = State.ExecutingSound;

      var clip = LoadSoundClip(node.SoundResourceIdentifier);

      if (clip == null)
      {
        Debug.LogWarning($"[ScenarioController] Sound clip '{node.SoundResourceIdentifier}' not found in Resources/Sound. Skipping.");
        Advance();
        yield break;
      }

      // 효과음은 TTS용 AudioSource 를 재사용한다(전용 SFX 소스가 없을 경우 PlayClipAtPoint 폴백).
      if (_ttsAudioSource != null)
      {
        _ttsAudioSource.PlayOneShot(clip);
      }
      else
      {
        AudioSource.PlayClipAtPoint(clip, UnityEngine.Camera.main != null ? UnityEngine.Camera.main.transform.position : Vector3.zero);
      }

      if (node.WaitUntilFinished)
      {
        // 실제 클립 길이만큼 대기.
        yield return new WaitForSeconds(clip.length);
      }

      Advance();
    }

    /// <summary>
    /// 사운드 식별자를 Resources 에서 로드한다.
    /// 우선 등록된 사운드 레지스트리(RegistryType.RuntimeState 가 아닌 별도 경로가 없으므로)
    /// `Resources/Sound/&lt;id&gt;` 를 시도하고, 실패 시 `Resources/&lt;id&gt;` 를 시도한다.
    /// </summary>
    private static AudioClip LoadSoundClip(string soundResourceIdentifier)
    {
      if (string.IsNullOrWhiteSpace(soundResourceIdentifier))
      {
        return null;
      }

      var clip = Resources.Load<AudioClip>($"Sound/{soundResourceIdentifier}");
      if (clip == null)
      {
        clip = Resources.Load<AudioClip>(soundResourceIdentifier);
      }

      return clip;
    }

    private void ExecuteQuestControlNode(ScenarioQuestControlNode node)
    {
      _state = State.ExecutingQuestControl;

      if (!ShouldApplyQuestControlNode(node))
      {
        Advance();
        return;
      }

      var manager = Registry.Registry.Get<QuestManager>(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());
      if (manager == null)
      {
        Debug.LogWarning("[ScenarioController] QuestManager not found; skipping quest control node.");
        Advance();
        return;
      }

      bool success = ApplyQuestOperation(manager, node);
      if (!success && node.FailureStrategy == ScenarioQuestFailureStrategy.Panic)
      {
        Debug.LogError($"[ScenarioController] Quest control node failed with panic strategy (questId: {node.Quest?.Id}). Ending scenario.");
        EndScenario();
        return;
      }

      Advance();
    }

    private bool ShouldApplyQuestControlNode(ScenarioQuestControlNode node)
    {
      if (node == null)
        return false;

      // 시나리오 owner가 지정된 경우, 퀘스트 노드는 해당 owner 클라이언트에서만 적용한다.
      // 그렇지 않으면 모든 피어에서 동일 퀘스트가 동시에 등록되어 역할별 분기가 깨질 수 있다.
      if (!_scenarioOwnerClientId.HasValue)
      {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ScenarioController] QuestControl '{node.Identifier}' apply=true (ownerClientId is null)");
#endif
        return true;
      }

      var localConn = InstanceFinder.ClientManager?.Connection;
      if (localConn == null)
      {
        // 서버 전용 컨텍스트(로컬 클라이언트 없음)에서는 클라이언트 전용 퀘스트 적용을 건너뛴다.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ScenarioController] QuestControl '{node.Identifier}' apply=false (local connection is null, ownerClientId={_scenarioOwnerClientId})");
#endif
        return false;
      }

      bool apply = localConn.ClientId == _scenarioOwnerClientId.Value;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
      Debug.Log($"[ScenarioController] QuestControl '{node.Identifier}' apply={apply} (ownerClientId={_scenarioOwnerClientId}, localClientId={localConn.ClientId})");
#endif
      return apply;
    }

    private void ExecuteQuestWaypointHighlightNode(ScenarioQuestWaypointHighlightNode node)
    {
      _state = State.ExecutingQuestWaypointHighlight;

      if (string.IsNullOrWhiteSpace(node.WaypointIdentifier))
      {
        Debug.LogWarning("[ScenarioController] Quest waypoint highlight node has no waypoint identifier.");
        Advance();
        return;
      }

      if (WaypointAnchor.TryGet(node.WaypointIdentifier, out var anchor))
      {
        anchor.Highlight();
      }
      else
      {
        Debug.LogWarning($"[ScenarioController] Waypoint '{node.WaypointIdentifier}' not found for highlight node '{node.Identifier}'.");
      }

      Advance();
    }

    private IEnumerator ExecuteDelayNode(ScenarioDelayNode node)
    {
      _state = State.ExecutingDelay;

      if (node.WaitUntil == ScenarioDelayWaitUntil.WaitUntilDone && node.DurationSeconds > 0f)
      {
        yield return new WaitForSeconds(node.DurationSeconds);
      }

      Advance();
    }

    /// <summary>
    /// 시간 표시(스톱워치/카운트다운) HUD 를 제어한다. <see cref="ScenarioTimeControlNode.Operation"/> 에 따라
    /// 생성/흐름/표시/삭제를 각각 수행하며, 서버 권한으로 모든 클라이언트에 전파하고 즉시 다음 노드로 진행한다.
    /// 표시 자체는 <see cref="MultiplayerInfrastructure.UI.TimeDisplayUIController"/> 가
    /// <see cref="ScenarioTimeState"/> 를 매 프레임 조회해 렌더한다.
    /// </summary>
    private void ExecuteTimeControlNode(ScenarioTimeControlNode node)
    {
      _state = State.ExecutingTimeControl;

      switch (node.Operation)
      {
        case ScenarioTimeOperationType.Create:
        {
          double duration = Mathf.Max(0f, node.DurationSeconds);
          if (node.Direction == ScenarioTimeDirection.Countdown)
          {
            // 카운트다운: StartSeconds 는 시작 표시할 "남은 값". 미지정(0)이면 목표 시간에서 시작.
            double displayStart = node.StartSeconds > 0f ? node.StartSeconds : duration;
            // 목표(총) 시간은 최소한 시작 표시값 이상이어야 한다(Duration 미지정 시 StartSeconds 로 대체).
            double target = Mathf.Max((float)duration, (float)displayStart);
            ScenarioTimeRelay.CreateAuthoritative(node.TimerId, node.Direction, displayStart, target);
          }
          else
          {
            double start = Mathf.Max(0f, node.StartSeconds);
            ScenarioTimeRelay.CreateAuthoritative(node.TimerId, node.Direction, start, duration);
          }
          break;
        }
        case ScenarioTimeOperationType.Start:
          ScenarioTimeRelay.StartAuthoritative(node.TimerId);
          break;
        case ScenarioTimeOperationType.Pause:
          ScenarioTimeRelay.PauseAuthoritative(node.TimerId);
          break;
        case ScenarioTimeOperationType.Resume:
          ScenarioTimeRelay.ResumeAuthoritative(node.TimerId);
          break;
        case ScenarioTimeOperationType.Stop:
          ScenarioTimeRelay.StopAuthoritative(node.TimerId);
          break;
        case ScenarioTimeOperationType.Set:
        {
          double displaySeconds = Mathf.Max(0f, node.StartSeconds);
          // DurationSeconds 가 양수이면 카운트다운 목표(총) 시간도 재설정한다.
          bool hasNewTarget = node.DurationSeconds > 0f;
          ScenarioTimeRelay.SetAuthoritative(node.TimerId, displaySeconds, hasNewTarget, Mathf.Max(0f, node.DurationSeconds));
          break;
        }
        case ScenarioTimeOperationType.Show:
          ScenarioTimeRelay.ShowAuthoritative(node.TimerId);
          break;
        case ScenarioTimeOperationType.Hide:
          ScenarioTimeRelay.HideAuthoritative();
          break;
        case ScenarioTimeOperationType.Remove:
          ScenarioTimeRelay.RemoveAuthoritative(node.TimerId);
          break;
      }

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] TimeControl '{node.Identifier}': operation={node.Operation}, " +
                $"timerId={node.TimerId}, direction={node.Direction}, duration={node.DurationSeconds}, start={node.StartSeconds}");
#endif

      Advance();
    }

    private IEnumerator ExecuteInteractionNode(ScenarioInteractionNode node)
    {
      _state = State.ExecutingInteraction;

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Interaction requested: actorScope={node.ActorScope}, target={node.TargetIdentifier}, item={node.RequiredItemIdentifier}, type={node.InteractionType}");
#endif

      if (!string.IsNullOrWhiteSpace(node.CompletionConditionIdentifier)
          && ScenarioEventIdentifierRegistry.TryGetHandler(node.CompletionConditionIdentifier, out var handler))
      {
        var routine = handler?.Invoke();
        if (routine != null)
        {
          yield return StartCoroutine(routine);
        }
      }

      Advance();
    }

    private IEnumerator ExecuteCombineItemNode(ScenarioCombineItemNode node)
    {
      _state = State.ExecutingCombineItem;

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Combine item: inputs={string.Join(",", node.InputItemIdentifiers ?? Array.Empty<string>())}, output={node.OutputItemIdentifier}, auto={node.AutoCombine}");
#endif

      if (!node.AutoCombine && !string.IsNullOrWhiteSpace(node.OutputItemIdentifier)
          && ScenarioEventIdentifierRegistry.TryGetHandler(node.OutputItemIdentifier, out var handler))
      {
        var routine = handler?.Invoke();
        if (routine != null)
        {
          yield return StartCoroutine(routine);
        }
      }

      Advance();
    }

    private void ExecuteQuizNode(ScenarioQuizNode node)
    {
      // 대화창 점유 충돌 검사(정책 적용). Cancel/Panic 이면 여기서 중단.
      if (!_uiController.IsUnityNull() && !TryClaimDialogueUI())
        return;

      _state = State.ExecutingQuiz;
      _activeQuizNode = node;

      if (!_uiController.IsUnityNull())
      {
        PresentQuiz(node);
      }
      else
      {
        Debug.LogWarning($"[ScenarioController] Quiz node '{node.Identifier}' cannot render choices because UI controller is missing.");
        ResolveQuizNext(node, false);
      }
    }

    /// <summary>
    /// Quiz 노드의 옵션 구성/UI 표시/TTS/_activeOptions 설정 공용 루틴.
    /// 전역 경로(<see cref="ExecuteQuizNode"/>)와 브랜치 경로가 함께 사용한다.
    /// </summary>
    private void PresentQuiz(ScenarioQuizNode node)
    {
      var options = (node.Options ?? Array.Empty<string>())
          .Select(text => new ScenarioChoiceOption
          {
            DisplayText = text,
            DisplayColor = Color.white,
            NextNodeIdentifier = null
          })
          .ToList();

      _activeOptions = options;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayChoice("Quiz", node.Question ?? string.Empty, null, options);

        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, node.Question, node.TtsVoiceIdentifier);
      }
    }

    /// <summary>
    /// 퀴즈 정답/오답 피드백을 표시한다(전역/브랜치 공용).
    /// </summary>
    private void ShowQuizFeedback(ScenarioQuizNode node, bool isCorrect)
    {
      var feedback = isCorrect ? node.FeedbackCorrect : node.FeedbackIncorrect;
      if (string.IsNullOrWhiteSpace(feedback) || _uiController.IsUnityNull())
      {
        return;
      }

      _uiController.DisplayDialogue("Quiz", feedback, null);

      if (node.PlayTTS)
      {
        string feedbackNodeId = node.Identifier + (isCorrect ? "_feedbackCorrect" : "_feedbackIncorrect");
        PlayInlineTTS(feedbackNodeId, feedback, node.TtsVoiceIdentifier);
      }
    }

    /// <summary>
    /// 퀴즈 정답/오답에 따른 다음 노드 식별자를 결정한다(전역/브랜치 공용).
    /// 전용 식별자가 비어 있으면 공통 NextIdentifier 로 폴백한다.
    /// </summary>
    private static string ResolveQuizTarget(ScenarioQuizNode node, bool isCorrect)
    {
      return isCorrect
          ? string.IsNullOrWhiteSpace(node.OnCorrectNextIdentifier) ? node.NextIdentifier : node.OnCorrectNextIdentifier
          : string.IsNullOrWhiteSpace(node.OnIncorrectNextIdentifier) ? node.NextIdentifier : node.OnIncorrectNextIdentifier;
    }

    private void ExecuteStateUpdateNode(ScenarioStateUpdateNode node)
    {
      _state = State.ExecutingStateUpdate;

      var key = string.IsNullOrWhiteSpace(node.TargetEntityIdentifier)
        ? node.StateKey
        : $"{node.TargetEntityIdentifier}.{node.StateKey}";
      _stateStore[key] = node.StateValue;
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] State updated: {key}={node.StateValue}");
#endif

      Advance();
    }

    private void ExecutePlayerTagNode(ScenarioPlayerTagNode node)
    {
      _state = State.ExecutingPlayerTag;

      // Swap 은 두 태그 그룹 간 교환이므로 대상-세션 루프와 별개로 처리한다.
      if (node.Operation == ScenarioPlayerTagOperationType.Swap)
      {
        ExecutePlayerTagSwap(node);
        Advance();
        return;
      }

      // 대상 세션 수집
      var targets = new List<UserDescriptor>();

      if (node.Scope == ScenarioPlayerTagScope.All)
      {
        foreach (var kvp in UserDescriptorService.GetAll())
          targets.Add(kvp.Value);
      }
      else if (node.Scope == ScenarioPlayerTagScope.ByTag)
      {
        var byTag = node.Tag?.Trim();
        if (string.IsNullOrWhiteSpace(byTag))
        {
          Debug.LogWarning($"[ScenarioController] PlayerTag node '{node.Identifier}': " +
                           "Scope=ByTag 이지만 Tag 가 비어 있습니다. 노드를 건너뜁니다.");
          Advance();
          return;
        }

        foreach (var kvp in UserDescriptorService.GetAll())
        {
          var session = kvp.Value;
          if (session != null && !string.IsNullOrWhiteSpace(session.Identifier)
              && PlayerTagService.HasTag(session.Identifier, byTag))
          {
            targets.Add(session);
          }
        }
      }
      else // Current
      {
        if (_scenarioOwnerClientId.HasValue &&
            UserDescriptorService.TryGetByClientId(_scenarioOwnerClientId.Value, out var ownerSession))
        {
          targets.Add(ownerSession);
        }
        else
        {
          Debug.LogWarning($"[ScenarioController] PlayerTag node '{node.Identifier}': " +
                           "Scope=Current 이지만 scenarioOwner 세션을 찾을 수 없습니다. 노드를 건너뜁니다.");
          Advance();
          return;
        }
      }

      // 태그 조작 수행
      foreach (var session in targets)
      {
        switch (node.Operation)
        {
          case ScenarioPlayerTagOperationType.Add:
            PlayerTagService.AddTag(session.Identifier, node.Tag);
#if UNITY_EDITOR
            Debug.Log($"[ScenarioController] Tag Add: player={session.DisplayName} tag={node.Tag}");
#endif
            break;

          case ScenarioPlayerTagOperationType.Remove:
            PlayerTagService.RemoveTag(session.Identifier, node.Tag);
#if UNITY_EDITOR
            Debug.Log($"[ScenarioController] Tag Remove: player={session.DisplayName} tag={node.Tag}");
#endif
            break;

          case ScenarioPlayerTagOperationType.Change:
            bool changed = PlayerTagService.ChangeTag(session.Identifier, node.FromTag, node.ToTag);
            if (!changed)
            {
              Debug.LogWarning($"[ScenarioController] Tag Change: player={session.DisplayName} " +
                               $"fromTag='{node.FromTag}' 이(가) 없어 변경하지 못했습니다.");
            }
#if UNITY_EDITOR
            else
            {
              Debug.Log($"[ScenarioController] Tag Change: player={session.DisplayName} " +
                        $"{node.FromTag} → {node.ToTag}");
            }
#endif
            break;
        }
      }

      Advance();
    }

    /// <summary>
    /// SwapTagA 보유 플레이어와 SwapTagB 보유 플레이어의 해당 태그를 서로 교환한다(역할 교대).
    /// 1:1 매칭을 가정하며, 한쪽 보유자가 없거나 2인 이상이면 경고 후 안전 스킵한다.
    /// </summary>
    private void ExecutePlayerTagSwap(ScenarioPlayerTagNode node)
    {
      var tagA = node.SwapTagA?.Trim();
      var tagB = node.SwapTagB?.Trim();

      if (string.IsNullOrWhiteSpace(tagA) || string.IsNullOrWhiteSpace(tagB))
      {
        Debug.LogWarning($"[ScenarioController] PlayerTag Swap '{node.Identifier}': swapTagA/swapTagB 가 비어 있어 건너뜁니다.");
        return;
      }

      var holdersA = new List<UserDescriptor>();
      var holdersB = new List<UserDescriptor>();
      foreach (var kvp in UserDescriptorService.GetAll())
      {
        var session = kvp.Value;
        if (session == null || string.IsNullOrWhiteSpace(session.Identifier))
        {
          continue;
        }

        if (PlayerTagService.HasTag(session.Identifier, tagA)) holdersA.Add(session);
        if (PlayerTagService.HasTag(session.Identifier, tagB)) holdersB.Add(session);
      }

      if (holdersA.Count != 1 || holdersB.Count != 1)
      {
        Debug.LogWarning($"[ScenarioController] PlayerTag Swap '{node.Identifier}': " +
                         $"1:1 매칭 실패(tagA='{tagA}' 보유 {holdersA.Count}명, tagB='{tagB}' 보유 {holdersB.Count}명). 교환을 건너뜁니다.");
        return;
      }

      var playerA = holdersA[0];
      var playerB = holdersB[0];

      // A 보유자는 tagA -> tagB, B 보유자는 tagB -> tagA 로 교체.
      PlayerTagService.ChangeTag(playerA.Identifier, tagA, tagB);
      PlayerTagService.ChangeTag(playerB.Identifier, tagB, tagA);

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] PlayerTag Swap: {playerA.DisplayName}({tagA}->{tagB}) <-> {playerB.DisplayName}({tagB}->{tagA})");
#endif
    }

    private void ExecuteEntityPresetSpawnNode(ScenarioEntityPresetSpawnNode node)
    {
      _state = State.ExecutingEntityPresetSpawn;

      if (node == null || string.IsNullOrWhiteSpace(node.PresetIdentifier))
      {
        Debug.LogWarning("[ScenarioController] EntityPresetSpawn node is missing presetIdentifier.");
        Advance();
        return;
      }

      Vector3 spawnPosition = new Vector3(node.PositionX, node.PositionY, node.PositionZ);
      if (!string.IsNullOrWhiteSpace(node.PositionSourceEntityIdentifier)
          && Registry.Registry.TryGetEntity(node.PositionSourceEntityIdentifier, out var sourceDescriptor)
          && sourceDescriptor?.GameObject != null)
      {
        spawnPosition = sourceDescriptor.GameObject.transform.position;
      }

      if (!Registry.Registry.TrySpawnEntityPreset(
            node.PresetIdentifier,
            spawnPosition,
            Quaternion.identity,
            node.SpawnedEntityIdentifier,
            out _,
            out var spawnedDescriptor,
            out var error))
      {
        Debug.LogWarning($"[ScenarioController] EntityPresetSpawn '{node.Identifier}' failed: {error}");
        Advance();
        return;
      }

      string stateKey = string.IsNullOrWhiteSpace(node.ResultStateKey)
        ? $"{node.Identifier}.spawnedEntityIdentifier"
        : node.ResultStateKey;

      // 네트워크 루트는 OnStartClient 에서 비동기 자가 등록하므로 스폰 직후 디스크립터가 아직 없을 수 있다.
      // 그 경우 노드에 지정된 식별자(있으면)를 결과로 저장한다.
      string spawnedIdentifier = spawnedDescriptor?.Identifier;
      if (string.IsNullOrWhiteSpace(spawnedIdentifier))
        spawnedIdentifier = node.SpawnedEntityIdentifier;
      _stateStore[stateKey] = spawnedIdentifier;

      Advance();
    }

    /// <summary>
    /// 아이템 제출 Interactable 을 사전 설정한다.
    /// 프리셋 스폰(서버 권한) 또는 기존 Interactable 참조 후, 요구 아이템/완료 신호/활성 상태를 오버라이드한다.
    /// </summary>
    private void ExecuteItemSubmissionConfigNode(ScenarioItemSubmissionConfigNode node)
    {
      _state = State.ExecutingItemSubmissionConfig;

      if (node == null)
      {
        Advance();
        return;
      }

      string resolvedIdentifier = null;

      if (!string.IsNullOrWhiteSpace(node.PresetIdentifier))
      {
        // 프리셋 스폰은 네트워크 엔티티일 수 있어 서버 권한이 필요하다.
        // 클라이언트에서는 서버가 스폰한 인스턴스가 동기화되어 Registry 에 등록되므로, 여기서는 스폰을 건너뛴다.
        // (오버라이드 설정은 인스턴스가 존재하는 서버 측에서 적용되며, IInteract 표시/CanInteract 는 클라이언트에서 평가된다.)
        if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsOffline)
        {
          Debug.Log($"[ScenarioController] ItemSubmissionConfig '{node.Identifier}': preset spawn skipped on client (server-authoritative).");
          Advance();
          return;
        }

        Vector3 spawnPosition = new Vector3(node.PositionX, node.PositionY, node.PositionZ);
        if (!string.IsNullOrWhiteSpace(node.PositionSourceEntityIdentifier)
            && Registry.Registry.TryGetEntity(node.PositionSourceEntityIdentifier, out var sourceDescriptor)
            && sourceDescriptor?.GameObject != null)
        {
          spawnPosition = sourceDescriptor.GameObject.transform.position;
        }

        if (!Registry.Registry.TrySpawnEntityPreset(
              node.PresetIdentifier,
              spawnPosition,
              Quaternion.identity,
              node.SpawnedEntityIdentifier,
              out _,
              out var spawnedDescriptor,
              out var error))
        {
          Debug.LogWarning($"[ScenarioController] ItemSubmissionConfig '{node.Identifier}' preset spawn failed: {error}");
          Advance();
          return;
        }

        resolvedIdentifier = spawnedDescriptor?.Identifier;
        if (string.IsNullOrWhiteSpace(resolvedIdentifier))
          resolvedIdentifier = node.SpawnedEntityIdentifier;
      }
      else
      {
        resolvedIdentifier = ResolveItemSubmissionTargetIdentifier(node);
      }

      if (string.IsNullOrWhiteSpace(resolvedIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] ItemSubmissionConfig '{node.Identifier}': target identifier is missing.");
        Advance();
        return;
      }

      var interactable = Registry.Registry.Get<ItemSubmissionInteractable>(RegistryType.InteractableEntity, resolvedIdentifier);
      if (interactable == null)
      {
        // 네트워크 스폰 직후에는 자가 등록이 비동기로 완료될 수 있어 즉시 조회되지 않을 수 있다.
        // 그 경우에도 노드 진행은 계속하고, 오버라이드는 인스턴스가 존재하는 컨텍스트에서만 적용된다.
        Debug.LogWarning($"[ScenarioController] ItemSubmissionConfig '{node.Identifier}': ItemSubmissionInteractable '{resolvedIdentifier}' not found (may spawn asynchronously).");
      }
      else
      {
        if (node.RequiredItems != null && node.RequiredItems.Count > 0)
        {
          var requirements = new List<ItemRequirement>();
          foreach (var req in node.RequiredItems)
          {
            if (req == null || string.IsNullOrWhiteSpace(req.ItemIdentifier))
              continue;
            requirements.Add(new ItemRequirement(req.ItemIdentifier, req.Count));
          }
          interactable.SetRequiredItems(requirements);
        }

        if (!string.IsNullOrWhiteSpace(node.CompletionSignalIdentifier))
          interactable.SetCompletionSignal(node.CompletionSignalIdentifier);

        interactable.SetEnabled(node.Enabled);
      }

      string stateKey = string.IsNullOrWhiteSpace(node.ResultStateKey)
        ? $"{node.Identifier}.submissionEntityIdentifier"
        : node.ResultStateKey;
      _stateStore[stateKey] = resolvedIdentifier;

      Advance();
    }

    private string ResolveItemSubmissionTargetIdentifier(ScenarioItemSubmissionConfigNode node)
    {
      if (node == null)
        return string.Empty;

      if (!string.IsNullOrWhiteSpace(node.TargetIdentifier))
        return node.TargetIdentifier;

      if (!string.IsNullOrWhiteSpace(node.TargetStateKey)
          && _stateStore.TryGetValue(node.TargetStateKey, out var value))
      {
        return value;
      }

      return string.Empty;
    }

    /// <summary>
    /// NPC 에 Interactable 을 추가/제거하거나 활성/비활성 전환한다.
    /// </summary>
    private void ExecuteNpcInteractControlNode(ScenarioNpcInteractControlNode node)
    {
      _state = State.ExecutingNpcInteractControl;

      if (node == null || string.IsNullOrWhiteSpace(node.NpcIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] NpcInteractControl '{node?.Identifier}': npcIdentifier is missing.");
        Advance();
        return;
      }

      var npcGo = Registry.Registry.Get<GameObject>(RegistryType.Npc, node.NpcIdentifier);
      var npc = npcGo != null ? npcGo.GetComponent<Entity.Npc>() : null;
      if (npc == null)
      {
        Debug.LogWarning($"[ScenarioController] NpcInteractControl '{node.Identifier}': NPC '{node.NpcIdentifier}' not found.");
        Advance();
        return;
      }

      // 대상 Interactable 컴포넌트를 식별자로 해석한다.
      // 1) InteractableEntity 저장소(ItemSubmissionInteractable 등 컴포넌트가 직접 등록됨)
      // 2) Entity 저장소(EntityDescriptor.GameObject 에서 IInteract 컴포넌트 탐색)
      MonoBehaviour interactableComponent = null;
      if (!string.IsNullOrWhiteSpace(node.InteractableIdentifier))
      {
        interactableComponent = Registry.Registry.Get<MonoBehaviour>(
          RegistryType.InteractableEntity, node.InteractableIdentifier);

        if (interactableComponent == null
            && Registry.Registry.TryGetEntity(node.InteractableIdentifier, out var interactableDescriptor)
            && interactableDescriptor?.GameObject != null)
        {
          var interact = interactableDescriptor.GameObject.GetComponentInChildren<IInteract>(true);
          interactableComponent = interact as MonoBehaviour;
        }
      }

      switch (node.Operation)
      {
        case ScenarioNpcInteractControlOperation.Add:
          if (interactableComponent == null)
          {
            Debug.LogWarning($"[ScenarioController] NpcInteractControl '{node.Identifier}': interactable '{node.InteractableIdentifier}' not found for Add.");
            break;
          }
          npc.AddCustomInteractSource(interactableComponent);
          break;

        case ScenarioNpcInteractControlOperation.Remove:
          if (interactableComponent != null)
            npc.RemoveCustomInteractSource(interactableComponent);
          break;

        case ScenarioNpcInteractControlOperation.Enable:
        case ScenarioNpcInteractControlOperation.Disable:
          if (interactableComponent is IInteractToggleable toggleable)
            toggleable.SetEnabled(node.Operation == ScenarioNpcInteractControlOperation.Enable);
          else
            Debug.LogWarning($"[ScenarioController] NpcInteractControl '{node.Identifier}': interactable '{node.InteractableIdentifier}' does not implement IInteractToggleable.");
          break;
      }

      // 상호작용 힌트를 갱신하여 변경이 즉시 반영되게 한다.
      var player = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      player?.RefreshInteractableHintsNow();

      Advance();
    }

    private void ExecuteEntityTagNode(ScenarioEntityTagNode node)
    {
      _state = State.ExecutingEntityTag;

      string targetIdentifier = ResolveEntityTagTargetIdentifier(node);
      if (string.IsNullOrWhiteSpace(targetIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] EntityTag '{node?.Identifier}' target identifier is missing.");
        Advance();
        return;
      }

      if (!Registry.Registry.TryGetEntity(targetIdentifier, out var entityDescriptor) || entityDescriptor?.GameObject == null)
      {
        Debug.LogWarning($"[ScenarioController] EntityTag '{node.Identifier}' target '{targetIdentifier}' was not found.");
        Advance();
        return;
      }

      switch (node.Operation)
      {
        case ScenarioPlayerTagOperationType.Add:
          PlayerTagService.AddTagToIdentifier(targetIdentifier, node.Tag);
          break;
        case ScenarioPlayerTagOperationType.Remove:
          PlayerTagService.RemoveTagFromIdentifier(targetIdentifier, node.Tag);
          break;
        case ScenarioPlayerTagOperationType.Change:
          PlayerTagService.ChangeTagForIdentifier(targetIdentifier, node.FromTag, node.ToTag);
          break;
      }

      Advance();
    }

    private string ResolveEntityTagTargetIdentifier(ScenarioEntityTagNode node)
    {
      if (node == null)
        return string.Empty;

      if (!string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        return node.TargetEntityIdentifier;

      if (string.IsNullOrWhiteSpace(node.TargetEntityStateKey))
        return string.Empty;

      return _stateStore.TryGetValue(node.TargetEntityStateKey, out var value)
        ? value
        : string.Empty;
    }

    /// <summary>
    /// 엔티티를 준비(프리셋 스폰 또는 기존 엔티티 참조)하고 초기 상태를 설정한다.
    /// 1차 목표는 환자 엔티티 부착물의 초기 표시 상태 설정이다.
    /// </summary>
    private void ExecuteEntityInitNode(ScenarioEntityInitNode node)
    {
      _state = State.ExecutingEntityInit;

      if (node == null)
      {
        Advance();
        return;
      }

      GameObject targetGameObject = null;
      string resolvedIdentifier = null;

      if (!string.IsNullOrWhiteSpace(node.PresetIdentifier))
      {
        // ── 프리셋 스폰 ──
        // 네트워크 엔티티 프리셋 스폰은 서버 권한이 필요하다. ScenarioController 는 클라이언트에서도
        // 실행되므로(ChatService.TargetRunScenario), 현재 컨텍스트가 서버가 아니면 스폰을 건너뛴다.
        // 클라이언트 측에서는 서버가 별도로 스폰한 엔티티가 동기화되어 레지스트리에 등록되므로,
        // 이 경로를 강제하는 대신 "기존 엔티티 참조" 경로로 InitState 만 적용하면 충분하다.
        if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsOffline)
        {
          Debug.Log($"[ScenarioController] EntityInit '{node.Identifier}': preset spawn skipped on client (server-authoritative). " +
                    $"Use targetEntityIdentifier to apply display state to an already-spawned entity on clients.");
          Advance();
          return;
        }

        Vector3 spawnPosition = new Vector3(node.PositionX, node.PositionY, node.PositionZ);
        if (!string.IsNullOrWhiteSpace(node.PositionSourceEntityIdentifier)
            && Registry.Registry.TryGetEntity(node.PositionSourceEntityIdentifier, out var sourceDescriptor)
            && sourceDescriptor?.GameObject != null)
        {
          spawnPosition = sourceDescriptor.GameObject.transform.position;
        }

        if (!Registry.Registry.TrySpawnEntityPreset(
              node.PresetIdentifier,
              spawnPosition,
              Quaternion.identity,
              node.EntityIdentifier,
              out var spawned,
              out var spawnedDescriptor,
              out var error))
        {
          Debug.LogWarning($"[ScenarioController] EntityInit '{node.Identifier}' preset spawn failed: {error}");
          Advance();
          return;
        }

        // 네트워크 루트는 OnStartClient 에서 비동기 자가 등록하므로 디스크립터가 아직 없을 수 있다.
        // 표시 상태 적용은 스폰된 GameObject 에서 직접 컴포넌트를 찾아 수행한다(레지스트리 등록과 무관).
        targetGameObject = spawned;
        resolvedIdentifier = spawnedDescriptor?.Identifier;
        if (string.IsNullOrWhiteSpace(resolvedIdentifier))
          resolvedIdentifier = node.EntityIdentifier;
      }
      else
      {
        // ── 기존 엔티티 참조 ──
        resolvedIdentifier = ResolveEntityInitTargetIdentifier(node);
        if (string.IsNullOrWhiteSpace(resolvedIdentifier))
        {
          Debug.LogWarning($"[ScenarioController] EntityInit '{node.Identifier}' target identifier is missing.");
          Advance();
          return;
        }

        if (!Registry.Registry.TryGetEntity(resolvedIdentifier, out var entityDescriptor)
            || entityDescriptor?.GameObject == null)
        {
          Debug.LogWarning($"[ScenarioController] EntityInit '{node.Identifier}' target '{resolvedIdentifier}' was not found.");
          Advance();
          return;
        }

        targetGameObject = entityDescriptor.GameObject;
      }

      // 확정된 식별자를 상태 저장소에 기록(후속 노드 참조용).
      // ResultStateKey 가 명시되지 않아도 기본 키로 기록하여, 후속 EntityTag/EntityInit 노드가
      // 이 노드의 식별자를 기반으로 엔티티를 참조할 수 있다(EntityPresetSpawn 동작과 일치).
      if (!string.IsNullOrWhiteSpace(resolvedIdentifier))
      {
        string storeKey = string.IsNullOrWhiteSpace(node.ResultStateKey)
          ? $"{node.Identifier}.entityIdentifier"
          : node.ResultStateKey;
        _stateStore[storeKey] = resolvedIdentifier;
      }

      ApplyEntityInitStateOperations(node, resolvedIdentifier, targetGameObject);

      Advance();
    }

    private string ResolveEntityInitTargetIdentifier(ScenarioEntityInitNode node)
    {
      if (node == null)
        return string.Empty;

      if (!string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
        return node.TargetEntityIdentifier;

      if (!string.IsNullOrWhiteSpace(node.EntityIdentifier))
        return node.EntityIdentifier;

      if (string.IsNullOrWhiteSpace(node.TargetEntityStateKey))
        return string.Empty;

      return _stateStore.TryGetValue(node.TargetEntityStateKey, out var value)
        ? value
        : string.Empty;
    }

    private void ApplyEntityInitStateOperations(ScenarioEntityInitNode node, string entityIdentifier, GameObject targetGameObject)
    {
      if (node.StateOperations == null || node.StateOperations.Count == 0)
        return;

      Entity.IScenarioEntityInitTarget displayTarget = null;
      if (targetGameObject != null)
        displayTarget = targetGameObject.GetComponentInChildren<Entity.IScenarioEntityInitTarget>(true);

      foreach (var op in node.StateOperations)
      {
        if (op == null)
          continue;

        switch (op.Kind)
        {
          case ScenarioEntityStateOperationKind.StateStore:
            if (string.IsNullOrWhiteSpace(op.Key))
              break;
            // 엔티티 식별자 접두로 기존 StateUpdate 노드와 동일한 네임스페이스를 따른다.
            string stateKey = string.IsNullOrWhiteSpace(entityIdentifier)
              ? op.Key
              : $"{entityIdentifier}.{op.Key}";
            _stateStore[stateKey] = op.Value;
            break;

          case ScenarioEntityStateOperationKind.DisplayState:
            if (string.IsNullOrWhiteSpace(op.Key))
              break;
            if (displayTarget == null)
            {
              Debug.LogWarning($"[ScenarioController] EntityInit '{node.Identifier}' target '{entityIdentifier}' has no IScenarioEntityInitTarget for display state '{op.Key}'.");
              break;
            }
            displayTarget.ApplyScenarioDisplayState(op.Key, op.DisplayActive);
            break;
        }
      }

      // DisplayState 항목이 하나 이상 적용됐으면, 모든 피어에 전체 상태를 일괄 동기화한다.
      // 이를 통해 BufferLast RPC 가 "마지막 단일 항목"만 버퍼링하는 제약을 우회하고,
      // 늦은 입장 클라이언트도 완전한 초기 상태를 수신한다.
      if (displayTarget != null && node.StateOperations != null)
      {
        bool hasDisplayOp = false;
        foreach (var op in node.StateOperations)
        {
          if (op != null && op.Kind == ScenarioEntityStateOperationKind.DisplayState)
          {
            hasDisplayOp = true;
            break;
          }
        }
        if (hasDisplayOp)
        {
          displayTarget.SyncAllDisplayStatesNetworked();
        }
      }
    }

    private void ExecuteTriageAssessControlNode(ScenarioTriageAssessControlNode node)
    {
      _state = State.ExecutingTriageAssessControl;

      if (node == null || string.IsNullOrWhiteSpace(node.TargetEntityIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] TriageAssessControl '{node?.Identifier}' target identifier is missing.");
        Advance();
        return;
      }

      if (!Registry.Registry.TryGetEntity(node.TargetEntityIdentifier, out var descriptor)
          || descriptor?.GameObject == null)
      {
        Debug.LogWarning($"[ScenarioController] TriageAssessControl '{node.Identifier}' target '{node.TargetEntityIdentifier}' was not found.");
        Advance();
        return;
      }

      var target = descriptor.GameObject.GetComponentInChildren<Entity.IScenarioTriageAssessTarget>(true);
      if (target == null)
      {
        Debug.LogWarning($"[ScenarioController] TriageAssessControl '{node.Identifier}' target '{node.TargetEntityIdentifier}' has no IScenarioTriageAssessTarget.");
        Advance();
        return;
      }

      target.SetTriageAssessable(node.Assessable);
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] TriageAssessControl: {node.TargetEntityIdentifier}.assessable={node.Assessable}");
#endif

      Advance();
    }

    /// <summary>
    /// 환자 엔티티에 의료 상태 프리셋을 적용한다.
    ///
    /// <para>
    /// <b>서버 전용 실행:</b> <see cref="ExecuteEntityInitNode"/> 와 동일하게, 프리셋 적용은 서버/호스트
    /// 컨텍스트에서만 수행하고 결과를 RPC로 클라이언트에 전파한다. 클라이언트에서는 노드를 건너뛴다.
    /// </para>
    ///
    /// <para>
    /// <b>레지스트리 타이밍:</b> <see cref="PatientController"/>는 FishNet <c>OnStartClient</c> 콜백에서
    /// 레지스트리에 등록된다. 이 콜백은 스폰 직후 즉시 호출되지 않을 수 있으므로, 대상이 등록될 때까지
    /// 최대 <c>PatientMedicalStatePresetRegistryTimeoutFrames</c> 프레임 동안 폴링한다.
    /// </para>
    /// </summary>
    private const int PatientMedicalStatePresetRegistryTimeoutFrames = 10;

    private IEnumerator ExecutePatientMedicalStatePresetNode(ScenarioPatientMedicalStatePresetNode node)
    {
      _state = State.ExecutingPatientMedicalStatePreset;

      if (node == null)
      {
        Advance();
        yield break;
      }

      // 서버(또는 오프라인) 컨텍스트에서만 실행한다.
      // ExecuteEntityInitNode 와 동일한 패턴. 클라이언트는 Advance 만 호출하고 프리셋 적용을 건너뛴다.
      // 프리셋 필드는 서버가 적용 후 RPC(ApplyMedicalStatePreset 내부의 RpcSyncVitalMedicalState)로 전파된다.
      if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsOffline)
      {
        Advance();
        yield break;
      }

      // 대상 엔티티 식별자 결정: 직접 지정 → 상태 저장소 조회 순서
      string targetIdentifier = node.TargetEntityIdentifier;
      if (string.IsNullOrWhiteSpace(targetIdentifier) && !string.IsNullOrWhiteSpace(node.TargetEntityStateKey))
      {
        _stateStore.TryGetValue(node.TargetEntityStateKey, out targetIdentifier);
      }

      if (string.IsNullOrWhiteSpace(targetIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] PatientMedicalStatePreset '{node.Identifier}': target identifier is missing.");
        Advance();
        yield break;
      }

      // PatientController 는 FishNet OnStartClient 콜백에서 레지스트리에 등록된다.
      // 스폰(EntityPresetSpawn) 직후 이 노드가 실행되면 등록이 아직 완료되지 않았을 수 있으므로,
      // 최대 PatientMedicalStatePresetRegistryTimeoutFrames 프레임 동안 폴링한다.
      PatientController patient = null;
      for (int frame = 0; frame < PatientMedicalStatePresetRegistryTimeoutFrames; frame++)
      {
        if (Registry.Registry.TryGetEntity(targetIdentifier, out var descriptor) && descriptor?.GameObject != null)
        {
          patient = descriptor.GameObject.GetComponentInChildren<PatientController>(true);
          if (patient != null)
            break;
        }

        yield return null; // 다음 프레임까지 대기
      }

      if (patient == null)
      {
        Debug.LogWarning($"[ScenarioController] PatientMedicalStatePreset '{node.Identifier}': target '{targetIdentifier}' not found after {PatientMedicalStatePresetRegistryTimeoutFrames} frames.");
        Advance();
        yield break;
      }

      patient.ApplyMedicalStatePreset(node);

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] PatientMedicalStatePreset applied to '{targetIdentifier}'.");
#endif

      Advance();
    }

    private IEnumerator ExecutePlayTTSNode(ScenarioPlayTTSNode node)
    {
      _state = State.ExecutingTTS;

      if (_ttsService == null)
      {
        Debug.LogWarning("[ScenarioController] TTSService 참조가 없습니다. PlayTTS 노드를 건너뜁니다.");
        Advance();
        yield break;
      }

      if (_ttsAudioSource == null)
      {
        Debug.LogWarning("[ScenarioController] TTS AudioSource 참조가 없습니다. PlayTTS 노드를 건너뜁니다.");
        Advance();
        yield break;
      }

      // TTSService가 준비될 때까지 대기
      if (!_ttsService.IsReady)
        yield return new WaitUntil(() => _ttsService.IsReady);

      // 동적 캐싱이 진행 중이면 완료될 때까지 대기
      if (_ttsService.IsDynamicCacheDirty)
        yield return new WaitUntil(() => !_ttsService.IsDynamicCacheDirty);

      var variables = node.Variables != null && node.Variables.Count > 0
          ? node.Variables
          : null;

      var playCoroutine = _ttsService.PlayTranscript(
        node.TranscriptIdentifier, _ttsAudioSource, variables,
        voiceIdentifier: string.IsNullOrEmpty(node.TtsVoiceIdentifier) ? null : node.TtsVoiceIdentifier);

      if (node.WaitUntilFinished)
        yield return playCoroutine;

      Advance();
    }

    /// <summary>
    /// 현재 그래프에 포함된 모든 PlayTTS 노드의 동적 세그먼트를
    /// 백그라운드에서 미리 합성합니다(sideeffect: IsDynamicCacheDirty 설정).
    /// </summary>
    private void PrewarmTTSCache()
    {
      if (_ttsService == null || _currentGraph == null) return;

      foreach (var node in _currentGraph.Nodes.Values)
      {
        if (node is ScenarioPlayTTSNode playTTS)
        {
          var vars = playTTS.Variables != null && playTTS.Variables.Count > 0
              ? playTTS.Variables
              : null;
          string voiceId = string.IsNullOrEmpty(playTTS.TtsVoiceIdentifier) ? null : playTTS.TtsVoiceIdentifier;
          _ttsService.PrepareTranscriptVariables(playTTS.TranscriptIdentifier, vars, voiceIdentifier: voiceId);
        }
      }
    }

    /// <summary>
    /// 시나리오 그래프의 인라인 텍스트(Dialogue/Choice/Quiz 콘텐츠)를 TTS로 재생한다.
    /// baked WAV가 있으면 우선 재생하고, 없으면 즉석 합성한다.
    /// TTSService/AudioSource 참조가 없거나 텍스트가 비어 있으면 아무 작업도 하지 않는다.
    /// 텍스트 표시와 병렬로 재생되며(대기하지 않음), 다음 노드 진행을 막지 않는다.
    /// </summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null이면 기본 목소리를 사용한다.
    /// </param>
    private void PlayInlineTTS(string nodeIdentifier, string text, string voiceIdentifier = null)
    {
      if (_ttsService == null || _ttsAudioSource == null) return;
      if (string.IsNullOrWhiteSpace(text)) return;

      // IsReady를 기다리지 않는다: baked WAV는 ONNX 초기화 없이 즉시 재생 가능하고,
      // baked가 없을 때만 내부에서 즉석 합성(초기화 완료 후 가능)으로 폴백한다.
      string scenarioIdentifier = _currentGraph != null ? _currentGraph.Identifier : null;
      _ttsService.PlayText(text, _ttsAudioSource, scenarioIdentifier, nodeIdentifier, voiceIdentifier);
    }

    private void HandleQuizSelection(int index, ScenarioQuizNode node)
    {
      if (node.Options == null || index < 0 || index >= node.Options.Count)
      {
        Debug.LogWarning($"[ScenarioController] Invalid quiz option index: {index}");
        return;
      }

      bool isCorrect = index == node.CorrectIndex;
      ResolveQuizNext(node, isCorrect);
    }

    private void ResolveQuizNext(ScenarioQuizNode node, bool isCorrect)
    {
      ShowQuizFeedback(node, isCorrect);

      // 정답/오답 전용 다음 노드가 없으면 공통 NextIdentifier 로 폴백한다.
      // (정답 경로에 폴백이 없으면 nextIdentifier 만 지정한 퀴즈에서 정답 시 시나리오가 종료되는 버그)
      var target = ResolveQuizTarget(node, isCorrect);

      _activeQuizNode = null;
      ClearOptions();

      if (string.IsNullOrWhiteSpace(target))
      {
        EndScenario();
        return;
      }

      if (!_currentGraph.TryGetNode(target, out var nextNode))
      {
        Debug.LogError($"[ScenarioController] Quiz next node '{target}' not found");
        EndScenario();
        return;
      }

      _currentNode = nextNode;
      ExecuteNode(nextNode);
    }

    private bool ApplyQuestOperation(QuestManager manager, ScenarioQuestControlNode node)
    {
      string questId = ResolveQuestId(node);
      var questData = BuildQuestPayload(node, questId);

      switch (node.Operation)
      {
        case ScenarioQuestOperationType.Add:
          if (manager.HasQuest(questId))
          {
            return HandleConflict(node, () => manager.AddOrUpdateQuest(questData));
          }

          if (questData == null)
          {
            Debug.LogWarning("[ScenarioController] Add quest operation missing quest data.");
            return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;
          }

          manager.AddOrUpdateQuest(questData);
          return true;

        case ScenarioQuestOperationType.Update:
          if (manager.HasQuest(questId))
          {
            if (questData == null)
            {
              Debug.LogWarning("[ScenarioController] Update quest operation missing quest data.");
              return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;
            }

            manager.AddOrUpdateQuest(questData);
            return true;
          }

          if (node.FailureStrategy == ScenarioQuestFailureStrategy.Overwrite)
          {
            if (questData == null)
            {
              Debug.LogWarning("[ScenarioController] Update->Overwrite quest operation missing quest data.");
              return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;
            }

            manager.AddOrUpdateQuest(questData);
            return true;
          }

          return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;

        case ScenarioQuestOperationType.Remove:
          if (manager.HasQuest(questId))
          {
            manager.RemoveQuest(questId);
            return true;
          }

          return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;

        default:
          Debug.LogWarning($"[ScenarioController] Unknown quest operation {node.Operation}.");
          return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;
      }
    }

    private static string ResolveQuestId(ScenarioQuestControlNode node)
    {
      if (node == null)
        return null;

      if (!string.IsNullOrWhiteSpace(node.Quest?.Id))
        return node.Quest.Id;

      if (!string.IsNullOrWhiteSpace(node.QuestDefinitionIdentifier))
        return node.QuestDefinitionIdentifier;

      return null;
    }

    private static QuestData BuildQuestPayload(ScenarioQuestControlNode node, string questId)
    {
      if (node == null)
        return null;

      if (node.Quest != null)
      {
        var copy = node.Quest.Clone();
        if (string.IsNullOrWhiteSpace(copy.Id))
          copy.Id = questId;

        if (string.IsNullOrWhiteSpace(copy.DefinitionIdentifier) && !string.IsNullOrWhiteSpace(node.QuestDefinitionIdentifier))
          copy.DefinitionIdentifier = node.QuestDefinitionIdentifier;

        return copy;
      }

      if (string.IsNullOrWhiteSpace(node.QuestDefinitionIdentifier))
        return null;

      return new QuestData
      {
        Id = string.IsNullOrWhiteSpace(questId) ? node.QuestDefinitionIdentifier : questId,
        DefinitionIdentifier = node.QuestDefinitionIdentifier
      };
    }

    private bool HandleConflict(ScenarioQuestControlNode node, Action overwriteAction)
    {
      switch (node.FailureStrategy)
      {
        case ScenarioQuestFailureStrategy.Overwrite:
          overwriteAction?.Invoke();
          return true;
        case ScenarioQuestFailureStrategy.Ignore:
          return true;
        case ScenarioQuestFailureStrategy.Panic:
          return false;
        default:
          return true;
      }
    }

    // 이동 도착 판정 임계값(수평 거리, m).
    private const float MoveArriveThreshold = 0.05f;
    private const string NpcWalkAnimationParameterName = "walk";

    private IEnumerator ExecutePlayerMoveNode(ScenarioPlayerMoveNode node)
    {
      _state = State.ExecutingPlayerMove;

      if (!TryResolveMoveDestination(node.DestinationType, node.DestinationIdentifier,
            node.DestinationX, node.DestinationY, node.DestinationZ, out var destination))
      {
        Debug.LogWarning($"[ScenarioController] Waypoint '{node.DestinationIdentifier}' not found. Fallback to no move.");
        Advance();
        yield break;
      }

      // 오우너 클라이언트에서만 로컬 플레이어를 이동시킨다.
      // 다른 피어에서는 owner와 동일 공식으로 산출한 이동 시간만큼 대기하여
      // 그래프 진행 타이밍을 맞춘다(피어 간 커서 동기화).
      var player = ResolveLocalOwnerPlayer();
      if (player == null)
      {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[ScenarioController] PlayerMove '{node.Identifier}' skipped local movement (not owner or player unavailable).");
#endif
        float waitSeconds = ComputeMoveDurationSeconds(node.MoveMode, node.MoveSpeed, node.MoveDuration,
          EstimatePlayerMoveDistance(destination));
        if (waitSeconds > 0f)
          yield return new WaitForSeconds(waitSeconds);
        Advance();
        yield break;
      }

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Moving player to: {destination} (mode={node.MoveMode})");
#endif

      player.BeginScriptedMovement();
      bool wasCanMove = player.canMove;
      player.canMove = false;
      try
      {
        yield return MovePlayerRoutine(player, destination, node.MoveMode, node.MoveSpeed, node.MoveDuration, node.IgnoreGroundCheck);
      }
      finally
      {
        // 이동 중 플레이어가 파괴되었을 수 있으므로 null 가드를 둔다(Unity의 == null 오버로드).
        if (player != null)
        {
          player.EndScriptedMovement();
          player.canMove = wasCanMove;
        }
      }

      Advance();
    }

    /// <summary>
    /// non-owner 대기 시간(BySpeed) 산출을 위해, 실제 이동하는 owner 플레이어의
    /// 예상 이동 거리를 추정한다. owner 플레이어의 위치는 FishNet으로 동기화되므로
    /// non-owner 피어에서도 동일한 시작 위치를 참조해 owner와 동일한 이동 시간을 얻는다.
    /// owner 엔티티를 찾지 못하면 0을 반환한다(speed 기반 대기가 0 → 즉시 진행).
    /// </summary>
    private float EstimatePlayerMoveDistance(Vector3 destination)
    {
      // owner가 지정된 경우 owner 플레이어 위치 기준, 아니면 로컬 플레이어 위치 기준.
      int? targetClientId = _scenarioOwnerClientId ?? (int?)InstanceFinder.ClientManager?.Connection?.ClientId;
      if (targetClientId.HasValue
          && Registry.Registry.TryGetEntityByClientId(targetClientId.Value, out var descriptor)
          && descriptor?.GameObject != null)
      {
        return HorizontalDistance(descriptor.GameObject.transform.position, destination);
      }

      return 0f;
    }

    /// <summary>
    /// 시나리오 owner인 로컬 플레이어의 <see cref="PlayerController"/> 를 반환한다.
    /// owner가 아니거나(다른 피어) 로컬 플레이어를 찾지 못하면 null.
    /// owner가 지정되지 않은 경우(null)에는 로컬 플레이어를 반환한다.
    /// </summary>
    private PlayerController ResolveLocalOwnerPlayer()
    {
      var localConn = InstanceFinder.ClientManager?.Connection;
      if (localConn == null)
        return null;

      int localClientId = localConn.ClientId;

      // owner가 지정된 경우 로컬 클라이언트가 owner일 때만 이동한다.
      if (_scenarioOwnerClientId.HasValue && _scenarioOwnerClientId.Value != localClientId)
        return null;

      if (Registry.Registry.TryGetEntityByClientId(localClientId, out var descriptor)
          && descriptor?.GameObject != null
          && descriptor.GameObject.TryGetComponent<PlayerController>(out var player))
      {
        return player;
      }

      return null;
    }

    /// <summary>
    /// 목적지(좌표 또는 웨이포인트)를 해석한다. 실패 시 false.
    /// </summary>
    private static bool TryResolveMoveDestination(
      ScenarioMoveDestinationType destinationType,
      string destinationIdentifier,
      float x, float y, float z,
      out Vector3 destination)
    {
      if (destinationType == ScenarioMoveDestinationType.Position)
      {
        destination = new Vector3(x, y, z);
        return true;
      }

      if (Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, destinationIdentifier, out destination)
          || Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, destinationIdentifier, out destination))
      {
        return true;
      }

      destination = Vector3.zero;
      return false;
    }

    /// <summary>
    /// 이동 총 소요 시간(초)을 산출한다. owner의 실제 이동과 non-owner의 대기가
    /// 동일 공식을 사용하도록 하여 피어 간 그래프 진행 타이밍을 일치시킨다.
    /// - Instant: 0
    /// - BySpeed: 거리 / 속도 (속도가 0 이하이면 0 → 즉시)
    /// - ByDuration: MoveDuration
    /// </summary>
    private static float ComputeMoveDurationSeconds(ScenarioMoveMode mode, float moveSpeed, float moveDuration, float distance)
    {
      switch (mode)
      {
        case ScenarioMoveMode.ByDuration:
          return Mathf.Max(0f, moveDuration);
        case ScenarioMoveMode.BySpeed:
          return moveSpeed > 0f ? distance / moveSpeed : 0f;
        default: // Instant
          return 0f;
      }
    }

    /// <summary>
    /// CharacterController 기반 시간 보간 이동. 총 소요 시간 동안 시작 위치→목적지를
    /// 선형 보간하므로 BySpeed/ByDuration 모두 예상 시간에 정확히 도착하며,
    /// non-owner 대기 시간(<see cref="ComputeMoveDurationSeconds"/>)과 완료 시점이 일치한다.
    /// IgnoreGroundCheck=false 이면 중력을 적용해 접지 상태를 유지한다.
    /// </summary>
    private IEnumerator MovePlayerRoutine(
      PlayerController player,
      Vector3 destination,
      ScenarioMoveMode mode,
      float moveSpeed,
      float moveDuration,
      bool ignoreGroundCheck)
    {
      var playerTransform = player.transform;
      Vector3 start = playerTransform.position;
      float distance = HorizontalDistance(start, destination);

      bool applyGravity = !ignoreGroundCheck;

      if (distance <= MoveArriveThreshold)
        yield break;

      FaceHorizontal(playerTransform, destination);

      float duration = ComputeMoveDurationSeconds(mode, moveSpeed, moveDuration, distance);
      if (duration <= 0f)
      {
        // Instant 또는 속도 0: 즉시 목적지로.
        player.ApplyScriptedMove(HorizontalDelta(playerTransform.position, destination), applyGravity: false);
        yield break;
      }

      float elapsed = 0f;
      Vector3 previousTarget = start;

      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // 이번 프레임에 있어야 할 보간 지점과 이전 지점의 차이만큼만 이동시킨다.
        Vector3 currentTarget = Vector3.Lerp(start, destination, t);
        Vector3 frameDelta = HorizontalDelta(previousTarget, currentTarget);
        previousTarget = currentTarget;

        player.ApplyScriptedMove(frameDelta, applyGravity);

        yield return null;
      }
    }

    private static Vector3 HorizontalDelta(Vector3 from, Vector3 to)
    {
      var delta = to - from;
      delta.y = 0f;
      return delta;
    }

    private static float HorizontalDistance(Vector3 from, Vector3 to)
      => HorizontalDelta(from, to).magnitude;

    private static void FaceHorizontal(Transform transform, Vector3 target)
    {
      Vector3 forward = target - transform.position;
      forward.y = 0f;
      if (forward.sqrMagnitude <= 0.0001f)
        return;

      transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
    }

    private IEnumerator ExecuteNPCMoveNode(ScenarioNPCMoveNode node)
    {
      _state = State.ExecutingNPCMove;

      // NPC 확인
      var npc = Registry.Registry.Get<GameObject>(RegistryType.Npc, node.NPCIdentifier)
                ?? Registry.Registry.Get<GameObject>(RegistryType.Entity, node.NPCIdentifier);
      if (npc == null)
      {
        Debug.LogWarning($"[ScenarioController] NPC '{node.NPCIdentifier}' not found. Skipping move.");
        Advance();
        yield break;
      }

      if (!TryResolveMoveDestination(node.DestinationType, node.DestinationIdentifier,
            node.DestinationX, node.DestinationY, node.DestinationZ, out var destination))
      {
        Debug.LogWarning($"[ScenarioController] Waypoint '{node.DestinationIdentifier}' not found. Fallback to no move.");
        Advance();
        yield break;
      }

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Moving NPC '{node.NPCIdentifier}' to: {destination} (mode={node.MoveMode})");
#endif

      var animation = npc.GetComponentInChildren<HumanoidAnimationController>(true);
      try
      {
        yield return MoveNpcRoutine(npc.transform, animation, destination,
          node.MoveMode, node.MoveSpeed, node.MoveDuration, node.IgnoreGroundCheck);
      }
      finally
      {
        // 이동 종료 시 walk 애니메이션 해제 (NPC가 파괴되지 않았을 때만)
        if (animation != null)
          animation.SetBool(NpcWalkAnimationParameterName, false);
      }

      Advance();
    }

    /// <summary>
    /// NPC(transform) 시간 보간 이동. NavMesh를 사용하지 않으므로 transform을 직접 이동한다.
    /// IgnoreGroundCheck=false 이면 CharacterController가 있을 때 누적 중력을,
    /// 없을 때는 지면 레이캐스트로 접지 y를 보정한다.
    /// 이동 중 HumanoidAnimationController의 walk 파라미터를 true로 설정한다.
    /// </summary>
    private IEnumerator MoveNpcRoutine(
      Transform npcTransform,
      HumanoidAnimationController animation,
      Vector3 destination,
      ScenarioMoveMode mode,
      float moveSpeed,
      float moveDuration,
      bool ignoreGroundCheck)
    {
      var characterController = npcTransform.GetComponent<CharacterController>();
      // 지면 레이캐스트가 NPC 자신을 맞히지 않도록 자신의 콜라이더를 제외 목록으로 둔다.
      var ownColliders = npcTransform.GetComponentsInChildren<Collider>(true);
      float verticalVelocity = 0f; // CharacterController 중력 누적 속도(m/s)

      void MoveStep(Vector3 horizontalDelta)
      {
        if (characterController != null && characterController.enabled)
        {
          Vector3 motion = horizontalDelta;
          if (!ignoreGroundCheck)
          {
            // 누적 속도 기반 중력 변위(m). 접지 시 하향 속도를 리셋한다.
            if (characterController.isGrounded && verticalVelocity < 0f)
              verticalVelocity = 0f;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            motion.y += verticalVelocity * Time.deltaTime;
          }
          characterController.Move(motion);
        }
        else
        {
          Vector3 next = npcTransform.position + horizontalDelta;
          if (!ignoreGroundCheck && TryRaycastGround(next, ownColliders, out float groundY))
          {
            next.y = groundY;
          }
          npcTransform.position = next;
        }
      }

      Vector3 start = npcTransform.position;
      float distance = HorizontalDistance(start, destination);

      if (distance <= MoveArriveThreshold)
        yield break;

      FaceHorizontal(npcTransform, destination);

      float duration = ComputeMoveDurationSeconds(mode, moveSpeed, moveDuration, distance);
      if (duration <= 0f)
      {
        MoveStep(HorizontalDelta(npcTransform.position, destination));
        yield break;
      }

      animation?.SetBool(NpcWalkAnimationParameterName, true);

      float elapsed = 0f;
      Vector3 previousTarget = start;

      while (elapsed < duration)
      {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        Vector3 currentTarget = Vector3.Lerp(start, destination, t);
        Vector3 frameDelta = HorizontalDelta(previousTarget, currentTarget);
        previousTarget = currentTarget;

        MoveStep(frameDelta);

        yield return null;
      }
    }

    /// <summary>
    /// 지정 위치 상공에서 하향 레이캐스트로 지면 y를 찾는다.
    /// 트리거는 무시하고, NPC 자신의 콜라이더에 맞은 히트는 건너뛴다.
    /// </summary>
    private static bool TryRaycastGround(Vector3 position, Collider[] ownColliders, out float groundY)
    {
      groundY = position.y;
      var hits = Physics.RaycastAll(position + Vector3.up * 2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
      float best = float.NegativeInfinity;
      bool found = false;

      for (int i = 0; i < hits.Length; i++)
      {
        var hit = hits[i];
        if (IsOwnCollider(hit.collider, ownColliders))
          continue;

        if (!found || hit.point.y > best)
        {
          best = hit.point.y;
          found = true;
        }
      }

      if (found)
        groundY = best;
      return found;
    }

    private static bool IsOwnCollider(Collider collider, Collider[] ownColliders)
    {
      if (collider == null || ownColliders == null)
        return false;

      for (int i = 0; i < ownColliders.Length; i++)
      {
        if (ReferenceEquals(collider, ownColliders[i]))
          return true;
      }

      return false;
    }

    private IEnumerator ExecuteCameraTargetNode(ScenarioCameraTargetNode node)
    {
      _state = State.ExecutingCameraTarget;

      // TODO: 카메라 타겟팅 로직 구현
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Targeting camera to: {node.TargetObjectIdentifier}");
#endif

      if (!_camController.IsUnityNull())
      {
        // TODO: 카메라 타겟 설정
        // _camController.SetTargetObject(node.TargetObjectIdentifier, node.OffsetX, node.OffsetY, node.OffsetZ, node.BlendTime);
      }

      yield return new WaitForSeconds(node.BlendTime);

      Advance();
    }

    private IEnumerator ExecuteInvokeEventNode(ScenarioInvokeEventNode node)
    {
      _state = State.ExecutingInvokeEvent;

      if (string.IsNullOrWhiteSpace(node.EventIdentifier))
      {
        Debug.LogWarning("[ScenarioController] InvokeEvent node has no eventIdentifier.");
      }
      else if (ScenarioEventIdentifierRegistry.TryGetHandler(node.EventIdentifier, out var handler))
      {
        System.Collections.IEnumerator routine = null;
        try
        {
          routine = handler?.Invoke();
        }
        catch (Exception ex)
        {
          Debug.LogException(ex);
        }

        switch (node.MoveNextBehavior)
        {
          case ScenarioInvokeEventMoveNextBehavior.WaitUntilDone:
            if (routine != null)
            {
              yield return StartCoroutine(routine);
            }
            break;
          case ScenarioInvokeEventMoveNextBehavior.Immediately:
            if (routine != null)
            {
              StartCoroutine(routine);
            }
            break;
          case ScenarioInvokeEventMoveNextBehavior.False:
            // Do nothing; do not advance.
            break;
        }
      }
      else
      {
        Debug.LogWarning($"[ScenarioController] No handler registered for event '{node.EventIdentifier}'.");
      }

      if (node.MoveNextBehavior != ScenarioInvokeEventMoveNextBehavior.False)
      {
        Advance();
      }
    }

    private IEnumerator ExecuteValidatorNode(ScenarioValidatorNode node)
    {
      _state = State.ExecutingValidator;

      // WaitForCondition=true 이면 조건 충족까지 폴링 대기하는 게이트로 동작한다.
      if (node.WaitForCondition)
      {
        // 조건 충족 또는 (지정 시) 타임아웃 중 먼저 도달하는 쪽까지 대기한다.
        yield return WaitForValidatorGate(node);

        // 조건이 충족된 상태로 빠져나왔다면 정상 진행한다.
        if (EvaluateValidator(node))
        {
          Advance();
          yield break;
        }

        // 여기 도달 = 타임아웃 발생(조건 미충족). 정책을 적용한다.
        OnValidatorWaitTimeout?.Invoke(node, node.OnWaitTimeout);

        switch (node.OnWaitTimeout)
        {
          case ScenarioValidatorWaitTimeoutBehavior.ForceAdvance:
            Debug.LogWarning($"[ScenarioController] Validator gate '{node.Identifier}' timed out after {node.WaitTimeoutSeconds}s. ForceAdvance (미수행 기록).");
            Advance();
            yield break;

          case ScenarioValidatorWaitTimeoutBehavior.FailBranch:
            if (!string.IsNullOrEmpty(node.FailureNextIdentifier)
                && _currentGraph.TryGetNode(node.FailureNextIdentifier, out var failureNode))
            {
              Debug.LogWarning($"[ScenarioController] Validator gate '{node.Identifier}' timed out. FailBranch → '{node.FailureNextIdentifier}'.");
              _currentNode = failureNode;
              ExecuteNode(failureNode);
              yield break;
            }
            // 분기 대상이 없으면 KeepWaiting 으로 폴백.
            Debug.LogWarning($"[ScenarioController] Validator gate '{node.Identifier}' timed out but FailureNextIdentifier '{node.FailureNextIdentifier}' is unavailable; falling back to KeepWaiting.");
            yield return new WaitUntil(() => EvaluateValidator(node));
            Advance();
            yield break;

          case ScenarioValidatorWaitTimeoutBehavior.WarnAndKeepWaiting:
            ReportValidatorWaitTimeoutWarning(node);
            yield return new WaitUntil(() => EvaluateValidator(node));
            Advance();
            yield break;

          case ScenarioValidatorWaitTimeoutBehavior.KeepWaiting:
          default:
            // 타임아웃을 무시하고 조건이 올라올 때까지 계속 대기(기존 동작).
            yield return new WaitUntil(() => EvaluateValidator(node));
            Advance();
            yield break;
        }
      }

      bool passed = EvaluateValidator(node, out var failureReason);

      if (passed)
      {
        Advance();
        yield break;
      }

      ReportValidatorFailure(node, failureReason);

      switch (node.OnFailure)
      {
        case ScenarioValidatorOnFailure.Panic:
          EndScenario();
          break;
        case ScenarioValidatorOnFailure.Branching:
          if (!string.IsNullOrEmpty(node.FailureNextIdentifier) && _currentGraph.TryGetNode(node.FailureNextIdentifier, out var failureNode))
          {
            _currentNode = failureNode;
            ExecuteNode(failureNode);
          }
          else
          {
            Debug.LogWarning($"[ScenarioController] Validator branching failed: next '{node.FailureNextIdentifier}' not found.");
            EndScenario();
          }
          break;
        case ScenarioValidatorOnFailure.Ignore:
          Advance();
          break;
      }

      yield break;
    }

    /// <summary>
    /// 브랜치 체인 내부에서 Validator 를 게이트로 평가한다(전역 Advance 미사용).
    /// WaitForCondition=true 이면 조건 충족까지 폴링 대기한다.
    /// false 이면 1회 평가하고, 실패 시 OnFailure 정책 중 Panic 만 브랜치를 즉시 중단한다
    /// (브랜치 내부에서는 EndScenario/전역 Branching 을 일으키지 않고 통과 진행한다).
    /// </summary>
    private IEnumerator ExecuteValidatorGate(ScenarioValidatorNode node)
    {
      if (node.WaitForCondition)
      {
        yield return WaitForValidatorGate(node);

        if (EvaluateValidator(node))
        {
          yield break;
        }

        // 타임아웃 발생. 브랜치 내부에서는 전역 Advance/EndScenario/Branching 을 일으키지 않고,
        // 정책에 따라 "대기 지속" 또는 "대기 종료(체인 진행 허용)" 만 결정한다.
        OnValidatorWaitTimeout?.Invoke(node, node.OnWaitTimeout);

        switch (node.OnWaitTimeout)
        {
          case ScenarioValidatorWaitTimeoutBehavior.ForceAdvance:
          case ScenarioValidatorWaitTimeoutBehavior.FailBranch:
            // 브랜치 체인은 NextIdentifier 로 진행하므로, 대기를 끝내면 체인이 다음 노드로 이동한다.
            // (브랜치 내부에는 전역 실패 분기가 없으므로 FailBranch 도 동일하게 게이트만 해제한다.)
            Debug.LogWarning($"[ScenarioController] Branch validator gate '{node.Identifier}' timed out after {node.WaitTimeoutSeconds}s; releasing gate (미수행 기록).");
            yield break;

          case ScenarioValidatorWaitTimeoutBehavior.WarnAndKeepWaiting:
            ReportValidatorWaitTimeoutWarning(node);
            yield return new WaitUntil(() => EvaluateValidator(node));
            yield break;

          case ScenarioValidatorWaitTimeoutBehavior.KeepWaiting:
          default:
            yield return new WaitUntil(() => EvaluateValidator(node));
            yield break;
        }
      }

      if (EvaluateValidator(node, out var failureReason))
      {
        yield break;
      }

      ReportValidatorFailure(node, failureReason);
      // 브랜치 내부에서는 진행을 막지 않고 통과한다(병렬 합류 흐름 보호).
      yield break;
    }

    /// <summary>
    /// WaitForCondition 게이트의 대기 루틴. WaitTimeoutSeconds 가 양수이면 "조건 충족 OR 타임아웃" 중
    /// 먼저 도달하는 쪽까지 대기하고, null/0 이하이면 조건이 충족될 때까지 무한 대기한다(기존 동작).
    /// 타임아웃 판정은 권위 컨텍스트(게이트를 구동하는 컨트롤러)에서 수행된다.
    /// 빠져나온 뒤 조건 충족 여부는 호출부가 <see cref="EvaluateValidator(ScenarioValidatorNode)"/> 로 재확인한다.
    /// </summary>
    private IEnumerator WaitForValidatorGate(ScenarioValidatorNode node)
    {
      if (!EvaluateValidator(node, out var failureReason))
      {
        ReportValidatorBlocked(node, failureReason);
      }

      var timeout = node.WaitTimeoutSeconds;
      if (timeout is > 0f)
      {
        float deadline = Time.time + timeout.Value;
        yield return new WaitUntil(() => EvaluateValidator(node) || Time.time >= deadline);
      }
      else
      {
        yield return new WaitUntil(() => EvaluateValidator(node));
      }
    }

    /// <summary>
    /// WarnAndKeepWaiting 정책에서 운영자에게 게이트 타임아웃 경고를 전달한다(콘솔 + 인게임챗).
    /// </summary>
    private void ReportValidatorWaitTimeoutWarning(ScenarioValidatorNode node)
    {
      var message = $"Validator gate '{node.Identifier}' timed out after {node.WaitTimeoutSeconds}s; still waiting for condition.";
      Debug.LogWarning($"[ScenarioController] {message}");
      AppendSystemChatMessage(message);
    }

    private IEnumerator ExecuteParallelNode(ScenarioParallelNode node)
    {
      _state = State.ExecutingParallel;

      var runningCoroutines = new List<Coroutine>();
      var runningTrackers = new List<BranchCompletionTracker>();
      int? localClientId = (int?)InstanceFinder.ClientManager?.Connection?.ClientId;
      var players = GetActivePlayerIds();
      var allocation = new Dictionary<ScenarioParallelBranch, int?>();

      if (!TryAllocateParallel(node, players, allocation))
      {
        EndScenario();
        yield break;
      }

      foreach (var branch in node.Branches)
      {
        if (!_currentGraph.TryGetNode(branch.Identifier, out var branchNode))
        {
          Debug.LogWarning($"[ScenarioController] Parallel branch target '{branch.Identifier}' not found.");
          continue;
        }

        allocation.TryGetValue(branch, out var assignedClientId);

        if (assignedClientId == null && node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Ignore)
        {
          continue; // skipped branch
        }

        if (assignedClientId == null && node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Panic)
        {
          EndScenario();
          yield break;
        }

        // 멀티플레이어에서는 로컬 클라이언트에 할당된 브랜치만 실행한다.
        // (미할당 브랜치 체인을 로컬에서 함께 돌리면 타 역할 노드가 한 번에 실행되는 문제가 발생한다.)
        if (assignedClientId.HasValue && localClientId.HasValue && assignedClientId.Value != localClientId.Value)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
          Debug.Log($"[ScenarioController] Parallel branch '{branch.Identifier}' skipped on local client {localClientId} (assigned to {assignedClientId}).");
#endif
          continue;
        }

        // Unity 의 Coroutine 핸들은 완료되어도 null 이 되지 않으므로,
        // 완료 추적 플래그로 감싸 WaitMode.Any 판정에 사용한다.
        // 병렬 노드 자신의 NextIdentifier(합류 노드)는 브랜치 체인의 정지 라벨로도 전달한다.
        // 브랜치 종단이 합류 노드를 가리키는 그래프에서, 합류 노드가 브랜치에서 1회 +
        // 전역 Advance 에서 1회 총 2회 실행되는 것을 방지한다.
        var tracker = new BranchCompletionTracker();
        var coroutine = StartCoroutine(RunTrackedBranch(branchNode, branch.CompletionConditionIdentifier, node.NextIdentifier, assignedClientId, tracker));
        runningCoroutines.Add(coroutine);
        runningTrackers.Add(tracker);
      }

      // WaitMode에 따라 대기
      switch (node.WaitMode)
      {
        case ScenarioWaitMode.All:
          foreach (var coroutine in runningCoroutines)
          {
            yield return coroutine;
          }
          break;
        case ScenarioWaitMode.Any:
          yield return WaitForAny(runningTrackers);
          break;
        case ScenarioWaitMode.None:
          // 바로 진행
          break;
      }

      // 병렬 노드 완료 후에는 NextIdentifier 로 진행한다.
      // (기존의 무조건 EndScenario 호출은 병렬 이후 노드를 모두 건너뛰고
      //  WaitMode.None 브랜치를 StopAllCoroutines 로 즉시 종료시키는 버그였다.)
      // NextIdentifier 가 없으면 Advance 가 EndScenario 로 폴백한다.
      Advance();
    }

    /// <summary>병렬 브랜치의 완료 여부를 추적하는 플래그 홀더.</summary>
    private sealed class BranchCompletionTracker
    {
      public bool Completed;
    }

    private IEnumerator RunTrackedBranch(IScenarioNode branchNode, string completionCondition, string joinNodeIdentifier, int? assignedClientId, BranchCompletionTracker tracker)
    {
      try
      {
        yield return ExecuteBranch(branchNode, completionCondition, joinNodeIdentifier, assignedClientId);
      }
      finally
      {
        tracker.Completed = true;
      }
    }

    private IEnumerator ExecuteServerInternalSignalNode(ScenarioServerInternalSignalNode node)
    {
      _state = State.ExecutingInvokeEvent;

      if (node == null || string.IsNullOrWhiteSpace(node.SignalIdentifier))
      {
        Debug.LogWarning("[ScenarioController] Server internal signal node is missing a signal identifier.");
        Advance();
        yield break;
      }

      var targetId = ScenarioServerInternalSignalRegistry.NormalizeTarget(node.TargetIdentifier);
      var signalId = node.SignalIdentifier.Trim();

      switch (node.Operation)
      {
        case ScenarioServerInternalSignalOperationType.Register:
        {
          bool resolved = false;
          resolved = ScenarioInteractionSignals.RegisterInternal(targetId, signalId, () => resolved = true);

          if (node.WaitForResolution)
          {
            while (!resolved)
            {
              if (_currentGraph == null)
              {
                yield break;
              }

              yield return null;
            }
          }

          break;
        }
        case ScenarioServerInternalSignalOperationType.Resolve:
          ScenarioInteractionSignals.ResolveInternal(targetId, signalId);
          break;
        default:
          Debug.LogWarning($"[ScenarioController] Unsupported server internal signal operation: {node.Operation}");
          break;
      }

      Advance();
    }

    private IEnumerator ExecuteBranch(IScenarioNode node, string completionCondition, string joinNodeIdentifier, int? branchOwnerClientId)
    {
      var previousOwner = _scenarioOwnerClientId;
      _scenarioOwnerClientId = branchOwnerClientId ?? previousOwner;

      try
      {
        // 브랜치의 시작 노드부터 NextIdentifier 체인을 끝까지(또는 완료조건 라벨까지) 실행한다.
        // 완료조건 식별자(completionCondition)는 보통 그래프에 실제 노드가 없는 "수렴 라벨"이며,
        // 브랜치 체인 마지막 노드의 NextIdentifier 가 이 라벨을 가리킨다.
        // 라벨에 도달하면 브랜치 완료로 간주한다(전역 Advance/EndScenario 를 건드리지 않음).
        // joinNodeIdentifier(병렬 노드의 NextIdentifier)도 정지 라벨로 취급하여,
        // 합류 노드가 브랜치와 전역 Advance 양쪽에서 이중 실행되는 것을 막는다.
        // 브랜치 내부의 게이팅(인터랙션 완료 대기)은 체인에 포함된
        // Validator(waitForCondition=true) 노드가 담당하므로, 라벨 도달 = 브랜치 완료가 된다.
        yield return RunBranchChain(node, completionCondition, joinNodeIdentifier);
      }
      finally
      {
        _scenarioOwnerClientId = previousOwner;
      }
    }

    /// <summary>
    /// 병렬 브랜치 전용 자가완결 실행기.
    /// 전역 <see cref="_currentNode"/> / <see cref="Advance"/> / <see cref="EndScenario"/> 에 의존하지 않고,
    /// 시작 노드부터 NextIdentifier 체인을 따라 노드를 하나씩 실행·대기한다.
    /// 다음 식별자가 비어 있거나(터미널) 완료조건 라벨(<paramref name="completionLabel"/>)과 같거나
    /// 그래프에 존재하지 않으면 브랜치를 종료한다.
    /// </summary>
    private IEnumerator RunBranchChain(IScenarioNode startNode, string completionLabel, string joinNodeIdentifier = null)
    {
      var cursor = startNode;
      int guard = 0;
      const int maxNodes = 10000; // 순환 방지 안전장치.
      // 브랜치 체인별 실행 컨텍스트(동시 실행되는 다른 브랜치와 상태를 공유하지 않는다).
      var chainContext = new BranchChainContext();

      while (cursor != null)
      {
        // 시나리오가 이미 종료되어 그래프가 해제된 경우(예: WaitMode.None 병렬 브랜치가
        // 전역 시나리오보다 오래 살아남은 경우)에는 브랜치를 안전하게 종료한다.
        // 이를 누락하면 아래 _currentGraph 역참조에서 NullReferenceException 이 발생한다.
        if (_currentGraph == null)
        {
          yield break;
        }

        if (++guard > maxNodes)
        {
          Debug.LogWarning("[ScenarioController] Branch chain exceeded node limit; aborting branch to avoid infinite loop.");
          yield break;
        }

        RecordNodeVisit(cursor);
        OnNodeChanged?.Invoke(cursor);

        // 단일 노드를 실행하고 완료를 대기한다(전역 Advance 미사용).
        chainContext.NextOverride = null;
        yield return ExecuteBranchNode(cursor, chainContext);

        // 노드 대기 도중 시나리오가 종료되어 그래프가 해제됐을 수 있으므로 재확인한다.
        if (_currentGraph == null)
        {
          yield break;
        }

        // Choice 등 선택 결과에 따라 다음 노드가 결정되는 노드는 NextOverride 를 사용한다.
        var nextId = chainContext.NextOverride ?? cursor.NextIdentifier;

        // 터미널: 다음 노드가 없음.
        if (string.IsNullOrEmpty(nextId))
        {
          yield break;
        }

        // 완료조건 수렴 라벨에 도달 → 브랜치 완료.
        if (!string.IsNullOrWhiteSpace(completionLabel)
            && string.Equals(nextId, completionLabel, StringComparison.Ordinal))
        {
          yield break;
        }

        // 병렬 노드의 합류 노드(NextIdentifier)에 도달 → 브랜치 완료.
        // 합류 노드는 병렬 대기 후 전역 Advance 가 실행하므로 브랜치에서 실행하지 않는다(이중 실행 방지).
        if (!string.IsNullOrWhiteSpace(joinNodeIdentifier)
            && string.Equals(nextId, joinNodeIdentifier, StringComparison.Ordinal))
        {
          yield break;
        }

        if (!_currentGraph.TryGetNode(nextId, out var nextNode))
        {
          // 다음 식별자가 그래프에 없으면(예: 미정의 수렴 라벨) 브랜치 완료로 간주한다.
          yield break;
        }

        cursor = nextNode;
      }
    }

    /// <summary>브랜치 체인 단위의 실행 컨텍스트. 선택 결과에 따른 다음 노드 오버라이드를 전달한다.</summary>
    private sealed class BranchChainContext
    {
      /// <summary>Choice/Quiz 등 선택 결과가 다음 노드를 결정하는 경우 설정된다.</summary>
      public string NextOverride;
    }

    /// <summary>
    /// 브랜치 내부에서 단일 노드를 실행하고 그 노드가 완료될 때까지 대기한다.
    /// 각 노드 실행기는 내부적으로 전역 <see cref="Advance"/> 를 호출하지만, 브랜치 체인에서는
    /// 그 진행을 사용하지 않고 NextIdentifier 로 직접 이동하므로 부작용이 격리된다.
    /// 코루틴형 노드(예: Delay/InvokeEvent(WaitUntilDone)/Sound)는 완료까지 yield 로 대기한다.
    /// </summary>
    private IEnumerator ExecuteBranchNode(IScenarioNode node, BranchChainContext context)
    {
      // 브랜치 노드 실행기가 내부적으로 전역 Advance 를 호출하더라도 전역 시나리오 커서가
      // 끌려가지 않도록, 브랜치 노드 실행 구간 동안 전역 Advance 를 억제한다.
      // 브랜치 진행은 RunBranchChain 이 NextIdentifier 로만 수행한다.
      _globalAdvanceSuppressionDepth++;
      try
      {
        switch (node)
        {
          case ScenarioDelayNode delay:
            yield return ExecuteDelayNode(delay);
            break;
          case ScenarioInvokeEventNode invoke:
            // WaitUntilDone/Immediately 모두 실행기 말미에 전역 Advance 를 호출하지만,
            // 억제 카운터로 무시된다. Immediately(fire-and-forget) 의 경우 핸들러 코루틴이
            // 이 메서드 종료 후 끝날 수 있어, 그 시점까지 억제가 유지되도록 전용 래퍼로 감싼다.
            if (invoke.MoveNextBehavior == ScenarioInvokeEventMoveNextBehavior.WaitUntilDone)
            {
              yield return ExecuteInvokeEventNode(invoke);
            }
            else
            {
              StartCoroutine(RunInvokeEventSuppressed(invoke));
            }
            break;
          case ScenarioSoundNode sound:
            yield return ExecuteSoundNode(sound);
            break;
          case ScenarioValidatorNode validator:
            yield return ExecuteValidatorGate(validator);
            break;
          case ScenarioInteractionNode interaction:
            yield return ExecuteInteractionNode(interaction);
            break;
          case ScenarioCombineItemNode combineItem:
            yield return ExecuteCombineItemNode(combineItem);
            break;
          case ScenarioDialogueNode dialogue:
            // 브랜치 내 다이얼로그: interactionRequired면 자동 닫힘 없이 입력으로만 닫힌다.
            // 다른 그래프/흐름이 대화창을 점유 중이면 정책을 적용한다(교차 그래프 충돌만 검사).
            if (!_uiController.IsUnityNull() && !TryClaimDialogueUI(considerBranchPrompt: false))
            {
              // Cancel: 이 다이얼로그를 표시하지 않고 브랜치 체인을 종료.
              // Panic: EndScenario 로 _currentGraph 가 정리됨.
              yield break;
            }
            if (!_uiController.IsUnityNull())
            {
              _uiController.DisplayDialogue(
                dialogue.SpeakerName,
                dialogue.DialogueContent,
                dialogue.PortraitSpriteIdentifier,
                dialogue.InteractionRequired);
            }

            var waitSeconds = (dialogue.AutoAdvanceSeconds.HasValue && dialogue.AutoAdvanceSeconds.Value > 0f)
              ? dialogue.AutoAdvanceSeconds.Value
              : 0f;

            if (waitSeconds > 0f)
            {
              yield return new WaitForSeconds(waitSeconds);
            }
            break;
          case ScenarioDisinteractableDialogueNode disinteractableDialogue:
            yield return ExecuteDisinteractableDialogueNode(disinteractableDialogue);
            break;
          case ScenarioChoiceNode choice:
            // 브랜치 내 선택지: 전역 커서 대신 선택 결과를 NextOverride 로 전달한다.
            yield return ExecuteChoiceNodeInBranch(choice, context);
            break;
          case ScenarioQuizNode quiz:
            yield return ExecuteQuizNodeInBranch(quiz, context);
            break;
          case ScenarioQuestControlNode questControl:
            ExecuteQuestControlNode(questControl);
            break;
          case ScenarioQuestWaypointHighlightNode waypointHighlight:
            ExecuteQuestWaypointHighlightNode(waypointHighlight);
            break;
          case ScenarioStateUpdateNode stateUpdate:
            ExecuteStateUpdateNode(stateUpdate);
            break;
          case ScenarioPlayerTagNode playerTag:
            ExecutePlayerTagNode(playerTag);
            break;
          case ScenarioPlayTTSNode playTTS:
            yield return ExecutePlayTTSNode(playTTS);
            break;
          case ScenarioPlayerMoveNode playerMove:
            yield return ExecutePlayerMoveNode(playerMove);
            break;
          case ScenarioNPCMoveNode npcMove:
            yield return ExecuteNPCMoveNode(npcMove);
            break;
          case ScenarioCameraTargetNode cameraTarget:
            yield return ExecuteCameraTargetNode(cameraTarget);
            break;
          case ScenarioServerInternalSignalNode internalSignal:
            yield return ExecuteServerInternalSignalNode(internalSignal);
            break;
          case ScenarioEntityPresetSpawnNode entityPresetSpawn:
            ExecuteEntityPresetSpawnNode(entityPresetSpawn);
            break;
          case ScenarioEntityTagNode entityTag:
            ExecuteEntityTagNode(entityTag);
            break;
          case ScenarioEntityInitNode entityInit:
            ExecuteEntityInitNode(entityInit);
            break;
          case ScenarioTriageAssessControlNode triageAssess:
            ExecuteTriageAssessControlNode(triageAssess);
            break;
          case ScenarioItemSubmissionConfigNode itemSubmission:
            ExecuteItemSubmissionConfigNode(itemSubmission);
            break;
          case ScenarioNpcInteractControlNode npcInteractControl:
            ExecuteNpcInteractControlNode(npcInteractControl);
            break;
          case ScenarioChatPrintNode chatPrint:
            ExecuteChatPrintNode(chatPrint);
            break;
          case ScenarioExecuteCommandNode executeCommand:
            ExecuteExecuteCommandNode(executeCommand);
            break;
          case ScenarioParallelNode nestedParallel:
            // 중첩 병렬: 내부 브랜치 완료까지 대기(말미의 전역 Advance 는 억제됨).
            yield return ExecuteParallelNode(nestedParallel);
            break;
          default:
            Debug.LogWarning($"[ScenarioController] Unsupported node type in branch chain: {node.GetType().Name} (id='{node.Identifier}'). Skipping.");
            break;
        }
      }
      finally
      {
        _globalAdvanceSuppressionDepth--;
      }
    }

    /// <summary>
    /// 선택지 선택을 브랜치 체인으로 위임하기 위한 인터셉터.
    /// 값이 설정되어 있으면 <see cref="SelectOption"/> 이 전역 진행 대신 이 콜백을 호출한다.
    /// </summary>
    private Action<int> _branchOptionInterceptor;

    /// <summary>
    /// 브랜치 프롬프트(Choice/Quiz)가 화면에 표시 중인 동안 true.
    /// 다이얼로그 UI와 인터셉터는 하나뿐이므로, 동시 실행 브랜치의 프롬프트는
    /// 이 플래그로 직렬화한다(덮어쓰기 시 미해결 브랜치가 영구 대기하는 교착 방지).
    /// </summary>
    private bool _branchPromptActive;

    /// <summary>브랜치 프롬프트의 선택 결과 홀더.</summary>
    private sealed class BranchOptionSelection
    {
      public bool Resolved;
      public int Index = -1;
    }

    /// <summary>
    /// 브랜치 프롬프트 실행 공용 루틴: (1) 다른 브랜치 프롬프트가 끝날 때까지 대기(직렬화),
    /// (2) present 콜백으로 UI 표시, (3) 인터셉터로 선택을 수신할 때까지 대기.
    /// 시나리오 종료 시 selection.Resolved == false 상태로 종료된다.
    /// </summary>
    private IEnumerator RunBranchPrompt(Action present, BranchOptionSelection selection)
    {
      // 직렬화: 다른 브랜치의 프롬프트가 진행 중이면 순서를 기다린다.
      while (_branchPromptActive)
      {
        if (_currentGraph == null)
        {
          yield break;
        }
        yield return null;
      }

      if (_currentGraph == null)
      {
        yield break;
      }

      // 다른 그래프/흐름이 이미 대화창을 점유 중이면 정책을 적용한다.
      // (같은 그래프 내 동시 프롬프트는 위 _branchPromptActive 직렬화로 이미 처리되므로
      //  considerBranchPrompt: false 로 교차 그래프 점유만 검사한다.)
      if (!TryClaimDialogueUI(considerBranchPrompt: false))
      {
        // Cancel: 이 브랜치 프롬프트를 표시하지 않고 미해결 상태로 종료.
        // Panic: TryClaimDialogueUI 내부 EndScenario 로 _currentGraph 가 이미 정리됨.
        yield break;
      }

      _branchPromptActive = true;
      try
      {
        present();

        _branchOptionInterceptor = index =>
        {
          selection.Resolved = true;
          selection.Index = index;
        };

        while (!selection.Resolved)
        {
          if (_currentGraph == null)
          {
            _branchOptionInterceptor = null;
            yield break;
          }
          yield return null;
        }
      }
      finally
      {
        _branchPromptActive = false;
      }
    }

    /// <summary>
    /// 브랜치 체인 안에서 Choice 노드를 실행한다.
    /// 전역 <see cref="SelectOption"/> 경로(전역 커서 이동)를 사용하지 않고,
    /// 선택 결과를 <see cref="BranchChainContext.NextOverride"/> 로 전달한다.
    /// </summary>
    private IEnumerator ExecuteChoiceNodeInBranch(ScenarioChoiceNode node, BranchChainContext context)
    {
      var selection = new BranchOptionSelection();
      yield return RunBranchPrompt(() =>
      {
        _state = State.ExecutingChoice;
        PresentChoice(node);
      }, selection);

      if (!selection.Resolved)
      {
        yield break; // 시나리오 종료 등으로 미해결 종료.
      }

      if (node.Options != null && selection.Index >= 0 && selection.Index < node.Options.Count)
      {
        var option = node.Options[selection.Index];
        OnOptionSelected?.Invoke(option);
        context.NextOverride = option.NextNodeIdentifier;
      }

      ClearOptions();
    }

    /// <summary>
    /// 브랜치 체인 안에서 Quiz 노드를 실행한다. 정답/오답에 따른 다음 노드를
    /// <see cref="BranchChainContext.NextOverride"/> 로 전달한다.
    /// </summary>
    private IEnumerator ExecuteQuizNodeInBranch(ScenarioQuizNode node, BranchChainContext context)
    {
      if (_uiController.IsUnityNull())
      {
        Debug.LogWarning($"[ScenarioController] Quiz node '{node.Identifier}' cannot render choices because UI controller is missing.");
        context.NextOverride = ResolveQuizTarget(node, isCorrect: false);
        yield break;
      }

      var selection = new BranchOptionSelection();
      yield return RunBranchPrompt(() =>
      {
        _state = State.ExecutingQuiz;
        PresentQuiz(node);
      }, selection);

      if (!selection.Resolved)
      {
        yield break; // 시나리오 종료 등으로 미해결 종료.
      }

      bool isCorrect = selection.Index == node.CorrectIndex;
      ShowQuizFeedback(node, isCorrect);
      context.NextOverride = ResolveQuizTarget(node, isCorrect);

      ClearOptions();
    }

    /// <summary>
    /// 브랜치 내부의 fire-and-forget InvokeEvent 핸들러 코루틴을 실행하는 동안
    /// 전역 Advance 억제를 유지한다. 핸들러가 이 노드 실행 완료 이후에 끝나며
    /// 말미의 전역 Advance 를 호출하더라도 전역 시나리오 커서를 끌고 가지 않게 한다.
    /// </summary>
    private IEnumerator RunInvokeEventSuppressed(ScenarioInvokeEventNode invoke)
    {
      _globalAdvanceSuppressionDepth++;
      try
      {
        yield return ExecuteInvokeEventNode(invoke);
      }
      finally
      {
        _globalAdvanceSuppressionDepth--;
      }
    }

    private IEnumerator WaitForAny(List<BranchCompletionTracker> trackers)
    {
      // Unity Coroutine 핸들은 완료 시 null 이 되지 않으므로,
      // 브랜치 완료 플래그(tracker)를 폴링하여 하나라도 끝나면 반환한다.
      if (trackers == null || trackers.Count == 0)
      {
        yield break;
      }

      while (true)
      {
        for (int i = 0; i < trackers.Count; i++)
        {
          if (trackers[i] != null && trackers[i].Completed)
          {
            yield break;
          }
        }
        yield return null;
      }
    }

    private List<int> GetActivePlayerIds()
    {
      var ids = new List<int>();

      // 서버가 실제로 구동 중일 때만 ServerManager.Clients 를 신뢰한다.
      // 순수 클라이언트에서도 ServerManager.Clients 는 null 이 아니라 "빈 딕셔너리"이므로
      // null 검사만으로 분기하면 클라이언트에서 플레이어 목록이 항상 비게 된다.
      if (InstanceFinder.IsServerStarted)
      {
        var serverClients = InstanceFinder.ServerManager?.Clients;
        if (serverClients != null)
        {
          foreach (var kvp in serverClients)
          {
            if (kvp.Value != null)
            {
              ids.Add((int)kvp.Value.ClientId);
            }
          }
        }
      }
      else if (InstanceFinder.ClientManager != null)
      {
        // 클라이언트 컨텍스트: FishNet ShareIds 가 활성화되어 있으면 전체 접속자 목록을,
        // 아니면 최소한 로컬 커넥션이라도 확보한다.
        var sharedClients = InstanceFinder.ClientManager.Clients;
        if (sharedClients != null && sharedClients.Count > 0)
        {
          foreach (var kvp in sharedClients)
          {
            if (kvp.Value != null)
            {
              ids.Add((int)kvp.Value.ClientId);
            }
          }
        }
        else
        {
          var localConn = InstanceFinder.ClientManager.Connection;
          if (localConn != null)
          {
            ids.Add((int)localConn.ClientId);
          }
        }
      }

      // 모든 피어에서 동일한 순서를 보장한다(딕셔너리 순회 순서에 의존하지 않도록).
      ids.Sort();
      return ids;
    }

    /// <summary>
    /// 병렬 브랜치 할당용 결정적 난수 생성기를 만든다.
    /// 모든 피어가 동일 그래프/노드/방문 순서를 공유하므로, 이를 시드로 쓰면
    /// RandomOneAll/SpreadRandom 할당이 피어 간에 일치한다.
    /// (로컬 UnityEngine.Random 을 쓰면 피어마다 다른 플레이어가 배정되는 버그가 발생한다.)
    /// </summary>
    private System.Random CreateDeterministicAllocationRandom(ScenarioParallelNode node)
    {
      int seed = 17;
      unchecked
      {
        // string.GetHashCode 는 런타임/프로세스별로 달라질 수 있으므로
        // 안정적인 FNV-1a 해시를 사용한다.
        seed = seed * 31 + StableStringHash(_currentGraph?.Identifier);
        seed = seed * 31 + StableStringHash(node?.Identifier);

        // 같은 노드를 반복 방문해도 매번 같은 결과가 나오지 않도록 방문 횟수를 섞는다.
        var visits = node != null ? GetNodeVisitOrders(node.Identifier) : null;
        seed = seed * 31 + (visits?.Count ?? 0);
      }

      return new System.Random(seed);
    }

    /// <summary>프로세스/플랫폼과 무관하게 동일한 값을 내는 문자열 해시(FNV-1a 32bit).</summary>
    private static int StableStringHash(string text)
    {
      if (string.IsNullOrEmpty(text))
      {
        return 0;
      }

      unchecked
      {
        uint hash = 2166136261u;
        for (int i = 0; i < text.Length; i++)
        {
          hash ^= text[i];
          hash *= 16777619u;
        }
        return (int)hash;
      }
    }

    private bool TryAllocateParallel(ScenarioParallelNode node, List<int> players, Dictionary<ScenarioParallelBranch, int?> allocation)
    {
      allocation.Clear();

      var branches = node.Branches?.ToList() ?? new List<ScenarioParallelBranch>();
      if (branches.Count == 0)
      {
        return true;
      }

      var playerPool = players?.ToList() ?? new List<int>();

      switch (node.AllocationType)
      {
        case ScenarioParallelAllocationType.SelfAll:
        {
          int? target = _scenarioOwnerClientId;
          if (target != null && !branches.All(branch => IsPlayerEligibleForBranch(branch, target.Value)))
          {
            target = null;
          }

          if (target == null && playerPool.Count > 0)
          {
            target = playerPool
                .Where(clientId => branches.All(branch => IsPlayerEligibleForBranch(branch, clientId)))
                .Select(clientId => (int?)clientId)
                .FirstOrDefault();
          }

          if (target == null)
          {
            return HandleParallelMismatch(node, branches.Count, playerPool.Count, playerPool, allocation, assignNull: true);
          }

          foreach (var branch in branches)
          {
            allocation[branch] = target;
          }
          return true;
        }
        case ScenarioParallelAllocationType.RandomOneAll:
        {
          var eligiblePlayers = playerPool.Where(clientId => branches.All(branch => IsPlayerEligibleForBranch(branch, clientId))).ToList();
          if (eligiblePlayers.Count == 0)
          {
            return HandleParallelMismatch(node, branches.Count, 0, playerPool, allocation, assignNull: true);
          }

          // 피어 간 동일한 배정을 위해 결정적 난수를 사용한다.
          var allocationRandom = CreateDeterministicAllocationRandom(node);
          var pick = eligiblePlayers[allocationRandom.Next(0, eligiblePlayers.Count)];
          foreach (var branch in branches)
          {
            allocation[branch] = pick;
          }
          return true;
        }
        case ScenarioParallelAllocationType.ByRole:
        {
          // 각 브랜치를 자격에 맞는 서로 다른 플레이어에게 1:1로 배정한다.
          // 자격 후보가 적은 브랜치부터 그리디로 처리하여 결정적 매칭을 보장한다.
          var assignedPlayers = new HashSet<int>();

          // 브랜치별 자격 후보 목록을 미리 계산.
          var candidatesByBranch = new Dictionary<ScenarioParallelBranch, List<int>>();
          foreach (var branch in branches)
          {
            candidatesByBranch[branch] = playerPool
                .Where(clientId => IsPlayerEligibleForBranch(branch, clientId))
                .ToList();
          }

          // 후보 수가 적은(제약이 강한) 브랜치부터 처리. 동률은 원래 정의 순서 유지(안정 정렬).
          var orderedBranches = branches
              .Select((branch, index) => (branch, index))
              .OrderBy(entry => candidatesByBranch[entry.branch].Count)
              .ThenBy(entry => entry.index)
              .Select(entry => entry.branch)
              .ToList();

          bool anyUnassigned = false;
          foreach (var branch in orderedBranches)
          {
            int? pick = candidatesByBranch[branch]
                .Where(clientId => !assignedPlayers.Contains(clientId))
                .Select(clientId => (int?)clientId)
                .FirstOrDefault();

            if (pick != null)
            {
              assignedPlayers.Add(pick.Value);
              allocation[branch] = pick;
            }
            else
            {
              allocation[branch] = null;
              anyUnassigned = true;
            }
          }

          if (anyUnassigned)
          {
            // 미배정 브랜치가 존재하면 미스매치 정책에 위임한다.
            // (Ignore: null 배정 그대로 스킵 / Panic: 중단 / Reallocation: 라운드로빈 재배정)
            int unmatchedCount = allocation.Count(kvp => kvp.Value == null);
            if (node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Panic)
            {
              Debug.LogWarning($"[ScenarioController] ByRole allocation panic: {unmatchedCount} branch(es) have no eligible/free player.");
              return false;
            }

            if (node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Reallocation)
            {
              return HandleParallelMismatch(node, branches.Count, playerPool.Count, playerPool, allocation, assignNull: false);
            }

            // Ignore: null 배정 유지(해당 브랜치 스킵).
            Debug.LogWarning($"[ScenarioController] ByRole allocation ignored {unmatchedCount} unmatched branch(es).");
          }

          return true;
        }
        case ScenarioParallelAllocationType.SpreadRandom:
        {
          // 피어 간 동일한 배정을 위해 결정적 난수로 셔플한다.
          Shuffle(playerPool, CreateDeterministicAllocationRandom(node));
          goto case ScenarioParallelAllocationType.SpreadOrdinary;
        }
        case ScenarioParallelAllocationType.SpreadOrdinary:
        {
          if (playerPool.Count == 0)
          {
            return HandleParallelMismatch(node, branches.Count, 0, playerPool, allocation, assignNull: true);
          }

          int playerCount = playerPool.Count;
          bool playersFewer = playerCount < branches.Count;

          if (playersFewer && node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Panic)
          {
            Debug.LogWarning($"[ScenarioController] Parallel allocation panic: branches {branches.Count}, players {playerCount}.");
            return false;
          }

          for (int i = 0; i < branches.Count; i++)
          {
            var branch = branches[i];
            var eligiblePlayers = playerPool.Where(clientId => IsPlayerEligibleForBranch(branch, clientId)).ToList();

            int? assigned;

            if (eligiblePlayers.Count == 0)
            {
              if (node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Panic)
              {
                Debug.LogWarning($"[ScenarioController] Parallel allocation panic: branch '{branch.Identifier}' has no eligible player.");
                return false;
              }

              assigned = null;
            }
            else if (playersFewer && node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Ignore && i >= playerCount)
            {
              assigned = null;
            }
            else
            {
              assigned = eligiblePlayers[i % eligiblePlayers.Count];
            }

            allocation[branch] = assigned;
          }

          return true;
        }
        default:
          return false;
      }
    }

    private bool HandleParallelMismatch(ScenarioParallelNode node, int branchCount, int playerCount, List<int> playerPool, Dictionary<ScenarioParallelBranch, int?> allocation, bool assignNull)
    {
      switch (node.WhenBranchingPlayerNotMatched)
      {
        case ScenarioParallelMismatchHandling.Panic:
          Debug.LogWarning($"[ScenarioController] Parallel allocation failed: branches {branchCount}, players {playerCount}.");
          return false;
        case ScenarioParallelMismatchHandling.Ignore:
          if (assignNull)
          {
            foreach (var branch in node.Branches ?? Array.Empty<ScenarioParallelBranch>())
            {
              allocation[branch] = null;
            }
          }
          Debug.LogWarning($"[ScenarioController] Parallel allocation ignored mismatch: branches {branchCount}, players {playerCount}.");
          return true;
        case ScenarioParallelMismatchHandling.Reallocation:
          if (playerCount == 0)
          {
            Debug.LogWarning($"[ScenarioController] Parallel allocation cannot reallocate with zero players. branches {branchCount}.");
            return false;
          }

          for (int i = 0; i < node.Branches.Count; i++)
          {
            allocation[node.Branches[i]] = playerPool[i % playerCount];
          }

          Debug.LogWarning($"[ScenarioController] Parallel allocation reallocated with round-robin. branches {branchCount}, players {playerCount}.");
          return true;
        default:
          return false;
      }
    }

    private bool IsPlayerEligibleForBranch(ScenarioParallelBranch branch, int clientId)
    {
      if (branch == null)
      {
        return true;
      }

      if (branch.RequiredPlayerTags != null && branch.RequiredPlayerTags.Count > 0)
      {
        if (!UserDescriptorService.TryGetByClientId(clientId, out var session)
            || session == null
            || string.IsNullOrWhiteSpace(session.Identifier))
        {
          return false;
        }

        var requiredTags = branch.RequiredPlayerTags
            .Where(requiredTag => !string.IsNullOrWhiteSpace(requiredTag))
            .ToList();

        if (requiredTags.Count == 0)
        {
          return true;
        }

        if (branch.RequiredPlayerTagsMatchMode == ScenarioPlayerTagMatchMode.Any)
        {
          bool hasAnyTag = requiredTags.Any(requiredTag => PlayerTagService.HasTag(session.Identifier, requiredTag));
          if (!hasAnyTag)
          {
            return false;
          }
        }
        else
        {
          foreach (var requiredTag in requiredTags)
          {
            if (!PlayerTagService.HasTag(session.Identifier, requiredTag))
            {
              return false;
            }
          }
        }
      }

      if (branch.ForbiddenPlayerTags != null && branch.ForbiddenPlayerTags.Count > 0)
      {
        if (!UserDescriptorService.TryGetByClientId(clientId, out var session)
            || session == null
            || string.IsNullOrWhiteSpace(session.Identifier))
        {
          return false;
        }

        foreach (var forbiddenTag in branch.ForbiddenPlayerTags)
        {
          if (string.IsNullOrWhiteSpace(forbiddenTag))
          {
            continue;
          }

          if (PlayerTagService.HasTag(session.Identifier, forbiddenTag))
          {
            return false;
          }
        }
      }

      return true;
    }

    private static void Shuffle(IList<int> list, System.Random random)
    {
      for (int i = list.Count - 1; i > 0; i--)
      {
        int j = random.Next(0, i + 1);
        (list[i], list[j]) = (list[j], list[i]);
      }
    }

    private bool EvaluateValidator(ScenarioValidatorNode node)
    {
      return EvaluateValidator(node, out _);
    }

    private bool EvaluateValidator(ScenarioValidatorNode node, out string failureReason)
    {
      failureReason = null;
      var rootConditions = node.RootConditions;
      if (rootConditions == null || rootConditions.Count == 0)
      {
        failureReason = "rootConditions is empty.";
        return false;
      }

      for (int i = 0; i < rootConditions.Count; i++)
      {
        var rootCondition = rootConditions[i];
        if (rootCondition == null)
        {
          continue;
        }

        if (!EvaluateValidatorRootCondition(rootCondition, out var rootReason))
        {
          failureReason = $"rootCondition[{i}] failed: {rootReason}";
          return false;
        }
      }

      bool hasAnyCondition = rootConditions.Any(each => each != null);
      if (!hasAnyCondition)
      {
        failureReason = "rootConditions has no valid condition entries.";
        return false;
      }

      return true;
    }

    private bool EvaluateValidatorRootCondition(ScenarioValidatorRootCondition rootCondition, out string failureReason)
    {
      failureReason = null;
      // 서버 구동 중에는 서버의 접속자 목록을 사용한다.
      // (ClientManager.Clients 는 전용 서버에서 0이고, ShareIds 비활성 시 클라이언트에서도 부정확하다.)
      var clientCount = InstanceFinder.IsServerStarted
          ? InstanceFinder.ServerManager?.Clients?.Count ?? 0
          : InstanceFinder.ClientManager?.Clients?.Count ?? 0;

      switch (rootCondition.Condition)
      {
        case ScenarioValidatorCondition.PlayerCountEqual:
          if (clientCount == rootCondition.TargetCount)
          {
            return true;
          }
          failureReason = $"player count {clientCount} is not equal to target {rootCondition.TargetCount}.";
          return false;
        case ScenarioValidatorCondition.PlayerCountNotEqual:
          if (clientCount != rootCondition.TargetCount)
          {
            return true;
          }
          failureReason = $"player count {clientCount} is equal to target {rootCondition.TargetCount}.";
          return false;
        case ScenarioValidatorCondition.PlayerCountLessThan:
          if (clientCount < rootCondition.TargetCount)
          {
            return true;
          }
          failureReason = $"player count {clientCount} is not less than target {rootCondition.TargetCount}.";
          return false;
        case ScenarioValidatorCondition.PlayerCountLessThanOrEqual:
          if (clientCount <= rootCondition.TargetCount)
          {
            return true;
          }
          failureReason = $"player count {clientCount} is greater than target {rootCondition.TargetCount}.";
          return false;
        case ScenarioValidatorCondition.PlayerCountGreaterThan:
          if (clientCount > rootCondition.TargetCount)
          {
            return true;
          }
          failureReason = $"player count {clientCount} is not greater than target {rootCondition.TargetCount}.";
          return false;
        case ScenarioValidatorCondition.PlayerCountGreaterThanOrEqual:
          if (clientCount >= rootCondition.TargetCount)
          {
            return true;
          }
          failureReason = $"player count {clientCount} is less than target {rootCondition.TargetCount}.";
          return false;
        case ScenarioValidatorCondition.RegistryContains:
        {
          var rules = rootCondition.ValidationRules;
          if (rules == null || rules.Count == 0)
          {
            failureReason = "validationRules is empty for RegistryContains condition.";
            return false;
          }

          for (int i = 0; i < rules.Count; i++)
          {
            var rule = rules[i];
            if (rule == null)
            {
              continue;
            }

            if (rule.Type != ScenarioValidatorRuleType.Registry)
            {
              failureReason = $"rule[{i}] has unsupported type '{rule.Type}'.";
              return false;
            }

            if (rule.Condition != ScenarioValidatorRuleCondition.Contains)
            {
              failureReason = $"rule[{i}] has unsupported condition '{rule.Condition}'.";
              return false;
            }

            var ruleIdentifier = rule.RegistryIdentifier?.Trim();
            if (string.IsNullOrWhiteSpace(ruleIdentifier))
            {
              failureReason = $"rule[{i}] registryIdentifier is null or empty.";
              return false;
            }

            if (!Registry.Registry.Contains(rule.RegistryType, ruleIdentifier))
            {
              failureReason = $"rule[{i}] identifier '{ruleIdentifier}' is not registered in {rule.RegistryType}.";
              return false;
            }
          }

          return true;
        }
        case ScenarioValidatorCondition.PlayerAssignedTag:
        {
          var tag = rootCondition.PlayerTag?.Trim();
          if (string.IsNullOrWhiteSpace(tag))
          {
            failureReason = "playerTag is null or empty.";
            return false;
          }

          var users = UserDescriptorService.GetAll();
          if (users == null || users.Count == 0)
          {
            failureReason = "no registered users found for player tag validation.";
            return false;
          }

          switch (rootCondition.PlayerScope)
          {
            case ScenarioValidatorPlayerScope.Any:
              if (users.Values.Any(each => each != null
                                           && !string.IsNullOrWhiteSpace(each.Identifier)
                                           && PlayerTagService.HasTag(each.Identifier, tag)))
              {
                return true;
              }
              failureReason = $"no registered player has tag '{tag}'.";
              return false;
            case ScenarioValidatorPlayerScope.All:
            {
              var missingPlayer = users.Values.FirstOrDefault(each => each == null
                                                                      || string.IsNullOrWhiteSpace(each.Identifier)
                                                                      || !PlayerTagService.HasTag(each.Identifier, tag));
              if (missingPlayer == null)
              {
                return true;
              }

              var missingLabel = !string.IsNullOrWhiteSpace(missingPlayer.DisplayName)
                  ? missingPlayer.DisplayName
                  : missingPlayer.Identifier ?? "<unknown>";
              failureReason = $"player '{missingLabel}' does not have required tag '{tag}'.";
              return false;
            }
            case ScenarioValidatorPlayerScope.Owner:
            {
              if (_scenarioOwnerClientId == null)
              {
                failureReason = "owner client id is not assigned for owner-scope tag validation.";
                return false;
              }

              if (!UserDescriptorService.TryGetByClientId(_scenarioOwnerClientId.Value, out var owner)
                  || owner == null
                  || string.IsNullOrWhiteSpace(owner.Identifier))
              {
                failureReason = $"owner descriptor not found for clientId {_scenarioOwnerClientId.Value}.";
                return false;
              }

              if (PlayerTagService.HasTag(owner.Identifier, tag))
              {
                return true;
              }

              var ownerLabel = !string.IsNullOrWhiteSpace(owner.DisplayName)
                  ? owner.DisplayName
                  : owner.Identifier;
              failureReason = $"owner player '{ownerLabel}' does not have tag '{tag}'.";
              return false;
            }
            default:
              failureReason = $"unknown player scope '{rootCondition.PlayerScope}'.";
              return false;
          }
        }
        default:
          failureReason = $"unsupported validator condition '{rootCondition.Condition}'.";
          return false;
      }
    }

    /// <summary>
    /// 채팅/콘솔에 텍스트를 출력하는 노드를 실행한다.
    /// 시그널/이벤트 발생을 눈으로 확인하는 디버깅·데모 용도. 대기 없이 즉시 진행한다.
    /// </summary>
    private void ExecuteChatPrintNode(ScenarioChatPrintNode node)
    {
      _state = State.ExecutingInvokeEvent;

      if (node == null)
      {
        Advance();
        return;
      }

      var message = node.Message ?? string.Empty;

      if ((node.Targets & ScenarioChatPrintTarget.UnityConsole) != 0)
      {
        Debug.Log($"[ScenarioController][ChatPrint] {message}");
      }

      if ((node.Targets & ScenarioChatPrintTarget.InGameChat) != 0)
      {
        if (node.Broadcast)
        {
          // 서버(또는 오프라인)만 전체 클라이언트로 브로드캐스트한다. 각 클라이언트도
          // 자기 그래프를 로컬 실행하므로, 브로드캐스트 중복을 막기 위해 서버 컨텍스트로 한정한다.
          if (InstanceFinder.IsServerStarted || InstanceFinder.IsOffline)
          {
            var chatService = ResolveChatService();
            if (chatService != null)
            {
              chatService.BroadcastSystemMessage(message);
            }
            else
            {
              // 채팅 서비스가 없으면 로컬 폴백.
              AppendSystemChatMessage(message);
            }
          }
        }
        else
        {
          // 로컬 전용: 각 피어가 자기 채팅창에만 출력한다.
          AppendSystemChatMessage(message);
        }
      }

      Advance();
    }

    /// <summary>
    /// 인게임 채팅 명령어를 서버 권한으로 실행하는 노드를 실행한다.
    /// 서버(또는 오프라인) 컨텍스트에서만 실제 실행하고, 클라이언트는 진행만 한다
    /// (명령은 서버 권한 자원을 변경하므로 중복 실행을 방지). 대기 없이 즉시 진행한다.
    /// </summary>
    private void ExecuteExecuteCommandNode(ScenarioExecuteCommandNode node)
    {
      _state = State.ExecutingInvokeEvent;

      if (node == null || string.IsNullOrWhiteSpace(node.CommandLine))
      {
        Advance();
        return;
      }

      if (InstanceFinder.IsServerStarted || InstanceFinder.IsOffline)
      {
        var chatService = ResolveChatService();
        if (chatService == null)
        {
          Debug.LogWarning($"[ScenarioController] ExecuteCommand node '{node.Identifier}' could not resolve ChatService; command skipped.");
        }
        else if (!chatService.TryExecuteSystemCommand(node.CommandLine.Trim(), out var result))
        {
          Debug.LogWarning($"[ScenarioController] ExecuteCommand node '{node.Identifier}' failed: {result}");
        }
      }

      Advance();
    }

    private ChatService ResolveChatService()
    {
      if (_chatService != null)
      {
        return _chatService;
      }

      Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out _chatService);
      return _chatService;
    }

    private void ReportValidatorFailure(ScenarioValidatorNode node, string reason)
    {
      var resolvedReason = string.IsNullOrWhiteSpace(reason)
          ? "condition evaluated to false"
          : reason;
      var message = $"[ScenarioController] Validator failed at node '{node.Identifier}': {resolvedReason}";

      if ((node.FailureReportTargets & ScenarioValidatorFailureReportTarget.UnityConsole) != 0)
      {
        Debug.LogWarning(message);
      }

      if ((node.FailureReportTargets & ScenarioValidatorFailureReportTarget.InGameChat) != 0)
      {
        AppendSystemChatMessage($"Validator failed: {resolvedReason}");
      }
    }

    private void ReportValidatorBlocked(ScenarioValidatorNode node, string reason)
    {
      var resolvedReason = string.IsNullOrWhiteSpace(reason)
        ? "condition evaluated to false"
        : reason;
      var graphIdentifier = _currentGraph?.Identifier ?? "<unknown>";
      var message = $"Validator gate blocked scenario progress: graph='{graphIdentifier}', node='{node.Identifier}', reason={resolvedReason}";

      if ((_validatorBlockLogTargets & ScenarioValidatorBlockLogTarget.UnityConsole) != 0)
      {
        Debug.LogError($"[ScenarioController] {message}", this);
      }

      if ((_validatorBlockLogTargets & ScenarioValidatorBlockLogTarget.InGameChat) != 0)
      {
        AppendSystemChatMessage(message);
      }

      if ((_validatorBlockLogTargets & ScenarioValidatorBlockLogTarget.SessionLog) != 0)
      {
        GameLogService.WriteScenario($"ERROR: {message}", graphIdentifier);
      }
    }

    private void AppendSystemChatMessage(string message)
    {
      if (string.IsNullOrWhiteSpace(message))
      {
        return;
      }

      if (_chatUIController == null)
      {
        Registry.Registry.TryGet<ChatUIController>(RegistryType.UI, Registry.Registry.TypeKey<ChatUIController>(), out _chatUIController);
      }

      _chatUIController?.AppendMessage($"<color=#FFD700>[System]</color> {message}", true);
    }

    #endregion

    #region Helper Methods

    private void ClearOptions()
    {
      _activeOptions.Clear();
      _activeQuizNode = null;

      if (_hintUIController != null)
      {
        _hintUIController.ClearDialogueSelections();
      }
    }

    #endregion
  }
}
