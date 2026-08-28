using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzeWithPlasterPatientB()
    {
      Register("apply_gauze_with_plaster_patient_b", Event_ApplyGauzeWithPlasterPatientB);
    }

    private IEnumerator Event_ApplyGauzeWithPlasterPatientB()
    {
      SetActiveIfPresent(_patientBGauzeVisual, false);
      SetActiveIfPresent(_patientBGauzeWithPlasterVisual, true);
      yield break;
    }
  }
}
