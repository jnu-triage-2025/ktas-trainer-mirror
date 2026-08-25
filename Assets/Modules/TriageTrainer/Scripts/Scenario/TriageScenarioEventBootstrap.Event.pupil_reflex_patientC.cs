using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_PupilReflexPatientC()
    {
      Register("pupil_reflex_patient_c", Event_PupilReflexPatientC);
    }

    private IEnumerator Event_PupilReflexPatientC()
    {
      yield return ShowPupilReflexPanel(_patientCPupilReflexUiPanel,
        _patientCPupilRightReactiveIndicator,
        _patientCPupilLeftFixedIndicator,
        "환자 C 동공반사 패널을 표시했습니다. (우측 반응, 좌측 고정)");
    }
  }
}
