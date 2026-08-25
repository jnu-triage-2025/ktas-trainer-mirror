using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzePatientB()
    {
      Register("apply_gauze_patient_b", Event_ApplyGauzePatientB);
    }

    private IEnumerator Event_ApplyGauzePatientB()
    {
      SetActiveIfPresent(_patientBGauzeVisual, true);
      EmitSystemMessage("환자 B 거즈 적용 연출을 적용했습니다.");
      yield break;
    }
  }
}
