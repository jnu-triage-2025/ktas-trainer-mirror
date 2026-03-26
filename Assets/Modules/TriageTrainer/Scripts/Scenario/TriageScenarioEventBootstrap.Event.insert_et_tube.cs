using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_InsertEtTube()
    {
      Register("insert_et_tube", Event_InsertEtTube);
    }

    private IEnumerator Event_InsertEtTube()
    {
      SetActiveIfPresent(_patientAEtTubePreparedVisual, false);
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, true);
      EmitSystemMessage("환자 A 기관내관 삽입 연출을 적용했습니다.");
      yield break;
    }
  }
}
