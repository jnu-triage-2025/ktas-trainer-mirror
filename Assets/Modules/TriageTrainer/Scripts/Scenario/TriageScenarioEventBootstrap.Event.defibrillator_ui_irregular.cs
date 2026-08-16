using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_DefibrillatorUiIrregular()
    {
      Register("defibrillator_ui_irregular", Event_DefibrillatorUiIrregular);
    }

    private IEnumerator Event_DefibrillatorUiIrregular()
    {
      SetActiveIfPresent(_defibrillatorIrregularUiPanel, true);
      yield return ApplyPatientAMonitorProfile(_patientADefibrillatorIrregularMonitorParameters,
        "제세동기 비정상 리듬 화면 연출을 적용했습니다.");
    }
  }
}
