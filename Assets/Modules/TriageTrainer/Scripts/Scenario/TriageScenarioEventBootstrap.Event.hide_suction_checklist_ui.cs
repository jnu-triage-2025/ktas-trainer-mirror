using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_HideSuctionChecklistUi()
    {
      Register("hide_suction_checklist_ui", Event_HideSuctionChecklistUi);
    }

    private IEnumerator Event_HideSuctionChecklistUi()
    {
      ToggleChecklistPanel(ref _suctionChecklistUiPanel,
        false,
        "흡인 준비물 체크리스트를 숨겼습니다.",
        new[]
        {
          "SuctionChecklistPanel",
          "SuctionChecklistUI",
          "ChecklistSuction",
          "흡인체크리스트"
        });
      yield break;
    }
  }
}
