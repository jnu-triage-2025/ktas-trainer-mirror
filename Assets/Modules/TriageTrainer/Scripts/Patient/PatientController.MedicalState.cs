using System.Collections.Generic;
using FishNet.Object;
using MultiplayerInfrastructure.Scenario;
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

    /// <summary>
    /// <see cref="ScenarioPatientMedicalStatePresetNode"/> 의 내용을 이 환자 컨트롤러에 적용하고,
    /// 서버 컨텍스트인 경우 변경 내용을 RPC로 모든 클라이언트에 전파한다.
    ///
    /// <para>null 인 항목은 현재 값을 유지하고, 비어 있지 않은 항목만 덮어쓴다.
    /// 이 메서드는 반드시 서버(또는 오프라인) 컨텍스트에서 호출해야 한다.
    /// (<see cref="ScenarioController"/>의 <c>ExecutePatientMedicalStatePresetNode</c>가 서버 전용 가드를 적용한다.)</para>
    ///
    /// <para>새 PatientDescriptor / PatientMedicalState 필드가 추가될 때
    /// <see cref="ScenarioPatientMedicalStatePresetNode"/> 에 프로퍼티를 추가하고,
    /// 이 메서드와 <see cref="RpcSyncVitalMedicalState"/> 두 곳에도 동일하게 추가한다:</para>
    /// <code>
    /// if (preset.NewField.HasValue) _patientDescriptor.NewField = preset.NewField.Value;
    /// // RpcSyncVitalMedicalState 파라미터에도 추가:
    /// int newField = preset.NewField ?? PresetSentinelNone
    /// </code>
    /// </summary>
    public void ApplyMedicalStatePreset(ScenarioPatientMedicalStatePresetNode preset)
    {
      if (preset == null)
        return;

      EnsureMedicalStateDefaults();

      // ── 환자 기술자(PatientDescriptor) ──
      // 새 PatientDescriptor 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.Name != null)
        _patientDescriptor.name = preset.Name;

      if (preset.Sex.HasValue)
        _patientDescriptor.sex = preset.Sex.Value;

      if (preset.Age.HasValue)
        _patientDescriptor.age = preset.Age.Value;

      if (preset.BloodType.HasValue)
        _patientDescriptor.bloodType = preset.BloodType.Value;

      if (preset.IntendedTriage.HasValue)
        _patientDescriptor.intendedTriage = preset.IntendedTriage.Value;

      // ── 의식(Consciousness) ──
      // 새 Consciousness 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.ConsciousnessGcs.HasValue
          || preset.ConsciousnessLocLabel.HasValue
          || preset.ConsciousnessPupillaryResponse.HasValue)
      {
        if (_medicalState.consciousness == null)
          _medicalState.consciousness = Consciousness.Default;

        if (preset.ConsciousnessGcs.HasValue)
          _medicalState.consciousness.gcs = preset.ConsciousnessGcs.Value;

        if (preset.ConsciousnessLocLabel.HasValue)
          _medicalState.consciousness.locLabel = preset.ConsciousnessLocLabel.Value;

        if (preset.ConsciousnessPupillaryResponse.HasValue)
          _medicalState.consciousness.pupillaryResponse = preset.ConsciousnessPupillaryResponse.Value;
      }

      // ── 호흡(Respiration) ──
      // 새 Respiration 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.RespirationAwRR.HasValue || preset.RespirationTypeValue.HasValue)
      {
        if (_medicalState.respiration == null)
          _medicalState.respiration = new Respiration();

        if (preset.RespirationAwRR.HasValue)
          _medicalState.respiration.awRR = preset.RespirationAwRR.Value;

        if (preset.RespirationTypeValue.HasValue)
          _medicalState.respiration.type = preset.RespirationTypeValue.Value;
      }

      // ── 맥박(BloodPulse) ──
      // 새 BloodPulse 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.PulseRate.HasValue || preset.PulseForceType.HasValue)
      {
        if (_medicalState.pulse == null)
          _medicalState.pulse = new BloodPulse();

        if (preset.PulseRate.HasValue)
          _medicalState.pulse.rate = preset.PulseRate.Value;

        if (preset.PulseForceType.HasValue)
          _medicalState.pulse.forceType = preset.PulseForceType.Value;
      }

      // ── 혈압(BloodPressure) ──
      // 새 BloodPressure 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.BloodPressureSystolic.HasValue || preset.BloodPressureDiastolic.HasValue)
      {
        if (_medicalState.bloodPressure == null)
          _medicalState.bloodPressure = new BloodPressure();

        if (preset.BloodPressureSystolic.HasValue)
          _medicalState.bloodPressure.systolic = preset.BloodPressureSystolic.Value;

        if (preset.BloodPressureDiastolic.HasValue)
          _medicalState.bloodPressure.diastolic = preset.BloodPressureDiastolic.Value;
      }

      // ── 피부(Skin) ──
      // 새 Skin 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.SkinColorHue.HasValue || preset.SkinTemperatureType.HasValue)
      {
        if (_medicalState.skin == null)
          _medicalState.skin = Skin.Default;

        if (preset.SkinColorHue.HasValue)
          _medicalState.skin.colorHue = preset.SkinColorHue.Value;

        if (preset.SkinTemperatureType.HasValue)
          _medicalState.skin.temperatureType = preset.SkinTemperatureType.Value;
      }

      // ── 기타 의료 상태 ──
      // 새 PatientMedicalState 필드 추가 시 아래에 동일 패턴으로 추가한다.

      if (preset.IsCardiacArrest.HasValue)
        _medicalState.isCardiacArrest = preset.IsCardiacArrest.Value;

      NotifyMedicalStateChanged();

      // 서버 컨텍스트이면 변경된 vital 필드를 모든 클라이언트에 전파한다.
      // BufferLast 옵션으로, 늦게 참여한 클라이언트도 마지막 프리셋 상태를 수신한다.
      // 새 필드 추가 시 RpcSyncVitalMedicalState 에도 동일하게 파라미터를 추가한다.
      if (IsServerStarted)
      {
        RpcSyncVitalMedicalState(
          name: preset.Name,
          sex: preset.Sex.HasValue ? (int)preset.Sex.Value : PresetSentinelNone,
          age: preset.Age ?? PresetSentinelNone,
          bloodType: preset.BloodType.HasValue ? (int)preset.BloodType.Value : PresetSentinelNone,
          intendedTriage: preset.IntendedTriage.HasValue ? (int)preset.IntendedTriage.Value : PresetSentinelNone,
          consciousnessGcs: preset.ConsciousnessGcs ?? PresetSentinelNone,
          consciousnessLocLabel: preset.ConsciousnessLocLabel.HasValue ? (int)preset.ConsciousnessLocLabel.Value : PresetSentinelNone,
          consciousnessPupillaryResponse: preset.ConsciousnessPupillaryResponse.HasValue ? (int)preset.ConsciousnessPupillaryResponse.Value : PresetSentinelNone,
          respirationAwRR: preset.RespirationAwRR ?? PresetSentinelNone,
          respirationTypeValue: preset.RespirationTypeValue.HasValue ? (int)preset.RespirationTypeValue.Value : PresetSentinelNone,
          pulseRate: preset.PulseRate ?? PresetSentinelNone,
          pulseForceType: preset.PulseForceType.HasValue ? (int)preset.PulseForceType.Value : PresetSentinelNone,
          bloodPressureSystolic: preset.BloodPressureSystolic ?? PresetSentinelNone,
          bloodPressureDiastolic: preset.BloodPressureDiastolic ?? PresetSentinelNone,
          skinColorHue: preset.SkinColorHue.HasValue ? (int)preset.SkinColorHue.Value : PresetSentinelNone,
          skinTemperatureType: preset.SkinTemperatureType.HasValue ? (int)preset.SkinTemperatureType.Value : PresetSentinelNone,
          isCardiacArrest: preset.IsCardiacArrest.HasValue ? (preset.IsCardiacArrest.Value ? 1 : 0) : PresetSentinelNone
        );
      }
    }

    /// <summary>
    /// 프리셋 RPC 파라미터에서 "값 없음(null)"을 나타내는 센티넬 값.
    /// RPC 파라미터는 nullable을 지원하지 않으므로 int로 인코딩한다.
    /// 이 값은 어떤 유효한 열거형 값이나 의료 수치와도 충돌하지 않도록 int.MinValue를 사용한다.
    /// </summary>
    private const int PresetSentinelNone = int.MinValue;

    /// <summary>
    /// 서버가 적용한 프리셋 vital 필드를 모든 클라이언트에 전파한다.
    /// <c>BufferLast = true</c> 로 늦게 참여한 클라이언트도 마지막 상태를 수신한다.
    ///
    /// <para>파라미터 인코딩: 설정된 값은 해당 타입의 int 캐스트 값, <see cref="PresetSentinelNone"/>이면 건너뜀.</para>
    ///
    /// <para>새 PatientDescriptor / PatientMedicalState 필드 추가 시 이 메서드에도 파라미터를 추가하고
    /// <see cref="ApplyMedicalStatePreset"/> 호출부에도 동일하게 추가한다.</para>
    /// </summary>
    [ObserversRpc(BufferLast = true)]
    private void RpcSyncVitalMedicalState(
      string name,
      int sex,
      int age,
      int bloodType,
      int intendedTriage,
      int consciousnessGcs,
      int consciousnessLocLabel,
      int consciousnessPupillaryResponse,
      int respirationAwRR,
      int respirationTypeValue,
      int pulseRate,
      int pulseForceType,
      int bloodPressureSystolic,
      int bloodPressureDiastolic,
      int skinColorHue,
      int skinTemperatureType,
      int isCardiacArrest)
    {
      // 서버에서는 이미 ApplyMedicalStatePreset 에서 직접 적용했으므로 중복 처리하지 않는다.
      if (IsServerStarted)
        return;

      EnsureMedicalStateDefaults();

      // 환자 기술자
      if (name != null) _patientDescriptor.name = name;
      if (sex != PresetSentinelNone) _patientDescriptor.sex = (Sex)sex;
      if (age != PresetSentinelNone) _patientDescriptor.age = age;
      if (bloodType != PresetSentinelNone) _patientDescriptor.bloodType = (BloodType)bloodType;
      if (intendedTriage != PresetSentinelNone) _patientDescriptor.intendedTriage = (TriageLevel)intendedTriage;

      // 의식
      if (consciousnessGcs != PresetSentinelNone
          || consciousnessLocLabel != PresetSentinelNone
          || consciousnessPupillaryResponse != PresetSentinelNone)
      {
        if (_medicalState.consciousness == null)
          _medicalState.consciousness = Consciousness.Default;

        if (consciousnessGcs != PresetSentinelNone)
          _medicalState.consciousness.gcs = consciousnessGcs;
        if (consciousnessLocLabel != PresetSentinelNone)
          _medicalState.consciousness.locLabel = (LOCLabel)consciousnessLocLabel;
        if (consciousnessPupillaryResponse != PresetSentinelNone)
          _medicalState.consciousness.pupillaryResponse = (PupillaryResponse)consciousnessPupillaryResponse;
      }

      // 호흡
      if (respirationAwRR != PresetSentinelNone || respirationTypeValue != PresetSentinelNone)
      {
        if (_medicalState.respiration == null)
          _medicalState.respiration = new Respiration();

        if (respirationAwRR != PresetSentinelNone)
          _medicalState.respiration.awRR = respirationAwRR;
        if (respirationTypeValue != PresetSentinelNone)
          _medicalState.respiration.type = (RespirationType)respirationTypeValue;
      }

      // 맥박
      if (pulseRate != PresetSentinelNone || pulseForceType != PresetSentinelNone)
      {
        if (_medicalState.pulse == null)
          _medicalState.pulse = new BloodPulse();

        if (pulseRate != PresetSentinelNone)
          _medicalState.pulse.rate = pulseRate;
        if (pulseForceType != PresetSentinelNone)
          _medicalState.pulse.forceType = (BloodPulseForceType)pulseForceType;
      }

      // 혈압
      if (bloodPressureSystolic != PresetSentinelNone || bloodPressureDiastolic != PresetSentinelNone)
      {
        if (_medicalState.bloodPressure == null)
          _medicalState.bloodPressure = new BloodPressure();

        if (bloodPressureSystolic != PresetSentinelNone)
          _medicalState.bloodPressure.systolic = bloodPressureSystolic;
        if (bloodPressureDiastolic != PresetSentinelNone)
          _medicalState.bloodPressure.diastolic = bloodPressureDiastolic;
      }

      // 피부
      if (skinColorHue != PresetSentinelNone || skinTemperatureType != PresetSentinelNone)
      {
        if (_medicalState.skin == null)
          _medicalState.skin = Skin.Default;

        if (skinColorHue != PresetSentinelNone)
          _medicalState.skin.colorHue = (SkinColorHue)skinColorHue;
        if (skinTemperatureType != PresetSentinelNone)
          _medicalState.skin.temperatureType = (SkinTemperatureType)skinTemperatureType;
      }

      // 기타
      if (isCardiacArrest != PresetSentinelNone)
        _medicalState.isCardiacArrest = isCardiacArrest != 0;

      NotifyMedicalStateChanged();
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
