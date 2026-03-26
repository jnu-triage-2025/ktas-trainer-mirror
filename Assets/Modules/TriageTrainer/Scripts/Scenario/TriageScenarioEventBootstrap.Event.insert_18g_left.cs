using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_Insert18gLeft()
    {
      Register("insert_18g_left", Event_Insert18gLeft);
    }

    private IEnumerator Event_Insert18gLeft()
    {
      SetActiveIfPresent(_patientA18gLeftVisual, true);
      EmitSystemMessage("환자 A 좌측 18G 삽입 연출을 적용했습니다.");
      yield break;
    }
  }
}
