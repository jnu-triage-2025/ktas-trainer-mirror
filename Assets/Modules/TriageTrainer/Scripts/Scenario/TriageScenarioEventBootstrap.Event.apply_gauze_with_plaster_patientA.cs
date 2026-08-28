using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzeWithPlasterPatientA()
    {
      Register("apply_gauze_with_plaster_patient_a", Event_ApplyGauzeWithPlasterPatientA);
    }

    private IEnumerator Event_ApplyGauzeWithPlasterPatientA()
    {
      SetActiveIfPresent(_patientAGauzeVisual, false);
      SetActiveIfPresent(_patientAGauzeWithPlasterVisual, true);
      yield break;
    }
  }
}
