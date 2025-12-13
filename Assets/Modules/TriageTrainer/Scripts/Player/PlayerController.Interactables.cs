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
    [Header("Interactable Configuration")]
    [SerializeField] private KeyCode _interactKey = KeyboardConfigurationRegistry.InteractInteractableObject;
    [Header("References")]
    [SerializeField] private InteractableObjectHintUIController _interactableHintUI;
    [SerializeField] private NearbyInteractablesDetector _detector;

    void Start_Interactables()
    {
      if (_interactableHintUI == null)
        _interactableHintUI = UIControlRegistry.Get<InteractableObjectHintUIController>();

      if (_detector.IsUnityNull())
        _detector = CurrentSessionPlayInfoRegistry.Get<NearbyInteractablesDetector>();

      if (_detector != null)
      {
        _detector.NearbyUpdated += HandleNearbyUpdated;
        HandleNearbyUpdated(_detector.Nearby);
      }
    }

    void Update_Interactables()
    {
      if (_interactKey != KeyCode.None && Input.GetKeyDown(_interactKey))
        TryInteractWithSelection();

      // _interactableHintUI.PrintoutForDebug();
    }

    void OnDestroy()
    {
      if (_detector != null)
        _detector.NearbyUpdated -= HandleNearbyUpdated;
    }

    private void HandleNearbyUpdated(IReadOnlyList<IInteractable> nearby)
    {
      if (_interactableHintUI == null) return;

      _interactableHintUI.Clear();

      if (nearby == null) return;

      for (int i = 0; i < nearby.Count; i++)
        _interactableHintUI.Add(nearby[i]);
    }

    private void TryInteractWithSelection()
    {
      var interactable = _interactableHintUI?.GetSelected();
      if (interactable == null) return;

      interactable.Interact(transform);
    }
  }
}
