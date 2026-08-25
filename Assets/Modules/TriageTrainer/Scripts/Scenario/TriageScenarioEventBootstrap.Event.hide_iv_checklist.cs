using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_HideIvChecklist()
    {
      Register("hide_iv_checklist", Event_HideIvChecklist);
    }

    private IEnumerator Event_HideIvChecklist()
    {
      ToggleChecklistPanel(ref _ivChecklistUiPanel,
        false,
        "IV 준비물 체크리스트를 숨겼습니다.",
        new[]
        {
          "IvChecklistPanel",
          "IVChecklistPanel",
          "ChecklistIV",
          "수액체크리스트"
        });
      yield break;
    }
  }
}
