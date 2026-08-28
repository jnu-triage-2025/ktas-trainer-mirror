using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzePatientB()
    {
      Register("apply_gauze_patient_b", Event_ApplyGauzePatientB);
    }

    private IEnumerator Event_ApplyGauzePatientB()
    {
      SetActiveIfPresent(_patientBGauzeVisual, true);
      yield break;
    }
  }
}
