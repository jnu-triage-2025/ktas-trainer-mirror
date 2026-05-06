using UnityEngine;
using TriageTrainer.Patient;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    private PatientDisplayState _patientDisplayStateCache;

    private PatientDisplayState GetPatientDisplayState()
    {
      if (_patientDisplayStateCache != null)
        return _patientDisplayStateCache;

      _patientDisplayStateCache = GetComponent<PatientDisplayState>();
      if (_patientDisplayStateCache == null)
        _patientDisplayStateCache = GetComponentInChildren<PatientDisplayState>(true);

      return _patientDisplayStateCache;
    }
  }
}
