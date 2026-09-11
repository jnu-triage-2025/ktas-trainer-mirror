using System.Collections;
using MultiplayerInfrastructure.Scenario;
using TriageTrainer.Entity;
using TriageTrainer.Entity.Patient;

namespace TriageTrainer.Scenario
{
  /// <summary>시나리오 이벤트 하나가 여는 의식 확인 항목의 정의.</summary>
  public readonly struct PatientRecognitionActivation
  {
    public readonly string EventIdentifier;
    public readonly bool TargetPatientC;
    public readonly string CompletionSignal;
    public readonly bool AllowMicrophone;
    public readonly string DisplayText;

    /// <summary>이 항목을 수행할 역할 태그. 비워 두면 역할을 제한하지 않는다.</summary>
    public readonly string RequiredRoleTag;

    public PatientRecognitionActivation(
      string eventIdentifier,
      bool targetPatientC,
      string completionSignal,
      bool allowMicrophone,
      string displayText,
      string requiredRoleTag)
    {
      EventIdentifier = eventIdentifier;
      TargetPatientC = targetPatientC;
      CompletionSignal = completionSignal;
      AllowMicrophone = allowMicrophone;
      DisplayText = displayText;
      RequiredRoleTag = requiredRoleTag;
    }
  }

  public partial class TriageScenarioEventBootstrap
  {
    private const string RecognitionRoleNurseA = "nurse_a";
    private const string RecognitionRoleNurseB = "nurse_b";

    /// <summary>
    /// 환자 B/C 의식 확인 항목의 활성화 정의. 역할 태그는 patient_b_c_ct 그래프의 P_B_CARE /
    /// P_C_CARE 분기 역할과 같아야 한다. 환자 B의 의식·근력·동공반사는 nurse_a가 담당하고,
    /// 환자 C의 의식·근력·동공반사는 nurse_b가 담당한다.
    /// </summary>
    internal static readonly PatientRecognitionActivation[] PatientBCRecognitionActivations =
    {
      new("activate_patient_b_recognition_1", false, "patient_b_recognition_1", true, "말 걸기", RecognitionRoleNurseA),
      new("activate_patient_b_recognition_2", false, "patient_b_recognition_2", true, "계속해서 말 걸기", RecognitionRoleNurseA),
      new("activate_patient_b_recognition_3", false, "patient_b_recognition_3", true, "계속해서 말 걸기", RecognitionRoleNurseA),
      new("activate_patient_b_recognition_4", false, "patient_b_recognition_4", false, "계속해서 말 걸기", RecognitionRoleNurseA),
      new("activate_patient_b_strength_check", false, "patient_b_strength_checked", false, "근력 확인", RecognitionRoleNurseA),
      new("activate_patient_b_pupil_check", false, "patient_b_pupil_checked", false, "동공반사 확인", RecognitionRoleNurseA),
      new("activate_patient_c_recognition_1", true, "patient_c_recognition_1", true, "말 걸기", RecognitionRoleNurseB),
      new("activate_patient_c_recognition_2", true, "patient_c_recognition_2", true, "말 걸기", RecognitionRoleNurseB),
      new("activate_patient_c_recognition_3", true, "patient_c_recognition_3", true, "말 걸기", RecognitionRoleNurseB),
      new("activate_patient_c_recognition_4", true, "patient_c_recognition_4", false, "말 걸기", RecognitionRoleNurseB),
      new("activate_patient_c_strength_check", true, "patient_c_strength_checked", false, "근력 확인", RecognitionRoleNurseB),
      new("activate_patient_c_pupil_check", true, "patient_c_pupil_checked", false, "동공반사 확인", RecognitionRoleNurseB),
    };

