using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ActivateVitalMonitorUiPatientC()
    {
      Register("activate_vital_monitor_ui_patient_c", Event_ActivateVitalMonitorUiPatientC);
    }

    private IEnumerator Event_ActivateVitalMonitorUiPatientC()
    {
      ResolveRuntimeReferencesIfNeeded();
      yield return ApplyMonitorProfile(_patientCVitalMonitorObject,
        _patientCVitalPanel,
        _patientCVitalMonitorController,
        _patientCInitialMonitorParameters,
        _applyPatientCInitialMonitorProfile,
        "환자 C 활력징후 모니터를 활성화했습니다.");
    }
  }
}
