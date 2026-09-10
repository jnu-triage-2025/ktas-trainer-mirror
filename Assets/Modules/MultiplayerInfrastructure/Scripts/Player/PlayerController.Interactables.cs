using Input = MultiplayerInfrastructure.Automation.PlayerInput;
using System.Collections.Generic;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Quest;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.Scenario;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// Interactables과의 상호작용을 정의합니다.
  /// </summary>
  public partial class PlayerController : IConditionStateProvider
  {
    [SerializeField] private NearbyInteractablesDetector _detector;
    [SerializeField] private InteractableObjectHintUIController _interactableHintUI;

    [Header("Dialogue")]
    [Tooltip("씬에 배치된 DialoguePanelUIController 참조 (Inspector에서 할당하거나 태그/이름으로 검색)")]
    [SerializeField] private DialoguePanelUIController _dialoguePanelUIController;
    private ILocalInteractionFocus _focusedInteraction;

    /// <summary>상호작용 힌트/감지기 바인딩이 이미 끝났는지 여부. 재시도 코루틴의 중복 실행을 막는다.</summary>
    private bool _interactablesBound;

    private void OnStartClient_Interactables()
    {
      if (!IsOwner)
        return;

      if (TryBindInteractables())
        return;

      // 씬을 추가 로드한 뒤 플레이어를 생성하는 구성에서는 OnStartClient 시점에 카메라 컨트롤러가
      // 아직 레지스트리에 없을 수 있다. 여기서 그냥 포기하면 _interactableHintUI 가 영영 비어,
      // 선택지 대화창이 확정 수단을 갖지 못한 채 입력만 잠그는 상태가 된다.
      // 따라서 카메라가 등록될 때까지 이후 프레임에서 바인딩을 다시 시도한다.
      StartCoroutine(BindInteractablesWhenCameraReady());
    }

    /// <summary>
    /// 카메라 컨트롤러가 준비될 때까지 상호작용 바인딩을 재시도한다.
    /// 성공하거나 소유권을 잃으면 종료한다.
    /// </summary>
    private System.Collections.IEnumerator BindInteractablesWhenCameraReady()
    {
      const float warnAfterSeconds = 5f;
      float startedAt = Time.unscaledTime;
      bool warned = false;

      while (!_interactablesBound && IsOwner)
      {
        yield return null;
        if (TryBindInteractables())
          yield break;

        if (!warned && Time.unscaledTime - startedAt >= warnAfterSeconds)
        {
          warned = true;
          Debug.LogWarning(
            "[PlayerController] Camera controller is still not registered; interactables binding keeps retrying.",
            this);
        }
      }
    }

    /// <summary>
    /// 카메라 컨트롤러에 붙어 있는 감지기와 힌트 UI를 바인딩한다.
    /// 카메라 컨트롤러를 아직 찾지 못했다면 false 를 돌려준다(호출부가 재시도한다).
    /// </summary>
    private bool TryBindInteractables()
    {
      if (_interactablesBound)
        return true;

      if (_camControl == null)
      {
        _camControl = MainCameraController.Instance
          ?? Registry.Registry.Get<MainCameraController>(
            RegistryType.Service,
            Registry.Registry.TypeKey<MainCameraController>());
      }

      if (_camControl == null)
        return false;

      _interactablesBound = true;

      // 카메라 컴포넌트
      _detector = _camControl.GetComponent<NearbyInteractablesDetector>();
      _interactableHintUI = _camControl.GetComponent<InteractableObjectHintUIController>();

      if (_detector != null)
      {
        _detector.RegisterDetectBased(transform);
        _detector.NearbyUpdated += HandleNearbyUpdated;
        HandleNearbyUpdated(_detector.Nearby);
      }

      // DialoguePanelUIController 찾기 (Inspector에서 할당되지 않은 경우)
      if (_dialoguePanelUIController.IsUnityNull())
      {
        _dialoguePanelUIController = FindDialoguePanelUIController();
      }

      // DialoguePanelUIController에 InteractableHintUI 연결
      if (_dialoguePanelUIController != null && _interactableHintUI != null)
      {
        _dialoguePanelUIController.SetInteractableHintUI(_interactableHintUI);
      }

      if (_interactableHintUI != null)
        _interactableHintUI.InteractionClicked += HandleInteractionMenuClicked;

      // 시나리오가 이미 실행 중일 때 늦게 바인딩됐다면, 시나리오 쪽 참조도 여기서 갱신한다.
      // 다음 StartScenario 까지 기다리면 그 사이의 선택지 노드가 힌트 UI 없이 표시된다.
      if (!_scenarioController.IsUnityNull())
        _scenarioController.RegisterReferences(_dialoguePanelUIController, _camControl, _interactableHintUI);

      // 순차 퀘스트의 완료 조건이 바뀌면 같은 감지 범위 안에서도 다음 상호작용을 즉시 다시 고른다.
      // 표시 UI만 다시 그리면 CanInteract 결과는 이전 목록에 고정되어 범위를 나갔다 들어와야 갱신된다.
      QuestPresentationService.PresentationChanged += RefreshInteractableHintsNow;
      // 레지스트리의 정의·오버라이드 변경과 신호 변경은 조건 판정 입력이므로 힌트를 즉시 다시 계산한다.
      InteractionRegistry.HintRefreshRequested += RefreshInteractableHintsNow;
      ScenarioInteractionSignals.OnSignalRegistered += HandleSignalChangedForHints;
      ScenarioInteractionSignals.OnSignalCleared += HandleSignalChangedForHints;
      return true;
    }

    private void HandleSignalChangedForHints(string signal) => RefreshInteractableHintsNow();

    private void OnDestroy()
    {
      OnStopClient_UIOverlaySync();

      StopSpectateFollow();

      if (_detector != null)
        _detector.NearbyUpdated -= HandleNearbyUpdated;

      if (_interactableHintUI != null)
        _interactableHintUI.InteractionClicked -= HandleInteractionMenuClicked;

      QuestPresentationService.PresentationChanged -= RefreshInteractableHintsNow;
      InteractionRegistry.HintRefreshRequested -= RefreshInteractableHintsNow;
      ScenarioInteractionSignals.OnSignalRegistered -= HandleSignalChangedForHints;
      ScenarioInteractionSignals.OnSignalCleared -= HandleSignalChangedForHints;

      OnDestroy_Item();
      OnDestroy_PlaceableItemPreview();
      OnDestroy_OverheadName();
    }

    private void HandleNearbyUpdated(IReadOnlyList<IInteractable> nearby)
    {
#if UNITY_EDITOR && (DEBUG == true) && false
      Debug.Log($"[PlayerController] Nearby interactables updated: {nearby.Count} items found.");
#endif
      if (_interactableHintUI == null)
        return;

      var interacts = CollectAvailableInteracts(nearby);
      KeepNearestExclusiveInteracts(interacts, _detector != null ? _detector.DetectionPosition : transform.position);
      OrderInteractsByDisplayPriority(interacts);

      // UpdateInteractables를 사용하여 모드에 따라 적절히 처리
      _interactableHintUI.UpdateInteractables(interacts);
      RefreshLocalInteractionFocus();
    }

    /// <summary>
    /// 큰 Interactable Detector와 개별 콜라이더가 구역 경계에서 겹치면 같은 이름의 설치 항목이
    /// 동시에 감지될 수 있다. INearestOnlyInteract가 같은 그룹으로 지정한 활성 후보끼리만 비교하여
    /// 플레이어와 가장 가까운 하나를 남긴다. 이미 조건 검사에서 제외된 항목이나 다른 종류의 상호작용은
    /// 이 목록에 관여하지 않으므로 기존 동작을 유지한다.
    /// </summary>
    internal static void KeepNearestExclusiveInteracts(List<IInteract> interacts, Vector3 distanceReference)
    {
      if (interacts == null || interacts.Count < 2)
        return;

      var nearestByGroup = new Dictionary<string, (IInteract interact, float distance, int tieBreaker)>();
      for (int i = 0; i < interacts.Count; i++)
      {
        if (!(interacts[i] is INearestOnlyInteract candidate))
          continue;

        string group = candidate.NearestOnlyGroup;
        if (string.IsNullOrWhiteSpace(group))
          continue;

        float distance = NearestOnlyInteractUtility.DistanceTo(candidate, distanceReference);
        int tieBreaker = candidate.NearestOnlyTieBreaker;
        if (!nearestByGroup.TryGetValue(group, out var nearest)
            || NearestOnlyInteractUtility.IsPreferred(distance, tieBreaker, nearest.distance, nearest.tieBreaker))
          nearestByGroup[group] = (interacts[i], distance, tieBreaker);
      }

      for (int i = interacts.Count - 1; i >= 0; i--)
      {
        if (!(interacts[i] is INearestOnlyInteract candidate))
          continue;

        string group = candidate.NearestOnlyGroup;
        if (!string.IsNullOrWhiteSpace(group)
            && nearestByGroup.TryGetValue(group, out var nearest)
            && !ReferenceEquals(interacts[i], nearest.interact))
          interacts.RemoveAt(i);
      }
    }

    /// <summary>
    /// 우선순위를 명시한 항목만 앞으로 이동한다. 같은 우선순위의 기존 감지 순서를 보존하여
    /// 주기적인 Physics 조회 결과가 선택 항목을 불필요하게 흔들지 않게 한다.
    /// </summary>
    public static void OrderInteractsByDisplayPriority(List<IInteract> interacts)
    {
      if (interacts == null || interacts.Count < 2)
        return;

      for (int i = 1; i < interacts.Count; i++)
      {
        var candidate = interacts[i];
        int candidatePriority = GetDisplayPriority(candidate);
        int insertAt = i;
        while (insertAt > 0 && GetDisplayPriority(interacts[insertAt - 1]) < candidatePriority)
        {
          interacts[insertAt] = interacts[insertAt - 1];
          insertAt--;
        }

        interacts[insertAt] = candidate;
      }
    }

    private static int GetDisplayPriority(IInteract interact)
    {
      if (InteractionRegistry.TryGetDataDisplay(interact, out var display) && display.PrioritySpecified)
        return display.Priority;
      return interact is IInteractDisplayPriority prioritized ? prioritized.DisplayPriority : 0;
    }

    public void RefreshInteractableHintsNow()
    {
      // EditMode/오프라인 유틸리티 오브젝트는 스폰된 NetworkObject 에 붙지 않을 수 있다.
      // 소유권은 NetworkObject 가 생긴 뒤에만 의미가 있으므로, 그 전에 IsOwner 를
      // 역참조하면 FishNet 내부에서 예외가 발생해 로컬 인벤토리 상호작용까지 망가뜨릴 수 있다.
      if (NetworkObject != null && !IsOwner)
        return;

      if (_detector == null)
        return;

      HandleNearbyUpdated(_detector.Nearby);
    }

    // PlayerController.Input 에서 호출
    private void TryInteractWithSelection()
    {
      // 대화창이 최상단인 동안에는 월드 상호작용을 절대 실행하지 않는다.
      // 힌트 UI가 (모드 전환 경쟁 등으로) 아직 선택지를 들고 있지 않은 프레임에 입력이 들어오면
      // 목록에 남아 있던 주변 Interactable이 실행되어, "선택지 대신 옆 오브젝트와 상호작용"하는
      // 문제가 된다. 대화창이 열려 있으면 처리 주체는 언제나 DialoguePanelUIController다.
      if (!_dialoguePanelUIController.IsUnityNull() && UIOverlayStack.IsTop(_dialoguePanelUIController))
      {
        _dialoguePanelUIController.TrySelectCurrentOption();
        return;
      }

      // 다이얼로그 모드에서는 DialoguePanelUIController를 통해 선택 처리
      if (_interactableHintUI != null && _interactableHintUI.IsDialogueMode)
      {
        if (_dialoguePanelUIController != null)
        {
          _dialoguePanelUIController.TrySelectCurrentOption();
        }
        return;
      }

      // 일반 모드에서는 기존 로직
      var interact = _interactableHintUI?.GetSelected();
      if (interact == null)
      {
        return;
      }

      // UI가 갱신되는 두 query 사이에 경계를 넘으면 이전 최단 후보가 잠시 선택 상태로 남을 수 있다.
      // 실행 직전에 현재 감지 목록과 조건을 다시 평가하여 더 먼 static entity가 활성화되지 않게 한다.
      if (!IsCurrentlyAvailableInteract(interact))
      {
        RefreshInteractableHintsNow();
        return;
      }

      interact.Interact(transform);
      // 범용 핸들러는 실제 완료 시점에 직접 통지한다(제출 UI 열기는 완료가 아니다).
      InteractionRegistry.NotifyCustomInteracted(interact, this);
    }

    private bool IsCurrentlyAvailableInteract(IInteract selected)
    {
      if (_detector == null || selected == null)
        return false;

      // 마지막 query의 Nearby 캐시가 아니라 현재 Physics 범위를 즉시 다시 조회한다.
      // queryInterval 사이에 범위를 벗어나거나 새 후보가 들어온 경우에도 오래된 대상을 실행하지 않는다.
      var currentlyNearby = new List<IInteractable>();
      _detector.QueryCurrentInteractables(currentlyNearby);
      var current = CollectAvailableInteracts(currentlyNearby);
      KeepNearestExclusiveInteracts(current, _detector.DetectionPosition);
      OrderInteractsByDisplayPriority(current);
      for (int i = 0; i < current.Count; i++)
      {
        if (ReferenceEquals(current[i], selected))
          return true;
      }

      return false;
    }

    private List<IInteract> CollectAvailableInteracts(IReadOnlyList<IInteractable> nearby)
    {
      var interacts = new List<IInteract>();
      if (nearby == null)
        return interacts;

      for (int i = 0; i < nearby.Count; i++)
      {
        var eachInteracts = nearby[i]?.Interacts;
        if (eachInteracts == null)
          continue;

        for (int j = 0; j < eachInteracts.Length; j++)
        {
          var interact = eachInteracts[j];
          if (interact == null)
            continue;
          // 레지스트리 항목은 가시성(오버라이드 → 조건 → 초기값)을 먼저 판정하고, 그다음 내재 능력 조건을 본다.
          if (InteractionRegistry.TryGetByHandler(interact, out var registryEntry))
          {
            if (!InteractionRegistry.IsVisible(registryEntry, this, out _))
              continue;
          }
          else
          {
            InteractionRegistry.WarnUnregisteredHandler(interact);
          }
          if (interact is IInteractorConditional conditional && !conditional.CanInteract(transform))
            continue;
          interacts.Add(interact);
        }
      }

      return interacts;
    }

    private void HandleInteractionMenuClicked(int index)
    {
      if (!IsOwner || _interactableHintUI == null)
        return;

      TryInteractWithSelection();
    }

    // PlayerController.Input 에서 호출
    private void HandleInteractablesSelectionInput()
    {
      if (_interactableHintUI == null)
        return;

      float scroll = Input.GetAxis("Mouse ScrollWheel");
      if (scroll > 0.01f)
        _interactableHintUI.MoveSelected(-1);
      else if (scroll < -0.01f)
        _interactableHintUI.MoveSelected(1);

      if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        _interactableHintUI.MoveSelected(-1);
      if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        _interactableHintUI.MoveSelected(1);

      RefreshLocalInteractionFocus();
    }

    // ── IConditionStateProvider ───────────────────────────────────────────

    private static readonly string[] PlayerConditionKeys =
    {
      "carrying",
      "patient_selection_mode",
      "user_identifier",
      "is_owner"
    };

    public IEnumerable<string> ConditionKeys => PlayerConditionKeys;

    public bool TryGetConditionValue(string key, string qualifier, out ConditionValue value)
    {
      switch (key)
      {
        case "carrying":
          value = ConditionValue.From(IsCarryingReposable);
          return true;
        case "patient_selection_mode":
          value = ConditionValue.From(IsPatientSelectionMode);
          return true;
        case "user_identifier":
          value = ConditionValue.From(UserIdentifier ?? string.Empty);
          return true;
        case "is_owner":
          value = ConditionValue.From(NetworkObject != null && IsOwner);
          return true;
        default:
          value = default;
          return false;
      }
    }

    private void RefreshLocalInteractionFocus()
    {
      var next = _interactableHintUI?.GetSelected() as ILocalInteractionFocus;
      if (ReferenceEquals(next, _focusedInteraction))
        return;

      _focusedInteraction?.SetLocalInteractionFocused(false);
      _focusedInteraction = next;
      _focusedInteraction?.SetLocalInteractionFocused(true);
    }
  }
}
