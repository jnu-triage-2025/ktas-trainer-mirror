using System.Collections;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterPatientBCRecognitionEvents()
    {
      RegisterRecognition("activate_patient_b_recognition_1", false, "patient_b_recognition_1", true, "말 걸기");
      RegisterRecognition("activate_patient_b_recognition_2", false, "patient_b_recognition_2", true, "말 걸기");
      RegisterRecognition("activate_patient_b_recognition_3", false, "patient_b_recognition_3", true, "말 걸기");
      RegisterRecognition("activate_patient_b_recognition_4", false, "patient_b_recognition_4", false, "말 걸기");
      RegisterRecognition("activate_patient_b_strength_check", false, "patient_b_strength_checked", false, "근력 확인");
      RegisterRecognition("activate_patient_b_pupil_check", false, "patient_b_pupil_checked", false, "동공반사 확인");
      RegisterRecognition("activate_patient_c_recognition_1", true, "patient_c_recognition_1", true, "말 걸기");
      RegisterRecognition("activate_patient_c_recognition_2", true, "patient_c_recognition_2", true, "말 걸기");
      RegisterRecognition("activate_patient_c_recognition_3", true, "patient_c_recognition_3", true, "말 걸기");
      RegisterRecognition("activate_patient_c_recognition_4", true, "patient_c_recognition_4", false, "말 걸기");
      RegisterRecognition("activate_patient_c_strength_check", true, "patient_c_strength_checked", false, "근력 확인");
      RegisterRecognition("activate_patient_c_pupil_check", true, "patient_c_pupil_checked", false, "동공반사 확인");
      Register("reset_patient_b_c_triage_attempt", Event_ResetPatientBCTriageAttempt);
      Register("complete_patient_b_c_triage", Event_CompletePatientBCTriage);
    }

    private void RegisterRecognition(
      string eventIdentifier,
      bool targetPatientC,
      string completionSignal,
      bool allowMicrophone,
      string displayText)
    {
      Register(eventIdentifier,
        () => Event_ActivatePatientRecognition(targetPatientC, completionSignal, allowMicrophone, displayText));
    }

    private IEnumerator Event_ActivatePatientRecognition(
      bool targetPatientC,
      string completionSignal,
      bool allowMicrophone,
      string displayText)
    {
      ResolveRuntimeReferencesIfNeeded();
      var target = targetPatientC ? _patientCObject : _patientBObject;
      var patient = target != null
        ? target.GetComponentInChildren<PatientController>(true)
        : null;
      if (patient == null)
      {
        string patientIdentifier = targetPatientC ? "patient_c" : "patient_b";
        UnityEngine.Debug.LogError(
          $"[TriageScenarioEventBootstrap] {patientIdentifier} recognition target is missing ({completionSignal}).",
          this);
        yield break;
      }

      patient.ActivateRecognitionCheck(completionSignal, allowMicrophone, displayText);
      yield break;
    }

    private IEnumerator Event_ResetPatientBCTriageAttempt()
    {
      ResolveRuntimeReferencesIfNeeded();
      string[] signals =
      {
        "triage_submitted_patient_b",
        "triage_submitted_patient_c",
        "triage_submitted_patient_dummy_d_b",
        "triage_correct_patient_b",
        "triage_correct_patient_c",
        "triage_correct_patient_dummy_d_b"
      };
      foreach (string signal in signals)
        ScenarioInteractionSignals.Clear(signal);

      SetPatientTriageAssessable(_patientBObject, true);
      SetPatientTriageAssessable(_patientCObject, true);
      SetPatientTriageAssessable(_patientDummyDBObject, true);
      yield break;
    }

    private IEnumerator Event_CompletePatientBCTriage()
    {
      ScenarioInteractionSignals.Raise("patient_b_c_triage_correct");
      yield break;
    }

    private static void SetPatientTriageAssessable(UnityEngine.GameObject target, bool assessable)
    {
      target?.GetComponentInChildren<PatientController>(true)?.SetTriageAssessable(assessable);
    }
  }
}
