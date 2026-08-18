using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ActivateVitalMonitorUiPatientB()
    {
      Register("activate_vital_monitor_ui_patient_b", Event_ActivateVitalMonitorUiPatientB);
    }

    private IEnumerator Event_ActivateVitalMonitorUiPatientB()
    {
      ResolveRuntimeReferencesIfNeeded();
      ResolvePatientVitalMonitor(_patientBObject,
        ref _patientBVitalMonitorObject,
        ref _patientBVitalMonitorController);
      var patient = _patientBObject != null
        ? _patientBObject.GetComponentInChildren<TriageTrainer.Entity.PatientController>(true)
        : null;
      ConfigureVitalMonitorClose(_patientBVitalMonitorController,
        patient,
        _patientBVitalMonitorObject,
        _patientBVitalPanel,
        "close_vital_ui_b");
      yield return ApplyMonitorProfile(_patientBVitalMonitorObject,
        _patientBVitalPanel,
        _patientBVitalMonitorController,
        _patientBInitialMonitorParameters,
        _applyPatientBInitialMonitorProfile,
        "많이 다친 남성 환자 활력징후 모니터를 활성화했습니다.");
      _patientBVitalMonitorController?.OpenPresentation();
    }
  }
}
