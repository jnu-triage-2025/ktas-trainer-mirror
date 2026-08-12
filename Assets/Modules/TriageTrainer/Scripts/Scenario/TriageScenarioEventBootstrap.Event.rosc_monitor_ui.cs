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
      var patient = ResolvePatientAController();
      if (patient != null)
      {
        // 첫 심정지 맥박 확인 메뉴를 제거하고 ROSC 확인용 메뉴만 노출한다.
        // 두 메뉴가 동시에 나타나는 것과 r1 신호가 V031을 통과하지 못하는 것을 함께 방지한다.
        patient.SetAssessActionEnabled("assess_pulse_r1", false);
        patient.SetAssessActionEnabled("assess_pulse_r2", true);
      }

      yield return ApplyPatientAMonitorProfile(_patientARoscMonitorParameters, "환자 A ROSC 모니터 프로필을 적용했습니다.");
    }
  }
}
