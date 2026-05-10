using System.Collections.Generic;
using FishNet.Object;
using TriageTrainer.Entity.Patient;
using UnityEngine;

namespace TriageTrainer.Entity
{
  public partial class PatientController
  {
    [Header("Medical Parameters")]
    [SerializeField] private PatientDescriptor _patientDescriptor = new PatientDescriptor();

    [Header("Medical State")]
    [SerializeField] private PatientMedicalState _medicalState = new PatientMedicalState();

    private readonly HashSet<IMedicalStateListener> _medicalStateListeners = new();

    public PatientDescriptor Descriptor => _patientDescriptor;

    public PatientMedicalState MedicalState => _medicalState;

    public BloodPressure MedicalStateBloodPressure
    {
      get => _medicalState.bloodPressure;
      set
      {
        _medicalState.bloodPressure = value;
        NotifyMedicalStateChanged();
      }
    }

    public BloodPulse MedicalStatePulse
    {
      get => _medicalState.pulse;
      set
      {
        _medicalState.pulse = value;
        NotifyMedicalStateChanged();
      }
    }

    public Skin MedicalStateSkin
    {
      get => _medicalState.skin;
      set
      {
        _medicalState.skin = value;
        NotifyMedicalStateChanged();
      }
    }

    public BodyTemperature MedicalStateBodyTemperature
    {
      get => _medicalState.bodyTemperature;
      set
      {
        _medicalState.bodyTemperature = value;
        NotifyMedicalStateChanged();
      }
    }

    public List<HealthProblem> MedicalStateHealthProblem => _medicalState.healthProblem;

    public Consciousness MedicalStateConsciousness
    {
      get => _medicalState.consciousness;
      set
      {
        _medicalState.consciousness = value;
        NotifyMedicalStateChanged();
      }
    }

    public Respiration MedicalStateRespiration
    {
      get => _medicalState.respiration;
      set
      {
        _medicalState.respiration = value;
        NotifyMedicalStateChanged();
      }
    }

    public List<RequiredDrug> MedicalStateRequiredDrugs => _medicalState.requiredDrugs;

    public bool MedicalStateIsCardiacArrest
    {
      get => _medicalState.isCardiacArrest;
      set
      {
        _medicalState.isCardiacArrest = value;
        NotifyMedicalStateChanged();
      }
    }

    public ECGParameters MedicalStateECG
    {
      get => _medicalState.ecg;
      set
      {
        _medicalState.ecg = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public ARTParameters MedicalStateART
    {
      get => _medicalState.art;
      set
      {
        _medicalState.art = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public CVPParameters MedicalStateCVP
    {
      get => _medicalState.cvp;
      set
      {
        _medicalState.cvp = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public PlethParameters MedicalStatePleth
    {
      get => _medicalState.pleth;
      set
      {
        _medicalState.pleth = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public NumericsParameters MedicalStateNumerics
    {
      get => _medicalState.numerics;
      set
      {
        _medicalState.numerics = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public NIBPParameters MedicalStateNIBP
    {
      get => _medicalState.nibp;
      set
      {
        _medicalState.nibp = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public TemperatureParameters MedicalStateTemperature
    {
      get => _medicalState.temperature;
      set
      {
        _medicalState.temperature = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public STLeadValues MedicalStateSTLeads
    {
      get => _medicalState.stLeads;
      set
      {
        _medicalState.stLeads = value;
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    public interface IMedicalStateListener
    {
      void HandleMedicalStateChanged(PatientMedicalState state);
    }

    public void RegisterMedicalStateListener(IMedicalStateListener listener)
    {
      if (listener == null)
        return;

      _medicalStateListeners.Add(listener);
    }

    public void UnregisterMedicalStateListener(IMedicalStateListener listener)
    {
      if (listener == null)
        return;

      _medicalStateListeners.Remove(listener);
    }

    public void MarkMedicalStateDirty()
    {
      NotifyMedicalStateChanged();
    }

    public void SetMonitorMedicalState(
      ECGParameters ecg,
      ARTParameters art,
      CVPParameters cvp,
      PlethParameters pleth,
      NumericsParameters numerics,
      NIBPParameters nibp,
      TemperatureParameters temperature,
      STLeadValues stLeads,
      bool notify = true)
    {
      _medicalState.ecg = ecg;
      _medicalState.art = art;
      _medicalState.cvp = cvp;
      _medicalState.pleth = pleth;
      _medicalState.numerics = numerics;
      _medicalState.nibp = nibp;
      _medicalState.temperature = temperature;
      _medicalState.stLeads = stLeads;

      if (notify)
      {
        NotifyAndSyncMonitorMedicalStateChanged();
      }
    }

    private void NotifyAndSyncMonitorMedicalStateChanged()
    {
      NotifyMedicalStateChanged();

      if (IsServerStarted)
      {
        RpcSyncMonitorMedicalState(
          _medicalState.ecg,
          _medicalState.art,
          _medicalState.cvp,
          _medicalState.pleth,
          _medicalState.numerics,
          _medicalState.nibp,
          _medicalState.temperature,
          _medicalState.stLeads);
        return;
      }

      if (IsClientInitialized)
      {
        CmdSetMonitorMedicalState(
          _medicalState.ecg,
          _medicalState.art,
          _medicalState.cvp,
          _medicalState.pleth,
          _medicalState.numerics,
          _medicalState.nibp,
          _medicalState.temperature,
          _medicalState.stLeads);
      }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CmdSetMonitorMedicalState(
      ECGParameters ecg,
      ARTParameters art,
      CVPParameters cvp,
      PlethParameters pleth,
      NumericsParameters numerics,
      NIBPParameters nibp,
      TemperatureParameters temperature,
      STLeadValues stLeads)
    {
      SetMonitorMedicalState(ecg, art, cvp, pleth, numerics, nibp, temperature, stLeads, notify: false);
      NotifyAndSyncMonitorMedicalStateChanged();
    }

    [ObserversRpc(BufferLast = true)]
    private void RpcSyncMonitorMedicalState(
      ECGParameters ecg,
      ARTParameters art,
      CVPParameters cvp,
      PlethParameters pleth,
      NumericsParameters numerics,
      NIBPParameters nibp,
      TemperatureParameters temperature,
      STLeadValues stLeads)
    {
      if (IsServerStarted)
        return;

      SetMonitorMedicalState(ecg, art, cvp, pleth, numerics, nibp, temperature, stLeads, notify: false);
      NotifyMedicalStateChanged();
    }

    private void NotifyMedicalStateChanged()
    {
      if (_medicalStateListeners.Count == 0)
        return;

      var snapshot = _medicalState;
      foreach (var listener in _medicalStateListeners)
      {
        listener?.HandleMedicalStateChanged(snapshot);
      }
    }

    private void EnsureMedicalStateDefaults()
    {
      if (_patientDescriptor == null)
        _patientDescriptor = new PatientDescriptor();

      if (_medicalState == null)
        _medicalState = new PatientMedicalState();

      if (_medicalState.healthProblem == null)
        _medicalState.healthProblem = new List<HealthProblem>();

      if (_medicalState.requiredDrugs == null)
        _medicalState.requiredDrugs = new List<RequiredDrug>();

      if (_medicalState.skin == null)
        _medicalState.skin = Skin.Default;

      if (_medicalState.consciousness == null)
        _medicalState.consciousness = Consciousness.Default;
    }
  }
}
