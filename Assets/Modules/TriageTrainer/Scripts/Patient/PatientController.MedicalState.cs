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
      var snapshot = _medicalState;

      // 세분화 상태 이벤트(OnVitalChanged) + 시나리오 바인딩 디스패치.
      RaiseVitalChangedEvent(snapshot);

      if (_medicalStateListeners.Count == 0)
        return;

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

      // ── 전이 방식 결정 ──
      // Gradual + 소요 시간 > 0 인 경우에만 점차 변화 코루틴을 실행한다.
      // (게임오브젝트가 비활성 상태이면 코루틴을 시작할 수 없으므로 즉시 적용으로 폴백한다.)
      bool gradual = preset.TransitionMode == PatientMedicalStateTransitionMode.Gradual
                     && preset.TransitionDurationSeconds > 0f
                     && isActiveAndEnabled;

      if (gradual)
      {
        // 비수치(열거형/불리언/문자열/피부/의식 라벨 등) 필드는 즉시 적용하고,
        // 수치 필드(GCS/호흡수/맥박수/혈압)는 소요 시간 동안 점차 보간한다.
        ApplyNonNumericPresetFields(preset);
        NotifyMedicalStateChanged();
        PropagatePresetToClients(preset, includeNumericVitals: false);

        if (_medicalStateTransitionRoutine != null)
          StopCoroutine(_medicalStateTransitionRoutine);
        _medicalStateTransitionRoutine = StartCoroutine(
          GradualNumericTransitionRoutine(preset, preset.TransitionDurationSeconds));
        return;
      }

      // ── 즉시 적용 ──
      if (_medicalStateTransitionRoutine != null)
      {
        StopCoroutine(_medicalStateTransitionRoutine);
        _medicalStateTransitionRoutine = null;
      }

      ApplyNonNumericPresetFields(preset);
      ApplyNumericPresetFields(preset);

      NotifyMedicalStateChanged();

      PropagatePresetToClients(preset, includeNumericVitals: true);
    }

    /// <summary>진행 중인 점차 변화(Gradual) 코루틴 핸들. 새 프리셋 적용 시 취소된다.</summary>
    private Coroutine _medicalStateTransitionRoutine;

    /// <summary>
    /// 서버 컨텍스트에서 프리셋 vital 필드를 모든 클라이언트에 전파한다.
    /// <c>BufferLast = true</c> 로, 늦게 참여한 클라이언트도 마지막 프리셋 상태를 수신한다.
    ///
    /// <para><paramref name="includeNumericVitals"/> 가 false이면 수치 필드(GCS/호흡수/맥박수/혈압)는
    /// <see cref="PresetSentinelNone"/> 로 전송되어 클라이언트에서 건너뛴다. 점차 변화(Gradual) 시작 시
    /// 비수치 필드만 먼저 전파하기 위해 사용한다. 수치 필드는 매 프레임 <see cref="RpcSyncMonitorMedicalState"/> 로
    /// 모니터 값이 전파되며, 종료 시 true 로 최종 확정 전파한다.</para>
    /// </summary>
    private void PropagatePresetToClients(ScenarioPatientMedicalStatePresetNode preset, bool includeNumericVitals)
    {
      if (!IsServerStarted)
        return;

      RpcSyncVitalMedicalState(
        name: preset.Name,
        sex: preset.Sex.HasValue ? (int)preset.Sex.Value : PresetSentinelNone,
        age: preset.Age ?? PresetSentinelNone,
        bloodType: preset.BloodType.HasValue ? (int)preset.BloodType.Value : PresetSentinelNone,
        intendedTriage: preset.IntendedTriage.HasValue ? (int)preset.IntendedTriage.Value : PresetSentinelNone,
        consciousnessGcs: includeNumericVitals ? (preset.ConsciousnessGcs ?? PresetSentinelNone) : PresetSentinelNone,
        consciousnessEyeOpening: preset.ConsciousnessEyeOpening.HasValue ? (int)preset.ConsciousnessEyeOpening.Value : PresetSentinelNone,
        consciousnessVerbal: preset.ConsciousnessVerbal.HasValue ? (int)preset.ConsciousnessVerbal.Value : PresetSentinelNone,
        consciousnessMotor: preset.ConsciousnessMotor.HasValue ? (int)preset.ConsciousnessMotor.Value : PresetSentinelNone,
        consciousnessLocLabel: preset.ConsciousnessLocLabel.HasValue ? (int)preset.ConsciousnessLocLabel.Value : PresetSentinelNone,
        consciousnessPupillaryResponse: preset.ConsciousnessPupillaryResponse.HasValue ? (int)preset.ConsciousnessPupillaryResponse.Value : PresetSentinelNone,
        respirationAwRR: includeNumericVitals ? (preset.RespirationAwRR ?? PresetSentinelNone) : PresetSentinelNone,
        respirationTypeValue: preset.RespirationTypeValue.HasValue ? (int)preset.RespirationTypeValue.Value : PresetSentinelNone,
        pulseRate: includeNumericVitals ? (preset.PulseRate ?? PresetSentinelNone) : PresetSentinelNone,
        pulseForceType: preset.PulseForceType.HasValue ? (int)preset.PulseForceType.Value : PresetSentinelNone,
        bloodPressureSystolic: includeNumericVitals ? (preset.BloodPressureSystolic ?? PresetSentinelNone) : PresetSentinelNone,
        bloodPressureDiastolic: includeNumericVitals ? (preset.BloodPressureDiastolic ?? PresetSentinelNone) : PresetSentinelNone,
        skinColorHue: preset.SkinColorHue.HasValue ? (int)preset.SkinColorHue.Value : PresetSentinelNone,
        skinTemperatureType: preset.SkinTemperatureType.HasValue ? (int)preset.SkinTemperatureType.Value : PresetSentinelNone,
        // 체온은 float 이므로 별도 파라미터(bodyTemperatureCelsius)로 전달하고, 값 없음은 NaN 으로 인코딩한다.
        bodyTemperatureCelsius: includeNumericVitals
          ? (preset.BodyTemperatureCelsius ?? float.NaN)
          : float.NaN,
        spo2: includeNumericVitals ? (preset.Spo2 ?? PresetSentinelNone) : PresetSentinelNone,
        isCardiacArrest: preset.IsCardiacArrest.HasValue ? (preset.IsCardiacArrest.Value ? 1 : 0) : PresetSentinelNone
      );
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
      int consciousnessEyeOpening,
      int consciousnessVerbal,
      int consciousnessMotor,
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
      float bodyTemperatureCelsius,
      int spo2,
      int isCardiacArrest)
    {
      // 서버에서는 이미 ApplyMedicalStatePreset 에서 직접 적용했으므로 중복 처리하지 않는다.
      if (IsServerStarted)
        return;

      EnsureMedicalStateDefaults();

      // 환자 기술자
      if (name != null)
      {
        _patientDescriptor.name = name;
        RefreshPatientDisplayName();
        CurrentBed?.RefreshDisplayName();
      }
      if (sex != PresetSentinelNone) _patientDescriptor.sex = (Sex)sex;
      if (age != PresetSentinelNone) _patientDescriptor.age = age;
      if (bloodType != PresetSentinelNone) _patientDescriptor.bloodType = (BloodType)bloodType;
      if (intendedTriage != PresetSentinelNone) _patientDescriptor.intendedTriage = (TriageLevel)intendedTriage;

      // 의식
      if (consciousnessGcs != PresetSentinelNone
          || consciousnessEyeOpening != PresetSentinelNone
          || consciousnessVerbal != PresetSentinelNone
          || consciousnessMotor != PresetSentinelNone
          || consciousnessLocLabel != PresetSentinelNone
          || consciousnessPupillaryResponse != PresetSentinelNone)
      {
        if (_medicalState.consciousness == null)
          _medicalState.consciousness = Consciousness.Default;

        if (consciousnessGcs != PresetSentinelNone)
          _medicalState.consciousness.gcs = consciousnessGcs;
        if (consciousnessEyeOpening != PresetSentinelNone)
          _medicalState.consciousness.eyeOpening = (EyeOpeningResponse)consciousnessEyeOpening;
        if (consciousnessVerbal != PresetSentinelNone)
          _medicalState.consciousness.verbal = (VerbalResponse)consciousnessVerbal;
        if (consciousnessMotor != PresetSentinelNone)
          _medicalState.consciousness.motor = (MotorResponse)consciousnessMotor;
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

      // 체온
      if (!float.IsNaN(bodyTemperatureCelsius))
      {
        if (_medicalState.bodyTemperature == null)
          _medicalState.bodyTemperature = new BodyTemperature();
        _medicalState.bodyTemperature.celsius = bodyTemperatureCelsius;
      }

      // 모니터 수치 구조체 브리지: 클라이언트에서도 환자 상태 모니터에 프리셋 수치가 반영되도록 한다.
      // -1(측정 불가)은 MonitorValueUnavailable 로 매핑되어 모니터에 -?- 로 표시된다.
      BridgeNumericVitalsToMonitor(
        pulseRate: pulseRate != PresetSentinelNone ? pulseRate : (int?)null,
        bloodPressureSystolic: bloodPressureSystolic != PresetSentinelNone ? bloodPressureSystolic : (int?)null,
        bloodPressureDiastolic: bloodPressureDiastolic != PresetSentinelNone ? bloodPressureDiastolic : (int?)null,
        bodyTemperatureCelsius: !float.IsNaN(bodyTemperatureCelsius) ? bodyTemperatureCelsius : (float?)null,
        spo2: spo2 != PresetSentinelNone ? spo2 : (int?)null);

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

    /// <summary>
    /// 비수치 프리셋 필드(환자 기술자, 의식 세부 반응/라벨/동공, 호흡 유형, 맥박 세기, 피부, 심정지)를 즉시 적용한다.
    /// 수치 필드(GCS/호흡수/맥박수/혈압)는 <see cref="ApplyNumericPresetFields"/> 에서 별도 처리한다.
    /// </summary>
    private void ApplyNonNumericPresetFields(ScenarioPatientMedicalStatePresetNode preset)
    {
      EnsureMedicalStateDefaults();

      // ── 환자 기술자(PatientDescriptor) ──
      if (preset.Name != null)
      {
        _patientDescriptor.name = preset.Name;
        RefreshPatientDisplayName();
        CurrentBed?.RefreshDisplayName();
      }
      if (preset.Sex.HasValue)
        _patientDescriptor.sex = preset.Sex.Value;
      if (preset.Age.HasValue)
        _patientDescriptor.age = preset.Age.Value;
      if (preset.BloodType.HasValue)
        _patientDescriptor.bloodType = preset.BloodType.Value;
      if (preset.IntendedTriage.HasValue)
        _patientDescriptor.intendedTriage = preset.IntendedTriage.Value;

      // ── 의식(Consciousness) 비수치 항목 ──
      if (preset.ConsciousnessEyeOpening.HasValue
          || preset.ConsciousnessVerbal.HasValue
          || preset.ConsciousnessMotor.HasValue
          || preset.ConsciousnessLocLabel.HasValue
          || preset.ConsciousnessPupillaryResponse.HasValue)
      {
        if (_medicalState.consciousness == null)
          _medicalState.consciousness = Consciousness.Default;

        if (preset.ConsciousnessEyeOpening.HasValue)
          _medicalState.consciousness.eyeOpening = preset.ConsciousnessEyeOpening.Value;
        if (preset.ConsciousnessVerbal.HasValue)
          _medicalState.consciousness.verbal = preset.ConsciousnessVerbal.Value;
        if (preset.ConsciousnessMotor.HasValue)
          _medicalState.consciousness.motor = preset.ConsciousnessMotor.Value;
        if (preset.ConsciousnessLocLabel.HasValue)
          _medicalState.consciousness.locLabel = preset.ConsciousnessLocLabel.Value;
        if (preset.ConsciousnessPupillaryResponse.HasValue)
          _medicalState.consciousness.pupillaryResponse = preset.ConsciousnessPupillaryResponse.Value;
      }

      // ── 호흡 유형 / 맥박 세기 ──
      if (preset.RespirationTypeValue.HasValue)
      {
        if (_medicalState.respiration == null)
          _medicalState.respiration = new Respiration();
        _medicalState.respiration.type = preset.RespirationTypeValue.Value;
      }

      if (preset.PulseForceType.HasValue)
      {
        if (_medicalState.pulse == null)
          _medicalState.pulse = new BloodPulse();
        _medicalState.pulse.forceType = preset.PulseForceType.Value;
      }

      // ── 피부(Skin) ──
      if (preset.SkinColorHue.HasValue || preset.SkinTemperatureType.HasValue)
      {
        if (_medicalState.skin == null)
          _medicalState.skin = Skin.Default;

        if (preset.SkinColorHue.HasValue)
          _medicalState.skin.colorHue = preset.SkinColorHue.Value;
        if (preset.SkinTemperatureType.HasValue)
          _medicalState.skin.temperatureType = preset.SkinTemperatureType.Value;
      }

      // ── 기타 ──
      if (preset.IsCardiacArrest.HasValue)
        _medicalState.isCardiacArrest = preset.IsCardiacArrest.Value;
    }

    /// <summary>
    /// 수치 프리셋 필드(GCS/호흡수/맥박수/혈압)를 최종 대상 값으로 즉시 적용하고,
    /// 모니터 수치 구조체(<c>numerics</c>/<c>nibp</c>)에 브리지한다.
    /// -1(측정 불가)은 모니터에 <c>-?-</c> 로 표시되도록 매핑된다.
    /// </summary>
    private void ApplyNumericPresetFields(ScenarioPatientMedicalStatePresetNode preset)
    {
      EnsureMedicalStateDefaults();

      if (preset.ConsciousnessGcs.HasValue)
      {
        if (_medicalState.consciousness == null)
          _medicalState.consciousness = Consciousness.Default;
        _medicalState.consciousness.gcs = preset.ConsciousnessGcs.Value;
      }

      if (preset.RespirationAwRR.HasValue)
      {
        if (_medicalState.respiration == null)
          _medicalState.respiration = new Respiration();
        _medicalState.respiration.awRR = preset.RespirationAwRR.Value;
      }

      if (preset.PulseRate.HasValue)
      {
        if (_medicalState.pulse == null)
          _medicalState.pulse = new BloodPulse();
        _medicalState.pulse.rate = preset.PulseRate.Value;
      }

      if (preset.BloodPressureSystolic.HasValue || preset.BloodPressureDiastolic.HasValue)
      {
        if (_medicalState.bloodPressure == null)
          _medicalState.bloodPressure = new BloodPressure();
        if (preset.BloodPressureSystolic.HasValue)
          _medicalState.bloodPressure.systolic = preset.BloodPressureSystolic.Value;
        if (preset.BloodPressureDiastolic.HasValue)
          _medicalState.bloodPressure.diastolic = preset.BloodPressureDiastolic.Value;
      }

      if (preset.BodyTemperatureCelsius.HasValue)
      {
        if (_medicalState.bodyTemperature == null)
          _medicalState.bodyTemperature = new BodyTemperature();
        _medicalState.bodyTemperature.celsius = preset.BodyTemperatureCelsius.Value;
      }

      BridgeNumericVitalsToMonitor(
        pulseRate: preset.PulseRate,
        bloodPressureSystolic: preset.BloodPressureSystolic,
        bloodPressureDiastolic: preset.BloodPressureDiastolic,
        bodyTemperatureCelsius: preset.BodyTemperatureCelsius,
        spo2: preset.Spo2);
    }

    /// <summary>
    /// 프리셋 수치 vital을 모니터 수치 구조체(<c>numerics</c>/<c>nibp</c>/<c>pleth</c>/<c>temperature</c>)에 반영한다.
    /// 음수(-1 등, 측정 불가)는 <see cref="PatientMedicalState.MonitorValueUnavailable"/> 로 매핑되어
    /// 모니터에 <c>-?-</c> 로 표시된다. null(값 미지정)은 기존 모니터 값을 유지한다.
    ///
    /// <para>체온은 모니터 심부체온 채널(<c>temperature.t1</c>)에, SpO2는 <c>numerics.spo2</c> 와
    /// pleth 파형 계산에 쓰이는 <c>pleth.spo2</c> 양쪽에 반영한다.</para>
    /// </summary>
    private void BridgeNumericVitalsToMonitor(
      int? pulseRate,
      int? bloodPressureSystolic,
      int? bloodPressureDiastolic,
      float? bodyTemperatureCelsius = null,
      int? spo2 = null)
    {
      var numerics = _medicalState.numerics;
      var nibp = _medicalState.nibp;
      var pleth = _medicalState.pleth;
      var temperature = _medicalState.temperature;

      if (pulseRate.HasValue)
      {
        float v = pulseRate.Value < 0 ? PatientMedicalState.MonitorValueUnavailable : pulseRate.Value;
        numerics.bpm = v;
        numerics.pulseRate = v;
      }

      if (bloodPressureSystolic.HasValue)
        nibp.systolic = bloodPressureSystolic.Value < 0 ? PatientMedicalState.MonitorValueUnavailable : bloodPressureSystolic.Value;
      if (bloodPressureDiastolic.HasValue)
        nibp.diastolic = bloodPressureDiastolic.Value < 0 ? PatientMedicalState.MonitorValueUnavailable : bloodPressureDiastolic.Value;

      if (spo2.HasValue)
      {
        float v = spo2.Value < 0 ? PatientMedicalState.MonitorValueUnavailable : spo2.Value;
        numerics.spo2 = v;
        pleth.spo2 = v;
      }

      if (bodyTemperatureCelsius.HasValue)
      {
        temperature.t1 = bodyTemperatureCelsius.Value < 0
          ? PatientMedicalState.MonitorValueUnavailable
          : bodyTemperatureCelsius.Value;
      }

      _medicalState.numerics = numerics;
      _medicalState.nibp = nibp;
      _medicalState.pleth = pleth;
      _medicalState.temperature = temperature;
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

    /// <summary>
    /// 점차 변화(Gradual) 중 모니터 수치를 클라이언트로 전파하는 최소 간격(초).
    /// 로컬 렌더링은 매 프레임 갱신하되, 네트워크 RPC는 이 간격으로 스로틀링하여
    /// 긴 전이/다수 클라이언트에서 대역폭을 절약한다(약 10Hz).
    /// </summary>
    private const float GradualTransitionSyncIntervalSeconds = 0.1f;

    /// <summary>
    /// 점차 변화(Gradual) 코루틴. <paramref name="durationSeconds"/> 동안 수치 필드를
    /// 현재 값에서 대상 값으로 선형 보간(lerp)한다. 측정 불가(-1) 값은 보간하지 않고
    /// 종료 시점에 즉시 확정 적용한다(수치가 서서히 줄어드는 연출이 부적절하므로).
    /// 로컬 렌더링은 매 프레임 갱신하고, 서버 컨텍스트에서는 모니터 수치를
    /// <see cref="GradualTransitionSyncIntervalSeconds"/> 간격으로 클라이언트에 전파하며,
    /// 종료 시 전체 vital 필드를 최종 값으로 확정 전파한다.
    /// </summary>
    private System.Collections.IEnumerator GradualNumericTransitionRoutine(
      ScenarioPatientMedicalStatePresetNode preset, float durationSeconds)
    {
      // 보간 시작(from) 값 스냅샷
      float fromGcs = _medicalState.consciousness?.gcs ?? 0;
      float fromRr = _medicalState.respiration?.awRR ?? 0;
      float fromPulse = _medicalState.numerics.bpm;
      float fromSys = _medicalState.nibp.systolic;
      float fromDia = _medicalState.nibp.diastolic;
      float fromTemp = _medicalState.temperature.t1;
      float fromSpo2 = _medicalState.numerics.spo2;

      // 측정 불가(-1) 대상은 보간에서 제외한다.
      bool gcsUnavailable = preset.ConsciousnessGcs.HasValue && preset.ConsciousnessGcs.Value < 0;
      bool rrUnavailable = preset.RespirationAwRR.HasValue && preset.RespirationAwRR.Value < 0;
      bool pulseUnavailable = preset.PulseRate.HasValue && preset.PulseRate.Value < 0;
      bool sysUnavailable = preset.BloodPressureSystolic.HasValue && preset.BloodPressureSystolic.Value < 0;
      bool diaUnavailable = preset.BloodPressureDiastolic.HasValue && preset.BloodPressureDiastolic.Value < 0;
      bool tempUnavailable = preset.BodyTemperatureCelsius.HasValue && preset.BodyTemperatureCelsius.Value < 0;
      bool spo2Unavailable = preset.Spo2.HasValue && preset.Spo2.Value < 0;

      float elapsed = 0f;
      float sinceLastSync = 0f;
      while (elapsed < durationSeconds)
      {
        elapsed += Time.deltaTime;
        sinceLastSync += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / durationSeconds);

        if (preset.ConsciousnessGcs.HasValue && !gcsUnavailable && _medicalState.consciousness != null)
          _medicalState.consciousness.gcs = Mathf.RoundToInt(Mathf.Lerp(fromGcs, preset.ConsciousnessGcs.Value, t));
        if (preset.RespirationAwRR.HasValue && !rrUnavailable && _medicalState.respiration != null)
          _medicalState.respiration.awRR = Mathf.RoundToInt(Mathf.Lerp(fromRr, preset.RespirationAwRR.Value, t));

        int? interpPulse = (preset.PulseRate.HasValue && !pulseUnavailable)
          ? Mathf.RoundToInt(Mathf.Lerp(fromPulse, preset.PulseRate.Value, t)) : (int?)null;
        int? interpSys = (preset.BloodPressureSystolic.HasValue && !sysUnavailable)
          ? Mathf.RoundToInt(Mathf.Lerp(fromSys, preset.BloodPressureSystolic.Value, t)) : (int?)null;
        int? interpDia = (preset.BloodPressureDiastolic.HasValue && !diaUnavailable)
          ? Mathf.RoundToInt(Mathf.Lerp(fromDia, preset.BloodPressureDiastolic.Value, t)) : (int?)null;

        if (interpPulse.HasValue && _medicalState.pulse != null)
          _medicalState.pulse.rate = interpPulse.Value;
        if (_medicalState.bloodPressure != null)
        {
          if (interpSys.HasValue)
            _medicalState.bloodPressure.systolic = interpSys.Value;
          if (interpDia.HasValue)
            _medicalState.bloodPressure.diastolic = interpDia.Value;
        }

        // 체온(float)은 정수 반올림 없이 보간한다. SpO2는 정수로 보간한다.
        float? interpTemp = (preset.BodyTemperatureCelsius.HasValue && !tempUnavailable)
          ? Mathf.Lerp(fromTemp, preset.BodyTemperatureCelsius.Value, t) : (float?)null;
        int? interpSpo2 = (preset.Spo2.HasValue && !spo2Unavailable)
          ? Mathf.RoundToInt(Mathf.Lerp(fromSpo2, preset.Spo2.Value, t)) : (int?)null;

        if (interpTemp.HasValue && _medicalState.bodyTemperature != null)
          _medicalState.bodyTemperature.celsius = interpTemp.Value;

        BridgeNumericVitalsToMonitor(interpPulse, interpSys, interpDia, interpTemp, interpSpo2);

        // 로컬 렌더링은 매 프레임 갱신한다(호스트/오프라인 모니터가 부드럽게 보이도록).
        NotifyMedicalStateChanged();

        // 네트워크 전파는 스로틀링한다. 최종 확정 값은 루프 종료 후 PropagatePresetToClients 로 전송된다.
        if (IsServerStarted && sinceLastSync >= GradualTransitionSyncIntervalSeconds)
        {
          sinceLastSync = 0f;
          RpcSyncMonitorMedicalState(
            _medicalState.ecg, _medicalState.art, _medicalState.cvp, _medicalState.pleth,
            _medicalState.numerics, _medicalState.nibp, _medicalState.temperature, _medicalState.stLeads);
        }

        yield return null;
      }

      // 최종 확정: 수치 필드를 정확한 대상 값(측정 불가 -1 포함)으로 적용하고 전체 전파한다.
      ApplyNumericPresetFields(preset);
      NotifyMedicalStateChanged();
      PropagatePresetToClients(preset, includeNumericVitals: true);

      _medicalStateTransitionRoutine = null;
    }
  }
}
