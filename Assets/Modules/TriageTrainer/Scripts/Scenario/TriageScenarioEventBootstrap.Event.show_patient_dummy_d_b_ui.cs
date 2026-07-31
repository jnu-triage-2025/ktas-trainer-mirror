using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowPatientDummyDBUi()
    {
      Register("show_patient_dummy_d_b_ui", Event_ShowPatientDummyDBUi);
    }

    private IEnumerator Event_ShowPatientDummyDBUi()
    {
      yield return ShowPanelTemporarily(_patientDummyDBUiPanel, _uiPanelAutoHideSeconds, "patient_dummy_d_b 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
