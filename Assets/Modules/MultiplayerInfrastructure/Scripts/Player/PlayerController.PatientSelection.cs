using UnityEngine;
using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;

namespace MultiplayerInfrastructure.Player
{
  public partial class PlayerController
  {
    [Header("Patient Selection")]
    [SerializeField] private bool _isPatientSelectionMode;

    [Header("Intravenous Line Connection")]
    [SerializeField] private bool _isIntravenousLineConnectionMode;

    private TitleUIController _titleUI;

    public bool IsPatientSelectionMode => _isPatientSelectionMode;
    public bool IsIntravenousLineConnectionMode => _isIntravenousLineConnectionMode;

    public void SetPatientSelectionMode(bool enabled)
    {
      _isPatientSelectionMode = enabled;
      RefreshInteractableHintsNow();
    }

    public void SetIntravenousLineConnectionMode(bool enabled, bool showActionbarHint = true)
    {
      if (_isIntravenousLineConnectionMode == enabled)
      {
        if (enabled && showActionbarHint)
          TryShowIntravenousLineModeActionbar();
        return;
      }

      _isIntravenousLineConnectionMode = enabled;

      if (enabled)
      {
        if (showActionbarHint)
          TryShowIntravenousLineModeActionbar();
      }
      else
      {
        TryClearActionbar();
      }

      RefreshInteractableHintsNow();

      if (!enabled)
        NotifyIntravenousLineConnectionModeCancelled();
    }

    private void TryShowIntravenousLineModeActionbar()
    {
      if (!IsOwner)
        return;

      EnsureTitleUI();
      _titleUI?.ShowActionbar(ConstantString.HintExitIntravenousLineConnectionMode);
    }

    private void TryClearActionbar()
    {
      if (!IsOwner)
        return;

      EnsureTitleUI();
      _titleUI?.ClearActionbar();
    }

    private void EnsureTitleUI()
    {
      if (_titleUI != null)
        return;

      _titleUI = Registry.Registry.Get<TitleUIController>(RegistryType.UI, Registry.Registry.TypeKey<TitleUIController>());
    }

    private void NotifyIntravenousLineConnectionModeCancelled()
    {
      var controllers = FindObjectsByType<TriageTrainer.Entity.IntravenousLine.IntravenousLineConnectionService>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);

      for (int i = 0; i < controllers.Length; i++)
        controllers[i]?.CancelConnectionMode(this);
    }
  }
}
