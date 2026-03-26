using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_PupilReflexPatientB()
    {
      Register("pupil_reflex_patientB", Event_PupilReflexPatientB);
    }

    private IEnumerator Event_PupilReflexPatientB()
    {
      yield return ShowPupilReflexPanel(_patientBPupilReflexUiPanel,
        _patientBPupilLeftReactiveIndicator,
        _patientBPupilRightFixedIndicator,
        "환자 B 동공반사 패널을 표시했습니다. (좌측 반응, 우측 고정)");
    }
  }
}
