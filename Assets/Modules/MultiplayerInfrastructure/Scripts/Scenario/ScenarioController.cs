using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Session;
using MultiplayerInfrastructure.Tag;
using MultiplayerInfrastructure.Scenario.Preflight;
using MultiplayerInfrastructure.TTS;
using FishNet.Object;
using FishNet;
using FishNet.Connection;
using Unity.VisualScripting;

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
    private Coroutine _dialogueAutoAdvanceRoutine;

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
      if (graph == null)
      {
        Debug.LogError("[ScenarioController] Cannot start scenario with null graph");
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

      _currentGraph = graph;
      _scenarioOwnerClientId = ownerClientId;

      // 시나리오가 요구하는 퀘스트 정의 include를 선로딩한다.
      QuestDefinitionRegistry.EnsureIncludesLoaded(graph.QuestDefinitionIncludes);

      ResolveUIControllers();
      ResolveTTSService();

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

      // 사전 요구사항 검증: 그래프가 요구하는 씬/레지스트리 요소가 준비됐는지 점검한다.
      // 정책이 AbortStart 이고 누락이 있으면 시작을 중단한다(경고 후 return).
      if (_preflightEnabled
          && !ScenarioPreflight.Run(graph, _preflightPolicy, AppendSystemChatMessage, out _))
      {
        _currentGraph = null;
        _currentNode = null;
        _state = State.Inactive;
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
      CancelDialogueAutoAdvance();

      // 아직 진행 중인 시나리오 코루틴(특히 WaitMode.None 으로 전역 시나리오보다 오래
      // 살아남는 병렬 브랜치)을 모두 정리한다. 이를 누락하면 그래프가 해제된 뒤에도
      // 브랜치 체인이 계속 돌면서 _currentGraph 역참조에서 NullReferenceException 이 발생한다.
      StopAllCoroutines();
      ScenarioInteractionSignals.ClearAllInternalSignals();

      _currentGraph = null;
      _currentNode = null;
      _state = State.Inactive;
      // 강제 종료된 브랜치 코루틴의 finally 가 실행되지 않을 수 있으므로 억제 카운터를 초기화한다.
      _globalAdvanceSuppressionDepth = 0;

      ClearOptions();
      _stateStore.Clear();

      // UI 종료
      if (!_uiController.IsUnityNull())
      {
        _uiController.EndScenario();
      }

      OnScenarioEnded?.Invoke();
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
      if (_state == State.ExecutingQuiz && _activeQuizNode != null)
      {
        HandleQuizSelection(index, _activeQuizNode);
        return;
      }

      if (index < 0 || index >= _activeOptions.Count)
      {
        Debug.LogWarning($"[ScenarioController] Invalid option index: {index}");
        return;
      }

      var option = _activeOptions[index];
      OnOptionSelected?.Invoke(option);

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
      OnNodeChanged?.Invoke(node);

      switch (node)
      {
        case ScenarioDialogueNode dialogue:
          ExecuteDialogueNode(dialogue);
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

    private void ExecuteDialogueNode(ScenarioDialogueNode node)
    {
      _state = State.ExecutingDialogue;

      // 이전 노드의 잔여 타이머가 있다면 정리.
      CancelDialogueAutoAdvance();

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayDialogue(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier, node.InteractionRequired);

        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, node.DialogueContent);

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
      _state = State.ExecutingChoice;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayChoice(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier, node.Options);

        if (node.PlayTTS)
          PlayInlineTTS(node.Identifier, node.DialogueContent);
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
      _state = State.ExecutingQuiz;
      _activeQuizNode = node;

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
          PlayInlineTTS(node.Identifier, node.Question);
      }
      else
      {
        Debug.LogWarning($"[ScenarioController] Quiz node '{node.Identifier}' cannot render choices because UI controller is missing.");
        ResolveQuizNext(node, false);
      }
    }

    private void ExecuteStateUpdateNode(ScenarioStateUpdateNode node)
    {
      _state = State.ExecutingStateUpdate;

      var key = $"{node.TargetEntityIdentifier}.{node.StateKey}";
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

      var playCoroutine = _ttsService.PlayTranscript(node.TranscriptIdentifier, _ttsAudioSource, variables);

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
          _ttsService.PrepareTranscriptVariables(playTTS.TranscriptIdentifier, vars);
        }
      }
    }

    /// <summary>
    /// 시나리오 그래프의 인라인 텍스트(Dialogue/Choice/Quiz 콘텐츠)를 TTS로 재생한다.
    /// baked WAV가 있으면 우선 재생하고, 없으면 즉석 합성한다.
    /// TTSService/AudioSource 참조가 없거나 텍스트가 비어 있으면 아무 작업도 하지 않는다.
    /// 텍스트 표시와 병렬로 재생되며(대기하지 않음), 다음 노드 진행을 막지 않는다.
    /// </summary>
    private void PlayInlineTTS(string nodeIdentifier, string text)
    {
      if (_ttsService == null || _ttsAudioSource == null) return;
      if (string.IsNullOrWhiteSpace(text)) return;

      // IsReady를 기다리지 않는다: baked WAV는 ONNX 초기화 없이 즉시 재생 가능하고,
      // baked가 없을 때만 내부에서 즉석 합성(초기화 완료 후 가능)으로 폴백한다.
      string scenarioIdentifier = _currentGraph != null ? _currentGraph.Identifier : null;
      _ttsService.PlayText(text, _ttsAudioSource, scenarioIdentifier, nodeIdentifier);
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
      var feedback = isCorrect ? node.FeedbackCorrect : node.FeedbackIncorrect;
      if (!string.IsNullOrWhiteSpace(feedback) && !_uiController.IsUnityNull())
      {
        _uiController.DisplayDialogue("Quiz", feedback, null);

        if (node.PlayTTS)
        {
          string feedbackNodeId = node.Identifier + (isCorrect ? "_feedbackCorrect" : "_feedbackIncorrect");
          PlayInlineTTS(feedbackNodeId, feedback);
        }
      }

      var target = isCorrect
          ? node.OnCorrectNextIdentifier
          : string.IsNullOrWhiteSpace(node.OnIncorrectNextIdentifier) ? node.NextIdentifier : node.OnIncorrectNextIdentifier;

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

    private IEnumerator ExecutePlayerMoveNode(ScenarioPlayerMoveNode node)
    {
      _state = State.ExecutingPlayerMove;

      Vector3 destination;
      if (node.DestinationType == ScenarioMoveDestinationType.Position)
      {
        destination = new Vector3(node.DestinationX, node.DestinationY, node.DestinationZ);
      }
      else
      {
        if (Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, node.DestinationIdentifier, out var waypointPos)
            || Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, node.DestinationIdentifier, out waypointPos))
        {
          destination = waypointPos;
        }
        else
        {
          Debug.LogWarning($"[ScenarioController] Waypoint '{node.DestinationIdentifier}' not found. Fallback to no move.");
          // Fallback: MoveDuration이 있으면 기다리고, 없으면 바로 스킵
          if (node.MoveMode == ScenarioMoveMode.ByDuration && node.MoveDuration > 0)
          {
            yield return new WaitForSeconds(node.MoveDuration);
          }
          Advance();
          yield break;
        }
      }

      // TODO: 플레이어 이동 로직 구현
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Moving player to: {destination}");
#endif

      // 임시 대기
      yield return new WaitForSeconds(1f);

      Advance();
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

      Vector3 destination;
      if (node.DestinationType == ScenarioMoveDestinationType.Position)
      {
        destination = new Vector3(node.DestinationX, node.DestinationY, node.DestinationZ);
      }
      else
      {
        if (Registry.Registry.TryGet<Vector3>(RegistryType.Waypoint, node.DestinationIdentifier, out var waypointPos)
            || Registry.Registry.TryGet<Vector3>(RegistryType.InteractableEntity, node.DestinationIdentifier, out waypointPos))
        {
          destination = waypointPos;
        }
        else
        {
          Debug.LogWarning($"[ScenarioController] Waypoint '{node.DestinationIdentifier}' not found. Fallback to no move.");
          // Fallback: MoveDuration이 있으면 기다리고, 없으면 바로 스킵
          if (node.MoveMode == ScenarioMoveMode.ByDuration && node.MoveDuration > 0)
          {
            yield return new WaitForSeconds(node.MoveDuration);
          }
          Advance();
          yield break;
        }
      }

      // TODO: NPC 이동 로직 구현
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Moving NPC '{node.NPCIdentifier}' to: {destination}");
#endif

      // 임시 대기
      yield return new WaitForSeconds(1f);

      Advance();
    }

    private IEnumerator ExecuteCameraTargetNode(ScenarioCameraTargetNode node)
    {
      _state = State.ExecutingCameraTarget;

      // TODO: 카메라 타겟팅 로직 구현
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Targeting camera to: {node.TargetObjectIdentifier}");
#endif

      if (_camController.IsUnityNull())
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

        var coroutine = StartCoroutine(ExecuteBranch(branchNode, branch.CompletionConditionIdentifier, assignedClientId));
        runningCoroutines.Add(coroutine);
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
          yield return WaitForAny(runningCoroutines);
          break;
        case ScenarioWaitMode.None:
          // 바로 진행
          break;
      }

      EndScenario();
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

    private IEnumerator ExecuteBranch(IScenarioNode node, string completionCondition, int? branchOwnerClientId)
    {
      var previousOwner = _scenarioOwnerClientId;
      _scenarioOwnerClientId = branchOwnerClientId ?? previousOwner;

      try
      {
        // 브랜치의 시작 노드부터 NextIdentifier 체인을 끝까지(또는 완료조건 라벨까지) 실행한다.
        // 완료조건 식별자(completionCondition)는 보통 그래프에 실제 노드가 없는 "수렴 라벨"이며,
        // 브랜치 체인 마지막 노드의 NextIdentifier 가 이 라벨을 가리킨다.
        // 라벨에 도달하면 브랜치 완료로 간주한다(전역 Advance/EndScenario 를 건드리지 않음).
        // 브랜치 내부의 게이팅(인터랙션 완료 대기)은 체인에 포함된
        // Validator(waitForCondition=true) 노드가 담당하므로, 라벨 도달 = 브랜치 완료가 된다.
        yield return RunBranchChain(node, completionCondition);
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
    private IEnumerator RunBranchChain(IScenarioNode startNode, string completionLabel)
    {
      var cursor = startNode;
      int guard = 0;
      const int maxNodes = 10000; // 순환 방지 안전장치.

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

        OnNodeChanged?.Invoke(cursor);

        // 단일 노드를 실행하고 완료를 대기한다(전역 Advance 미사용).
        yield return ExecuteBranchNode(cursor);

        // 노드 대기 도중 시나리오가 종료되어 그래프가 해제됐을 수 있으므로 재확인한다.
        if (_currentGraph == null)
        {
          yield break;
        }

        var nextId = cursor.NextIdentifier;

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

        if (!_currentGraph.TryGetNode(nextId, out var nextNode))
        {
          // 다음 식별자가 그래프에 없으면(예: 미정의 수렴 라벨) 브랜치 완료로 간주한다.
          yield break;
        }

        cursor = nextNode;
      }
    }

    /// <summary>
    /// 브랜치 내부에서 단일 노드를 실행하고 그 노드가 완료될 때까지 대기한다.
    /// 각 노드 실행기는 내부적으로 전역 <see cref="Advance"/> 를 호출하지만, 브랜치 체인에서는
    /// 그 진행을 사용하지 않고 NextIdentifier 로 직접 이동하므로 부작용이 격리된다.
    /// 코루틴형 노드(예: Delay/InvokeEvent(WaitUntilDone)/Sound)는 완료까지 yield 로 대기한다.
    /// </summary>
    private IEnumerator ExecuteBranchNode(IScenarioNode node)
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
        }
      }
      finally
      {
        _globalAdvanceSuppressionDepth--;
      }
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

    private IEnumerator WaitForAny(List<Coroutine> coroutines)
    {
      while (coroutines.Count > 0)
      {
        for (int i = coroutines.Count - 1; i >= 0; i--)
        {
          if (coroutines[i] == null) // 완료된 코루틴
          {
            coroutines.RemoveAt(i);
            yield break;
          }
        }
        yield return null;
      }
    }

    private List<int> GetActivePlayerIds()
    {
      var ids = new List<int>();

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
      else if (InstanceFinder.ClientManager != null)
      {
        var localConn = InstanceFinder.ClientManager.Connection;
        if (localConn != null)
        {
          ids.Add((int)localConn.ClientId);
        }
      }

      return ids;
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

          var pick = eligiblePlayers[UnityEngine.Random.Range(0, eligiblePlayers.Count)];
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
          Shuffle(playerPool);
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

    private static void Shuffle(IList<int> list)
    {
      for (int i = list.Count - 1; i > 0; i--)
      {
        int j = UnityEngine.Random.Range(0, i + 1);
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
      var clientCount = InstanceFinder.ClientManager?.Clients?.Count ?? 0;

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
