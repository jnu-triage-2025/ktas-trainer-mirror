using System.Collections.Generic;
using MultiplayerInfrastructure.InteractableEntity;
using MultiplayerInfrastructure.Player;
using MultiplayerInfrastructure.Scenario;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  /// <summary>
  /// 시나리오에서 요구하는 물체/부위 상호작용을 위한 경량 Interactable 입니다.
  /// 상호작용이 확정되면 완료 신호를 올리고, 필요하면 연결된 시각 오브젝트를 표시 또는 숨깁니다.
  /// </summary>
  [DisallowMultipleComponent]
  [RequireComponent(typeof(Collider))]
  public sealed class ScenarioActionInteractable : MonoBehaviour, IInteractable, IInteract, IInteractorConditional, IInteractToggleable, IInteractDisplayIcons
  {
    [Header("Scenario Action")]
    [SerializeField] private string _displayText = "상호작용";
    [SerializeField] private Sprite _displayIcon;
    [SerializeField] private List<Sprite> _displayIcons = new();
    [SerializeField] private string _completionSignal;
    [SerializeField] private bool _enabled = true;
    [SerializeField] private bool _consumeOnce = true;

    [Header("Visual State (optional)")]
    [Tooltip("상호작용 성공 시 표시할 오브젝트입니다.")]
    [SerializeField] private GameObject[] _activateOnInteract;
    [Tooltip("상호작용 성공 시 숨길 오브젝트입니다.")]
    [SerializeField] private GameObject[] _deactivateOnInteract;

    private bool _completed;

    public IInteract[] Interacts => new IInteract[] { this };
    public string DisplayText => _displayText;
    public Sprite DisplayIcon => _displayIcon;
    public IReadOnlyList<Sprite> DisplayIcons => _displayIcons;
    public bool AllowDisplayIconFallback => true;
    public Color DisplayColor => Color.white;

    public bool CanInteract(Transform interactor)
    {
      if (!_enabled || (_consumeOnce && _completed))
        return false;

      return interactor != null && interactor.GetComponentInParent<PlayerController>() != null;
    }

    public void Interact(Transform interactor)
    {
      if (!CanInteract(interactor))
        return;

      SetObjectsActive(_activateOnInteract, true);
      SetObjectsActive(_deactivateOnInteract, false);

      if (!string.IsNullOrWhiteSpace(_completionSignal))
        ScenarioInteractionSignals.Raise(_completionSignal);

      _completed = true;
    }

    public void SetEnabled(bool enabled)
    {
      _enabled = enabled;
      if (enabled)
        _completed = false;
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
      if (targets == null)
        return;

      for (int i = 0; i < targets.Length; i++)
      {
        if (targets[i] != null)
          targets[i].SetActive(active);
      }
    }
  }
}
