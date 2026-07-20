using TriageTrainer.Entity.Patient;

namespace MultiplayerInfrastructure.Scenario
{
  /// <summary>
  /// 프리셋 값이 대상 환자에게 적용되는 방식.
  /// </summary>
  public enum PatientMedicalStateTransitionMode
  {
    /// <summary>즉시(한 프레임에) 대상 값으로 덮어쓴다.</summary>
    Immediate = 0,

    /// <summary>
    /// 지정한 소요 시간(<see cref="ScenarioPatientMedicalStatePresetNode.TransitionDurationSeconds"/>)
    /// 동안 현재 값에서 대상 값으로 점차 보간(lerp)한다.
    /// 수치 필드(GCS/호흡수/맥박수/혈압 등)만 보간되며, 열거형/불리언 등 비수치 필드는 보간 종료 시점에 적용된다.
    /// 측정 불가(-1) 값은 보간 대상이 아니므로 즉시 적용된다.
    /// </summary>
    Gradual = 1
  }

  /// <summary>
  /// 환자 엔티티의 의료 상태(PatientDescriptor 및 PatientMedicalState)를 일괄 초기화(프리셋)하는 노드.
  ///
  /// <para>
  /// 지정한 엔티티 식별자(<see cref="TargetEntityIdentifier"/>)에 해당하는 <see cref="TriageTrainer.Entity.PatientController"/>를
  /// 찾아 아래 필드를 덮어쓴다. <b>null 인 항목은 현재 값을 유지</b>하므로,
  /// 설정이 필요한 필드만 기입하면 된다.
  /// </para>
  ///
  /// <para>새 환자 상태 필드를 추가할 때도 이 노드를 통해 프리셋 값을 설정할 수 있다.
  /// 아래 프로퍼티 목록에 필드를 추가하고, DTO(<see cref="ScenarioPatientMedicalStatePresetNodeDTO"/>),
  /// 로더(<c>ScenarioGraphLoader.ConvertPatientMedicalStatePreset</c>),
  /// 컨트롤러(<c>PatientController.ApplyMedicalStatePreset</c>) 세 곳에도 동일하게 추가한다.</para>
  ///
  /// <para>
  /// ── 프리셋 가능 필드 목록 ──
  /// <list type="table">
  /// <listheader><term>필드 그룹</term><term>프로퍼티</term><term>타입</term></listheader>
  /// <item><term>환자 기술자</term>    <term><see cref="Sex"/></term>              <term>Sex?</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="Age"/></term>              <term>int?</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="Name"/></term>             <term>string</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="BloodType"/></term>        <term>BloodType?</term></item>
  /// <item><term>환자 기술자</term>    <term><see cref="IntendedTriage"/></term>   <term>TriageLevel?</term></item>
  /// <item><term>의료 상태</term>      <term><see cref="ConsciousnessGcs"/></term> <term>int?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessEyeOpening"/></term><term>EyeOpeningResponse?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessVerbal"/></term><term>VerbalResponse?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessMotor"/></term><term>MotorResponse?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessLocLabel"/></term><term>LOCLabel?</term></item>
  /// <item><term>의료 상태/의식</term> <term><see cref="ConsciousnessPupillaryResponse"/></term><term>PupillaryResponse?</term></item>
  /// <item><term>의료 상태/호흡</term> <term><see cref="RespirationAwRR"/></term>  <term>int?</term></item>
  /// <item><term>의료 상태/호흡</term> <term><see cref="RespirationTypeValue"/></term><term>RespirationType?</term></item>
  /// <item><term>의료 상태/맥박</term> <term><see cref="PulseRate"/></term>        <term>int?</term></item>
  /// <item><term>의료 상태/맥박</term> <term><see cref="PulseForceType"/></term>   <term>BloodPulseForceType?</term></item>
  /// <item><term>의료 상태/혈압</term> <term><see cref="BloodPressureSystolic"/></term><term>int?</term></item>
  /// <item><term>의료 상태/혈압</term> <term><see cref="BloodPressureDiastolic"/></term><term>int?</term></item>
  /// <item><term>의료 상태/피부</term> <term><see cref="SkinColorHue"/></term>     <term>SkinColorHue?</term></item>
  /// <item><term>의료 상태/피부</term> <term><see cref="SkinTemperatureType"/></term><term>SkinTemperatureType?</term></item>
  /// <item><term>의료 상태/체온</term> <term><see cref="BodyTemperatureCelsius"/></term><term>float?</term></item>
  /// <item><term>의료 상태/모니터</term><term><see cref="Spo2"/></term>            <term>int?</term></item>
  /// <item><term>의료 상태</term>      <term><see cref="IsCardiacArrest"/></term>  <term>bool?</term></item>
  /// </list>
  /// </para>
  ///
  /// <para>
  /// ── 측정 불가/무의식/없음 표현 ──
  /// 수치 필드(<see cref="ConsciousnessGcs"/>, <see cref="RespirationAwRR"/>, <see cref="PulseRate"/>,
  /// <see cref="BloodPressureSystolic"/>, <see cref="BloodPressureDiastolic"/>)에 <b>-1</b>을 지정하면
  /// "무의식 / 호흡 없음 / 측정 불가" 등 <b>값이 존재하지 않는 상태</b>를 의미한다.
  /// 이 경우 환자 상태 모니터에는 해당 수치가 <c>-?-</c> 로 표시된다.
  /// (null은 "현재 값 유지", -1은 "측정 불가"로 서로 다른 의미임에 유의한다.)
  /// </para>
  ///
  /// <para>
  /// ── 전이(Transition) 방식 ──
  /// <see cref="TransitionMode"/> 로 프리셋 값이 적용되는 방식을 지정한다.
  /// <see cref="PatientMedicalStateTransitionMode.Immediate"/> 는 즉시 적용,
  /// <see cref="PatientMedicalStateTransitionMode.Gradual"/> 은 <see cref="TransitionDurationSeconds"/> 동안
  /// 수치 값을 현재 값에서 대상 값으로 점차 보간한다.
  /// </para>
  /// </summary>
  public sealed class ScenarioPatientMedicalStatePresetNode : IScenarioNode
  {
    public string Identifier { get; set; }
    public ScenarioNodeType NodeType => ScenarioNodeType.PatientMedicalStatePreset;
    public string NextIdentifier { get; set; }

