using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ActivateVitalMonitorUiPatientA()
    {
      Register("activate_vital_monitor_ui_patientA", Event_ActivateVitalMonitorUiPatientA);
    }

    private IEnumerator Event_ActivateVitalMonitorUiPatientA()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientAVitalMonitorObject, true);
      SetActiveIfPresent(_patientAVitalPanel, true);

      if (_patientAVitalMonitorController != null)
      {
        if (_applyPatientAInitialMonitorProfile)
        {
          _patientAVitalMonitorController.SetCustomParameters(_patientAInitialMonitorParameters);
        }

        _patientAVitalMonitorController.enabled = true;
      }

      EmitSystemMessage("환자 A 활력징후 모니터를 활성화했습니다.");
      yield break;
    }
  }
}
