using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController
  {
    [Header("Monitoring")]
    [SerializeField] private PatientController _monitoringPatient;

    private PatientController _subscribedPatient;

    public PatientController MonitoringPatient => _monitoringPatient;

    public void SetMonitoringPatient(PatientController patient)
    {
      if (ReferenceEquals(_monitoringPatient, patient))
        return;

      _monitoringPatient = patient;
      ResolvePatientStateIfNeeded();
      PullParametersFromPatientState();
    }

    private void ResolvePatientStateIfNeeded()
    {
      var previousPatient = patientState;

      if (_monitoringPatient != null)
        patientState = _monitoringPatient;
      else
        patientState = GetComponentInParent<PatientController>();

      UpdateMedicalStateSubscription(patientState);

      if (!ReferenceEquals(previousPatient, patientState))
        PullParametersFromPatientState();
    }

    private void UpdateMedicalStateSubscription(PatientController nextPatient)
    {
      if (ReferenceEquals(_subscribedPatient, nextPatient))
        return;

      if (_subscribedPatient != null)
      {
        _subscribedPatient.UnregisterMedicalStateListener(this);
      }

      _subscribedPatient = nextPatient;

      if (_subscribedPatient != null)
      {
        _subscribedPatient.RegisterMedicalStateListener(this);
      }
    }

    private void UnregisterMedicalStateSubscription()
    {
      if (_subscribedPatient == null)
        return;

      _subscribedPatient.UnregisterMedicalStateListener(this);
      _subscribedPatient = null;
    }
  }
}
