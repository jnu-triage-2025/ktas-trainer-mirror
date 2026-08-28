using System.Collections;
using TriageTrainer.Entity;

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
      ResolvePatientAController()?.ApplyScenarioDisplayState(
        nameof(PatientController.TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian),
        true);
      yield break;
    }
  }
}
