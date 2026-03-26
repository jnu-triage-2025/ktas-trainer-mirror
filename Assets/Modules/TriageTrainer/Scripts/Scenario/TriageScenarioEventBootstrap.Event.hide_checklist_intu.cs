using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_HideChecklistIntu()
    {
      Register("hide_checklist_intu", Event_HideChecklistIntu);
    }

    private IEnumerator Event_HideChecklistIntu()
    {
      SetActiveIfPresent(_intuChecklistUiPanel, false);
      EmitSystemMessage("기관내삽관 준비물 체크리스트를 숨겼습니다.");
      yield break;
    }
  }
}
