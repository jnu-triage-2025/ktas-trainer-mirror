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
using TextToSpeechService;
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
    [SerializeField] private TTSService _ttsService;
    [SerializeField] private AudioSource _ttsAudioSource;

    #endregion

    #region Private Fields

    private ScenarioGraph _currentGraph;
    private IScenarioNode _currentNode;
    private List<ScenarioChoiceOption> _activeOptions = new();
    private ScenarioQuizNode _activeQuizNode;
    private ScenarioRoleAssignmentNode _activeRoleAssignmentNode;
    private List<string> _activeRoleOptions = new List<string>();
    private readonly Dictionary<int, string> _assignedRolesByClientId = new Dictionary<int, string>();
    private readonly Dictionary<string, string> _stateStore = new Dictionary<string, string>();
    private int? _scenarioOwnerClientId;

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
      ExecutingNotification,
      ExecutingDelay,
      ExecutingInteraction,
      ExecutingCombineItem,
      ExecutingQuiz,
      ExecutingStateUpdate,
      ExecutingRoleAssignment,
      ExecutingTTS,
    }

    [SerializeField] private State _state = State.Inactive;

    #endregion

    #region Events

    public event Action OnScenarioStarted;
    public event Action OnScenarioEnded;
    public event Action<IScenarioNode> OnNodeChanged;
    public event Action<ScenarioChoiceOption> OnOptionSelected;

    #endregion

    #region Properties

    public bool IsActive => _state != State.Inactive;
    public State CurrentState => _state;
    public IScenarioNode CurrentNode => _currentNode;
    public ScenarioGraph CurrentGraph => _currentGraph;

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

      _currentGraph = graph;
      _scenarioOwnerClientId = ownerClientId;

      ResolveUIControllers();

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
      _currentGraph = null;
      _currentNode = null;
      _state = State.Inactive;

      ClearOptions();
      _assignedRolesByClientId.Clear();
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

      if (_state == State.ExecutingRoleAssignment && _activeRoleAssignmentNode != null)
      {
        HandleRoleSelection(index);
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
        case ScenarioNotificationNode notification:
          StartCoroutine(ExecuteNotificationNode(notification));
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
        case ScenarioRoleAssignmentNode roleAssignment:
          ExecuteRoleAssignmentNode(roleAssignment);
          break;
        case ScenarioPlayTTSNode playTTS:
          StartCoroutine(ExecutePlayTTSNode(playTTS));
          break;
        default:
          Debug.LogWarning($"[ScenarioController] Unsupported node type: {node.GetType().Name}");
          Advance();
          break;
      }
    }

    private void ExecuteDialogueNode(ScenarioDialogueNode node)
    {
      _state = State.ExecutingDialogue;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayDialogue(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier);
      }
      else
      {
        // UI 없으면 바로 진행
        Advance();
      }
    }

    private void ExecuteChoiceNode(ScenarioChoiceNode node)
    {
      _state = State.ExecutingChoice;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayChoice(node.SpeakerName, node.DialogueContent, node.PortraitSpriteIdentifier, node.Options);
      }

      _activeOptions = new List<ScenarioChoiceOption>(node.Options);
    }

    private IEnumerator ExecuteSoundNode(ScenarioSoundNode node)
    {
      _state = State.ExecutingSound;

      // TODO: 사운드 재생 로직 구현
      // 예: AudioSource.PlayClipAtPoint(Resources.Load<AudioClip>(node.SoundResourceIdentifier), transform.position);
#if UNITY_EDITOR
      Debug.Log($"[ScenarioController] Playing sound: {node.SoundResourceIdentifier}");
#endif

      if (node.WaitUntilFinished)
      {
        // TODO: 실제 클립 길이만큼 대기
        yield return new WaitForSeconds(1f); // 임시
      }

      Advance();
    }

    private void ExecuteQuestControlNode(ScenarioQuestControlNode node)
    {
      _state = State.ExecutingQuestControl;

      var manager = Registry.Registry.Get<QuestManager>(RegistryType.Entity, Registry.Registry.TypeKey<QuestManager>());
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

    private IEnumerator ExecuteNotificationNode(ScenarioNotificationNode node)
    {
      _state = State.ExecutingNotification;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayDialogue("System", node.Message ?? string.Empty, null);
      }
      else
      {
        Debug.Log($"[ScenarioController] Notification({node.DisplayMode}): {node.Message}");
      }

      if (node.Duration.HasValue && node.Duration.Value > 0f)
      {
        yield return new WaitForSeconds(node.Duration.Value);
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

    private void ExecuteRoleAssignmentNode(ScenarioRoleAssignmentNode node)
    {
      _state = State.ExecutingRoleAssignment;
      _activeRoleAssignmentNode = node;

      if (node.AssignmentMode == ScenarioRoleAssignmentMode.Auto)
      {
        AutoAssignRoles(node);
        Advance();
        return;
      }

      var roleOptions = node.RoleOptions?.Where(role => !string.IsNullOrWhiteSpace(role)).ToList() ?? new List<string>();
      if (roleOptions.Count == 0)
      {
        Debug.LogWarning($"[ScenarioController] RoleAssignment node '{node.Identifier}' has no role options.");
        Advance();
        return;
      }

      var options = roleOptions.Select(role => new ScenarioChoiceOption
      {
        DisplayText = role,
        DisplayColor = Color.white,
        NextNodeIdentifier = null
      }).ToList();

      _activeRoleOptions = roleOptions;
      _activeOptions = options;

      if (!_uiController.IsUnityNull())
      {
        _uiController.DisplayChoice("System", "역할을 선택하세요.", null, options);
      }
      else
      {
        AssignRoleToOwnerOrFirst(roleOptions[0]);
        Advance();
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

    private void HandleRoleSelection(int index)
    {
      if (index < 0 || index >= _activeRoleOptions.Count)
      {
        Debug.LogWarning($"[ScenarioController] Invalid role option index: {index}");
        return;
      }

      AssignRoleToOwnerOrFirst(_activeRoleOptions[index]);
      _activeRoleAssignmentNode = null;
      ClearOptions();
      Advance();
    }

    private void AutoAssignRoles(ScenarioRoleAssignmentNode node)
    {
      var roles = node.RoleOptions?.Where(role => !string.IsNullOrWhiteSpace(role)).ToList() ?? new List<string>();
      if (roles.Count == 0)
      {
        return;
      }

      var players = GetActivePlayerIds();
      if (players.Count == 0)
      {
        return;
      }

      for (int i = 0; i < players.Count; i++)
      {
        _assignedRolesByClientId[players[i]] = roles[i % roles.Count];
      }
    }

    private void AssignRoleToOwnerOrFirst(string role)
    {
      var players = GetActivePlayerIds();
      if (_scenarioOwnerClientId.HasValue)
      {
        _assignedRolesByClientId[_scenarioOwnerClientId.Value] = role;
        return;
      }

      if (players.Count > 0)
      {
        _assignedRolesByClientId[players[0]] = role;
      }
    }

    private bool ApplyQuestOperation(QuestManager manager, ScenarioQuestControlNode node)
    {
      string questId = node.Quest?.Id;

      switch (node.Operation)
      {
        case ScenarioQuestOperationType.Add:
          if (manager.HasQuest(questId))
          {
            return HandleConflict(node, () => manager.AddOrUpdateQuest(node.Quest));
          }

          if (node.Quest == null)
          {
            Debug.LogWarning("[ScenarioController] Add quest operation missing quest data.");
            return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;
          }

          manager.AddOrUpdateQuest(node.Quest);
          return true;

        case ScenarioQuestOperationType.Update:
          if (manager.HasQuest(questId))
          {
            if (node.Quest == null)
            {
              Debug.LogWarning("[ScenarioController] Update quest operation missing quest data.");
              return node.FailureStrategy != ScenarioQuestFailureStrategy.Panic;
            }

            manager.AddOrUpdateQuest(node.Quest);
            return true;
          }

          if (node.FailureStrategy == ScenarioQuestFailureStrategy.Overwrite)
          {
            manager.AddOrUpdateQuest(node.Quest);
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

      bool passed = EvaluateValidator(node);

      if (passed)
      {
        Advance();
        yield break;
      }

      switch (node.OnFailure)
      {
        case ScenarioValidatorOnFailure.Panic:
          Debug.LogWarning($"[ScenarioController] Validator failed at node '{node.Identifier}'.");
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

    private IEnumerator ExecuteParallelNode(ScenarioParallelNode node)
    {
      _state = State.ExecutingParallel;

      var runningCoroutines = new List<Coroutine>();
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

      Advance();
    }

    private IEnumerator ExecuteBranch(IScenarioNode node, string completionCondition, int? branchOwnerClientId)
    {
      var previousOwner = _scenarioOwnerClientId;
      _scenarioOwnerClientId = branchOwnerClientId ?? previousOwner;

      // 브랜치 실행 (간단히 재귀 호출)
      if (node is ScenarioInvokeEventNode invoke)
      {
        if (invoke.MoveNextBehavior == ScenarioInvokeEventMoveNextBehavior.Immediately)
        {
          StartCoroutine(ExecuteInvokeEventNode(invoke));
        }
        else if (invoke.MoveNextBehavior == ScenarioInvokeEventMoveNextBehavior.WaitUntilDone)
        {
          yield return ExecuteInvokeEventNode(invoke);
        }
        else
        {
          ExecuteNode(node);
        }
      }
      else
      {
        ExecuteNode(node);
      }

      _scenarioOwnerClientId = previousOwner;

      // TODO: completionCondition 체크 로직
      yield return null; // 임시
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
          if (target != null && !IsPlayerEligibleForBranch(branches[0], target.Value))
          {
            target = null;
          }

          if (target == null && playerPool.Count > 0)
          {
            target = playerPool
                .Where(clientId => IsPlayerEligibleForBranch(branches[0], clientId))
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
      if (branch?.RequiredRoleIdentifiers == null || branch.RequiredRoleIdentifiers.Count == 0)
      {
        return true;
      }

      if (!_assignedRolesByClientId.TryGetValue(clientId, out var role) || string.IsNullOrWhiteSpace(role))
      {
        return false;
      }

      return branch.RequiredRoleIdentifiers.Any(requiredRole =>
          string.Equals(requiredRole, role, StringComparison.OrdinalIgnoreCase));
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
      var clientCount = InstanceFinder.ClientManager?.Clients?.Count ?? 0;
      var value = clientCount;

      switch (node.Condition)
      {
        case ScenarioValidatorCondition.PlayerCountEqual:
          return value == node.TargetCount;
        case ScenarioValidatorCondition.PlayerCountNotEqual:
          return value != node.TargetCount;
        case ScenarioValidatorCondition.PlayerCountLessThan:
          return value < node.TargetCount;
        case ScenarioValidatorCondition.PlayerCountLessThanOrEqual:
          return value <= node.TargetCount;
        case ScenarioValidatorCondition.PlayerCountGreaterThan:
          return value > node.TargetCount;
        case ScenarioValidatorCondition.PlayerCountGreaterThanOrEqual:
          return value >= node.TargetCount;
        default:
          return false;
      }
    }

    #endregion

    #region Helper Methods

    private void ClearOptions()
    {
      _activeOptions.Clear();
      _activeQuizNode = null;
      _activeRoleAssignmentNode = null;
      _activeRoleOptions.Clear();

      if (_hintUIController != null)
      {
        _hintUIController.ClearDialogueSelections();
      }
    }

    #endregion
  }
}