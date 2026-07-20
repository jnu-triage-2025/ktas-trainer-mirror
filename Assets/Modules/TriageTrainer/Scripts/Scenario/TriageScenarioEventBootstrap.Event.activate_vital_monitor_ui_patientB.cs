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
      ConfigureVitalMonitorClose(_patientBVitalMonitorController,
        _patientBVitalMonitorObject,
        _patientBVitalPanel,
        "close_vital_ui_b");
      yield return ApplyMonitorProfile(_patientBVitalMonitorObject,
        _patientBVitalPanel,
        _patientBVitalMonitorController,
        _patientBInitialMonitorParameters,
        _applyPatientBInitialMonitorProfile,
        "환자 B 활력징후 모니터를 활성화했습니다.");
    }
  }
}
