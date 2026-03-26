using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowDummyAUi()
    {
      Register("show_dummyA_ui", Event_ShowDummyAUi);
    }

    private IEnumerator Event_ShowDummyAUi()
    {
      yield return ShowPanelTemporarily(_dummyAUiPanel, _uiPanelAutoHideSeconds, "더미 A 상태 패널을 표시했습니다.");
      yield break;
    }
  }
}
