using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_Insert20gRightPatientB()
    {
      Register("insert_20g_right_patient_b", Event_Insert20gRightPatientB);
    }

    private IEnumerator Event_Insert20gRightPatientB()
    {
      SetActiveIfPresent(_patientB20gRightVisual, true);
      EmitSystemMessage("환자 B 우측 20G 삽입 연출을 적용했습니다.");
      yield break;
    }
  }
}
