using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ApplyGauzeWithPlasterPatientC()
    {
      Register("apply_gauze_with_plaster_patient_c", Event_ApplyGauzeWithPlasterPatientC);
    }

    private IEnumerator Event_ApplyGauzeWithPlasterPatientC()
    {
      SetActiveIfPresent(_patientCGauzeVisual, false);
      SetActiveIfPresent(_patientCGauzeWithPlasterVisual, true);
      yield break;
    }
  }
}
