using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_InsertEtTube()
    {
      Register("insert_et_tube", Event_InsertEtTube);
    }

    private IEnumerator Event_InsertEtTube()
    {
      SetActiveIfPresent(_patientAEtTubePreparedVisual, false);
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, true);
      ResolvePatientAController()?.SetTreatmentApplied(
        "endotracheal_tube_stylet_inserted",
        true,
        TriageTrainer.Entity.PatientController.TreatmentDisplay.EndotrachealTubeStyletInserted);
      yield break;
    }
  }
}
