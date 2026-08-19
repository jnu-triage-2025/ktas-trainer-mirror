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
      ResolvePatientVitalMonitor(_patientCObject,
        ref _patientCVitalMonitorObject,
        ref _patientCVitalMonitorController);
      var patient = _patientCObject != null
        ? _patientCObject.GetComponentInChildren<TriageTrainer.Entity.PatientController>(true)
        : null;
      ConfigureVitalMonitorClose(_patientCVitalMonitorController,
        patient,
        "close_vital_ui_c");
      // 모니터 활성화는 시나리오 연출일 뿐이므로 채팅 시스템 메시지를 남기지 않는다.
      yield return ApplyMonitorProfile(_patientCVitalMonitorObject,
        _patientCVitalPanel,
        _patientCVitalMonitorController,
        _patientCInitialMonitorParameters,
        _applyPatientCInitialMonitorProfile);
    }
  }
}
