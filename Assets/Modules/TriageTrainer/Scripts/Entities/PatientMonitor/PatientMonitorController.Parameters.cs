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
    private bool _hasAppliedDisconnectedDisplayPolicy;
    private bool _hasCapturedDummyParameters;
    private ECGParameters _dummyECG;
    private ARTParameters _dummyART;
    private CVPParameters _dummyCVP;
    private PlethParameters _dummyPleth;
    private NumericsParameters _dummyNumerics;
    private NIBPParameters _dummyNIBP;
    private TemperatureParameters _dummyTemperature;
    private STLeadValues _dummySTLeads;

    private void PullParametersFromPatientState()
    {
      // patientState는 부모 오브젝트나 이전 구독에서 남아 있을 수 있다. 실제 연결 여부는
      // 모니터가 현재 감시 대상으로 갖는 환자로 판정해야 한다.
      if (_monitoringPatient == null || patientState?.Descriptor == null)
      {
        // 최초 미연결 상태와 연결 해제 전이에만 적용한다. 매 프레임 적용하면
        // 시나리오가 설정한 모니터 프로파일이 즉시 지워질 수 있다.
        if (_wasPatientBound || !_hasAppliedDisconnectedDisplayPolicy)
        {
          ResetMonitorParametersToDisconnected();
          _wasPatientBound = false;
          _hasAppliedDisconnectedDisplayPolicy = true;
        }
        return;
      }

      _wasPatientBound = true;
      _hasAppliedDisconnectedDisplayPolicy = false;
      ApplyMedicalState(patientState.MedicalState);
    }

    private void ResetMonitorParametersToDisconnected()
    {
      CaptureDummyParametersIfNeeded();

      if (_disconnectedPatientDisplayMode == DisconnectedPatientDisplayMode.PlayDummyValues)
      {
        monitorECG = _dummyECG;
        monitorART = _dummyART;
        monitorCVP = _dummyCVP;
        monitorPleth = _dummyPleth;
        monitorNumerics = _dummyNumerics;
        monitorNIBP = _dummyNIBP;
        monitorTemperature = _dummyTemperature;
        monitorSTLeads = _dummySTLeads;
      }
      else
      {
        SetUnavailableMonitorParameters();
      }

      _targetParameters = _disconnectedPatientDisplayMode == DisconnectedPatientDisplayMode.PlayDummyValues
        ? ResolveConfiguredParameters()
        : monitorECG;
      _currentParameters = _targetParameters;
      _transitionTimer = 0f;
      ecgNextBeatInterval = ComputeBaseInterval(_currentParameters.bpm);

      if (_disconnectedPatientDisplayMode == DisconnectedPatientDisplayMode.UnavailableValues)
        ResetGraphHistoryToDisconnectedValue();
    }

    private bool IsPatientMonitorDisconnected()
    {
      return _monitoringPatient == null || patientState?.Descriptor == null;
    }

    private bool IsDisplayingUnavailableValues()
    {
      return _disconnectedPatientDisplayMode == DisconnectedPatientDisplayMode.UnavailableValues &&
             IsPatientMonitorDisconnected();
    }

    private void SetUnavailableMonitorParameters()
    {
      const float unavailable = PatientMedicalState.MonitorValueUnavailable;
      monitorECG = new ECGParameters
      {
        bpm = unavailable,
        irregularity = unavailable,
        pAmp = unavailable,
        pWidth = unavailable,
        qAmp = unavailable,
        rAmp = unavailable,
        sAmp = unavailable,
        tAmp = unavailable,
        tWidth = unavailable,
        uAmp = unavailable,
        stElevation = unavailable,
        noise = unavailable,
        qrsWidthScale = unavailable
      };
      monitorART = new ARTParameters { bpm = unavailable, systolic = unavailable, diastolic = unavailable, noise = unavailable };
      monitorCVP = new CVPParameters { bpm = unavailable, mean = unavailable, noise = unavailable };
      monitorPleth = new PlethParameters { bpm = unavailable, spo2 = unavailable, noise = unavailable };
      monitorNumerics = new NumericsParameters
      {
        bpm = unavailable,
        pvcs = unavailable,
        pulseRate = unavailable,
        perfusionIndex = unavailable,
        spo2 = unavailable
      };
      monitorNIBP = new NIBPParameters { systolic = unavailable, diastolic = unavailable };
      monitorTemperature = new TemperatureParameters { t1 = unavailable, t2 = unavailable };
      monitorSTLeads = new STLeadValues
      {
        i = unavailable,
        ii = unavailable,
        iii = unavailable,
        avr = unavailable,
        avl = unavailable,
        avf = unavailable,
        v1 = unavailable,
        v2 = unavailable,
        v3 = unavailable,
        v4 = unavailable,
        v5 = unavailable,
        v6 = unavailable
      };
    }

    private void CaptureDummyParametersIfNeeded()
    {
      if (_hasCapturedDummyParameters)
        return;

      _hasCapturedDummyParameters = true;
      _dummyECG = monitorECG;
      _dummyART = monitorART;
      _dummyCVP = monitorCVP;
      _dummyPleth = monitorPleth;
      _dummyNumerics = monitorNumerics;
      _dummyNIBP = monitorNIBP;
      _dummyTemperature = monitorTemperature;
      _dummySTLeads = monitorSTLeads;
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