    private void RegisterPatientBCRecognitionEvents()
    {
      for (int i = 0; i < PatientBCRecognitionActivations.Length; i++)
        RegisterRecognition(PatientBCRecognitionActivations[i]);
      Register("evaluate_patient_b_c_triage", Event_EvaluatePatientBCTriage);
      Register("reset_patient_b_c_triage_attempt", Event_ResetPatientBCTriageAttempt);
      Register("reset_patient_b_triage_attempt", () => Event_ResetPatientBCTriageAttempt("patient_b"));
      Register("reset_patient_c_triage_attempt", () => Event_ResetPatientBCTriageAttempt("patient_c"));
      Register("reset_patient_dummy_d_b_triage_attempt", () => Event_ResetPatientBCTriageAttempt("patient_dummy_d_b"));
      Register("complete_patient_b_c_triage", Event_CompletePatientBCTriage);
    }

    private void RegisterRecognition(PatientRecognitionActivation activation)
    {
      Register(activation.EventIdentifier, () => Event_ActivatePatientRecognition(activation));
    }

    private IEnumerator Event_ActivatePatientRecognition(PatientRecognitionActivation activation)
    {
      ResolveRuntimeReferencesIfNeeded();
      string patientIdentifier = activation.TargetPatientC ? "patient_c" : "patient_b";

      // 시나리오가 보관한 프리팹 참조는 네트워크 스폰 뒤에도 남아 있을 수 있다. 인식 상태는
      // 실제로 레지스트리에 등록된 NetworkObject의 SyncList에 기록해야 하므로 그 인스턴스를
      // 우선 해석한다.
      PatientController patient = null;
      if (MultiplayerInfrastructure.Registry.Registry.TryGetEntity(patientIdentifier, out var descriptor)
          && descriptor?.GameObject != null)
        patient = descriptor.GameObject.GetComponentInChildren<PatientController>(true);

      var target = activation.TargetPatientC ? _patientCObject : _patientBObject;
      if (patient == null && target != null)
        patient = target.GetComponentInChildren<PatientController>(true);
      if (patient == null)
      {
        UnityEngine.Debug.LogError(
          $"[TriageScenarioEventBootstrap] {patientIdentifier} recognition target is missing ({activation.CompletionSignal}).",
          this);
        yield break;
      }

      patient.ActivateRecognitionCheck(
        activation.CompletionSignal,
        activation.AllowMicrophone,
        activation.RequiredRoleTag,
        activation.DisplayText);
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
        "triage_correct_patient_dummy_d_b",
        "patient_b_c_triage_current_correct"
      };
      foreach (string signal in signals)
        ScenarioInteractionSignals.Clear(signal);

      SetPatientTriageAssessable(_patientBObject, true);
      SetPatientTriageAssessable(_patientCObject, true);
      SetPatientTriageAssessable(_patientDummyDBObject, true);
      yield break;
    }

    private IEnumerator Event_ResetPatientBCTriageAttempt(string patientIdentifier)
    {
      ResolveRuntimeReferencesIfNeeded();
      ScenarioInteractionSignals.Clear($"triage_submitted_{patientIdentifier}");
      ScenarioInteractionSignals.Clear($"triage_correct_{patientIdentifier}");

      var target = patientIdentifier switch
      {
        "patient_b" => _patientBObject,
        "patient_c" => _patientCObject,
        "patient_dummy_d_b" => _patientDummyDBObject,
        _ => null
      };
      target?.GetComponentInChildren<PatientController>(true)?.ResetTriageAssessmentForRetry();
      yield break;
    }

    private IEnumerator Event_EvaluatePatientBCTriage()
    {
      ResolveRuntimeReferencesIfNeeded();
      ScenarioInteractionSignals.Clear("patient_b_c_triage_current_correct");

      if (ArePatientBCTriageAssignmentsCorrect(
            _patientBObject,
            _patientCObject,
            _patientDummyDBObject))
      {
        ScenarioInteractionSignals.Raise("patient_b_c_triage_current_correct");
      }

      yield break;
    }

    private static bool ArePatientBCTriageAssignmentsCorrect(params UnityEngine.GameObject[] targets)
    {
      foreach (var target in targets)
      {
        var patient = target != null
          ? target.GetComponentInChildren<PatientController>(true)
          : null;
        if (patient == null
            || patient.AssessedTriage == TriageLevel.Unassessed
            || patient.AssessedTriage != patient.IntendedTriage)
        {
          return false;
        }
      }

      return true;
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
