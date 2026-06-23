using System.Collections;
using UnityEngine;

namespace TriageTrainer.Scenario
{
  public partial class TriageScenarioEventBootstrap
  {
    private void RegisterEvent_MovePatientsToCt()
    {
      Register("move_patients_to_ct", Event_MovePatientsToCt);
    }

    private IEnumerator Event_MovePatientsToCt()
    {
      ResolveRuntimeReferencesIfNeeded();

      var patientBMoveTarget = _patientBTreatmentBedObject != null ? _patientBTreatmentBedObject : _patientBObject;
      var patientCMoveTarget = _patientCTreatmentBedObject != null ? _patientCTreatmentBedObject : _patientCObject;

      if (_patientsToCtMoveDurationSeconds > 0f)
      {
        yield return MoveToIfPresent(patientBMoveTarget, _patientBCtRoomPoint, _patientsToCtMoveDurationSeconds);
        yield return MoveToIfPresent(patientCMoveTarget, _patientCCtRoomPoint, _patientsToCtMoveDurationSeconds);
      }
      else
      {
        SnapToIfPresent(patientBMoveTarget, _patientBCtRoomPoint);
        SnapToIfPresent(patientCMoveTarget, _patientCCtRoomPoint);
      }

      if (_ctTransferFadePanel != null)
      {
        _ctTransferFadePanel.SetActive(true);
        if (_ctTransferFadeHoldSeconds > 0f)
        {
          yield return new WaitForSeconds(_ctTransferFadeHoldSeconds);
        }

        if (_ctTransferFadeAutoHideSeconds > 0f)
        {
          yield return new WaitForSeconds(_ctTransferFadeAutoHideSeconds);
          _ctTransferFadePanel.SetActive(false);
        }
      }

      EmitSystemMessage("환자 B/C를 CT 구역으로 이동시켰습니다.");
      yield break;
    }
  }
}
