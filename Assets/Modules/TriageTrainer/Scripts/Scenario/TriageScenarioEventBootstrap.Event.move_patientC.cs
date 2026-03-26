using System.Collections;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_MovePatientC()
    {
      Register("move_patientC", Event_MovePatientC);
    }

    private IEnumerator Event_MovePatientC()
    {
      ResolveRuntimeReferencesIfNeeded();

      SetActiveIfPresent(_patientCTreatmentBedObject, true);

      if (_autoAttachPatientCToTreatmentBed)
      {
        TryAttachPatientToBed(_patientCObject, _patientCTreatmentBedObject, "patientC-treatment");
      }

      var moveTarget = _patientCTreatmentBedObject != null ? _patientCTreatmentBedObject : _patientCObject;
      if (_patientCTreatmentMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(moveTarget, _patientCTreatmentRoomPoint, _patientCTreatmentMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(moveTarget, _patientCTreatmentRoomPoint);
      }

      EmitSystemMessage("환자 C를 처치 구역으로 이동시켰습니다.");
      yield break;
    }
  }
}
