using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_MovePatientB()
    {
      Register("move_patient_b", Event_MovePatientB);
    }

    private IEnumerator Event_MovePatientB()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientBTreatmentBedObject, true);

      if (_autoAttachPatientBToTreatmentBed)
      {
        TryAttachPatientToBed(_patientBObject, _patientBTreatmentBedObject, "patientB-treatment");
      }

      var moveTarget = _patientBTreatmentBedObject != null ? _patientBTreatmentBedObject : _patientBObject;
      if (_patientBTreatmentMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(moveTarget, _patientBTreatmentRoomPoint, _patientBTreatmentMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(moveTarget, _patientBTreatmentRoomPoint);
      }

      EmitSystemMessage("환자 B를 처치 구역으로 이동시켰습니다.");
      yield break;
    }
  }
}
