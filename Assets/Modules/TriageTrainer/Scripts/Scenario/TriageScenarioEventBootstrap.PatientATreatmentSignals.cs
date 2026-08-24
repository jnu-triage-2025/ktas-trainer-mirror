using MultiplayerInfrastructure.Scenario;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void EnablePatientATreatmentSignalHandlers()
      => ScenarioInteractionSignals.OnSignalRegistered += HandlePatientATreatmentSignal;

    private void DisablePatientATreatmentSignalHandlers()
      => ScenarioInteractionSignals.OnSignalRegistered -= HandlePatientATreatmentSignal;

    private void HandlePatientATreatmentSignal(string signalIdentifier)
    {
      if (signalIdentifier == "sig.pass_et_tube_ready")
      {
        SetActiveIfPresent(_patientAEtTubePreparedVisual, false);
        SetActiveIfPresent(_patientAEtTubeInsertedVisual, true);
        ResolvePatientAController()?.SetTreatmentApplied(
          "endotracheal_tube_stylet_inserted",
          true,
          TriageTrainer.Entity.PatientController.TreatmentDisplay.EndotrachealTubeStyletInserted);
        return;
      }

      if (signalIdentifier == "sig.interact_tpiece")
      {
        ResolvePatientAController()?.SetTreatmentApplied(
          "tpiece_attached",
          true,
          TriageTrainer.Entity.PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula);
        return;
      }

      if (signalIdentifier != "sig.remove_intu_stylet") return;
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, false);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, true);
      var patient = ResolvePatientAController();
      patient?.SetTreatmentApplied(
        "endotracheal_tube_stylet_inserted",
        false,
        TriageTrainer.Entity.PatientController.TreatmentDisplay.EndotrachealTubeStyletInserted);
      patient?.SetTreatmentApplied(
        "endotracheal_tube_insert_done",
        true,
        TriageTrainer.Entity.PatientController.TreatmentDisplay.EndotrachealTubeInsertDone);
    }
  }
}
