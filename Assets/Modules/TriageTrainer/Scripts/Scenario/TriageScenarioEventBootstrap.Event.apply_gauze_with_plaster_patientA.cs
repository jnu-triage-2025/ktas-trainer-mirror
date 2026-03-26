using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzeWithPlasterPatientA()
    {
      Register("apply_gauze_with_plaster_patientA", Event_ApplyGauzeWithPlasterPatientA);
    }

    private IEnumerator Event_ApplyGauzeWithPlasterPatientA()
    {
      SetActiveIfPresent(_patientAGauzeVisual, false);
      SetActiveIfPresent(_patientAGauzeWithPlasterVisual, true);
      EmitSystemMessage("환자 A 거즈+플라스터 연출을 적용했습니다.");
      yield break;
    }
  }
}
