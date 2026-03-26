using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowSuctionChecklistUi()
    {
      Register("show_suction_checklist_ui", Event_ShowSuctionChecklistUi);
    }

    private IEnumerator Event_ShowSuctionChecklistUi()
    {
      SetActiveIfPresent(_suctionChecklistUiPanel, true);
      EmitSystemMessage("흡인 준비물 체크리스트를 표시했습니다.");
      yield break;
    }
  }
}
