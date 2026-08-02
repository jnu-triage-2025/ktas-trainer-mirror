using System.Collections;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterPatientBRecognitionEvents()
    {
      RegisterRecognition("activate_patient_b_recognition_1", "patient_b_recognition_1", true, "말 걸기");
      RegisterRecognition("activate_patient_b_recognition_2", "patient_b_recognition_2", true, "말 걸기");
      RegisterRecognition("activate_patient_b_recognition_3", "patient_b_recognition_3", true, "말 걸기");
      RegisterRecognition("activate_patient_b_recognition_4", "patient_b_recognition_4", false, "말 걸기");
      RegisterRecognition("activate_patient_b_strength_check", "patient_b_strength_checked", false, "근력 확인");
      RegisterRecognition("activate_patient_b_pupil_check", "patient_b_pupil_checked", false, "동공반사 확인");
      Register("reset_patient_b_c_triage_attempt", Event_ResetPatientBCTriageAttempt);
      Register("complete_patient_b_c_triage", Event_CompletePatientBCTriage);
    }

    private void RegisterRecognition(string eventIdentifier, string completionSignal, bool allowMicrophone, string displayText)
    {
      Register(eventIdentifier, () => Event_ActivatePatientBRecognition(completionSignal, allowMicrophone, displayText));
    }

    private IEnumerator Event_ActivatePatientBRecognition(string completionSignal, bool allowMicrophone, string displayText)
    {
      ResolveRuntimeReferencesIfNeeded();
      var patient = _patientBObject != null
        ? _patientBObject.GetComponentInChildren<PatientController>(true)
        : null;
      if (patient == null)
      {
        UnityEngine.Debug.LogError($"[TriageScenarioEventBootstrap] patient_b recognition target is missing ({completionSignal}).", this);
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
