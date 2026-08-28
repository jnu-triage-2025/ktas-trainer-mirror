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
      yield break;
    }
  }
}
