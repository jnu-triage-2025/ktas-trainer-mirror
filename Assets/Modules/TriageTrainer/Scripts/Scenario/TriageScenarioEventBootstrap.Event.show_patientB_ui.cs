using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowPatientBUi()
    {
      Register("show_patientB_ui", Event_ShowPatientBUi);
    }

    private IEnumerator Event_ShowPatientBUi()
    {
      yield return ShowPanelTemporarily(_patientBUiPanel, _uiPanelAutoHideSeconds, "환자 B 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
