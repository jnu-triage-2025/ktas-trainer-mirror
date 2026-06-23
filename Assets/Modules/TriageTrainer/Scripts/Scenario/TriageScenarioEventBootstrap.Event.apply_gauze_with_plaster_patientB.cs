using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzeWithPlasterPatientB()
    {
      Register("apply_gauze_with_plaster_patient_b", Event_ApplyGauzeWithPlasterPatientB);
    }

    private IEnumerator Event_ApplyGauzeWithPlasterPatientB()
    {
      SetActiveIfPresent(_patientBGauzeVisual, false);
      SetActiveIfPresent(_patientBGauzeWithPlasterVisual, true);
      EmitSystemMessage("환자 B 거즈+플라스터 연출을 적용했습니다.");
      yield break;
    }
  }
}
