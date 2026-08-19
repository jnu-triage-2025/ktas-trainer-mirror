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
        "close_vital_ui_b");
      // 모니터 활성화는 시나리오 연출일 뿐이므로 채팅 시스템 메시지를 남기지 않는다.
      yield return ApplyMonitorProfile(_patientBVitalMonitorObject,
        _patientBVitalPanel,
        _patientBVitalMonitorController,
        _patientBInitialMonitorParameters,
        _applyPatientBInitialMonitorProfile);
    }
  }
}
