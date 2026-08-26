using System.Collections;
using TriageTrainer.Entity;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_ActivatePatientAStageInteractions()
    {
      Register("activate_patient_a_vital_assess", Event_ActivatePatientAVitalAssess);
      Register("activate_patient_a_avpu_gcs_assess", Event_ActivatePatientAAvpuGcsAssess);
      Register("activate_patient_a_arrest_actions", Event_ActivatePatientAArrestActions);
      Register("activate_patient_a_stylet_removal", Event_ActivatePatientAStyletRemoval);
      Register("activate_patient_a_tpiece_attach", Event_ActivatePatientATpieceAttach);
      Register("activate_patient_a_cpr2_actions", Event_ActivatePatientACpr2Actions);
      Register("activate_patient_a_clothing_removal", Event_ActivatePatientAClothingRemoval);
    }

    private IEnumerator Event_ActivatePatientAVitalAssess()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
        patient.SetAssessActionEnabled("assess_vital", true);
      yield break;
    }

    private IEnumerator Event_ActivatePatientAAvpuGcsAssess()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
        patient.SetAssessActionEnabled("assess_avpu_gcs", true);
      yield break;
    }

    private IEnumerator Event_ActivatePatientAArrestActions()
    {
      var patient = ResolvePatientAController();
      if (patient == null)
        yield break;

      patient.SetAssessActionEnabled("assess_pulse_r1", true);
      SetPatientAScenarioActionEnabled(patient, "click_to_start_comp", true);
      SetPatientAScenarioActionEnabled(patient, "start_ambu_r1", true);
      SetPatientAScenarioActionEnabled(patient, "interact_patient_chest", true);
      SetPatientAScenarioActionEnabled(patient, "remove_tpiece", true);
      yield break;
    }

    private IEnumerator Event_ActivatePatientAStyletRemoval()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
        SetPatientAScenarioActionEnabled(patient, "remove_intu_stylet", true);
      yield break;
    }

    private IEnumerator Event_ActivatePatientATpieceAttach()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
        SetPatientAScenarioActionEnabled(patient, "interact_tpiece", true);
      yield break;
    }

    private IEnumerator Event_ActivatePatientACpr2Actions()
    {
      var patient = ResolvePatientAController();
      if (patient == null)
        yield break;

      SetPatientAScenarioActionEnabled(patient, "interact_chest", true);
      SetPatientAScenarioActionEnabled(patient, "start_ambu_r2", true);
      yield break;
    }

    private IEnumerator Event_ActivatePatientAClothingRemoval()
    {
      var patient = ResolvePatientAController();
      if (patient != null)
        SetPatientAScenarioActionEnabled(patient, "remove_patient_clothing", true);
      yield break;
    }

    private static void SetPatientAScenarioActionEnabled(
      PatientController patient, string completionSignal, bool enabled)
    {
      if (patient == null || string.IsNullOrWhiteSpace(completionSignal))
        return;

      var actions = patient.GetComponentsInChildren<ScenarioActionInteractable>(true);
      foreach (var action in actions)
      {
        if (action != null && action.CompletionSignal == completionSignal)
          action.SetEnabled(enabled);
      }
    }
  }
}
