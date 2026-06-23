using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_Insert20gLeftPatientC()
    {
      Register("insert_20g_left_patient_c", Event_Insert20gLeftPatientC);
    }

    private IEnumerator Event_Insert20gLeftPatientC()
    {
      SetActiveIfPresent(_patientC20gLeftVisual, true);
      EmitSystemMessage("환자 C 좌측 20G 삽입 연출을 적용했습니다.");
      yield break;
    }
  }
}
