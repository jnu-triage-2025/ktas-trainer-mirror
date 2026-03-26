using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ShowIvChecklist()
    {
      Register("show_iv_checklist", Event_ShowIvChecklist);
    }

    private IEnumerator Event_ShowIvChecklist()
    {
      SetActiveIfPresent(_ivChecklistUiPanel, true);
      EmitSystemMessage("IV 준비물 체크리스트를 표시했습니다.");
      yield break;
    }
  }
}
