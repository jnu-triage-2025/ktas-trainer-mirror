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
      yield break;
    }
  }
}
