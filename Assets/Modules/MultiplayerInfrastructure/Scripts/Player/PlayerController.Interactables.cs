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

    void OnStartClient_Interactables()
    {
      if (!IsOwner) return;

      // Camera components
      _detector = _camControl.GetComponent<NearbyInteractablesDetector>();
      _interactableHintUI = _camControl.GetComponent<InteractableObjectHintUIController>();

      _detector.RegisterDetectBased(transform);

      if (_detector != null)
      {
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
    }

    void OnDestroy()
    {
      StopSpectateFollow();

      if (_detector != null)
        _detector.NearbyUpdated -= HandleNearbyUpdated;
    }

    private void HandleNearbyUpdated(IReadOnlyList<IInteractable> nearby)
    {
      Debug.Log($"[PlayerController] Nearby interactables updated: {nearby.Count} items found.");
      if (_interactableHintUI == null) return;

      var interacts = new List<IInteract>();
      if (nearby != null)
      {
        for (int i = 0; i < nearby.Count; i++)
        {
          var interactable = nearby[i];
          if (interactable == null) continue;

          var eachInteracts = interactable.Interacts;
          if (eachInteracts == null || eachInteracts.Length == 0) continue;

          for (int j = 0; j < eachInteracts.Length; j++)
          {
            var interact = eachInteracts[j];
            if (interact != null)
              interacts.Add(interact);
          }
        }
      }

      // UpdateInteractables를 사용하여 모드에 따라 적절히 처리
      _interactableHintUI.UpdateInteractables(interacts);
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

      interact.Interact(transform);
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
    }
  }
}
