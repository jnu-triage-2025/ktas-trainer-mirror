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
      SetActiveIfPresent(_intuChecklistUiPanel, true);
      EmitSystemMessage("기관내삽관 준비물 체크리스트를 표시했습니다.");
      yield break;
    }
  }
}
