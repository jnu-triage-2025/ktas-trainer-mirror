using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_AsystoleMonitorUi()
    {
      Register("asystole_monitor_ui", Event_AsystoleMonitorUi);
    }

    private IEnumerator Event_AsystoleMonitorUi()
    {
      yield return ApplyPatientAMonitorProfile(_patientAAsystoleMonitorParameters, "환자 A 무수축(Asystole) 모니터 프로필을 적용했습니다.");
    }
  }
}
