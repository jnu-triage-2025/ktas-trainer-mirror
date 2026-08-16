using System.Collections.Generic;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  /// <summary>
  /// Interactables과의 상호작용을 정의합니다.
  /// </summary>
  public partial class PlayerController
  {
    [SerializeField] private NearbyInteractablesDetector _detector;
    [SerializeField] private InteractableObjectHintUIController _interactableHintUI;

    [Header("Dialogue")]
    [Tooltip("씬에 배치된 DialoguePanelUIController 참조 (Inspector에서 할당하거나 태그/이름으로 검색)")]
    [SerializeField] private DialoguePanelUIController _dialoguePanelUIController;
    private ILocalInteractionFocus _focusedInteraction;

    void OnStartClient_Interactables()
    {
      if (!IsOwner) return;

      if (_camControl == null)
      {
        Debug.LogWarning("[PlayerController] Camera controller is not ready; skipping interactables initialization.");
        return;
      }

      // Camera components
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
    }

    void OnDestroy()
    {
      OnStopClient_UIOverlaySync();

      StopSpectateFollow();

      if (_detector != null)
        _detector.NearbyUpdated -= HandleNearbyUpdated;

      if (_interactableHintUI != null)
        _interactableHintUI.InteractionClicked -= HandleInteractionMenuClicked;

      OnDestroy_Item();
      OnDestroy_PlaceableItemPreview();
    }

    private void HandleNearbyUpdated(IReadOnlyList<IInteractable> nearby)
    {
#if UNITY_EDITOR && (DEBUG == true) && false
      Debug.Log($"[PlayerController] Nearby interactables updated: {nearby.Count} items found.");
#endif
      if (_interactableHintUI == null) return;

      var interacts = CollectAvailableInteracts(nearby);
      KeepNearestExclusiveInteracts(interacts, _detector != null ? _detector.DetectionPosition : transform.position);

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

    public void RefreshInteractableHintsNow()
    {
      if (!IsOwner)
        return;

      if (_detector == null)
        return;

      HandleNearbyUpdated(_detector.Nearby);
    }

    // called from PlayerController.Input
    private void TryInteractWithSelection()
    {
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
      if (interact == null) return;

      // UI가 갱신되는 두 query 사이에 경계를 넘으면 이전 최단 후보가 잠시 선택 상태로 남을 수 있다.
      // 실행 직전에 현재 감지 목록과 조건을 다시 평가하여 더 먼 static entity가 활성화되지 않게 한다.
      if (!IsCurrentlyAvailableInteract(interact))
      {
        RefreshInteractableHintsNow();
        return;
      }

      interact.Interact(transform);
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

    // called from PlayerController.Input
    private void HandleInteractablesSelectionInput()
    {
      if (_interactableHintUI == null) return;

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
