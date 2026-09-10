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
        ResolvePatientAController()?.RequestApplyPatientATreatmentSignal(signalIdentifier);
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

      if (signalIdentifier != "sig.remove_intu_stylet")
        return;
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, false);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, true);
      var patient = ResolvePatientAController();
      patient?.RequestApplyPatientATreatmentSignal(signalIdentifier);
    }
  }
}
