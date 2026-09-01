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
    /// 범용 라인 연결 API 이다. IV(intravenous) 명칭의 직렬화 필드는 기존 씬과
    /// 프리팹 데이터를 보존하기 위해 유지한다.
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
