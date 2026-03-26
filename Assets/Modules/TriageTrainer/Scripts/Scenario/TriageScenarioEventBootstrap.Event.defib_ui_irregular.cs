using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_DefibUiIrregular()
    {
      Register("defib_ui_irregular", Event_DefibUiIrregular);
    }

    private IEnumerator Event_DefibUiIrregular()
    {
      SetActiveIfPresent(_defibIrregularUiPanel, true);
      yield return ApplyPatientAMonitorProfile(_patientADefibIrregularMonitorParameters,
        "제세동기 비정상 리듬 화면 연출을 적용했습니다.");
    }
  }
}
