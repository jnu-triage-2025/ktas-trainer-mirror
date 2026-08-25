using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyAmbuPatientA()
    {
      Register("apply_ambu_patient_a", Event_ApplyAmbuPatientA);
    }

    private IEnumerator Event_ApplyAmbuPatientA()
    {
      SetActiveIfPresent(_patientAAmbuConnectedVisual, true);
      EmitSystemMessage("환자 A 앰부백 연결 연출을 적용했습니다.");
      yield break;
    }
  }
}
