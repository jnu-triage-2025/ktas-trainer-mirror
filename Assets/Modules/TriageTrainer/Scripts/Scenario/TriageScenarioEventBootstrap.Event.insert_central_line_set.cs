using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_InsertCentralLineSet()
    {
      Register("insert_central_line_set", Event_InsertCentralLineSet);
    }

    private IEnumerator Event_InsertCentralLineSet()
    {
      SetActiveIfPresent(_patientACentralLineVisual, true);
      EmitSystemMessage("환자 A 중심정맥관 삽입 연출을 적용했습니다.");
      yield break;
    }
  }
}
