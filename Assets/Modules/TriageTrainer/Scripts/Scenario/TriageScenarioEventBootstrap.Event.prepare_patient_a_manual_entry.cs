using System.Collections;
using MultiplayerInfrastructure.Logging;
using MultiplayerInfrastructure.Registry;
using TriageTrainer.Entity;
using TriageTrainer.Entity.Patient;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private bool _manualPatientASetupSucceeded;

    private void RegisterEvent_PreparePatientAManualEntry()
    {
      Register("prepare_patient_a_manual_scen_entry", Event_PreparePatientAManualScenEntry);
      Register("prepare_patient_a_manual_doc_inst", Event_PreparePatientAManualDocInst);
      Register("prepare_patient_a_manual_arrest", Event_PreparePatientAManualArrest);
      Register("prepare_patient_a_manual_cpr_1st_cycle", Event_PreparePatientAManualCpr1);
      Register("prepare_patient_a_manual_cpr_2nd_cycle", Event_PreparePatientAManualCpr2);
      Register("prepare_patient_a_manual_rosc_followup", Event_PreparePatientAManualRosc);
    }

    private IEnumerator Event_PreparePatientAManualScenEntry()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.ScenEntry);
    }

    private IEnumerator Event_PreparePatientAManualDocInst()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.DocInstruction);
    }

    private IEnumerator Event_PreparePatientAManualArrest()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.PreArrest);
    }

    private IEnumerator Event_PreparePatientAManualCpr1()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.Cpr1);
    }

    private IEnumerator Event_PreparePatientAManualCpr2()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.Cpr2);
    }

    private IEnumerator Event_PreparePatientAManualRosc()
    {
      yield return EnsurePatientAForManualEntry();
      if (!_manualPatientASetupSucceeded)
        yield break;
      ApplyManualStage(ManualPatientAStage.RoscFollowup);
    }

    private IEnumerator EnsurePatientAForManualEntry()
    {
      _manualPatientASetupSucceeded = false;
      ResolveRuntimeReferencesIfNeeded();
      if (_patientAObject == null)
      {
        Vector3 position = _patientATreatmentRoomPoint != null
          ? _patientATreatmentRoomPoint.position
          : Vector3.zero;
        if (!Registry.TrySpawnEntityPreset(
              "patient_a", position, Quaternion.identity, "patient_a",
              out var spawned, out _, out var error))
        {
          Debug.LogError($"[TriageScenarioEventBootstrap] Manual patient A setup failed: {error}", this);
          yield break;
        }

        _patientAObject = spawned;
        GameLogService.WriteScenario(
          "Patient A spawned for a manual scenario entry.",
          "patient_a_critical");
      }

      SetActiveIfPresent(_patientABedObject, true);
      SetActiveIfPresent(_patientATreatmentBedObject, true);
      var bed = _patientATreatmentBedObject != null ? _patientATreatmentBedObject : _patientABedObject;
      if (_autoAttachPatientAToTreatmentBed)
        TryAttachPatientToBed(_patientAObject, bed, "patientA-manual-entry");
      else
        SnapToIfPresent(_patientAObject, _patientATreatmentRoomPoint);

      EnsurePatientADoctorForManualEntry();
      _manualPatientASetupSucceeded = ResolvePatientAController() != null;
      if (!_manualPatientASetupSucceeded)
        Debug.LogError("[TriageScenarioEventBootstrap] Manual patient A setup completed without a PatientController.", this);
      yield break;
    }

    private void EnsurePatientADoctorForManualEntry()
    {
      const string doctorIdentifier = "npc-doctor-patient-a-critical";
      if (ResolveEntityObject(null, doctorIdentifier) != null)
        return;

      Vector3 position = FindWaypoint(PatientADoctorTreatroomEnteredAnchorId) != null
        ? FindWaypoint(PatientADoctorTreatroomEnteredAnchorId).transform.position
        : _patientATreatmentRoomPoint != null ? _patientATreatmentRoomPoint.position : Vector3.zero;
      if (!Registry.TrySpawnEntityPreset(
            "npc_doctor_preset", position, Quaternion.Euler(0f, 180f, 0f), doctorIdentifier,
            out _, out _, out var error))
      {
        Debug.LogError($"[TriageScenarioEventBootstrap] Manual doctor setup failed: {error}", this);
      }
    }

    private enum ManualPatientAStage
    {
      ScenEntry,
      DocInstruction,
      PreArrest,
      Cpr1,
      Cpr2,
      RoscFollowup
    }

    private void ApplyManualStage(ManualPatientAStage stage)
    {
      var patient = ResolvePatientAController();
      if (patient == null)
        return;

      ResetManualVisualState(patient);
      switch (stage)
      {
        case ManualPatientAStage.ScenEntry:
          ApplyManualTreatmentSnapshot(patient);
          return;
        case ManualPatientAStage.DocInstruction:
          ApplyInitialAssessmentState(patient);
          return;
        case ManualPatientAStage.PreArrest:
          ApplyPreArrestTreatmentState(patient);
          return;
        case ManualPatientAStage.Cpr1:
          ApplyPreArrestTreatmentState(patient);
          ApplyPeaState(patient);
          return;
        case ManualPatientAStage.Cpr2:
        case ManualPatientAStage.RoscFollowup:
          ApplyPreArrestTreatmentState(patient);
          ApplyCpr1TreatmentState(patient);
          ApplyCpr2TreatmentState(patient);
          ApplyPeaState(patient);
          return;
      }
    }

    private void ApplyInitialAssessmentState(PatientController patient)
    {
      ApplyDisplay(patient, PatientController.TreatmentDisplay.CervicalCollarOnNeck);
      ApplyManualTreatmentSnapshot(patient, "initial_assessment_complete");
    }

    private void ApplyPreArrestTreatmentState(PatientController patient)
    {
      ApplyInitialAssessmentState(patient);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.EndotrachealTubeInsertDone);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.Syringe18GInsertedIntoLeftArm);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.Syringe18GInsertedIntoRightArm);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.CentralVenousCatheterInsertedIntoSubclavian);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.GauzePatchedOnThorax);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.GauzeDressingDoneOnThorax);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, true);
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      SetActiveIfPresent(_patientA18gLeftVisual, true);
      SetActiveIfPresent(_patientANs1LeftConnectedVisual, true);
      SetActiveIfPresent(_patientA18gRightVisual, true);
      SetActiveIfPresent(_patientAPs1RightConnectedVisual, true);
      SetActiveIfPresent(_patientACentralLineVisual, true);
      SetActiveIfPresent(_level1ReadyVisual, true);
      ApplyManualTreatmentSnapshot(patient, "initial_assessment_complete", "oxygen_line_connected", "pre_arrest_treatment_complete");
    }

    private void ApplyCpr1TreatmentState(PatientController patient)
    {
      ApplyDisplay(patient, PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula);
      SetActiveIfPresent(_patientATPieceConnectedVisual, true);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.AmbuBagAttachedToEndotrachealTube);
      ApplyManualTreatmentSnapshot(patient, "initial_assessment_complete", "oxygen_line_connected", "pre_arrest_treatment_complete", "cpr_1_complete");
    }

    private void ApplyCpr2TreatmentState(PatientController patient)
    {
      // CPR 2주기부터는 T-piece가 분리되고 앰부백/패드와 1주기 처치가 완료된 상태다.
      patient.ApplyScenarioDisplayState(PatientController.TreatmentDisplay.TPieceAttachedToNasalCannula.ToString(), false);
      SetActiveIfPresent(_patientATPieceConnectedVisual, false);
      ApplyDisplay(patient, PatientController.TreatmentDisplay.AmbuBagAttachedToEndotrachealTube);
      SetActiveIfPresent(_patientAAmbuConnectedVisual, true);
      SetActiveIfPresent(_patientADefibrillatorPadVisual, true);
      patient.SetNamedChildActive("defibrillatorpad_midaxillary_A", true);
      patient.SetNamedChildActive("defibrillatorpad_subclavicle_A", true);
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
      ApplyManualTreatmentSnapshot(patient,
        "initial_assessment_complete", "oxygen_line_connected", "pre_arrest_treatment_complete",
        "cpr_1_complete", "cpr_2_complete", "defibrillator_pads_attached",
        "epinephrine_round_1_complete", "normal_saline_round_1_complete", "defibrillation_round_1_complete");
    }

    private void ApplyPeaState(PatientController patient)
    {
      float unavailable = TriageTrainer.Entity.Patient.PatientMedicalState.MonitorValueUnavailable;
      patient.MedicalStateIsCardiacArrest = true;
      patient.SetMonitorMedicalState(
        _patientACrashMonitorParameters,
        new ARTParameters { bpm = unavailable, systolic = unavailable, diastolic = unavailable, noise = 0f },
        new CVPParameters { bpm = unavailable, mean = unavailable, noise = 0f },
        new PlethParameters { bpm = unavailable, spo2 = unavailable, noise = 0f },
        new NumericsParameters { bpm = _patientACrashMonitorParameters.bpm, pvcs = unavailable, pulseRate = unavailable, perfusionIndex = unavailable, spo2 = unavailable },
        new NIBPParameters { systolic = unavailable, diastolic = unavailable },
        new TemperatureParameters { t1 = unavailable, t2 = unavailable },
        new STLeadValues { i = unavailable, ii = unavailable, iii = unavailable, avr = unavailable, avl = unavailable, avf = unavailable, v1 = unavailable, v2 = unavailable, v3 = unavailable, v4 = unavailable, v5 = unavailable, v6 = unavailable });
      GameLogService.WriteScenario(
        "Patient A medical state restored for manual entry: cardiac arrest (PEA).",
        "patient_a_critical");
    }

    private static void ApplyDisplay(PatientController patient, PatientController.TreatmentDisplay display)
    {
      patient.ApplyScenarioDisplayState(display.ToString(), true);
    }

    private void ResetManualVisualState(PatientController patient)
    {
      foreach (PatientController.TreatmentDisplay display in System.Enum.GetValues(typeof(PatientController.TreatmentDisplay)))
      {
        if (display != PatientController.TreatmentDisplay.None)
          patient.ApplyScenarioDisplayState(display.ToString(), false);
      }
      SetActiveIfPresent(_patientAEtTubePreparedVisual, false);
      SetActiveIfPresent(_patientAEtTubeInsertedVisual, false);
      SetActiveIfPresent(_patientAEtTubeWithoutStyletVisual, false);
      SetActiveIfPresent(_patientATPieceConnectedVisual, false);
      SetActiveIfPresent(_patientAGauzeVisual, false);
      SetActiveIfPresent(_patientAGauzeWithPlasterVisual, false);
      SetActiveIfPresent(_patientA18gLeftVisual, false);
      SetActiveIfPresent(_patientANs1LeftConnectedVisual, false);
      SetActiveIfPresent(_patientA18gRightVisual, false);
      SetActiveIfPresent(_patientAPs1RightConnectedVisual, false);
      SetActiveIfPresent(_patientACentralLineVisual, false);
      SetActiveIfPresent(_level1ReadyVisual, false);
      SetActiveIfPresent(_patientAAmbuConnectedVisual, false);
      SetActiveIfPresent(_patientADefibrillatorPadVisual, false);
      patient.SetNamedChildActive("defibrillatorpad_midaxillary_A", false);
      patient.SetNamedChildActive("defibrillatorpad_subclavicle_A", false);
      SetAnimatorsBool(_ambuBaggingAnimators, _ambuBaggingBoolName, false);
      SetAnimatorsBool(_chestCompressionAnimators, _chestCompressionBoolName, false);
    }

    private static void ApplyManualTreatmentSnapshot(PatientController patient, params string[] identifiers)
      => patient.SetTreatmentStateSnapshot(identifiers ?? System.Array.Empty<string>());
  }
}
