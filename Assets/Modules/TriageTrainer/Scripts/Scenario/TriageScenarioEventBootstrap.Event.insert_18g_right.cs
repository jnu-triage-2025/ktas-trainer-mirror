using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_Insert18gRight()
    {
      Register("insert_18g_right", Event_Insert18gRight);
    }

    private IEnumerator Event_Insert18gRight()
    {
      SetActiveIfPresent(_patientA18gRightVisual, true);
      EmitSystemMessage("환자 A 우측 18G 삽입 연출을 적용했습니다.");
      yield break;
    }
  }
}
