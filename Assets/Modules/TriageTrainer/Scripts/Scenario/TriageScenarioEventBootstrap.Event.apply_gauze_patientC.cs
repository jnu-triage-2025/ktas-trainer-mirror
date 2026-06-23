using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzePatientC()
    {
      Register("apply_gauze_patient_c", Event_ApplyGauzePatientC);
    }

    private IEnumerator Event_ApplyGauzePatientC()
    {
      SetActiveIfPresent(_patientCGauzeVisual, true);
      EmitSystemMessage("환자 C 거즈 적용 연출을 적용했습니다.");
      yield break;
    }
  }
}
