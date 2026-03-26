using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowPatientCUi()
    {
      Register("show_patient_c_ui", Event_ShowPatientCUi);
    }

    private IEnumerator Event_ShowPatientCUi()
    {
      yield return ShowPanelTemporarily(_patientCUiPanel, _uiPanelAutoHideSeconds, "환자 C 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
