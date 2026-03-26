using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzeWithPlasterPatientC()
    {
      Register("apply_gauze_with_plaster_patientC", Event_ApplyGauzeWithPlasterPatientC);
    }

    private IEnumerator Event_ApplyGauzeWithPlasterPatientC()
    {
      SetActiveIfPresent(_patientCGauzeVisual, false);
      SetActiveIfPresent(_patientCGauzeWithPlasterVisual, true);
      EmitSystemMessage("환자 C 거즈+플라스터 연출을 적용했습니다.");
      yield break;
    }
  }
}
