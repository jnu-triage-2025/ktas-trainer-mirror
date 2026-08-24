using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ActivateVitalMonitorUiPatientA()
    {
      Register("activate_vital_monitor_ui_patient_a", Event_ActivateVitalMonitorUiPatientA);
    }

    private IEnumerator Event_ActivateVitalMonitorUiPatientA()
    {
      ResolveRuntimeReferencesIfNeeded();

      ResolvePatientVitalMonitor(_patientAObject,
        ref _patientAVitalMonitorObject,
        ref _patientAVitalMonitorController);
      var patient = _patientAObject != null
        ? _patientAObject.GetComponentInChildren<TriageTrainer.Entity.PatientController>(true)
        : null;
      ConfigureVitalMonitorClose(_patientAVitalMonitorController,
        patient,
        "close_vital_ui_a");

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
