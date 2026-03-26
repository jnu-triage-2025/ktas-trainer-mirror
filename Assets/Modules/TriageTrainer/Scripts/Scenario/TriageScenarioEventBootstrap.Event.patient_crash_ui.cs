using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_PatientCrashUi()
    {
      Register("patient_crash_ui", Event_PatientCrashUi);
    }

    private IEnumerator Event_PatientCrashUi()
    {
      yield return ApplyPatientAMonitorProfile(_patientACrashMonitorParameters, "환자 A 상태 악화 모니터 프로필을 적용했습니다.");
    }
  }
}
