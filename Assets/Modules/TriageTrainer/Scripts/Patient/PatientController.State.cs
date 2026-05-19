using UnityEngine;
using TriageTrainer.Patient;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private PatientDisplayState _patientDisplayStateCache;
    private PatientStateABC _patientStateCache;

    private PatientDisplayState GetPatientDisplayState()
    {
      if (_patientDisplayStateCache != null)
        return _patientDisplayStateCache;

      _patientDisplayStateCache = GetComponent<PatientDisplayState>();
      if (_patientDisplayStateCache == null)
        _patientDisplayStateCache = GetComponentInChildren<PatientDisplayState>(true);

      return _patientDisplayStateCache;
    }

    private PatientStateABC GetPatientState()
    {
      if (_patientStateCache != null)
        return _patientStateCache;

      _patientStateCache = GetComponent<PatientStateABC>();
      if (_patientStateCache == null)
        _patientStateCache = GetComponentInChildren<PatientStateABC>(true);

      return _patientStateCache;
    }

    public bool TryGetLayingOnMovingBedOffsets(out Vector3 modelLocalPosition, out Quaternion modelLocalRotation)
    {
      modelLocalPosition = Vector3.zero;
      modelLocalRotation = Quaternion.identity;

      var state = GetPatientState();
      if (state == null)
        return false;

      modelLocalPosition = state.PositionOnLayingOnPatientMovingBed;
      modelLocalRotation = Quaternion.Euler(state.EulerAnglesOnLayingOnPatientMovingBed);
      return true;
    }

    private bool TryGetLayingOnMovingBedCollider(
      out Vector3 center,
      out float height,
      out float radius,
      out int direction)
    {
      center = Vector3.zero;
      height = 1.8f;
      radius = 0.3f;
      direction = 2;

      var state = GetPatientState();
      if (state == null)
        return false;

      center = state.ColliderCenterOnLayingOnPatientMovingBed;
      height = state.ColliderHeightOnLayingOnPatientMovingBed;
      radius = state.ColliderRadiusOnLayingOnPatientMovingBed;
      direction = state.ColliderDirectionOnLayingOnPatientMovingBed;
      return true;
    }
  }
}
