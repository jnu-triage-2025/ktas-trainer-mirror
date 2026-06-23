using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzePatientA()
    {
      Register("apply_gauze_patient_a", Event_ApplyGauzePatientA);
    }

    private IEnumerator Event_ApplyGauzePatientA()
    {
      SetActiveIfPresent(_patientAGauzeVisual, true);
      EmitSystemMessage("환자 A 거즈 적용 연출을 적용했습니다.");
      yield break;
    }
  }
}
