using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_MovePatientAToTreatmentRoom()
    {
      Register("move_patientA_to_treatmentroom", Event_MovePatientAToTreatmentRoom);
    }

    private IEnumerator Event_MovePatientAToTreatmentRoom()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientATreatmentBedObject, true);

      if (_autoAttachPatientAToTreatmentBed)
      {
        TryAttachPatientToBed(_patientAObject, _patientATreatmentBedObject, "patientA-treatment");
      }

      var moveTarget = _patientATreatmentBedObject != null
        ? _patientATreatmentBedObject
        : (_patientABedObject != null ? _patientABedObject : _patientAObject);

      if (_patientATreatmentMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(moveTarget, _patientATreatmentRoomPoint, _patientATreatmentMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(moveTarget, _patientATreatmentRoomPoint);
      }

      EmitSystemMessage("환자 A를 처치 구역으로 이동시켰습니다.");
      yield break;
    }
  }
}
