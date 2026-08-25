using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowPatientAUi()
    {
      Register("show_patientA_ui", Event_ShowPatientAUi);
    }

    private IEnumerator Event_ShowPatientAUi()
    {
      yield return ShowPanelTemporarily(_patientAUiPanel, _uiPanelAutoHideSeconds, "환자 A 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
