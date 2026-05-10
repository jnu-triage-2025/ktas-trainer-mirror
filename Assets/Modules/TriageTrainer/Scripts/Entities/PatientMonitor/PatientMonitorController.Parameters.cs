using TriageTrainer.Entity.Patient;
using UnityEngine;

namespace TriageTrainer.Entity.PatientMonitor.Models
{
  public partial class PatientMonitorController
  {
    [SerializeField] private ECGParameters monitorECG = ECGParameters.Normal;
    [SerializeField] private ARTParameters monitorART = ARTParameters.Default;
    [SerializeField] private CVPParameters monitorCVP = CVPParameters.Default;
    [SerializeField] private PlethParameters monitorPleth = PlethParameters.Default;
    [SerializeField] private NumericsParameters monitorNumerics = NumericsParameters.Default;
    [SerializeField] private NIBPParameters monitorNIBP = NIBPParameters.Default;
    [SerializeField] private TemperatureParameters monitorTemperature = TemperatureParameters.Default;
    [SerializeField] private STLeadValues monitorSTLeads = STLeadValues.Default;

    private ECGParameters ResolveConfiguredParameters()
    {
      return displayMode == ECGDisplayMode.Preset ? ECGParameters.FromRhythm(rhythmPreset) : monitorECG;
    }

    private void PullParametersFromPatientState()
    {
      if (patientState?.Descriptor == null)
      {
        ResetMonitorParametersToDisconnected();
        return;
      }

      ApplyMedicalState(patientState.MedicalState);
    }

    private void ResetMonitorParametersToDisconnected()
    {
      monitorECG = default;
      monitorART = default;
      monitorCVP = default;
      monitorPleth = default;
      monitorNumerics = default;
      monitorNIBP = default;
      monitorTemperature = default;
      monitorSTLeads = default;

      _targetParameters = monitorECG;
      _currentParameters = monitorECG;
      _transitionTimer = 0f;
      ecgNextBeatInterval = ComputeBaseInterval(_currentParameters.bpm);
    }

    private void PushParametersToPatientState()
    {
      if (patientState?.Descriptor == null)
        return;

      patientState.SetMonitorMedicalState(
        monitorECG,
        monitorART,
        monitorCVP,
        monitorPleth,
        monitorNumerics,
        monitorNIBP,
        monitorTemperature,
        monitorSTLeads);
    }

    public void HandleMedicalStateChanged(PatientMedicalState state)
    {
      ApplyMedicalState(state);
    }

    private void ApplyMedicalState(PatientMedicalState state)
    {
      if (state == null)
      {
        ResetMonitorParametersToDisconnected();
        return;
      }

      monitorECG = state.ecg;
      monitorART = state.art;
      monitorCVP = state.cvp;
      monitorPleth = state.pleth;
      monitorNumerics = state.numerics;
      monitorNIBP = state.nibp;
      monitorTemperature = state.temperature;
      monitorSTLeads = state.stLeads;

      _targetParameters = monitorECG;
      if (_transitionTimer <= 0f)
      {
        _currentParameters = _targetParameters;
        ecgNextBeatInterval = ComputeBaseInterval(_currentParameters.bpm);
      }
    }

    public void SetARTParameters(ARTParameters parameters)
    {
      monitorART = parameters;
      PushParametersToPatientState();
    }

    public void SetCVPParameters(CVPParameters parameters)
    {
      monitorCVP = parameters;
      PushParametersToPatientState();
    }

    public void SetPlethParameters(PlethParameters parameters)
    {
      monitorPleth = parameters;
      PushParametersToPatientState();
    }

    public void SetNumericsParameters(NumericsParameters parameters)
    {
      monitorNumerics = parameters;
      PushParametersToPatientState();
    }

    public void SetNIBPParameters(NIBPParameters parameters)
    {
      monitorNIBP = parameters;
      PushParametersToPatientState();
    }

    public void SetTemperatureParameters(TemperatureParameters parameters)
    {
      monitorTemperature = parameters;
      PushParametersToPatientState();
    }

    public void SetSTLeadValues(STLeadValues values)
    {
      monitorSTLeads = values;
      PushParametersToPatientState();
    }
  }
}
