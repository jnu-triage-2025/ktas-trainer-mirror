using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowChecklistIntu()
    {
      Register("show_checklist_intu", Event_ShowChecklistIntu);
    }

    private IEnumerator Event_ShowChecklistIntu()
    {
      ToggleChecklistPanel(ref _intuChecklistUiPanel,
        true,
        "기관내삽관 준비물 체크리스트를 표시했습니다.",
        new[]
        {
          "IntuChecklistPanel",
          "IntubationChecklistPanel",
          "ChecklistIntu",
          "삽관체크리스트"
        });
      yield break;
    }
  }
}