    // ── 대상 엔티티 ──

    /// <summary>
    /// 상태를 초기화할 환자 엔티티 식별자.
    /// 비어 있으면 상태 저장소의 <see cref="TargetEntityStateKey"/> 값을 사용한다.
    /// </summary>
    public string TargetEntityIdentifier { get; set; }

    /// <summary>
    /// 상태 저장소에서 대상 엔티티 식별자를 간접 조회할 키.
    /// <see cref="TargetEntityIdentifier"/> 가 비어 있을 때만 사용된다.
    /// </summary>
    public string TargetEntityStateKey { get; set; }

    // ── 전이(Transition) ──

    /// <summary>
    /// 프리셋 값 적용 방식. 기본값은 <see cref="PatientMedicalStateTransitionMode.Immediate"/>(즉시).
    /// <see cref="PatientMedicalStateTransitionMode.Gradual"/> 로 지정하면
    /// <see cref="TransitionDurationSeconds"/> 동안 수치 값을 점차 변화시킨다.
    /// </summary>
    public PatientMedicalStateTransitionMode TransitionMode { get; set; } = PatientMedicalStateTransitionMode.Immediate;

    /// <summary>
    /// 점차 변화(<see cref="PatientMedicalStateTransitionMode.Gradual"/>) 시 소요 시간(초).
    /// <see cref="PatientMedicalStateTransitionMode.Immediate"/> 에서는 무시된다.
    /// 0 이하이면 즉시 적용과 동일하게 동작한다.
    /// </summary>
    public float TransitionDurationSeconds { get; set; }

    // ── 환자 기술자(PatientDescriptor) 프리셋 필드 ──
    // 새 PatientDescriptor 필드 추가 시 아래에 nullable 프로퍼티를 추가하고,
    // DTO / ScenarioGraphLoader.ConvertPatientMedicalStatePreset / PatientController.ApplyMedicalStatePreset
    // 세 곳에도 동일하게 추가한다.

    /// <summary>환자 성명. null이면 현재 값 유지.</summary>
    public string Name { get; set; }

    /// <summary>환자 성별(Sex.Male / Sex.Female). null이면 현재 값 유지.</summary>
    public Sex? Sex { get; set; }

