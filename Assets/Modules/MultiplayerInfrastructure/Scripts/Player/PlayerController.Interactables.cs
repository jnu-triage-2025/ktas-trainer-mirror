using System.Collections.Generic;
using MultiplayerInfrastructure.Camera;
using MultiplayerInfrastructure.Dialogue;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Registry;
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

    /// <summary>
    /// DialoguePanelUIController를 씬에서 찾습니다.
    /// Inspector에서 직접 할당하는 것을 권장합니다.
    /// </summary>
    private DialoguePanelUIController FindDialoguePanelUIController()
    {
      GameObject dialogueGO;
      // 방법 1: 태그로 찾기
      //var dialogueGO = GameObject.FindGameObjectWithTag("DialoguePanelUI");
      //if (dialogueGO != null)
      //{
      //  var controller = dialogueGO.GetComponent<DialoguePanelUIController>();
      //  if (controller != null) return controller;
      //}

      // 방법 2: 이름으로 찾기
      dialogueGO = GameObject.Find("DialoguePanelUI");
      if (dialogueGO != null)
      {
        var controller = dialogueGO.GetComponent<DialoguePanelUIController>();
        if (controller != null) return controller;
      }

      // 방법 3: FindObjectOfType (성능상 권장하지 않음)
      var found = FindObjectOfType<DialoguePanelUIController>();
      if (found != null)
      {
        Debug.LogWarning("[PlayerController] DialoguePanelUIController found via FindObjectOfType. Consider assigning it directly in Inspector.");
        return found;
      }

      Debug.LogWarning("[PlayerController] DialoguePanelUIController not found in scene.");
      return null;
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

      // UpdateInteractables를 사용하여 모드에 따라 적절히 처리
      _interactableHintUI.UpdateInteractables(nearby);
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
      var interactable = _interactableHintUI?.GetSelected();
      if (interactable == null) return;

      interactable.Interact(transform);
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
