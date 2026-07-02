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

    /// <summary>직전 프레임에 환자가 연결되어 있었는지(연결 해제 전이 감지용).</summary>
    private bool _wasPatientBound;

    private void PullParametersFromPatientState()
    {
      if (patientState?.Descriptor == null)
      {
        // 환자 미연결 상태에서는 "연결 해제 전이" 시점에만 1회 리셋한다.
        // 매 프레임 리셋하면 시나리오가 SetCustomParameters/SetRhythm 으로 설정한
        // 모니터 프로파일(심정지/ROSC 등)이 즉시 지워지는 버그가 발생한다.
        if (_wasPatientBound)
        {
          ResetMonitorParametersToDisconnected();
          _wasPatientBound = false;
        }
        return;
      }

      _wasPatientBound = true;
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
