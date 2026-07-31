using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowPatientDummyDAUi()
    {
      Register("show_patient_dummy_d_a_ui", Event_ShowPatientDummyDAUi);
    }

    private IEnumerator Event_ShowPatientDummyDAUi()
    {
      yield return ShowPanelTemporarily(_patientDummyDAUiPanel, _uiPanelAutoHideSeconds, "patient_dummy_d_a 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
