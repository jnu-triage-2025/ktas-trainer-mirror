using UnityEngine;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Patient Selection")]
    [SerializeField] private bool _isPatientSelectionMode;

    public bool IsPatientSelectionMode => _isPatientSelectionMode;

    public void SetPatientSelectionMode(bool enabled)
    {
      _isPatientSelectionMode = enabled;
      RefreshInteractableHintsNow();
    }
  }
}
