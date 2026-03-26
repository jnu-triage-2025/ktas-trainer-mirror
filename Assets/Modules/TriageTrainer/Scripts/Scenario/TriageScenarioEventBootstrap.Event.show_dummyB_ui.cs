using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowDummyBUi()
    {
      Register("show_dummyB_ui", Event_ShowDummyBUi);
    }

    private IEnumerator Event_ShowDummyBUi()
    {
      yield return ShowPanelTemporarily(_dummyBUiPanel, _uiPanelAutoHideSeconds, "더미 B 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
