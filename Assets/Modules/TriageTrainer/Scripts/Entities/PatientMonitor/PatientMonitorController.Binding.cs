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

      var previousPatient = _monitoringPatient;

      // 이전 환자에게 이 모니터가 더 이상 감시하지 않음을 알림
      if (previousPatient != null)
      {
        previousPatient.ClearMonitoringPatientMonitor(this);
      }

      _monitoringPatient = patient;

      // 새 환자에게 이 모니터가 감시 중임을 알림
      if (_monitoringPatient != null)
      {
        _monitoringPatient.SetMonitoringPatientMonitor(this);
      }

      ResolvePatientStateIfNeeded();
      PullParametersFromPatientState();
      UpdateTrackingLine();
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
