using System.Collections.Generic;
using TriageTrainer.Camera;
using TriageTrainer.InteractableEntity;
using TriageTrainer.Registry;
using TriageTrainer.UI;
using Unity.VisualScripting;
using UnityEngine;

namespace TriageTrainer.Player
{
  /// <summary>
  /// Interactables과의 상호작용을 정의합니다.
  ///
  /// 의존:
  /// - 주위의 Interactable 객체를 감지:
  ///     - InteractableDetector(카메라를 기준으로 상호작용하므로, Camera 디렉토리에 배치됨)
  /// - Interactable 감지 결과를 UI에 반영:
  ///     - InteractableObjectHintUIController
  /// </summary>
  public partial class PlayerController
  {
    [SerializeField] private NearbyInteractablesDetector _detector;
    [SerializeField] private InteractableObjectHintUIController _interactableHintUI;

    void OnStartClient_Interactables()
    {
      if (!IsOwner) return;
      // PlayerController.Camera must be initialized first
      _detector = _camControl.GetComponent<NearbyInteractablesDetector>();
      _interactableHintUI = _camControl.GetComponent<InteractableObjectHintUIController>();
      _detector.RegisterDetectBased(transform);

      if (_detector != null)
      {
        _detector.NearbyUpdated += HandleNearbyUpdated;
        HandleNearbyUpdated(_detector.Nearby);
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

      _interactableHintUI.Clear();

      if (nearby == null) return;

      for (int i = 0; i < nearby.Count; i++)
        _interactableHintUI.Add(nearby[i]);
    }

    // called from PlayerController.Input
    private void TryInteractWithSelection()
    {
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
