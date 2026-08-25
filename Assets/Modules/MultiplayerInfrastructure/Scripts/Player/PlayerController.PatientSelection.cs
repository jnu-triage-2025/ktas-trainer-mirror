using MultiplayerInfrastructure.Definitions;
using MultiplayerInfrastructure.Registry;
using MultiplayerInfrastructure.UI;
using UnityEngine;

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
    public bool IsLineConnectionMode => _isIntravenousLineConnectionMode;

    /// <summary>
    /// Generic line-connection API. The serialized IV-named backing field is
    /// retained to preserve existing scene and prefab data.
    /// </summary>
    public void SetLineConnectionMode(bool enabled, bool showActionbarHint = true) =>
      SetIntravenousLineConnectionMode(enabled, showActionbarHint);

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
      var controllers = FindObjectsByType<TriageTrainer.Entity.LineConnection.LineConnectionService>(
        FindObjectsInactive.Exclude,
        FindObjectsSortMode.None);

      for (int i = 0; i < controllers.Length; i++)
        controllers[i]?.CancelConnectionMode(this);
    }
  }
}
