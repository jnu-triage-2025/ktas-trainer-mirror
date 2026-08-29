using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Chat;
using MultiplayerInfrastructure.Command;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using MultiplayerInfrastructure.TTS;
using MultiplayerInfrastructure.UI;
using TriageTrainer.Entity;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>단일 노드 방문의 진입 및 이탈 시각.</summary>
  public readonly struct ScenarioNodeVisitTiming
  {
    public int Sequence { get; }
    public DateTime EnteredAt { get; }
    public DateTime? ExitedAt { get; }

    public ScenarioNodeVisitTiming(int sequence, DateTime enteredAt, DateTime? exitedAt)
    {
      Sequence = sequence;
      EnteredAt = enteredAt;
      ExitedAt = exitedAt;
    }
  }

  /// <summary>
  /// 시나리오 흐름을 제어합니다.
  /// UI 제어는 ScenarioPanelUIController에 위임합니다.
  /// </summary>
  public class ScenarioController : MonoBehaviour
  {
    #region Serialized Fields

    private static ScenarioController _instance;
    public static ScenarioController Instance => _instance;
    public static event Action<ScenarioController> InstanceAvailable;

    // NPC 이동과 수동 진입은 모두 메인 스레드에서 실행된다. 공통 버퍼로 일반적인 접지 검사에서
    // RaycastAll 배열 할당을 피한다. 충돌면이 이 수보다 많은 드문 경우에는 아래에서 정확성을
    // 우선해 RaycastAll로 다시 검사한다.
    private const int GroundRaycastHitCapacity = 32;
    private static readonly RaycastHit[] GroundRaycastHits = new RaycastHit[GroundRaycastHitCapacity];

    // WaitMode.All/Any 병렬 노드가 분기 완료를 기다리는 동안 외부 자동 진행이
    // 병렬 부모의 NextIdentifier로 건너뛰지 못하게 한다.
    private int _parallelAdvanceBlockDepth;

    [Header("References")]
    [SerializeField] private DialoguePanelUIController _uiController;
    [SerializeField] private MainCameraController _camController;
    [SerializeField] private InteractableObjectHintUIController _hintUIController;
    [SerializeField] private ScenarioTTSService _ttsService;
    [SerializeField] private AudioSource _ttsAudioSource;


    [Header("Concurrency (동시 실행 충돌)")]
    [Tooltip("두 개 이상의 시나리오 흐름이 동시에 대화창 UI(Dialogue/Choice/Quiz)를 점유하려 할 때의 처리 정책.")]
    [SerializeField]
    private ScenarioConcurrencyConflictPolicy _concurrencyConflictPolicy = ScenarioConcurrencyConflictPolicy.Warn;

    [Header("Validator Block Logging")]
    [Tooltip("Validator 게이트가 조건 미충족으로 대기를 시작할 때 상태를 기록할 대상.")]
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
    private readonly HashSet<string> _reportedTagGateBypasses = new HashSet<string>();
    private readonly Dictionary<string, string> _stateStore = new Dictionary<string, string>();
    private int _activeMainNodeVisitSequence;
    private int? _scenarioOwnerClientId;
    private ChatUIController _chatUIController;
    private ChatService _chatService;
    private Coroutine _dialogueAutoAdvanceRoutine;
    private CancellationTokenSource _inlineTTSPrewarmCancellation;
    private ExecutionMode _executionMode = ExecutionMode.Local;
    private bool _currentPresentationNodeRoleScoped;
    private readonly List<ScenarioOwnedActingNpc> _scenarioOwnedActingNpcs = new List<ScenarioOwnedActingNpc>();
    private readonly List<ScenarioOwnedWaypoint> _scenarioOwnedWaypoints = new List<ScenarioOwnedWaypoint>();
    private readonly Dictionary<int, int> _activeRoleBranchDepthByClientId = new Dictionary<int, int>();
    private readonly Stack<Action> _cleanupJournal = new Stack<Action>();
    private bool _isRevertingCleanupJournal;

    private sealed class ScenarioOwnedActingNpc
    {
      public GameObject GameObject;
      public bool DespawnOnScenarioEnd;
    }

    private sealed class ScenarioOwnedWaypoint
    {
      public GameObject GameObject;
      public bool DespawnOnScenarioEnd;
    }

    /// <summary>
    /// Local은 기존 오프라인/호환 실행, ServerAuthoritative는 서버만 그래프를 순회,
    /// ClientPresentation은 서버가 보낸 표시와 입력 보고만 담당한다.
    /// </summary>
    private enum ExecutionMode
    {
      Local,
      ServerAuthoritative,
      ClientPresentation,
    }

    private sealed class GraphVisitHistory
    {
      public readonly Dictionary<string, List<int>> NodeVisitOrders = new Dictionary<string, List<int>>();
      public readonly Dictionary<int, ScenarioNodeVisitTiming> VisitTimings = new Dictionary<int, ScenarioNodeVisitTiming>();
      public readonly Dictionary<int, List<string>> VisitNotes = new Dictionary<int, List<string>>();
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
      ExecutingNPCControl,
      ExecutingCameraTarget,
      ExecutingInvokeEvent,
      ExecutingValidator,
      ExecutingParallel,
      ExecutingQuestControl,
      ExecutingQuestWaypointHighlight,
      ExecutingQuestMark,
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
      ExecutingManualEntrypoint,
      ExecutingBedSnap,
      ExecutingReturnToOrigin,
      ExecutingLifecycle,
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

    /// <summary>
    /// 활성화된(재생 중인) 시나리오 그래프가 존재하는지 여부.
    /// </summary>
    /// <remarks>
    /// Local/ServerAuthoritative/ClientPresentation 모든 실행 모드에서 그래프가 시작되어
    /// 종료되지 않은 동안 true 이다(설정: StartScenarioInternal/BeginPresentationScenario,
    /// 해제: EndScenario/EndPresentationScenario). <see cref="IsActive"/> 는 현재 노드 실행 상태
    /// (_state) 기반이라 클라이언트 표시 모드나 즉시 진행 노드 체인 사이에서는 false 일 수
    /// 있으므로, "시나리오가 재생 중인가" 판정에는 이 프로퍼티를 사용해야 한다.
    /// </remarks>
    public bool HasActiveScenario => _currentGraph != null;
    public State CurrentState => _state;
    public IScenarioNode CurrentNode => _currentNode;
    public ScenarioGraph CurrentGraph => _currentGraph;

    /// <summary>
    /// B/C 환자 모니터 닫기 완료 신호의 승인 여부를 판정한다.
    ///
    /// <para>모니터 UI 활성화 여부(arm)는 모니터 쪽 서버 권한 상태가 관리하므로, 여기서는
    /// 실행 모드와 무관하게 '지금 이 그래프가 활성 상태이고, 발신자가 활성 역할 브랜치에
    /// 참여 중이며, 신호가 아직 올라가지 않았는가'만 검사한다. 호환 실행 경로(Local 모드,
    /// 그래프에 미지원 노드가 있어 릴레이가 권위 실행을 거부한 경우)에서도 호스트가 그래프를
    /// 실행하므로 모드로 거부하면 닫기 완료가 불가능해진다.</para>
    /// </summary>
    public bool CanAcceptPatientBCMonitorClose(int senderClientId, string normalizedSignal)
    {
      if (_currentGraph == null
          || (!string.Equals(_currentGraph.Identifier, "patient_a_critical", StringComparison.Ordinal)
              && !string.Equals(_currentGraph.Identifier, "patient_b_c_ct", StringComparison.Ordinal))
          || !_activeRoleBranchDepthByClientId.ContainsKey(senderClientId)
          || ScenarioInteractionSignals.IsRaised(normalizedSignal))
        return false;

      return true;
    }
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

    /// <summary>지정한 방문 순서의 노드 진입/이탈 시각을 반환한다.</summary>
    public bool TryGetNodeVisitTiming(string graphIdentifier, int sequence, out ScenarioNodeVisitTiming timing)
    {
      timing = default;
      return !string.IsNullOrWhiteSpace(graphIdentifier)
             && _graphVisitHistory.TryGetValue(graphIdentifier, out var history)
             && history != null
             && history.VisitTimings.TryGetValue(sequence, out timing);
    }

    /// <summary>지정한 노드 방문에서 발생한 흐름 변경/경고 메모를 반환한다.</summary>
    public IReadOnlyList<string> GetNodeVisitNotes(string graphIdentifier, int sequence)
    {
      if (!string.IsNullOrWhiteSpace(graphIdentifier)
          && _graphVisitHistory.TryGetValue(graphIdentifier, out var history)
          && history != null
          && history.VisitNotes.TryGetValue(sequence, out var notes))
      {
        return notes;
      }

      return Array.Empty<string>();
    }

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
      if (_instance != null && _instance != this)
      {
        // GameObject 전체를 파괴하므로, 같은 오브젝트에 붙은 다른 컴포넌트까지 함께 사라진다.
        // 조용히 지우면 그 컴포넌트들이 동작하지 않는 이유를 찾을 수 없다.
        Debug.LogWarning(
          $"[ScenarioController] 이미 인스턴스가 존재하여 중복 오브젝트 '{name}' 을 파괴합니다. "
          + $"기존 인스턴스='{_instance.name}'. 이 오브젝트에 붙은 다른 컴포넌트도 함께 제거됩니다.",
          this);
        Destroy(gameObject);
        return;
      }
      _instance = this;
      InstanceAvailable?.Invoke(this);
    }

    private void Reset()
    {
      // 컴포넌트를 처음 붙일 때 Preflight 정책을 안전한 기본값(콘솔/인게임챗 둘 다 경고 + 계속 진행)으로 초기화한다.
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
      if (InstanceFinder.ServerManager != null)
        InstanceFinder.ServerManager.OnRemoteConnectionState += HandleRemoteConnectionState;
    }

    private void OnDestroy()
    {
      CleanupScenarioActingNpcs();
      CleanupScenarioWaypoints();
      // 이벤트 구독 해제
      ScenarioInteractable.OnScenarioRequested -= HandleScenarioRequested;
      ScenarioTriggerZone.OnScenarioRequested -= HandleScenarioRequested;
      if (InstanceFinder.ServerManager != null)
        InstanceFinder.ServerManager.OnRemoteConnectionState -= HandleRemoteConnectionState;

      if (_instance == this)
      {
        _instance = null;
      }
    }

    /// <summary>
    /// 로스터 기반 시그널 카운터를 다시 평가하는 안전망 주기(초).
    /// 정상 경로는 신호 도착(Dispatch)과 접속 상태 변화에서 즉시 평가되므로, 이 폴링은 그 두
    /// 경로가 놓친 경우만 뒤늦게 복구하면 된다. 매 프레임 평가하면 활성 역할 로스터 조회와
    /// 그에 딸린 LINQ 할당이 시나리오 내내 프레임마다 반복된다.
    /// </summary>
    private const float DynamicThresholdRefreshIntervalSeconds = 1f;

    private float _nextDynamicThresholdRefreshTime;

    private void Update()
    {
      if (!IsActive || Time.time < _nextDynamicThresholdRefreshTime)
        return;

      _nextDynamicThresholdRefreshTime = Time.time + DynamicThresholdRefreshIntervalSeconds;
      ScenarioSignalCounters.RefreshDynamicThresholds();
    }

    private void HandleRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs args)
    {
      if (args.ConnectionState != RemoteConnectionState.Stopped || connection == null)
        return;

      if (_activeRoleBranchDepthByClientId.ContainsKey(connection.ClientId))
      {
        var message = $"[ScenarioController] Aborting scenario because client {connection.ClientId} disconnected during an assigned ByRole branch.";
        Debug.LogError(message, this);
        GameLogService.WriteScenario(message, _currentGraph?.Identifier);
        EndScenario();
        return;
      }

      // 역할 브랜치를 맡지 않은 인원이 나갔다면 시나리오는 계속 진행한다. 다만 활성 역할
      // 로스터가 줄었으므로, 남은 인원의 신호만으로 이미 충족된 로스터 기반 카운터가 있는지
      // 폴링을 기다리지 않고 이 자리에서 즉시 재평가한다.
      ScenarioSignalCounters.RefreshDynamicThresholds();
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
      _executionMode = ExecutionMode.Local;
      StartScenarioInternal(graph, startNodeIdentifier, ownerClientId);
    }

    /// <summary>현재 시나리오를 정리한 뒤 같은 그래프의 지정 진입점부터 다시 시작한다.</summary>
    public bool RestartScenario(string startNodeIdentifier = null)
    {
      if (_currentGraph == null || _executionMode == ExecutionMode.ClientPresentation)
        return false;

      var graph = _currentGraph;
      var ownerClientId = _scenarioOwnerClientId;
      var mode = _executionMode;
      if (!string.IsNullOrWhiteSpace(startNodeIdentifier)
          && TryFindManualEntrypoint(graph, startNodeIdentifier, out var manualEntrypoint))
      {
        startNodeIdentifier = manualEntrypoint.Identifier;
      }
      if (!string.IsNullOrWhiteSpace(startNodeIdentifier)
          && !graph.TryGetNode(startNodeIdentifier, out _))
      {
        Debug.LogWarning($"[ScenarioController] Restart node '{startNodeIdentifier}' was not found.");
        return false;
      }
      // 표시 전용 피어의 세션은 유지해야 새 시작 노드가 RPC로 즉시 갱신된다.
      // EndPresentationScenario를 먼저 보내면 재시작 뒤 BeginPresentationScenario를 다시
      // 보낼 대상 목록이 없어 원격 UI가 영구적으로 비활성 상태에 남는다.
      EndScenarioInternal(endAuthoritativePresentation: false);
      _executionMode = mode;
      if (mode == ExecutionMode.ServerAuthoritative)
        ScenarioNetworkRelay.DismissAuthoritativePresentation(graph.Identifier);
      StartScenarioInternal(graph, startNodeIdentifier, ownerClientId);
      return true;
    }

    /// <summary>서버 권위 시나리오를 시작한다. 그래프 순회는 이 서버 인스턴스에서만 수행한다.</summary>
    public void StartAuthoritativeScenario(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
    {
      if (!InstanceFinder.IsOffline && !InstanceFinder.IsServerStarted)
      {
        Debug.LogWarning("[ScenarioController] Rejected authoritative scenario start outside server context.");
        return;
      }

      _executionMode = ExecutionMode.ServerAuthoritative;
      StartScenarioInternal(graph, startNodeIdentifier, ownerClientId);
    }

    /// <summary>
    /// 서버가 시작을 통지한 클라이언트의 표시 전용 상태를 준비한다. 이 경로는 그래프를
    /// 실행하거나 RuntimeState를 지우지 않는다.
    /// </summary>
    public void BeginPresentationScenario(ScenarioGraph graph, int? ownerClientId)
    {
      if (graph == null)
      {
        Debug.LogWarning("[ScenarioController] Cannot begin presentation for a null graph.");
        return;
      }

      StopAllCoroutines();
      CancelDialogueAutoAdvance();
      _parallelAdvanceBlockDepth = 0;
      _executionMode = ExecutionMode.ClientPresentation;
      ScenarioParallelAssignmentState.ClearGraph(graph.Identifier);
      _currentGraph = graph;
      _currentNode = null;
      _currentPresentationNodeRoleScoped = false;
      _scenarioOwnerClientId = ownerClientId;
      _state = State.Inactive;
      _activeOptions.Clear();
      _activeQuizNode = null;
      _branchOptionInterceptor = null;
      _branchDialogueAdvanceInterceptors.Clear();
      _branchPromptActive = false;
      _activeRemoteBranchPromptClients.Clear();
      _branchDialogueActive = false;
      _activeRemoteBranchDialogueClients.Clear();
      _remoteBranchChoiceSelections.Clear();
      ResolveUIControllers();
      ResolveTTSService();
      _ttsService?.ConfigureScenarioVoiceProfiles(graph.TtsVoiceProfiles);
      StartInlineTTSPrewarm(graph);
      if (!_uiController.IsUnityNull())
      {
        _uiController.SetInteractableHintUI(_hintUIController);
        _uiController.StartScenario(this);
      }

      OnScenarioStarted?.Invoke();
    }

    /// <summary>서버가 보낸 노드를 클라이언트 UI에 표시한다.</summary>
    public void PresentAuthoritativeNode(string graphIdentifier, string nodeIdentifier, bool roleScoped = false)
    {
      if (_executionMode != ExecutionMode.ClientPresentation
          || _currentGraph == null
          || !string.Equals(_currentGraph.Identifier, graphIdentifier, StringComparison.Ordinal)
          || !_currentGraph.TryGetNode(nodeIdentifier, out var node))
      {
        Debug.LogWarning($"[ScenarioController] Ignored presentation node '{graphIdentifier}/{nodeIdentifier}' outside active presentation.");
        return;
      }

      switch (node)
      {
        case ScenarioDialogueNode dialogue:
          _currentNode = node;
          _currentPresentationNodeRoleScoped = roleScoped;
          PresentDialogueNode(dialogue);
          break;
        case ScenarioChoiceNode choice:
          _currentNode = node;
          _currentPresentationNodeRoleScoped = roleScoped;
          _state = State.ExecutingChoice;
          PresentChoice(choice);
          break;
        case ScenarioInvokeEventNode invokeEvent when roleScoped && invokeEvent.InvokeOnRoleClient:
          StartCoroutine(ExecutePresentationEvent(invokeEvent.EventIdentifier));
          break;
        case ScenarioQuestControlNode questControl:
          PresentQuestControlNode(questControl, roleScoped);
          break;
        case ScenarioQuestWaypointHighlightNode waypointHighlight:
          PresentQuestWaypointHighlightNode(waypointHighlight);
          break;
        case ScenarioQuestMarkNode questMark:
          ApplyQuestMarkNode(questMark);
          break;
      }
    }

    private IEnumerator ExecutePresentationEvent(string eventIdentifier)
    {
      if (string.IsNullOrWhiteSpace(eventIdentifier)
          || !ScenarioEventIdentifierRegistry.TryGetHandler(eventIdentifier, out var handler))
      {
        Debug.LogWarning($"[ScenarioController] No presentation handler registered for event '{eventIdentifier}'.");
        yield break;
      }

      IEnumerator routine = null;
      try
      {
        routine = handler?.Invoke();
      }
      catch (Exception ex)
      {
        Debug.LogException(ex);
      }

      if (routine != null)
        yield return StartCoroutine(routine);
    }

    /// <summary>
    /// 서버가 실행한 연출 전용 이벤트를 표시 피어에서도 실행한다.
    /// <see cref="ScenarioNetworkRelay.InvokePresentationEventAuthoritative"/> 가 호출한다.
    /// 그래프 순회는 서버가 담당하므로 이 경로는 노드를 진행시키지 않는다.
    /// </summary>
    public void RunPresentationEvent(string graphIdentifier, string eventIdentifier)
    {
      // 그래프를 직접 순회하는 피어는 같은 이벤트를 이미 스스로 실행했다. 여기서 또 실행하면
      // 같은 연출이 두 번 적용된다(RPC 는 ExcludeServer 로도 막지만 이중 방어).
      if (_executionMode != ExecutionMode.ClientPresentation)
        return;

      if (_currentGraph == null
          || !string.Equals(_currentGraph.Identifier, graphIdentifier, StringComparison.Ordinal))
        return;

      StartCoroutine(ExecutePresentationEvent(eventIdentifier));
    }

    /// <summary>서버가 종료를 통지한 클라이언트 표시 상태만 정리한다.</summary>
    public void EndPresentationScenario(string graphIdentifier)
    {
      if (_executionMode != ExecutionMode.ClientPresentation
          || _currentGraph == null
          || !string.Equals(_currentGraph.Identifier, graphIdentifier, StringComparison.Ordinal))
        return;

      CancelInlineTTSPrewarm();
      CancelDialogueAutoAdvance();
      _currentGraph = null;
      _currentNode = null;
      _currentPresentationNodeRoleScoped = false;
      _state = State.Inactive;
      _activeOptions.Clear();
      _activeQuizNode = null;
      if (!_uiController.IsUnityNull())
        _uiController.EndScenario();
      OnScenarioEnded?.Invoke();
    }

    internal bool TryAdvanceFromPresentation(int senderClientId, string graphIdentifier, string nodeIdentifier)
    {
      if (TryAdvanceBranchDialogue(senderClientId))
        return true;

      if (!CanAcceptPresentationInput(senderClientId, graphIdentifier, nodeIdentifier, State.ExecutingDialogue))
        return false;

      Advance();
      return true;
    }

    internal bool TrySelectOptionFromPresentation(int senderClientId, string graphIdentifier, string nodeIdentifier, int optionIndex)
    {
      if (TryResolveRemoteBranchChoice(senderClientId, graphIdentifier, nodeIdentifier, optionIndex))
        return true;

      if (!CanAcceptPresentationInput(senderClientId, graphIdentifier, nodeIdentifier, State.ExecutingChoice))
        return false;

      SelectOption(optionIndex);
      return true;
    }

    /// <summary>
    /// 로컬 대화 UI의 다음 진행 요청을 처리한다. 서버와 클라이언트를 겸하는 호스트에서는
    /// 권위 상태기가 직접 노출되므로, 이 진입점에서만 owner 정책을 검사한다. 내부 자동 진행은
    /// <see cref="Advance"/>를 계속 사용하여 원격 소유 시나리오도 서버에서 정상 진행한다.
    /// </summary>
    public void SubmitLocalAdvance()
    {
      if (TryAdvanceBranchDialogue(GetLocalClientId()))
        return;

      if (_executionMode == ExecutionMode.ServerAuthoritative && !IsLocalScenarioOwner())
      {
        Debug.LogWarning("[ScenarioController] Ignored authoritative advance from a non-owner host UI.");
        return;
      }

      Advance();
    }

    /// <summary>로컬 대화 UI의 선택 요청을 owner 정책에 따라 처리한다.</summary>
    public void SubmitLocalOptionSelection(int index)
    {
      if (_executionMode == ExecutionMode.ServerAuthoritative && !IsLocalScenarioOwner())
      {
        Debug.LogWarning("[ScenarioController] Ignored authoritative choice selection from a non-owner host UI.");
        return;
      }

      SelectOption(index);
    }

    private bool IsLocalScenarioOwner()
    {
      if (!_scenarioOwnerClientId.HasValue)
        return true;

      var localConnection = InstanceFinder.ClientManager?.Connection;
      return localConnection != null && localConnection.ClientId == _scenarioOwnerClientId.Value;
    }

    private bool CanAcceptPresentationInput(int senderClientId, string graphIdentifier, string nodeIdentifier, State requiredState)
    {
      if (_executionMode != ExecutionMode.ServerAuthoritative
          || _currentGraph == null
          || _currentNode == null
          || _state != requiredState
          || !string.Equals(_currentGraph.Identifier, graphIdentifier, StringComparison.Ordinal)
          || !string.Equals(_currentNode.Identifier, nodeIdentifier, StringComparison.Ordinal))
        return false;

      return !_scenarioOwnerClientId.HasValue || _scenarioOwnerClientId.Value == senderClientId;
    }

    private int GetLocalClientId()
    {
      var connection = InstanceFinder.ClientManager?.Connection;
      return connection != null && connection.IsValid ? connection.ClientId : int.MinValue;
    }

    private bool TryAdvanceBranchDialogue(int clientId)
    {
      if (!_branchDialogueAdvanceInterceptors.TryGetValue(clientId, out var advance)
          && !_branchDialogueAdvanceInterceptors.TryGetValue(int.MinValue, out advance))
        return false;

      advance();
      return true;
    }

    private void StartScenarioInternal(ScenarioGraph graph, string startNodeIdentifier, int? ownerClientId)
    {
      if (graph == null)
      {
        Debug.LogError("[ScenarioController] Cannot start scenario with null graph");
        return;
      }

      // 이전 시나리오 실행에서 남은 코루틴(병렬 브랜치 등)이 있으면 새 시나리오 시작 전에 정리한다.
      StopAllCoroutines();
      CancelDialogueAutoAdvance();
      RevertTrackedChanges();
      if (!PrepareScenarioOwnedObjects(graph))
        return;
      // StopAllCoroutines 로 강제 종료된 브랜치 코루틴은 finally 가 실행되지 않아
      // 억제 카운터가 불균형 상태로 남을 수 있으므로 명시적으로 초기화한다.
      _globalAdvanceSuppressionDepth = 0;
      _parallelAdvanceBlockDepth = 0;
      _activeRoleBranchDepthByClientId.Clear();
      ResetNodeVisitOrders(graph.Identifier);
      ScenarioInteractionSignals.ClearAllInternalSignals();
      ScenarioInteractionSignals.ClearAllRaisedSignals();
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      ScenarioConditionalSignalListeners.ClearAll();
      ScenarioEntityStateSignalBindings.ClearAll();
      ScenarioSignalCounters.ClearAll();
      // 트리거 존의 중복 방지 상태는 한 번의 실행 안에서만 의미가 있다. 되돌리지 않으면
      // 두 번째 실행에서 존 진입 신호가 다시 올라가지 않아 그 신호를 기다리는 게이트가 막힌다.
      ScenarioTriggerZone.ResetAllForNewScenarioRun();

      // 이전 시나리오에서 남았을 수 있는 모든 타이머/표시를 새 시나리오 시작 시 정리한다.
      ScenarioTimeRelay.ClearAllAuthoritative();

      _currentGraph = graph;
      _scenarioOwnerClientId = ownerClientId;
      ScenarioNetworkRelay.ConfigureClientSignalAuthorization(graph);
      ScenarioParallelAssignmentState.ClearGraph(graph.Identifier);

      // 시나리오가 요구하는 퀘스트 정의 include를 선로딩한다.
      QuestDefinitionRegistry.EnsureIncludesLoaded(graph.QuestDefinitionIncludes);

      ResolveUIControllers();
      ResolveTTSService();
      _ttsService?.ConfigureScenarioVoiceProfiles(graph.TtsVoiceProfiles);
      StartInlineTTSPrewarm(graph);

      // 이전 실행이 비상호작용 대화 fade 도중 중단된 경우 남은 UI 상태를 정리한다.
      if (!_uiController.IsUnityNull())
        _uiController.HideDisinteractableDialogue();

      // 새 시나리오 시작은 이전 실행을 강제 정리한 직후이므로, 대화창 점유 상태를 초기화한다.
      // (이전 실행이 EndScenario 를 거치지 않고 덮어써진 경우, 스테일 점유자가 새 시나리오
      //  자신의 대화 노드를 오탐(충돌)하게 만드는 것을 방지한다.)
      _branchPromptActive = false;
      _activeRemoteBranchPromptClients.Clear();
      _branchDialogueActive = false;
      _activeRemoteBranchDialogueClients.Clear();
      _remoteBranchChoiceSelections.Clear();
      if (!_uiController.IsUnityNull())
        _uiController.ClearDialogueOwner();

      // 시작 노드 찾기
      string startId = startNodeIdentifier;
      if (string.IsNullOrEmpty(startId))
      {
        // defaultEntrypoint가 선언되어 있으면 우선 사용
        if (!string.IsNullOrEmpty(graph.DefaultEntrypoint))
        {
          startId = graph.DefaultEntrypoint;
        }
      }

      if (!graph.TryGetNode(startId, out var startNode))
      {
        Debug.LogError($"[ScenarioController] Start node '{startId}' not found");
        CleanupScenarioActingNpcs(forceDespawn: true);
        CleanupScenarioWaypoints(forceDespawn: true);
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

    /// <summary>
    /// 시나리오 종료
    /// </summary>
    public void EndScenario()
    {
      EndScenarioInternal(endAuthoritativePresentation: true);
    }

    private void EndScenarioInternal(bool endAuthoritativePresentation)
    {
      CancelInlineTTSPrewarm();
      CompleteNodeVisit(_activeMainNodeVisitSequence);
      CompleteOpenNodeVisits();
      _activeMainNodeVisitSequence = 0;
      // 로그 기록을 위해 그래프 ID를 먼저 캡처 (_currentGraph는 이후 null로 초기화됨)
      string endingGraphId = _currentGraph?.Identifier;
      if (_executionMode == ExecutionMode.ServerAuthoritative && !string.IsNullOrEmpty(endingGraphId))
      {
        if (endAuthoritativePresentation)
          ScenarioNetworkRelay.EndAuthoritativePresentation(endingGraphId);
        ScenarioParallelAssignmentState.ClearGraph(endingGraphId);
      }
      CancelDialogueAutoAdvance();

      // 아직 진행 중인 시나리오 코루틴(특히 WaitMode.None 으로 전역 시나리오보다 오래
      // 살아남는 병렬 브랜치)을 모두 정리한다. 이를 누락하면 그래프가 해제된 뒤에도
      // 브랜치 체인이 계속 돌면서 _currentGraph 역참조에서 NullReferenceException 이 발생한다.
      StopAllCoroutines();
      RevertTrackedChanges();
      _parallelAdvanceBlockDepth = 0;
      ScenarioInteractionSignals.ClearAllInternalSignals();
      ScenarioInteractionSignals.ClearAllRaisedSignals();
      ScenarioConditionalSignalListeners.ClearAll();
      ScenarioEntityStateSignalBindings.ClearAll();
      ScenarioSignalCounters.ClearAll();
      ScenarioNetworkRelay.ClearClientSignalAuthorization();

      // 시나리오가 남긴 모든 타이머/표시를 정리한다.
      // 명시적 정리 없이 종료(또는 조기/오류 종료)하더라도 다음 시나리오로 새어 나가지 않게 한다.
      ScenarioTimeRelay.ClearAllAuthoritative();
      CleanupScenarioActingNpcs();
      CleanupScenarioWaypoints();

      _currentGraph = null;
      _currentNode = null;
      _state = State.Inactive;
      // 강제 종료된 브랜치 코루틴의 finally 가 실행되지 않을 수 있으므로 억제 카운터를 초기화한다.
      _globalAdvanceSuppressionDepth = 0;
      _activeRoleBranchDepthByClientId.Clear();
      // 브랜치 Choice/Quiz 대기 중 종료된 경우 남은 인터셉터/프롬프트 상태를 정리한다.
      // (StopAllCoroutines 로 강제 종료된 프롬프트 코루틴의 finally 가 실행되지 않을 수 있음)
      _branchOptionInterceptor = null;
      _branchDialogueAdvanceInterceptors.Clear();
      _branchPromptActive = false;
      _activeRemoteBranchPromptClients.Clear();
      _branchDialogueActive = false;
      _activeRemoteBranchDialogueClients.Clear();
      _remoteBranchChoiceSelections.Clear();

      ClearOptions();
      _stateStore.Clear();
      _executionMode = ExecutionMode.Local;

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

    private bool PrepareScenarioOwnedObjects(ScenarioGraph graph)
    {
      if (!PrepareScenarioActingNpcs(graph))
        return false;
      if (PrepareScenarioWaypoints(graph))
        return true;

      CleanupScenarioActingNpcs(forceDespawn: true);
      return false;
    }

    private bool PrepareScenarioActingNpcs(ScenarioGraph graph)
    {
      CleanupScenarioActingNpcs();
      if (graph?.ActingNpcs == null || graph.ActingNpcs.Count == 0)
        return true;

      for (int i = 0; i < graph.ActingNpcs.Count; i++)
      {
        var actingNpc = graph.ActingNpcs[i];
        if (actingNpc == null || !actingNpc.SpawnOnStart)
          continue;
        if (!TrySpawnScenarioActingNpc(graph, actingNpc, out var error))
        {
          Debug.LogError($"[ScenarioController] Failed to spawn start actingNpc '{actingNpc.Identifier}': {error}");
          CleanupScenarioActingNpcs(forceDespawn: true);
          return false;
        }
      }

      return true;
    }

    private bool PrepareScenarioWaypoints(ScenarioGraph graph)
    {
      CleanupScenarioWaypoints();
      if (graph?.Waypoints == null || graph.Waypoints.Count == 0)
        return true;

      foreach (var waypoint in graph.Waypoints)
      {
        if (waypoint == null)
          continue;
        if (string.IsNullOrWhiteSpace(waypoint.Identifier))
        {
          Debug.LogError("[ScenarioController] Scenario waypoint identifier is required.");
          CleanupScenarioWaypoints(forceDespawn: true);
          return false;
        }
        if (WaypointAnchor.TryGet(waypoint.Identifier, out _))
        {
          Debug.LogError($"[ScenarioController] Waypoint '{waypoint.Identifier}' already exists in the scene.");
          CleanupScenarioWaypoints(forceDespawn: true);
          return false;
        }

        var target = new GameObject($"ScenarioWaypoint_{waypoint.Identifier}");
        target.transform.SetPositionAndRotation(
          new Vector3(waypoint.PositionX, waypoint.PositionY, waypoint.PositionZ),
          Quaternion.Euler(waypoint.RotationX, waypoint.RotationY, waypoint.RotationZ));
        var anchor = target.AddComponent<WaypointAnchor>();
        anchor.ConfigureIdentifier(waypoint.Identifier);
        _scenarioOwnedWaypoints.Add(new ScenarioOwnedWaypoint
        {
          GameObject = target,
          DespawnOnScenarioEnd = waypoint.DespawnOnScenarioEnd
        });
      }

      return true;
    }

    private bool TrySpawnScenarioActingNpc(
      ScenarioGraph graph,
      ScenarioActingNpcDefinition actingNpc,
      out string error,
      Vector3? positionOverride = null)
    {
      error = string.Empty;
      if (actingNpc == null || string.IsNullOrWhiteSpace(actingNpc.Identifier))
      {
        error = "ActingNpc identifier is required.";
        return false;
      }
      if (_scenarioOwnedActingNpcs.Any(value => value?.GameObject != null
          && string.Equals(value.GameObject.GetComponentInChildren<Entity.Npc>(true)?.Identifier,
            actingNpc.Identifier, StringComparison.Ordinal)))
      {
        error = $"ActingNpc '{actingNpc.Identifier}' is already spawned by this scenario.";
        return false;
      }
      if (actingNpc.ActingNpcType != ScenarioActingNpcType.Npc)
      {
        error = $"Unsupported actingNpc type '{actingNpc.ActingNpcType}'.";
        return false;
      }

      var position = positionOverride
        ?? new Vector3(actingNpc.PositionX, actingNpc.PositionY, actingNpc.PositionZ);
      var rotation = Quaternion.Euler(actingNpc.RotationX, actingNpc.RotationY, actingNpc.RotationZ);
      if (!Registry.Registry.TrySpawnEntityPreset(actingNpc.PresetIdentifier, position, rotation,
            actingNpc.Identifier, out var spawned, out _, out error))
      {
        return false;
      }

      var npc = spawned != null ? spawned.GetComponentInChildren<Entity.Npc>(true) : null;
      if (npc == null)
      {
        DestroyScenarioActingNpc(spawned);
        error = $"Preset '{actingNpc.PresetIdentifier}' does not contain an Npc component.";
        return false;
      }

      var ownedActingNpc = new ScenarioOwnedActingNpc
      {
        GameObject = spawned,
        DespawnOnScenarioEnd = actingNpc.DespawnOnScenarioEnd
      };
      _scenarioOwnedActingNpcs.Add(ownedActingNpc);
      try
      {
        npc.ConfigureScenarioActingNpc(actingNpc);
        if (!ScenarioNetworkRelay.PublishScenarioActingNpcConfiguration(
              graph.Identifier, actingNpc.Identifier, spawned.GetComponent<NetworkObject>()))
        {
          throw new InvalidOperationException(
            "Networked scenario actingNpc requires a spawned NetworkObject and ScenarioNetworkRelay.");
        }
        return true;
      }
      catch (Exception ex)
      {
        error = ex.Message;
        _scenarioOwnedActingNpcs.Remove(ownedActingNpc);
        DestroyScenarioActingNpc(spawned);
        return false;
      }
    }

    private void TrackCleanup(Action undo)
    {
      if (!_isRevertingCleanupJournal && undo != null)
        _cleanupJournal.Push(undo);
    }

    private void RevertTrackedChanges()
    {
      _isRevertingCleanupJournal = true;
      try
      {
        while (_cleanupJournal.Count > 0)
        {
          try { _cleanupJournal.Pop()?.Invoke(); }
          catch (Exception ex) { Debug.LogException(ex, this); }
        }
      }
      finally { _isRevertingCleanupJournal = false; }
    }

    private void SetTrackedState(string key, string value)
    {
      bool existed = _stateStore.TryGetValue(key, out var previous);
      TrackCleanup(() =>
      {
        if (existed) _stateStore[key] = previous;
        else _stateStore.Remove(key);
      });
      _stateStore[key] = value;
    }

    private void CleanupScenarioActingNpcs(bool forceDespawn = false)
    {
      for (int i = _scenarioOwnedActingNpcs.Count - 1; i >= 0; i--)
      {
        var owned = _scenarioOwnedActingNpcs[i];
        if (forceDespawn || owned?.DespawnOnScenarioEnd == true)
          DestroyScenarioActingNpc(owned.GameObject);
      }
      _scenarioOwnedActingNpcs.Clear();
    }

    private void CleanupScenarioWaypoints(bool forceDespawn = false)
    {
      for (int i = _scenarioOwnedWaypoints.Count - 1; i >= 0; i--)
      {
        var owned = _scenarioOwnedWaypoints[i];
        if (forceDespawn || owned?.DespawnOnScenarioEnd == true)
          Destroy(owned.GameObject);
      }
      _scenarioOwnedWaypoints.Clear();
    }

    private static void DestroyScenarioActingNpc(GameObject actingNpc)
    {
      if (actingNpc == null)
        return;

      var networkObject = actingNpc.GetComponent<NetworkObject>();
      if (!InstanceFinder.IsOffline
          && InstanceFinder.IsServerStarted
          && networkObject != null
          && networkObject.IsSpawned)
      {
        InstanceFinder.ServerManager.Despawn(networkObject);
        return;
      }

      Destroy(actingNpc);
    }

    /// <summary>
    /// 다음 노드로 진행
    /// </summary>
    public void Advance()
    {
      if (_executionMode == ExecutionMode.ClientPresentation)
      {
        if (_currentGraph != null && _currentNode != null)
          ScenarioNetworkRelay.RequestAdvance(_currentGraph.Identifier, _currentNode.Identifier);
        return;
      }
      // 브랜치 체인이 노드를 실행하는 동안에는 전역 진행을 무시한다.
      // 브랜치는 NextIdentifier 로 직접 이동하므로, 브랜치 노드 실행기가 호출한
      // Advance 가 전역 _currentNode 를 끌고 가서 시나리오를 조기 종료시키는 것을 막는다.
      if (_globalAdvanceSuppressionDepth > 0)
      {
        return;
      }

      // 분기 코루틴은 대기 중에도 전역 Advance 억제를 해제한다. 이때 이전 대화의
      // 자동 진행처럼 분기 밖에서 발생한 Advance가 병렬 부모를 조기 완료시키면 안 된다.
      if (_parallelAdvanceBlockDepth > 0)
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
      if (_executionMode == ExecutionMode.ClientPresentation)
      {
        if (_currentGraph != null && _currentNode != null)
          ScenarioNetworkRelay.RequestChoiceSelection(_currentGraph.Identifier, _currentNode.Identifier, index);
        if (_currentPresentationNodeRoleScoped && !_uiController.IsUnityNull())
          _uiController.DismissPresentationNode();
        return;
      }
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
      RecordChoiceAssessment(_currentNode as ScenarioChoiceNode, index);
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

    #region Manual entrypoint

    /// <summary>
    /// 현재 그래프가 선언한 ManualEntrypoint 별칭을 순서 없이 모아 반환한다.
    /// 명령 자동완성과 오류 안내에 쓴다.
    /// </summary>
    public IReadOnlyList<string> GetManualEntrypointIdentifiers()
      => CollectManualEntrypointIdentifiers(_currentGraph);

    /// <summary>그래프에서 ManualEntrypoint 별칭만 뽑아낸다.</summary>
    public static IReadOnlyList<string> CollectManualEntrypointIdentifiers(ScenarioGraph graph)
    {
      var result = new List<string>();
      if (graph == null)
        return result;

      foreach (var node in graph.Nodes.Values)
      {
        if (node is ScenarioManualEntrypointNode entrypoint
            && !string.IsNullOrWhiteSpace(entrypoint.ResolvedEntrypointIdentifier))
        {
          result.Add(entrypoint.ResolvedEntrypointIdentifier);
        }
      }

      return result;
    }

    /// <summary>
    /// 별칭(<c>entrypointIdentifier</c>) 또는 노드 식별자로 ManualEntrypoint 노드를 찾는다.
    /// 별칭이 먼저이고, 없으면 노드 식별자로 한 번 더 찾는다.
    /// </summary>
    public static bool TryFindManualEntrypoint(
      ScenarioGraph graph,
      string entrypointIdentifier,
      out ScenarioManualEntrypointNode entrypoint)
    {
      entrypoint = null;
      if (graph == null || string.IsNullOrWhiteSpace(entrypointIdentifier))
        return false;

      string wanted = entrypointIdentifier.Trim();

      foreach (var node in graph.Nodes.Values)
      {
        if (node is ScenarioManualEntrypointNode candidate
            && string.Equals(candidate.ResolvedEntrypointIdentifier, wanted, StringComparison.OrdinalIgnoreCase))
        {
          entrypoint = candidate;
          return true;
        }
      }

      foreach (var node in graph.Nodes.Values)
      {
        if (node is ScenarioManualEntrypointNode candidate
            && string.Equals(candidate.Identifier, wanted, StringComparison.OrdinalIgnoreCase))
        {
          entrypoint = candidate;
          return true;
        }
      }

      return false;
    }

    /// <summary>
    /// 이 피어가 그래프를 서버 권위로 순회하고 있는지. true 면 나머지 피어는 표시 전용이라
    /// 서버 커서만 옮기면 되고, false 면 각 피어가 자기 상태기를 직접 옮겨야 한다
    /// (호환 실행 경로에서는 대상 클라이언트마다 독립 상태기가 돈다).
    /// </summary>
    public bool IsAuthoritativeExecutor
      => _executionMode == ExecutionMode.ServerAuthoritative && _currentGraph != null;

    /// <summary>
    /// 호환 실행 경로에서 서버 브로드캐스트를 받아 이 피어의 상태기를 직접 옮긴다.
    /// 서버 권위 실행 중인 피어와 표시 전용 피어는 서버 커서를 따라가므로 무시한다.
    /// </summary>
    internal void EnterManualEntrypointFromRelay(string entrypointIdentifier, bool clearState)
    {
      if (_executionMode != ExecutionMode.Local || _currentGraph == null)
        return;

      if (!TryEnterManualEntrypoint(entrypointIdentifier, clearState, out string error))
        Debug.LogWarning($"[ScenarioController] Relayed manual entry was rejected: {error}");
    }

    /// <summary>
    /// 재생 위치를 ManualEntrypoint 노드로 옮긴다. 진행 중이던 노드/브랜치 코루틴은 모두 중단된다.
    /// </summary>
    /// <param name="entrypointIdentifier">ManualEntrypoint 노드의 별칭 또는 식별자.</param>
    /// <param name="clearState">
    /// true 면 지금까지 쌓인 시나리오 상태(상태값·신호·카운터·타이머·발행된 퀘스트)를 먼저 비운다.
    /// </param>
    /// <param name="error">실패했을 때 사용자에게 보여줄 사유.</param>
    public bool TryEnterManualEntrypoint(string entrypointIdentifier, bool clearState, out string error)
    {
      error = string.Empty;

      if (string.IsNullOrWhiteSpace(entrypointIdentifier))
      {
        error = "Entrypoint identifier is required.";
        return false;
      }

      if (_currentGraph == null)
      {
        error = "No scenario is currently playing.";
        return false;
      }

      // 표시 전용 피어는 그래프 커서를 소유하지 않으므로 진입 지점을 옮길 수 없다.
      if (_executionMode == ExecutionMode.ClientPresentation)
      {
        error = "Manual entry is only available on the scenario-executing peer.";
        return false;
      }

      if (!TryFindManualEntrypoint(_currentGraph, entrypointIdentifier, out var entrypoint))
      {
        var available = GetManualEntrypointIdentifiers();
        error = available.Count == 0
          ? $"Scenario '{_currentGraph.Identifier}' declares no manual entrypoint."
          : $"Manual entrypoint '{entrypointIdentifier}' not found. Available: {string.Join(", ", available)}";
        return false;
      }

      if (!string.IsNullOrWhiteSpace(entrypoint.ManualEnterSetupIdentifier)
          && !_currentGraph.TryGetNode(entrypoint.ManualEnterSetupIdentifier, out _))
      {
        error = $"Manual entrypoint '{entrypoint.ResolvedEntrypointIdentifier}' points at a missing setup node "
                + $"'{entrypoint.ManualEnterSetupIdentifier}'.";
        return false;
      }

      // 코루틴 정리가 새로 띄울 진입 코루틴까지 잡아먹지 않도록 중단을 먼저 끝낸다.
      AbortActiveExecutionForManualEntry();

      if (clearState)
        ClearRuntimeStateForManualEntry();

      Debug.Log($"[ScenarioController] Manual entry into '{entrypoint.ResolvedEntrypointIdentifier}' "
                + $"(node='{entrypoint.Identifier}', clearState={clearState})");
      try
      {
        GameLogService.WriteScenario(
          $"Manual entry: entrypoint={entrypoint.ResolvedEntrypointIdentifier}, node={entrypoint.Identifier}, "
          + $"clearState={clearState}, graph={_currentGraph.Identifier}",
          _currentGraph.Identifier);
      }
      catch { /* 로그 실패는 진입 처리에 영향 없음 */ }

      StartCoroutine(ManualEntryRoutine(entrypoint));
      return true;
    }

    /// <summary>
    /// 준비 체인을 먼저 돌린 뒤 ManualEntrypoint 노드부터 본 흐름을 이어간다.
    /// </summary>
    private IEnumerator ManualEntryRoutine(ScenarioManualEntrypointNode entrypoint)
    {
      // 준비 체인이 도는 동안에도 커서는 이미 이 노드다. 체인이 대기하는 사이 들어온 진행 요청이
      // 스킵 이전 노드의 next 로 흘러가 본 흐름과 준비 체인이 겹쳐 도는 것을 막는다.
      _currentNode = entrypoint;

      if (!string.IsNullOrWhiteSpace(entrypoint.ManualEnterSetupIdentifier)
          && _currentGraph != null
          && _currentGraph.TryGetNode(entrypoint.ManualEnterSetupIdentifier, out var setupStart))
      {
        // RunWithGlobalAdvanceSuppressed 는 각 MoveNext 순간에만 억제하므로 체인이 yield 로
        // 대기하는 동안에는 풀린다. 병렬 노드와 같은 방식으로 전역 진행 자체를 막아 둔다.
        // (강제 중단으로 finally 를 못 거쳐도 EndScenario/StartScenarioInternal/
        //  AbortActiveExecutionForManualEntry 가 카운터를 0 으로 되돌린다.)
        _parallelAdvanceBlockDepth++;
        try
        {
          // 준비 체인은 전역 커서를 건드리면 안 되므로 병렬 브랜치와 같은 자가완결 실행기로 돌린다.
          // 체인이 이 노드나 이 노드의 다음 노드로 이어지면 거기서 멈추고 제어가 돌아온다.
          yield return RunBranchChain(
            setupStart,
            entrypoint.Identifier,
            entrypoint.NextIdentifier,
            _scenarioOwnerClientId);
        }
        finally
        {
          ReleaseParallelAdvanceBlock();
        }
      }

      if (_currentGraph == null)
        yield break;

      _currentNode = entrypoint;
      ExecuteNode(entrypoint);
    }

    /// <summary>진행 중이던 노드·브랜치·대화 UI 점유를 모두 끊는다.</summary>
    private void AbortActiveExecutionForManualEntry()
    {
      CancelDialogueAutoAdvance();
      CancelInlineTTSPrewarm();
      CompleteNodeVisit(_activeMainNodeVisitSequence);
      CompleteOpenNodeVisits();
      _activeMainNodeVisitSequence = 0;

      StopAllCoroutines();

      // 강제 중단된 코루틴은 finally 를 못 거치므로 억제 카운터를 직접 되돌린다.
      _globalAdvanceSuppressionDepth = 0;
      _parallelAdvanceBlockDepth = 0;
      _activeRoleBranchDepthByClientId.Clear();

      _branchOptionInterceptor = null;
      _branchDialogueAdvanceInterceptors.Clear();
      _branchPromptActive = false;
      _activeRemoteBranchPromptClients.Clear();
      _branchDialogueActive = false;
      _activeRemoteBranchDialogueClients.Clear();
      _remoteBranchChoiceSelections.Clear();

      ClearOptions();
      _state = State.Inactive;

      // 표시 중이던 대화창을 실제로 내린다. ClearDialogueOwner 는 점유자 문자열만 지우기 때문에,
      // 이것만으로는 패널과 입력 대기 상태가 살아남아 스킵 직후 노드에 Advance 가 꽂힌다
      // (대기 중인 Validator 게이트가 조건 미충족으로 통과되는 경로).
      DismissDialogueSurfaces();

      // 표시 전용 피어에도 같은 정리를 시킨다. 다음 노드가 UI 를 쓰지 않는 종류면
      // 클라이언트 화면에 스킵 이전 대화가 그대로 남는다.
      if (_executionMode == ExecutionMode.ServerAuthoritative && _currentGraph != null)
        ScenarioNetworkRelay.DismissAuthoritativePresentation(_currentGraph.Identifier);

      // 중단으로 날아간 인라인 TTS 선합성을 다시 걸어 둔다.
      // 베이크 WAV 와 이미 캐시된 텍스트는 서비스가 건너뛰므로 재합성 비용은 없다.
      StartInlineTTSPrewarm(_currentGraph);
    }

    /// <summary>대화창 계열 UI 를 내리고 입력 대기 상태를 푼다.</summary>
    private void DismissDialogueSurfaces()
    {
      if (_uiController.IsUnityNull())
        return;

      // 대화창이 실제로 떠 있을 때만 해제한다. 그냥 부르면 힌트 UI 가 Dialogue 모드가 아닌데도
      // ExitDialogueMode 가 불려 경고만 남기고 빈 캐시로 힌트 목록을 덮어쓴다.
      bool dialogueEngaged = _uiController.IsWaitingForInput
                             || _uiController.IsTyping
                             || _uiController.HasActiveSelections
                             || !string.IsNullOrEmpty(_uiController.CurrentDialogueOwner);
      if (dialogueEngaged)
        _uiController.DismissPresentationNode();

      _uiController.HideDisinteractableDialogue();
      _uiController.ClearDialogueOwner();

      if (!dialogueEngaged)
        return;

      // EndScenario 와 같은 이유로, 대화 모드에서 빠져나온 직후 월드 상호작용 힌트를
      // 현재 상태로 다시 인식시킨다. 복원 캐시가 실제 근처 상황과 어긋날 수 있다.
      var localPlayer = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      localPlayer?.RefreshInteractableHintsNow();
    }

    /// <summary>
    /// 서버가 재생 위치를 옮겼을 때 표시 피어에 남은 대화 UI 를 내린다.
    /// <see cref="ScenarioNetworkRelay.DismissAuthoritativePresentation"/> 가 호출한다.
    /// </summary>
    public void DismissPresentationUI(string graphIdentifier)
    {
      // 그래프를 직접 순회하는 피어는 스킵 시점에 동기적으로 정리를 끝냈다. 여기서 또 손대면
      // 그 사이 진행된 노드의 표시를 되돌린다(RPC 는 ExcludeServer 로도 막지만 이중 방어).
      if (_executionMode != ExecutionMode.ClientPresentation)
        return;

      if (_currentGraph == null
          || !string.Equals(_currentGraph.Identifier, graphIdentifier, StringComparison.Ordinal))
        return;

      CancelDialogueAutoAdvance();
      ClearOptions();

      // 표시 피어의 커서는 서버가 보낸 노드를 되돌려 보내는 용도뿐이다. 스킵으로 무효가 됐으므로
      // 비워 두면 잔여 UI 에서 올라온 입력이 서버로 나가지 않는다.
      _currentNode = null;
      _currentPresentationNodeRoleScoped = false;
      _state = State.Inactive;

      DismissDialogueSurfaces();
    }

    /// <summary>건너뛴 구간이 남긴 시나리오 상태를 비운다.</summary>
    private void ClearRuntimeStateForManualEntry()
    {
      _stateStore.Clear();
      _reportedTagGateBypasses.Clear();

      ScenarioInteractionSignals.ClearAllInternalSignals();
      ScenarioInteractionSignals.ClearAllRaisedSignals();
      ScenarioNetworkRelay.FlushSignalParametersAuthoritative();
      ScenarioConditionalSignalListeners.ClearAll();
      ScenarioEntityStateSignalBindings.ClearAll();
      ScenarioSignalCounters.ClearAll();
      ScenarioTriggerZone.ResetAllForNewScenarioRun();
      ScenarioTimeRelay.ClearAllAuthoritative();
      ScenarioNetworkRelay.ClearScenarioQuestsAuthoritative(_currentGraph?.Identifier);

      if (_currentGraph != null)
        ScenarioParallelAssignmentState.ClearGraph(_currentGraph.Identifier);
    }

    /// <summary>
    /// 일반 재생에서는 표식일 뿐이라 아무것도 하지 않고 다음 노드로 넘어간다.
    /// 준비 체인은 <see cref="TryEnterManualEntrypoint"/> 로 진입했을 때만 실행된다.
    /// </summary>
    private void ExecuteManualEntrypointNode(ScenarioManualEntrypointNode node)
    {
      _state = State.ExecutingManualEntrypoint;
      Advance();
    }

    /// <summary>
    /// 곁가지 종료 표식. 곁가지에서의 종료 처리는 <see cref="RunBranchChain"/> 이 담당하므로
    /// 이 실행기는 메인 흐름이 이 노드에 닿았을 때만 불린다.
    ///
    /// <para>이 노드는 출력 포트가 없어 <see cref="IScenarioNode.NextIdentifier"/> 가 비어 있다.
    /// 따라서 메인 흐름에서 닿으면 <see cref="Advance"/> 가 시나리오를 종료시킨다. 곁가지 전용
    /// 표식이 메인 흐름에 연결된 것이므로 배선 실수일 가능성이 높다. 조용히 끝내지 않고 알린다.</para>
    /// </summary>
    private void ExecuteReturnToOriginNode(ScenarioReturnToOriginNode node)
    {
      _state = State.ExecutingReturnToOrigin;

      Debug.LogWarning(
        $"[ScenarioController] ReturnToOrigin '{node.Identifier}' 를 메인 흐름에서 실행했습니다. "
        + "이 노드는 곁가지(ManualEntrypoint 준비 체인 · 병렬 브랜치) 전용 종료 표식입니다. "
        + "다음 노드가 없으므로 시나리오가 여기서 종료됩니다. 배선을 확인하세요.");

      Advance();
    }

    /// <summary>
    /// 이동식 환자 침대를 지정한 포지셔닝 포인트에 붙인다.
    /// 서버 권위에서만 실제로 적용되고, 결과는 침대 자신의 RPC 로 각 피어에 전파된다.
    /// 호환 실행 경로의 클라이언트에서는 아무 일도 하지 않고 다음 노드로 넘어간다.
    /// </summary>
    private void ExecuteBedSnapNode(ScenarioBedSnapNode node)
    {
      _state = State.ExecutingBedSnap;

      string bedIdentifier = ResolveBedSnapTargetIdentifier(node);
      if (string.IsNullOrWhiteSpace(bedIdentifier) || string.IsNullOrWhiteSpace(node.SnapPointIdentifier))
      {
        ReportBedSnapFailure(node,
          $"target='{bedIdentifier ?? "null"}', snapPoint='{node.SnapPointIdentifier ?? "null"}' 중 비어 있는 값이 있습니다.");
        Advance();
        return;
      }

      // 침대 이동과 스냅은 서버 권위 상태다. 클라이언트가 각자 상태기를 돌리는 호환 경로에서도
      // 실제 적용은 서버만 수행하고 나머지는 RPC 로 결과를 받는다.
      if (!InstanceFinder.IsServerStarted && !InstanceFinder.IsOffline)
      {
        Advance();
        return;
      }

      if (!Registry.Registry.TryGetEntity(bedIdentifier, out var descriptor) || descriptor?.GameObject == null)
      {
        ReportBedSnapFailure(node, $"침대 엔티티 '{bedIdentifier}' 를 레지스트리에서 찾지 못했습니다.");
        Advance();
        return;
      }

      var bed = descriptor.GameObject.GetComponentInChildren<MovingPatientBedController>(true);
      if (bed == null)
      {
        ReportBedSnapFailure(node, $"엔티티 '{bedIdentifier}' 에 MovingPatientBedController 가 없습니다.");
        Advance();
        return;
      }

      // 침대 프리팹의 허용 포인트 목록은 "플레이어가 밀어서 붙일 수 있는 곳"을 제한하는 값이다.
      // 시나리오가 지시한 배치는 그 제한보다 우선하므로, 대상 포인트를 목록에 먼저 보정한다.
      // (move_patientA_to_treatmentroom 이벤트가 쓰는 것과 같은 방식)
      bed.EnsureAllowedPositioningPointIdentifier(node.SnapPointIdentifier);

      if (!bed.TryForceSnapToPositioningPoint(node.SnapPointIdentifier, node.Teleport))
      {
        ReportBedSnapFailure(node,
          $"침대 '{bedIdentifier}' 를 '{node.SnapPointIdentifier}' 에 붙이지 못했습니다"
          + $"(teleport={node.Teleport}). 포인트가 씬에 없거나, 허용 목록 밖이거나, 다른 침대가 점유 중일 수 있습니다.");
        Advance();
        return;
      }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
      Debug.Log($"[ScenarioController] BedSnap '{node.Identifier}': {bedIdentifier} -> {node.SnapPointIdentifier}");
#endif
      Advance();
    }

    /// <summary>직접 지정한 침대 식별자를 우선 쓰고, 없으면 상태 저장소에서 읽는다.</summary>
    private string ResolveBedSnapTargetIdentifier(ScenarioBedSnapNode node)
    {
      if (!string.IsNullOrWhiteSpace(node.BedEntityIdentifier))
        return node.BedEntityIdentifier.Trim();

      if (!string.IsNullOrWhiteSpace(node.BedEntityStateKey)
          && _stateStore.TryGetValue(node.BedEntityStateKey.Trim(), out var stored))
        return stored;

      return null;
    }

    private void ReportBedSnapFailure(ScenarioBedSnapNode node, string reason)
    {
      string message = $"[ScenarioController] BedSnap '{node.Identifier}' 실패: {reason}";
      if (node.IgnoreFailure)
        Debug.LogWarning(message);
      else
        Debug.LogError(message);
    }

    #endregion

    #region Scenario text and assessment log

    private string ResolveScenarioText(string value, int? explicitClientId = null)
    {
      int? clientId = explicitClientId;
      if (!clientId.HasValue && InstanceFinder.IsClientStarted)
      {
        var local = InstanceFinder.ClientManager?.Connection;
        if (local != null)
          clientId = local.ClientId;
      }
      if (!clientId.HasValue)
        clientId = _scenarioOwnerClientId;

      return ScenarioTextResolver.Resolve(value, clientId);
    }

    private string ResolveTTSText(string content, string ttsPassing, int? explicitClientId = null)
    {
      return ResolveScenarioText(string.IsNullOrWhiteSpace(ttsPassing) ? content : ttsPassing, explicitClientId);
    }

    private void RecordChoiceAssessment(ScenarioChoiceNode node, int selectedIndex)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.AssessmentIdentifier))
        return;

      string selected = node.Options != null && selectedIndex >= 0 && selectedIndex < node.Options.Count
        ? node.Options[selectedIndex]?.DisplayText
        : null;
      string intended = node.CorrectOptionIndex.HasValue
          && node.Options != null
          && node.CorrectOptionIndex.Value >= 0
          && node.CorrectOptionIndex.Value < node.Options.Count
        ? node.Options[node.CorrectOptionIndex.Value]?.DisplayText
        : null;
      string correct = node.CorrectOptionIndex.HasValue
        ? (selectedIndex == node.CorrectOptionIndex.Value).ToString()
        : "unknown";

      GameLogService.WriteScenario(
        $"Choice assessment: assessment={node.AssessmentIdentifier}, selectedIndex={selectedIndex}, selected='{selected}', intendedIndex={node.CorrectOptionIndex?.ToString() ?? "null"}, intended='{intended}', correct={correct}",
        _currentGraph?.Identifier);
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
      CompleteNodeVisit(_activeMainNodeVisitSequence);
      _activeMainNodeVisitSequence = RecordNodeVisit(node);
      LogNodeExecution(node);
      try
      {
        GameLogService.WriteScenario(
          $"Node executed: id='{node?.Identifier}', type={node?.NodeType}, graph={_currentGraph?.Identifier}",
          _currentGraph?.Identifier);
      }
      catch { /* 로그 실패는 노드 실행에 영향 없음 */ }
      OnNodeChanged?.Invoke(node);

      if (_executionMode == ExecutionMode.ServerAuthoritative)
        ScenarioNetworkRelay.PresentAuthoritativeNode(_currentGraph?.Identifier, node.Identifier);

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
        case ScenarioNPCControlNode npcControl:
          StartCoroutine(ExecuteNPCControlNode(npcControl));
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
        case ScenarioSignalListenerNode signalListener:
          ExecuteSignalListenerNode(signalListener);
          break;
        case ScenarioEntityStateSignalBindingNode stateBinding:
          ExecuteEntityStateSignalBindingNode(stateBinding);
          break;
        case ScenarioSignalCounterNode signalCounter:
          ExecuteSignalCounterNode(signalCounter);
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
        case ScenarioQuestMarkNode questMark:
          ExecuteQuestMarkNode(questMark);
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
        case ScenarioManualEntrypointNode manualEntrypoint:
          ExecuteManualEntrypointNode(manualEntrypoint);
          break;
        case ScenarioBedSnapNode bedSnap:
          ExecuteBedSnapNode(bedSnap);
          break;
        case ScenarioReturnToOriginNode returnToOrigin:
          ExecuteReturnToOriginNode(returnToOrigin);
          break;
        case ScenarioLifecycleNode lifecycle:
          ExecuteLifecycleNode(lifecycle);
          break;
        default:
          Debug.LogWarning($"[ScenarioController] Unsupported node type: {node.GetType().Name}");
          Advance();
          break;
      }
    }

    private void ExecuteLifecycleNode(ScenarioLifecycleNode node)
    {
      _state = State.ExecutingLifecycle;
      if (node.RevertTrackedChanges)
        RevertTrackedChanges();
      if (node.ClearRuntimeState)
      {
        ClearRuntimeStateForManualEntry();
        CleanupScenarioActingNpcs();
        CleanupScenarioWaypoints();
      }

      switch (node.Operation)
      {
        case ScenarioLifecycleOperation.Cleanup:
          Advance();
          break;
        case ScenarioLifecycleOperation.End:
          EndScenario();
          break;
        case ScenarioLifecycleOperation.Restart:
          RestartScenario(node.RestartEntrypointIdentifier);
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
      _reportedTagGateBypasses.Clear();
      _activeMainNodeVisitSequence = 0;
    }

    private void ResetNodeVisitOrders(string graphIdentifier)
    {
      if (string.IsNullOrWhiteSpace(graphIdentifier))
      {
        return;
      }

      _graphVisitHistory[graphIdentifier] = new GraphVisitHistory();
      _reportedTagGateBypasses.Clear();
      _activeMainNodeVisitSequence = 0;
    }

    private int RecordNodeVisit(IScenarioNode node)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.Identifier))
      {
        return 0;
      }

      if (_currentGraph == null || string.IsNullOrWhiteSpace(_currentGraph.Identifier))
      {
        return 0;
      }

      if (!_graphVisitHistory.TryGetValue(_currentGraph.Identifier, out var history) || history == null)
      {
        history = new GraphVisitHistory();
        _graphVisitHistory[_currentGraph.Identifier] = history;
      }

      history.Sequence++;
      var sequence = history.Sequence;
      history.VisitTimings[sequence] = new ScenarioNodeVisitTiming(sequence, DateTime.Now, null);
      if (!history.NodeVisitOrders.TryGetValue(node.Identifier, out var orders) || orders == null)
      {
        orders = new List<int>();
        history.NodeVisitOrders[node.Identifier] = orders;
      }

      orders.Insert(0, sequence);
      return sequence;
    }

    private void ReportIgnoredTagGate(string subject, string reason)
    {
      var graphIdentifier = _currentGraph?.Identifier ?? "<unknown>";
      var nodeIdentifier = _currentNode?.Identifier ?? "<unknown>";
      var dedupeKey = $"{graphIdentifier}|{nodeIdentifier}|{subject}|{reason}";
      if (!_reportedTagGateBypasses.Add(dedupeKey))
      {
        return;
      }

      var message = $"[ScenarioController] WARNING: IgnoreTagAssignFullSatisfactionOnScenarioPlay bypassed tag gate. graph='{graphIdentifier}', node='{nodeIdentifier}', subject='{subject}', reason={reason}";
      Debug.LogWarning(message, this);
      AppendSystemChatMessage(message);
      GameLogService.WriteScenario(message, graphIdentifier);
      AddCurrentVisitNote(message);
    }

    private void AddCurrentVisitNote(string message)
    {
      var graphIdentifier = _currentGraph?.Identifier;
      if (_activeMainNodeVisitSequence > 0
          && !string.IsNullOrWhiteSpace(graphIdentifier)
          && _graphVisitHistory.TryGetValue(graphIdentifier, out var history)
          && history != null)
      {
        if (!history.VisitNotes.TryGetValue(_activeMainNodeVisitSequence, out var notes))
        {
          notes = new List<string>();
          history.VisitNotes[_activeMainNodeVisitSequence] = notes;
        }
        notes.Add(message);
      }
    }

    private bool TryIgnoreMissingTagGate(string subject, string reason)
    {
      if (!ScenarioGameRules.IgnoreTagAssignFullSatisfactionOnScenarioPlay)
      {
        return false;
      }

      ReportIgnoredTagGate(subject, reason);
      return true;
    }

    private void CompleteNodeVisit(int sequence)
    {
      if (sequence <= 0 || _currentGraph == null || string.IsNullOrWhiteSpace(_currentGraph.Identifier)
          || !_graphVisitHistory.TryGetValue(_currentGraph.Identifier, out var history)
          || history == null
          || !history.VisitTimings.TryGetValue(sequence, out var timing)
          || timing.ExitedAt.HasValue)
      {
        return;
      }

      history.VisitTimings[sequence] = new ScenarioNodeVisitTiming(sequence, timing.EnteredAt, DateTime.Now);
    }

    /// <summary>시나리오 종료 시 병렬 브랜치를 포함한 진행 중 방문을 모두 닫는다.</summary>
    private void CompleteOpenNodeVisits()
    {
      if (_currentGraph == null || string.IsNullOrWhiteSpace(_currentGraph.Identifier)
          || !_graphVisitHistory.TryGetValue(_currentGraph.Identifier, out var history)
          || history == null)
      {
        return;
      }

      var exitedAt = DateTime.Now;
      var openSequences = new List<int>();
      foreach (var pair in history.VisitTimings)
      {
        if (!pair.Value.ExitedAt.HasValue)
        {
          openSequences.Add(pair.Key);
        }
      }

      foreach (var sequence in openSequences)
      {
        var timing = history.VisitTimings[sequence];
        history.VisitTimings[sequence] = new ScenarioNodeVisitTiming(sequence, timing.EnteredAt, exitedAt);
      }
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

      if (_executionMode == ExecutionMode.ServerAuthoritative)
      {
        // 호스트는 권위 상태기와 로컬 UI가 같은 Controller를 공유한다. 원격 클라이언트는
        // Relay의 ObserversRpc로만 표시하지만, 호스트에는 직접 표시해야 한다.
        if (InstanceFinder.IsClientStarted && IsLocalScenarioOwner())
          PresentDialogueNode(node);
        if (node.AutoAdvanceSeconds.HasValue && node.AutoAdvanceSeconds.Value > 0f)
          _dialogueAutoAdvanceRoutine = StartCoroutine(DialogueAutoAdvanceRoutine(node.AutoAdvanceSeconds.Value, node.InteractionRequired));
        return;
      }

      // 대화창 점유 충돌 검사(정책 적용). Cancel/Panic 이면 여기서 중단.
      if (!_uiController.IsUnityNull() && !TryClaimDialogueUI())
        return;

      if (!_uiController.IsUnityNull())
      {
        PresentDialogueNode(node);

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

    private void PresentDialogueNode(ScenarioDialogueNode node)
    {
      _state = State.ExecutingDialogue;
      if (_uiController.IsUnityNull())
        return;

      string speaker = ResolveScenarioText(node.SpeakerName);
      string content = ResolveScenarioText(node.DialogueContent);
      _uiController.DisplayDialogue(speaker, content, node.PortraitSpriteIdentifier, node.InteractionRequired);
      if (node.PlayTTS)
        PlayInlineTTS(node.Identifier, ResolveTTSText(node.DialogueContent, node.DialogueContentTTSPassing), node.TtsVoiceIdentifier);
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

        if (_executionMode == ExecutionMode.ClientPresentation && _currentPresentationNodeRoleScoped)
        {
          if (!_uiController.IsUnityNull())
            _uiController.DismissPresentationNode();
          yield break;
        }

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
        string speaker = ResolveScenarioText(node.SpeakerName);
        string content = ResolveScenarioText(node.DialogueContent);
        _uiController.DisplayDisinteractableDialogue(
          speaker,
          content,
          node.PortraitSpriteIdentifier);
        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, ResolveTTSText(node.DialogueContent, node.DialogueContentTTSPassing), node.TtsVoiceIdentifier);
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
      if (_executionMode == ExecutionMode.ServerAuthoritative)
      {
        _state = State.ExecutingChoice;
        _activeOptions = new List<ScenarioChoiceOption>(node.Options);
        if (InstanceFinder.IsClientStarted && IsLocalScenarioOwner())
          PresentChoice(node);
        return;
      }

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
        _uiController.DisplayChoice(
          ResolveScenarioText(node.SpeakerName),
          ResolveScenarioText(node.DialogueContent),
          node.PortraitSpriteIdentifier,
          node.Options);

        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, ResolveTTSText(node.DialogueContent, node.DialogueContentTTSPassing), node.TtsVoiceIdentifier);
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

    private void ExecuteQuestControlNode(ScenarioQuestControlNode node, int? ownerClientId)
    {
      _state = State.ExecutingQuestControl;

      if (!ShouldApplyQuestControlNode(node, ownerClientId))
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

    private void ExecuteQuestControlNode(ScenarioQuestControlNode node)
      => ExecuteQuestControlNode(node, _scenarioOwnerClientId);

    /// <summary>
    /// 서버가 이미 권위적으로 순회한 퀘스트 노드를 클라이언트 UI에만 반영한다.
    /// 여기서는 <see cref="Advance"/>를 절대 호출하지 않아 표시 피어가 그래프 커서를 소유하지 않는다.
    /// </summary>
    private void PresentQuestControlNode(ScenarioQuestControlNode node, bool roleScoped)
    {
      if (!roleScoped && !ShouldApplyQuestControlNode(node, _scenarioOwnerClientId))
        return;

      var manager = Registry.Registry.Get<QuestManager>(RegistryType.Service, Registry.Registry.TypeKey<QuestManager>());
      if (manager == null)
      {
        Debug.LogWarning("[ScenarioController] QuestManager not found on presentation client; skipping quest presentation.");
        return;
      }

      if (!ApplyQuestOperation(manager, node) && node.FailureStrategy == ScenarioQuestFailureStrategy.Panic)
        Debug.LogError($"[ScenarioController] Presentation quest operation failed: '{node.Identifier}'.");
    }

    private bool ShouldApplyQuestControlNode(ScenarioQuestControlNode node, int? ownerClientId)
    {
      if (node == null)
        return false;

      // 시나리오 owner가 지정된 경우, 퀘스트 노드는 해당 owner 클라이언트에서만 적용한다.
      // 그렇지 않으면 모든 피어에서 동일 퀘스트가 동시에 등록되어 역할별 분기가 깨질 수 있다.
      if (!ownerClientId.HasValue)
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
        Debug.Log($"[ScenarioController] QuestControl '{node.Identifier}' apply=false (local connection is null, ownerClientId={ownerClientId})");
#endif
        return false;
      }

      bool apply = localConn.ClientId == ownerClientId.Value;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
      Debug.Log($"[ScenarioController] QuestControl '{node.Identifier}' apply={apply} (ownerClientId={ownerClientId}, localClientId={localConn.ClientId})");
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

    private void ExecuteQuestMarkNode(ScenarioQuestMarkNode node)
    {
      _state = State.ExecutingQuestMark;
      ApplyQuestMarkNode(node);
      Advance();
    }

    /// <summary>
    /// QuestMark 노드를 표시 상태에 반영한다. 서버 실행 경로와 클라이언트 표시 경로가 같은 처리를 사용한다.
    /// </summary>
    private static void ApplyQuestMarkNode(ScenarioQuestMarkNode node)
    {
      if (node == null)
        return;

      if (string.IsNullOrWhiteSpace(node.EntityIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] Quest mark node '{node.Identifier}' has no entity identifier.");
        return;
      }

      if (node.TargetType == QuestPresentationTargetType.Interaction
          && string.IsNullOrWhiteSpace(node.InteractionIdentifier))
      {
        Debug.LogWarning(
          $"[ScenarioController] Quest mark node '{node.Identifier}' targets an interaction but has no interaction identifier.");
        return;
      }

      if (node.Operation == ScenarioQuestMarkOperationType.Hide)
      {
        QuestPresentationService.ClearScenarioMark(
          node.TargetType,
          node.EntityIdentifier,
          node.InteractionIdentifier);
        return;
      }

      QuestPresentationService.SetScenarioMark(
        node.TargetType,
        node.EntityIdentifier,
        node.InteractionIdentifier,
        node.IconIdentifier,
        node.Priority);
    }

    /// <summary>표시 클라이언트에만 waypoint 강조를 적용한다. 그래프 진행은 서버가 담당한다.</summary>
    private static void PresentQuestWaypointHighlightNode(ScenarioQuestWaypointHighlightNode node)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.WaypointIdentifier))
        return;

      if (WaypointAnchor.TryGet(node.WaypointIdentifier, out var anchor))
        anchor.Highlight();
      else
        Debug.LogWarning($"[ScenarioController] Presentation waypoint '{node.WaypointIdentifier}' was not found.");
    }

    private IEnumerator ExecuteDelayNode(ScenarioDelayNode node)
    {
      _state = State.ExecutingDelay;

      float durationSeconds = (float)node.Duration.ToSeconds();
      if (node.WaitUntil == ScenarioDelayWaitUntil.WaitUntilDone && durationSeconds > 0f)
      {
        yield return new WaitForSeconds(durationSeconds);
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
          PlayInlineTTS(node.Identifier, ResolveTTSText(node.Question, node.QuestionTTSPassing), node.TtsVoiceIdentifier);
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
        string feedbackTTSPassing = isCorrect ? node.FeedbackCorrectTTSPassing : node.FeedbackIncorrectTTSPassing;
        PlayInlineTTS(feedbackNodeId, ResolveTTSText(feedback, feedbackTTSPassing), node.TtsVoiceIdentifier);
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
      SetTrackedState(key, node.StateValue);
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
            bool hadTag = PlayerTagService.HasTag(session.Identifier, node.Tag);
            PlayerTagService.AddTag(session.Identifier, node.Tag);
            if (!hadTag)
              TrackCleanup(() => PlayerTagService.RemoveTag(session.Identifier, node.Tag));
#if UNITY_EDITOR
            Debug.Log($"[ScenarioController] Tag Add: player={session.DisplayName} tag={node.Tag}");
#endif
            break;

          case ScenarioPlayerTagOperationType.Remove:
            bool hadRemovedTag = PlayerTagService.HasTag(session.Identifier, node.Tag);
            PlayerTagService.RemoveTag(session.Identifier, node.Tag);
            if (hadRemovedTag)
              TrackCleanup(() => PlayerTagService.AddTag(session.Identifier, node.Tag));
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

        if (PlayerTagService.HasTag(session.Identifier, tagA))
          holdersA.Add(session);
        if (PlayerTagService.HasTag(session.Identifier, tagB))
          holdersB.Add(session);
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

      if (node == null)
      {
        Debug.LogWarning("[ScenarioController] EntityPresetSpawn node is null.");
        Advance();
        return;
      }

      if (!string.IsNullOrWhiteSpace(node.ActingNpcIdentifier))
      {
        var actingNpc = _currentGraph?.ActingNpcs?.FirstOrDefault(value => value != null
          && string.Equals(value.Identifier, node.ActingNpcIdentifier, StringComparison.Ordinal));
        if (actingNpc == null)
        {
          Debug.LogWarning($"[ScenarioController] EntityPresetSpawn '{node.Identifier}' references unknown actingNpc '{node.ActingNpcIdentifier}'.");
          Advance();
          return;
        }
        Vector3? actorSpawnPosition = null;
        if (!string.IsNullOrWhiteSpace(node.PositionSourceEntityIdentifier))
        {
          if (TryResolveSpawnPositionSource(node.PositionSourceEntityIdentifier, out var resolvedPosition))
          {
            actorSpawnPosition = resolvedPosition;
          }
          else
          {
            Debug.LogWarning(
              $"[ScenarioController] EntityPresetSpawn '{node.Identifier}' position source " +
              $"'{node.PositionSourceEntityIdentifier}' was not found. Using actingNpc definition position.");
          }
        }
        if (!TrySpawnScenarioActingNpc(
              _currentGraph,
              actingNpc,
              out var actorError,
              actorSpawnPosition))
        {
          Debug.LogWarning($"[ScenarioController] EntityPresetSpawn '{node.Identifier}' actingNpc '{node.ActingNpcIdentifier}' failed: {actorError}");
          Advance();
          return;
        }

        string actorStateKey = string.IsNullOrWhiteSpace(node.ResultStateKey)
          ? $"{node.Identifier}.spawnedEntityIdentifier"
          : node.ResultStateKey;
        SetTrackedState(actorStateKey, actingNpc.Identifier);
        Advance();
        return;
      }

      if (string.IsNullOrWhiteSpace(node.PresetIdentifier))
      {
        Debug.LogWarning("[ScenarioController] EntityPresetSpawn node requires presetIdentifier or actingNpcIdentifier.");
        Advance();
        return;
      }

      Vector3 spawnPosition = new Vector3(node.PositionX, node.PositionY, node.PositionZ);
      if (TryResolveSpawnPositionSource(node.PositionSourceEntityIdentifier, out var resolvedSpawnPosition))
        spawnPosition = resolvedSpawnPosition;

      if (!Registry.Registry.TrySpawnEntityPreset(
            node.PresetIdentifier,
            spawnPosition,
            Quaternion.Euler(node.RotationX, node.RotationY, node.RotationZ),
            node.SpawnedEntityIdentifier,
            out var spawnedGameObject,
            out var spawnedDescriptor,
            out var error))
      {
        Debug.LogWarning($"[ScenarioController] EntityPresetSpawn '{node.Identifier}' failed: {error}");
        Advance();
        return;
      }
      TrackCleanup(() => DestroyScenarioActingNpc(spawnedGameObject));

      string stateKey = string.IsNullOrWhiteSpace(node.ResultStateKey)
        ? $"{node.Identifier}.spawnedEntityIdentifier"
        : node.ResultStateKey;

      // 네트워크 루트는 OnStartClient 에서 비동기 자가 등록하므로 스폰 직후 디스크립터가 아직 없을 수 있다.
      // 그 경우 노드에 지정된 식별자(있으면)를 결과로 저장한다.
      string spawnedIdentifier = spawnedDescriptor?.Identifier;
      if (string.IsNullOrWhiteSpace(spawnedIdentifier))
        spawnedIdentifier = node.SpawnedEntityIdentifier;
      SetTrackedState(stateKey, spawnedIdentifier);

      Advance();
    }

    private static bool TryResolveSpawnPositionSource(
      string sourceIdentifier,
      out Vector3 position)
    {
      if (!string.IsNullOrWhiteSpace(sourceIdentifier))
      {
        if (Registry.Registry.TryGetEntity(sourceIdentifier, out var sourceDescriptor)
            && sourceDescriptor?.GameObject != null)
        {
          position = sourceDescriptor.GameObject.transform.position;
          return true;
        }

        if (Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, sourceIdentifier, out var waypoint)
            || Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, sourceIdentifier, out waypoint))
        {
          position = waypoint;
          return true;
        }
      }

      position = default;
      return false;
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
              out var spawnedGameObject,
              out var spawnedDescriptor,
              out var error))
        {
          Debug.LogWarning($"[ScenarioController] ItemSubmissionConfig '{node.Identifier}' preset spawn failed: {error}");
          Advance();
          return;
        }
        TrackCleanup(() => DestroyScenarioActingNpc(spawnedGameObject));

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
        {
          // 이전 실행/단계에서 남아있는 완료 신호를 정리해, 이번 제출 단계가 실제 제출 1회를 요구하도록 보장한다.
          ScenarioInteractionSignals.Clear(node.CompletionSignalIdentifier);
          interactable.SetCompletionSignal(node.CompletionSignalIdentifier);
          interactable.ResetCompletion();
        }

        interactable.SetEnabled(node.Enabled);
      }

      string stateKey = string.IsNullOrWhiteSpace(node.ResultStateKey)
        ? $"{node.Identifier}.submissionEntityIdentifier"
        : node.ResultStateKey;
      SetTrackedState(stateKey, resolvedIdentifier);

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

      if (node.Operation == ScenarioNpcInteractControlOperation.UpdateDisplay)
      {
        npc.SetScenarioDisplay(node.DisplayName, node.ShowOverheadName);
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
          bool hadTag = PlayerTagService.HasTag(targetIdentifier, node.Tag);
          PlayerTagService.AddTagToIdentifier(targetIdentifier, node.Tag);
          if (!hadTag)
            TrackCleanup(() => PlayerTagService.RemoveTagFromIdentifier(targetIdentifier, node.Tag));
          break;
        case ScenarioPlayerTagOperationType.Remove:
          bool hadRemovedTag = PlayerTagService.HasTag(targetIdentifier, node.Tag);
          PlayerTagService.RemoveTagFromIdentifier(targetIdentifier, node.Tag);
          if (hadRemovedTag)
            TrackCleanup(() => PlayerTagService.AddTagToIdentifier(targetIdentifier, node.Tag));
          break;
        case ScenarioPlayerTagOperationType.Change:
          if (PlayerTagService.ChangeTagForIdentifier(targetIdentifier, node.FromTag, node.ToTag))
          {
            string identifier = targetIdentifier;
            string fromTag = node.FromTag;
            string toTag = node.ToTag;
            TrackCleanup(() => PlayerTagService.ChangeTagForIdentifier(identifier, toTag, fromTag));
          }
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
        TrackCleanup(() => DestroyScenarioActingNpc(spawned));

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
        SetTrackedState(storeKey, resolvedIdentifier);
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
            SetTrackedState(stateKey, op.Value);
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

      // TTSService가 준비될 때까지 대기(모델 누락 등으로 초기화가 실패한 경우 영구 대기하지 않는다)
      if (!_ttsService.IsReady && !_ttsService.IsInitializationFailed)
        yield return new WaitUntil(() => _ttsService.IsReady || _ttsService.IsInitializationFailed);

      if (!_ttsService.IsReady)
      {
        Debug.LogWarning("[ScenarioController] TTSService 초기화 실패. PlayTTS 노드를 건너뜁니다.");
        Advance();
        yield break;
      }

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
      if (_ttsService == null || _currentGraph == null)
        return;

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
    /// 시나리오 그래프의 인라인 텍스트(Dialogue/DisinteractableDialogue/Choice/Quiz 콘텐츠)를 TTS로 재생한다.
    /// baked WAV가 있으면 우선 재생하고, 없으면 즉석 합성한다.
    /// TTSService/AudioSource 참조가 없거나 텍스트가 비어 있으면 아무 작업도 하지 않는다.
    /// 텍스트 표시와 병렬로 재생되며(대기하지 않음), 다음 노드 진행을 막지 않는다.
    /// </summary>
    /// <param name="voiceIdentifier">
    /// 사용할 목소리 프로파일 식별자. null이면 기본 목소리를 사용한다.
    /// </param>
    private void PlayInlineTTS(string nodeIdentifier, string text, string voiceIdentifier = null)
    {
      if (_ttsService == null || _ttsAudioSource == null)
        return;
      if (string.IsNullOrWhiteSpace(text))
        return;

      // IsReady를 기다리지 않는다: baked WAV는 ONNX 초기화 없이 즉시 재생 가능하고,
      // baked가 없을 때만 내부에서 초기화 완료를 기다린 뒤 즉석 합성으로 폴백한다.
      string scenarioIdentifier = _currentGraph != null ? _currentGraph.Identifier : null;
      _ttsService.PlayText(text, _ttsAudioSource, scenarioIdentifier, nodeIdentifier, voiceIdentifier);
    }

    /// <summary>
    /// 시작 시 확정되는 텍스트 치환 결과를 기준으로, 미래 인라인 TTS의 런타임 합성 후보를
    /// 순차 준비한다. 베이크 WAV와 이미 캐시된 텍스트는 서비스에서 즉시 건너뛴다.
    /// </summary>
    private void StartInlineTTSPrewarm(ScenarioGraph graph)
    {
      if (_ttsService == null || graph == null)
        return;
      CancelInlineTTSPrewarm();
      _inlineTTSPrewarmCancellation = new CancellationTokenSource();
      StartCoroutine(PrewarmInlineTTSCacheRoutine(graph, _inlineTTSPrewarmCancellation.Token));
    }

    private void CancelInlineTTSPrewarm()
    {
      _inlineTTSPrewarmCancellation?.Cancel();
      _inlineTTSPrewarmCancellation?.Dispose();
      _inlineTTSPrewarmCancellation = null;
    }

    private IEnumerator PrewarmInlineTTSCacheRoutine(ScenarioGraph graph, CancellationToken cancellationToken)
    {
      // 초기 장면과 입력 처리를 먼저 안정화한 뒤, 하나씩만 합성한다.
      yield return null;
      var queuedTexts = new HashSet<string>(StringComparer.Ordinal);
      foreach (var node in graph.Nodes.Values)
      {
        if (cancellationToken.IsCancellationRequested)
          yield break;

        string text;
        string voiceIdentifier;
        switch (node)
        {
          case ScenarioDialogueNode dialogue when dialogue.PlayTTS:
            text = ResolveTTSText(dialogue.DialogueContent, dialogue.DialogueContentTTSPassing);
            voiceIdentifier = dialogue.TtsVoiceIdentifier;
            break;
          case ScenarioDisinteractableDialogueNode dialogue when dialogue.PlayTTS:
            text = ResolveTTSText(dialogue.DialogueContent, dialogue.DialogueContentTTSPassing);
            voiceIdentifier = dialogue.TtsVoiceIdentifier;
            break;
          default:
            continue;
        }

        // 서비스가 캐시와 베이크 WAV를 먼저 검사한다. 여기서는 향후 실행될 모든 대사를
        // 넘겨 누락되었거나 런타임에 달라진 텍스트까지 사전 준비한다.
        string cacheKey = (voiceIdentifier ?? string.Empty) + "\0" + text;
        if (!string.IsNullOrWhiteSpace(text)
            && queuedTexts.Add(cacheKey))
          yield return _ttsService.PrepareInlineText(
            text, graph.Identifier, node.Identifier, voiceIdentifier, cancellationToken);

        // 프레임과 CPU 시간을 게임에 돌려준다. 다음 합성은 다음 간격에만 시작한다.
        yield return new WaitForSecondsRealtime(0.25f);
      }
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
      if (!node.SkipCompletionDisplayDelay)
        return ApplyQuestOperationInternal(manager, node);

      manager.SetQuestPreviewImmediateTransition(true);
      try
      {
        return ApplyQuestOperationInternal(manager, node);
      }
      finally
      {
        manager.SetQuestPreviewImmediateTransition(false);
      }
    }

    private bool ApplyQuestOperationInternal(QuestManager manager, ScenarioQuestControlNode node)
    {
      string questId = ResolveQuestId(node);
      var questData = BuildQuestPayload(node, questId);
      if (questData != null && node.PersistProgressOnSessionEnd.HasValue)
        questData.PersistProgressOnSessionEnd = node.PersistProgressOnSessionEnd.Value;

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

    private QuestData BuildQuestPayload(ScenarioQuestControlNode node, string questId)
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

        copy.SourceScenarioIdentifier = _currentGraph?.Identifier;

        return copy;
      }

      if (string.IsNullOrWhiteSpace(node.QuestDefinitionIdentifier))
        return null;

      return new QuestData
      {
        Id = string.IsNullOrWhiteSpace(questId) ? node.QuestDefinitionIdentifier : questId,
        DefinitionIdentifier = node.QuestDefinitionIdentifier,
        SourceScenarioIdentifier = _currentGraph?.Identifier
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

      if (!TryResolveMoveDestinations(node.DestinationType, node.DestinationIdentifier,
            node.DestinationX, node.DestinationY, node.DestinationZ, out var destinations))
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
          EstimatePlayerMoveDistance(destinations));
        if (waitSeconds > 0f)
          yield return new WaitForSeconds(waitSeconds);
        Advance();
        yield break;
      }

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Moving player through {destinations.Count} destination(s) (mode={node.MoveMode})");
#endif

      player.BeginScriptedMovement();
      bool wasCanMove = player.canMove;
      player.canMove = false;
      try
      {
        yield return MovePlayerDestinationsRoutine(
          player, destinations, node.MoveMode, node.MoveSpeed, node.MoveDuration, node.IgnoreGroundCheck);
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
    private float EstimatePlayerMoveDistance(IReadOnlyList<Vector3> destinations)
    {
      // owner가 지정된 경우 owner 플레이어 위치 기준, 아니면 로컬 플레이어 위치 기준.
      int? targetClientId = _scenarioOwnerClientId ?? (int?)InstanceFinder.ClientManager?.Connection?.ClientId;
      if (targetClientId.HasValue
          && Registry.Registry.TryGetEntityByClientId(targetClientId.Value, out var descriptor)
          && descriptor?.GameObject != null)
      {
        return CalculatePathDistance(descriptor.GameObject.transform.position, destinations);
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

    /// <summary>목적지(좌표·단일 waypoint·waypoint set)를 순서 있는 좌표 목록으로 해석한다.</summary>
    private static bool TryResolveMoveDestinations(
      ScenarioMoveDestinationType destinationType,
      string destinationIdentifier,
      float x, float y, float z,
      out List<Vector3> destinations)
    {
      destinations = new List<Vector3>();
      if (destinationType == ScenarioMoveDestinationType.Position)
      {
        destinations.Add(new Vector3(x, y, z));
        return true;
      }

      Vector3 destination;
      if (destinationType == ScenarioMoveDestinationType.Waypoint
          && (Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, destinationIdentifier, out destination)
              || Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, destinationIdentifier, out destination)))
      {
        destinations.Add(destination);
        return true;
      }

      if (destinationType != ScenarioMoveDestinationType.WaypointSet
          || !WaypointSet.TryGet(destinationIdentifier, out var waypointSet))
        return false;

      var waypoints = waypointSet.Waypoints;
      for (int i = 0; i < waypoints.Count; i++)
      {
        if (waypoints[i] != null)
          destinations.Add(waypoints[i].transform.position);
      }

      return destinations.Count > 0;
    }

    private static float CalculatePathDistance(Vector3 start, IReadOnlyList<Vector3> destinations)
    {
      float distance = 0f;
      Vector3 previous = start;
      if (destinations == null)
        return distance;

      for (int i = 0; i < destinations.Count; i++)
      {
        distance += HorizontalDistance(previous, destinations[i]);
        previous = destinations[i];
      }

      return distance;
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

    private IEnumerator MovePlayerDestinationsRoutine(
      PlayerController player, IReadOnlyList<Vector3> destinations, ScenarioMoveMode mode,
      float moveSpeed, float moveDuration, bool ignoreGroundCheck)
    {
      float totalDistance = CalculatePathDistance(player.transform.position, destinations);
      for (int i = 0; i < destinations.Count; i++)
      {
        float segmentDistance = HorizontalDistance(player.transform.position, destinations[i]);
        float segmentDuration = mode == ScenarioMoveMode.ByDuration && totalDistance > 0f
          ? moveDuration * segmentDistance / totalDistance
          : moveDuration;
        yield return MovePlayerRoutine(
          player, destinations[i], mode, moveSpeed, segmentDuration, ignoreGroundCheck);
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

      if (!TryResolveMoveDestinations(node.DestinationType, node.DestinationIdentifier,
            node.DestinationX, node.DestinationY, node.DestinationZ, out var destinations))
      {
        Debug.LogWarning($"[ScenarioController] Waypoint '{node.DestinationIdentifier}' not found. Fallback to no move.");
        Advance();
        yield break;
      }

#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Moving NPC '{node.NPCIdentifier}' through {destinations.Count} destination(s) (mode={node.MoveMode})");
#endif

      var animation = npc.GetComponentInChildren<HumanoidAnimationController>(true);
      try
      {
        yield return MoveNpcDestinationsRoutine(npc.transform, animation, destinations,
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

    private IEnumerator ExecuteNPCControlNode(ScenarioNPCControlNode node)
    {
      _state = State.ExecutingNPCControl;

      if (node == null || string.IsNullOrWhiteSpace(node.NPCIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] NPCControl '{node?.Identifier}': npcIdentifier is missing.");
        Advance();
        yield break;
      }

      var npcObject = Registry.Registry.Get<GameObject>(RegistryType.Npc, node.NPCIdentifier)
                      ?? Registry.Registry.Get<GameObject>(RegistryType.Entity, node.NPCIdentifier);
      var npc = npcObject != null ? npcObject.GetComponentInChildren<Entity.Npc>(true) : null;
      if (npcObject == null || npc == null)
      {
        Debug.LogWarning($"[ScenarioController] NPCControl '{node.Identifier}': NPC '{node.NPCIdentifier}' not found.");
        Advance();
        yield break;
      }

      if (node.Mode == ScenarioNPCControlMode.Update)
      {
        bool interactableFound = ApplyNPCControlUpdate(
          npc, node.Identifier, node.DisplayName, node.ShowOverheadName,
          node.InteractOperation, node.InteractableIdentifier, node.InteractEnabled);
        if (node.InteractOperation == ScenarioNPCInteractCrudOperation.Read
            && !string.IsNullOrWhiteSpace(node.ResultStateKey))
        {
          _stateStore[node.ResultStateKey.Trim()] = interactableFound ? "true" : "false";
        }
        ApplyNpcFacing(npcObject, node.FacingYawDegrees);
        ScenarioNetworkRelay.PublishNPCControlUpdate(
          npc.GetComponentInParent<NetworkObject>(), node);

        Advance();
        yield break;
      }

      if (!TryResolveMoveDestinations(node.DestinationType, node.DestinationIdentifier,
            node.DestinationX, node.DestinationY, node.DestinationZ, out var destinations))
      {
        Debug.LogWarning($"[ScenarioController] NPCControl '{node.Identifier}': destination could not be resolved.");
        Advance();
        yield break;
      }

      var animation = npcObject.GetComponentInChildren<HumanoidAnimationController>(true);
      try
      {
        yield return MoveNpcDestinationsRoutine(npcObject.transform, animation, destinations,
          node.MoveMode, node.MoveSpeed, node.MoveDuration, node.IgnoreGroundCheck);
      }
      finally
      {
        if (animation != null)
          animation.SetBool(NpcWalkAnimationParameterName, false);
      }

      // 이동 경로는 도착 지점만 정하고 방향은 정하지 않는다. 방향까지 지정한 노드는
      // 이동이 끝난 뒤에 적용해야 마지막 이동 방향에 덮이지 않는다.
      ApplyNpcFacing(npcObject, node.FacingYawDegrees);

      Advance();
    }

    /// <summary>
    /// NPC가 바라볼 방향을 월드 Y축 회전으로 지정한다. 값이 없으면 회전을 건드리지 않는다.
    /// X/Z 회전은 유지해서, 프리팹이 가진 기울기 설정을 이 지시가 지우지 않게 한다.
    /// </summary>
    private static void ApplyNpcFacing(GameObject npcObject, float? facingYawDegrees)
    {
      if (npcObject == null || !facingYawDegrees.HasValue)
        return;

      var euler = npcObject.transform.rotation.eulerAngles;
      npcObject.transform.rotation = Quaternion.Euler(euler.x, facingYawDegrees.Value, euler.z);
    }

    internal static bool ApplyNPCControlUpdate(
      Entity.Npc npc,
      string nodeIdentifier,
      string displayName,
      bool? showOverheadName,
      ScenarioNPCInteractCrudOperation interactOperation,
      string interactableIdentifier,
      bool? interactEnabled)
    {
      if (npc == null)
        return false;

      npc.SetScenarioDisplay(displayName, showOverheadName);
      if (interactOperation == ScenarioNPCInteractCrudOperation.None)
        return false;

      MonoBehaviour interactableComponent = null;
      if (!string.IsNullOrWhiteSpace(interactableIdentifier))
      {
        interactableComponent = Registry.Registry.Get<MonoBehaviour>(
          RegistryType.InteractableEntity, interactableIdentifier);

        if (interactableComponent == null
            && Registry.Registry.TryGetEntity(interactableIdentifier, out var descriptor)
            && descriptor?.GameObject != null)
        {
          interactableComponent = descriptor.GameObject.GetComponentInChildren<IInteract>(true) as MonoBehaviour;
        }
      }

      switch (interactOperation)
      {
        case ScenarioNPCInteractCrudOperation.Create:
          if (interactableComponent == null || !npc.AddCustomInteractSource(interactableComponent))
            Debug.LogWarning($"[ScenarioController] NPCControl '{nodeIdentifier}': interactable '{interactableIdentifier}' is missing or does not implement IInteract for Create.");
          break;

        case ScenarioNPCInteractCrudOperation.Read:
          if (interactableComponent == null)
            Debug.LogWarning($"[ScenarioController] NPCControl '{nodeIdentifier}': interactable '{interactableIdentifier}' was not found.");
          break;

        case ScenarioNPCInteractCrudOperation.Update:
          if (interactableComponent is IInteractToggleable toggleable)
            toggleable.SetEnabled(interactEnabled ?? true);
          else
            Debug.LogWarning($"[ScenarioController] NPCControl '{nodeIdentifier}': interactable '{interactableIdentifier}' does not implement IInteractToggleable.");
          break;

        case ScenarioNPCInteractCrudOperation.Delete:
          if (interactableComponent != null)
            npc.RemoveCustomInteractSource(interactableComponent);
          break;
      }

      var ownerPlayer = Registry.Registry.GetFirstEntityComponent<PlayerController>(
        EntityType.Player, each => each != null && each.IsOwner);
      ownerPlayer?.RefreshInteractableHintsNow();
      return interactableComponent != null;
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
          if (!ignoreGroundCheck && TryFindGroundY(next, ownColliders, out float groundY))
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

    private IEnumerator MoveNpcDestinationsRoutine(
      Transform npcTransform, HumanoidAnimationController animation,
      IReadOnlyList<Vector3> destinations, ScenarioMoveMode mode,
      float moveSpeed, float moveDuration, bool ignoreGroundCheck)
    {
      float totalDistance = CalculatePathDistance(npcTransform.position, destinations);
      for (int i = 0; i < destinations.Count; i++)
      {
        float segmentDistance = HorizontalDistance(npcTransform.position, destinations[i]);
        float segmentDuration = mode == ScenarioMoveMode.ByDuration && totalDistance > 0f
          ? moveDuration * segmentDistance / totalDistance
          : moveDuration;
        yield return MoveNpcRoutine(
          npcTransform, animation, destinations[i], mode, moveSpeed, segmentDuration, ignoreGroundCheck);
      }
    }

    /// <summary>
    /// 지정 위치 아래의 가장 높은 비트리거 충돌면을 찾는다. 일반적인 경우에는 재사용 버퍼를
    /// 사용해 할당하지 않으며, 버퍼가 가득 찬 경우에만 정확한 결과를 위해 전체 히트를 다시 읽는다.
    /// </summary>
    public static bool TryFindGroundY(Vector3 position, Collider[] ignoredColliders, out float groundY)
    {
      groundY = position.y;
      int hitCount = Physics.RaycastNonAlloc(position + Vector3.up * 2f, Vector3.down,
        GroundRaycastHits, 10f, ~0, QueryTriggerInteraction.Ignore);
      if (hitCount == GroundRaycastHits.Length)
      {
        var hits = Physics.RaycastAll(position + Vector3.up * 2f, Vector3.down, 10f,
          ~0, QueryTriggerInteraction.Ignore);
        return TryFindHighestGroundY(hits, hits.Length, ignoredColliders, position.y, out groundY);
      }

      return TryFindHighestGroundY(GroundRaycastHits, hitCount, ignoredColliders, position.y, out groundY);
    }

    private static bool TryFindHighestGroundY(
      RaycastHit[] hits, int hitCount, Collider[] ignoredColliders, float defaultGroundY, out float groundY)
    {
      groundY = defaultGroundY;
      float best = float.NegativeInfinity;
      bool found = false;

      for (int i = 0; i < hitCount; i++)
      {
        var hit = hits[i];
        if (IsOwnCollider(hit.collider, ignoredColliders))
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
    /// false 이면 1회 평가하고, Branching은 브랜치 로컬 커서를 바꾸며 Panic은 시나리오를 중단한다.
    /// </summary>
    private IEnumerator ExecuteValidatorGate(ScenarioValidatorNode node, BranchChainContext context)
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
            // 브랜치 체인은 NextIdentifier 로 진행하므로, 대기를 끝내면 체인이 다음 노드로 이동한다.
            Debug.LogWarning($"[ScenarioController] Branch validator gate '{node.Identifier}' timed out after {node.WaitTimeoutSeconds}s; releasing gate (미수행 기록).");
            yield break;

          case ScenarioValidatorWaitTimeoutBehavior.FailBranch:
            if (!string.IsNullOrWhiteSpace(node.FailureNextIdentifier))
              context.NextOverride = node.FailureNextIdentifier;
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
      if (node.OnFailure == ScenarioValidatorOnFailure.Branching
          && !string.IsNullOrWhiteSpace(node.FailureNextIdentifier))
      {
        context.NextOverride = node.FailureNextIdentifier;
      }
      else if (node.OnFailure == ScenarioValidatorOnFailure.Panic)
      {
        EndScenario();
      }
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

      // WaitMode.None은 의도적으로 즉시 다음 노드로 진행하므로 차단하지 않는다.
      bool blockGlobalAdvance = node.WaitMode != ScenarioWaitMode.None;
      bool advanceBlockReleased = false;
      if (blockGlobalAdvance)
        _parallelAdvanceBlockDepth++;

      try
      {
        var runningCoroutines = new List<Coroutine>();
        var runningTrackers = new List<BranchCompletionTracker>();
        var routinesByClient = new Dictionary<int, List<IEnumerator>>();
        int? localClientId = (int?)InstanceFinder.ClientManager?.Connection?.ClientId;
        var players = GetActivePlayerIds();
        bool sequenceRoleBranches = ShouldAllowMultipleRoleBranches(
          node,
          ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer);
        var allocation = new Dictionary<ScenarioParallelBranch, int?>();

        // activeRoleTags 기반 그래프는 플레이어/태그 등록이 완료되기 전에 첫 Parallel에
        // 도달할 수 있다. 이때 빈 roster를 "모든 역할 부재"로 해석하면 skipAbsentRoleBranches가
        // 모든 분기를 건너뛰고 WaitMode.All이 즉시 완료되어 분기 내부 게이트를 전부 우회한다.
        // 최소 한 역할이 확인된 뒤에만 실제 부재 역할을 계산한다.
        yield return WaitForInitialActiveRoleRoster(node);
        if (_currentGraph == null)
        {
          yield break;
        }

        if (!TryAllocateParallel(node, players, allocation))
        {
          EndScenario();
          yield break;
        }

        if (_executionMode == ExecutionMode.ServerAuthoritative)
          ScenarioNetworkRelay.PublishParallelAssignments(_currentGraph?.Identifier, node.Identifier, allocation);

        if (node.AllocationType == ScenarioParallelAllocationType.ByRole)
        {
          foreach (var assignedClientId in allocation.Values.Where(value => value.HasValue))
          {
            _activeRoleBranchDepthByClientId.TryGetValue(assignedClientId.Value, out var depth);
            _activeRoleBranchDepthByClientId[assignedClientId.Value] = depth + 1;
          }
        }

        foreach (var branch in node.Branches)
        {
          if (!_currentGraph.TryGetNode(branch.Identifier, out var branchNode))
          {
            Debug.LogWarning($"[ScenarioController] Parallel branch target '{branch.Identifier}' not found.");
            if (node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Panic)
            {
              EndScenario();
              yield break;
            }
            continue;
          }

          allocation.TryGetValue(branch, out var assignedClientId);

          if (assignedClientId == null
              && (_currentGraph?.SkipAbsentRoleBranches == true && IsDeclaredRoleAbsent(branch)
                  || node.WhenBranchingPlayerNotMatched == ScenarioParallelMismatchHandling.Ignore))
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
          if (_executionMode != ExecutionMode.ServerAuthoritative
              && assignedClientId.HasValue && localClientId.HasValue && assignedClientId.Value != localClientId.Value)
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
          runningTrackers.Add(tracker);
          var routine = RunTrackedBranch(branchNode, branch.CompletionConditionIdentifier, node.NextIdentifier, assignedClientId, tracker);
          if (!sequenceRoleBranches)
          {
            runningCoroutines.Add(StartCoroutine(routine));
            continue;
          }

          var clientKey = assignedClientId ?? int.MinValue;
          if (!routinesByClient.TryGetValue(clientKey, out var routines))
          {
            routines = new List<IEnumerator>();
            routinesByClient[clientKey] = routines;
          }
          routines.Add(routine);
        }

        // 한 플레이어에게 여러 역할 브랜치가 배정되면 UI와 입력이 겹치지 않도록
        // 그래프에 정의된 순서대로 실행한다. 플레이어별 시퀀스끼리는 계속 병렬 실행한다.
        foreach (var routines in routinesByClient.Values)
        {
          runningCoroutines.Add(StartCoroutine(RunSequentially(routines)));
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
        if (node.AllocationType == ScenarioParallelAllocationType.ByRole)
        {
          foreach (var assignedClientId in allocation.Values.Where(value => value.HasValue))
          {
            if (!_activeRoleBranchDepthByClientId.TryGetValue(assignedClientId.Value, out var depth))
              continue;
            if (depth <= 1)
              _activeRoleBranchDepthByClientId.Remove(assignedClientId.Value);
            else
              _activeRoleBranchDepthByClientId[assignedClientId.Value] = depth - 1;
          }
        }

        if (blockGlobalAdvance)
        {
          ReleaseParallelAdvanceBlock();
          advanceBlockReleased = true;
        }
        Advance();
      }
      finally
      {
        if (blockGlobalAdvance && !advanceBlockReleased)
          ReleaseParallelAdvanceBlock();
      }
    }

    private void ReleaseParallelAdvanceBlock()
    {
      _parallelAdvanceBlockDepth = Math.Max(0, _parallelAdvanceBlockDepth - 1);
    }

    /// <summary>병렬 브랜치의 완료 여부를 추적하는 플래그 홀더.</summary>
    private sealed class BranchCompletionTracker
    {
      public bool Completed;
    }

    private static IEnumerator RunSequentially(IReadOnlyList<IEnumerator> routines)
    {
      for (var index = 0; index < routines.Count; index++)
        yield return routines[index];
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

    private void ExecuteSignalListenerNode(ScenarioSignalListenerNode node)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.ListenerIdentifier))
      { Advance(); return; }
      if (node.Operation == ScenarioSignalListenerOperation.Unregister)
        ScenarioConditionalSignalListeners.Unregister(node.ListenerIdentifier);
      else
        ScenarioConditionalSignalListeners.Register(node.ListenerIdentifier, node.SourceSignalIdentifier, node.OutputSignalIdentifier, node.RequiredSignalIdentifiers, node.ConsumeOnce);
      Advance();
    }

    /// <summary>
    /// EntityStateSignalBinding 노드를 실행한다: 대상 엔티티의
    /// <see cref="Entity.IScenarioEntityStateEventSource"/> 에 상태 이벤트 → 신호 바인딩을 등록/해제한다.
    /// </summary>
    private void ExecuteEntityStateSignalBindingNode(ScenarioEntityStateSignalBindingNode node)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.BindingIdentifier))
      {
        Advance();
        return;
      }

      if (node.Operation == ScenarioEntityStateSignalBindingOperation.Unregister)
      {
        ScenarioEntityStateSignalBindings.Unregister(node.BindingIdentifier);
        Advance();
        return;
      }

      string entityIdentifier = ResolveEntityStateBindingTargetIdentifier(node);
      if (string.IsNullOrWhiteSpace(entityIdentifier))
      {
        Debug.LogWarning($"[ScenarioController] EntityStateSignalBinding '{node.Identifier}' target identifier is missing.");
        Advance();
        return;
      }

      if (!Registry.Registry.TryGetEntity(entityIdentifier, out var entityDescriptor)
          || entityDescriptor?.GameObject == null)
      {
        Debug.LogWarning($"[ScenarioController] EntityStateSignalBinding '{node.Identifier}' target '{entityIdentifier}' was not found.");
        Advance();
        return;
      }

      var source = entityDescriptor.GameObject.GetComponentInChildren<Entity.IScenarioEntityStateEventSource>(true);
      if (source == null)
      {
        Debug.LogWarning($"[ScenarioController] EntityStateSignalBinding '{node.Identifier}' target '{entityIdentifier}' has no IScenarioEntityStateEventSource.");
        Advance();
        return;
      }

      bool ok = ScenarioEntityStateSignalBindings.Register(
        node.BindingIdentifier,
        source,
        node.EventName,
        node.EventKey,
        node.OutputSignalIdentifier,
        node.ConsumeOnce);

      if (!ok)
      {
        Debug.LogWarning($"[ScenarioController] EntityStateSignalBinding '{node.Identifier}' failed to register event '{node.EventName}' on target '{entityIdentifier}'.");
      }

      Advance();
    }

    private string ResolveEntityStateBindingTargetIdentifier(ScenarioEntityStateSignalBindingNode node)
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
    /// SignalCounter 노드를 실행한다: 접두사 매칭 신호의 distinct 개수를 세어 임계치 도달 시
    /// 출력 신호를 발신하는 카운터를 등록/해제한다.
    /// </summary>
    private void ExecuteSignalCounterNode(ScenarioSignalCounterNode node)
    {
      if (node == null || string.IsNullOrWhiteSpace(node.CounterIdentifier))
      {
        Advance();
        return;
      }

      if (node.Operation == ScenarioSignalCounterOperation.Unregister)
        ScenarioSignalCounters.Unregister(node.CounterIdentifier);
      else
      {
        Func<IReadOnlyCollection<string>> expectedSignals = null;
        int threshold = node.Threshold;
        if (node.UseActiveRoleRosterThreshold)
        {
          if (!TryGetActiveRoleRoster(out var roster, out var error))
          {
            Debug.LogError($"[ScenarioController] SignalCounter active-role roster failed: {error}", this);
            EndScenario();
            return;
          }

          threshold = roster.Count;
          expectedSignals = () => TryGetActiveRoleRoster(out var current, out _)
            // 한 플레이어가 여러 역할을 맡는 단독 디버그에서는 역할마다 같은 도착 신호가 생긴다.
            // 도착 완료는 역할 수가 아니라 실제 플레이어별 1회 도착으로 판단한다.
            ? current.Select(entry => ScenarioInteractionSignals.Normalize(node.SourceSignalPrefix + entry.PlayerIdentifier))
              .Distinct(StringComparer.Ordinal)
              .ToArray()
            : Array.Empty<string>();
        }

        ScenarioSignalCounters.Register(
          node.CounterIdentifier,
          node.SourceSignalPrefix,
          threshold,
          node.OutputSignalIdentifier,
          expectedSignals);
      }

      Advance();
    }

    private static bool ShouldAllowMultipleRoleBranches(
      ScenarioParallelNode node,
      bool enabled)
      => enabled
        && node?.AllocationType == ScenarioParallelAllocationType.ByRole
        && node.WaitMode == ScenarioWaitMode.All;

    private IEnumerator WaitForInitialActiveRoleRoster(ScenarioParallelNode node)
    {
      if (node?.AllocationType != ScenarioParallelAllocationType.ByRole
          || _currentGraph?.SkipAbsentRoleBranches != true
          || _currentGraph.ActiveRoleTags == null
          || _currentGraph.ActiveRoleTags.Count == 0)
      {
        yield break;
      }

      bool reported = false;
      while (_currentGraph != null)
      {
        if (!TryGetActiveRoleRoster(out var roster, out _)
            || roster.Count > 0)
        {
          yield break;
        }

        if (!reported)
        {
          Debug.Log($"[ScenarioController] Parallel '{node.Identifier}' is waiting for the initial active-role roster.");
          reported = true;
        }

        yield return null;
      }
    }

    private IEnumerator ExecuteBranch(IScenarioNode node, string completionCondition, string joinNodeIdentifier, int? branchOwnerClientId)
    {
      // _scenarioOwnerClientId는 전역 시나리오 입력 권한을 나타내는 상태다. 병렬 코루틴이
      // 이를 임시로 교체하면 A 브랜치가 yield한 사이 B 브랜치가 owner를 덮어써, Quest/이동 등
      // 후속 노드가 잘못된 역할에 적용되는 경쟁 조건이 생긴다. 역할 owner는 아래의 명시적
      // branchOwnerClientId로만 전달하고, 역할별 표현은 TargetRpc 경로에서 처리한다.
      // 브랜치의 시작 노드부터 NextIdentifier 체인을 끝까지(또는 완료조건 라벨까지) 실행한다.
      // 완료조건/합류 라벨 도달은 전역 Advance/EndScenario를 건드리지 않는 브랜치 완료다.
      yield return RunBranchChain(node, completionCondition, joinNodeIdentifier, branchOwnerClientId);
    }

    /// <summary>
    /// 병렬 브랜치 전용 자가완결 실행기.
    /// 전역 <see cref="_currentNode"/> / <see cref="Advance"/> / <see cref="EndScenario"/> 에 의존하지 않고,
    /// 시작 노드부터 NextIdentifier 체인을 따라 노드를 하나씩 실행·대기한다.
    /// 다음 식별자가 비어 있거나(터미널) 완료조건 라벨(<paramref name="completionLabel"/>)과 같거나
    /// 그래프에 존재하지 않으면 브랜치를 종료한다.
    /// </summary>
    private IEnumerator RunBranchChain(
      IScenarioNode startNode,
      string completionLabel,
      string joinNodeIdentifier = null,
      int? branchOwnerClientId = null)
    {
      var cursor = startNode;
      int guard = 0;
      const int maxNodes = 10000; // 순환 방지 안전장치.
      // 브랜치 체인별 실행 컨텍스트(동시 실행되는 다른 브랜치와 상태를 공유하지 않는다).
      var chainContext = new BranchChainContext(branchOwnerClientId);

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

        var branchVisitSequence = RecordNodeVisit(cursor);
        OnNodeChanged?.Invoke(cursor);

        // Send branch dialogues only after their per-screen queue slot is acquired.
        if (_executionMode == ExecutionMode.ServerAuthoritative
            && branchOwnerClientId.HasValue
            && cursor is not ScenarioDialogueNode)
          ScenarioNetworkRelay.PresentAuthoritativeNodeToClient(branchOwnerClientId.Value, _currentGraph.Identifier, cursor.Identifier);

        // 단일 노드를 실행하고 완료를 대기한다(전역 Advance 미사용).
        // 분기 코루틴이 실제로 MoveNext 되는 순간에만 전역 Advance 를 억제한다.
        // 대기 중인 WaitMode.None 분기가 억제 상태를 계속 점유하면 메인 체인의
        // Advance 까지 차단되므로, 코루틴 전체 수명 동안 억제해서는 안 된다.
        chainContext.NextOverride = null;
        yield return RunWithGlobalAdvanceSuppressed(ExecuteBranchNode(cursor, chainContext));
        CompleteNodeVisit(branchVisitSequence);

        // 노드 대기 도중 시나리오가 종료되어 그래프가 해제됐을 수 있으므로 재확인한다.
        if (_currentGraph == null)
        {
          yield break;
        }

        // 명시적 종료 표식: 곁가지를 닫고 원래 흐름으로 돌아간다.
        // 이 노드는 다음으로 잇지 않으므로 NextIdentifier / NextOverride 를 보지 않는다.
        if (cursor is ScenarioReturnToOriginNode)
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
      public BranchChainContext(int? ownerClientId) => OwnerClientId = ownerClientId;

      public int? OwnerClientId { get; }

      /// <summary>Choice/Quiz 등 선택 결과가 다음 노드를 결정하는 경우 설정된다.</summary>
      public string NextOverride;
    }

    private bool ShouldPresentBranchLocally(BranchChainContext context)
    {
      if (_executionMode != ExecutionMode.ServerAuthoritative || !context.OwnerClientId.HasValue)
        return true;

      var local = InstanceFinder.IsClientStarted
        ? InstanceFinder.ClientManager?.Connection
        : null;
      return local != null && local.ClientId == context.OwnerClientId.Value;
    }

    /// <summary>
    /// 브랜치 내부에서 단일 노드를 실행하고 그 노드가 완료될 때까지 대기한다.
    /// 각 노드 실행기는 내부적으로 전역 <see cref="Advance"/> 를 호출하지만, 브랜치 체인에서는
    /// 그 진행을 사용하지 않고 NextIdentifier 로 직접 이동하므로 부작용이 격리된다.
    /// 코루틴형 노드(예: Delay/InvokeEvent(WaitUntilDone)/Sound)는 완료까지 yield 로 대기한다.
    /// </summary>
    private IEnumerator ExecuteBranchNode(IScenarioNode node, BranchChainContext context)
    {
      switch (node)
      {
        case ScenarioDelayNode delay:
          yield return ExecuteDelayNode(delay);
          break;
        case ScenarioInvokeEventNode invoke:
          if (invoke.InvokeOnRoleClient
              && context.OwnerClientId.HasValue
              && !ShouldPresentBranchLocally(context))
          {
            // TargetPresentRoleNode가 배정 클라이언트에서 표시용 핸들러를 실행한다.
            break;
          }
          // WaitUntilDone/Immediately 모두 실행기 말미에 전역 Advance 를 호출하지만,
          // RunWithGlobalAdvanceSuppressed 가 각 MoveNext 순간에만 이를 억제한다.
          if (invoke.MoveNextBehavior == ScenarioInvokeEventMoveNextBehavior.WaitUntilDone)
          {
            yield return ExecuteInvokeEventNode(invoke);
          }
          else
          {
            StartCoroutine(RunWithGlobalAdvanceSuppressed(ExecuteInvokeEventNode(invoke)));
          }
          break;
        case ScenarioSoundNode sound:
          yield return ExecuteSoundNode(sound);
          break;
        case ScenarioValidatorNode validator:
          yield return ExecuteValidatorGate(validator, context);
          break;
        case ScenarioInteractionNode interaction:
          yield return ExecuteInteractionNode(interaction);
          break;
        case ScenarioCombineItemNode combineItem:
          yield return ExecuteCombineItemNode(combineItem);
          break;
        case ScenarioSignalListenerNode signalListener:
          ExecuteSignalListenerNode(signalListener);
          break;
        case ScenarioEntityStateSignalBindingNode stateBinding:
          ExecuteEntityStateSignalBindingNode(stateBinding);
          break;
        case ScenarioSignalCounterNode signalCounter:
          ExecuteSignalCounterNode(signalCounter);
          break;
        case ScenarioDialogueNode dialogue:
          yield return ExecuteDialogueNodeInBranch(dialogue, context);
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
          ExecuteQuestControlNode(questControl, context.OwnerClientId);
          break;
        case ScenarioQuestWaypointHighlightNode waypointHighlight:
          ExecuteQuestWaypointHighlightNode(waypointHighlight);
          break;
        case ScenarioQuestMarkNode questMark:
          // 브랜치 노드는 자동 표시 브로드캐스트 대상이 아니므로 표시 클라이언트에 직접 전달한다.
          ScenarioNetworkRelay.PresentAuthoritativeNode(_currentGraph?.Identifier, questMark.Identifier);
          ApplyQuestMarkNode(questMark);
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
        case ScenarioNPCControlNode npcControl:
          yield return ExecuteNPCControlNode(npcControl);
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
        case ScenarioPatientMedicalStatePresetNode patientPreset:
          // 수동 진입 준비 체인이 환자 의료 상태를 복원할 때 쓴다. 메인 경로와 같은 실행기를
          // 대기시켜야, 프리셋 적용과 RPC 전파가 끝난 뒤 다음 준비 노드로 넘어간다.
          yield return ExecutePatientMedicalStatePresetNode(patientPreset);
          break;
        case ScenarioTimeControlNode timeControl:
          ExecuteTimeControlNode(timeControl);
          break;
        case ScenarioLifecycleNode lifecycle:
          // 종료/재시작은 전역 커서를 바꾼다. 그 경우 아래 _currentGraph 재확인에서 체인이 끝난다.
          ExecuteLifecycleNode(lifecycle);
          break;
        case ScenarioManualEntrypointNode:
          // 표식일 뿐이라 브랜치 안에서는 통과시킨다. 준비 체인은 명령 진입 경로에서만 돈다.
          break;
        case ScenarioReturnToOriginNode:
          // 실행할 것은 없다. 체인 종료는 RunBranchChain 이 이 타입을 보고 처리한다.
          break;
        case ScenarioBedSnapNode bedSnap:
          ExecuteBedSnapNode(bedSnap);
          break;
        default:
          Debug.LogWarning($"[ScenarioController] Unsupported node type in branch chain: {node.GetType().Name} (id='{node.Identifier}'). Skipping.");
          break;
      }
    }

    /// <summary>
    /// 분기 노드 코루틴을 실행하되, 해당 코루틴(및 중첩 IEnumerator)이 실제로
    /// MoveNext 되는 동안에만 전역 Advance 를 억제한다.
    /// </summary>
    private IEnumerator RunWithGlobalAdvanceSuppressed(IEnumerator routine)
    {
      if (routine == null)
        yield break;

      try
      {
        while (true)
        {
          bool hasNext;
          object yielded = null;
          _globalAdvanceSuppressionDepth++;
          try
          {
            hasNext = routine.MoveNext();
            if (hasNext)
              yielded = routine.Current;
          }
          finally
          {
            _globalAdvanceSuppressionDepth--;
          }

          if (!hasNext)
            yield break;

          if (yielded is IEnumerator nested)
            yield return RunWithGlobalAdvanceSuppressed(nested);
          else
            yield return yielded;
        }
      }
      finally
      {
        // StopCoroutine/시나리오 종료로 래퍼가 중단돼도 원래 코루틴의 finally
        // 정리 로직이 실행되게 한다. Dispose 중 발생하는 Advance 역시 분기 진행이므로 억제한다.
        _globalAdvanceSuppressionDepth++;
        try
        {
          (routine as IDisposable)?.Dispose();
        }
        finally
        {
          _globalAdvanceSuppressionDepth--;
        }
      }
    }

    /// <summary>
    /// 선택지 선택을 브랜치 체인으로 위임하기 위한 인터셉터.
    /// 값이 설정되어 있으면 <see cref="SelectOption"/> 이 전역 진행 대신 이 콜백을 호출한다.
    /// </summary>
    private Action<int> _branchOptionInterceptor;
    // Branch dialogue input must complete only its branch, never Advance the enclosing main node.
    private readonly Dictionary<int, Action> _branchDialogueAdvanceInterceptors = new();

    // Branch dialogues share one per-screen queue, independent from Choice and Quiz prompts.
    private bool _branchDialogueActive;
    private readonly HashSet<int> _activeRemoteBranchDialogueClients = new();

    private readonly Dictionary<string, BranchOptionSelection> _remoteBranchChoiceSelections = new(StringComparer.Ordinal);
    private readonly HashSet<int> _activeRemoteBranchPromptClients = new();

    /// <summary>
    /// 이 컨트롤러의 로컬 화면에 브랜치 프롬프트(Choice/Quiz)가 표시 중인 동안 true.
    /// 로컬 다이얼로그 UI와 인터셉터는 하나뿐이므로 로컬 프롬프트만 직렬화한다.
    /// 원격 프롬프트는 대상 클라이언트별 잠금으로 독립적으로 진행한다.
    /// </summary>
    private bool _branchPromptActive;

    /// <summary>브랜치 프롬프트의 선택 결과 홀더.</summary>
    private sealed class BranchOptionSelection
    {
      public bool Resolved;
      public int Index = -1;
      public int OptionCount;
    }

    private static string BuildRemoteBranchChoiceKey(int clientId, string graphIdentifier, string nodeIdentifier)
      => $"{clientId}|{graphIdentifier}|{nodeIdentifier}";

    private bool TryResolveRemoteBranchChoice(
      int senderClientId,
      string graphIdentifier,
      string nodeIdentifier,
      int optionIndex)
    {
      string key = BuildRemoteBranchChoiceKey(senderClientId, graphIdentifier, nodeIdentifier);
      if (!_remoteBranchChoiceSelections.TryGetValue(key, out var selection)
          || selection == null
          || selection.Resolved
          || optionIndex < 0
          || optionIndex >= selection.OptionCount)
        return false;

      selection.Index = optionIndex;
      selection.Resolved = true;
      return true;
    }

    /// <summary>
    /// 브랜치 프롬프트 실행 공용 루틴: (1) 같은 화면의 다른 브랜치 프롬프트가 끝날 때까지 대기(직렬화),
    /// (2) present 콜백으로 UI 표시, (3) 인터셉터로 선택을 수신할 때까지 대기.
    /// 시나리오 종료 시 selection.Resolved == false 상태로 종료된다.
    /// </summary>
    private IEnumerator RunBranchPrompt(
      Action present,
      BranchOptionSelection selection,
      string nodeIdentifier,
      int? branchOwnerClientId)
    {
      int? localClientId = InstanceFinder.IsClientStarted
        ? InstanceFinder.ClientManager?.Connection?.ClientId
        : null;
      bool remotePrompt = branchOwnerClientId.HasValue
        && (!localClientId.HasValue || localClientId.Value != branchOwnerClientId.Value);

      // 로컬 프롬프트는 단일 UI를 공유하고, 원격 프롬프트는 같은 대상 클라이언트의
      // 단일 UI만 공유한다. 서로 다른 원격 클라이언트의 프롬프트는 병렬 진행한다.
      while (remotePrompt
               ? _activeRemoteBranchPromptClients.Contains(branchOwnerClientId.Value)
               : _branchPromptActive)
      {
        if (_currentGraph == null)
          yield break;
        yield return null;
      }

      if (_currentGraph == null)
        yield break;

      // 다른 그래프/흐름이 이미 대화창을 점유 중이면 정책을 적용한다.
      // (같은 화면의 동시 프롬프트는 위 화면별 직렬화로 이미 처리되므로
      //  considerBranchPrompt: false 로 교차 그래프 점유만 검사한다.)
      if (!remotePrompt && !TryClaimDialogueUI(considerBranchPrompt: false))
      {
        // Cancel: 이 브랜치 프롬프트를 표시하지 않고 미해결 상태로 종료.
        // Panic: TryClaimDialogueUI 내부 EndScenario 로 _currentGraph 가 이미 정리됨.
        yield break;
      }

      if (remotePrompt)
        _activeRemoteBranchPromptClients.Add(branchOwnerClientId.Value);
      else
        _branchPromptActive = true;
      string remoteKey = null;
      try
      {
        if (remotePrompt)
        {
          remoteKey = BuildRemoteBranchChoiceKey(
            branchOwnerClientId.Value,
            _currentGraph?.Identifier,
            nodeIdentifier);
          _remoteBranchChoiceSelections[remoteKey] = selection;
        }
        else
        {
          present();
          _branchOptionInterceptor = index =>
          {
            selection.Resolved = true;
            selection.Index = index;
          };
        }

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
        if (!string.IsNullOrEmpty(remoteKey))
          _remoteBranchChoiceSelections.Remove(remoteKey);
        if (remotePrompt)
          _activeRemoteBranchPromptClients.Remove(branchOwnerClientId.Value);
        else
          _branchPromptActive = false;
      }
    }

    /// <summary>Serializes branch dialogues per screen and completes them only from player input.</summary>
    private IEnumerator ExecuteDialogueNodeInBranch(ScenarioDialogueNode node, BranchChainContext context)
    {
      // 원격 표시 판정은 다른 브랜치 노드와 동일한 규칙(ShouldPresentBranchLocally)을 따른다.
      // ServerAuthoritative 가 아닌 실행 모드에서는 owner 가 배정돼 있어도 이 인스턴스가 직접
      // 표시해야 한다. (실행 모드 검사를 빠뜨리면 Local 모드에서 대화를 표시하지 않은 채
      // 서버 전용 릴레이만 호출되어 아무 화면에도 대사가 뜨지 않는다.)
      bool remoteDialogue = _executionMode == ExecutionMode.ServerAuthoritative
        && !ShouldPresentBranchLocally(context);

      while (remoteDialogue
               ? _activeRemoteBranchDialogueClients.Contains(context.OwnerClientId.Value)
               : _branchDialogueActive)
      {
        if (_currentGraph == null)
          yield break;
        yield return null;
      }

      if (_currentGraph == null)
        yield break;

      if (!remoteDialogue && !_uiController.IsUnityNull()
          && !TryClaimDialogueUI(considerBranchPrompt: false))
      {
        yield break;
      }

      if (remoteDialogue)
        _activeRemoteBranchDialogueClients.Add(context.OwnerClientId.Value);
      else
        _branchDialogueActive = true;

      int dialogueOwnerClientId = context.OwnerClientId ?? int.MinValue;
      bool advanceRequested = false;
      Action advanceBranchDialogue = () => advanceRequested = true;
      try
      {
        if (remoteDialogue)
        {
          ScenarioNetworkRelay.PresentAuthoritativeNodeToClient(
            context.OwnerClientId.Value,
            _currentGraph.Identifier,
            node.Identifier);
        }
        else if (!_uiController.IsUnityNull())
        {
          string speakerName = ResolveScenarioText(node.SpeakerName, context.OwnerClientId);
          string dialogueContent = ResolveScenarioText(node.DialogueContent, context.OwnerClientId);
          _uiController.DisplayDialogue(
            speakerName,
            dialogueContent,
            node.PortraitSpriteIdentifier,
            node.InteractionRequired);
          if (node.PlayTTS)
            PlayInlineTTS(node.Identifier, ResolveTTSText(node.DialogueContent, node.DialogueContentTTSPassing, context.OwnerClientId), node.TtsVoiceIdentifier);
        }

        _branchDialogueAdvanceInterceptors[dialogueOwnerClientId] = advanceBranchDialogue;
        yield return new WaitUntil(() => advanceRequested || _currentGraph == null);
      }
      finally
      {
        if (_branchDialogueAdvanceInterceptors.TryGetValue(dialogueOwnerClientId, out var current)
            && current == advanceBranchDialogue)
          _branchDialogueAdvanceInterceptors.Remove(dialogueOwnerClientId);

        if (remoteDialogue)
          _activeRemoteBranchDialogueClients.Remove(context.OwnerClientId.Value);
        else
          _branchDialogueActive = false;
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
      selection.OptionCount = node.Options?.Count ?? 0;
      yield return RunBranchPrompt(() =>
      {
        _state = State.ExecutingChoice;
        PresentChoice(node);
      }, selection, node.Identifier, context.OwnerClientId);

      if (!selection.Resolved)
      {
        yield break; // 시나리오 종료 등으로 미해결 종료.
      }

      if (node.Options != null && selection.Index >= 0 && selection.Index < node.Options.Count)
      {
        var option = node.Options[selection.Index];
        OnOptionSelected?.Invoke(option);
        RecordChoiceAssessment(node, selection.Index);
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
      selection.OptionCount = node.Options?.Count ?? 0;
      yield return RunBranchPrompt(() =>
      {
        _state = State.ExecutingQuiz;
        PresentQuiz(node);
      }, selection, node.Identifier, context.OwnerClientId);

      if (!selection.Resolved)
      {
        yield break; // 시나리오 종료 등으로 미해결 종료.
      }

      bool isCorrect = selection.Index == node.CorrectIndex;
      ShowQuizFeedback(node, isCorrect);
      context.NextOverride = ResolveQuizTarget(node, isCorrect);

      ClearOptions();
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

    private sealed class ActiveRoleRosterEntry
    {
      public string Role;
      public int ClientId;
      public string PlayerIdentifier;
    }

    private bool TryGetActiveRoleRoster(out List<ActiveRoleRosterEntry> roster, out string error)
    {
      roster = new List<ActiveRoleRosterEntry>();
      error = null;
      var declaredRoles = _currentGraph?.ActiveRoleTags?
        .Where(role => !string.IsNullOrWhiteSpace(role))
        .Distinct(StringComparer.Ordinal)
        .ToArray() ?? Array.Empty<string>();
      if (declaredRoles.Length == 0)
      {
        error = "graph has no activeRoleTags";
        return false;
      }

      var holderByRole = new Dictionary<string, ActiveRoleRosterEntry>(StringComparer.Ordinal);
      var activePlayers = GetActivePlayerIds()
        .Select(clientId => UserDescriptorService.TryGetByClientId(clientId, out var player)
          ? (ClientId: clientId, Player: player)
          : (ClientId: clientId, Player: null))
        .Where(each => each.Player != null && !string.IsNullOrWhiteSpace(each.Player.Identifier))
        .ToArray();
      bool allowSinglePlayerMultipleRoles = ShouldAllowMultipleActiveRolesForSinglePlayer(
        ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer,
        activePlayers.Length);

      foreach (var activePlayer in activePlayers)
      {
        var clientId = activePlayer.ClientId;
        var player = activePlayer.Player;

        var roles = declaredRoles.Where(role => PlayerTagService.HasTag(player.Identifier, role)).ToArray();
        if (roles.Length == 0)
          continue;
        if (roles.Length > 1 && !allowSinglePlayerMultipleRoles)
        {
          error = $"player '{player.Identifier}' has multiple active roles [{string.Join(", ", roles)}]";
          return false;
        }

        foreach (var role in roles)
        {
          var entry = new ActiveRoleRosterEntry
          {
            Role = role,
            ClientId = clientId,
            PlayerIdentifier = player.Identifier
          };
          if (holderByRole.TryGetValue(entry.Role, out var duplicate))
          {
            error = $"duplicate active role '{entry.Role}' on players '{duplicate.PlayerIdentifier}' and '{entry.PlayerIdentifier}'";
            return false;
          }
          holderByRole.Add(entry.Role, entry);
        }
      }

      roster.AddRange(holderByRole.Values.OrderBy(entry => Array.IndexOf(declaredRoles, entry.Role)));
      return true;
    }

    private static bool ShouldAllowMultipleActiveRolesForSinglePlayer(bool enabled, int activePlayerCount)
      => enabled && activePlayerCount == 1;

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
            if (_currentGraph?.ActiveRoleTags?.Count > 0)
              return TryAllocateActiveRoleParallel(node, branches, allocation);

            // 각 브랜치를 자격에 맞는 서로 다른 플레이어에게 1:1로 배정한다.
            // 후보 산출은 현재 실행 권위(서버)의 세션/태그 상태에서 수행하고, 순수 배정 규칙은
            // ScenarioParallelRoleAllocator로 위임한다. P2 서버 상태기와 같은 규칙을 공유한다.
            var candidatesByBranch = new Dictionary<ScenarioParallelBranch, IReadOnlyList<int>>();
            foreach (var branch in branches)
            {
              candidatesByBranch[branch] = playerPool
                  .Where(clientId => IsPlayerEligibleForBranch(branch, clientId))
                  .ToList();
            }

            if (!ScenarioParallelRoleAllocator.TryAllocateDistinct(branches, candidatesByBranch, allocation))
            {
              if (ShouldAllowMultipleRoleBranches(
                    node,
                    ScenarioGameRules.AllowMultipleRoleBranchesForSinglePlayer)
                  && ScenarioParallelRoleAllocator.TryAllocateAllowingDuplicates(
                    branches, candidatesByBranch, allocation))
              {
                Debug.Log("[ScenarioController] ByRole branches with duplicate player assignments will run sequentially per player.");
                return true;
              }

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
                // ByRole의 distinct matching이 실패했다면 같은 후보 집합으로 중복 없는 재배정은 불가능하다.
                // 공통 round-robin은 역할 자격을 무시하고 한 플레이어에게 UI 브랜치를 겹쳐 배정하므로 금지한다.
                Debug.LogWarning("[ScenarioController] ByRole reallocation cannot complete without duplicate player assignments.");
                return false;
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
          ReportIgnoredParallelAllocation(node, branchCount, playerCount, playerPool, assignNull);
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

    private void ReportIgnoredParallelAllocation(
      ScenarioParallelNode node,
      int branchCount,
      int playerCount,
      List<int> playerPool,
      bool allBranchesSkipped)
    {
      var graphIdentifier = _currentGraph?.Identifier ?? "<unknown>";
      var activePlayers = playerPool == null || playerPool.Count == 0
        ? "none"
        : string.Join(", ", playerPool);
      var nextNode = string.IsNullOrWhiteSpace(node?.NextIdentifier) ? "<end scenario>" : node.NextIdentifier;
      var branchRequirements = string.Join("; ", (node?.Branches ?? Array.Empty<ScenarioParallelBranch>())
        .Select(branch =>
        {
          var tags = branch?.RequiredPlayerTags?.Where(tag => !string.IsNullOrWhiteSpace(tag)).ToArray() ?? Array.Empty<string>();
          return $"{branch?.Identifier ?? "<unknown>"} requires [{string.Join(", ", tags)}]";
        }));

      var outcome = allBranchesSkipped
        ? $"All {branchCount} branches were left unassigned and skipped; WaitMode={node?.WaitMode} therefore completes immediately and advances to '{nextNode}'."
        : $"One or more branches were left unassigned; configured mismatch handling continues toward '{nextNode}'.";
      var message = $"[ScenarioController] WARNING: Parallel allocation mismatch was ignored. graph='{graphIdentifier}', node='{node?.Identifier}', allocation={node?.AllocationType}, activePlayers=[{activePlayers}], {outcome} Requirements: {branchRequirements}";

      Debug.LogWarning(message, this);
      AppendSystemChatMessage(message);
      GameLogService.WriteScenario(message, graphIdentifier);
      AddCurrentVisitNote(message);
    }

    private bool IsPlayerEligibleForBranch(ScenarioParallelBranch branch, int clientId, bool allowTagGateBypass = true)
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
          return allowTagGateBypass
            && TryIgnoreMissingTagGate(branch.Identifier, $"player descriptor for clientId {clientId} is unavailable while required tags are configured");
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
            return allowTagGateBypass
              && TryIgnoreMissingTagGate(branch.Identifier, $"player '{session.Identifier}' has none of the required tags [{string.Join(", ", requiredTags)}]");
          }
        }
        else
        {
          foreach (var requiredTag in requiredTags)
          {
            if (!PlayerTagService.HasTag(session.Identifier, requiredTag))
            {
              return allowTagGateBypass
                && TryIgnoreMissingTagGate(branch.Identifier, $"player '{session.Identifier}' is missing required tag '{requiredTag}'");
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

    private bool TryAllocateActiveRoleParallel(
      ScenarioParallelNode node,
      IReadOnlyList<ScenarioParallelBranch> branches,
      IDictionary<ScenarioParallelBranch, int?> allocation)
    {
      if (!TryGetActiveRoleRoster(out var roster, out var error))
      {
        Debug.LogError($"[ScenarioController] ByRole active roster failed: {error}", this);
        return false;
      }

      // 한 플레이어가 여러 역할을 맡은 경우에도 실제로 부여된 역할의 브랜치만 배정한다.
      // 미보유 역할 브랜치는 SkipAbsentRoleBranches 정책에 따라 스킵하고, 같은 클라이언트에
      // 중복 배정된 브랜치는 실행 단계에서 그래프 정의 순서대로 순차 실행한다.
      var roles = new HashSet<string>(_currentGraph.ActiveRoleTags, StringComparer.Ordinal);
      var holderClientIdByRole = roster.ToDictionary(
        entry => entry.Role,
        entry => entry.ClientId,
        StringComparer.Ordinal);
      if (!TryAssignActiveRoleBranches(
            branches,
            roles,
            holderClientIdByRole,
            _currentGraph.SkipAbsentRoleBranches,
            (branch, clientId) => IsPlayerEligibleForBranch(branch, clientId, allowTagGateBypass: false),
            allocation,
            out var assignmentError))
      {
        Debug.LogError($"[ScenarioController] {assignmentError}", this);
        return false;
      }

      return true;
    }

    /// <summary>
    /// activeRoleTags 기반 ByRole 브랜치를 각 역할의 홀더에게 배정한다.
    /// 각 브랜치는 정확히 하나의 activeRoleTag를 요구해야 하며, 연결된 홀더가 없는 역할의
    /// 브랜치는 skipAbsentRoleBranches이면 null(스킵)로 남기고 아니면 오류로 처리한다.
    /// 한 플레이어가 여러 역할을 보유하면 보유한 역할의 브랜치만 같은 클라이언트에 배정된다.
    /// </summary>
    private static bool TryAssignActiveRoleBranches(
      IReadOnlyList<ScenarioParallelBranch> branches,
      System.Collections.Generic.ISet<string> activeRoleTags,
      IReadOnlyDictionary<string, int> holderClientIdByRole,
      bool skipAbsentRoleBranches,
      Func<ScenarioParallelBranch, int, bool> isHolderEligible,
      IDictionary<ScenarioParallelBranch, int?> allocation,
      out string error)
    {
      error = null;
      foreach (var branch in branches)
      {
        var branchRoles = branch.RequiredPlayerTags?
          .Where(tag => activeRoleTags.Contains(tag))
          .Distinct(StringComparer.Ordinal)
          .ToArray() ?? Array.Empty<string>();
        if (branchRoles.Length == 0)
        {
          error = $"ByRole branch '{branch.Identifier}' must require exactly one activeRoleTag.";
          return false;
        }

        // 단일 역할 브랜치: 해당 역할의 홀더에게 배정한다.
        if (branchRoles.Length == 1)
        {
          if (!TryAssignSingleRoleBranch(branch, branchRoles[0], holderClientIdByRole, skipAbsentRoleBranches, isHolderEligible, allocation, ref error))
            return false;
          continue;
        }

        // 복수 역할 브랜치(requiredPlayerTagsMatchMode=Any): 선언된 순서대로 홀더가 연결된
        // 첫 번째 역할의 홀더에게 배정한다. 어느 역할도 연결되어 있지 않으면 단일 역할과
        // 같은 부재 정책(skipAbsentRoleBranches)을 따른다.
        if (branch.RequiredPlayerTagsMatchMode != ScenarioPlayerTagMatchMode.Any)
        {
          error = $"ByRole branch '{branch.Identifier}' must require exactly one activeRoleTag.";
          return false;
        }

        int? fallbackHolderClientId = null;
        bool anyRoleHolderConnected = false;
        foreach (var role in branchRoles)
        {
          if (!holderClientIdByRole.TryGetValue(role, out var roleHolderClientId))
            continue;
          anyRoleHolderConnected = true;
          if (!isHolderEligible(branch, roleHolderClientId))
            continue;
          fallbackHolderClientId = roleHolderClientId;
          break;
        }

        if (fallbackHolderClientId.HasValue)
        {
          allocation[branch] = fallbackHolderClientId;
          continue;
        }

        if (anyRoleHolderConnected)
        {
          error = $"ByRole holders for [{string.Join(", ", branchRoles)}] do not strictly satisfy branch '{branch.Identifier}'.";
          return false;
        }

        if (!skipAbsentRoleBranches)
        {
          error = $"ByRole branch '{branch.Identifier}' has no connected holder for roles [{string.Join(", ", branchRoles)}].";
          return false;
        }

        allocation[branch] = null;
      }

      return true;
    }

    private static bool TryAssignSingleRoleBranch(
      ScenarioParallelBranch branch,
      string role,
      IReadOnlyDictionary<string, int> holderClientIdByRole,
      bool skipAbsentRoleBranches,
      Func<ScenarioParallelBranch, int, bool> isHolderEligible,
      IDictionary<ScenarioParallelBranch, int?> allocation,
      ref string error)
    {
      if (holderClientIdByRole.TryGetValue(role, out var holderClientId))
      {
        if (!isHolderEligible(branch, holderClientId))
        {
          error = $"ByRole holder for '{role}' does not strictly satisfy branch '{branch.Identifier}'.";
          return false;
        }
        allocation[branch] = holderClientId;
        return true;
      }

      if (!skipAbsentRoleBranches)
      {
        error = $"ByRole branch '{branch.Identifier}' has no connected holder for role '{role}'.";
        return false;
      }

      allocation[branch] = null;
      return true;
    }

    private bool IsDeclaredRoleAbsent(ScenarioParallelBranch branch)
    {
      if (!TryGetActiveRoleRoster(out var roster, out _))
        return false;
      var activeRoles = new HashSet<string>(roster.Select(entry => entry.Role), StringComparer.Ordinal);
      var declaredRoles = new HashSet<string>(_currentGraph.ActiveRoleTags, StringComparer.Ordinal);
      var branchRoles = branch?.RequiredPlayerTags?.Where(declaredRoles.Contains).Distinct(StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
      if (branchRoles.Length == 1)
        return !activeRoles.Contains(branchRoles[0]);

      // 복수 역할 브랜치(Any): 어느 선언 역할의 홀더도 연결되어 있지 않으면 부재 브랜치로 본다.
      if (branchRoles.Length > 1 && branch.RequiredPlayerTagsMatchMode == ScenarioPlayerTagMatchMode.Any)
        return branchRoles.All(role => !activeRoles.Contains(role));

      return false;
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

        if (!EvaluateValidatorRootCondition(node, rootCondition, out var rootReason))
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

    private bool EvaluateValidatorRootCondition(ScenarioValidatorNode node, ScenarioValidatorRootCondition rootCondition, out string failureReason)
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

            bool anyMode = rootCondition.MatchMode == ScenarioValidatorMatchMode.Any;
            bool anyMatched = false;
            var anyModeFailures = anyMode ? new List<string>() : null;

            for (int i = 0; i < rules.Count; i++)
            {
              var rule = rules[i];
              if (rule == null)
              {
                if (anyMode)
                {
                  anyModeFailures.Add($"rule[{i}] is null.");
                }
                continue;
              }

              string misconfiguration = null;
              if (rule.Type != ScenarioValidatorRuleType.Registry)
              {
                misconfiguration = $"rule[{i}] has unsupported type '{rule.Type}'.";
              }
              else if (rule.Condition != ScenarioValidatorRuleCondition.Contains)
              {
                misconfiguration = $"rule[{i}] has unsupported condition '{rule.Condition}'.";
              }

              var ruleIdentifier = rule.RegistryIdentifier?.Trim();
              if (misconfiguration == null && string.IsNullOrWhiteSpace(ruleIdentifier))
              {
                misconfiguration = $"rule[{i}] registryIdentifier is null or empty.";
              }

              if (misconfiguration != null)
              {
                if (anyMode)
                {
                  anyModeFailures.Add(misconfiguration);
                  continue;
                }

                failureReason = misconfiguration;
                return false;
              }

              bool matched = Registry.Registry.Contains(rule.RegistryType, ruleIdentifier);
              if (anyMode)
              {
                if (matched)
                {
                  anyMatched = true;
                  break;
                }

                anyModeFailures.Add($"rule[{i}] identifier '{ruleIdentifier}' is not registered in {rule.RegistryType}.");
              }
              else if (!matched)
              {
                failureReason = $"rule[{i}] identifier '{ruleIdentifier}' is not registered in {rule.RegistryType}.";
                return false;
              }
            }

            if (anyMode && !anyMatched)
            {
              failureReason = $"no rule matched (Any mode). details: {string.Join(" | ", anyModeFailures)}";
              return false;
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
              return TryIgnoreMissingTagGate(node.Identifier, failureReason);
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
                return TryIgnoreMissingTagGate(node.Identifier, failureReason);
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
                  return TryIgnoreMissingTagGate(node.Identifier, failureReason);
                }
              case ScenarioValidatorPlayerScope.Owner:
                {
                  if (_scenarioOwnerClientId == null)
                  {
                    failureReason = "owner client id is not assigned for owner-scope tag validation.";
                    return TryIgnoreMissingTagGate(node.Identifier, failureReason);
                  }

                  if (!UserDescriptorService.TryGetByClientId(_scenarioOwnerClientId.Value, out var owner)
                      || owner == null
                      || string.IsNullOrWhiteSpace(owner.Identifier))
                  {
                    failureReason = $"owner descriptor not found for clientId {_scenarioOwnerClientId.Value}.";
                    return TryIgnoreMissingTagGate(node.Identifier, failureReason);
                  }

                  if (PlayerTagService.HasTag(owner.Identifier, tag))
                  {
                    return true;
                  }

                  var ownerLabel = !string.IsNullOrWhiteSpace(owner.DisplayName)
                      ? owner.DisplayName
                      : owner.Identifier;
                  failureReason = $"owner player '{ownerLabel}' does not have tag '{tag}'.";
                  return TryIgnoreMissingTagGate(node.Identifier, failureReason);
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
        Debug.LogWarning(
          $"[ScenarioController] ExecuteCommand node '{node?.Identifier}' 에 실행할 명령이 없어 건너뜁니다.");
        Advance();
        return;
      }

      if (InstanceFinder.IsServerStarted || InstanceFinder.IsOffline)
      {
        // 아이템 지급은 채팅 명령 서비스가 아니라 /give와 공유하는 지급 도메인 로직을 직접 호출한다.
        // 시나리오 실행은 UI/권한/채팅 전송과 무관한 서버 작업이므로 명령 파이프라인을 거치지 않는다.
        var ownerConnection = ResolveOwnerConnection();
        if (TryExecuteScenarioGive(node.CommandLine, ownerConnection, out var giveSucceeded, out var giveResult))
        {
          if (giveSucceeded)
            Debug.Log($"[ScenarioController] ExecuteCommand node '{node.Identifier}': {giveResult}");
          else
            Debug.LogWarning($"[ScenarioController] ExecuteCommand node '{node.Identifier}' failed: {giveResult}");
        }
        else
        {
          var chatService = ResolveChatService();
          if (chatService == null)
          {
            Debug.LogWarning($"[ScenarioController] ExecuteCommand node '{node.Identifier}' could not resolve ChatService; command skipped.");
          }
          else if (!chatService.TryExecuteSystemCommand(node.CommandLine.Trim(), ownerConnection, out var result))
          {
            Debug.LogWarning($"[ScenarioController] ExecuteCommand node '{node.Identifier}' failed: {result}");
          }
        }
      }

      Advance();
    }

    private static bool TryExecuteScenarioGive(string commandLine, NetworkConnection ownerConnection, out bool succeeded, out string result)
    {
      succeeded = false;
      result = string.Empty;
      string normalized = commandLine?.Trim().TrimStart('/');
      if (string.IsNullOrWhiteSpace(normalized))
        return false;

      string[] tokens = normalized.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
      if (tokens.Length == 0)
        return false;

      string[] giveArguments = tokens.Skip(1).ToArray();
      if (string.Equals(tokens[0], "give", StringComparison.OrdinalIgnoreCase))
      {
        succeeded = CommandDefinition_Give.TryExecuteGive(ownerConnection, giveArguments, out result);
        return true;
      }

      if (string.Equals(tokens[0], "give-if-missing", StringComparison.OrdinalIgnoreCase))
      {
        succeeded = TryExecuteScenarioGiveIfMissing(ownerConnection, giveArguments, out result);
        return true;
      }

      return false;
    }

    /// <summary>
    /// 시나리오 시작 시처럼 재실행될 수 있는 흐름에서, 이미 지급한 안내 아이템을 중복 지급하지 않는다.
    /// 일반 채팅 <c>/give</c>의 의미는 변경하지 않고 시나리오 실행 명령에만 적용한다.
    /// </summary>
    private static bool TryExecuteScenarioGiveIfMissing(NetworkConnection ownerConnection, string[] args, out string result)
    {
      result = string.Empty;
      if (args == null || args.Length == 0)
      {
        result = "Usage: give-if-missing <item_identifier> [count=1] [target_selector]";
        return false;
      }

      string itemIdentifier = args[0];
      if (!Registry.Registry.Contains(RegistryType.Item, itemIdentifier))
      {
        result = $"Item '{itemIdentifier}' is not registered.";
        return false;
      }

      int count = 1;
      string targetSelector = null;
      if (args.Length >= 2)
      {
        if (int.TryParse(args[1], out int parsedCount))
        {
          if (parsedCount <= 0)
          {
            result = "Count must be greater than 0.";
            return false;
          }

          count = parsedCount;
          if (args.Length >= 3)
            targetSelector = args[2];
        }
        else
        {
          targetSelector = args[1];
        }
      }

      if (args.Length > 3)
      {
        result = "Usage: give-if-missing <item_identifier> [count=1] [target_selector]";
        return false;
      }

      List<NetworkConnection> targets;
      if (string.IsNullOrWhiteSpace(targetSelector))
      {
        if (ownerConnection == null || !ownerConnection.IsValid)
        {
          result = "System execution requires a target selector.";
          return false;
        }

        targets = new List<NetworkConnection> { ownerConnection };
      }
      else if (!TargetSelectorResolver.TryResolveTargets(ownerConnection, targetSelector, out targets, out var targetError))
      {
        result = targetError;
        return false;
      }

      int grantedPlayers = 0;
      int alreadyOwnedPlayers = 0;
      int unavailablePlayers = 0;
      int droppedCount = 0;
      foreach (var target in targets)
      {
        if (target?.FirstObject == null || !target.FirstObject.TryGetComponent<PlayerController>(out var player) || player == null)
        {
          unavailablePlayers++;
          continue;
        }

        if (player.CountItemInInventory(itemIdentifier) > 0)
        {
          alreadyOwnedPlayers++;
          continue;
        }

        var item = Registry.Registry.CreateItemInstance(itemIdentifier);
        if (item == null)
        {
          result = $"Item '{itemIdentifier}' data is unavailable.";
          return false;
        }

        item.CurrentStackCount = count;
        player.TryAddItemToInventory(item, out var leftover);
        if (leftover != null && leftover.CurrentStackCount > 0)
        {
          droppedCount += leftover.CurrentStackCount;
          player.TryDropItemInFront(leftover);
        }

        grantedPlayers++;
      }

      result = $"Granted '{itemIdentifier}' to {grantedPlayers} player(s); {alreadyOwnedPlayers} already had it"
        + (droppedCount > 0 ? $"; dropped {droppedCount} due to full inventories" : string.Empty)
        + (unavailablePlayers > 0 ? $"; {unavailablePlayers} target(s) were unavailable" : string.Empty)
        + ".";
      return true;
    }

    private ChatService ResolveChatService()
    {
      if (_chatService != null)
      {
        return _chatService;
      }

      if (Registry.Registry.TryGet<ChatService>(RegistryType.Service, Registry.Registry.TypeKey<ChatService>(), out _chatService))
      {
        return _chatService;
      }

      // ChatService가 어떤 GameObject에 배치되어 있든(분리 배치 포함) 해결할 수 있도록
      // 레지스트리 미등록 시 씬 전역 검색으로 폴백한다.
      _chatService = FindFirstObjectByType<ChatService>(FindObjectsInactive.Include);
      return _chatService;
    }

    /// <summary>
    /// 시나리오 owner 클라이언트의 서버 측 연결을 반환한다. owner가 없으면(시스템 시나리오)
    /// 호스트/오프라인의 로컬 연결을 폴백으로 사용하며, 서버 전용 컨텍스트에서는 null을 반환한다.
    /// </summary>
    private NetworkConnection ResolveOwnerConnection()
    {
      if (_scenarioOwnerClientId.HasValue)
      {
        var serverManager = InstanceFinder.ServerManager;
        if (serverManager != null)
        {
          foreach (var kvp in serverManager.Clients)
          {
            var candidate = kvp.Value;
            if (candidate != null && candidate.ClientId == _scenarioOwnerClientId.Value)
            {
              return candidate;
            }
          }
        }

        return null;
      }

      // ClientManager.Connection은 미접속 시 null이 아니라 EmptyConnection(ClientId -1)이므로
      // IsValid로 걸러내지 않으면 시스템 메시지/RPC가 잘못된 연결로 발송된다.
      var localConnection = InstanceFinder.ClientManager?.Connection;
      return localConnection != null && localConnection.IsValid ? localConnection : null;
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
      var message = $"Validator gate is waiting for its condition: graph='{graphIdentifier}', node='{node.Identifier}', reason={resolvedReason}";

      if ((_validatorBlockLogTargets & ScenarioValidatorBlockLogTarget.UnityConsole) != 0)
      {
        Debug.LogWarning($"[ScenarioController] {message}", this);
      }

      if ((_validatorBlockLogTargets & ScenarioValidatorBlockLogTarget.InGameChat) != 0)
      {
        AppendSystemChatMessage(message);
      }

      if ((_validatorBlockLogTargets & ScenarioValidatorBlockLogTarget.SessionLog) != 0)
      {
        GameLogService.WriteScenario($"WAITING: {message}", graphIdentifier);
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

      if (_chatUIController == null)
        return;

      try
      {
        _chatUIController.AppendMessage($"<color=#FFD700>[System]</color> {message}", true);
      }
      catch (Exception ex)
      {
        // 부가 UI가 아직 초기화되지 않았거나 파괴 중이어도 시나리오 진행은 중단하지 않는다.
        Debug.LogWarning($"[ScenarioController] System chat message could not be displayed: {ex.Message}");
      }
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
