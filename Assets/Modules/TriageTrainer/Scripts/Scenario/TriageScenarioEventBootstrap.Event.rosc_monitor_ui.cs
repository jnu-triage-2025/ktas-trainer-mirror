using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_RoscMonitorUi()
    {
      Register("rosc_monitor_ui", Event_RoscMonitorUi);
    }

    private IEnumerator Event_RoscMonitorUi()
    {
      yield return ApplyPatientAMonitorProfile(_patientARoscMonitorParameters, "환자 A ROSC 모니터 프로필을 적용했습니다.");
    }
  }
}