    /// <summary>환자 나이. null이면 현재 값 유지.</summary>
    public int? Age { get; set; }

    /// <summary>환자 혈액형. null이면 현재 값 유지.</summary>
    public BloodType? BloodType { get; set; }

    /// <summary>의도된(정답) 트리아지 등급. null이면 현재 값 유지.</summary>
    public TriageLevel? IntendedTriage { get; set; }

    // ── 의료 상태(PatientMedicalState) 프리셋 필드 ──
    // 새 PatientMedicalState 필드 추가 시 아래에 nullable 프로퍼티를 추가하고,
    // DTO / ScenarioGraphLoader.ConvertPatientMedicalStatePreset / PatientController.ApplyMedicalStatePreset
    // 세 곳에도 동일하게 추가한다.

    // ── 의식(Consciousness) ──

    /// <summary>GCS 점수(3~15). null이면 현재 값 유지, -1이면 무의식(측정 불가).</summary>
    public int? ConsciousnessGcs { get; set; }

    /// <summary>GCS의 E(Eye Opening, 눈뜨기 반응) 세부 항목(1~4점). null이면 현재 값 유지.</summary>
    public EyeOpeningResponse? ConsciousnessEyeOpening { get; set; }

    /// <summary>GCS의 V(Verbal Response, 언어 반응) 세부 항목(1~5점). null이면 현재 값 유지.</summary>
    public VerbalResponse? ConsciousnessVerbal { get; set; }

    /// <summary>GCS의 M(Motor Response, 운동 반응) 세부 항목(1~6점). null이면 현재 값 유지.</summary>
    public MotorResponse? ConsciousnessMotor { get; set; }

    /// <summary>의식수준 5단계(LOC). null이면 현재 값 유지.</summary>
    public LOCLabel? ConsciousnessLocLabel { get; set; }

    /// <summary>동공 반사 상태. null이면 현재 값 유지.</summary>
    public PupillaryResponse? ConsciousnessPupillaryResponse { get; set; }

    // ── 호흡(Respiration) ──

    /// <summary>분당 호흡수(awRR). null이면 현재 값 유지, -1이면 호흡 없음(측정 불가).</summary>
    public int? RespirationAwRR { get; set; }

    /// <summary>호흡 유형. null이면 현재 값 유지.</summary>
    public RespirationType? RespirationTypeValue { get; set; }

    // ── 맥박(BloodPulse) ──

    /// <summary>분당 맥박수. null이면 현재 값 유지, -1이면 맥박 없음(측정 불가).</summary>
    public int? PulseRate { get; set; }

    /// <summary>맥박 세기 유형. null이면 현재 값 유지.</summary>
    public BloodPulseForceType? PulseForceType { get; set; }

    // ── 혈압(BloodPressure) ──

    /// <summary>수축기 혈압(mmHg). null이면 현재 값 유지, -1이면 측정 불가.</summary>
    public int? BloodPressureSystolic { get; set; }

    /// <summary>이완기 혈압(mmHg). null이면 현재 값 유지, -1이면 측정 불가.</summary>
    public int? BloodPressureDiastolic { get; set; }

    // ── 피부(Skin) ──

    /// <summary>피부 색조. null이면 현재 값 유지.</summary>
    public SkinColorHue? SkinColorHue { get; set; }

    /// <summary>피부 표면 온도 유형. null이면 현재 값 유지.</summary>
    public SkinTemperatureType? SkinTemperatureType { get; set; }

    // ── 체온(BodyTemperature) ──

    /// <summary>
    /// 심부 체온(°C). null이면 현재 값 유지, -1이면 측정 불가.
    /// 적용 시 <c>PatientMedicalState.bodyTemperature.celsius</c> 와 모니터 체온(T1)에 반영된다.
    /// </summary>
    public float? BodyTemperatureCelsius { get; set; }

    // ── 산소포화도(SpO2) ──

    /// <summary>
    /// 산소포화도(SpO2, %). null이면 현재 값 유지, -1이면 측정 불가.
    /// 적용 시 모니터 numerics/pleth 의 SpO2 에 반영된다.
    /// </summary>
    public int? Spo2 { get; set; }

    // ── 기타 의료 상태 ──

    /// <summary>심정지 여부. null이면 현재 값 유지.</summary>
    public bool? IsCardiacArrest { get; set; }
  }
}
