using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using MultiplayerInfrastructure.Camera;
using FishNet.Object;
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

    #endregion

    #region Private Fields

    private ScenarioGraph _currentGraph;
    private IScenarioNode _currentNode;
    private List<ScenarioChoiceOption> _activeOptions = new();

    #endregion

    #region State

    public enum State
    {
      Inactive,
      ExecutingDialogue,
      ExecutingChoice,
      ExecutingSound,
      ExecutingPlayerMove,
      ExecutingCameraTarget,
      ExecutingParallel
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
      if (graph == null)
      {
        Debug.LogError("[ScenarioController] Cannot start scenario with null graph");
        return;
      }

      _currentGraph = graph;

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

      // UI 종료
      if (_uiController.IsUnityNull())
      {
        _uiController.EndScenario();
      }

      OnScenarioEnded?.Invoke();
      Debug.Log("[ScenarioController] Scenario ended");
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

    private void HandleScenarioRequested(ScenarioGraph graph, string startNodeIdentifier)
    {
      if (IsActive)
      {
        Debug.LogWarning("[ScenarioController] Scenario already active, ignoring request");
        return;
      }

      StartScenario(graph, startNodeIdentifier);
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
        case ScenarioCameraTargetNode camera:
          StartCoroutine(ExecuteCameraTargetNode(camera));
          break;
        case ScenarioParallelNode parallel:
          StartCoroutine(ExecuteParallelNode(parallel));
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

      if (_uiController != null)
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

      if (_uiController != null)
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

      Debug.Log($"[ScenarioController] Playing sound: {node.SoundResourceIdentifier}");

      if (node.WaitUntilFinished)
      {
        // TODO: 실제 클립 길이만큼 대기
        yield return new WaitForSeconds(1f); // 임시
      }

      Advance();
    }

    private IEnumerator ExecutePlayerMoveNode(ScenarioPlayerMoveNode node)
    {
      _state = State.ExecutingPlayerMove;

      // TODO: 플레이어 이동 로직 구현
      Debug.Log($"[ScenarioController] Moving player to: {node.DestinationX}, {node.DestinationY}, {node.DestinationZ}");

      // 임시 대기
      yield return new WaitForSeconds(1f);

      Advance();
    }

    private IEnumerator ExecuteCameraTargetNode(ScenarioCameraTargetNode node)
    {
      _state = State.ExecutingCameraTarget;

      // TODO: 카메라 타겟팅 로직 구현
      Debug.Log($"[ScenarioController] Targeting camera to: {node.TargetObjectIdentifier}");

      if (_camController.IsUnityNull())
      {
        // TODO: 카메라 타겟 설정
        // _camController.SetTargetObject(node.TargetObjectIdentifier, node.OffsetX, node.OffsetY, node.OffsetZ, node.BlendTime);
      }

      yield return new WaitForSeconds(node.BlendTime);

      Advance();
    }

    private IEnumerator ExecuteParallelNode(ScenarioParallelNode node)
    {
      _state = State.ExecutingParallel;

      var runningCoroutines = new List<Coroutine>();

      foreach (var branch in node.Branches)
      {
        if (_currentGraph.TryGetNode(branch.Identifier, out var branchNode))
        {
          var coroutine = StartCoroutine(ExecuteBranch(branchNode, branch.CompletionConditionIdentifier));
          runningCoroutines.Add(coroutine);
        }
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

    private IEnumerator ExecuteBranch(IScenarioNode node, string completionCondition)
    {
      // 브랜치 실행 (간단히 재귀 호출)
      ExecuteNode(node);

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

    #endregion

    #region Helper Methods

    private void ClearOptions()
    {
      _activeOptions.Clear();

      if (_hintUIController != null)
      {
        _hintUIController.ClearDialogueSelections();
      }
    }

    #endregion
  }
}